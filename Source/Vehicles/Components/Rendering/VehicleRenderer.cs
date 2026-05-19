using System;
using RimWorld;
using SmashTools.Rendering;
using UnityEngine;
using Verse;

namespace Vehicles.Rendering;

public sealed class VehicleRenderer : IParallelRenderer
{
  private readonly VehiclePawn vehicle;

  private PreRenderResults results;

  public VehicleRenderer(VehiclePawn vehicle)
  {
    this.vehicle = vehicle;
  }

  bool IParallelRenderer.IsDirty { get; set; }

  public void DynamicDrawPhaseAt(DrawPhase phase, in TransformData transformData,
    bool forceDraw = false)
  {
    switch (phase)
    {
      case DrawPhase.EnsureInitialized:
        // Ensure meshes are cached beforehand
        for (int i = 0; i < 4; i++)
          _ = vehicle.VehicleGraphic.MeshAt(new Rot4(i));
        break;
      case DrawPhase.ParallelPreDraw:
        results = ParallelGetPreRenderResults(in transformData);
        break;
      case DrawPhase.Draw:
        // Out of phase drawing must immediately generate pre-render results for valid data.
        if (!results.valid)
          results = ParallelGetPreRenderResults(in transformData);
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
    return vehicle.VehicleGraphic.ParallelGetPreRenderResults(in transformData,
      forceDraw: forceDraw, thing: vehicle);
  }

  private void Draw()
  {
    Graphics.DrawMesh(results.mesh, results.position, results.quaternion, results.material, 0);
    vehicle.VehicleGraphic.ShadowGraphic?.Draw(results.position, vehicle.FullRotation, vehicle);

    if (vehicle.Spawned && !vehicle.Dead)
      vehicle.vehiclePather.PatherDraw();
  }
}