using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SmashTools;
using SmashTools.Animations;
using SmashTools.Rendering;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.Rendering;

[PublicAPI]
public class VehicleDrawTracker
{
  private readonly VehiclePawn vehicle;

  public readonly VehicleRenderer renderer;

  [AnimationProperty, TweakField]
  public GraphicOverlayRenderer overlayRenderer;

  public readonly VehicleTweener tweener;

  // TODO - Reimplement for vehicle specific "footprints"
  public VehicleTrackMaker trackMaker;
  public Vehicle_RecoilTracker recoilTracker;

  private readonly List<IParallelRenderer> parallelRenderers = [];

  public VehicleDrawTracker(VehiclePawn vehicle)
  {
    this.vehicle = vehicle;
    tweener = new VehicleTweener(vehicle);
    renderer = new VehicleRenderer(vehicle);
    overlayRenderer = new GraphicOverlayRenderer(vehicle);
    trackMaker = new VehicleTrackMaker(vehicle);
    recoilTracker = new Vehicle_RecoilTracker();

    AddRenderer(renderer);
  }

  private bool RenderersInitialized { get; set; }

  internal IReadOnlyList<IParallelRenderer> ParallelRenderers => parallelRenderers;

  public Vector3 DrawPos
  {
    get
    {
      tweener.PreDrawPosCalculation();
      Vector3 vector = tweener.TweenedPos;
      vector.y = vehicle.def.Altitude;

      if (recoilTracker.Recoil > 0f)
      {
        vector = vector.PointFromAngle(recoilTracker.Recoil, recoilTracker.Angle);
      }
      return vector;
    }
  }

  public void AddRenderer(IParallelRenderer parallelRenderer)
  {
    Assert.IsFalse(parallelRenderers.Contains(parallelRenderer));
    parallelRenderer.SetDirty();
    parallelRenderers.Add(parallelRenderer);
  }

  public void RemoveRenderer(IParallelRenderer parallelRenderer)
  {
    parallelRenderers.Remove(parallelRenderer);
  }

  public void DynamicDrawPhaseAt(DrawPhase phase, in Vector3 drawLoc, Rot8 rot, float rotation)
  {
    TransformData transformData = new(drawLoc, rot, rotation);
    foreach (IParallelRenderer parallelRenderer in parallelRenderers)
    {
      switch (phase)
      {
        case DrawPhase.EnsureInitialized:
          // Only initialize on request
          if (parallelRenderer.IsDirty)
          {
            parallelRenderer.DynamicDrawPhaseAt(phase, in transformData);
            parallelRenderer.IsDirty = false;
          }
          break;
        case DrawPhase.ParallelPreDraw:
        case DrawPhase.Draw:
          parallelRenderer.DynamicDrawPhaseAt(phase, in transformData);
          break;
        default:
          throw new NotImplementedException(nameof(DrawPhase));
      }
    }
  }

  public void ProcessPostTickVisuals(int ticksPassed)
  {
    if (!vehicle.Spawned)
      return;
    trackMaker.ProcessPostTickVisuals(ticksPassed);
    recoilTracker.ProcessPostTickVisuals(ticksPassed);
  }

  public void Notify_Spawned()
  {
    tweener.ResetTweenedPosToRoot();
  }
}