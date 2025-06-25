using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class GraphicDataLayered : GraphicData
{
  public const int SubLayerCount = 10;

  /// <summary>
  /// Conversion factor for layering a sprite relative to the vehicle's body position.
  /// Layer is relative to SubLayerCount, eg. layer=1 will offset the graphic 1/10th of
  /// an altitude layer toward the camera.
  /// </summary>
  private int layer;

  /// <summary>
  /// Depth offset relative to AltitudeLayer.
  /// </summary>
  /// <remarks>
  /// Only applies when Thing this graphic applies to is spawned.
  /// </remarks>
  public AltitudeLayer? altLayerSpawned;

  private Vector3 originalDrawOffset;
  private Vector3? originalDrawOffsetNorth;
  private Vector3? originalDrawOffsetEast;
  private Vector3? originalDrawOffsetSouth;
  private Vector3? originalDrawOffsetWest;

  public virtual void CopyFrom(GraphicDataLayered graphicData)
  {
    base.CopyFrom(graphicData);
    layer = graphicData.layer;
    CacheDrawOffsets();
    RecacheLayerOffsets();
  }

  public void PostLoad()
  {
    CacheDrawOffsets();
  }

  public virtual void Init(IMaterialCacheTarget target)
  {
    RecacheLayerOffsets();
  }

  private void CacheDrawOffsets()
  {
    originalDrawOffset = drawOffset;
    originalDrawOffsetNorth = drawOffsetNorth;
    originalDrawOffsetEast = drawOffsetEast;
    originalDrawOffsetSouth = drawOffsetSouth;
    originalDrawOffsetWest = drawOffsetWest;
  }

  public void RecacheLayerOffsets()
  {
    if (layer == 0)
      return;

    float layerOffset = layer * (Altitudes.AltInc / SubLayerCount);

    drawOffset = originalDrawOffset;
    drawOffset.y += layerOffset;

    if (drawOffsetNorth != null)
    {
      Assert.IsTrue(originalDrawOffsetNorth.HasValue);
      drawOffsetNorth = originalDrawOffsetNorth.Value;
      drawOffsetNorth = new Vector3(drawOffsetNorth.Value.x, drawOffsetNorth.Value.y + layerOffset,
        drawOffsetNorth.Value.z);
    }

    if (drawOffsetEast != null)
    {
      Assert.IsTrue(originalDrawOffsetEast.HasValue);
      drawOffsetEast = originalDrawOffsetEast.Value;
      drawOffsetEast = new Vector3(drawOffsetEast.Value.x, drawOffsetEast.Value.y + layerOffset,
        drawOffsetEast.Value.z);
    }

    if (drawOffsetSouth != null)
    {
      Assert.IsTrue(originalDrawOffsetSouth.HasValue);
      drawOffsetSouth = originalDrawOffsetSouth.Value;
      drawOffsetSouth = new Vector3(drawOffsetSouth.Value.x, drawOffsetSouth.Value.y + layerOffset,
        drawOffsetSouth.Value.z);
    }

    if (drawOffsetWest != null)
    {
      Assert.IsTrue(originalDrawOffsetWest.HasValue);
      drawOffsetWest = originalDrawOffsetWest.Value;
      drawOffsetWest = new Vector3(drawOffsetWest.Value.x, drawOffsetWest.Value.y + layerOffset,
        drawOffsetWest.Value.z);
    }
  }
}