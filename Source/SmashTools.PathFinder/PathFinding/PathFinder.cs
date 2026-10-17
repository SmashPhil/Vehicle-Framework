using System;
using System.Collections.Generic;
using CoreLib;
using CoreLib.Collections;
using CoreLib.PathFinding;
using CoreLib.Performance;
using CoreLib.Jobs;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SmashTools.PathFinder
{
  /// <summary>
  /// Multi-cell pathfinder utilizing burst compiled jobs.
  /// </summary>
  /// <remarks>All requests must be made from the main thread.</remarks>
  [PublicAPI]
  public class PathFinder : IJobDispatcher, IDisposable, IPathGridSync
  {
    private const int JobBufferSize = 24;

    public const int CostCardinal = 10;
    public const int CostDiagonal = 14;
    public const int ImpassableCost = 10000;

    private bool disposed;
    private readonly ObjectPool<PathData> pathDataPool;
    private readonly List<PathReceipt> scheduledJobs;

    private readonly int2 mapSize;
    private readonly int2 hitbox;
    private readonly int batchSize;

    private NativeArray<int>.ReadOnly pathGrid;
    private NativeArray<uint>.ReadOnly indexToCell;

    private NativeArray<int> snapshot;
    private SumAreaTable cardinalTable;
    private SumAreaTable diagonalTable;

    private NativeBitArraySlim emptyScalar;

    private JobHandle snapshotJobHandle;

    public PathFinder(in Settings settings)
    {
      if (!settings.pathGrid.IsCreated)
        throw new ArgumentNullException(nameof(settings.pathGrid));

      mapSize = settings.mapSize;
      hitbox = settings.hitbox;
      batchSize = settings.batchSize > 0 ? settings.batchSize : mapSize.x;

      pathGrid = settings.pathGrid;
      snapshot = new NativeArray<int>(pathGrid.Length, Allocator.Persistent);
      indexToCell = IndexLookupCache.Reserve(mapSize);
      emptyScalar = new NativeBitArraySlim(1, Allocator.Persistent);

      if (hitbox.x > 1 || hitbox.y > 1)
      {
        if (hitbox.x == 0 || hitbox.y == 0)
          throw new ArgumentException("Using SumAreaTable with 0-size hitbox.");

        cardinalTable = new SumAreaTable(new SumAreaTable.Config
        {
          size = mapSize,
          tableCosts = snapshot.AsReadOnly(),
          layout = SumAreaTable.Layout.Cardinal
        });
        diagonalTable = new SumAreaTable(new SumAreaTable.Config
        {
          size = mapSize,
          tableCosts = snapshot.AsReadOnly(),
          layout = SumAreaTable.Layout.Diagonal
        });
        MultiCellPathFinding = true;
      }

      scheduledJobs = new List<PathReceipt>(capacity: JobBufferSize);
      if (settings.poolObjects)
      {
        pathDataPool = new ObjectPool<PathData>(size: JobBufferSize,
          factory: () => CreatePathData(Allocator.Persistent))
        {
          Resizable = true
        };
      }
      if (JobDispatchManager.Exists)
      {
        JobDispatchManager.Register(this);
      }
    }

    public int2 MapSize => mapSize;

    public bool MultiCellPathFinding { get; set; }

    private bool IsSnapshotStale { get; set; } = true;

    internal NativeArray<int>.ReadOnly Snapshot => snapshot.AsReadOnly();

    public void NotifyWritingToGrid()
    {
      if (snapshotJobHandle.IsCompleted)
        return;

      const int MaxWaitTime = 500; // ms
      UnityThread.ExecuteOnMainThreadAndWait(delegate
      {
        snapshotJobHandle.Complete();
        IsSnapshotStale = true;
      }, waitTimeout: MaxWaitTime);
    }

    private bool IsValidDestination(in PathRequest request)
    {
      int3 end = request.end;
      if (math.all(request.start == end))
        return false;

      int index = end.x + end.z * mapSize.x;
      if (request.mask != null)
      {
        int mask = request.mask.ArrayUnsafe[index];
        if (mask > 0)
        {
          return mask < ImpassableCost;
        }
      }
      return pathGrid[index] < ImpassableCost;
    }

    public Path FindPath(in PathRequest request)
    {
      if (!IsValidDestination(request))
      {
        // Empty path with no path found
        return new Path();
      }
      using PathReceipt receipt = CreateReceipt(request);
      return FindPathNow(receipt);
    }

    public PathReceipt RequestPath(in PathRequest request)
    {
      if (!IsValidDestination(request))
      {
        PathReceipt invalidReceipt = new(request, default);
        return invalidReceipt;
      }
      PathReceipt receipt = CreateReceipt(request);
      if (!JobDispatchManager.Exists)
      {
        Logger.Error("Requesting path when dispatch manager does not exist. Executing immediately.");
        FindPathNow(receipt);
        return receipt;
      }

      SnapshotGrid(receipt);

      HeuristicGrid.Data heuristicData = request.heuristic;
      if (MultiCellPathFinding)
      {
        receipt.JobHandle = heuristicData.grid != null ?
          CreateWorker(receipt,
              provider: new SumTableCostProvider(cardinalTable, diagonalTable, hitbox),
              heuristic: new WeightedHeuristic(mapSize, heuristicData.weights, heuristicData.grid.ArrayUnsafe))
            .Schedule(snapshotJobHandle) :
          CreateWorker(receipt,
              provider: new SumTableCostProvider(cardinalTable, diagonalTable, hitbox),
              heuristic: new NormalHeuristic())
            .Schedule(snapshotJobHandle);
      }
      else
      {
        receipt.JobHandle = heuristicData.grid != null
          ? CreateWorker(receipt,
              provider: new NormalCostProvider(),
              heuristic: new WeightedHeuristic(mapSize, heuristicData.weights, heuristicData.grid.ArrayUnsafe))
            .Schedule(snapshotJobHandle)
          : CreateWorker(receipt,
              provider: new NormalCostProvider(),
              heuristic: new NormalHeuristic())
            .Schedule(snapshotJobHandle);
      }
      scheduledJobs.Add(receipt);
      return receipt;
    }

    private Path FindPathNow(PathReceipt receipt)
    {
      PathRequest request = receipt.PathRequest;
      bool needSnapshot = IsSnapshotStale || request.costOffsets is { Count: > 0 };
      if (needSnapshot)
      {
        SnapshotGrid(receipt);
        IsSnapshotStale = false;
      }
      snapshotJobHandle.Complete();

      HeuristicGrid.Data heuristicData = request.heuristic;
      if (MultiCellPathFinding)
      {
        if (needSnapshot)
        {
          cardinalTable.Recalculate();
          diagonalTable.Recalculate();
        }

        if (heuristicData.grid != null)
        {
          var worker = CreateWorker(receipt,
            provider: new SumTableCostProvider(cardinalTable, diagonalTable, hitbox),
            heuristic: new WeightedHeuristic(mapSize, heuristicData.weights, heuristicData.grid.ArrayUnsafe));
          worker.Run();
        }
        else
        {
          var worker = CreateWorker(receipt,
            provider: new SumTableCostProvider(cardinalTable, diagonalTable, hitbox),
            heuristic: new NormalHeuristic());
          worker.Run();
        }
      }
      else
      {
        if (heuristicData.grid != null)
        {
          var worker = CreateWorker(receipt,
            provider: new NormalCostProvider(),
            heuristic: new WeightedHeuristic(mapSize, heuristicData.weights, heuristicData.grid.ArrayUnsafe));
          worker.Run();
        }
        else
        {
          var worker = CreateWorker(receipt, new NormalCostProvider(), new NormalHeuristic());
          worker.Run();
        }
      }
      receipt.JobHandle.Complete();
      return receipt.GetPath();
    }

    private void SnapshotGrid(PathReceipt receipt)
    {
      PathRequest request = receipt.PathRequest;
      bool hasCostOffsets = request.costOffsets is { Count: > 0 };
      bool hasBitOffsets = request.bitOffsets is { Count: > 0 };
      bool needsSnapshot = IsSnapshotStale || request.modifiers != null || hasCostOffsets || hasBitOffsets ||
                           request.entityCost.IsValid || request.mask != null;

      if (needsSnapshot)
      {
        if (request.mask != null)
        {
          snapshotJobHandle = new SnapshotMaskedGridJob(pathGrid, request.mask.ArrayUnsafe, snapshot)
            .ScheduleParallel(pathGrid.Length, batchSize, dependency: default);
        }
        else
        {
          snapshotJobHandle = new SnapshotGridJob(pathGrid, snapshot).Schedule();
        }
      }

      if (hasCostOffsets || hasBitOffsets)
      {
        if (hasCostOffsets)
        {
          NativeArrayList<short> costOffsets = PackCostOffsets(request, Allocator.TempJob);
          snapshotJobHandle = new SnapshotOffsetsJob(snapshot, costOffsets)
            .ScheduleParallel(pathGrid.Length, batchSize, dependency: snapshotJobHandle);
          snapshotJobHandle = costOffsets.Dispose(snapshotJobHandle);
        }
        if (hasBitOffsets)
        {
          var data = PackBitOffsets(request, Allocator.TempJob);
          snapshotJobHandle = new SnapshotBitOffsetsJob(snapshot, data)
            .ScheduleParallel(pathGrid.Length, batchSize, snapshotJobHandle);
          snapshotJobHandle = data.Dispose(snapshotJobHandle);
        }
      }

      if (request.modifiers != null)
      {
        snapshotJobHandle = new SnapshotModifiersJob(snapshot, request.modifiers.InnerGrid)
          .ScheduleParallel(pathGrid.Length, batchSize, snapshotJobHandle);
      }

      EntityConfig config = request.entityCost;
      if (config.IsValid)
      {
        snapshotJobHandle = new SnapshotEntityCostsJob(config, snapshot)
          .ScheduleParallel(pathGrid.Length, batchSize, snapshotJobHandle);
      }

      if (needsSnapshot && MultiCellPathFinding)
      {
        snapshotJobHandle = cardinalTable.ScheduleRecalculate(snapshotJobHandle);
        snapshotJobHandle = diagonalTable.ScheduleRecalculate(snapshotJobHandle);
      }

      IsSnapshotStale = false;
    }

    private FindPathJob<P, H> CreateWorker<P, H>(PathReceipt receipt, P provider, H heuristic)
      where P : unmanaged, IPathCostProvider
      where H : unmanaged, IHeuristicProvider
    {
      return new FindPathJob<P, H>(provider, heuristic, receipt.PathData, GetMapData(receipt.PathRequest));
    }

    private PathReceipt CreateReceipt(in PathRequest request)
    {
      PathData pathData = GetPathData(request);
      PathReceipt receipt = new(request, pathData, pathDataPool);
      return receipt;
    }

    private RequestData GetMapData(in PathRequest request)
    {
      RequestData data = new()
      {
        start = request.start,
        end = request.end,
        rotation = request.rotation,
        turnCost = request.turnData.turnCost,
        turnCooldown = request.turnData.turnCooldown,
        turnFactor = request.turnData.turnFactorPerDegree,
        useScalar = request.scalar.grid != null
      };
      if (data.turnCost is TurnCost.Restricted)
      {
#if RELEASE
        Logger.WarningOnce("TurnCost.Restricted is an experimental feature, path results may be inaccurate.",
          "TurnCostRestricted".GetHashCode());
#endif
        if (data.turnCooldown == 0)
        {
          Logger.Error("Using TurnCost.Restricted with no turn cooldown specified. " +
                         "Use TurnCost.Linear if no turn cooldown is intended.");
          data.turnCost = TurnCost.Linear;
        }
      }
      if (data.useScalar)
      {
        data.scalar = request.scalar.grid.ArrayUnsafe;
        data.scalarFactor = request.scalar.factor;
      }
      else
      {
        data.scalar = emptyScalar.AsReadOnly();
        data.scalarFactor = 0;
      }
      return data;
    }

    private PathData GetPathData(in PathRequest request)
    {
      if (pathDataPool != null)
        return pathDataPool.Get();

      return CreatePathData(Allocator.Persistent);
    }

    private PathData CreatePathData(Allocator allocator)
    {
      GridData gridData = new()
      {
        mapSize = mapSize,
        pathGrid = snapshot.AsReadOnly(),
        indexToCell = indexToCell
      };
      return new PathData(gridData, allocator);
    }

    void IJobDispatcher.LateUpdate()
    {
      for (int i = scheduledJobs.Count; --i >= 0;)
      {
        PathReceipt receipt = scheduledJobs[i];
        if (!receipt.JobHandle.IsCompleted)
          continue;

        scheduledJobs.RemoveAt(i);
      }
    }

    void IJobDispatcher.OnDestroy()
    {
      CompleteAllJobs();
    }

    private void CompleteAllJobs()
    {
      if (scheduledJobs.Count == 0)
        return;

      NativeArray<JobHandle> jobsToComplete = new(scheduledJobs.Count, Allocator.TempJob);
      for (int i = 0; i < scheduledJobs.Count; i++)
      {
        PathReceipt receipt = scheduledJobs[i];
        jobsToComplete[i] = receipt.JobHandle;
      }

      JobHandle.CompleteAll(jobsToComplete);

      jobsToComplete.Dispose();

      foreach (PathReceipt receipt in scheduledJobs)
      {
        receipt.Dispose();
      }
      scheduledJobs.Clear();
      Dispose();
    }

    public void Dispose()
    {
      if (disposed)
        return;

      disposed = true;
      if (JobDispatchManager.Exists)
      {
        JobDispatchManager.Deregister(this);
      }
      pathDataPool?.Clear();
      snapshot.Dispose();
      cardinalTable?.Dispose();
      diagonalTable?.Dispose();
      emptyScalar.Dispose();

      IndexLookupCache.Release(mapSize);
    }

    private static NativeArrayList<short> PackCostOffsets(in PathRequest request, Allocator allocator)
    {
      var arrays = new NativeArray<short>.ReadOnly[request.costOffsets.Count];
      for (int i = 0; i < arrays.Length; i++)
      {
        arrays[i] = request.costOffsets[i].ArrayUnsafe;
      }
      return NativeArrayList<short>.PackArrayList(arrays, allocator);
    }

    private static SnapshotBitOffsetsJob.Data PackBitOffsets(in PathRequest request, Allocator allocator)
    {
      int length = request.bitOffsets.Count;
      NativeArray<short> offsetsSet = new(length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
      NativeArray<short> offsetsUnset = new(length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
      var arrays = new NativeBitArraySlim.ReadOnly[length];
      for (int i = 0; i < arrays.Length; i++)
      {
        BitOffset offsetData = request.bitOffsets[i];
        arrays[i] = offsetData.boolGrid.ArrayUnsafe;
        offsetsSet[i] = offsetData.cost.set;
        offsetsUnset[i] = offsetData.cost.unset;
      }

      return new SnapshotBitOffsetsJob.Data
      {
        bitArrays = NativeArrayList<ulong>.PackBitArrayList(arrays, allocator),
        offsetSet = offsetsSet,
        offsetUnset = offsetsUnset
      };
    }

    [PublicAPI]
    public struct Settings
    {
      public int2 mapSize;
      public int2 hitbox;
      public NativeArray<int>.ReadOnly pathGrid;

      public bool poolObjects;
      public int batchSize;
    }
  }
}