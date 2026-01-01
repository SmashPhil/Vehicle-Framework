using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Algorithms;
using UnityEngine;
using Verse;

namespace Vehicles.World;

public static class WorldHelper
{
  private static readonly List<Thing> InventoryItems = [];

  private static readonly BFS<PlanetTile> WorldTileBfs = new();
  private static readonly Dictionary<PlanetTile, float> TileWeights = [];
  private static readonly List<PlanetTile> CandidateTiles = [];

  public static List<Thing> AllInventoryItems(AerialVehicleInFlight aerialVehicle)
  {
    // TODO - could use some cleanup either with ClearOnDispose or iterator method
    InventoryItems.Clear();
    foreach (Pawn pawn in aerialVehicle.Vehicle.AllPawnsAboard)
    {
      InventoryItems.AddRange(pawn.inventory.innerContainer);
    }
    InventoryItems.AddRange(aerialVehicle.Vehicle.inventory.innerContainer);
    return InventoryItems;
  }

  public static float RiverCostAt(int tile, VehiclePawn vehicle)
  {
    RiverDef river = Find.WorldGrid[tile].Rivers.MaxBy(r => r.river.widthOnWorld).river;
    return vehicle.VehicleDef.properties.customRiverCosts.TryGetValue(river,
      WorldVehiclePathGrid.ImpassableMovementDifficulty);
  }

  /// <summary>
  /// Biggest river in a tile
  /// </summary>
  /// <param name="list"></param>
  public static SurfaceTile.RiverLink BiggestRiverOnTile(List<SurfaceTile.RiverLink> list)
  {
    return list.MaxBy(riverlink => ModSettingsHelper.RiverSizeWithMultiplier(riverlink.river));
  }

  /// <summary>
  /// Determine if <paramref name="riverDef"/> is large enough to fit vehicle
  /// </summary>
  public static bool VehicleBiggerThanRiver(VehicleDef vehicleDef, RiverDef riverDef)
  {
    if (vehicleDef.properties.customRiverCosts.NullOrEmpty())
    {
      return false;
    }
    //Multiplied by sqrt(2) to account for worst case scenario where river is diagonal
    return ModSettingsHelper.RiverSizeWithMultiplier(riverDef) / 2 < vehicleDef.Size.x;
  }

  /// <summary>
  /// Get Heading between 2 points on World
  /// </summary>
  public static float TryFindHeading(Vector3 source, Vector3 target)
  {
    float heading = Find.WorldGrid.GetHeadingFromTo(source, target);
    return heading;
  }

  public static WorldObject WorldObjectAt(PlanetTile tile)
  {
    foreach (WorldObject worldObject in Find.WorldObjects.AllWorldObjects)
    {
      if (worldObject.Tile == tile)
      {
        return worldObject;
      }
    }
    return null;
  }

  public static (WorldObject sourceObject, WorldObject destObject) WorldObjectsAt(PlanetTile source,
    PlanetTile destination)
  {
    WorldObject sourceObject = null;
    WorldObject destObject = null;
    List<WorldObject> worldObjects = Find.WorldObjects.AllWorldObjects;
    for (int i = 0; i < worldObjects.Count && (sourceObject == null || destObject == null); i++)
    {
      WorldObject worldObject = worldObjects[i];
      if (worldObject.Tile == source)
      {
        sourceObject = worldObject;
      }
      if (worldObject.Tile == destination)
      {
        destObject = worldObject;
      }
    }
    return (sourceObject, destObject);
  }

  // TODO - These helpers can get removed if SOS2 gives their space objects a position on the space layer
  public static Vector3 GetTilePos(PlanetTile tile)
  {
    WorldObject worldObject = WorldObjectAt(tile);
    return GetTilePos(tile, worldObject, out _);
  }

  // TODO - These helpers can get removed if SOS2 gives their space objects a position on the space layer
  public static Vector3 GetTilePos(PlanetTile tile, out bool spaceObject)
  {
    WorldObject worldObject = WorldObjectAt(tile);
    return GetTilePos(tile, worldObject, out spaceObject);
  }

  // TODO - These helpers can get removed if SOS2 gives their space objects a position on the space layer
  public static Vector3 GetTilePos(PlanetTile tile, WorldObject worldObject, out bool spaceObject)
  {
    spaceObject = false;
    if (!tile.Valid)
      return Vector3.zero;

    Vector3 pos = Find.WorldGrid.GetTileCenter(tile);
    if (worldObject != null && worldObject.def.HasModExtension<SpaceObjectDefModExtension>())
    {
      spaceObject = true;
      pos = worldObject.DrawPos;
    }
    return pos;
  }

