using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Vehicles.Rendering;
using Verse;

namespace Vehicles.World;

[PublicAPI]
[StaticConstructorOnStartup]
public class AerialVehicleInFlight : DynamicDrawnWorldObject, IVehicleWorldObject
{
  private static readonly Texture2D ViewQuestCommandTex =
    ContentFinder<Texture2D>.Get("UI/Commands/ViewQuest");

  public const float ReconFlightSpeed = 5;
  public const float TransitionTakeoff = 0.025f;
  public const float PctPerTick = 0.001f;
  public const int TicksPerValidateFlightPath = 60;

  protected static readonly SimpleCurve climbRateCurve =
  [
    new CurvePoint(0, 0.65f),
    new CurvePoint(0.05f, 1),
    new CurvePoint(0.95f, 1),
    new CurvePoint(1, 0.15f),
  ];

  public VehiclePawn vehicle;
  public ThingOwner<VehiclePawn> innerContainer;

  public AerialVehicleArrivalAction arrivalAction;

  protected internal FlightPath flightPath;

  internal float transition;
  public float elevation;
  public bool recon;
  private float speedPctPerTick;

  public Vector3 position;

  private Material vehicleMat;
  private Material vehicleMatNonLit;
  private Material material;

  public AerialVehicleInFlight()
  {
    innerContainer = new ThingOwner<VehiclePawn>(this, false, LookMode.Reference);
  }

  public override string Label => vehicle.Label;

  public virtual bool IsPlayerControlled => vehicle.Faction == Faction.OfPlayer;

  public float Elevation => 0; // vehicle.CompVehicleLauncher.inFlight ? elevation : 0;

  public float ElevationChange { get; protected set; }

  public float Rate => vehicle.CompVehicleLauncher.ClimbRateStat *
    climbRateCurve.Evaluate(Elevation / vehicle.CompVehicleLauncher.MaxAltitude);

  public int TicksTillLandingElevation =>
    Mathf.RoundToInt((Elevation - vehicle.CompVehicleLauncher.LandingAltitude / 2f) / Rate);

  protected virtual Rot8 FullRotation => Rot8.North;

  protected virtual float RotatorSpeeds => 59;

  /// <summary>
  /// Vehicle is in-flight towards destination. This includes skyfaller animations 
  /// where the vehicle has not yet been spawned, but is no longer on the world map.
  /// </summary>
  public bool Flying => vehicle.CompVehicleLauncher.inFlight;

  public bool CanDismount => false;

  public override Vector3 DrawPos
  {
    get
    {
      Vector3 nodePos = flightPath.First.GetCenter(this);
      if (position == nodePos)
      {
        return position;
      }
      return Vector3.Slerp(position, nodePos, transition);
    }
  }

  // For WITab readouts related to vehicles
  public IEnumerable<VehiclePawn> Vehicles
  {
    get { yield return vehicle; }
  }

  // All pawns will be in the AerialVehicle at all times.
  public IEnumerable<Pawn> DismountedPawns
  {
    get { yield break; }
  }

  [Obsolete]
  public virtual Material VehicleMat
  {
    get
    {
      if (vehicle is null)
      {
        return Material;
      }
      vehicleMat ??= new Material(vehicle.VehicleGraphic.MatAtFull(FullRotation))
      {
        shader = ShaderDatabase.WorldOverlayTransparentLit,
        renderQueue = WorldMaterials.WorldObjectRenderQueue
      };
      return vehicleMat;
    }
  }

  [Obsolete]
  public virtual Material VehicleMatNonLit
  {
    get
    {
      if (vehicle is null)
      {
        return Material;
      }
      vehicleMatNonLit ??= new Material(vehicle.VehicleGraphic.MatAtFull(FullRotation))
      {
        shader = ShaderDatabase.WorldOverlayTransparent,
        renderQueue = WorldMaterials.WorldObjectRenderQueue
      };
      return vehicleMatNonLit;
    }
  }

  public override Material Material
  {
    get
    {
      if (!material)
      {
        material = MaterialPool.MatFrom(VehicleTex.CachedTextureIconPaths.TryGetValue(
            vehicle.VehicleDef,
            VehicleTex.DefaultVehicleIconTexPath), ShaderDatabase.WorldOverlayTransparentLit,
          WorldMaterials.WorldObjectRenderQueue);
      }
      return material;
    }
  }

