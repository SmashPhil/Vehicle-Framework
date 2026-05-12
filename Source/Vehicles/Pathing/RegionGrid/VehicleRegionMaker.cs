using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using CoreLib;
using CoreLib.Performance;
using LudeonTK;
using SmashTools;
using SmashTools.Collections;
using SmashTools.Performance;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

[StaticConstructorOnStartup]
public class VehicleRegionMaker : VehicleGridManager
{
  private VehicleRegionGridManager regionGridManager;

  // Only accessed within the same thread.
  private readonly List<IntVec3> regionCells = [];

  private readonly ObjectPool<VehicleRegion> regionPool;
  private readonly ObjectPool<HashSet<IntVec3>[]> linkProcessors = new(size: 5, factory: () => [[], [], [], []]);

  internal readonly VehicleRegionLinkDatabase linkDatabase;

  private readonly FloodFiller floodFiller;
  private int nextId = 1;

  public VehicleRegionMaker(IPathingManager pathing, VehicleDef createdFor) : base(pathing,
    createdFor)
  {
    const float PoolSize = 0.5f; // Create pool for 50% of average regions / links count

    int gridCount = VehicleRegionGridManager.AllGridTypes.Length;
    float totalRegions = ((float)pathing.Map.Size.x / VehicleRegion.ChunkSize) *
      ((float)pathing.Map.Size.z / VehicleRegion.ChunkSize) * gridCount;
    int regions = Mathf.CeilToInt(totalRegions * PoolSize);
    int links = Mathf.CeilToInt(regions * 4); // 4 cardinal directions are typical
    regionPool = new ObjectPool<VehicleRegion>(regions);
    linkDatabase = new VehicleRegionLinkDatabase(links);
    floodFiller = new FloodFiller(this);
  }

  private bool CreatingRegions { get; set; }

  public override void PostInit()
  {
    base.PostInit();
    regionGridManager = pathing.GetRegionGridManager(createdFor);
  }

  /// <summary>
  /// Generate region at <paramref name="root"/>
  /// </summary>
  [Profile]
  public RegionResult TryGenerateRegionFrom(IntVec3 root, RegionGridType gridType, ref VehicleRegion region)
  {
    VehicleRegionGrid regionGrid = regionGridManager[gridType];
    RegionType expectedRegionType = regionGrid.Source.ExpectedRegionType(root, pathing, createdFor);

    if (expectedRegionType == RegionType.None)
      return RegionResult.NoRegion;

    if (CreatingRegions)
    {
      Log.Error(
        "Trying to generate a new region while already in the process. Nested calls not allowed.");
      return RegionResult.Failed;
    }

    using ClearOnDispose<IntVec3> cod = new(regionCells);
    try
    {
      CreatingRegions = true;
      region = GetRegion(root, gridType);
      region.type = expectedRegionType;

      floodFiller.FloodFill(root, region, CancellationToken.None);
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
    }
    return RegionResult.Success;
  }

  /// <summary>
  /// Generate region links for region currently being created
  /// </summary>
  [Profile]
  private void CreateLinks(VehicleRegion region)
  {
    using var lr = linkProcessors.GetTemporary(out HashSet<IntVec3>[] linksProcessedAt);
    Assert.IsTrue(linksProcessedAt.All(static set => set.Count == 0));
    foreach (IntVec3 cell in regionCells)
    {
      SweepInTwoDirectionsAndTryToCreateLink(region, Rot4.North, cell, linksProcessedAt[Rot4.NorthInt]);
      SweepInTwoDirectionsAndTryToCreateLink(region, Rot4.South, cell, linksProcessedAt[Rot4.SouthInt]);
      SweepInTwoDirectionsAndTryToCreateLink(region, Rot4.East, cell, linksProcessedAt[Rot4.EastInt]);
      SweepInTwoDirectionsAndTryToCreateLink(region, Rot4.West, cell, linksProcessedAt[Rot4.WestInt]);
    }
    foreach (HashSet<IntVec3> linksProcessed in linksProcessedAt)
    {
      linksProcessed.Clear();
    }
  }

