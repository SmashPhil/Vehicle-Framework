using System.Runtime.CompilerServices;
using CoreLib.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static SmashTools.PathFinder.PathData;

namespace SmashTools.PathFinder;

internal struct FindPathJob<TProvider, THeuristic> : IJob
  where TProvider : unmanaged, IPathCostProvider
  where THeuristic : unmanaged, IHeuristicProvider
{
  // Use integer costs with scalar of 10 to support 1 decimal place for partial costs.
  private const int ScalarFactor = 10;

  private readonly int width;
  private readonly int height;

  private readonly int xEnd;
  private readonly int yEnd;
  private readonly int maxIterations;

  private readonly int start;
  private readonly int end;
  private readonly int rotation;
  private readonly TurnCost turnCost;
  private readonly int turnFactor;
  private readonly int turnCooldown;

  private readonly bool useScalar;
  private readonly int scalarFactor;
  private readonly NativeBitArraySlim.ReadOnly scalar;

  private TProvider provider;
  private THeuristic heuristic;
  private PathData pathData;

  private readonly Offset offsetCardinal;
  private readonly Offset offsetDiagonal;

  public FindPathJob(TProvider provider, THeuristic heuristic, in PathData pathData, in RequestData request)
  {
    width = pathData.mapSize.x;
    height = pathData.mapSize.y;
    this.provider = provider;
    this.heuristic = heuristic;
    this.pathData = pathData;

    turnCost = request.turnCost;
    turnFactor = request.turnFactor;
    turnCooldown = request.turnCooldown;
    start = request.start.x + request.start.z * pathData.mapSize.x;
    end = request.end.x + request.end.z * pathData.mapSize.x;
    xEnd = end % pathData.mapSize.x;
    yEnd = end / pathData.mapSize.x;
    maxIterations = pathData.mapSize.x * pathData.mapSize.y;
    useScalar = request.useScalar;
    scalarFactor = request.scalarFactor;
    scalar = request.scalar;

    // North, East, South, West
    offsetCardinal = new Offset
    {
      xIndices = new int4(0, 1, 0, -1),
      yIndices = new int4(1, 0, -1, 0)
    };
    // NorthEast, SouthEast, SouthWest, NorthWest
    offsetDiagonal = new Offset
    {
      xIndices = new int4(1, 1, -1, -1),
      yIndices = new int4(1, -1, -1, 1)
    };

    rotation = request.rotation;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private bool InBounds(int x, int y)
  {
    return MapUtils.InBounds(x, y, width, height);
  }

  public void Execute()
  {
    NativeMinHeap frontier = pathData.frontier;
    frontier.Push(start, 0);

    pathData.gCosts[start] = 0;
    pathData.parents[start] = start;
    pathData.heading[start] = rotation;

    int iteration = 0;
    while (frontier.Length > 0)
    {
      int current = frontier.Pop();

      if (pathData.NodeStatus(current) == Status.Closed)
        continue;

      if (current == end)
      {
        FinalizePath(current);
        return;
      }

      if (iteration++ == maxIterations)
        break;

      uint packedPos = pathData.indexToCell[current];
      IndexLookupCache.Unpack(packedPos, out int x, out int y);
      Process(current, x, y, cardinal: true);
      Process(current, x, y, cardinal: false);
      pathData.SetNodeStatus(current, Status.Closed);
    }
  }

  private void Process(int current, int x, int y, bool cardinal)
  {
    int octileCost = cardinal ? PathFinder.CostCardinal : PathFinder.CostDiagonal;
    Offset offset = cardinal ? offsetCardinal : offsetDiagonal;

    int4 nxIndices = x + offset.xIndices;
    int4 nyIndices = y + offset.yIndices;
    int4 hCost = heuristic.GetHeuristic(nxIndices, nyIndices, xEnd, yEnd);

    int4 headings = cardinal ? Rotation.CardinalHeadings : Rotation.DiagonalHeadings;

    int heading = pathData.heading[current];
    int distCooldown = pathData.turnCooldown[current];
    int gCost = pathData.gCosts[current];
    for (int i = 0; i < 4; i++)
    {
      int nx = nxIndices[i];
      int ny = nyIndices[i];
      if (!InBounds(nx, ny))
        continue;

      int ni = nx + ny * width;
      Status status = pathData.NodeStatus(ni);
      if (status == Status.Closed)
        continue;

      if (!cardinal)
      {
        // Strict diagonal movement where both adjacent cardinal nodes must be passable
        int dx = nx + y * width;
        int dy = x + ny * width;
        if (pathData.pathGrid[dx] >= PathFinder.ImpassableCost || pathData.pathGrid[dy] >= PathFinder.ImpassableCost)
          continue;
      }

      int nextHeading = headings[i];
      int pathCost = provider.GetPathCost(nx, ny, nextHeading, in pathData);
      if (pathCost >= PathFinder.ImpassableCost)
        continue;

      int newG = gCost + octileCost + pathCost * ScalarFactor;
      if (useScalar && scalar.IsSet(ni))
      {
        ulong prod = (ulong)newG * (ulong)scalarFactor;
        newG = (int)((prod + (1 << (PathGridScalar.Precision - 1))) >> PathGridScalar.Precision);
      }

      int turn = 0;
      if (turnCost != TurnCost.None)
      {
        if (heading != nextHeading)
        {
          int degrees = Rotation.TurnCost(heading, nextHeading);
          if (turnCost is TurnCost.Linear)
          {
            turn = degrees * turnFactor;
          }
          else if (turnCost is TurnCost.Restricted)
          {
            turn = degrees * (turnFactor << distCooldown);
          }
          pathData.turnCooldown[ni] = turnCooldown;
        }
        else if (distCooldown > 0)
        {
          pathData.turnCooldown[ni] = distCooldown - 1;
        }
        newG += turn;
      }

      if (status == Status.Stale || newG < pathData.gCosts[ni])
      {
        pathData.gCosts[ni] = newG;
        pathData.heading[ni] = headings[i];
        pathData.parents[ni] = current;
        pathData.frontier.Push(ni, newG + hCost[i]);
        pathData.SetNodeStatus(ni, Status.Open);
      }
    }
  }

  private void FinalizePath(int finalIndex)
  {
    int index = finalIndex;
    for (int i = 0; i < maxIterations; i++)
    {
      pathData.path.Add(index);

      int parent = pathData.parents[index];
      if (index == parent)
        return;

      index = parent;
    }
  }
}