  public virtual void Initialize()
  {
    position = base.DrawPos;
  }

  public virtual Vector3 DrawPosAhead(int ticksAhead)
  {
    return Vector3.Slerp(position, flightPath.First.GetCenter(this),
      transition + speedPctPerTick * ticksAhead);
  }

  public override void Draw()
  {
    if (!this.HiddenBehindTerrainNow())
    {
      WorldHelper.DrawQuadTangentialToPlanet(DrawPos, 0.7f * Find.WorldGrid.AverageTileSize,
        0.015f, Material);
    }
  }

  public override IEnumerable<Gizmo> GetGizmos()
  {
    foreach (Gizmo gizmo in base.GetGizmos())
    {
      yield return gizmo;
    }

    if (ShowRelatedQuests)
    {
      List<Quest> quests = Find.QuestManager.QuestsListForReading;
      foreach (Quest quest in quests)
      {
        if (!quest.hidden && !quest.Historical && !quest.dismissed &&
          quest.QuestLookTargets.Contains(this))
        {
          yield return new Command_Action
          {
            defaultLabel = "CommandViewQuest".Translate(quest.name),
            defaultDesc = "CommandViewQuestDesc".Translate(),
            icon = ViewQuestCommandTex,
            action = delegate
            {
              Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Quests);
              ((MainTabWindow_Quests)MainButtonDefOf.Quests.TabWindow).Select(quest);
            }
          };
        }
      }
    }

