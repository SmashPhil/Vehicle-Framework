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

  //public VehicleGraphicSet graphics;

  //private Graphic_DynamicShadow shadowGraphic;

  //private PawnFirefoamDrawer firefoamOverlays;

  public VehicleRenderer(VehiclePawn vehicle)
  {
    this.vehicle = vehicle;
    //graphics = new VehicleGraphicSet(vehicle);

    //firefoamOverlays = new PawnFirefoamDrawer(vehicle);
  }

  [Obsolete("Not currently implemented, still WIP. Do not reference.", error: true)]
  public PawnFirefoamDrawer FirefoamOverlays => throw new NotImplementedException();

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

    // TODO - Firefoam overlays for vehicle
    //if (firefoamOverlays.IsCoveredInFoam)
    //{
    //	Vector3 overlayPos = rootLoc;
    //	overlayPos.y += YOffset_CoveredInOverlay;
    //	firefoamOverlays.RenderPawnOverlay(overlayPos, mesh, quaternion, flags.FlagSet(PawnRenderFlags.DrawNow), PawnOverlayDrawer.OverlayLayer.Body, bodyFacing);
    //}

    // TODO - pack graphics
    //if (vehicle.inventory != null && vehicle.inventory.innerContainer.Count > 0 && graphics.packGraphic != null)
    //{
    //	Graphics.DrawMesh(mesh, drawLoc, quaternion, graphics.packGraphic.MatAt(bodyFacing, null), 0);
    //}
  }
}