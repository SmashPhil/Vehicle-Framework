using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.World;

[PublicAPI]
[StaticConstructorOnStartup]
public class VehicleCaravan : Caravan, IVehicleWorldObject
{
  private const int RepairMothballTicks = 300;

  private static readonly Dictionary<VehicleDef, int> VehicleCounts = [];

  private static readonly MaterialPropertyBlock PropertyBlock = new();
  private static readonly Dictionary<ThingDef, Material> Materials = [];

  public VehicleCaravan_PathFollower vehiclePather;
  public VehicleCaravanTweener vehicleTweener;

  private VehiclePawn leadVehicle;
  private bool initialized;

  private bool repairing;

  // Strictly for appending pawns in vehicles to ThingOwner list of vanilla caravan.
  private List<Pawn> allPawns = [];

  // Vehicles in caravan
  private List<VehiclePawn> vehicles = [];

  public VehicleCaravan()
  {
    vehiclePather = new VehicleCaravan_PathFollower(this);
    vehicleTweener = new VehicleCaravanTweener(this);
  }

  public float ConstructionAverage { get; private set; }

  public bool VehiclesNeedRepairs { get; private set; }

  public override Vector3 DrawPos => vehicleTweener.TweenedPos;

  public bool CanDismount => true;

  public bool AerialVehicle =>
    vehicles.FirstOrDefault()?.VehicleDef.type == VehicleType.Air;

  public IEnumerable<VehiclePawn> Vehicles => vehicles;

  public List<VehiclePawn> VehiclesListForReading => vehicles;

  /// <summary>
  /// Strictly for appending internal pawns in AllPawns list from vanilla.
  /// </summary>
  public List<Pawn> AllPawnsAndVehiclePassengers => allPawns;

  public IEnumerable<Pawn> DismountedPawns
  {
    get
    {
      foreach (Pawn pawn in PawnsListForReading)
      {
        if (!(pawn is VehiclePawn) && !pawn.InVehicle())
        {
          yield return pawn;
        }
      }
    }
  }

  public bool Repairing
  {
    get { return !vehiclePather.Moving && repairing; }
    set { repairing = value; }
  }

  public VehiclePawn LeadVehicle
  {
    get
    {
      leadVehicle ??= PawnsListForReading.FirstOrDefault(v => v is VehiclePawn) as VehiclePawn;
      return leadVehicle;
    }
  }

  public override Material Material
  {
    get
    {
      VehicleDef leadVehicleDef = LeadVehicle?.VehicleDef;
      if (leadVehicleDef is null)
      {
        return null;
      }
      if (!Materials.ContainsKey(leadVehicleDef))
      {
        Texture2D texture = VehicleTex.CachedTextureIcons[leadVehicleDef];
        Material material = MaterialPool.MatFrom(texture,
          ShaderDatabase.WorldOverlayTransparentLit, Color.white,
          WorldMaterials.WorldObjectRenderQueue);
        Materials.Add(leadVehicleDef, material);
      }
      return Materials[leadVehicleDef];
    }
  }

  public bool OutOfFuel
  {
    get
    {
      foreach (VehiclePawn vehicle in vehicles)
      {
        if (vehicle.CompFueledTravel is { Fuel: <= 0 })
          return true;
      }
      return false;
    }
  }

  public bool VehicleCantMove
  {
    get { return VehiclesListForReading.Any(vehicle => !vehicle.CanMoveFinal); }
  }

  // TODO - just patch Caravan
  public new int TicksPerMove
  {
    get { return VehicleCaravanTicksPerMoveUtility.GetTicksPerMove(this); }
  }

  // TODO - just patch Caravan
  public new string TicksPerMoveExplanation
  {
    get
    {
      StringBuilder stringBuilder = new();
      _ = VehicleCaravanTicksPerMoveUtility.GetTicksPerMove(this, stringBuilder);
      return stringBuilder.ToString();
    }
  }

  public override void Draw()
  {
    float averageTileSize = Find.WorldGrid.AverageTileSize;
    float transitionPct = ExpandableWorldObjectsUtility.TransitionPct(this);
    if (def.expandingIcon && transitionPct > 0f)
    {
      Color color = Material.color;
      float num = 1f - transitionPct;
      PropertyBlock.SetColor(ShaderPropertyIDs.Color,
        new Color(color.r, color.g, color.b, color.a * num));
      WorldHelper.DrawQuadTangentialToPlanet(DrawPos, 0.7f * averageTileSize, 0.015f, Material,
        propertyBlock: PropertyBlock);
      return;
    }
    WorldHelper.DrawQuadTangentialToPlanet(DrawPos, 0.7f * averageTileSize, 0.015f, Material);
  }

