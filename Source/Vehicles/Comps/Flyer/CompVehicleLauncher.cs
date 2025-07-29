using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Targeting;
using UnityEngine;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;

namespace Vehicles;

[PublicAPI]
[StaticConstructorOnStartup]
[HeaderTitle(Label = nameof(CompVehicleLauncher))]
public class CompVehicleLauncher : VehicleComp, ILauncher, ITargeterSource<GlobalTargetInfo, ArrivalOption>
{
  private static readonly List<ArrivalOption> ArrivalOptions = [];

  private static readonly SimpleCurve ClimbRateCurve =
  [
    new CurvePoint(0, -5),
    new CurvePoint(0.15f, -2.5f),
    new CurvePoint(0.25f, -1f),
    new CurvePoint(0.35f, -0.25f),
    new CurvePoint(0.45f, 0),
    new CurvePoint(0.5f, 0.25f),
    new CurvePoint(0.75f, 0.45f),
    new CurvePoint(0.8f, 0.75f),
    new CurvePoint(0.9f, 0.95f),
    new CurvePoint(1, 1)
  ];

  [GraphEditable]
  public LaunchProtocol launchProtocol;

  public float fuelEfficiencyWorldModifier;
  public float flightSpeedModifier;
  public bool? signalJammer;

  public float rateOfClimbModifier;
  public int maxAltitudeModifier;
  public int landingAltitudeModifier;

  private DeploymentTimer timer;

  // TODO - these 2 can be cleaned up
  public bool loiter;
  public bool inFlight;

  private Command_ActionHighlighter takeoffCommand;

  public bool AnyLeftToLoad => !Vehicle.cargoToLoad.NullOrEmpty();

  public CompProperties_VehicleLauncher Props => props as CompProperties_VehicleLauncher;

  bool ITargeterSource<GlobalTargetInfo, ArrivalOption>.TargeterValid => Vehicle.Spawned && !Vehicle.Destroyed;

  public PlanetTile Tile
  {
    get
    {
      if (!Vehicle.Spawned)
        return Vehicle.ParentHolder is WorldObject obj ? obj.Tile : PlanetTile.Invalid;
      return Vehicle.Map.Tile;
    }
  }

  public Vector3 Origin
  {
    get
    {
      if (Vehicle.ParentHolder is ILauncher launcher)
        return launcher.Origin;
      return WorldHelper.GetTilePos(Tile);
    }
  }

  public int MaxLaunchDistance => FixedMaxDistance > 0 ? FixedMaxDistance : int.MaxValue;

  public float FlightSpeed =>
    flightSpeedModifier + Vehicle.GetStatValue(VehicleStatDefOf.FlightSpeed);

