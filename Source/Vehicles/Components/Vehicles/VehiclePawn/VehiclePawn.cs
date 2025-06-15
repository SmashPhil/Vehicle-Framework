using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Animations;
using SmashTools.Rendering;
using UnityEngine;
using Vehicles.Rendering;
using Verse;

namespace Vehicles
{
  public partial class VehiclePawn : Pawn, IInspectable, IThingHolderTickable,
                                     IAnimationTarget, IAnimator, ITransformable,
                                     IEventManager<VehicleEventDef>, IMaterialCacheTarget
  {
    public EventManager<VehicleEventDef> EventRegistry { get; set; }

    public VehicleDef VehicleDef => def as VehicleDef;

    public Pawn FindPawnWithBestStat(StatDef stat, Predicate<Pawn> pawnValidator = null)
    {
      Pawn bestPawn = null;
      float curValue = -1f;
      foreach (Pawn pawn in AllPawnsAboard)
      {
        if (!pawn.Dead && !pawn.Downed && !pawn.InMentalState &&
          CaravanUtility.IsOwner(pawn, Faction) && !stat.Worker.IsDisabledFor(pawn) &&
          (pawnValidator is null || pawnValidator(pawn)))
        {
          float statValue = pawn.GetStatValue(stat);
          if (bestPawn == null || statValue > curValue)
          {
            bestPawn = pawn;
            curValue = statValue;
          }
        }
      }

      return bestPawn;
    }

    public int AverageSkillOfCapablePawns(SkillDef skill)
    {
      if (AllCapablePawns.Count == 0)
      {
        return 0;
      }

      int value = 0;
      foreach (Pawn p in AllCapablePawns)
      {
        value += p.skills.GetSkill(skill).Level;
      }

      value /= AllCapablePawns.Count;
      return value;
    }

    private void InitializeVehicle()
    {
      if (handlers is { Count: > 0 })
        return;

      cargoToLoad ??= [];
      bills ??= [];

      if (!VehicleDef.properties.roles.NullOrEmpty())
      {
        foreach (VehicleRole role in VehicleDef.properties.roles)
        {
          handlers.Add(new VehicleRoleHandler(this, role));
        }
      }
      CacheCompRenderers();
      RecacheComponents();
      RecacheMovementPermissions();
    }

    public override void PostMapInit()
    {
      vehiclePather.TryResumePathingAfterLoading();
    }

    public virtual void PostGenerationSetup()
    {
      this.RegisterEvents();
      InitializeVehicle();
      ageTracker.AgeBiologicalTicks = 0;
      ageTracker.AgeChronologicalTicks = 0;
      ageTracker.BirthAbsTicks = 0;
      //health.Reset();
      statHandler.InitializeComponents();

      if (Faction != Faction.OfPlayer && VehicleDef.npcProperties != null)
      {
        GenerateInventory();
      }
    }

    public override void PostMake()
    {
      base.PostMake();
      this.EnsureUncachedCompList();
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

    public void RegisterEvents()
    {
      if (EventRegistry != null && EventRegistry.Initialized())
        return; //Disallow re-registering events

      this.FillEvents_Def();

      this.AddEvent(VehicleEventDefOf.CargoAdded, statHandler.MarkAllDirty);
      this.AddEvent(VehicleEventDefOf.CargoRemoved, statHandler.MarkAllDirty);
      this.AddEvent(VehicleEventDefOf.PawnEntered, RecachePawnCount);
      this.AddEvent(VehicleEventDefOf.PawnExited, vehiclePather.RecalculatePermissions,
        RecachePawnCount);
      this.AddEvent(VehicleEventDefOf.PawnRemoved, vehiclePather.RecalculatePermissions,
        RecachePawnCount);
      this.AddEvent(VehicleEventDefOf.PawnChangedSeats, vehiclePather.RecalculatePermissions,
        RecachePawnCount);
      this.AddEvent(VehicleEventDefOf.PawnKilled, vehiclePather.RecalculatePermissions,
        RecachePawnCount);
      this.AddEvent(VehicleEventDefOf.PawnCapacitiesDirty, vehiclePather.RecalculatePermissions);
      this.AddEvent(VehicleEventDefOf.IgnitionOff, vehiclePather.RecalculatePermissions);
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
      this.RegisterEvents();
      RegenerateUnsavedComponents();
      CacheCompRenderers();
      RecacheComponents();
      RecachePawnCount();
      animator?.PostLoad();

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
      this.RegisterEvents();
      base.SpawnSetup(map, respawningAfterLoad);

#if ANIMATOR
      if (VehicleDef.drawProperties.controller != null)
      {
        animator ??= new AnimationManager(this, VehicleDef.drawProperties.controller);
        animator.SetBool(PropertyIds.Disabled, CanMove);
        animator.PostLoad();
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

      foreach (Pawn pawn in AllPawnsAboard)
      {
        if (pawn.IsWorldPawn())
        {
          // Remove internal pawns from WorldPawns
          Find.WorldPawns.RemovePawn(pawn);
        }
      }

      foreach (Thing thing in inventory.innerContainer)
      {
        if (thing is Pawn pawn)
        {
          // Remove inventory pawns in case some were transfered here (like animals)
          Find.WorldPawns.RemovePawn(pawn);
        }
      }

      UpdateRotationAndAngle();

      DrawTracker.Notify_Spawned();
      InitializeHitbox();
      Map.GetCachedMapComponent<VehiclePathingSystem>().RequestGridsFor(this);
      ReclaimPosition();
      Map.GetCachedMapComponent<ListerVehiclesRepairable>().NotifyVehicleSpawned(this);
      ResetRenderStatus();
    }

    public override void ExposeData()
    {
      base.ExposeData();

      Scribe_Collections.Look(ref activatableComps, nameof(activatableComps),
        lookMode: LookMode.Deep, this);
      activatableComps ??= [];

      if (Scribe.mode == LoadSaveMode.LoadingVars)
      {
        SyncActivatableComps();
      }

      if (!deactivatedComps.NullOrEmpty())
      {
        foreach (ThingComp comp in deactivatedComps)
        {
          comp.PostExposeData();
        }
      }

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

      if (!VehicleMod.settings.main.useCustomShaders)
      {
        patternData = new PatternData(VehicleDef.graphicData.color,
          VehicleDef.graphicData.colorTwo,
          VehicleDef.graphicData.colorThree,
          PatternDefOf.Default, Vector2.zero, 0);
        retextureDef = null;
        patternToPaint = null;
      }

      Scribe_Values.Look(ref movementStatus, nameof(movementStatus), VehicleMovementStatus.Online);
      //Scribe_Values.Look(ref navigationCategory, nameof(navigationCategory), NavigationCategory.Opportunistic);
      Scribe_Values.Look(ref currentlyFishing, nameof(currentlyFishing));
      Scribe_Values.Look(ref showAllItemsOnMap, nameof(showAllItemsOnMap));

      Scribe_Collections.Look(ref cargoToLoad, nameof(cargoToLoad), lookMode: LookMode.Deep);

      Scribe_Collections.Look(ref handlers, nameof(handlers), LookMode.Deep);
      Scribe_Collections.Look(ref bills, nameof(bills), LookMode.Deep);

      if (Scribe.mode == LoadSaveMode.PostLoadInit)
      {
        this.EnsureUncachedCompList();
        PostLoad();
      }
    }
  }
}