  /// <summary>
  /// Try to make region link with neighboring rotations as fallback
  /// </summary>
  private void SweepInTwoDirectionsAndTryToCreateLink(VehicleRegion region,
    Rot4 potentialOtherRegionDir, IntVec3 cell, HashSet<IntVec3> linksProcessed)
  {
    if (!potentialOtherRegionDir.IsValid)
      return;

    if (linksProcessed.Contains(cell))
      return;

    IntVec3 facingCell = cell + potentialOtherRegionDir.FacingCell;
    if (facingCell.InBounds(map) && regionGridManager[region.GridType].GetRegionAt(facingCell) == region)
      return;

    RegionType expectedRegionType = regionGridManager[region.GridType].Source.ExpectedRegionType(facingCell, pathing, createdFor);
    if (expectedRegionType == RegionType.None)
      return;

    Rot4 rotClockwise = potentialOtherRegionDir.Rotated(RotationDirection.Clockwise);
    linksProcessed.Add(cell);

    int spanRight = 0;
    int spanUp = 0;

    if (!expectedRegionType.IsOneCellRegion())
    {
      VehicleRegionGrid regionGrid = regionGridManager[region.GridType];
      bool IsInvalidForLinking(Rot4 rot, IntVec3 next)
      {
        if (!next.InBounds(map))
          return true;

        if (regionGrid.GetRegionAt(next) != region)
          return true;

        return regionGridManager[region.GridType].Source
          .ExpectedRegionType(next + rot.FacingCell, pathing, createdFor) != expectedRegionType;
      }

      for (spanRight = 0; spanRight <= VehicleRegion.ChunkSize; spanRight++)
      {
        IntVec3 sweepRight = cell + rotClockwise.FacingCell * (spanRight + 1);
        if (IsInvalidForLinking(potentialOtherRegionDir, sweepRight))
          break;
        if (!linksProcessed.Add(sweepRight))
          Log.Error("Attempting to process the same cell twice.");
      }

      for (spanUp = 0; spanUp <= VehicleRegion.ChunkSize; spanUp++)
      {
        IntVec3 sweepUp = cell - rotClockwise.FacingCell * (spanUp + 1);
        if (IsInvalidForLinking(potentialOtherRegionDir, sweepUp))
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
    VehicleRegionLink regionLink = linkDatabase.LinkFrom(span, region.GridType);
    regionLink.Register(region, potentialOtherRegionDir);
    region.AddLink(regionLink);
  }

  public void Return(VehicleRegion region)
  {
    regionPool.Return(region);
  }

  public void Return(VehicleRegionLink regionLink)
  {
    linkDatabase.Return(regionLink);
  }

  private VehicleRegion GetRegion(IntVec3 root, RegionGridType gridType)
  {
    VehicleRegionGrid regionGrid = regionGridManager[gridType];
    VehicleRegion region = regionGrid.GetRegionAt(root);
    if (region != null)
    {
      // Clear existing region and reuse it. Reset will be called after
      // region is forcibly removed from grid to maintain safe behavior.
      regionGrid.ClearFromGrid(region);
      SetNew(region, root, gridType);
      return region;
    }
    region = GetNew(root, gridType);
    return region;
  }

  private VehicleRegion GetNew(IntVec3 root, RegionGridType gridType)
  {
    VehicleRegion region = regionPool.Get();
    region.ObjectPool = regionPool;
    SetNew(region, root, gridType);
    return region;
  }

  private void SetNew(VehicleRegion region, IntVec3 root, RegionGridType gridType)
  {
    if (region == null)
    {
      Log.Warning("Attempting to populate null region.");
      return;
    }

    int id = GetRegionId();
    region.Init(pathing, createdFor, id, gridType);
    region.extentsClose = new CellRect
    {
      minX = root.x,
      maxX = root.x,
      minZ = root.z,
      maxZ = root.z
    };
    region.extentsLimit = VehicleRegion.ChunkAt(root).ClipInsideMap(map);
  }

  private int GetRegionId()
  {
    return Interlocked.Increment(ref nextId);
  }

  [DebugAction(VehicleHarmony.VehiclesLabel, allowedGameStates = AllowedGameStates.PlayingOnMap,
    hideInSubMenu = true)]
  private static List<DebugActionNode> ForceRegenerateRegion()
  {
    if (VehicleHarmony.AllMoveableVehicleDefs.NullOrEmpty())
      return null;

    List<DebugActionNode> debugActions = [];
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
    return debugActions;
  }

  private class FloodFiller
  {
    private const int BufferSize = VehicleRegion.ChunkSize * VehicleRegion.ChunkSize;

    private readonly VehicleRegionMaker regionMaker;

    private readonly int mapWidth;
    private readonly int mapHeight;
    private readonly IPathingManager pathing;

    private readonly FlatQueue<IntVec3> openQueue = new(BufferSize);
    private readonly uint[] visited = new uint[BufferSize];

    private uint visitId;

    public FloodFiller(VehicleRegionMaker regionMaker)
    {
      this.regionMaker = regionMaker;
      pathing = regionMaker.pathing;
      Map map = regionMaker.map;
      (mapWidth, mapHeight) = (map.Size.x, map.Size.z);
    }

    private bool IsRunning => Region != null;

    private VehicleDef CreatedFor => regionMaker.createdFor;

    private VehicleRegion Region { get; set; }

    private VehicleRegionGrid RegionGrid { get; set; }

    private bool VerifyBounds { get; set; }

    private void Init(VehicleRegion region)
    {
      Region = region;
      RegionGrid = regionMaker.regionGridManager[region.GridType];
      visitId++;
    }

    /// <summary>
    /// Halt and clear the BFS traverser
    /// </summary>
    private void Stop()
    {
      openQueue.Reset();
      Region = null;
      RegionGrid = null;
    }

    private void AddCell(IntVec3 cell)
    {
      RegionGrid.SetRegionAt(cell, Region);
      regionMaker.regionCells.Add(cell);
      if (Region.extentsClose.minX > cell.x)
      {
        Region.extentsClose.minX = cell.x;
      }

      if (Region.extentsClose.maxX < cell.x)
      {
        Region.extentsClose.maxX = cell.x;
      }

      if (Region.extentsClose.minZ > cell.z)
      {
        Region.extentsClose.minZ = cell.z;
      }

      if (Region.extentsClose.maxZ < cell.z)
      {
        Region.extentsClose.maxZ = cell.z;
      }

      if (cell.x == CreatedFor.SizePadding ||
          cell.x == mapWidth - 1 - CreatedFor.SizePadding ||
          cell.z == CreatedFor.SizePadding ||
          cell.z == mapHeight - 1 - CreatedFor.SizePadding)
      {
        Region.touchesMapEdge = true;
      }
    }

    private bool IsChunkTouchingEdge(IntVec3 cell)
    {
      CellRect cellRect = VehicleRegion.ChunkAt(cell);
      return cellRect.minX == 0 || cellRect.minZ == 0 || cellRect.maxX == mapWidth - 1 ||
             cellRect.maxZ == mapHeight - 1;
    }

    private static int CellToChunkIndex(IntVec3 cell)
    {
      int relativeX = cell.x - (cell.x - cell.x % VehicleRegion.ChunkSize);
      int relativeZ = cell.z - (cell.z - cell.z % VehicleRegion.ChunkSize);
      return relativeZ * VehicleRegion.ChunkSize + relativeX;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanEnter(IntVec3 cell)
    {
      return RegionGrid.Source.ExpectedRegionType(cell, pathing, CreatedFor) == Region.type;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool InBounds(IntVec3 cell)
    {
      return (uint)cell.x < mapWidth && (uint)cell.z < mapHeight;
    }

    public void FloodFill(IntVec3 start, VehicleRegion region, CancellationToken token)
    {
      if (IsRunning)
      {
        Log.Error("Attempting to run FloodFill while it's already in use.");
        return;
      }

      VerifyBounds = IsChunkTouchingEdge(start);
      Init(region);
      try
      {
        if (!CanEnter(start))
          return;

        openQueue.Enqueue(start);
        visited[CellToChunkIndex(start)] = visitId;
        while (openQueue.Count > 0)
        {
          if (token.IsCancellationRequested)
            return;

          IntVec3 current = openQueue.Dequeue();
          AddCell(current);

          foreach (IntVec3 offset in GenAdj.CardinalDirectionsAround)
          {
            IntVec3 neighbor = current + offset;

            if (VerifyBounds && !InBounds(neighbor))
              continue;
            if (visited[CellToChunkIndex(neighbor)] == visitId)
              continue;

            // TODO - gather cell links ahead of time instead of 2nd pass
            if (!Region.extentsLimit.Contains(neighbor))
              continue;

            if (!CanEnter(neighbor))
              continue;

            visited[CellToChunkIndex(neighbor)] = visitId;
            openQueue.Enqueue(neighbor);
          }
        }
        Region.CountCells();
      }
      catch (Exception ex)
      {
        Log.Error($"Exception thrown while performing BFS FloodFill.\n{ex}");
      }
      finally
      {
        Stop();
      }
    }
  }
}