  public override IEnumerable<Gizmo> GetGizmos()
  {
    foreach (Gizmo gizmo in base.GetGizmos())
    {
      if (gizmo is Command command && !command.icon)
      {
        continue; // skip all vanilla caravan devmode commands
      }
      yield return gizmo;
    }

    if (IsPlayerControlled)
    {
      if (AerialVehicle)
      {
        VehiclePawn vehicle = vehicles.FirstOrDefault();
        Assert.IsNotNull(vehicle);
        Command_Action launchCommand = new()
        {
          defaultLabel = "CommandLaunchGroup".Translate(),
          defaultDesc = "CommandLaunchGroupDesc".Translate(),
          icon = TexData.LaunchCommandTex,
          alsoClickIfOtherInGroupClicked = false,
          action = delegate
          {
            void LaunchAction() => LaunchTargeter.BeginTargeting(vehicle,
              (target, fuelCost) =>
                AerialVehicleLaunchHelper.ChoseTargetOnMap(vehicle, Tile, target, fuelCost), Tile,
              true, TexData.TargeterMouseAttachment, closeWorldTabWhenFinished: false,
              onUpdate: null,
              extraLabelGetter: (target, path, fuelCost) =>
                vehicle.CompVehicleLauncher.launchProtocol.TargetingLabelGetter(target, Tile,
                  path, fuelCost));

            int pawnCount = PawnsListForReading.Count;
            if (pawnCount > vehicle.TotalSeats)
            {
              Find.WindowStack.Add(new Dialog_Confirm(
                "VF_PawnsLeftBehindConfirm".Translate(),
                "VF_PawnsLeftBehindConfirmDesc".Translate(),
                LaunchAction));
            }
            else
            {
              LaunchAction();
            }
          }
        };
        if (!vehicle.CompVehicleLauncher.CanLaunchWithCargoCapacity(out string disableReason))
        {
          launchCommand.Disabled = true;
          launchCommand.disabledReason = disableReason;
        }
        yield return launchCommand;
      }
      if (vehiclePather.Moving)
      {
        yield return new Command_Toggle
        {
          defaultLabel = "CommandPauseCaravan".Translate(),
          defaultDesc =
            "CommandToggleCaravanPauseDesc".Translate(2f.ToString("0.#"), 0.3f.ToStringPercent()),
          icon = TexCommand.PauseCaravan,
          hotKey = KeyBindingDefOf.Misc1,
          isActive = () => vehiclePather.Paused,
          toggleAction = delegate
          {
            if (!vehiclePather.Moving)
            {
              return;
            }
            vehiclePather.Paused = !vehiclePather.Paused;
          },
        };
      }
      else
      {
        if (VehiclesNeedRepairs)
        {
          yield return new Command_Toggle
          {
            defaultLabel = "VF_ToggleRepairVehicle".Translate(),
            defaultDesc = "VF_ToggleRepairVehicleDesc".Translate(),
            icon = VehicleTex.RepairVehicles,
            hotKey = KeyBindingDefOf.Misc2,
            isActive = () => !vehiclePather.Moving && repairing && VehiclesNeedRepairs,
            toggleAction = delegate { repairing = !repairing; },
          };
        }

        Command_Action disembark = new()
        {
          icon = VehicleTex.StashVehicle,
          defaultLabel = "VF_CommandDisembark".Translate(),
          defaultDesc = "VF_CommandDisembarkDesc".Translate(),
          action = delegate { CaravanHelper.StashVehicles(this); }
        };

        // If tile is impassable, normal caravan won't be able to return
        if (Find.World.Impassable(Tile))
        {
          disembark.Disable("VF_CommandDisembarkImpassableBiome".Translate());
        }
        yield return disembark;
      }
      foreach (Gizmo gizmo2 in forage.GetGizmos())
      {
        yield return gizmo2;
      }
      foreach (WorldObject worldObject in Find.WorldObjects.ObjectsAt(Tile))
      {
        foreach (Gizmo gizmo3 in worldObject.GetCaravanGizmos(this))
        {
          yield return gizmo3;
        }
      }
    }

    foreach (VehiclePawn vehicle in VehiclesListForReading)
    {
      foreach (ThingComp thingComp in vehicle.AllComps)
      {
        if (thingComp is not VehicleComp vehicleComp)
          continue;

        foreach (Gizmo gizmo in vehicleComp.CompCaravanGizmos())
        {
          yield return gizmo;
        }
      }
    }

    if (DebugSettings.ShowDevGizmos)
    {
      yield return new Command_Action
      {
        defaultLabel = "Vehicle Dev: Teleport to destination",
        action = delegate
        {
          Tile = vehiclePather.Destination;
          vehiclePather.StopDead();
        }
      };
      yield return new Command_Action
      {
        defaultLabel = "Repair all Vehicles",
        action = delegate
        {
          foreach (VehiclePawn vehicle in VehiclesListForReading)
          {
            vehicle.statHandler.components.ForEach(c => c.HealComponent(float.MaxValue));
          }
        }
      };
    }
  }

