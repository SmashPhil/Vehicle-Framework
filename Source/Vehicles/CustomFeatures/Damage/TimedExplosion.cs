using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using SmashTools.Rendering;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Vehicles;

// TODO - Create ExplosionManager to contain these
// TODO - Rename to something more descriptive
[PublicAPI]
[StaticConstructorOnStartup]
public class TimedExplosion : IExposable, IParallelRenderer
{
  private const float WickerFlickerTicks = 3;
  private const int ExplosionNotificationInterval = 60;

  private static readonly int PawnNotifyCellCount = GenRadial.NumCellsInRadius(4.5f);

  private static readonly Material WickMaterialA =
    MaterialPool.MatFrom("Things/Special/BurningWickA", ShaderDatabase.MetaOverlay);

  private static readonly Material WickMaterialB =
    MaterialPool.MatFrom("Things/Special/BurningWickB", ShaderDatabase.MetaOverlay);

  private static readonly float WickAltitude = AltitudeLayer.MetaOverlays.AltitudeFor();

  // internal void Pawn_MindState::Notify_DangerousExploderAboutToExplode(Thing exploder)
  private static readonly MethodInfo Notify_DangerousExploderAboutToExplode = AccessTools.Method(
    typeof(Pawn_MindState),
    "Notify_DangerousExploderAboutToExplode");

  private int ticksLeft;

  public VehiclePawn vehicle;

  private PreRenderResults results;
  private Data data;
  private readonly DrawOffsets drawOffsets;

  private Sustainer wickSoundSustainer;

  private readonly List<Pawn> pawnsNotifiedOfExplosion = [];

  // NOTE - while this could be an event, setting it up to save / load would be a massive
  // pain, and right now it's only used for the pop turret event.
  public Action<TimedExplosion> explosionCallback;

  bool IParallelRenderer.IsDirty { get; set; }

  public bool Active { get; private set; }

  public TimedExplosion(VehiclePawn vehicle, Data data,
    DrawOffsets drawOffsets = null)
  {
    this.vehicle = vehicle;
    this.drawOffsets = drawOffsets;
    this.data = data;
    ticksLeft = data.wickTicks;

    Start();
  }

  private IntVec3 AdjustedCell
  {
    get
    {
      IntVec2 adjustedLoc =
        data.cell.RotatedBy(vehicle.Rotation, vehicle.VehicleDef.Size, reverseRotate: true);
      IntVec3 adjustedCell = new(adjustedLoc.x + vehicle.Position.x, 0,
        adjustedLoc.z + vehicle.Position.z);
      return adjustedCell;
    }
  }

  public void DynamicDrawPhaseAt(DrawPhase phase, in TransformData transformData,
    bool forceDraw = false)
  {
    switch (phase)
    {
      case DrawPhase.EnsureInitialized:
      break;
      case DrawPhase.ParallelPreDraw:
        results = ParallelGetPreRenderResults(in transformData, forceDraw: forceDraw);
      break;
      case DrawPhase.Draw:
        // Out of phase drawing must immediately generate pre-render results for valid data.
        if (!results.valid)
          results = ParallelGetPreRenderResults(in transformData, forceDraw: forceDraw);
        Draw();
        results = default;
      break;
      default:
        throw new NotImplementedException();
    }
  }

  private PreRenderResults ParallelGetPreRenderResults(ref readonly TransformData transformData,
    bool forceDraw = false)
  {
    PreRenderResults render = new()
    {
      valid = true,
      draw = true,
      material = (vehicle.thingIDNumber + Find.TickManager.TicksGame) % (WickerFlickerTicks * 2) <
        WickerFlickerTicks ?
          WickMaterialA :
          WickMaterialB
    };
    Vector3 drawLoc = transformData.position with { y = WickAltitude };
    if (drawOffsets != null)
    {
      Vector3 offset = drawOffsets.OffsetFor(transformData.orientation);
      drawLoc += offset;
    }
    else
    {
      IntVec2 rotatedHitCell =
        data.cell.RotatedBy(transformData.orientation, vehicle.VehicleDef.Size,
          reverseRotate: true);
      Vector3 position = rotatedHitCell.ToIntVec3.ToVector3();
      position = position.RotatedBy(transformData.orientation.AsRotationAngle);
      drawLoc = new Vector3(drawLoc.x + position.x, drawLoc.y, drawLoc.z + position.z);
    }
    render.matrix = Matrix4x4.TRS(drawLoc, Quaternion.identity, Vector3.one);
    return render;
  }

  private void Draw()
  {
    if (!Active || !results.draw)
      return;
    Graphics.DrawMesh(MeshPool.plane20, results.matrix, results.material, 0);
  }

  private void Start()
  {
    if (!Active)
    {
      Active = true;
      StartWickSustainer();
      NotifyPawnsOfExplosion();
    }
  }

