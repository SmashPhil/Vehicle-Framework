using System.Collections.Generic;
using SmashTools;
using SmashTools.Animations;
using Verse;

namespace Vehicles;

public sealed class GraphicOverlayRenderer
{
  private readonly VehiclePawn vehicle;

  [TweakField]
  [AnimationProperty(Name = "Overlays")]
  private readonly List<GraphicOverlay> overlays = [];

  [TweakField]
  private readonly List<GraphicOverlay> extraOverlays = [];

  private readonly Dictionary<string, List<GraphicOverlay>> extraOverlayLookup = [];

  public GraphicOverlayRenderer(VehiclePawn vehicle)
  {
    this.vehicle = vehicle;
  }

  public List<GraphicOverlay> Overlays => overlays;

  public List<GraphicOverlay> ExtraOverlays => extraOverlays;

  public List<GraphicOverlay> AllOverlaysListForReading { get; } = [];

  private List<GraphicOverlay> RotatorOverlays { get; } = [];

  public void Init()
  {
    if (!vehicle.VehicleDef.drawProperties.overlays.NullOrEmpty())
    {
      overlays.Clear();
      foreach (GraphicDataOverlay graphicDataOverlay in vehicle.VehicleDef.drawProperties
       .graphicOverlays)
      {
        GraphicOverlay graphicOverlay = GraphicOverlay.Create(graphicDataOverlay, vehicle);
        overlays.Add(graphicOverlay);
        AllOverlaysListForReading.Add(graphicOverlay);
        vehicle.DrawTracker.AddRenderer(graphicOverlay);
      }
      RecacheRotatorOverlays();
    }
  }

  private void RecacheRotatorOverlays()
  {
    RotatorOverlays.Clear();
    foreach (GraphicOverlay overlay in AllOverlaysListForReading)
    {
      if (overlay.Graphic is Graphic_Rotator)
        RotatorOverlays.Add(overlay);
    }
  }

  public void AddOverlay(string key, GraphicOverlay graphicOverlay)
  {
    extraOverlayLookup.AddOrAppend(key, graphicOverlay);
    extraOverlays.Add(graphicOverlay);
    AllOverlaysListForReading.Add(graphicOverlay);
    vehicle.DrawTracker.AddRenderer(graphicOverlay);
    RecacheRotatorOverlays();
  }

  public void RemoveOverlays(string key)
  {
    if (extraOverlayLookup.ContainsKey(key))
    {
      foreach (GraphicOverlay graphicOverlay in extraOverlayLookup[key])
      {
        extraOverlays.Remove(graphicOverlay);
        AllOverlaysListForReading.Remove(graphicOverlay);
        vehicle.DrawTracker.RemoveRenderer(graphicOverlay);
        graphicOverlay.Destroy();
      }
      extraOverlayLookup.Remove(key);
      RecacheRotatorOverlays();
    }
  }

  // Right now this is strictly for the old animation system which has applies the same
  // rotation rates for all Graphic_Rotator overlays. The new animator will remove the necessity
  // for all of this and directly write to the transform / acceleration rate of the overlay.
  public void SetAcceleration(float rotation)
  {
    foreach (GraphicOverlay graphicOverlay in RotatorOverlays)
    {
      graphicOverlay.acceleration =
        ((Graphic_Rotator)graphicOverlay.Graphic).ModifyIncomingRotation(rotation);
      graphicOverlay.Transform.rotation =
        (graphicOverlay.Transform.rotation + graphicOverlay.acceleration).ClampAngle();
    }
  }
}