  public void Notify_VehicleTeleported()
  {
    vehicleTweener.ResetTweenedPosToRoot();
    vehiclePather.Notify_Teleported_Int();
  }

  public override void Notify_Merged(List<Caravan> group)
  {
    base.Notify_Merged(group);
    RecacheVehiclesOrConvertCaravan();
    RecacheStatAverages();
  }

  public override void Notify_MemberDied(Pawn member)
  {
    if (!Spawned)
    {
      Log.Error(
        "Caravan member died in an unspawned caravan. Unspawned caravans shouldn't be kept for more than a single frame.");
    }
    if (!PawnsListForReading.NotNullAndAny(x =>
      x is VehiclePawn { Dead: false } vehicle &&
      vehicle.AllPawnsAboard.NotNullAndAny(pawn => pawn != member && IsOwner(pawn))))
    {
      RemovePawn(member);
      if (Faction == Faction.OfPlayer)
      {
        Find.LetterStack.ReceiveLetter("LetterLabelAllCaravanColonistsDied".Translate(),
          "LetterAllCaravanColonistsDied".Translate(Name).CapitalizeFirst(),
          LetterDefOf.NegativeEvent, new GlobalTargetInfo(Tile));
      }
      pawns.Clear();
      Destroy();
    }
    else
    {
      member.Strip();
      RemovePawn(member);
    }
    RecacheStatAverages();
  }

  /// <summary>
  /// Recache internal vehicle list for quick reading
  /// </summary>
  public void RecacheVehicles()
  {
    vehicles = pawns.InnerListForReading.Where(pawn => pawn is VehiclePawn).Cast<VehiclePawn>()
     .ToList();
    allPawns = [.. pawns.InnerListForReading];
    allPawns.AddRange(vehicles.SelectMany(vehicle => vehicle.AllPawnsAboard));
    leadVehicle = null;
  }

  /// <summary>
  /// Recache vehicle list and convert to normal caravan if no vehicles remain
  /// </summary>
  public void RecacheVehiclesOrConvertCaravan()
  {
    RecacheVehicles();
    ValidateCaravanType();
    RecacheStatAverages();
  }

  public void RecacheStatAverages()
  {
    float total = 0;
    int pawnCount = 0;
    foreach (Pawn pawn in PawnsListForReading)
    {
      if (pawn.IsColonistPlayerControlled || pawn.IsColonyMechPlayerControlled)
      {
        total += pawn.GetStatValue(StatDefOf.ConstructionSpeed);
        pawnCount++;
      }
    }
    float average = 1;
    if (pawnCount > 0)
    {
      average = total / pawnCount;
    }
    ConstructionAverage = average;

    VehiclesNeedRepairs = vehicles.Any(vehicle =>
      vehicle.statHandler.ComponentsPrioritized.Any(c => c.HealthPercent < 1));
  }

  public virtual void PostInit()
  {
    initialized = true;
    RecacheVehiclesOrConvertCaravan();
  }

  /// <summary>
  /// Convert to vanilla caravan if no vehicles exist in VehicleCaravan
  /// </summary>
  private void ValidateCaravanType()
  {
    if (initialized && vehicles.NullOrEmpty())
    {
      Debug.Message(
        $"VehicleCaravan {this} has no more vehicles. Converting to normal caravan. Pawns={string.Join(", ", pawns.InnerListForReading.Select(pawn => pawn.Label))}");
      List<Pawn> pawnsToTransfer = pawns.InnerListForReading.ToList();
      if (!pawnsToTransfer.NullOrEmpty())
      {
        // base pawn list must be cleared before this Caravan object is ultimately destroyed,
        // otherwise their references will persist here and they will still get destroyed.
        RemoveAllPawns();
        _ = CaravanMaker.MakeCaravan(pawnsToTransfer, Faction, Tile, true);
      }
      if (!Destroyed)
      {
        Destroy();
      }
    }
  }