  public float FuelConsumptionWorldMultiplier => fuelEfficiencyWorldModifier +
    SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher),
      nameof(Props.fuelConsumptionWorldMultiplier), Props.fuelConsumptionWorldMultiplier);

  public int FixedMaxDistance => SettingsCache.TryGetValue(Vehicle.VehicleDef,
    typeof(CompProperties_VehicleLauncher), nameof(Props.fixedLaunchDistanceMax),
    Props.fixedLaunchDistanceMax);

  public float RateOfClimb => rateOfClimbModifier + SettingsCache.TryGetValue(Vehicle.VehicleDef,
    typeof(CompProperties_VehicleLauncher), nameof(Props.rateOfClimb), Props.rateOfClimb);

  public int MaxAltitude => maxAltitudeModifier + SettingsCache.TryGetValue(Vehicle.VehicleDef,
    typeof(CompProperties_VehicleLauncher), nameof(Props.maxAltitude), Props.maxAltitude);

  public int LandingAltitude => landingAltitudeModifier +
    SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher),
      nameof(Props.landingAltitude), Props.landingAltitude);

  public bool ControlInFlight => SettingsCache.TryGetValue(Vehicle.VehicleDef,
    typeof(CompProperties_VehicleLauncher), nameof(Props.controlInFlight), Props.controlInFlight);

  public int ReconDistance => SettingsCache.TryGetValue(Vehicle.VehicleDef,
    typeof(CompProperties_VehicleLauncher), nameof(Props.reconDistance), Props.reconDistance);

  public bool SpaceFlight => SettingsCache.TryGetValue(Vehicle.VehicleDef,
    typeof(CompProperties_VehicleLauncher), nameof(Props.spaceFlight), Props.spaceFlight);

  public bool SignalJammer => signalJammer ?? SettingsCache.TryGetValue(Vehicle.VehicleDef,
    typeof(CompProperties_VehicleLauncher), nameof(Props.signalJammer), Props.signalJammer);

  public override bool TickByRequest => true;

  public override IEnumerable<AnimationDriver> Animations => launchProtocol.Animations;

  public IEnumerable<VehicleTurret> StrafeTurrets
  {
    get
    {
      if (Vehicle.CompVehicleTurrets is null)
      {
        Log.Error(
          "Cannot retrieve StrafeTurrets with no CompVehicleTurrets comp.");
        yield break;
      }
      foreach (VehicleTurret turret in Vehicle.CompVehicleTurrets.Turrets.Where(t =>
        Props.strafing.turrets.Contains(t.key) || Props.strafing.turrets.Contains(t.groupKey)))
      {
        yield return turret;
      }
    }
  }

  public void SetTimedDeployment()
  {
    timer.Reset(Props.deployTicks);
    StartTicking();
  }

  public override IEnumerable<Gizmo> CompGetGizmosExtra()
  {
    foreach (Gizmo gizmo in base.CompGetGizmosExtra())
      yield return gizmo;

    if (launchProtocol is null)
    {
      Log.ErrorOnce(
        $"No launch protocols for {Vehicle}. At least 1 must be included in order to initiate takeoff.",
        Vehicle.thingIDNumber);
      yield break;
    }

    takeoffCommand.Disabled = false;
    if (Vehicle.Spawned && launchProtocol.LaunchProperties.restriction != null)
    {
      takeoffCommand.mouseOver = () =>
        launchProtocol.LaunchProperties.restriction.DrawRestrictionsTargeter(Vehicle, Vehicle.Map,
          Vehicle.Position, Vehicle.Rotation);
    }
    if (!CanLaunchWithCargoCapacity(out string disableReason))
    {
      takeoffCommand.Disable(disableReason);
    }
    yield return takeoffCommand;
  }

  public bool CanLaunchWithCargoCapacity(out string disableReason)
  {
    disableReason = null;
    if (Vehicle.Spawned)
    {
      if (Vehicle.vehiclePather.Moving)
      {
        disableReason = "VF_CannotLaunchWhileMoving".Translate(Vehicle.LabelShort);
      }
      else if (Ext_Vehicles.IsRoofed(Vehicle.Position, Vehicle.Map))
      {
        disableReason = "CommandLaunchGroupFailUnderRoof".Translate();
      }
    }

    if (Vehicle.MovementPermissions.HasFlag(VehiclePermissions.Mobile))
    {
      if (!Vehicle.CanMoveFinal)
      {
        disableReason = "VF_CannotLaunchImmobile".Translate(Vehicle.LabelShort);
      }
      else if (Vehicle.Angle != 0)
      {
        disableReason = "VF_CannotLaunchRotated".Translate(Vehicle.LabelShort);
      }
    }
    else
    {
      float capacity = Vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity);
      if (MassUtility.InventoryMass(Vehicle) > capacity)
      {
        disableReason = "VF_CannotLaunchOverEncumbered".Translate(Vehicle.LabelShort);
      }
    }

    if ((!Vehicle.HasEnoughOperators || Vehicle.PawnCountToOperateLeft > 0) &&
      !VehicleMod.settings.debug.debugDraftAnyVehicle)
    {
      disableReason = "VF_NotEnoughToOperate".Translate();
    }
    else if (Vehicle.CompFueledTravel is { EmptyTank: true })
    {
      disableReason = "VF_LaunchOutOfFuel".Translate();
    }
    else if (FlightSpeed <= 0)
    {
      disableReason = "VF_NoFlightSpeed".Translate();
    }

    if (!launchProtocol.CanLaunchNow)
    {
      disableReason = launchProtocol.FailLaunchMessage;
    }
    return disableReason.NullOrEmpty();
  }

  public override string CompInspectStringExtra()
  {
    if (Vehicle.HasEnoughOperators && AnyLeftToLoad)
    {
      return
        $"{"NotReadyForLaunch".Translate()}: {"TransportPodInGroupHasSomethingLeftToLoad".Translate().CapitalizeFirst()}.";
    }
    return "ReadyForLaunch".Translate();
  }

  public float FuelNeededToLaunchAtDist(PlanetTile destination)
  {
    return FuelNeededToLaunchAtDist(Origin, destination);
  }

  public float FuelNeededToLaunchAtDist(Vector3 origin, PlanetTile destination)
  {
    float tileDistance = Ext_Math.SphericalDistance(origin, WorldHelper.GetTilePos(destination));
    return FuelNeededToLaunchAtDist(tileDistance);
  }

  public float FuelNeededToLaunchAtDist(float tileDistance)
  {
    if (Vehicle.CompFueledTravel == null)
      return 0;
    float speedPctPerTick = (AerialVehicleInFlight.PctPerTick / tileDistance) * FlightSpeed;
    float amount = Vehicle.CompFueledTravel.ConsumptionRatePerTickRaw *
      FuelConsumptionWorldMultiplier;
    return amount / speedPctPerTick;
  }

  private void StartChoosingDestination()
  {
    if (AnyLeftToLoad)
    {
      Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
        "ConfirmSendNotCompletelyLoadedLaunchable".Translate(Vehicle.LabelCapNoCount), ConfirmStart));
      return;
    }
    ConfirmStart();
    return;

    void ConfirmStart()
    {
      CameraJumper.TryJump(CameraJumper.GetWorldTarget(Vehicle));
      Find.WorldSelector.ClearSelection();
      ITargeterUpdate<GlobalTargetInfo> updater =
        Vehicle.CompFueledTravel != null ?
          new FuelTargetUpdater(Vehicle, this) :
          null;
      new WorldTargeter<ArrivalOption>(this, updater)
      {
        TargetTexture = TexData.TargeterMouseAttachment
      }.Start();
    }
  }

  public void Launch(TargetData<GlobalTargetInfo> targetData, IArrivalAction arrivalAction)
  {
    Assert.IsTrue(Vehicle.Spawned);
    Assert.IsFalse(targetData.targets.NullOrEmpty());
    Vehicle.CompVehicleLauncher.inFlight = true;
    Vehicle.CompVehicleLauncher.launchProtocol.OrderProtocol(LaunchProtocol.LaunchType.Takeoff);
    VehicleSkyfaller_Leaving vehicleLeaving =
      (VehicleSkyfaller_Leaving)VehicleSkyfallerMaker.MakeSkyfaller(Props.skyfallerLeaving,
        Vehicle);
    vehicleLeaving.arrivalAction = arrivalAction;
    vehicleLeaving.flightPath = targetData.targets.Select(target => new FlightNode(target)).ToList();
    GenSpawn.Spawn(vehicleLeaving, Vehicle.Position, Vehicle.Map,
      Vehicle.CompVehicleLauncher.launchProtocol.CurAnimationProperties.forcedRotation ?? Vehicle.Rotation);

    CameraJumper.TryHideWorld();
    Vehicle.EventRegistry[VehicleEventDefOf.AerialVehicleLaunch].ExecuteEvents();

#if DEBUG
    if (VehicleMod.settings.debug.debugInstantSendOff)
      AccessTools.Method(typeof(VehicleSkyfaller_Leaving), "LeaveMap").Invoke(vehicleLeaving, []);
#endif
  }

  public IEnumerable<ArrivalOption> OptionsAt(GlobalTargetInfo target)
  {
    return launchProtocol.GetArrivalOptions(target);
  }

  public TargetValidation CanTarget(GlobalTargetInfo target)
  {
    if (!target.IsValid)
      return TargetValidation.Failed;
    if (!CanReach(target, out TaggedString failReason) || !HasEnoughFuel(target, out failReason))
      return TargetValidation.Failed with { Tooltip = failReason };
    return TargetValidation.Success with { Tooltip = FloatMenuTooltip() };

    TaggedString FloatMenuTooltip()
    {
      using ClearOnDispose<ArrivalOption> cod = new(ArrivalOptions);
      ArrivalOptions.AddRange(launchProtocol.GetArrivalOptions(target));
      switch (ArrivalOptions.Count)
      {
        case 0:
          return null;
        case 1:
          if (!ArrivalOptions[0].AcceptanceReport.Accepted)
          {
            GUI.color = TexData.RedReadable;
          }
          return ArrivalOptions[0].label;
        case > 0:
          MapParent mapVehicle;
          if ((mapVehicle = (target.WorldObject as MapParent)) != null)
          {
            return "ClickToSeeAvailableOrders_WorldObject".Translate(mapVehicle.LabelCap);
          }
          return "ClickToSeeAvailableOrders_Empty".Translate();
        default:
          throw new InvalidOperationException(nameof(ArrivalOptions));
      }
    }
  }

  public TargeterResult Select(GlobalTargetInfo target)
  {
    List<ArrivalOption> options = OptionsAt(target).ToList();
    if (options.Count == 0)
      return TargeterResult.Reject;
    return TargeterResult.Accept(options);
  }

  void ITargeterSource<GlobalTargetInfo, ArrivalOption>.OnTargetingFinished(
    TargetData<GlobalTargetInfo> targetData,
    ArrivalOption arrivalOption)
  {
    if (arrivalOption.continueWith != null)
    {
      arrivalOption.continueWith(targetData);
    }
    else
    {
      Launch(targetData, arrivalOption.arrivalAction);
    }
  }

  // TODO - Decouple from CompFueledTravel
  private bool HasEnoughFuel(GlobalTargetInfo target, out TaggedString failReason)
  {
    failReason = null;
    float fuelCost = FuelNeededToLaunchAtDist(target.Tile); // TODO Launcher - Total fuel cost
    if (Vehicle.CompFueledTravel != null && fuelCost > Vehicle.CompFueledTravel.Fuel)
    {
      failReason = "VF_NotEnoughFuel".Translate().Colorize(TexData.RedReadable);
      return false;
    }
    if (Vehicle.CompFueledTravel != null && target.IsValid &&
      Vehicle.CompVehicleLauncher.FuelNeededToLaunchAtDist(Origin,
        target.Tile) > Vehicle.CompFueledTravel.Fuel - fuelCost)
    {
      failReason = "VF_NoFuelReturnTrip".Translate().Colorize(TexData.YellowReadable);
    }
    return true;
  }

  private bool CanReach(GlobalTargetInfo target, out TaggedString failReason)
  {
    failReason = null;
    if (!target.IsValid)
    {
      failReason = "MessageTransportPodsDestinationIsInvalid".Translate();
      return false;
    }
    if (target.HasWorldObject)
    {
      if (!SpaceFlight && target.WorldObject.def.GetModExtension<SpaceObjectDefModExtension>() != null)
      {
        failReason = "VF_NoSpaceFlight".Translate(Vehicle.LabelCap);
        return false;
      }
      if (ModsConfig.OdysseyActive && target.WorldObject.RequiresSignalJammerToReach &&
        !SignalJammer)
      {
        failReason = "TransportPodDestinationRequiresSignalJammer".Translate();
        return false;
      }
    }

    float tileDistance = Ext_Math.SphericalDistance(Origin, WorldHelper.GetTilePos(target.Tile));
    float fuelCost = FuelNeededToLaunchAtDist(tileDistance);
    if (tileDistance > MaxLaunchDistance ||
      Vehicle.CompFueledTravel is { } compFueledTravel && fuelCost > compFueledTravel.Fuel)
    {
      failReason = "TransportPodDestinationBeyondMaximumRange".Translate();
      return false;
    }
    return true;
  }


  public override void PostLoad()
  {
    ResolveProtocolProperties();

    takeoffCommand = new Command_ActionHighlighter
    {
      defaultLabel = "CommandLaunchGroup".Translate(),
      defaultDesc = "CommandLaunchGroupDesc".Translate(),
      icon = TexData.LaunchCommandTex,
      alsoClickIfOtherInGroupClicked = false,
      action = StartChoosingDestination
    };
  }

  public override void PostGeneration()
  {
    base.PostGeneration();
    InitLaunchProtocol();

    takeoffCommand = new Command_ActionHighlighter
    {
      defaultLabel = "CommandLaunchGroup".Translate(),
      defaultDesc = "CommandLaunchGroupDesc".Translate(),
      icon = TexData.LaunchCommandTex,
      alsoClickIfOtherInGroupClicked = false,
      action = StartChoosingDestination
    };
  }

  protected virtual void ResolveProtocolProperties()
  {
    launchProtocol.ResolveProperties(Props.launchProtocol);
  }

  private void InitLaunchProtocol()
  {
    if (Props.launchProtocol == null)
    {
      Log.Error("Vehicle has null launchProtocol.");
      return;
    }
    launchProtocol ??=
      (LaunchProtocol)Activator.CreateInstance(Props.launchProtocol.GetType(), Props.launchProtocol, Vehicle);
  }

  public override void CompTick()
  {
    timer.Tick(Vehicle);
    if (timer.Expired)
      StopTicking();
  }

  public override void PostSpawnSetup(bool respawningAfterLoad)
  {
    base.PostSpawnSetup(respawningAfterLoad);
    inFlight = false;
    if (!respawningAfterLoad)
    {
      fuelEfficiencyWorldModifier = 0;
    }
  }

  public override void PostExposeData()
  {
    base.PostExposeData();
    Scribe_Deep.Look(ref launchProtocol, nameof(launchProtocol));
    Scribe_Values.Look(ref flightSpeedModifier, nameof(flightSpeedModifier));
    Scribe_Values.Look(ref fuelEfficiencyWorldModifier, nameof(fuelEfficiencyWorldModifier));
    Scribe_Values.Look(ref signalJammer, nameof(signalJammer));

    Scribe_Values.Look(ref rateOfClimbModifier, nameof(rateOfClimbModifier));
    Scribe_Values.Look(ref maxAltitudeModifier, nameof(maxAltitudeModifier));
    Scribe_Values.Look(ref landingAltitudeModifier, nameof(landingAltitudeModifier));

    Scribe_Values.Look(ref inFlight, nameof(inFlight));
    Scribe_Values.Look(ref loiter, nameof(loiter));
    Scribe_Values.Look(ref timer, nameof(timer));
  }

  [PublicAPI]
  public struct DeploymentTimer
  {
    private int ticksLeft;
    private bool enabled;

    public DeploymentTimer(int ticksLeft, bool enabled)
    {
      this.ticksLeft = ticksLeft;
      this.enabled = enabled;
    }

    public static DeploymentTimer Default => new(0, false);

    public bool Expired => !enabled || ticksLeft <= 0;

    public void Reset(int delayDeploymentTicks)
    {
      ticksLeft = delayDeploymentTicks +
        Mathf.RoundToInt(VehicleMod.settings.main.delayDeployOnLanding * 60);
      enabled = true;
    }

    /// <summary>
    /// Tick DeploymentTimer for delayed disembarkation 
    /// </summary>
    /// <param name="vehicle"></param>
    /// <returns></returns>
    public void Tick(VehiclePawn vehicle)
    {
      ticksLeft--;
      if (enabled)
      {
        if (ticksLeft <= 0)
        {
          enabled = false;
          vehicle.DisembarkAll();
        }
      }
    }

    public static DeploymentTimer FromString(string entry)
    {
      entry = entry.TrimStart('(').TrimEnd(')');
      string[] data = entry.Split(',');

      try
      {
        CultureInfo invariantCulture = CultureInfo.InvariantCulture;
        int ticksLeft = Convert.ToInt32(data[0], invariantCulture);
        bool enabled = Convert.ToBoolean(data[1], invariantCulture);
        return new DeploymentTimer(ticksLeft, enabled);
      }
      catch (Exception ex)
      {
        SmashLog.Error(
          $"{entry} is not a valid <struct>DeploymentTimer</struct> format. Exception: {ex}");
        return Default;
      }
    }

    public override string ToString()
    {
      return $"({ticksLeft},{enabled})";
    }
  }
}