using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using RimWorld;
using SmashTools;
using SmashTools.Animations;
using SmashTools.Performance;
using SmashTools.Rendering;
using UnityEngine;
using Vehicles.Rendering;
using Verse;

namespace Vehicles;

public partial class VehiclePawn : Pawn, IInspectable, IThingHolderTickable,
                                   IAnimationTarget, IAnimator, ITransformable,
                                   IEventManager<VehicleEventDef>, IMaterialCacheTarget
{
	private static readonly AccessTools.FieldRef<Pawn_DraftController, bool> DraftedIntFieldRef =
		AccessTools.FieldRefAccess<Pawn_DraftController, bool>(
			AccessTools.Field(typeof(Pawn_DraftController), "draftedInt"));

	public EventManager<VehicleEventDef> EventRegistry { get; set; }

	// NOTE - vanilla drafter will be null when despawned, for vehicle logic it should prefer  to check directly
	// with the ignition controller. Masking base property to reduce accidental references to it.
	public new bool Drafted => ignition.Drafted;

	public VehicleDef VehicleDef => def as VehicleDef;

	public int AverageSkillOfCapablePawns(SkillDef skill)
	{
		if (AllCapablePawns.Count == 0)
			return 0;

		int value = 0;
		foreach (Pawn pawn in AllCapablePawns)
		{
			if (pawn.skills?.GetSkill(skill) is { } record)
			{
				value += record.Level;
			}
		}
		value /= AllCapablePawns.Count;
		return value;
	}

	private void InitializeVehicle()
	{
		if (handlers is { Count: > 0 })
			return;

		cargoToLoad ??= [];
		boardingAssignments ??= [];

		if (!VehicleDef.properties.roles.NullOrEmpty())
		{
			foreach (VehicleRole role in VehicleDef.properties.roles)
			{
				handlers.Add(new VehicleRoleHandler(this, role));
			}
		}
		handlers.Sort();

		RecacheComponents();
		RecacheMovementPermissions();
	}

	public override void PostMapInit()
	{
		vehiclePather.TryResumePathingAfterLoading();
	}

	public virtual void PostGenerationSetup()
	{
		ignition = new VehicleIgnitionController(this);
		ageTracker.AgeBiologicalTicks = 0;
		ageTracker.AgeChronologicalTicks = 0;
		ageTracker.BirthAbsTicks = 0;
		statHandler.InitializeComponents();
		this.RegisterEvents();
		InitializeVehicle();
		RegenerateUnsavedComponents();
		UnityThread.ExecuteOnMainThread(DrawTracker.overlayRenderer.Init);

		if (Faction != Faction.OfPlayer && VehicleDef.npcProperties != null)
		{
			GenerateInventory();
		}
	}

	private void GenerateInventory()
	{
		if (VehicleDef.npcProperties?.raidParams?.inventory != null)
		{
			foreach (PawnInventoryOption inventoryOption in VehicleDef.npcProperties.raidParams
			 .inventory)
			{
				foreach (Thing thing in inventoryOption.GenerateThings())
				{
					inventory.innerContainer.TryAdd(thing);
				}
			}
		}
	}

	private void RecacheAlerts()
	{
		ticksSinceBoarded = 0;
	}

	private void UpdateDraftController()
	{
		// drafter might be null if vehicle was despawned. Ludeon sets 'dynamic' components to null when pawns are
		// despawned or passed to world. Why? Who knows... cause it certainly wasn't for performance reasons.
		if (drafter != null && ignition != null)
		{
			DraftedIntFieldRef.Invoke(drafter) = ignition.Drafted;
		}
	}

	[Conditional("ANIMATOR")]
	private void UpdateDraftAnimationProperty()
	{
		animator?.SetBool(PropertyIds.IgnitionOn, ignition.Drafted);
	}

	public void RegisterEvents()
	{
		if (EventRegistry != null && EventRegistry.Initialized())
			return; // Disallow re-registering events

		this.FillEventsDef();

		inventory.innerContainer.OnContentsChanged += RecacheInventoryPawns;

		this.AddEvent(VehicleEventDefOf.CargoAdded, statHandler.MarkAllDirty);
		this.AddEvent(VehicleEventDefOf.CargoRemoved, statHandler.MarkAllDirty);
		this.AddEvent(VehicleEventDefOf.PawnEntered, RecachePawnCount, RecacheAlerts);
		this.AddEvent(VehicleEventDefOf.PawnExited, vehiclePather.RecalculatePermissions, RecachePawnCount);
		this.AddEvent(VehicleEventDefOf.PawnRemoved, vehiclePather.RecalculatePermissions, RecachePawnCount);
		this.AddEvent(VehicleEventDefOf.PawnChangedSeats, vehiclePather.RecalculatePermissions, RecachePawnCount);
		this.AddEvent(VehicleEventDefOf.PawnKilled, vehiclePather.RecalculatePermissions, RecachePawnCount);
		this.AddEvent(VehicleEventDefOf.PawnCapacitiesDirty, vehiclePather.RecalculatePermissions);
		this.AddEvent(VehicleEventDefOf.MoveStart, RecacheAlerts);
		this.AddEvent(VehicleEventDefOf.MoveStop, RecacheAlerts);
		this.AddEvent(VehicleEventDefOf.IgnitionOn, vehiclePather.RecalculatePermissions, RecacheAlerts,
			UpdateDraftController);
		this.AddEvent(VehicleEventDefOf.IgnitionOff, vehiclePather.RecalculatePermissions, UpdateDraftController);
		this.AddEvent(VehicleEventDefOf.HealthChanged, vehiclePather.RecalculatePermissions);
		this.AddEvent(VehicleEventDefOf.DamageTaken, statHandler.MarkAllDirty, Notify_TookDamage);
		this.AddEvent(VehicleEventDefOf.Repaired, statHandler.MarkAllDirty);
		this.AddEvent(VehicleEventDefOf.OutOfFuel, delegate
		{
			if (Spawned)
			{
				vehiclePather.PatherFailed();
				ignition.Drafted = false;
			}
		});
		this.AddEvent(VehicleEventDefOf.ScanRare, statHandler.MarkAllDirty);
		this.AddEvent(VehicleEventDefOf.UpgradeCompleted, ResetRenderStatus,
			RecacheMovementPermissions);
		this.AddEvent(VehicleEventDefOf.UpgradeRefundCompleted, ResetRenderStatus,
			RecacheMovementPermissions);
		if (!VehicleDef.events.NullOrEmpty())
		{
			foreach ((VehicleEventDef vehicleEventDef, List<DynamicDelegate<VehiclePawn>> methods) in
				VehicleDef.events)
			{
				if (!methods.NullOrEmpty())
				{
					foreach (DynamicDelegate<VehiclePawn> method in methods)
					{
						this.AddEvent(vehicleEventDef, () => method.Invoke(null, this));
					}
				}
			}
		}

		if (!VehicleDef.statEvents.NullOrEmpty())
		{
			foreach (StatCache.EventLister eventLister in VehicleDef.statEvents)
			{
				foreach (VehicleEventDef eventDef in eventLister.eventDefs)
				{
					this.AddEvent(eventDef,
						() => statHandler.MarkStatDirty(eventLister.statDef));
				}
			}
		}

		//One Shots
		if (!VehicleDef.soundOneShotsOnEvent.NullOrEmpty())
		{
			foreach (VehicleSoundEventEntry<VehicleEventDef> soundEventEntry in VehicleDef
			 .soundOneShotsOnEvent)
			{
				this.AddEvent(soundEventEntry.key, () => this.PlayOneShotOnVehicle(soundEventEntry),
					soundEventEntry.removalKey);
			}
		}

		//Sustainers
		if (!VehicleDef.soundSustainersOnEvent.NullOrEmpty())
		{
			foreach (VehicleSustainerEventEntry<VehicleEventDef> soundEventEntry in VehicleDef
			 .soundSustainersOnEvent)
			{
				this.AddEvent(soundEventEntry.start, () => this.StartSustainerOnVehicle(soundEventEntry),
					soundEventEntry.removalKey);
				this.AddEvent(soundEventEntry.stop, () => this.StopSustainerOnVehicle(soundEventEntry),
					soundEventEntry.removalKey);
			}
		}

		foreach (ThingComp comp in AllComps)
		{
			if (comp is VehicleComp vehicleComp)
			{
				vehicleComp.EventRegistration();
			}
		}
	}

	/// <summary>
	/// Executes after vehicle has been loaded into the game
	/// </summary>
	/// <remarks>
	/// Called regardless if vehicle is spawned or unspawned. Responsible for important
	/// variables being set that may be called even for unspawned vehicles.
	/// </remarks>
	protected virtual void PostLoad()
	{
		// Events must be registered before comp post loads, SpawnSetup won't trigger register in this case
		RegisterEvents();
		RegenerateUnsavedComponents();
		RecacheComponents();
		RecachePawnCount();
		RecacheMovementPermissions();
		statHandler.MarkAllDirty();
		animator?.PostLoad();
		UnityThread.ExecuteOnMainThread(DrawTracker.overlayRenderer.Init);

		foreach (ThingComp comp in AllComps)
		{
			if (comp is VehicleComp vehicleComp)
				vehicleComp.PostLoad();
		}
	}

	protected virtual void RegenerateUnsavedComponents()
	{
		vehicleAI = new VehicleAI(this);
		drawTracker = new VehicleDrawTracker(this);
		sustainers ??= new VehicleSustainers(this);
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		// Must register before comps call SpawnSetup to allow comps to access Registry
		RegisterEvents();
		base.SpawnSetup(map, respawningAfterLoad);

#if ANIMATOR
		if (VehicleDef.drawProperties.controller != null)
		{
			animator ??= new AnimationManager(this, VehicleDef.drawProperties.controller);
			animator.SetBool(PropertyIds.Disabled, CanMove);
			animator.PostLoad();
			this.AddEvent(VehicleEventDefOf.IgnitionOn, UpdateDraftAnimationProperty);
			this.AddEvent(VehicleEventDefOf.IgnitionOff, UpdateDraftAnimationProperty);
		}
#endif

		if (PropertyBlock == null)
			LongEventHandler.ExecuteWhenFinished(() => PropertyBlock = new MaterialPropertyBlock());

		// Ensure SustainerTarget and sustainer manager is given a clean slate to work with
		ReleaseSustainerTarget();
		EventRegistry[VehicleEventDefOf.Spawned].ExecuteEvents();
		if (Drafted)
		{
			// Trigger draft event if spawned with draft status On
			// This is important for sustainers and tick requests.
			EventRegistry[VehicleEventDefOf.IgnitionOn].ExecuteEvents();
		}

		sharedJob ??= new SharedJob();
		if (!respawningAfterLoad)
		{
			vehiclePather.ResetToCurrentPosition();
		}

		if (Faction != Faction.OfPlayer)
		{
			ignition.Drafted = true;
			CompVehicleTurrets turretComp = CompVehicleTurrets;
			if (turretComp != null)
			{
				foreach (VehicleTurret turret in turretComp.Turrets)
				{
					turret.autoTargeting = true;
					turret.AutoTarget = true;
				}
			}
		}

		RecachePawnCount();
		RecacheMovementPermissions();

		UpdateRotationAndAngle();
		UpdateDraftController();

		DrawTracker.Notify_Spawned();
		InitializeHitbox();
		Map.GetCachedMapComponent<VehiclePathingSystem>().RequestGridsFor(this);
		Map.GetCachedMapComponent<ListerVehiclesRepairable>().NotifyVehicleSpawned(this);
		ResetRenderStatus();

		UnityThread.ExecuteOnMainThread(ReclaimPosition);
		if (!respawningAfterLoad)
		{
			// Pawn::SpawnSetup checks game over condition but we need to redo this after registering this vehicle's position.
			UnityThread.ExecuteOnMainThread(Find.GameEnder.CheckOrUpdateGameOver);
		}
	}

	public override void ExposeData()
	{
		// Needs to occur before comps so vehicle can initialize managers before comps access them
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
			PostLoad();

		base.ExposeData();

		Scribe_Deep.Look(ref vehiclePather, nameof(vehiclePather), this);
		Scribe_Deep.Look(ref ignition, nameof(ignition), this);
		Scribe_Deep.Look(ref statHandler, nameof(statHandler), this);
		Scribe_Deep.Look(ref sharedJob, nameof(sharedJob));
		Scribe_Deep.Look(ref animator, nameof(animator), this, VehicleDef.drawProperties.controller);

		Scribe_Values.Look(ref angle, nameof(angle));
		Scribe_Values.Look(ref reverse, nameof(reverse));
		Scribe_Values.Look(ref crashLanded, nameof(crashLanded));

		Scribe_Deep.Look(ref patternData, nameof(patternData));
		Scribe_Defs.Look(ref retextureDef, nameof(retextureDef));
		Scribe_Deep.Look(ref patternToPaint, nameof(patternToPaint));
		Scribe_Values.Look(ref movementStatus, nameof(movementStatus), VehicleMovementStatus.Online);
		//Scribe_Values.Look(ref navigationCategory, nameof(navigationCategory), NavigationCategory.Opportunistic);
		Scribe_Values.Look(ref fishing, nameof(fishing));

		Scribe_Collections.Look(ref cargoToLoad, nameof(cargoToLoad), lookMode: LookMode.Deep);

		Scribe_Collections.Look(ref handlers, nameof(handlers), LookMode.Deep);
		Scribe_Collections.Look(ref boardingAssignments, nameof(boardingAssignments), LookMode.Deep);

		activatableComps ??= [];

		switch (Scribe.mode)
		{
			case LoadSaveMode.LoadingVars:
			{
				// statHandler won't be fully initialized until post-load, statHandler will be marked 'all dirty'
				// post load so we only need to make sure upgrades have been applied.
				using StatDirtyDisabler sdd = new(this);
				RecacheComponents();
				CompUpgradeTree?.ReactivateComps();
				// Execute after comp list has called PostExposeData so activated comps don't accidentally run
				// twice, registering duplicates to cross refs.
				LoadVarsActivatableComps();
			}
			break;
			case LoadSaveMode.PostLoadInit:
				CompUpgradeTree?.ReloadUnlocks();
				UpdateDraftController();
			break;
		}

		if (!deactivatedComps.NullOrEmpty())
		{
			foreach (ThingComp comp in deactivatedComps)
				comp.PostExposeData();
		}

		if (!VehicleMod.settings.main.useCustomShaders)
		{
			patternData = new PatternData(VehicleDef.graphicData.color,
				VehicleDef.graphicData.colorTwo,
				VehicleDef.graphicData.colorThree,
				PatternDefOf.Default, Vector2.zero, 0);
			retextureDef = null;
			patternToPaint = null;
		}
	}
}