  public static float GetTileDistance(PlanetTile source, PlanetTile destination)
  {
    (WorldObject sourceObject, WorldObject destObject) = WorldObjectsAt(source, destination);

    Vector3 sourcePos = GetTilePos(source, sourceObject, out _);
    Vector3 destPos = GetTilePos(destination, destObject, out _);

    return Ext_Math.SphericalDistance(sourcePos, destPos);
  }

  /// <summary>
  /// Find best tile to snap to when ordering a caravan
  /// </summary>
  /// <param name="caravan"></param>
  /// <param name="tile"></param>
  public static PlanetTile BestGotoDestForVehicle(VehicleCaravan caravan, PlanetTile tile)
  {
    if (CaravanReachable(tile))
      return tile;

    GenWorldClosest.TryFindClosestTile(tile, CaravanReachable, out PlanetTile result, 50);
    return result;

    bool CaravanReachable(PlanetTile planetTile)
    {
      return caravan.UniqueVehicleDefsInCaravan()
         .All(vehicleDef => WorldVehiclePathGrid.Instance.Passable(planetTile, vehicleDef)) &&
        WorldVehiclePathGrid.Instance.reachability.CanReach(caravan, planetTile);
    }
  }

  /// <summary>
  /// Find best negotiator in VehicleCaravan for trading on the World Map
  /// </summary>
  /// <param name="vehicle"></param>
  /// <param name="faction"></param>
  /// <param name="trader"></param>
  public static Pawn FindBestNegotiator(this VehiclePawn vehicle, Faction faction = null,
    TraderKindDef trader = null)
  {
    Predicate<Pawn> pawnValidator = null;
    if (faction != null)
    {
      pawnValidator = delegate(Pawn p)
      {
        AcceptanceReport report = p.CanTradeWith(faction, trader);
        return report.Accepted;
      };
    }
    return vehicle.FindPawnWithBestStat(StatDefOf.TradePriceImprovement, pawnValidator);
  }

  /// <summary>
  /// Find pawn with best <param name="stat"> value.</param>
  /// </summary>
  /// <param name="vehicle"></param>
  /// <param name="stat"></param>
  /// <param name="pawnValidator"></param>
  public static Pawn FindPawnWithBestStat(this VehiclePawn vehicle, StatDef stat,
    Predicate<Pawn> pawnValidator)
  {
    Pawn bestPawn = null;
    float curValue = -1f;
    foreach (Pawn pawn in vehicle.AllPawnsAboard)
    {
      if (!pawn.Dead && !pawn.Downed && !pawn.InMentalState &&
        CaravanUtility.IsOwner(pawn, vehicle.Faction) && !stat.Worker.IsDisabledFor(pawn) &&
        (pawnValidator is null || pawnValidator(pawn)))
      {
        float statValue = pawn.GetStatValue(stat);
        if (bestPawn == null || statValue > curValue)
        {
          bestPawn = pawn;
          curValue = statValue;
        }
      }
    }

    return bestPawn;
  }

  /// <summary>
  /// Find best negotiator in Vehicle for trading on the World Map
  /// </summary>
  public static Pawn FindBestNegotiator(VehicleCaravan caravan, Faction faction = null,
    TraderKindDef trader = null)
  {
    Predicate<Pawn> pawnValidator = null;
    if (faction != null)
    {
      pawnValidator = delegate(Pawn p)
      {
        AcceptanceReport report = p.CanTradeWith(faction, trader);
        return report.Accepted;
      };
    }
    return BestCaravanPawnUtility.FindPawnWithBestStat(caravan, StatDefOf.TradePriceImprovement,
      pawnValidator: pawnValidator);
  }

  /// <summary>
  /// Get nearest tile id to <paramref name="worldCoord"/>
  /// </summary>
  /// <param name="worldCoord"></param>
  public static int GetNearestTile(Vector3 worldCoord)
  {
    for (int tile = 0; tile < Find.WorldGrid.TilesCount; tile++)
    {
      Vector3 pos = Find.WorldGrid.GetTileCenter(tile);
      if (Ext_Math.SphericalDistance(worldCoord, pos) <= 0.75f)
      {
        return tile;
      }
    }
    return -1;
  }

