using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using Unity.Collections;
using UnityEngine;
using Verse;

namespace SmashTools;

public static class Ext_World
{
  /// <summary>
  /// Fetch all tiles within <paramref name="radius"/> of <paramref name="tile"/>.
  /// For radial search within <paramref name="radius"/>, use <paramref name="result"/>
  /// </summary>
  /// <returns>Coastal tile within <paramref name="radius"/>, otherwise <paramref name="tile"/> if no coastal tile is found.</returns>
  public static PlanetTile Bfs(PlanetTile tile, Action<PlanetTile> processor, int radius = 1,
    Func<PlanetTile, bool> validator = null, Func<PlanetTile, PlanetTile, bool> result = null)
  {
    if (radius < 1)
    {
      Log.Error("Attempting to perform BFS search with max radius < 1");
      return tile;
    }

    Queue<PlanetTile> queue = [];
    queue.Enqueue(tile);

    List<PlanetTile> neighbors = [];
    HashSet<PlanetTile> visitedTiles = [tile];

    int currentRadius = 0;
    // Need to track amount of new neighbor tiles added for radii limits
    int tilesAdded = 0;
    int searchCount = queue.Count;
    while (currentRadius < radius && queue.Count > 0)
    {
      for (int r = 0; r < searchCount; r++)
      {
        PlanetTile currentTile = queue.Dequeue();
        neighbors.Clear();
        Find.WorldGrid.GetTileNeighbors(currentTile, neighbors);
        foreach (PlanetTile neighbor in neighbors)
        {
          if (visitedTiles.Contains(neighbor) || validator != null && !validator(neighbor))
            continue;

          processor(neighbor);
          tilesAdded++;
          if (result != null && result(neighbor, currentRadius))
            return neighbor;

          queue.Enqueue(neighbor);
          visitedTiles.Add(neighbor);
        }
      }

      searchCount = tilesAdded;
      tilesAdded = 0;
      currentRadius++;
    }
    // If BFS executes to full radius, return source tile as searched tiles are the result
    return tile;
  }

  public static IEnumerable<PlanetTile> GetTileNeighbors(PlanetTile tile)
  {
    NativeArray<int> offsets = tile.Layer.UnsafeTileIDToNeighbors_offsets;
    NativeArray<PlanetTile> values = tile.Layer.UnsafeTileIDToNeighbors_values;

    int root = offsets[tile.tileId];
    int count = tile.tileId + 1 < offsets.Length ? offsets[tile.tileId + 1] : values.Length;

    for (int i = root; i < count; i++)
      yield return values[i];
  }

  /// <summary>
  /// Get neighbors of <paramref name="tile"/> on world map.
  /// </summary>
  public static void GetTileNeighbors(PlanetTile tile, List<PlanetTile> tileNeighbors,
    int radius = 1,
    Vector3? nearestTo = null)
  {
    if (radius == 1)
    {
      Find.WorldGrid.GetTileNeighbors(tile, tileNeighbors);
      return;
    }

    tile.Layer.Filler.FloodFill(tile, _ => true,
      delegate (PlanetTile currentTile, int dist)
      {
        // ReSharper disable AccessToModifiedClosure
        if (dist > radius + 1)
          return true;
        if (dist == radius + 1)
          tileNeighbors.Add(currentTile);
        return false;
      });

    WorldGrid worldGrid = Find.WorldGrid;
    Vector3 c = worldGrid.GetTileCenter(tile);
    Vector3 n = c.normalized;
    tileNeighbors.Sort(delegate (PlanetTile a, PlanetTile b)
    {
      float num = Vector3.Dot(n,
        Vector3.Cross(worldGrid.GetTileCenter(a) - c, worldGrid.GetTileCenter(b) - c));
      if (Mathf.Abs(num) < 0.0001f)
      {
        return 0;
      }

      if (num < 0f)
      {
        return -1;
      }

      return 1;
    });
    if (nearestTo.HasValue)
    {
      int closestTile = tileNeighbors.MinBy(t =>
        Vector3.Dot(n, Vector3.Cross(worldGrid.GetTileCenter(t) - c, nearestTo.Value - c)));
      tileNeighbors = tileNeighbors.ReorderOn(closestTile);
    }
  }
}