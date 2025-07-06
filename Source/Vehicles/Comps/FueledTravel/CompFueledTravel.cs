using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using UnityEngine;
using Vehicles.Rendering;
using Vehicles.World;
using Verse;
using Verse.AI;

namespace Vehicles;

[PublicAPI, StaticConstructorOnStartup]
[HeaderTitle(Label = nameof(CompFueledTravel))]
public class CompFueledTravel : VehicleComp, IRefundable
{
  private const float FuelPerLeak = 1;
  private const float TicksPerLeakCheck = 120;
  private const float MaxTicksPerLeak = 400;

  private const float EfficiencyTickMultiplier = 1f / GenDate.TicksPerDay;
  private const float EfficiencyIdleMultiplier = 0.5f;
  private const float CellOffsetIntVec3ToVector3 = 0.5f;
  private const float TicksToCharge = 120;

  private static readonly Texture2D ElectricPowerTex =
    ContentFinder<Texture2D>.Get("UI/Overlays/NeedsPower");

  private static readonly MethodInfo PowerNetMethod;

  public bool allowAutoRefuel = true;
  private float fuel;
  private float targetFuelPercent = 1;

  private bool terminateMotes;
  private Vector3 motePosition;
  private float offsetX;
  private float offsetZ;

  // PowerNet::ChangeStoredEnergy(float)
  private Action<float> changeStoredEnergy;
  private CompPower connectedPower;
  private bool postLoadReconnect;

  private readonly Gizmo_RefuelableFuelTravel refuelGizmo;

  static CompFueledTravel()
  {
    PowerNetMethod = AccessTools.Method(typeof(PowerNet), "ChangeStoredEnergy");
  }

  public CompFueledTravel()
  {
    refuelGizmo = new Gizmo_RefuelableFuelTravel(this, false);
  }

  private List<(VehicleComponent component, Reactor_FuelLeak fuelLeak)> FuelComponents { get; set; }

  public bool FuelLeaking { get; private set; }

  public CompProperties_FueledTravel Props => props as CompProperties_FueledTravel;

  public override bool TickByRequest => true;

  public float Fuel => fuel;

  public float FuelPercent => Fuel / FuelCapacity;

  public bool EmptyTank => Fuel <= 0f;

  public bool FullTank => Mathf.Approximately(fuel, TargetFuelLevel);

  public int FuelCountToFull => Mathf.CeilToInt(TargetFuelLevel - Fuel);

  public float TargetFuelPercent
  {
    get { return targetFuelPercent; }
    set { targetFuelPercent = value; }
  }

  public float TargetFuelLevel => targetFuelPercent * FuelCapacity;

  private float FuelPercentOfTarget => TargetFuelLevel == 0 ? 0 : fuel / TargetFuelLevel;

  // Fuel Consumption
  public float ConsumptionRatePerTick => FuelEfficiency * EfficiencyTickMultiplier;

  public float ConsumptionRateWorldPerTick =>
    ConsumptionRatePerTick * Props.fuelConsumptionWorldMultiplier;

  public FuelConsumptionCondition FuelCondition => Props.fuelConsumptionCondition;

  public bool ShouldAutoRefuelNow => FuelPercentOfTarget <= Props.autoRefuelPercent &&
    !FullTank && TargetFuelLevel > 0f && ShouldAutoRefuelNowIgnoringFuelPct;

  private bool ShouldAutoRefuelNowIgnoringFuelPct => allowAutoRefuel && !Vehicle.Drafted &&
    !Vehicle.IsBurning() &&
    parent.Map.designationManager.DesignationOn(Vehicle,
      DesignationDefOf_Vehicles.DisassembleVehicle) == null;

  // Electric
  public bool Charging => connectedPower != null && !FullTank &&
    connectedPower.PowerNet.CurrentStoredEnergy() > Props.chargeRate;

  public IEnumerable<(ThingDef thingDef, float count)> Refunds
  {
    get
    {
      if (!Props.ElectricPowered)
      {
        yield return (Props.fuelType, Fuel);
      }
    }
  }

  protected virtual float ChargeRate
  {
    get
    {
      float chargeRate = SettingsCache.TryGetValue(Vehicle.VehicleDef,
        typeof(CompProperties_FueledTravel),
        nameof(CompProperties_FueledTravel.chargeRate),
        Props.chargeRate);
      chargeRate =
        Vehicle.statHandler.GetStatOffset(VehicleStatUpgradeCategoryDefOf.ChargeRate, chargeRate);
      return chargeRate;
    }
  }