  /// <summary>
  /// Adjust <paramref name="tile"/> by weight if within radius of a river or coastline.
  /// </summary>
  /// <returns>new tileID if a nearby feature is found or <paramref name="tile"/> if not found</returns>
  public static PlanetTile AdjustSettlement(PlanetTile tile)
  {
    if (tile.LayerDef.isSpace)
      return tile;
    if (tile.Tile.IsCoastal)
      return tile;
    if (tile.Tile.OnSurface &&
      tile.Tile is SurfaceTile { Roads.Count: > 0 } ||
      tile.Tile is SurfaceTile { Rivers.Count: > 0 })
      return tile;

    using ClearOnDispose<PlanetTile> cod = new(CandidateTiles);
    Ext_World.Bfs(tile, ProcessTile, VehicleMod.settings.main.adjustSettlementRadius, CanEnter);

    if (CandidateTiles.NullOrEmpty())
      return tile;
    PlanetTile result = CandidateTiles.RandomElementByWeightWithFallback(GetTileWeight, fallback: tile);

    if (DebugProperties.Debug)
      DebugHelper.DebugAddSettlementOrigin(tile, result);

    return result;

    static float GetTileWeight(PlanetTile currentTile)
    {
      return TileWeights.TryGetValue(currentTile);
    }

    static bool CanEnter(PlanetTile currentTile)
    {
      if (!Find.WorldGrid[currentTile].PrimaryBiome.canBuildBase)
        return false;
      if (!Find.WorldGrid[currentTile].PrimaryBiome.implemented)
        return false;
      if (Find.WorldGrid[currentTile].hilliness == Hilliness.Impassable)
        return false;
      if (Find.WorldObjects.AnySettlementBaseAtOrAdjacent(currentTile, out _))
        return false;
      return SettleInEmptyTileUtility.CanCreateMapAt(currentTile);
    }

    static void ProcessTile(PlanetTile currentTile)
    {
      CandidateTiles.Add(currentTile);

      // Only process tiles once, there may be some overlap with larger radii settings
      if (TileWeights.ContainsKey(currentTile))
        return;
      float weight = TileWeightAt(currentTile);
      TileWeights[currentTile] = weight;
    }

    static float TileWeightAt(PlanetTile currentTile)
    {
      float weight = 0;
      if (currentTile.Tile.IsCoastal)
        weight += VehicleMod.settings.main.adjustCoastWeight;

      if (currentTile.Tile.OnSurface && currentTile.Tile is SurfaceTile surfaceTile)
      {
        if (!surfaceTile.Rivers.NullOrEmpty())
          weight += surfaceTile.Rivers.Count * VehicleMod.settings.main.adjustRiverWeight;
      }
      return weight;
    }
  }

  /// <summary>
  /// Convert <paramref name="pos"/> to matrix in World space
  /// </summary>
  public static Matrix4x4 GetWorldQuadAt(Vector3 pos, float size, float altOffset,
    bool counterClockwise = false)
  {
    Vector3 normalized = pos.normalized;
    Vector3 vector;
    if (counterClockwise)
    {
      vector = -normalized;
    }
    else
    {
      vector = normalized;
    }
    Quaternion q = Quaternion.LookRotation(Vector3.Cross(vector, Vector3.up), vector);
    Vector3 s = new(size, 1f, size);
    Matrix4x4 matrix = default;
    matrix.SetTRS(pos + normalized * altOffset, q, s);
    return matrix;
  }

  /// <summary>
  /// Alternative to <see cref="WorldRendererUtility.DrawQuadTangentialToPlanet"/> that rotates by -90 degrees for vehicle icons
  /// </summary>
  public static void DrawQuadTangentialToPlanet(Vector3 pos, float size, float altOffset,
    Material material, bool counterClockwise = false, bool useSkyboxLayer = false,
    MaterialPropertyBlock propertyBlock = null)
  {
    if (material == null)
    {
      Log.Warning("Tried to draw quad with null material.");
      return;
    }
    Vector3 normalized = pos.normalized;
    Vector3 vector;

    if (counterClockwise)
    {
      vector = -normalized;
    }
    else
    {
      vector = normalized;
    }
    Quaternion q = Quaternion.LookRotation(Vector3.Cross(vector, Vector3.up), vector) *
      Quaternion.Euler(0, -90f, 0);
    Vector3 s = new(size, 1f, size);
    Matrix4x4 matrix = default;
    matrix.SetTRS(pos + normalized * altOffset, q, s);
    int layer = useSkyboxLayer ?
      WorldCameraManager.WorldSkyboxLayer :
      WorldCameraManager.WorldLayer;
    if (propertyBlock != null)
    {
      Graphics.DrawMesh(MeshPool.plane10, matrix, material, layer, null, 0, propertyBlock);
      return;
    }
    Graphics.DrawMesh(MeshPool.plane10, matrix, material, layer);
  }
}