  private void End()
  {
    Active = false;
    wickSoundSustainer?.End();

    foreach (Pawn pawn in pawnsNotifiedOfExplosion)
    {
      if (pawn.mindState != null && pawn.mindState.knownExploder == vehicle)
      {
        pawn.mindState.knownExploder = null;
      }
    }
  }

  public bool Tick()
  {
    if (!Active || !vehicle.Spawned)
    {
      return false;
    }

    if (ticksLeft % ExplosionNotificationInterval == 0)
    {
      NotifyPawnsOfExplosion();
    }

    UpdateWick();
    if (ticksLeft <= 0)
    {
      Explode();
      return false;
    }

    ticksLeft--;
    return true;
  }

  private void NotifyPawnsOfExplosion()
  {
    if (!data.notifyNearbyPawns)
      return;

    for (int index1 = 0; index1 < PawnNotifyCellCount; ++index1)
    {
      IntVec3 notifyAtCell = AdjustedCell + GenRadial.RadialPattern[index1];
      if (notifyAtCell.InBounds(vehicle.MapHeld))
      {
        //using ListSnapshot<Thing> thingList = new(notifyAtCell.GetThingList(vehicle.MapHeld));
        foreach (Thing thing in notifyAtCell.GetThingList(vehicle.MapHeld))
        {
          if (thing is Pawn pawn && CanNotifyPawn(pawn))
          {
            Room room = pawn.GetRoom();
            if (room == null || room.CellCount == 1 || room == vehicle.GetRoom() &&
              GenSight.LineOfSightToThing(pawn.Position, vehicle, vehicle.MapHeld, true))
            {
              pawnsNotifiedOfExplosion.Add(pawn);
              Notify_DangerousExploderAboutToExplode.Invoke(pawn.mindState, [vehicle]);
            }
          }
        }
      }
    }
    return;

    bool CanNotifyPawn(Pawn pawn)
    {
      return pawn.RaceProps.intelligence >= Intelligence.Humanlike &&
        data.damageDef.ExternalViolenceFor(pawn);
    }
  }

  private void UpdateWick()
  {
    if (wickSoundSustainer == null)
    {
      StartWickSustainer();
    }
    else
    {
      wickSoundSustainer.Maintain();
    }
  }

  private void StartWickSustainer()
  {
    SoundInfo info = SoundInfo.InMap(vehicle, MaintenanceType.PerTick);
    wickSoundSustainer = SoundDefOf.HissSmall.TrySpawnSustainer(info);
  }

  private void Explode()
  {
    End();
    GenExplosion.DoExplosion(AdjustedCell, vehicle.Map, data.radius, data.damageDef, vehicle,
      data.damageAmount,
      data.armorPenetration);
    explosionCallback?.Invoke(this);
  }

  public void ExposeData()
  {
    Scribe_References.Look(ref vehicle, nameof(vehicle));
    Scribe_Deep.Look(ref data, nameof(data));
    Scribe_Values.Look(ref ticksLeft, nameof(ticksLeft));
    // TODO
    //Scribe_Values.Look(ref explosionCallback, nameof(explosionCallback));
  }

  private struct PreRenderResults
  {
    public bool valid;
    public bool draw;
    public Material material;
    public Matrix4x4 matrix;
  }

  public class Data : IExposable
  {
    public IntVec2 cell;
    public int wickTicks;
    public int radius;
    public DamageDef damageDef;
    public int damageAmount;
    public float armorPenetration;
    public bool notifyNearbyPawns;

    public Data(IntVec2 cell, int wickTicks, int radius,
      DamageDef damageDef, int damageAmount, float armorPenetration = -1,
      bool notifyNearbyPawns = true)
    {
      this.cell = cell;
      this.wickTicks = wickTicks;
      this.radius = radius;
      this.damageDef = damageDef;
      this.damageAmount = damageAmount;
      this.armorPenetration = armorPenetration;
      this.notifyNearbyPawns = notifyNearbyPawns;

      if (this.armorPenetration < 0)
      {
        this.armorPenetration = damageDef.defaultArmorPenetration;
      }
    }

    public void ExposeData()
    {
      Scribe_Values.Look(ref cell, nameof(cell));
      Scribe_Values.Look(ref wickTicks, nameof(wickTicks));
      Scribe_Values.Look(ref radius, nameof(radius));
      Scribe_Defs.Look(ref damageDef, nameof(damageDef));
      Scribe_Values.Look(ref damageAmount, nameof(damageAmount));
      Scribe_Values.Look(ref armorPenetration, nameof(armorPenetration), defaultValue: -1);
      Scribe_Values.Look(ref notifyNearbyPawns, nameof(notifyNearbyPawns));
    }
  }
}