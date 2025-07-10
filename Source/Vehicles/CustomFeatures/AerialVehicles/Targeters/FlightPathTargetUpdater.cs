using System.Collections.Generic;
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

  public FlightPathTargetUpdater(VehiclePawn vehicle, ILauncher launcher)
  {
    this.launcher = launcher;
    this.vehicle = vehicle;
  }

  protected float TotalDistance { get; private set; }

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
    Material lineMat = LineMaterial;
    foreach (GlobalTargetInfo target in targetData.targets)
    {
      PlanetTile tile = target.Tile;
      Vector3 to = WorldHelper.GetTilePos(tile);
      TotalDistance += Ext_Math.SphericalDistance(from, to);
      FlightPath.DrawPath(from, to, lineMat);
      from = to;
    }

    LaunchProtocol launchProtocol = vehicle.CompVehicleLauncher.launchProtocol;
    if (mouseTarget.IsValid &&
      targetData.targets.Count < launchProtocol.MaxFlightNodes)
    {
      const float FeedbackTexSize = 0.8f;

      TotalDistance += Ext_Math.SphericalDistance(from, mousePos);
      FlightPath.DrawPath(from, mousePos, lineMat);

      WorldRendererUtility.DrawQuadTangentialToPlanet(mousePos,
        FeedbackTexSize * Find.WorldGrid.AverageTileSize, 0.018f,
        WorldMaterials.CurTargetingMat);
    }

    string destLabel = "VF_DoubleClickShuttleTarget".Translate();
    Vector2 labelGetterText = Text.CalcSize(destLabel);
    Rect destPosition = new(mousePos.x, mousePos.y, 32f, 32f);
    Rect rect = new(destPosition.xMax, destPosition.y, 9999f, 100f);
    Rect bgRect = new(rect.x - labelGetterText.x * 0.1f, rect.y, labelGetterText.x * 1.2f,
      labelGetterText.y);
    Graphics.DrawTexture(bgRect, TexUI.GrayTextBG);
  }

  protected virtual ShuttleLaunchStatus LaunchStatus()
  {
    if (vehicle.CompVehicleLauncher.FixedMaxDistance > 0 &&
      TotalDistance > vehicle.CompVehicleLauncher.FixedMaxDistance)
      return ShuttleLaunchStatus.Invalid;
    return ShuttleLaunchStatus.Valid;
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