using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Animations;
using UnityEngine;
using UnityEngine.Assertions;
using Vehicles.Rendering;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using Transform = SmashTools.Rendering.Transform;

namespace Vehicles;

/// <summary>
/// Rendering & Graphics
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public partial class VehiclePawn
{
  [AnimationProperty, TweakField]
  private VehicleDrawTracker drawTracker;

  public PatternData patternData;

  private RetextureDef retextureDef;

  // -45 is left, 45 is right : relative to Rot4 direction
  private float angle;
  private bool reverse;

  // Transform relative to DrawPos, excluding tweener offset
  [TweakField]
  [AnimationProperty(Name = "Transform")]
  private readonly Transform transform = new();

  public AnimationManager animator;
  private Graphic_Vehicle graphic;

  public PatternData patternToPaint;

  private bool crashLanded;

  public float CachedAngle { get; set; }

  public bool NorthSouthRotation => VehicleGraphic.EastDiagonalRotated &&
    (FullRotation == Rot8.NorthEast ||
      FullRotation == Rot8.SouthEast) || (VehicleGraphic.WestDiagonalRotated &&
      (FullRotation == Rot8.NorthWest ||
        FullRotation == Rot8.SouthWest));

  public bool CanPaintNow => patternToPaint != null;

  public bool Nameable => SettingsCache.TryGetValue(VehicleDef, typeof(VehicleDef),
    nameof(VehicleDef.nameable), VehicleDef.nameable);

  public override Vector3 DrawPos => DrawTracker.DrawPos + transform.position;

  public (Vector3 drawPos, float rotation) DrawData => (DrawPos, Angle);

  public ThingWithComps Thing => this;

  public RetextureDef Retexture => retextureDef;

  public MaterialPropertyBlock PropertyBlock { get; private set; }

  ModContentPack IAnimator.ModContentPack => VehicleDef.modContentPack;

  AnimationManager IAnimator.Manager => animator;

  string IAnimationObject.ObjectId => nameof(VehiclePawn);

  public Transform Transform => transform;

  public bool CrashLanded
  {
    get { return crashLanded; }
    set
    {
      if (crashLanded == value)
      {
        return;
      }

      crashLanded = value;
      if (!crashLanded)
      {
        if (!VehicleDef.graphicData.drawRotated)
        {
          Rotation = VehicleDef.defaultPlacingRot;
        }

        Angle = 0;
      }
    }
  }

  public float Angle
  {
    get
    {
      if (!VehicleMod.settings.main.allowDiagonalRendering ||
        !VehicleDef.properties.diagonalRotation)
      {
        return 0f;
      }

      return angle;
    }
    set
    {
      if (Mathf.Approximately(value, angle))
        return;
      // Flips across axis (negative = NE & SW, positive = NW & SE)
      angle = Reverse ? -value : value;
    }
  }

  public Rot8 FullRotation
  {
    get
    {
      if (!VehicleDef.graphicData.drawRotated)
      {
        return Rot8.North;
      }

      return new Rot8(Rotation, Angle);
    }
    set
    {
      if (value == FullRotation)
      {
        return;
      }

      Rotation = value;
      Angle = 0;
      if (value == Rot8.NorthEast || value == Rot8.SouthWest)
      {
        Angle = -45;
      }
      else if (value == Rot8.SouthEast || value == Rot8.NorthWest)
      {
        Angle = 45;
      }
    }
  }

  /// <summary>
  /// Vehicle is in reverse, flipping rotation for pathing
  /// </summary>
  public bool Reverse
  {
    get { return reverse; }
    set
    {
      vehiclePather.StopDead();
      reverse = value;
    }
  }

  [Obsolete("Vehicles should call DrawTracker instead of the vanilla implementation", true)]
  public new VehicleDrawTracker Drawer => DrawTracker;

  public VehicleDrawTracker DrawTracker
  {
    get
    {
      drawTracker ??= new VehicleDrawTracker(this);
      return drawTracker;
    }
  }

  public Graphic_Vehicle VehicleGraphic
  {
    get
    {
      graphic ??= GenerateGraphic();
      return graphic;
    }
  }

  public int MaterialCount => 8;

  public PatternDef PatternDef => Pattern;

  string IMaterialCacheTarget.Name => $"{VehicleDef}_{this}";

  public override Color DrawColor
  {
    get { return Pattern?.properties?.colorOne ?? patternData.color; }
    set { patternData.color = value; }
  }

  public new Color DrawColorTwo
  {
    get { return Pattern?.properties?.colorTwo ?? patternData.colorTwo; }
    set { patternData.colorTwo = value; }
  }

  public Color DrawColorThree
  {
    get { return Pattern?.properties?.colorThree ?? patternData.colorThree; }
    set { patternData.colorThree = value; }
  }

  public Vector2 Displacement
  {
    get { return patternData.displacement; }
    set { patternData.displacement = value; }
  }

  public float Tiles
  {
    get { return patternData.tiles; }
    set { patternData.tiles = value; }
  }

  public PatternDef Pattern
  {
    get
    {
      return patternData.patternDef ?? VehicleMod.settings.vehicles.defaultGraphics
         .TryGetValue(VehicleDef.defName, VehicleGraphic.DataRgb)?.patternDef ??
        PatternDefOf.Default;
    }
    set { patternData.patternDef = value; }
  }

  public Vector3 OverlayCenter
  {
    get
    {
      float movePercent = DrawTracker.tweener.MovedPercent();
      return GenThing.TrueCenter(Position, Rotation, VehicleDef.Size, VehicleDef.Altitude);
    }
  }

  public IEnumerable<AnimationDriver> Animations
  {
    get
    {
      if (CompVehicleLauncher != null)
      {
        foreach (AnimationDriver animationDriver in compVehicleLauncher.Animations)
        {
          yield return animationDriver;
        }
      }
    }
  }

  internal void SetRotationInt(Rot4 value, ref Rot4 rotationInt)
  {
    if (Reverse)
    {
      value = value.Opposite;
    }

    if (rotationInt == value)
      return;

    Rot4 oldRot = Rotation;

    if (Spawned)
    {
      // Don't let near-edge turns go through if it would put the vehicle out of bounds.
      if (!this.OccupiedRectShifted(IntVec2.Zero, value).InBounds(Map))
      {
        return;
      }

      Map.thingGrid.Deregister(this);
      Map.coverGrid.DeRegister(this);
    }

    rotationInt = value;

    if (Spawned)
    {
      Map.thingGrid.Register(this);
      Map.coverGrid.Register(this);
      ReclaimPosition();

      CellRect oldRect = this.OccupiedRectShifted(IntVec2.Zero, oldRot);
      CellRect newRect = this.OccupiedRectShifted(IntVec2.Zero, rotationInt);
      foreach (IntVec3 cell in oldRect.AllCellsNoRepeat(newRect))
      {
        Map.pathing.RecalculatePerceivedPathCostAt(cell);
      }
    }
  }

  public Vector3 TrueCenter()
  {
    return TrueCenter(Position);
  }

  public Vector3 TrueCenter(IntVec3 cell, float? altitude = null)
  {
    float altitudeValue = altitude ?? VehicleDef.Altitude;
    Vector3 result = cell.ToVector3ShiftedWithAltitude(altitudeValue);
    IntVec2 size = VehicleDef.Size;
    Rot8 rot = Rotation; //Switch to FullRotation when diagonal hitboxes are implemented
    if (size.x != 1 || size.z != 1)
    {
      if (rot.IsHorizontal)
      {
        (size.x, size.z) = (size.z, size.x);
      }

      switch (rot.AsInt)
      {
        case 0:
        case 2:
          if (size.x % 2 == 0)
          {
            result.x += 0.5f;
          }

          if (size.z % 2 == 0)
          {
            result.z += 0.5f;
          }

        break;
        case 1:
        case 3:
          if (size.x % 2 == 0)
          {
            result.x += 0.5f;
          }

          if (size.z % 2 == 0)
          {
            result.z -= 0.5f;
          }

        break;
      }
    }

    return result;
  }

  public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
  {
    if (this.AnimationLocked())
      return;

    DrawTracker.DynamicDrawPhaseAt(phase, transform.position + drawLoc, FullRotation,
      transform.rotation);

    if (phase == DrawPhase.Draw)
    {
      if (HighlightedComponent != null)
        statHandler.DrawHitbox(HighlightedComponent);
      Comps_PostDraw();
    }
  }

  protected override void DrawAt(Vector3 drawLoc, bool flip = false)
  {
    Log.ErrorOnce("Calling DrawAt instead of DynamicDrawPhaseAt", GetHashCode());
    DrawAt(drawLoc, FullRotation, transform.rotation);
  }

  public virtual void DrawAt(in Vector3 drawLoc, Rot8 rot, float rotation)
  {
    // Normally transform position and rotation
    DrawTracker.DynamicDrawPhaseAt(DrawPhase.Draw, drawLoc, rot, rotation);
  }

  public new void ProcessPostTickVisuals(int ticksPassed, CellRect viewRect)
  {
    if (!Suspended && Spawned && Current.ProgramState != ProgramState.Playing ||
      viewRect.Contains(Position))
    {
      DrawTracker.ProcessPostTickVisuals(ticksPassed);
    }
    rotationTracker.ProcessPostTickVisuals(ticksPassed);
  }

  public void ResetRenderStatus()
  {
    foreach (VehicleRoleHandler handler in handlers)
      DrawTracker.RemoveRenderer(handler);
    foreach (VehicleRoleHandler handler in handlers)
    {
      if (handler.role.PawnRenderer is not null)
        DrawTracker.AddRenderer(handler);
    }
  }

  public override void Notify_ColorChanged()
  {
    ResetMaterialProperties();
    EventRegistry[VehicleEventDefOf.ColorChanged].ExecuteEvents();
    base.Notify_ColorChanged();
  }

  public void ResetGraphic()
  {
    graphic = GenerateGraphic();
  }

  private void ResetMaterialProperties()
  {
    if (UnityData.IsInMainThread)
    {
      RGBMaterialPool.SetProperties(this, patternData);
      foreach (ThingComp thingComp in AllComps)
      {
        if (thingComp is VehicleComp vehicleComp)
        {
          vehicleComp.Notify_ColorChanged();
        }
      }
    }
  }

  private Graphic_Vehicle GenerateGraphic()
  {
    if (Destroyed && !RGBMaterialPool.GetAll(this).NullOrEmpty())
    {
      Log.Error(
        $"Reinitializing RGB Materials but {this} has already been destroyed and the cache was not cleared for this entry. This may result in a memory leak.");
      RGBMaterialPool.Release(this);
    }

    Graphic_Vehicle newGraphic;
    GraphicDataRGB graphicData = new();
    graphicData.CopyFrom(retextureDef?.graphicData ?? VehicleDef.graphicData);
    graphicData.color = patternData.color;
    graphicData.colorTwo = patternData.colorTwo;
    graphicData.colorThree = patternData.colorThree;
    graphicData.tiles = patternData.tiles;
    graphicData.displacement = patternData.displacement;
    graphicData.pattern = patternData.patternDef;

    if (graphicData.shaderType.Shader.SupportsRGBMaskTex())
    {
      RGBMaterialPool.CacheMaterialsFor(this);
      GraphicDatabaseRGB
       .Remove(this); // Clear cached graphic to pick up potential retexture changes
      graphicData.Init(this);
      newGraphic = graphicData.Graphic as Graphic_Vehicle;
      Assert.IsNotNull(newGraphic);
      RGBMaterialPool.SetProperties(this, patternData, newGraphic.TexAt, newGraphic.MaskAt);
    }
    else
    {
      // Triggers vanilla Init call for normal material caching
      newGraphic = ((GraphicData)graphicData).Graphic as Graphic_Vehicle;
      Assert.IsNotNull(newGraphic);
    }

    // Ensure meshes are cached beforehand, without needing to call this in EnsureInitialized event
    // for IParallelRenderer implementation.
    for (int i = 0; i < 8; i++)
      _ = newGraphic.MeshAtFull(new Rot8(i));

    // TODO - generate combined mesh here and/or build list of things that should be batch rendered

    return newGraphic;
  }

  public void UpdateRotationAndAngle()
  {
    UpdateRotation();
    UpdateAngle();
  }

  public void UpdateRotation()
  {
    if (vehiclePather.nextCell == Position)
    {
      return;
    }

    if (!VehicleDef.rotatable)
    {
      Rotation = VehicleDef.defaultPlacingRot;
      return;
    }

    IntVec3 intVec = vehiclePather.nextCell - Position;
    if (intVec.x > 0)
    {
      Rotation = Rot4.East;
    }
    else if (intVec.x < 0)
    {
      Rotation = Rot4.West;
    }
    else if (intVec.z > 0)
    {
      Rotation = Rot4.North;
    }
    else
    {
      Rotation = Rot4.South;
    }
  }

  public void UpdateAngle()
  {
    if (vehiclePather.Moving)
    {
      IntVec3 c = vehiclePather.nextCell - Position;
      if (c.x > 0 && c.z > 0)
      {
        angle = -45f;
      }
      else if (c.x > 0 && c.z < 0)
      {
        angle = 45f;
      }
      else if (c.x < 0 && c.z < 0)
      {
        angle = -45f;
      }
      else if (c.x < 0 && c.z > 0)
      {
        angle = 45f;
      }
      else
      {
        angle = 0f;
      }
    }
  }

  public override void DrawGUIOverlay()
  {
    // TODO - UI Overlays could still apply to vehicles
  }

  public override void DrawExtraSelectionOverlays()
  {
    base.DrawExtraSelectionOverlays();
    if (vehiclePather.curPath is { NodesLeft: > 0 })
    {
      vehiclePather.curPath.DrawPath(this);
    }

    RenderHelper.DrawLinesBetweenTargets(this, jobs.curJob, jobs.jobQueue);

    if (!cargoToLoad.NullOrEmpty())
    {
      foreach (TransferableOneWay transferable in cargoToLoad)
      {
        if (transferable.HasAnyThing)
        {
          GenDraw.DrawLineBetween(DrawPos, transferable.AnyThing.DrawPos);
        }
      }
    }
  }

  public override IEnumerable<Gizmo> GetGizmos()
  {
    if (Faction != Faction.OfPlayer && !DebugSettings.ShowDevGizmos)
    {
      yield break;
    }

    if (!MovementPermissions.HasFlag(VehiclePermissions.Immobile))
    {
      foreach (Gizmo gizmo in ignition.GetGizmos())
      {
        yield return gizmo;
      }
    }

    if (DebugSettings.ShowDevGizmos && Spawned && !pather.Moving)
    {
      yield return new Command_Action
      {
        defaultLabel = $"Gear: {(Reverse ? "Reverse" : "Drive")}",
        hotKey = KeyBindingDefOf_Vehicles.VF_Command_ReverseVehicle,
        action = delegate { Reverse = !Reverse; }
      };
      yield return new Command_Action
      {
        defaultLabel = "Teleport",
        action = delegate()
        {
          Find.Targeter.BeginTargeting(new TargetingParameters()
          {
            canTargetLocations = true,
            canTargetPawns = false,
            canTargetBuildings = false,
          }, delegate(LocalTargetInfo target)
          {
            Position = target.Cell;
            Notify_Teleported();
          }, highlightAction: (target) =>
          {
            Color color = LandingTargeter.GhostDrawerColor(Validator(target.Cell) ?
              LandingTargeter.PositionState.Valid :
              LandingTargeter.PositionState.Invalid);
            GhostDrawer.DrawGhostThing(target.Cell, FullRotation, VehicleDef.buildDef,
              VehicleDef.buildDef.graphic, color, AltitudeLayer.Blueprint);
          }, (LocalTargetInfo target) => Validator(target.Cell));
        }
      };

      bool Validator(IntVec3 cell)
      {
        VehiclePositionManager positionManager =
          Map.GetDetachedMapComponent<VehiclePositionManager>();
        foreach (IntVec3 cell2 in this.PawnOccupiedCells(cell, Rotation))
        {
          if (!cell2.InBounds(Map) || !GenGridVehicles.Walkable(cell2, VehicleDef, Map) ||
            positionManager.PositionClaimed(cell2))
          {
            return false;
          }
        }

        return true;
      }
    }

    bool upgrading = CompUpgradeTree != null && CompUpgradeTree.Upgrading;

    if (!cargoToLoad.NullOrEmpty())
    {
      Command_Action cancelLoad = new Command_Action
      {
        defaultLabel = "DesignatorCancel".Translate(),
        icon = VehicleDef.CancelCargoIcon,
        action = delegate()
        {
          Map.GetCachedMapComponent<VehicleReservationManager>()
           .RemoveLister(this, ReservationType.LoadVehicle);
          cargoToLoad.Clear();
        }
      };
      yield return cancelLoad;
    }
    else
    {
      Command_Action loadVehicle = new Command_Action
      {
        defaultLabel = "VF_LoadCargo".Translate(),
        icon = VehicleDef.LoadCargoIcon,
        action = delegate() { Find.WindowStack.Add(new Dialog_LoadCargo(this)); }
      };
      if (upgrading)
      {
        loadVehicle.Disable("VF_DisabledByVehicleUpgrading".Translate(LabelCap));
      }

      yield return loadVehicle;
    }

    if (FishingCompatibility.Active && SettingsCache.TryGetValue(VehicleDef,
      typeof(VehicleProperties),
      nameof(VehicleProperties.fishing), VehicleDef.properties.fishing))
    {
      Command_Toggle fishing = new Command_Toggle
      {
        defaultLabel = "VF_StartFishing".Translate(),
        defaultDesc = "VF_StartFishingDesc".Translate(),
        icon = VehicleTex.FishingIcon,
        isActive = () => currentlyFishing,
        toggleAction = delegate() { currentlyFishing = !currentlyFishing; }
      };
      yield return fishing;
    }

    Command_Action flagForLoading = new Command_Action
    {
      defaultLabel = "VF_HaulPawnToVehicle".Translate(),
      icon = VehicleTex.HaulPawnToVehicle,
      action = delegate()
      {
        SoundDefOf.Click.PlayOneShotOnCamera();
        HaulTargeter.BeginTargeting(new TargetingParameters()
        {
          canTargetPawns = true,
          canTargetBuildings = false,
          neverTargetHostileFaction = true,
          canTargetItems = false,
          thingCategory = ThingCategory.Pawn,
          validator = delegate(TargetInfo target)
          {
            if (!target.HasThing)
            {
              return false;
            }

            if (target.Thing is Pawn pawn)
            {
              if (pawn is VehiclePawn)
              {
                return false;
              }

              return pawn.Faction == Faction.OfPlayer || pawn.IsColonist || pawn.IsColonyMech ||
                pawn.IsSlaveOfColony || pawn.IsPrisonerOfColony;
            }

            return false;
          }
        }, delegate(LocalTargetInfo target)
        {
          if (target.Thing is Pawn pawn && pawn.IsColonistPlayerControlled && !pawn.Downed)
          {
            VehicleRoleHandler handler = pawn.IsColonistPlayerControlled ?
              NextAvailableHandler() :
              handlers.FirstOrDefault(handler => handler.AreSlotsAvailableAndReservable &&
                handler.role.HandlingTypes == HandlingType.None);
            PromptToBoardVehicle(pawn, handler);
            return;
          }

          TransferableOneWay transferable = new TransferableOneWay()
          {
            things = new List<Thing>() { target.Thing },
          };
          transferable.AdjustTo(1);
          cargoToLoad.Add(transferable);
          Map.GetCachedMapComponent<VehicleReservationManager>()
           .RegisterLister(this, ReservationType.LoadVehicle);
        }, this);
      }
    };
    if (upgrading)
    {
      flagForLoading.Disable("VF_DisabledByVehicleUpgrading".Translate(LabelCap));
    }

    yield return flagForLoading;

    if (!Drafted)
    {
      Command_Action unloadAll = new Command_Action
      {
        defaultLabel = "VF_DisembarkAllPawns".Translate(),
        icon = VehicleTex.UnloadAll,
        action = DisembarkAll,
        hotKey = KeyBindingDefOf.Misc2
      };
      yield return unloadAll;
      bool exitBlocked = !SurroundingCells.NotNullAndAny(cell => cell.Walkable(Map));
      if (exitBlocked)
      {
        unloadAll.Disable("VF_DisembarkNoExit".Translate());
      }

      foreach (VehicleRoleHandler handler in handlers)
      {
        for (int i = 0; i < handler.thingOwner.Count; i++)
        {
          Pawn currentPawn = handler.thingOwner.InnerListForReading[i];
          Command_ActionPawnDrawer unloadAction = new Command_ActionPawnDrawer();
          unloadAction.defaultLabel = "VF_DisembarkSinglePawn".Translate(currentPawn.LabelShort);
          unloadAction.groupable = false;
          unloadAction.pawn = currentPawn;
          unloadAction.action = delegate() { DisembarkPawn(currentPawn); };
          if (exitBlocked)
          {
            unloadAction.Disable("VF_DisembarkNoExit".Translate());
          }

          yield return unloadAction;
        }
      }
    }

    if (this.GetLord()?.LordJob is LordJob_FormAndSendVehicles formCaravanLordJob)
    {
      Command_Action forceCaravanLeave = new()
      {
        defaultLabel = "VF_ForceLeaveCaravan".Translate(),
        defaultDesc = "VF_ForceLeaveCaravanDesc".Translate(),
        icon = TexData.CaravanIcon,
        activateSound = SoundDefOf.Tick_Low,
        action = delegate()
        {
          formCaravanLordJob.ForceCaravanLeave();
          Messages.Message("VF_ForceLeaveConfirmation".Translate(),
            MessageTypeDefOf.TaskCompletion);
        }
      };
      yield return forceCaravanLeave;

      Command_Action cancelCaravan = new()
      {
        defaultLabel = "CommandCancelFormingCaravan".Translate(),
        defaultDesc = "CommandCancelFormingCaravanDesc".Translate(),
        icon = TexCommand.ClearPrioritizedWork,
        activateSound = SoundDefOf.Tick_Low,
        action = delegate() { CaravanFormingUtility.StopFormingCaravan(formCaravanLordJob.lord); },
        hotKey = KeyBindingDefOf.Designator_Cancel
      };
      yield return cancelCaravan;
    }

    foreach (ThingComp comp in AllComps)
    {
      foreach (Gizmo gizmo in comp.CompGetGizmosExtra())
      {
        yield return gizmo;
      }
    }

    if (DebugSettings.ShowDevGizmos && Spawned)
    {
      yield return new Command_Action
      {
        defaultLabel = "Destroy Component",
        action = delegate()
        {
          var options = new List<FloatMenuOption>();
          foreach (VehicleComponent component in statHandler.components)
          {
            options.Add(new FloatMenuOption(component.props.label, delegate()
            {
              component.TakeDamage(this, new DamageInfo(DamageDefOf.Vaporize, float.MaxValue),
                ignoreArmor: true);
              Notify_TookDamage();
            }));
          }

          if (!options.NullOrEmpty())
          {
            Find.WindowStack.Add(new FloatMenu(options));
          }
        }
      };
      yield return new Command_Action
      {
        defaultLabel = "Damage Component",
        action = delegate()
        {
          var options = new List<FloatMenuOption>();
          foreach (VehicleComponent component in statHandler.components)
          {
            options.Add(new FloatMenuOption(component.props.label, delegate()
            {
              component.TakeDamage(this,
                new DamageInfo(DamageDefOf.Vaporize, component.health * Rand.Range(0.1f, 1)),
                ignoreArmor: true);
              Notify_TookDamage();
            }));
          }

          if (!options.NullOrEmpty())
          {
            Find.WindowStack.Add(new FloatMenu(options));
          }
        }
      };
      yield return new Command_Action
      {
        defaultLabel = "Explode Component",
        action = delegate
        {
          List<FloatMenuOption> options = [];
          foreach (VehicleComponent component in statHandler.components)
          {
            if (component.props.GetReactor<Reactor_Explosive>() is { } reactorExplosive)
            {
              options.Add(new FloatMenuOption(component.props.label,
                delegate { reactorExplosive.SpawnExploder(this, component); }));
            }
          }

          if (!options.NullOrEmpty())
          {
            Find.WindowStack.Add(new FloatMenu(options));
          }
        }
      };
      yield return new Command_Action
      {
        defaultLabel = "Heal All Components",
        action = delegate()
        {
          statHandler.components.ForEach(c => c.HealComponent(float.MaxValue));
          Map.GetCachedMapComponent<ListerVehiclesRepairable>().NotifyVehicleRepaired(this);
        }
      };
      yield return new Command_Action
      {
        defaultLabel = "Recache All Stats",
        action = delegate() { statHandler.MarkAllDirty(); }
      };
      yield return new Command_Action()
      {
        defaultLabel = "Give Random Pawn MentalState",
        action = delegate()
        {
          if (AllPawnsAboard.TryRandomElement(out Pawn result))
          {
            foreach (MentalStateDef mentalState in DefDatabase<MentalStateDef>
             .AllDefsListForReading)
            {
              if (result.mindState.mentalStateHandler.TryStartMentalState(mentalState, "testing"))
              {
                break;
              }
              else
              {
                Log.Warning($"Failed to execute {mentalState} inside vehicles.");
              }
            }
          }
        }
      };
      yield return new Command_Action()
      {
        defaultLabel = "Kill Random Pawn",
        action = delegate()
        {
          Pawn pawn = AllPawnsAboard.RandomElementWithFallback();
          pawn?.Kill(null);
        }
      };
      yield return new Command_Action()
      {
        defaultLabel = "Flash OccupiedRect",
        action = delegate()
        {
          if (vehiclePather.Moving)
          {
            IntVec3 prevCell = Position;
            Rot8 rot = FullRotation;
            HashSet<IntVec3> cellsToHighlight = [];
            foreach (IntVec3 cell in vehiclePather.curPath.Nodes)
            {
              if (prevCell != cell)
                rot = Rot8.DirectionFromCells(prevCell, cell);
              if (!rot.IsValid)
                rot = Rot8.North;
              foreach (IntVec3 occupiedCell in this.VehicleRect(cell, rot).Cells)
              {
                if (occupiedCell.InBounds(Map) && cellsToHighlight.Add(occupiedCell))
                {
                  Map.debugDrawer.FlashCell(occupiedCell, 0.95f, duration: 180);
                }
              }

              prevCell = cell;
            }
          }
          else
          {
            CellRect occupiedRect = this.OccupiedRect();
            foreach (IntVec3 cell in occupiedRect)
            {
              if (cell.InBounds(Map))
              {
                Map.debugDrawer.FlashCell(cell, 0.95f, duration: 180);
              }
            }

            CellRect vehicleRect = this.VehicleRect();
            foreach (IntVec3 cell in vehicleRect)
            {
              if (cell.InBounds(Map))
              {
                Map.debugDrawer.FlashCell(cell, 0, duration: 180);
              }
            }
          }
        }
      };
      if (animator != null)
      {
        if (CompVehicleLauncher != null)
        {
          yield return new Command_Action()
          {
            defaultLabel = "Toggle Loitering",
            action = delegate()
            {
              CompVehicleLauncher.loiter = !CompVehicleLauncher.loiter;
              animator.SetBool(PropertyIds.Loiter, CompVehicleLauncher.loiter);
            }
          };
        }
      }
    }
  }

  public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
  {
    if (selPawn is null)
      yield break;

    if (selPawn.Faction != Faction)
      yield break;

    if (!selPawn.RaceProps.ToolUser)
      yield break;

    if (selPawn is VehiclePawn)
      yield break;

    if (!selPawn.CanReserveAndReach(this, PathEndMode.InteractionCell, Danger.Deadly))
      yield break;

    if (movementStatus is VehicleMovementStatus.Offline)
      yield break;

    if (!IdeoAllowsBoarding(selPawn))
    {
      yield return new FloatMenuOption("VF_CantEnterVehicle_IdeoligionForbids".Translate(), null);
      yield break;
    }

    foreach (ThingComp thingComp in AllComps)
    {
      if (thingComp is VehicleComp vehicleComp)
      {
        foreach (FloatMenuOption floatMenuOption in vehicleComp.CompFloatMenuOptions())
        {
          yield return floatMenuOption;
        }
      }
    }

    foreach (VehicleRoleHandler handler in handlers)
    {
      if (handler.AreSlotsAvailableAndReservable)
      {
        VehicleReservationManager reservationManager =
          Map.GetCachedMapComponent<VehicleReservationManager>();
        FloatMenuOption opt = new("VF_EnterVehicle".Translate(LabelShort,
            handler.role.label,
            (handler.role.Slots - (handler.thingOwner.Count +
              reservationManager.GetReservation<VehicleHandlerReservation>(this)
              ?.ClaimantsOnHandler(handler) ?? 0)).ToString()),
          delegate { PromptToBoardVehicle(selPawn, handler); });
        yield return opt;
      }
    }
  }

  public void PromptToBoardVehicle(Pawn pawn, VehicleRoleHandler handler)
  {
    if (handler == null)
    {
      Messages.Message("VF_HandlerNotEnoughRoom".Translate(pawn, this),
        MessageTypeDefOf.RejectInput,
        historical: false);
      return;
    }

    Job job = new Job(JobDefOf_Vehicles.Board, this);
    GiveLoadJob(pawn, handler);
    pawn.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);
    if (!pawn.Spawned)
    {
      return;
    }

    Map.GetCachedMapComponent<VehicleReservationManager>()
     .Reserve<VehicleRoleHandler, VehicleHandlerReservation>(this, pawn, pawn.CurJob, handler);
  }

  public bool IdeoAllowsBoarding(Pawn selPawn)
  {
    if (!ModsConfig.IdeologyActive)
    {
      return true;
    }

    switch (this.VehicleDef.type)
    {
      case VehicleType.Air:
        if (!IdeoUtility.DoerWillingToDo(HistoryEventDefOf_Vehicles.VF_BoardAirVehicle, selPawn))
        {
          return false;
        }

      break;
      case VehicleType.Sea:
        if (!IdeoUtility.DoerWillingToDo(HistoryEventDefOf_Vehicles.VF_BoardSeaVehicle, selPawn))
        {
          return false;
        }

      break;
      case VehicleType.Land:
        if (!IdeoUtility.DoerWillingToDo(HistoryEventDefOf_Vehicles.VF_BoardLandVehicle, selPawn))
        {
          return false;
        }

      break;
      case VehicleType.Universal:
        if (!IdeoUtility.DoerWillingToDo(HistoryEventDefOf_Vehicles.VF_BoardUniversalVehicle,
          selPawn))
        {
          return false;
        }

      break;
    }

    return true;
  }


  public void ChangeColor()
  {
    Dialog_VehiclePainter.OpenColorPicker(this, delegate(Color colorOne, Color colorTwo,
      Color colorThree,
      PatternDef patternDef, Vector2 displacement, float tiles)
    {
      patternToPaint =
        new PatternData(colorOne, colorTwo, colorThree, patternDef, displacement, tiles);
      if (DebugSettings.godMode)
      {
        SetColor();
      }
    });
  }

  public void SetRetexture(RetextureDef retextureDef)
  {
    SetRetextureInternal(this, retextureDef);
  }

  private static void SetRetextureInternal(VehiclePawn vehicle, RetextureDef retextureDef)
  {
    vehicle.retextureDef = retextureDef;
    vehicle.ResetGraphic();
    vehicle.Notify_ColorChanged();
  }

  public void Rename()
  {
    if (Nameable)
    {
      Find.WindowStack.Add(new Dialog_GiveVehicleName(this));
    }
  }

  public void SetColor()
  {
    if (!CanPaintNow)
    {
      return;
    }

    patternData.Copy(patternToPaint);

    DrawColor = patternData.color;
    DrawColorTwo = patternData.colorTwo;
    DrawColorThree = patternData.colorThree;
    Notify_ColorChanged();

    patternToPaint = null;
  }

  public virtual float DoInspectPaneButtons(float x)
  {
    const float IconSize = 30;

    Rect rect = new(x, 0f, IconSize, IconSize);
    float usedWidth = 0;
    if (Nameable)
    {
      rect.x -= rect.width;
      usedWidth += rect.width;
      {
        TooltipHandler.TipRegionByKey(rect, "VF_RenameVehicleTooltip");
        if (Widgets.ButtonImage(rect, TexData.Rename))
        {
          Rename();
        }
      }
    }

    if (VehicleMod.settings.main.useCustomShaders && VehicleGraphic.Shader.SupportsRGBMaskTex())
    {
      rect.x -= rect.width;
      usedWidth += rect.width;
      {
        TooltipHandler.TipRegionByKey(rect, "VF_RecolorTooltip");
        if (Widgets.ButtonImage(rect, VehicleTex.Recolor))
        {
          ChangeColor();
        }
      }
    }

    if (Prefs.DevMode)
    {
      rect.x -= rect.width;
      usedWidth += rect.width;
      {
        if (Widgets.ButtonImage(rect, VehicleTex.Settings))
        {
          List<FloatMenuOption> options = new List<FloatMenuOption>();
          options.Add(new FloatMenuOption("Tweak Values",
            delegate() { Find.WindowStack.Add(new EditWindow_TweakFields(this)); }));
          if (CompVehicleLauncher != null)
          {
            options.Add(new FloatMenuOption("Open in Graph Editor", OpenInAnimator));
          }
#if DEBUG
          options.Add(new FloatMenuOption("Open in Animator (test version)", OpenInAnimatorTemp));
#endif
          if (!options.NullOrEmpty())
          {
            Find.WindowStack.Add(new FloatMenu(options));
          }
          else
          {
            Messages.Message($"{this} doesn't have any configuration options available.",
              MessageTypeDefOf.RejectInput,
              historical: false);
          }
        }
      }
    }

    return usedWidth;
  }

  public void OpenInAnimator()
  {
    Dialog_GraphEditor dialog_GraphEditor = new Dialog_GraphEditor(this, false);
    //dialog_GraphEditor.LogReport = VehicleMod.settings.debug.debugLogging;
    Find.WindowStack.Add(dialog_GraphEditor);
  }

  public void OpenInAnimatorTemp()
  {
    Dialog_AnimationEditor dialogGraphEditor = new(this);
    Find.WindowStack.Add(dialogGraphEditor);
  }

  public void MultiplePawnFloatMenuOptions(List<Pawn> pawns)
  {
    List<FloatMenuOption> options = new List<FloatMenuOption>();

    if (pawns.Any(pawn => !IdeoAllowsBoarding(pawn)))
    {
      options.Add(new FloatMenuOption("VF_CantEnterVehicle_IdeoligionForbids".Translate(), null));
    }
    else
    {
      VehicleReservationManager reservationManager =
        Map.GetCachedMapComponent<VehicleReservationManager>();
      FloatMenuOption opt1 = new FloatMenuOption("VF_BoardVehicleGroup".Translate(LabelShort),
        delegate()
        {
          List<IntVec3> cells = this.OccupiedRect().Cells.ToList();
          foreach (Pawn p in pawns)
          {
            if (cells.Contains(p.Position))
            {
              continue;
            }

            VehicleRoleHandler handler = p.IsColonistPlayerControlled ?
              NextAvailableHandler() :
              handlers.FirstOrDefault(handler => handler.AreSlotsAvailableAndReservable &&
                handler.role.HandlingTypes == HandlingType.None);
            PromptToBoardVehicle(p, handler);
          }
        });
      FloatMenuOption opt2 =
        new FloatMenuOption("VF_BoardVehicleGroupFail".Translate(LabelShort), null)
        {
          Disabled = true
        };
      int r = 0;
      foreach (VehicleRoleHandler h in handlers)
      {
        r += reservationManager.GetReservation<VehicleHandlerReservation>(this)
        ?.ClaimantsOnHandler(h) ?? 0;
      }

      options.Add(pawns.Count + r > SeatsAvailable ? opt2 : opt1);
    }

    FloatMenuMulti floatMenuMap =
      new FloatMenuMulti(options, pawns, this, pawns[0].LabelCap, Verse.UI.MouseMapPosition())
      {
        givesColonistOrders = true
      };
    Find.WindowStack.Add(floatMenuMap);
  }
}