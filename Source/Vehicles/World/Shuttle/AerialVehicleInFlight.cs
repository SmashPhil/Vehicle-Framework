using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Targeting;
using UnityEngine;
using UnityEngine.Assertions;
using Vehicles.Rendering;
using Verse;

namespace Vehicles.World;

[PublicAPI]
[StaticConstructorOnStartup]
public class AerialVehicleInFlight : DynamicDrawnWorldObject, IVehicleWorldObject,
                                     IThingHolderTickable, IThingHolderEvents<VehiclePawn>,
                                     ILauncher, ITargeterSource<GlobalTargetInfo, ArrivalOption>
{
	private static readonly Texture2D ViewQuestCommandTex =
		ContentFinder<Texture2D>.Get("UI/Commands/ViewQuest");

	public const float ReconFlightSpeed = 5;
	public const float TransitionTakeoff = 0.025f;
	public const float PctPerTick = 0.001f;
	public const int TicksPerValidateFlightPath = 60;

	private VehiclePawn vehicle;
	public ThingOwner<VehiclePawn> innerContainer;

	public FlightPath flightPath;

	private Gizmo_RefuelableFuelTravel fuelGizmo;

	internal float transition;
	public float elevation;
	public bool recon;
	private float speedPctPerTick;

	public Vector3 position;

	private Material material;

	[Obsolete("This constructor is requierd for Xml Deserialization, use AerialVehicleInFlight::Create instead")]
	public AerialVehicleInFlight()
	{
		innerContainer = new ThingOwner<VehiclePawn>(this, false, LookMode.Reference);
	}

	public override string Label => vehicle?.Label;

	public VehiclePawn Vehicle => vehicle;

	// Vehicle is stored and ticked from world pawns
	bool IThingHolderTickable.ShouldTickContents => false;

	public virtual bool IsPlayerControlled => vehicle.Faction == Faction.OfPlayer;

	public float Elevation => 0; // vehicle.CompVehicleLauncher.inFlight ? elevation : 0;

	public float ElevationChange { get; protected set; }

	protected virtual Rot8 FullRotation => Rot8.North;

	protected virtual float RotatorSpeeds => 59;

	/// <summary>
	/// Vehicle is in-flight towards destination. This includes skyfaller animations 
	/// where the vehicle has not yet been spawned, but is no longer on the world map.
	/// </summary>
	public bool Flying => vehicle.CompVehicleLauncher.inFlight;

	public bool CanDismount => false;

	Vector3 ILauncher.Origin => DrawPos;

	bool ITargeterSource<GlobalTargetInfo, ArrivalOption>.TargeterValid =>
		!Destroyed && Vehicle is { Spawned: false, Destroyed: false };

	public override Vector3 DrawPos
	{
		get
		{
			if (flightPath.Path.NullOrEmpty())
				return WorldHelper.GetTilePos(Tile);
			Vector3 nodePos = flightPath.First.GetCenter(this);
			if (position == nodePos)
				return position;
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

	public override Material Material
	{
		get
		{
			if (!material)
			{
				string texPath = VehicleTex.CachedTextureIconPaths.TryGetValue(
					vehicle.VehicleDef, VehicleTex.DefaultVehicleIconTexPath);
				material = MaterialPool.MatFrom(texPath, ShaderDatabase.WorldOverlayTransparentLit, Faction.Color,
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
				fuelGizmo ??= new Gizmo_RefuelableFuelTravel(vehicle.CompFueledTravel, false);
				yield return fuelGizmo;
				if (DebugSettings.ShowDevGizmos)
				{
					foreach (Gizmo devModeGizmo in vehicle.CompFueledTravel.DevModeGizmos())
						yield return devModeGizmo;
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
					action = StartTargeting
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
					action = delegate { InitiateCrashEvent(); }
				};
			}
		}
	}

	private void StartTargeting()
	{
		CameraJumper.TryJump(CameraJumper.GetWorldTarget(this));
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

  protected override void Tick()
	{
		base.Tick();
		if (vehicle.CompVehicleLauncher.inFlight)
		{
			SpendFuel();

			if (vehicle.CompFueledTravel?.Fuel <= 0)
			{
				InitiateCrashEvent(null, "VF_IncidentCrashedSiteReason_OutOfFuel".Translate());
			}

			// Self destructive, should always tick last
			MoveForward();
		}
	}

	// TODO - Decouple from CompFueledTravel and let the comp handle this condition
	public virtual void SpendFuel()
	{
		if (vehicle.CompFueledTravel != null &&
			(vehicle.CompFueledTravel.FuelCondition & FuelConsumptionCondition.Flying) != 0)
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

	public void InitiateCrashEvent(WorldObject culprit = null, params string[] reasons)
	{
		vehicle.CompVehicleLauncher.inFlight = false;
		Tile = WorldHelper.GetNearestTile(DrawPos);
		ResetPosition(DrawPos);
		flightPath.ResetPath();
		AirDefensePositionTracker.DeregisterAerialVehicle(this);
		IncidentWorker_ShuttleDowned.Execute(this, reasons, culprit: culprit);
	}

	public virtual void MoveForward()
	{
		if (flightPath.Empty)
		{
			Log.Error($"{this} in flight with empty FlightPath.  Grounding to current Tile.");
			if (!Destroyed)
			{
				ArriveAtTile(Tile);
				SwitchToCaravan();
			}
			return;
		}
		transition += speedPctPerTick;
		if (transition >= 1)
		{
			if (vehicle.Faction.IsPlayer && flightPath.Count == 1)
			{
				Messages.Message("VF_AerialVehicleArrived".Translate(vehicle.LabelShort),
					MessageTypeDefOf.NeutralEvent);
			}
			ArriveAtTile(flightPath.First.Tile);
			flightPath.ConsumeNode(!recon);
			if (!Destroyed)
				InitializeNextFlight(DrawPos);
		}
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
			ClearAndDestroy();

		if (autoSelect)
			Find.WorldSelector.Select(vehicleCaravan, playSound: false);
	}

	private void InitializeNextFlight(Vector3 origin)
	{
		vehicle.CompVehicleLauncher.inFlight = true;
		ResetPosition(origin);
		SetSpeed();
	}

	private void SetSpeed()
	{
		Vector3 center = flightPath.First.GetCenter(this);
		if (position == center)
		{
			speedPctPerTick = 1;
			return;
		}
    // Clamp tile distance to PctPerTick
    float tileDistance = Mathf.Clamp(Ext_Math.SphericalDistance(position, center), 0.00001f, float.MaxValue); 
		float flightSpeed = recon ? ReconFlightSpeed : vehicle.CompVehicleLauncher.FlightSpeed;
		speedPctPerTick = (PctPerTick / tileDistance) * flightSpeed.Clamp(0, 99999);
	}

	TargetValidation ITargeterSource<GlobalTargetInfo, ArrivalOption>.CanTarget(GlobalTargetInfo target)
	{
		if (vehicle == null || Destroyed)
			return TargetValidation.Failed;

		return vehicle.CompVehicleLauncher.CanTarget(target);
	}

	TargeterResult ITargeterSource<GlobalTargetInfo, ArrivalOption>.Select(GlobalTargetInfo target)
	{
		return vehicle.CompVehicleLauncher.Select(target);
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

	public void Launch(TargetData<GlobalTargetInfo> targetData, IArrivalAction arrivalAction)
	{
		List<FlightNode> nodes = targetData.targets.Select(target => new FlightNode(target)).ToList();
		OrderFlyToTiles(nodes, arrivalAction);
	}

	IEnumerable<ArrivalOption> ILauncher.OptionsAt(GlobalTargetInfo target)
	{
		return vehicle.CompVehicleLauncher.OptionsAt(target);
	}

	public void ArriveAtTile(PlanetTile tile)
	{
		Tile = tile;
		ResetPosition(DrawPos);
		vehicle.CompVehicleLauncher.inFlight = false;
		AirDefensePositionTracker.DeregisterAerialVehicle(this);
	}

	private void ResumePathPostLoad()
	{
		// NewPath clears current flight path, new up list so it doesn't clear before reassigning.
		// TODO - this is ugly, but it works currently so revisit later
		OrderFlyToTiles(flightPath.Path.ToList(), flightPath.ArrivalAction);
	}

	public void OrderFlyToTiles(List<FlightNode> flightPath, [NotNull] IArrivalAction arrivalAction)
	{
		Assert.IsFalse(flightPath.NullOrEmpty());
		if (flightPath.Any(node => !node.Tile.Valid))
			throw new ArgumentException("Invalid tiles in flight path.", nameof(flightPath));
		Vector3 origin = DrawPos; // Capture position before registered Tile changes through flight path change
		this.flightPath.NewPath(flightPath, arrivalAction);
		InitializeNextFlight(origin);
		//List<AirDefense> flyoverDefenses =
		//  AirDefensePositionTracker.GetNearbyObjects(this, speedPctPerTick);
		//AirDefensePositionTracker.RegisterAerialVehicle(this, flyoverDefenses);
		vehicle.EventRegistry[VehicleEventDefOf.AerialVehicleOrdered].ExecuteEvents();
	}

	public override void DrawExtraSelectionOverlays()
	{
		base.DrawExtraSelectionOverlays();
		DrawFlightPath();
	}

	private void DrawFlightPath()
	{
		if (flightPath.Path.Count > 1)
		{
			Vector3 nodePosition = DrawPos;
			for (int i = 0; i < flightPath.Path.Count; i++)
			{
				Vector3 nextNodePosition = flightPath[i].GetCenter(this);
				FlightPath.DrawPath(nodePosition, nextNodePosition, TexData.WorldLineMatWhite);
				nodePosition = nextNodePosition;
			}
			FlightPath.DrawPath(nodePosition, flightPath.Last.GetCenter(this), TexData.WorldLineMatWhite);
		}
		else if (flightPath.Path.Count == 1)
		{
			FlightPath.DrawPath(DrawPos, flightPath.First.GetCenter(this), TexData.WorldLineMatWhite);
		}
	}

	public void SetCircle(PlanetTile tile)
	{
		flightPath.PushCircleAt(tile);
	}

	public void GenerateMapForRecon(PlanetTile tile)
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

	/// <summary>
	/// Clear contents of aerial vehicle before destroying the world object.
	/// </summary>
	/// <remarks>Keeps vehicle(s) alive post-destruction.</remarks>
	public void ClearAndDestroy()
	{
		vehicle = null;
		innerContainer.Clear();
		Destroy();
	}

	public override void Destroy()
	{
		base.Destroy();

		// This should only occur if we're full destroying an aerial vehicle w/ the vehicle
		// reference still attached.
		if (vehicle is { Destroyed: false })
		{
			if (innerContainer is { Any: false })
			{
				Trace.Fail($"Trying to destroy {vehicle} but it's not inside the aerial vehicle.");
				return;
			}
			vehicle.DestroyVehicleAndPawns();
			innerContainer.Clear();
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_References.Look(ref vehicle, nameof(vehicle), true);

		Scribe_Deep.Look(ref flightPath, nameof(flightPath), this);
		Scribe_Values.Look(ref transition, nameof(transition));
		Scribe_Values.Look(ref position, nameof(position));

		//Scribe_Values.Look(ref elevation, "elevation");
		Scribe_Values.Look(ref recon, nameof(recon));

		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			// No need to save container, vehicle is already saved. HoldingOwner is necessary for Vehicle's ParentHolder to
			// point to the aerial vehicle for WorldPawnGC and misc. world map handling.
			innerContainer.TryAdd(vehicle, canMergeWithExistingStacks: false);

			if (flightPath != null && !flightPath.Path.NullOrEmpty())
				ResumePathPostLoad();
		}
	}

	public override void SpawnSetup()
	{
		base.SpawnSetup();

		vehicle.RegisterEvents();
	}

	void IThingHolder.GetChildHolders(List<IThingHolder> outChildren)
	{
	}

	ThingOwner IThingHolder.GetDirectlyHeldThings()
	{
		return innerContainer;
	}

	public static AerialVehicleInFlight Create(VehiclePawn vehicle, PlanetTile tile)
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
		if (!vehicle.IsWorldPawn())
			Find.WorldPawns.PassToWorld(vehicle);
		foreach (Pawn pawn in vehicle.AllPawnsAboard)
		{
			if (pawn.IsWorldPawn())
				Find.WorldPawns.RemovePawn(pawn);
		}
		return aerialVehicle;
	}

	void IThingHolderEvents<VehiclePawn>.Notify_ItemAdded(VehiclePawn vehicle)
	{
		// TODO VF-215 - Add support for multi-vehicle aerial vehicle squadrons
	}

	void IThingHolderEvents<VehiclePawn>.Notify_ItemRemoved(VehiclePawn vehicle)
	{
		// TODO VF-215 - Add support for multi-vehicle aerial vehicle squadrons
	}
}