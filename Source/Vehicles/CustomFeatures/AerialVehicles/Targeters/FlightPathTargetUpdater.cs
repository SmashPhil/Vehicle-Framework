using System.Collections.Generic;
using CoreLib;
using CoreLib.Collections;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Targeting;
using UnityEngine;
using Verse;

namespace Vehicles.World;

public class FlightPathTargetUpdater : ITargeterUpdate<GlobalTargetInfo>
{
  protected readonly ILauncher launcher;
  protected readonly VehiclePawn vehicle;

  private readonly List<LinePath> pathPoints = [];

  private struct LinePath
  {
    public required Vector3 from;
    public required Vector3 to;
  }

  public FlightPathTargetUpdater(VehiclePawn vehicle, ILauncher launcher)
  {
    this.launcher = launcher;
    this.vehicle = vehicle;
  }

  protected float TotalDistance { get; private set; }

  private bool BeyondMaxDistance => TotalDistance > vehicle.CompVehicleLauncher.MaxLaunchDistance;

  private Material LineMaterial
  {
    get
    {
      return LaunchStatus() switch
      {
        ShuttleLaunchStatus.Valid        => TexData.WorldLineMatWhite,
        ShuttleLaunchStatus.NoReturnTrip => TexData.WorldLineMatYellow,
        ShuttleLaunchStatus.Invalid      => TexData.WorldLineMatRed,
        _                                => throw new System.NotImplementedException(nameof(ShuttleLaunchStatus))
      };
    }
  }

  public virtual void TargeterOnGUI()
  {
  }

  public virtual void TargeterUpdate(ref readonly TargetData<GlobalTargetInfo> targetData)
  {
    TotalDistance = 0;
    GlobalTargetInfo mouseTarget = CurrentTargetUnderMouse();
    Vector3 mousePos = WorldHelper.GetTilePos(mouseTarget.Tile);
    Vector3 from = launcher.Origin;

    bool mouseTargetIsValid = mouseTarget.IsValid &&
                              targetData.targets.Count < vehicle.CompVehicleLauncher.launchProtocol.MaxFlightNodes;

    using ClearOnDispose<LinePath> cod = new(pathPoints);

    // Calculate total distance for status checks
    foreach (GlobalTargetInfo target in targetData.targets)
    {
      PlanetTile tile = target.Tile;
      Vector3 to = WorldHelper.GetTilePos(tile);
      TotalDistance += Ext_Math.SphericalDistance(from, to);
      pathPoints.Add(new LinePath { from = from, to = to });
      from = to;
    }
    if (mouseTargetIsValid)
    {
      TotalDistance += Ext_Math.SphericalDistance(from, mousePos);
    }

    // Draw path after all status checks are ready
    Material lineMat = LineMaterial;
    foreach (LinePath path in pathPoints)
    {
      FlightPath.DrawPath(path.from, path.to, lineMat);
    }

    if (mouseTargetIsValid)
    {
      FlightPath.DrawPath(from, mousePos, lineMat);

      const float FeedbackTexSize = 0.8f;
      WorldRendererUtility.DrawQuadTangentialToPlanet(mousePos,
        FeedbackTexSize * Find.WorldGrid.AverageTileSize, 0.018f,
        WorldMaterials.CurTargetingMat);
    }

    const float CursorSize = 32;
    string destLabel = "VF_DoubleClickShuttleTarget".Translate();
    Vector2 labelGetterText = Text.CalcSize(destLabel);
    Rect destPosition = new(mousePos.x, mousePos.y, CursorSize, CursorSize);
    Rect rect = new(destPosition.xMax, destPosition.y, width: 9999f, height: 100f);
    Rect bgRect = new(rect.x - labelGetterText.x * 0.1f, rect.y, labelGetterText.x * 1.2f,
      labelGetterText.y);
    Graphics.DrawTexture(bgRect, TexUI.GrayTextBG);
  }

  public virtual TargetValidation ValidateTargets(ReadOnlyList<GlobalTargetInfo> targets)
  {
    if (BeyondMaxDistance)
    {
      return TargetValidation.Failed with
      {
        Tooltip = "TransportPodDestinationBeyondMaximumRange".Translate()
      };
    }
    return TargetValidation.Success;
  }

  protected virtual ShuttleLaunchStatus LaunchStatus()
  {
    return BeyondMaxDistance ? ShuttleLaunchStatus.Invalid : ShuttleLaunchStatus.Valid;
  }

  protected static GlobalTargetInfo CurrentTargetUnderMouse()
  {
    List<WorldObject> list = GenWorldUI.WorldObjectsUnderMouse(UI.MousePositionOnUI);
    if (!list.NullOrEmpty())
      return list[0];

    PlanetTile tile = GenWorld.MouseTile();
    return tile.Valid ? new GlobalTargetInfo(tile) : GlobalTargetInfo.Invalid;
  }
}