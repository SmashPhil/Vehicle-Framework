using System.Collections.Generic;
using JetBrains.Annotations;
using SmashTools;
using SmashTools.Animations;
using UnityEngine;
using Verse;

namespace Vehicles;

/// <summary>
/// Draw Properties for multiple UI dialogs and ModSettings
/// </summary>
[PublicAPI]
[HeaderTitle(Label = nameof(VehicleDrawProperties))]
public class VehicleDrawProperties
{
  public Rot8 displayRotation = Rot8.East;

  //Same concept as display coord and size. Fit to settings window
  [TweakField(SettingsType = UISettingsType.FloatBox)]
  public Vector2 displayOffset = Vector2.zero;

  [TweakField(SettingsType = UISettingsType.FloatBox)]
  public Vector2? displayOffsetNorth;

  [TweakField(SettingsType = UISettingsType.FloatBox)]
  public Vector2? displayOffsetEast;

  [TweakField(SettingsType = UISettingsType.FloatBox)]
  public Vector2? displayOffsetSouth;

  [TweakField(SettingsType = UISettingsType.FloatBox)]
  public Vector2? displayOffsetWest;

  public string loadCargoTexPath = string.Empty;
  public string cancelCargoTexPath = string.Empty;

  /// <summary>
  /// GraphicDataOverlays used for instantiating <see cref="overlays"/>
  /// </summary>
  public List<GraphicDataOverlay> graphicOverlays = [];

  public AnimationController controller;

  // TODO 1.7 - this is nonsense, 2 similarly named lists with different purposes. This should be implemented
  // as an auto-property with an appropriate name to make it clear this is cached list of overlay instances.
  [Unsaved]
  public readonly List<GraphicOverlay> overlays = [];

  public void PostDefDatabase(VehicleDef vehicleDef)
  {
    LongEventHandler.ExecuteWhenFinished(delegate
    {
      foreach (GraphicDataOverlay graphicDataOverlay in graphicOverlays)
      {
        GraphicOverlay graphicOverlay = GraphicOverlay.Create(graphicDataOverlay, vehicleDef);
        graphicOverlay.data.graphicData.RecacheLayerOffsets();
        overlays.Add(graphicOverlay);
      }
    });
  }

  public Vector3 DisplayOffsetForRot(Rot4 rot)
  {
    switch (rot.AsInt)
    {
      case 0:
      {
        Vector3? vector = displayOffsetNorth;
        if (vector == null)
        {
          return displayOffset;
        }
        return vector.GetValueOrDefault();
      }
      case 1:
      {
        Vector3? vector = displayOffsetEast;
        if (vector == null)
        {
          return displayOffset;
        }
        return vector.GetValueOrDefault();
      }
      case 2:
      {
        Vector3? vector = displayOffsetSouth;
        if (vector == null)
        {
          return displayOffset;
        }
        return vector.GetValueOrDefault();
      }
      case 3:
      {
        Vector3? vector = displayOffsetWest;
        if (vector == null)
        {
          return displayOffset;
        }
        return vector.GetValueOrDefault();
      }
      default:
        return displayOffset;
    }
  }
}