  public override void Destroy()
  {
    // Make sure we're not destroying based on a stale cache
    RecacheVehicles();

    foreach (VehiclePawn vehicle in vehicles)
    {
      for (int i = vehicle.AllPawnsAboard.Count; --i >= 0;)
      {
        Pawn pawn = vehicle.AllPawnsAboard[i];
        vehicle.RemovePawn(pawn);
        Assert.IsFalse(pawn.IsWorldPawn());
        Find.WorldPawns.PassToWorld(pawn);
      }
      vehicle.Destroy();
    }
    base.Destroy();
  }

  public override string GetInspectString()
  {
    StringBuilder stringBuilder = new();

    int colonists = 0;
    int animals = 0;
    int prisoners = 0;
    int downed = 0;
    int mentalState = 0;
    int vehicleCount = 0;

    vehicleCount++;
    foreach (Pawn pawn in PawnsListForReading)
    {
      if (pawn is VehiclePawn) vehicleCount++;
      if (pawn.IsColonist) colonists++;
      if (pawn.RaceProps.Animal) animals++;
      if (pawn.IsPrisoner) prisoners++;
      if (pawn.Downed) downed++;
      if (pawn.InMentalState) mentalState++;
    }

    if (vehicleCount >= 1)
    {
      VehicleCounts.Clear();
      {
        foreach (VehiclePawn vehicle in VehiclesListForReading)
        {
          if (!VehicleCounts.TryAdd(vehicle.VehicleDef, 1))
            VehicleCounts[vehicle.VehicleDef]++;
        }
        foreach ((VehicleDef vehicleDef, int count) in VehicleCounts)
        {
          stringBuilder.Append($"{count} {vehicleDef.LabelCap}, ");
        }
      }
      VehicleCounts.Clear();
    }
    stringBuilder.Append("CaravanColonistsCount".Translate(colonists,
      (colonists != 1) ? Faction.OfPlayer.def.pawnsPlural : Faction.OfPlayer.def.pawnSingular));
    if (animals == 1)
    {
      stringBuilder.Append(", " + "CaravanAnimal".Translate());
    }
    else if (animals > 1)
    {
      stringBuilder.Append(", " + "CaravanAnimalsCount".Translate(animals));
    }
    if (prisoners == 1)
    {
      stringBuilder.Append(", " + "CaravanPrisoner".Translate());
    }
    else if (prisoners > 1)
    {
      stringBuilder.Append(", " + "CaravanPrisonersCount".Translate(prisoners));
    }
    stringBuilder.AppendLine();
    if (mentalState > 0)
    {
      stringBuilder.Append("CaravanPawnsInMentalState".Translate(mentalState));
    }
    if (downed > 0)
    {
      if (mentalState > 0)
      {
        stringBuilder.Append(", ");
      }
      stringBuilder.Append("CaravanPawnsDowned".Translate(downed));
    }
    VehiclePawn vehicleIncapacitated = null;
    string vehicleIncapReason = null;
    foreach (VehiclePawn vehicle in VehiclesListForReading)
    {
      // We only care about the first, any incap. vehicle blocks the whole caravan from moving
      // and checking roles + stats can be expensive if uncached.
      if (vehicleIncapacitated == null)
      {
        if (!vehicle.CanMove)
        {
          vehicleIncapacitated = vehicle;
          vehicleIncapReason = "VF_VehicleUnableToMove".Translate(vehicle);
        }
        else if (!vehicle.CanMoveWithOperators)
        {
          vehicleIncapacitated = vehicle;
          vehicleIncapReason = "VF_NotEnoughToOperate".Translate();
        }
      }

      foreach (ThingComp comp in vehicle.AllComps)
      {
        if (comp is VehicleComp vehicleComp)
        {
          vehicleComp.CompCaravanInspectString(stringBuilder);
        }
      }
    }
    if (mentalState > 0 || downed > 0)
    {
      stringBuilder.AppendLine();
    }

    if (vehiclePather.Moving)
    {
      if (vehiclePather.ArrivalAction != null)
      {
        stringBuilder.Append(vehiclePather.ArrivalAction.ReportString);
      }
      else if (this.HasBoat())
      {
        stringBuilder.Append("VF_Sailing".Translate());
      }
      else
      {
        stringBuilder.Append("CaravanTraveling".Translate());
      }
    }
    else
    {
      Settlement settlementBase = CaravanVisitUtility.SettlementVisitedNow(this);
      stringBuilder.Append(settlementBase is not null ?
        "CaravanVisiting".Translate(settlementBase.Label) :
        "CaravanWaiting".Translate());
    }
    if (vehiclePather.Moving)
    {
      float estimatedDaysToArrive =
        VehicleCaravanPathingHelper.EstimatedTicksToArrive(this, true) / 60000f;
      stringBuilder.AppendLine();
      stringBuilder.Append(
        "CaravanEstimatedTimeToDestination".Translate(estimatedDaysToArrive.ToString("0.#")));
    }
    if (vehicleIncapacitated != null)
    {
      stringBuilder.AppendLine();
      stringBuilder.Append(vehicleIncapReason);
    }
    else if (AllOwnersDowned)
    {
      stringBuilder.AppendLine();
      stringBuilder.Append("AllCaravanMembersDowned".Translate());
    }
    else if (AllOwnersHaveMentalBreak)
    {
      stringBuilder.AppendLine();
      stringBuilder.Append("AllCaravanMembersMentalBreak".Translate());
    }
    else if (ImmobilizedByMass)
    {
      stringBuilder.AppendLine();
      stringBuilder.Append("CaravanImmobilizedByMass".Translate());
    }
    if (needs.AnyPawnOutOfFood(out string text))
    {
      stringBuilder.AppendLine();
      stringBuilder.Append("CaravanOutOfFood".Translate());
      if (!text.NullOrEmpty())
      {
        stringBuilder.Append(" ");
        stringBuilder.Append(text);
        stringBuilder.Append(".");
      }
    }
    if (!vehiclePather.MovingNow)
    {
      int usedBedCount = beds.GetUsedBedCount();
      stringBuilder.AppendLine();
      stringBuilder.Append(
        CaravanBedUtility.AppendUsingBedsLabel("CaravanResting".Translate(), usedBedCount));
    }
    else
    {
      string inspectStringLine = carryTracker.GetInspectStringLine();
      if (!inspectStringLine.NullOrEmpty())
      {
        stringBuilder.AppendLine();
        stringBuilder.Append(inspectStringLine);
      }
      string inBedForMedicalReasonsInspectStringLine =
        beds.GetInBedForMedicalReasonsInspectStringLine();
      if (!inBedForMedicalReasonsInspectStringLine.NullOrEmpty())
      {
        stringBuilder.AppendLine();
        stringBuilder.Append(inBedForMedicalReasonsInspectStringLine);
      }
    }
    return stringBuilder.ToString();
  }

