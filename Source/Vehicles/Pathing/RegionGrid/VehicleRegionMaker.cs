using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LudeonTK;
using SmashTools;
using SmashTools.Algorithms;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Dijkstra = SmashTools.Algorithms.Dijkstra<int>;

namespace Vehicles;

[StaticConstructorOnStartup]
public class VehicleRegionMaker : VehicleGridManager
{
  private VehicleRegionGrid regionGrid;

  // Only accessed within the same thread.
  private readonly HashSet<IntVec3> regionCells = [];

  // 4 Hashsets, 1 for each cardinal direction
  private readonly HashSet<IntVec3>[] linksProcessedAt = [[], [], [], []];

  internal readonly ObjectPool<VehicleRegionLink> linkPool;
  internal readonly ObjectPool<VehicleRegion> regionPool;

  private readonly ConcurrentDictionary<ulong, VehicleRegionLink> activeLinks = [];

  private readonly BFS<IntVec3> floodfiller = new();

  private int nextId = 1;

  public VehicleRegionMaker(VehiclePathingSystem mapping, VehicleDef createdFor) : base(mapping,
    createdFor)
  {
    const float PoolSize = 0.5f; // Create pool for 50% of average regions / links count

    float totalRegions = ((float)mapping.map.Size.x / VehicleRegion.ChunkSize) *
      ((float)mapping.map.Size.z / VehicleRegion.ChunkSize);
    int regions = Mathf.CeilToInt(totalRegions * PoolSize);
    int links = Mathf.CeilToInt(regions * 4); // 4 cardinal directions are typical
    regionPool = new ObjectPool<VehicleRegion>(regions);
    linkPool = new ObjectPool<VehicleRegionLink>(links);
  }

  private bool CreatingRegions { get; set; }

  public override void PostInit()
  {
    base.PostInit();
    regionGrid = mapping[createdFor].VehicleRegionGrid;
  }

  /// <summary>
  /// Generate region at <paramref name="root"/>
  /// </summary>
  public RegionResult TryGenerateRegionFrom(IntVec3 root, ref VehicleRegion region)
  {
    RegionType expectedRegionType =
      VehicleRegionTypeUtility.GetExpectedRegionType(root, mapping, createdFor);

    if (expectedRegionType == RegionType.None) return RegionResult.NoRegion;

    if (CreatingRegions)
    {
      Log.Error(
        "Trying to generate a new region while already in the process. Nested calls not allowed.");
      return RegionResult.Failed;
    }

    CreatingRegions = true;
    regionCells.Clear();

    region = GetRegion(root);
    try
    {
      region.type = expectedRegionType;

      FloodFillAndAddCells(region, root);
      CreateLinks(region);
    }
    catch (Exception ex)
    {
      SmashLog.ErrorLabel(VehicleHarmony.LogLabel,
        $"Exception thrown while generating region at {root}. Exception={ex}");
      region = null;
      return RegionResult.Failed;
    }
    finally
    {
      CreatingRegions = false;
      regionCells.Clear();
    }

    return RegionResult.Success;
  }

  private static IEnumerable<IntVec3> GetFloodFillNeighbors(IntVec3 root)
  {
    foreach (IntVec3 cardinalCellOffset in GenAdj.CardinalDirectionsAround)
    {
      yield return root + cardinalCellOffset;
    }
  }

  /// <summary>
  /// Floodfill from <paramref name="root"/> and calculate valid neighboring cells to form a new region
  /// </summary>
  private void FloodFillAndAddCells(VehicleRegion region, IntVec3 root)
  {
    regionCells.Clear();
    floodfiller.FloodFill(root, GetFloodFillNeighbors, canEnter: Validator, processor: Processor);
    return;

    bool Validator(IntVec3 cell)
    {
      if (!cell.InBounds(mapping.map))
      {
        return false;
      }

      if (!region.extentsLimit.Contains(cell))
      {
        return false;
      }

      return VehicleRegionTypeUtility.GetExpectedRegionType(cell, mapping, createdFor) ==
        region.type;
    }

    void Processor(IntVec3 cell)
    {
      AddCell(region, cell);
    }
  }

