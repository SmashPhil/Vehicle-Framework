using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LudeonTK;
using SmashTools;
using SmashTools.Performance;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

public class VehicleRegionConnector : VehicleGridManager
{
  private const int ChunkCellCount = VehicleRegion.ChunkSize * VehicleRegion.ChunkSize;

  private VehicleRegionGrid regionGrid;
  private VehiclePathGrid pathGrid;
  private readonly ObjectPool<ConnectorGroup> connectorPool;
  private readonly ThreadLocal<CostFinder> costFinder;

  private readonly List<VehicleRegion> regions = [];

  private readonly ConcurrentDictionary<VehicleRegion, ConnectorGroup> connectors = [];

  public VehicleRegionConnector(VehiclePathingSystem mapping, VehicleDef createdFor) : base(mapping,
    createdFor)
  {
    const float PoolSize = 0.5f; // Create pool for 50% of average region connector count
    const int AverageConnections = 8; // 4 edges * 2 connections per

    float totalRegions = ((float)mapping.map.Size.x / VehicleRegion.ChunkSize) *
      ((float)mapping.map.Size.z / VehicleRegion.ChunkSize);

    connectorPool =
      new ObjectPool<ConnectorGroup>(Mathf.CeilToInt(totalRegions * AverageConnections * PoolSize));
    costFinder = new ThreadLocal<CostFinder>(() => new CostFinder(this));
  }

  public bool GridConnected { get; private set; }

  public bool IsDisabled { get; internal set; }

  public override void PostInit()
  {
    base.PostInit();
    pathGrid = mapping[createdFor].VehiclePathGrid;
    regionGrid = mapping[createdFor].VehicleRegionGrid;
  }

  public void RebuildAllConnections()
  {
    regionGrid.GetAllRegions(regions);
    foreach (VehicleRegion region in regions)
    {
      RecalculateWeights(region);
    }
  }

  internal void RecalculateWeights(VehicleRegion region)
  {
    GridConnected = false;
    using ListSnapshot<VehicleRegionLink> linksSnapshot = region.Links;

    if (!connectors.TryGetValue(region, out ConnectorGroup connectorGroup))
    {
      connectorGroup = connectorPool.Get();
      connectors[region] = connectorGroup;
    }

    // Clear lists for preprocessing
    connectorGroup.Reset();

    // Form connections in A,B pairs where order does not matter as the resulting weights
    // will be cached for both links.
    for (int i = 0; i < linksSnapshot.Count; i++)
    {
      VehicleRegionLink from = linksSnapshot.items[i];
      IntVec3 fromEnd = from.End;
      for (int j = i + 1; j < linksSnapshot.Count; j++)
      {
        VehicleRegionLink to = linksSnapshot.items[j];
        IntVec3 toEnd = to.End;
        Connect(region, connectorGroup, from.Root, to.Root);
        Connect(region, connectorGroup, from.Root, toEnd);
        Connect(region, connectorGroup, fromEnd, to.Root);
        Connect(region, connectorGroup, fromEnd, toEnd);
      }
    }

    GridConnected = true;
  }

  private void AdjustInfacing(VehicleRegion region, VehicleRegionLink linkA)
  {
    // TODO
  }

  private void Connect(VehicleRegion region, ConnectorGroup group, IntVec3 from, IntVec3 to)
  {
    float cost = costFinder.Value.ConnectionCost(region, from, to);
    ulong hash = from.UniqueHashCode();
    int fromIdx = mapping.map.cellIndices.CellToIndex(from);
    int toIdx = mapping.map.cellIndices.CellToIndex(from);
    group.Add(hash, fromIdx, toIdx, cost);
  }

  public static int SymmetricHash(int x1, int z1, int x2, int z2)
  {
    unchecked
    {
      int p1 = QuickHash(x1, z1);
      int p2 = QuickHash(x2, z2);
      return p1 + p2 + (p1 ^ p2);
    }

    static int QuickHash(int a, int b)
    {
      unchecked
      {
        int hash = 17;
        hash = hash * 31 + a;
        hash = hash * 31 + b;
        return hash;
      }
    }
  }