    if (IsPlayerControlled)
    {
      if (vehicle.CompFueledTravel != null)
      {
        yield return new Gizmo_RefuelableFuelTravel(vehicle.CompFueledTravel, false);
        foreach (Gizmo fuelGizmo in vehicle.CompFueledTravel.DevModeGizmos())
        {
          yield return fuelGizmo;
        }
      }
      if (vehicle.CompVehicleLauncher.ControlInFlight)
      {
        Command_Action launchCommand = new()
        {
          defaultLabel = "CommandLaunchGroup".Translate(),
          defaultDesc = "CommandLaunchGroupDesc".Translate(),
          icon = TexData.LaunchCommandTex,
          alsoClickIfOtherInGroupClicked = false,
          action = delegate
          {
            LaunchTargeter.BeginTargeting(vehicle, ChoseTargetOnMap, this, true,
              TexData.TargeterMouseAttachment, false, null,
              (target, path, fuelCost) =>
                vehicle.CompVehicleLauncher.launchProtocol.TargetingLabelGetter(target, Tile,
                  path, fuelCost));
          }
        };
        if (!vehicle.CompVehicleLauncher.CanLaunchWithCargoCapacity(out string disableReason))
        {
          launchCommand.Disabled = true;
          launchCommand.disabledReason = disableReason;
        }
        yield return launchCommand;
      }
      if (DebugSettings.ShowDevGizmos)
      {
        yield return new Command_Action
        {
          defaultLabel = "Debug: Land at Nearest Player Settlement",
          action = delegate { Patch_Debug.DebugLandAerialVehicle(this); }
        };
        yield return new Command_Action
        {
          defaultLabel = "Debug: Initiate Crash Event",
          action = delegate { InitiateCrashEvent(null); }
        };
      }
    }
  }

  public virtual bool ChoseTargetOnMap(GlobalTargetInfo target, float fuelCost)
  {
    return vehicle.CompVehicleLauncher.launchProtocol.ChoseWorldTarget(target, DrawPos, Validator,
      NewDestination);

    bool Validator(GlobalTargetInfo globalTarget, Vector3 pos,
      Action<int, AerialVehicleArrivalAction, bool> launchAction)
    {
      if (!globalTarget.IsValid)
      {
        Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(),
          MessageTypeDefOf.RejectInput, false);
        return false;
      }

      if (Ext_Math.SphericalDistance(pos, WorldHelper.GetTilePos(globalTarget.Tile)) >
        vehicle.CompVehicleLauncher.MaxLaunchDistance || (vehicle.CompFueledTravel is not null &&
          fuelCost > vehicle.CompFueledTravel.Fuel))
      {
        Messages.Message("TransportPodDestinationBeyondMaximumRange".Translate(),
          MessageTypeDefOf.RejectInput, false);
        return false;
      }

      List<FloatMenuOption> source =
        vehicle.CompVehicleLauncher.launchProtocol.GetFloatMenuOptionsAt(globalTarget.Tile)
         .ToList();
      if (source.NullOrEmpty())
      {
        if (!WorldVehiclePathGrid.Instance.Passable(globalTarget.Tile, vehicle.VehicleDef))
        {
          Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(),
            MessageTypeDefOf.RejectInput, false);
          return false;
        }
        launchAction(globalTarget.Tile, null, false);
        return true;
      }

      if (source.Count != 1)
      {
        Find.WindowStack.Add(new FloatMenuTargeter(source.ToList()));
        return false;
      }
      if (!source[0].Disabled)
      {
        source[0].action();
        return true;
      }
      return false;
    }
  }

  public void NewDestination(int destinationTile, AerialVehicleArrivalAction arrivalAction,
    bool recon = false)
  {
    vehicle.CompVehicleLauncher.inFlight = true;
    this.recon = recon;
    OrderFlyToTiles(LaunchTargeter.FlightPath, DrawPos, arrivalAction: arrivalAction);
  }

  protected override void Tick()
  {
    base.Tick();
    if (vehicle.CompVehicleLauncher.inFlight)
    {
      MoveForward();
      SpendFuel();

      if (vehicle.CompFueledTravel?.Fuel <= 0)
      {
        InitiateCrashEvent(null, "VF_IncidentCrashedSiteReason_OutOfFuel".Translate());
      }
      //ChangeElevation();
    }
    //if (Find.TickManager.TicksGame % TicksPerValidateFlightPath == 0)
    //{
    //	flightPath.VerifyFlightPath();
    //}
  }

  protected void ChangeElevation()
  {
    int altSign = flightPath.AltitudeDirection;
    float elevationChange = vehicle.CompVehicleLauncher.ClimbRateStat * climbRateCurve.Evaluate(
      elevation /
      vehicle.CompVehicleLauncher.MaxAltitude);
    ElevationChange = elevationChange;
    if (elevationChange < 0)
    {
      altSign = 1;
    }
    elevation += elevationChange * altSign;
    elevation = elevation.Clamp(AltitudeMeter.MinimumAltitude, AltitudeMeter.MaximumAltitude);
    if (!vehicle.CompVehicleLauncher.AnyFlightControl)
    {
      InitiateCrashEvent(null, "VF_IncidentCrashedSiteReason_FlightControl".Translate());
    }
    else if (elevation <= AltitudeMeter.MinimumAltitude &&
      !vehicle.CompVehicleLauncher.ControlledDescent)
    {
      InitiateCrashEvent(null, "VF_IncidentCrashedSiteReason_FlightControl".Translate());
    }
  }

  public virtual void SpendFuel()
  {
    if (vehicle.CompFueledTravel != null &&
      vehicle.CompFueledTravel.FuelCondition.HasFlag(FuelConsumptionCondition.Flying))
    {
      float amount = vehicle.CompFueledTravel.ConsumptionRatePerTick *
        vehicle.CompVehicleLauncher.FuelConsumptionWorldMultiplier;
      vehicle.CompFueledTravel.ConsumeFuel(amount);
    }
  }

  public virtual void TakeDamage(DamageInfo damageInfo, IntVec2 cell)
  {
    vehicle.TakeDamage(damageInfo, cell);
  }

  public void InitiateCrashEvent(WorldObject culprit, params string[] reasons)
  {
    vehicle.CompVehicleLauncher.inFlight = false;
    Tile = WorldHelper.GetNearestTile(DrawPos);
    ResetPosition(WorldHelper.GetTilePos(Tile));
    flightPath.ResetPath();
    AirDefensePositionTracker.DeregisterAerialVehicle(this);
    IncidentWorker_ShuttleDowned.Execute(this, reasons, culprit: culprit);
  }

  public virtual void MoveForward()
  {
    if (flightPath.Empty)
    {
      Log.Error($"{this} in flight with empty FlightPath.  Grounding to current Tile.");
      ResetPosition(Find.WorldGrid.GetTileCenter(Tile));
      vehicle.CompVehicleLauncher.inFlight = false;
      AirDefensePositionTracker.DeregisterAerialVehicle(this);
    }
    else
    {
      transition += speedPctPerTick;
      if (transition >= 1)
      {
        if (flightPath.Path.Count > 1)
        {
          Vector3 newPos = DrawPos;
          int ticksLeft = Mathf.RoundToInt(1 / speedPctPerTick);
          flightPath.NodeReached(ticksLeft > TicksTillLandingElevation && !recon);
          if (Spawned)
          {
            InitializeNextFlight(newPos);
          }
        }
        else
        {
          if (vehicle.Faction.IsPlayer)
          {
            Messages.Message("VF_AerialVehicleArrived".Translate(vehicle.LabelShort),
              MessageTypeDefOf.NeutralEvent);
          }
          LandAtTile(flightPath.First.tile);

          //if (Elevation <= vehicle.CompVehicleLauncher.LandingAltitude)
          //{

          //}
          //else if (flightPath.Path.Count <= 1 && vehicle.CompVehicleLauncher.Props.circleToLand)
          //{
          //	Vector3 newPos = DrawPos;
          //	SetCircle(flightPath.First.tile);
          //	InitializeNextFlight(newPos);
          //}
        }
      }
    }
  }

  public void LandAtTile(int tile)
  {
    Tile = tile;
    ResetPosition(Find.WorldGrid.GetTileCenter(Tile));
    arrivalAction?.Arrived(this, tile);
    vehicle.CompVehicleLauncher.inFlight = false;
    AirDefensePositionTracker.DeregisterAerialVehicle(this);
  }

  public void OrderFlyToTiles(List<FlightNode> flightPath, Vector3 origin,
    AerialVehicleArrivalAction arrivalAction = null)
  {
    if (flightPath.NullOrEmpty() || flightPath.Any(node => node.tile < 0))
    {
      return;
    }
    if (arrivalAction != null)
    {
      this.arrivalAction = arrivalAction;
    }
    this.flightPath.NewPath(flightPath);
    InitializeNextFlight(origin);
    List<AirDefense> flyoverDefenses =
      AirDefensePositionTracker.GetNearbyObjects(this, speedPctPerTick);
    AirDefensePositionTracker.RegisterAerialVehicle(this, flyoverDefenses);
    vehicle.EventRegistry[VehicleEventDefOf.AerialVehicleOrdered].ExecuteEvents();
  }

  private void ResetPosition(Vector3 position)
  {
    this.position = position;
    transition = 0;
  }

  public void SwitchToCaravan()
  {
    bool autoSelect = Find.WorldSelector.SelectedObjects.Contains(this);
    innerContainer.Remove(vehicle);
    VehicleCaravan vehicleCaravan =
      CaravanHelper.MakeVehicleCaravan([vehicle], vehicle.Faction, Tile, true);
    if (!Destroyed)
    {
      Destroy();
    }

    if (autoSelect)
    {
      Find.WorldSelector.Select(vehicleCaravan, playSound: false);
    }
  }

  private void InitializeNextFlight(Vector3 position)
  {
    vehicle.CompVehicleLauncher.inFlight = true;
    ResetPosition(position);
    SetSpeed();
  }

  private void SetSpeed()
  {
    Vector3 center = flightPath.First.GetCenter(this);
    if (position == center) //If position is still at origin, set speed to instantaneous
    {
      speedPctPerTick = 1;
      return;
    }
    float tileDistance = Mathf.Clamp(Ext_Math.SphericalDistance(position, center), 0.00001f,
      float.MaxValue); //Clamp tile distance to PctPerTick
    float flightSpeed = recon ? ReconFlightSpeed : vehicle.CompVehicleLauncher.FlightSpeed;
    speedPctPerTick = (PctPerTick / tileDistance) * flightSpeed.Clamp(0, 99999);
  }

  public override void DrawExtraSelectionOverlays()
  {
    base.DrawExtraSelectionOverlays();
    DrawFlightPath();
  }

  public void DrawFlightPath()
  {
    if (!LaunchTargeter.Instance.IsTargeting)
    {
      if (flightPath.Path.Count > 1)
      {
        Vector3 nodePosition = DrawPos;
        for (int i = 0; i < flightPath.Path.Count; i++)
        {
          Vector3 nextNodePosition = flightPath[i].GetCenter(this);
          LaunchTargeter.DrawTravelPoint(nodePosition, nextNodePosition);
          nodePosition = nextNodePosition;
        }
        LaunchTargeter.DrawTravelPoint(nodePosition, flightPath.Last.GetCenter(this));
      }
      else if (flightPath.Path.Count == 1)
      {
        LaunchTargeter.DrawTravelPoint(DrawPos, flightPath.First.GetCenter(this));
      }
    }
  }

  public void SetCircle(int tile)
  {
    flightPath.PushCircleAt(tile);
  }

  public void GenerateMapForRecon(int tile)
  {
    if (flightPath.InRecon && Find.WorldObjects.MapParentAt(tile) is { HasMap: false } mapParent)
    {
      LongEventHandler.QueueLongEvent(delegate
      {
        Map map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, null);
        TaggedString label = "LetterLabelCaravanEnteredEnemyBase".Translate();
        TaggedString text = "LetterTransportPodsLandedInEnemyBase".Translate(mapParent.Label)
         .CapitalizeFirst();
        if (mapParent is Settlement settlement)
        {
          SettlementUtility.AffectRelationsOnAttacked(settlement, ref text);
        }
        if (!mapParent.HasMap)
        {
          Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
          PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(map.mapPawns.AllPawns, ref label,
            ref text,
            "LetterRelatedPawnsInMapWherePlayerLanded".Translate(Faction.OfPlayer.def
             .pawnsPlural), true);
        }
        Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NeutralEvent, vehicle,
          mapParent.Faction);
        Current.Game.CurrentMap = map;
        CameraJumper.TryHideWorld();
      }, "GeneratingMap", false, null);
    }
  }

  public override void PostMake()
  {
    base.PostMake();
    flightPath = new FlightPath(this);
  }

  public override void Destroy()
  {
    base.Destroy();

    // This should only occur if we're full destroying an aerial vehicle w/ the vehicle
    // reference still attached.
    if (vehicle is { Destroyed: false })
    {
      Assert.IsTrue(innerContainer is { Any: true });
      vehicle.DestroyVehicleAndPawns();
      innerContainer.Clear();
    }
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_References.Look(ref vehicle, nameof(vehicle), true);

    Scribe_Deep.Look(ref flightPath, nameof(flightPath), this);
    Scribe_Deep.Look(ref arrivalAction, nameof(arrivalAction));
    Scribe_Values.Look(ref transition, nameof(transition));
    Scribe_Values.Look(ref position, nameof(position));

    //Scribe_Values.Look(ref elevation, "elevation");
    Scribe_Values.Look(ref recon, nameof(recon));

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
      // No need to save container, vehicle is already saved. HoldingOwner is necessary for Vehicle's ParentHolder to
      // point to the aerial vehicle for WorldPawnGC and misc. world map handling.
      innerContainer.TryAdd(vehicle, canMergeWithExistingStacks: false);
    }
  }

  public override void SpawnSetup()
  {
    base.SpawnSetup();

    vehicle.RegisterEvents();

    if (flightPath != null && !flightPath.Path.NullOrEmpty())
    {
      //Needs new list instance to avoid clearing before reset.
      //This is only necessary for resetting with saved flight path due to flight being uninitialized from load.
      OrderFlyToTiles(flightPath.Path.ToList(), DrawPos, arrivalAction: arrivalAction);
    }
  }

  void IThingHolder.GetChildHolders(List<IThingHolder> outChildren)
  {
    outChildren.AddRange(vehicle.handlers);
  }

  ThingOwner IThingHolder.GetDirectlyHeldThings()
  {
    return vehicle.inventory.innerContainer;
  }

  public static AerialVehicleInFlight Create(VehiclePawn vehicle, int tile)
  {
    AerialVehicleInFlight aerialVehicle =
      (AerialVehicleInFlight)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfVehicles
       .AerialVehicle);
    aerialVehicle.vehicle = vehicle;
    aerialVehicle.Tile = tile;
    aerialVehicle.SetFaction(vehicle.Faction);
    aerialVehicle.Initialize();
    aerialVehicle.innerContainer.TryAddOrTransfer(vehicle, canMergeWithExistingStacks: false);
    Find.WorldObjects.Add(aerialVehicle);
    return aerialVehicle;
  }
}