  /// <summary>
  /// Add cell to region currently being created
  /// </summary>
  private void AddCell(VehicleRegion region, IntVec3 cell)
  {
    regionGrid.SetRegionAt(cell, region);
    regionCells.Add(cell);
    if (region.extentsClose.minX > cell.x)
    {
      region.extentsClose.minX = cell.x;
    }

    if (region.extentsClose.maxX < cell.x)
    {
      region.extentsClose.maxX = cell.x;
    }

    if (region.extentsClose.minZ > cell.z)
    {
      region.extentsClose.minZ = cell.z;
    }

    if (region.extentsClose.maxZ < cell.z)
    {
      region.extentsClose.maxZ = cell.z;
    }

    if (cell.x == createdFor.SizePadding ||
      cell.x == mapping.map.Size.x - 1 - createdFor.SizePadding ||
      cell.z == createdFor.SizePadding ||
      cell.z == mapping.map.Size.z - 1 - createdFor.SizePadding)
    {
      region.touchesMapEdge = true;
    }
  }

  private void ClearProcessedLinks()
  {
    foreach (HashSet<IntVec3> linksProcessed in linksProcessedAt)
    {
      linksProcessed.Clear();
    }
  }

  /// <summary>
  /// Region link between <paramref name="span"/>
  /// </summary>
  /// <param name="span"></param>
  private VehicleRegionLink LinkFrom(EdgeSpan span)
  {
    ulong key = span.UniqueHashCode();
    if (!activeLinks.TryGetValue(key, out VehicleRegionLink regionLink))
    {
      regionLink = linkPool.Get();
      regionLink.SetNew(span);
      activeLinks.TryAdd(key, regionLink);
    }
    return regionLink;
  }

  /// <summary>
  /// Generate region links for region currently being created
  /// </summary>
  private void CreateLinks(VehicleRegion region)
  {
    Assert.IsTrue(linksProcessedAt.All(set => set.Count == 0));
    foreach (IntVec3 cell in regionCells)
    {
      SweepInTwoDirectionsAndTryToCreateLink(region, Rot4.North, cell);
      SweepInTwoDirectionsAndTryToCreateLink(region, Rot4.South, cell);
      SweepInTwoDirectionsAndTryToCreateLink(region, Rot4.East, cell);
      SweepInTwoDirectionsAndTryToCreateLink(region, Rot4.West, cell);
    }
    ClearProcessedLinks();
  }

  /// <summary>
  /// Try to make region link with neighboring rotations as fallback
  /// </summary>
  private void SweepInTwoDirectionsAndTryToCreateLink(VehicleRegion region,
    Rot4 potentialOtherRegionDir, IntVec3 cell)
  {
    if (!potentialOtherRegionDir.IsValid)
      return;

    HashSet<IntVec3> linksProcessed = linksProcessedAt[potentialOtherRegionDir.AsInt];
    if (linksProcessed.Contains(cell))
      return;

    IntVec3 facingCell = cell + potentialOtherRegionDir.FacingCell;
    if (facingCell.InBounds(mapping.map) && regionGrid.GetRegionAt(facingCell) == region)
      return;

    RegionType expectedRegionType =
      VehicleRegionTypeUtility.GetExpectedRegionType(facingCell, mapping, createdFor);
    if (expectedRegionType == RegionType.None)
      return;

    Rot4 rotClockwise = potentialOtherRegionDir.Rotated(RotationDirection.Clockwise);
    linksProcessed.Add(cell);

    int spanRight = 0;
    int spanUp = 0;

    if (!expectedRegionType.IsOneCellRegion())
    {
      for (spanRight = 0; spanRight <= VehicleRegion.ChunkSize; spanRight++)
      {
        IntVec3 sweepRight = cell + rotClockwise.FacingCell * (spanRight + 1);
        if (InvalidForLinking(region, sweepRight, potentialOtherRegionDir, expectedRegionType))
          break;
        if (!linksProcessed.Add(sweepRight))
          Log.Error("Attempting to process the same cell twice.");
      }

      for (spanUp = 0; spanUp <= VehicleRegion.ChunkSize; spanUp++)
      {
        IntVec3 sweepUp = cell - rotClockwise.FacingCell * (spanUp + 1);
        if (InvalidForLinking(region, sweepUp, potentialOtherRegionDir, expectedRegionType))
          break;
        if (!linksProcessed.Add(sweepUp))
          Log.Error("Attempting to process the same cell twice.");
      }
    }

    int length = spanRight + spanUp + 1;
    SpanDirection dir;
    IntVec3 root;
    if (potentialOtherRegionDir == Rot4.North)
    {
      dir = SpanDirection.East;
      root = cell - rotClockwise.FacingCell * spanUp;
      root.z++;
    }
    else if (potentialOtherRegionDir == Rot4.South)
    {
      dir = SpanDirection.East;
      root = cell + rotClockwise.FacingCell * spanRight;
    }
    else if (potentialOtherRegionDir == Rot4.East)
    {
      dir = SpanDirection.North;
      root = cell + rotClockwise.FacingCell * spanRight;
      root.x++;
    }
    else
    {
      dir = SpanDirection.North;
      root = cell - rotClockwise.FacingCell * spanUp;
    }

    EdgeSpan span = new(root, dir, length);
    VehicleRegionLink regionLink = LinkFrom(span);
    regionLink.Register(region, potentialOtherRegionDir);
    region.AddLink(regionLink);
  }