  public override void DrawExtraSelectionOverlays()
  {
    if (IsPlayerControlled && vehiclePather.curPath != null)
    {
      vehiclePather.curPath.DrawPath(this);
    }
    gotoMote.RenderMote();
  }

  protected override void Tick()
  {
    base.Tick();
    vehiclePather.PatherTick();

    if (vehiclePather.MovingNow)
    {
      foreach (VehiclePawn vehicle in vehicles)
      {
        vehicle.CompFueledTravel?.ConsumeFuelWorld();
      }
    }
    else
    {
      int gameTicks = Find.TickManager.TicksGame;
      if (gameTicks % RepairMothballTicks == 0)
      {
        RecacheStatAverages();
      }
      if (VehiclesNeedRepairs && Repairing && gameTicks % RepairMothballTicks == 0)
      {
        RepairAllVehicles();
      }
    }
  }

  public void RepairAllVehicles()
  {
    LearnSkill(SkillDefOf.Construction, RepairMothballTicks);
    foreach (VehiclePawn vehicle in vehicles)
    {
      VehicleComponent component =
        vehicle.statHandler.ComponentsPrioritized.FirstOrDefault(c => c.HealthPercent < 1);
      if (component != null)
      {
        component.HealComponent(vehicle.GetStatValue(VehicleStatDefOf.RepairRate) *
          RepairMothballTicks / JobDriver_RepairVehicle.TicksForRepair);
        vehicle.CrashLanded = false;
        return; //Only repair 1 vehicle at a time
      }
    }
  }

  private void LearnSkill(SkillDef skillDef, int mothballTicks)
  {
    foreach (Pawn pawn in PawnsListForReading)
    {
      if (pawn.IsColonistPlayerControlled || pawn.IsColonyMechPlayerControlled)
      {
        pawn.skills?.Learn(skillDef, 0.08f * mothballTicks);
        pawn.records.Increment(RecordDefOf.ThingsRepaired);
      }
    }
  }

  public override void PostRemove()
  {
    base.PostRemove();
    vehiclePather.StopDead();
  }

  public override void SpawnSetup()
  {
    base.SpawnSetup();
    RecacheVehicles();
    vehicleTweener.ResetTweenedPosToRoot();

    // Necessary check for post load, otherwise registry will be null until spawned on map
    foreach (VehiclePawn vehicle in vehicles)
    {
      vehicle.RegisterEvents();
    }
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Deep.Look(ref vehiclePather, nameof(vehiclePather), this);
    Scribe_Values.Look(ref repairing, nameof(repairing));

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
      initialized = true;
    }
  }
}