  protected virtual float DischargeRate
  {
    get
    {
      float dischargeRate = SettingsCache.TryGetValue(Vehicle.VehicleDef,
        typeof(CompProperties_FueledTravel),
        nameof(CompProperties_FueledTravel.dischargeRate),
        Props.dischargeRate);
      dischargeRate =
        Vehicle.statHandler.GetStatOffset(VehicleStatUpgradeCategoryDefOf.DischargeRate,
          dischargeRate);
      return dischargeRate;
    }
  }

  public virtual float FuelEfficiency
  {
    get
    {
      float consumptionRate = SettingsCache.TryGetValue(Vehicle.VehicleDef,
        typeof(CompProperties_FueledTravel),
        nameof(CompProperties_FueledTravel.fuelConsumptionRate), Props.fuelConsumptionRate);
      consumptionRate =
        Vehicle.statHandler.GetStatOffset(VehicleStatUpgradeCategoryDefOf.FuelConsumptionRate,
          consumptionRate);
      return consumptionRate;
    }
  }

  public virtual float FuelCapacity
  {
    get
    {
      float fuelCapacity = SettingsCache.TryGetValue(Vehicle.VehicleDef,
        typeof(CompProperties_FueledTravel), nameof(CompProperties_FueledTravel.fuelCapacity),
        Props.fuelCapacity);
      fuelCapacity =
        Vehicle.statHandler.GetStatOffset(VehicleStatUpgradeCategoryDefOf.FuelCapacity,
          fuelCapacity);
      return fuelCapacity;
    }
  }

  /// Flying fuel consumption is handled in <see cref="AerialVehicleInFlight.SpendFuel"/>
  private bool ShouldConsumeNow => !EmptyTank && Vehicle.Spawned && (ConsumeWhenDrafted ||
    ConsumeWhenMoving || ConsumeAlways);

  private bool ConsumeAlways => FuelCondition.HasFlag(FuelConsumptionCondition.Always);

  private bool ConsumeWhenDrafted => Vehicle.Spawned &&
    FuelCondition.HasFlag(FuelConsumptionCondition.Drafted) && Vehicle.Drafted;

  private bool ConsumeWhenMoving
  {
    get
    {
      if (FuelCondition.HasFlag(FuelConsumptionCondition.Moving))
      {
        if (Vehicle.Spawned && Vehicle.vehiclePather.Moving)
        {
          return true;
        }

        if (Vehicle.GetVehicleCaravan() is { } caravan && caravan.vehiclePather.MovingNow)
        {
          return true;
        }
      }

      return false;
    }
  }

  public virtual Thing ClosestFuelAvailable(Pawn pawn)
  {
    if (Props.ElectricPowered)
      return null;
    return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
      ThingRequest.ForDef(Props.fuelType), PathEndMode.ClosestTouch, TraverseParms.For(pawn),
      validator: Validator);