  [DebugAction(category = VehicleHarmony.VehiclesLabel, name = "Rebuild All Connections",
    actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
  private static void DebugRebuildAllConnections()
  {
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    VehiclePathingSystem mapping = map.GetCachedMapComponent<VehiclePathingSystem>();
    Assert.IsNotNull(mapping);

    VehicleDef vehicleDef =
      mapping.GridOwners.AllOwners.FirstOrDefault(def => !mapping[def].Suspended);
    Assert.IsNotNull(vehicleDef);

    VehicleRegionConnector connector = mapping[vehicleDef].VehicleRegionConnector;
    DeepProfiler.Start("Rebuild All Connections");
    connector.RebuildAllConnections();
    DeepProfiler.End();
  }

  public readonly struct Disabler : IDisposable
  {
    private readonly VehicleRegionConnector connector;

    public Disabler(VehicleRegionConnector connector)
    {
      this.connector = connector;
      this.connector.IsDisabled = true;
    }

    void IDisposable.Dispose()
    {
      connector.IsDisabled = false;
    }
  }

  private class ConnectorGroup : IPoolable
  {
    // Connections stemming from root of span
    private readonly Dictionary<ulong, List<Connection>> roots = [];
    private readonly object dictLock = new();

    public bool InPool { get; set; }

    public void Add(ulong hash, int root, int to, float cost)
    {
      lock (dictLock)
      {
        if (!roots.TryGetValue(hash, out List<Connection> connections))
        {
          // Use SimplePool here so we can just use the Verse extension. Access to these
          // pooled list objects are behind locks anyways so there won't be a race condition.
          connections = SimplePool<List<Connection>>.Get();
          roots[hash] = connections;
        }
        connections.Add(new Connection(root, to, cost));
      }
    }

    public void Reset()
    {
      lock (dictLock)
      {
        roots.ClearAndPoolValueLists();
      }
    }
  }

  private class CostFinder
  {
    private readonly VehicleRegionConnector regionConnector;

    private readonly PriorityQueue<int, int> openQueue = new();
    private readonly Node[] nodes = new Node[ChunkCellCount];

    private readonly BoolGrid visited = new(VehicleRegion.ChunkSize + 1,
      VehicleRegion.ChunkSize + 1);

    private readonly CellIndices cellIndices;

    public CostFinder(VehicleRegionConnector regionConnector)
    {
      this.regionConnector = regionConnector;
      cellIndices = new CellIndices(regionConnector.mapping.map);
    }

    private bool IsRunning { get; set; }

    public float ConnectionCost(VehicleRegion region, IntVec3 start, IntVec3 destination)
    {
      Assert.IsFalse(IsRunning);
      IsRunning = true;
      try
      {
        CellRect chunkRect = VehicleRegion.ChunkAt(start);
        // Converts map grid index to Chunk based index where 0,0 = bottom left. This will allow us
        // to use a smaller array (the size of a single chunk) for all node processing.
        int startIdx = cellIndices.CellToIndex(start);
        int destIdx = cellIndices.CellToIndex(destination);
        using ListSnapshot<VehicleRegionLink> links = region.Links;
        Assert.IsTrue(openQueue.Count == 0);
        openQueue.Enqueue(startIdx, 0);
        while (openQueue.Count > 0)
        {
          if (!openQueue.TryDequeue(out int current, out _))
            break;

          if (current.Equals(destIdx))
            return TotalCost(startIdx, destIdx, in chunkRect);

          int currentRelative = RelativeIndex(current, in chunkRect);
          foreach (int neighbor in NeighborsAt(region, links.items, current))
          {
            int neighborRelative = RelativeIndex(neighbor, in chunkRect);
            if (visited[neighborRelative])
              continue;

            Node node = CreateNode(currentRelative, neighborRelative);
            nodes[neighborRelative] = node;
            openQueue.Enqueue(neighbor, node.cost);
          }
        }
      }
      finally
      {
        IsRunning = false;
        openQueue.Clear();
        visited.Clear();
      }
      Log.Error($"Ran out of cells to process from {start} to {destination}.");
      return 0;
    }

    private IEnumerable<int> NeighborsAt(VehicleRegion region, List<VehicleRegionLink> links,
      int current)
    {
      IntVec3 cell = cellIndices.IndexToCell(current);
      for (int i = 0; i < 8; i++)
      {
        int x = cell.x + VehiclePathFinder.neighborOffsets[i];
        int z = cell.z + VehiclePathFinder.neighborOffsets[i + 8];
        int index = cellIndices.CellToIndex(x, z);
        if (regionConnector.regionGrid.GetRegionAt(index) == region)
          yield return index;
        else
        {
          foreach (VehicleRegionLink link in links)
          {
            if ((link.Root.x == x && link.Root.z == z) || (link.End.x == x && link.End.z == z))
              yield return index;
          }
        }
      }
    }

    private Node CreateNode(int current, int neighbor)
    {
      return new Node
      {
        parent = current,
        cost = regionConnector.pathGrid.innerArray[neighbor]
      };
    }

    private int RelativeIndex(int index, ref readonly CellRect chunkRect)
    {
      IntVec3 cell = cellIndices.IndexToCell(index);
      return (cell.z - chunkRect.minZ) * VehicleRegion.ChunkSize + (cell.x - chunkRect.minX);
    }

    private float TotalCost(int start, int destination, ref readonly CellRect chunkRect)
    {
      int startRelative = RelativeIndex(start, in chunkRect);
      int current = RelativeIndex(destination, in chunkRect);
      Node node = nodes[current];
      float total = node.cost;
      for (int i = 0; i < ChunkCellCount; i++)
      {
        current = node.parent;
        node = nodes[current];
        total += node.cost;

        if (current == startRelative)
          return total;
      }
      Log.Error("Misconfigured path in region connector. Unable to resolve total cost");
      return total;
    }
  }

  private struct Node
  {
    public int parent;
    public int cost;
  }
}