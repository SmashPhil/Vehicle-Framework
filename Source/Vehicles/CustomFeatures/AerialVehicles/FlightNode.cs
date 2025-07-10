using JetBrains.Annotations;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Vehicles.World;

/// <summary>
/// A tile or object on the world map as an individual node along a flight path for an aerial vehicle.
/// </summary>
/// <remarks>
/// Identical to <see cref="GlobalTargetInfo"/> except not multi-purpose and bloated. If I want both the tile
/// and the world object at that tile, I'd be mutating GlobalTargetInfo into something it wasn't designed for.
/// </remarks>
[PublicAPI] // TODO - could probably generalize and move to SmashTools
public struct FlightNode : IExposable
{
  private PlanetTile tile;

  private Vector3 origin;
  private bool spaceObject;

  public PlanetTile Tile => tile;

  public WorldObject WorldObject { get; private set; }

  public FlightNode(PlanetTile tile)
  {
    this.tile = tile;
    WorldObject = WorldHelper.WorldObjectAt(tile);
    origin = WorldHelper.GetTilePos(tile, WorldObject, out spaceObject);
  }

  public FlightNode(GlobalTargetInfo target)
  {
    tile = target.Tile;
    if (target.HasWorldObject)
    {
      WorldObject = target.WorldObject;
      tile = WorldObject.Tile;
    }
    origin = WorldHelper.GetTilePos(tile, WorldObject, out spaceObject);
  }

  public Vector3 GetCenter(AerialVehicleInFlight aerialVehicle)
  {
    if (WorldObject != null && WorldObject != aerialVehicle)
    {
      return WorldObject.DrawPos;
    }
    return origin;
  }

  public void RecalculateCenter()
  {
    if (spaceObject)
    {
      origin = WorldHelper.GetTilePos(tile, WorldObject, out _);
    }
  }

  public static implicit operator GlobalTargetInfo(FlightNode node)
  {
    if (node.WorldObject != null)
      return new GlobalTargetInfo(node.WorldObject);
    return new GlobalTargetInfo(node.Tile);
  }

  public void ExposeData()
  {
    Scribe_Values.Look(ref tile, nameof(tile));
    if (Scribe.mode == LoadSaveMode.LoadingVars)
    {
      WorldObject = WorldHelper.WorldObjectAt(tile);
      origin = WorldHelper.GetTilePos(tile, WorldObject, out spaceObject);
    }
  }
}