    bool Validator(Thing thing)
    {
      return !thing.IsForbidden(pawn) && pawn.CanReserve(thing) && thing.def == Props.fuelType;
    }
  }

  public override AcceptanceReport CanMove(FloatMenuContext context)
  {
    if (EmptyTank)
      return "VF_OutOfFuel".Translate(Vehicle);
    return true;
  }

  public override AcceptanceReport CanDraft()
  {
    if (EmptyTank)
      return "VF_OutOfFuel".Translate(Vehicle);
    return true;
  }

  public virtual void Refuel(List<Thing> fuelThings)
  {
    int countToFull = FuelCountToFull;
    while (countToFull > 0 && fuelThings.Count > 0)
    {
      Thing thing = fuelThings.Pop();
      int count = Mathf.Min(countToFull, thing.stackCount);
      Refuel(count);
      thing.SplitOff(count).Destroy();
      countToFull -= count;
    }
  }

  public virtual void Refuel(float amount)
  {
    if (fuel >= FuelCapacity)
      return;

    fuel += amount;
    Vehicle.EventRegistry?[VehicleEventDefOf.Refueled].ExecuteEvents();
    if (fuel >= FuelCapacity)
    {
      fuel = FuelCapacity;
    }
  }

  /// <summary>
  /// Only for Incident spawning / AI spawning. Will randomize fuel levels later (REDO)
  /// </summary>
  private void RefuelHalfway()
  {
    ConsumeFuel(float.MaxValue);
    Refuel(FuelCapacity / 2f);
  }

  public virtual void ConsumeFuel(float amount)
  {
    if (fuel <= 0f)
      return;

    fuel -= amount;
    if (fuel <= 0f)
    {
      fuel = 0f;
      Vehicle.EventRegistry[VehicleEventDefOf.OutOfFuel].ExecuteEvents();
    }
  }

  public virtual void ConsumeFuelWorld()
  {
    if (fuel <= 0f)
      return;

    float fuelToConsume = ConsumptionRateWorldPerTick;
    VehicleCaravan caravan = Vehicle.GetVehicleCaravan();
    if (!caravan.vehiclePather.Moving) fuelToConsume *= EfficiencyIdleMultiplier;

    fuel -= fuelToConsume;
    if (fuel <= 0f)
    {
      fuel = 0f;
      Vehicle.EventRegistry[VehicleEventDefOf.OutOfFuel].ExecuteEvents();
    }
  }

  public override void PostDraw()
  {
    base.PostDraw();
    if (EmptyTank)
    {
      parent.Map.overlayDrawer.DrawOverlay(parent,
        Props.ElectricPowered ? OverlayTypes.NeedsPower : OverlayTypes.OutOfFuel);
    }
  }

  public override float CompStatCard(Rect rect)
  {
    Widgets.DrawHighlightIfMouseover(rect);
    rect.SplitVertically(rect.width / 2, out Rect labelRect, out Rect valueRect);
    using TextBlock fontBlock = new(GameFont.Small);
    //TooltipHandler.TipRegionByKey(rect, fuel tooltip here...);

    float fuelConsumption = ConsumptionRateWorldPerTick;
    float fuelPerDay = 0;
    if (fuelConsumption > 0)
    {
      // Conversion for tiles per day
      fuelPerDay = fuelConsumption * GenDate.TicksPerDay;
    }
    using (new TextBlock(TextAnchor.MiddleLeft))
    {
      Widgets.Label(labelRect, Props.GizmoLabel);
    }
    using (new TextBlock(TextAnchor.MiddleRight))
    {
      string fuelPerDayText = "VF_PerDay".Translate();
      float width = Text.CalcSize(fuelPerDayText).x;
      Rect fuelSuffixRect = valueRect;
      fuelSuffixRect.xMin = fuelSuffixRect.xMax - width;
      Widgets.Label(fuelSuffixRect, fuelPerDayText);
      Rect fuelIconRect = valueRect with { width = valueRect.height };
      fuelIconRect.x = fuelSuffixRect.x - fuelIconRect.width;
      if (Props.ElectricPowered)
      {
        GUI.DrawTexture(fuelIconRect, ElectricPowerTex);
      }
      else
      {
        Widgets.DefIcon(fuelIconRect, Props.fuelType);
        TooltipHandler.TipRegion(fuelIconRect, Props.fuelType.LabelCap);
      }
      valueRect.xMax = fuelIconRect.xMin;
      Widgets.Label(valueRect, $"{fuelPerDay:0.#}");
    }
    return Text.LineHeight;
  }

  public override IEnumerable<Gizmo> CompGetGizmosExtra()
  {
    foreach (Gizmo gizmo in base.CompGetGizmosExtra())
    {
      yield return gizmo;
    }

    if (Find.Selector.SelectedObjects.Count == 1)
    {
      yield return refuelGizmo;
    }

    if (DebugSettings.ShowDevGizmos)
    {
      foreach (Gizmo gizmo in DevModeGizmos())
      {
        yield return gizmo;
      }
    }
  }

  public override IEnumerable<Gizmo> CompCaravanGizmos()
  {
    yield return new Gizmo_RefuelableFuelTravel(this, true);

    if (DebugSettings.ShowDevGizmos)
    {
      yield return new Command_Action
      {
        defaultLabel = $"Vehicle Dev: [{Vehicle.Label}] Set fuel to 0.",
        action = delegate { ConsumeFuel(float.MaxValue); }
      };
      yield return new Command_Action
      {
        defaultLabel = $"Vehicle Dev: [{Vehicle.Label}] Set fuel to max.",
        action = delegate { Refuel(FuelCapacity); }
      };
    }
  }

  public virtual IEnumerable<Gizmo> DevModeGizmos()
  {
    yield return new Command_Action
    {
      defaultLabel = "Debug: Set fuel to 0",
      action = delegate { ConsumeFuel(float.MaxValue); }
    };
    yield return new Command_Action
    {
      defaultLabel = "Debug: Set fuel to half",
      action = RefuelHalfway
    };
    yield return new Command_Action
    {
      defaultLabel = "Debug: Set fuel to max",
      action = delegate { Refuel(FuelCapacity); }
    };
    yield return new Command_Action
    {
      defaultLabel = "Debug: Set fuel to 99.99%",
      action = delegate
      {
        ConsumeFuel(float.MaxValue);
        Refuel(FuelCapacity * 0.999999f);
      }
    };
  }

  public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
  {
    yield return new FloatMenuOption("Refuel".Translate().ToString(),
      delegate
      {
        Job job = new(JobDefOf_Vehicles.RefuelVehicle, parent, ClosestFuelAvailable(selPawn));
        selPawn.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);
      });
  }

  public override void CompCaravanInspectString(StringBuilder stringBuilder)
  {
    if (EmptyTank)
    {
      stringBuilder.AppendLine("VF_OutOfFuel".Translate(Vehicle));
    }
  }

  private void RevalidateConsumptionStatus()
  {
    if (ShouldConsumeNow || Charging)
    {
      StartTicking();
    }
    else
    {
      StopTicking();
    }
  }

  protected void ChangeStoredEnergy(float extra)
  {
    if (changeStoredEnergy == null && connectedPower.PowerNet != null)
    {
      changeStoredEnergy =
        AccessTools.MethodDelegate<Action<float>>(PowerNetMethod, connectedPower.PowerNet,
          virtualCall: false, delegateArgs: [typeof(float)]);
    }
    changeStoredEnergy?.Invoke(extra);
  }

  public override void CompTick()
  {
    float fuelToConsume = ConsumptionRatePerTick;
    if (!Vehicle.vehiclePather.Moving) fuelToConsume *= EfficiencyIdleMultiplier;
    ConsumeFuel(fuelToConsume);

    // TODO - Remove when animation system is finalized
    if (!terminateMotes && !Props.motesGenerated.NullOrEmpty() &&
      Find.TickManager.TicksGame % Props.ticksToSpawnMote == 0)
    {
      DrawMotes();
    }

    if (EmptyTank && !VehicleMod.settings.debug.debugDraftAnyVehicle)
    {
      Vehicle.ignition.Drafted = false;
    }

    if (Props.ElectricPowered)
    {
      if (!Charging)
      {
        ConsumeFuel(Mathf.Min(DischargeRate * EfficiencyTickMultiplier, Fuel));
      }
      else if (Find.TickManager.TicksGame % TicksToCharge == 0)
      {
        ChangeStoredEnergy(-ChargeRate);
        Refuel(ChargeRate);
      }
    }
  }

  public void LeakTick()
  {
    if (Find.TickManager.TicksGame % TicksPerLeakCheck == 0 && !FuelComponents.NullOrEmpty())
    {
      FuelLeaking = false;
      foreach ((VehicleComponent component, Reactor_FuelLeak fuelLeak) in FuelComponents)
      {
        FuelLeaking |= component.HealthPercent <= fuelLeak.healthPercent;
      }
    }

    if (FuelLeaking)
    {
      foreach ((VehicleComponent component, Reactor_FuelLeak fuelLeak) in FuelComponents)
      {
        float t = (fuelLeak.healthPercent - component.HealthPercent) * (1 / fuelLeak.healthPercent);
        float rate = Mathf.Lerp(fuelLeak.rate.min, fuelLeak.rate.max, t);
        if (rate == 0)
        {
          continue;
        }

        int ticksPerLeak = Mathf.CeilToInt(60 / rate);
        if (Find.TickManager.TicksGame % ticksPerLeak == 0)
        {
          ConsumeFuel(FuelPerLeak);
          if (Vehicle.Spawned && Props.leakDef != null && !EmptyTank)
          {
            IntVec2 offset =
              component.props.hitbox.cells.RandomElementWithFallback(fallback: IntVec2.Zero);
            IntVec3 leakCell = new(Vehicle.Position.x + offset.x, 0,
              Vehicle.Position.z + offset.z);
            FilthMaker.TryMakeFilth(leakCell, Vehicle.Map, Props.leakDef);
          }
        }
      }
    }
  }

  public override void CompTickRare()
  {
    base.CompTickRare();

    RevalidateConsumptionStatus(); //Intermittent checks to ensure no missed cases cause vehicle to drain

    if (!Vehicle.Spawned)
      return;

    if (!FullTank)
    {
      Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>()
       .RegisterLister(Vehicle, ReservationType.Refuel);
    }
    else
    {
      Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>()
       .RemoveLister(Vehicle, ReservationType.Refuel);
    }

    if (!Mathf.Approximately(Props.ambientHeat, 0))
    {
      GenTemperature.PushHeat(Vehicle, Props.ambientHeat);
    }

    if (Vehicle.vehiclePather.Moving)
    {
      DisconnectPower();
    }
  }

  public override void OnDeSpawn()
  {
    DisconnectPower();
  }

  public bool TryConnectPower()
  {
    if (!Props.ElectricPowered)
      return false;

    Vehicle.RequestTickStart(this);
    foreach (IntVec3 cell in Vehicle.InhabitedCells(1))
    {
      Thing building = Vehicle.Map.thingGrid.ThingAt(cell, ThingCategory.Building);
      CompPower powerSource = building?.TryGetComp<CompPower>();
      if (powerSource is { TransmitsPowerNow: true })
      {
        connectedPower = powerSource;
        return true;
      }
    }
    return false;
  }

  public void DisconnectPower()
  {
    connectedPower = null;
    changeStoredEnergy = null;
  }

  // TODO - Remove when animation system is finalized
  protected virtual void DrawMotes()
  {
    if (Props.motesGenerated.NullOrEmpty())
      return;

    foreach (OffsetMote offset in Props.motesGenerated)
    {
      for (int i = 0; i < offset.NumTimesSpawned; i++)
      {
        try
        {
          Vector2 moteOffset = VehicleGraphics.VehicleDrawOffset(Vehicle.FullRotation,
            offset.xOffset, offset.zOffset);
          offsetX = moteOffset.x;
          offsetZ = moteOffset.y;

          motePosition = new Vector3(parent.Position.x + offsetX + CellOffsetIntVec3ToVector3,
            parent.Position.y, parent.Position.z + offsetZ + CellOffsetIntVec3ToVector3);

          MoteThrown mote = (MoteThrown)ThingMaker.MakeThing(Props.moteDisplayed);
          mote.exactPosition = motePosition;
          mote.Scale = 1f;
          mote.rotationRate = 15f;
          float moteAngle = offset.predeterminedAngleVector ?? 0;
          float moteSpeed = offset.windAffected ?
            Rand.Range(0.5f, 3.5f) * Vehicle.Map.windManager.WindSpeed :
            offset.moteThrownSpeed;
          mote.SetVelocity(moteAngle, moteSpeed);
          RenderHelper.ThrowMoteEnhanced(motePosition, parent.Map, mote);
        }
        catch (Exception ex)
        {
          Log.Error(
            $"Exception thrown while trying to display {Props.moteDisplayed.defName}.\n{ex}");
          terminateMotes = true;
          return;
        }
      }
    }
  }

  public override void EventRegistration()
  {
    FuelComponents = new List<(VehicleComponent component, Reactor_FuelLeak fuelLeak)>();
    foreach (VehicleComponent component in Vehicle.statHandler.components.Where(component =>
      component.props.HasReactor<Reactor_FuelLeak>()))
    {
      if (component.props.HasReactor<Reactor_FuelLeak>())
      {
        FuelComponents.Add((component, component.props.GetReactor<Reactor_FuelLeak>()));
      }
    }

    Vehicle.AddEvent(VehicleEventDefOf.MoveStart, RevalidateConsumptionStatus);
    Vehicle.AddEvent(VehicleEventDefOf.MoveStop, RevalidateConsumptionStatus);
    Vehicle.AddEvent(VehicleEventDefOf.OutOfFuel, RevalidateConsumptionStatus);
    Vehicle.AddEvent(VehicleEventDefOf.Refueled, RevalidateConsumptionStatus);
    Vehicle.AddEvent(VehicleEventDefOf.IgnitionOn, RevalidateConsumptionStatus);
    Vehicle.AddEvent(VehicleEventDefOf.IgnitionOff, RevalidateConsumptionStatus);
    Vehicle.AddEvent(VehicleEventDefOf.HealthChanged, RevalidateConsumptionStatus);
  }

  public override void PostGeneration()
  {
    base.PostGeneration();
    targetFuelPercent = 1;
    if (Vehicle.Faction != Faction.OfPlayer)
    {
      Refuel(FuelCapacity * Rand.Range(0.45f, 0.85f));
    }
  }

  public override void PostSpawnSetup(bool respawningAfterLoad)
  {
    base.PostSpawnSetup(respawningAfterLoad);

    RevalidateConsumptionStatus();

    if (postLoadReconnect)
    {
      TryConnectPower();
    }
  }

  public override void PostExposeData()
  {
    base.PostExposeData();

    Scribe_Values.Look(ref allowAutoRefuel, nameof(allowAutoRefuel), defaultValue: true);
    Scribe_Values.Look(ref fuel, nameof(fuel));
    Scribe_Values.Look(ref targetFuelPercent, nameof(targetFuelPercent), defaultValue: 1);

    if (Scribe.mode == LoadSaveMode.Saving)
    {
      postLoadReconnect = Charging;
    }

    Scribe_Values.Look(ref postLoadReconnect, nameof(postLoadReconnect), defaultValue: false);
  }
}