  public void Return(VehicleRegion region)
  {
    regionPool.Return(region);
  }

  public void Return(VehicleRegionLink regionLink)
  {
    activeLinks.TryRemove(regionLink.UniqueHashCode(), out _);
    linkPool.Return(regionLink);
  }

  private VehicleRegion GetRegion(IntVec3 root)
  {
    VehicleRegion region = regionGrid.GetRegionAt(root);
    if (region != null)
    {
      // Clear existing region and reuse it. Reset will be called after
      // region is forcibly removed from grid to maintain safe behavior.
      regionGrid.ClearFromGrid(region);
      SetNew(region, root);
      return region;
    }

    region = CreateNew(root);
    return region;
  }

  private VehicleRegion CreateNew(IntVec3 root)
  {
    VehicleRegion region = regionPool.Get();
    SetNew(region, root);
    return region;
  }

  private void SetNew(VehicleRegion region, IntVec3 root)
  {
    if (region == null)
    {
      Log.Warning("Attempting to populate null region.");
      return;
    }

    int id = GetRegionId();
    region.Init(createdFor, id);
    region.Map = mapping.map;
    region.extentsClose = new CellRect()
    {
      minX = root.x,
      maxX = root.x,
      minZ = root.z,
      maxZ = root.z
    };
    region.extentsLimit = VehicleRegion.ChunkAt(root).ClipInsideMap(mapping.map);
  }

  private bool InvalidForLinking(VehicleRegion region, IntVec3 cell, Rot4 rot,
    RegionType expectedRegionType)
  {
    //Not in bounds || Region at cell != this || Region Type != expected
    return !cell.InBounds(mapping.map) || regionGrid.GetRegionAt(cell) != region ||
      VehicleRegionTypeUtility.GetExpectedRegionType(cell + rot.FacingCell, mapping,
        createdFor) != expectedRegionType;
  }

  private int GetRegionId()
  {
    return Interlocked.Increment(ref nextId);
  }

  [DebugAction(VehicleHarmony.VehiclesLabel, allowedGameStates = AllowedGameStates.PlayingOnMap,
    hideInSubMenu = true)]
  private static List<DebugActionNode> ForceRegenerateRegion()
  {
    List<DebugActionNode> debugActions = [];
    if (!VehicleHarmony.AllMoveableVehicleDefs.NullOrEmpty())
    {
      foreach (VehicleDef vehicleDef in VehicleHarmony.AllMoveableVehicleDefs)
      {
        debugActions.Add(new DebugActionNode(vehicleDef.defName, DebugActionType.ToolMap)
        {
          action = delegate
          {
            Map map = Find.CurrentMap;
            if (map == null)
            {
              Log.Error("Attempting to use DebugRegionOptions with null map.");
              return;
            }

            DebugHelper.Local.VehicleDef = vehicleDef;
            DebugHelper.Local.DebugType = DebugRegionType.Regions | DebugRegionType.Links;

            IntVec3 cell = UI.MouseCell();
            map.GetCachedMapComponent<VehiclePathingSystem>()[vehicleDef].VehicleRegionDirtyer
             .NotifyWalkabilityChanged(cell);
          }
        });
      }
    }

    return debugActions;
  }

  public enum RegionResult
  {
    Failed,
    NoRegion,
    Success
  }
}