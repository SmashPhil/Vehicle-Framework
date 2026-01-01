using System;
using System.Collections.Generic;
using System.Threading;
using CoreLib.Performance;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

/// <summary>
/// Traverser utility methods for traversing between 2 regions
/// </summary>
public static class VehicleRegionTraverser
{
  public const int WorkerCount = 8;

  private static readonly ThreadLocal<Queue<BreadthFirstSearchImpl>> Workers = new(CreateWorkers);

  public delegate bool VehicleRegionEntry(VehicleRegion from, VehicleRegion to);

  public delegate bool VehicleRegionProcessor(VehicleRegion reg);

  /// <summary>
  /// <paramref name="cellA"/> and <paramref name="cellB"/> are contained within the same region or can traverse
  /// between regions
  /// </summary>
  public static bool WithinRegions(this IntVec3 cellA, IntVec3 cellB, Map map, VehicleDef vehicleDef,
    int regionLookCount, TraverseParms traverseParams,
    RegionType traversableRegionTypes = RegionType.Set_Passable)
  {
    VehicleRegion regionA =
      VehicleRegionAndRoomQuery.RegionAt(cellA, map, vehicleDef, traversableRegionTypes);
    if (regionA is null)
      return false;

    VehicleRegion regionB =
      VehicleRegionAndRoomQuery.RegionAt(cellB, map, vehicleDef, traversableRegionTypes);
    if (regionB is null)
      return false;

    if (regionA == regionB)
      return true;

    bool found = false;
    BreadthFirstTraverse(regionA, EntryCondition, Processor, regionLookCount,
      traversableRegionTypes);
    return found;

    bool EntryCondition(VehicleRegion from, VehicleRegion to)
    {
      return to.Allows(traverseParams);
    }

    bool Processor(VehicleRegion region)
    {
      if (region != regionB)
        return false;

      found = true;
      return true;
    }
  }

  public static void MarkRegionsBfs(VehicleRegion root, VehicleRegionEntry entryCondition,
    int maxRegions, int inRadiusMark, RegionType traversableRegionTypes = RegionType.Set_Passable)
  {
    BreadthFirstTraverse(root, entryCondition, delegate (VehicleRegion region)
    {
      region.mark = inRadiusMark;
      return false;
    }, maxRegions: maxRegions, traversableRegionTypes: traversableRegionTypes);
  }

  /// <summary>
  /// Create all workers up to max worker count.
  /// </summary>
  private static Queue<BreadthFirstSearchImpl> CreateWorkers()
  {
    Queue<BreadthFirstSearchImpl> workerQueue = new Queue<BreadthFirstSearchImpl>(WorkerCount);
    for (int i = 0; i < WorkerCount; i++)
    {
      workerQueue.Enqueue(new BreadthFirstSearchImpl(i));
    }
    return workerQueue;
  }

  /// <summary>
  /// BreadthFirstSearch from <paramref name="start"/> and <paramref name="regionProcessor"/>
  /// </summary>
  public static void BreadthFirstTraverse(IntVec3 start, Map map, VehicleDef vehicleDef,
    VehicleRegionEntry entryCondition, VehicleRegionProcessor regionProcessor,
    int maxRegions = 999999, RegionType traversableRegionTypes = RegionType.Set_Passable)
  {
    VehicleRegion region =
      VehicleRegionAndRoomQuery.RegionAt(start, map, vehicleDef, traversableRegionTypes);
    if (region is null) return;
    BreadthFirstTraverse(region, entryCondition, regionProcessor, maxRegions,
      traversableRegionTypes);
  }

  /// <summary>
  /// BreadthFirstSearch from <paramref name="root"/> and <paramref name="regionProcessor"/>
  /// </summary>
  public static void BreadthFirstTraverse(VehicleRegion root, VehicleRegionEntry entryCondition,
    VehicleRegionProcessor regionProcessor, int maxRegions = 999999,
    RegionType traversableRegionTypes = RegionType.Set_Passable)
  {
    if (root is null)
    {
      Log.Error("BFS with null root region.");
      return;
    }

    if (Workers.Value.Count == 0)
    {
      Log.Error(
        $"No free workers for BFS. BFS recurred deeper than {WorkerCount}, or this system is in an inconsistent state.");
      return;
    }

    BreadthFirstSearchImpl bfsWorker = Workers.Value.Dequeue();
    try
    {
      bfsWorker.BreadthFirstTraverseWork(root, entryCondition, regionProcessor, maxRegions,
        traversableRegionTypes);
    }
    catch (Exception ex)
    {
      Log.Error($"Exception thrown while traversing regions.\n{ex}");
    }
    finally
    {
      bfsWorker.Clear();
      Workers.Value.Enqueue(bfsWorker);
    }
  }

  /// <summary>
  /// Breadth First Search to fill room based on <paramref name="region"/>
  /// </summary>
  public static VehicleRoom FloodAndSetRooms(VehicleRegion region, Map map, VehicleDef vehicleDef,
    VehicleRoom existingRoom)
  {
    VehicleRoom floodingRoom = existingRoom ?? VehicleRoom.MakeNew(map, vehicleDef, region.gridType);
    region.Room = floodingRoom;
    if (!region.type.AllowsMultipleRegionsPerDistrict())
    {
      return floodingRoom;
    }
    BreadthFirstTraverse(region, EntryCondition, Processor, traversableRegionTypes: RegionType.Set_All);
    return floodingRoom;

    bool EntryCondition(VehicleRegion _, VehicleRegion to)
    {
      return to.type == region.type && to.Room != floodingRoom;
    }

    bool Processor(VehicleRegion r)
    {
      r.Room = floodingRoom;
      return false;
    }
  }

  /// <summary>
  /// Breadth First Search to assign new region group indices for <paramref name="root"/>
  /// </summary>
  /// <param name="root"></param>
  /// <param name="newRegionGroupIndex"></param>
  public static void FloodAndSetNewRegionIndex(VehicleRegion root, int newRegionGroupIndex)
  {
    root.newRegionGroupIndex = newRegionGroupIndex;
    if (!root.type.AllowsMultipleRegionsPerDistrict())
    {
      return;
    }
    BreadthFirstTraverse(root, EntryCondition, RegionProcessor, traversableRegionTypes: RegionType.Set_All);
    return;

    bool EntryCondition(VehicleRegion from, VehicleRegion r)
    {
      return r.type == root.type && r.newRegionGroupIndex < 0;
    }

    bool RegionProcessor(VehicleRegion r)
    {
      r.newRegionGroupIndex = newRegionGroupIndex;
      return false;
    }
  }

  /// <summary>
  /// Breadth First Search worker class
  /// </summary>
  private class BreadthFirstSearchImpl
  {
    private readonly Queue<VehicleRegion> open = [];

    private int numRegionsProcessed;
    private uint closedIndex = 1u;
    private readonly int closedArrayPos;

    public BreadthFirstSearchImpl(int closedArrayPos)
    {
      this.closedArrayPos = closedArrayPos;
    }

    /// <summary>
    /// Clear region queue
    /// </summary>
    public void Clear()
    {
      open.Clear();
    }

    /// <summary>
    /// Queue region available for traversal
    /// </summary>
    /// <param name="region"></param>
    private void QueueNewOpenRegion(VehicleRegion region)
    {
      Assert.IsFalse(region.closedIndex.Value[closedArrayPos] == closedIndex, "Enqueueing already closed index.");
      open.Enqueue(region);
      region.closedIndex.Value[closedArrayPos] = closedIndex;
    }

    /// <summary>
    /// Breadth First Traversal search algorithm
    /// </summary>
    public void BreadthFirstTraverseWork(VehicleRegion root, VehicleRegionEntry entryCondition,
      VehicleRegionProcessor regionProcessor, int maxRegions, RegionType traversableRegionTypes)
    {
      if (root.type == RegionType.None)
      {
        return;
      }
      closedIndex += 1u;
      open.Clear();
      numRegionsProcessed = 0;
      QueueNewOpenRegion(root);
      while (open.Count > 0)
      {
        VehicleRegion region = open.Dequeue();
        if (regionProcessor != null && regionProcessor(region))
        {
          return;
        }
        numRegionsProcessed++;
        if (numRegionsProcessed >= maxRegions)
        {
          return;
        }
        using ListSnapshot<VehicleRegionLink> links = region.Links;
        foreach (VehicleRegionLink regionLink in links)
        {
          ProcessRegion(region, regionLink.GetOtherRegion(region));
        }
      }
      return;

      void ProcessRegion(VehicleRegion region, VehicleRegion linkedRegion)
      {
        if (linkedRegion != null &&
          linkedRegion.closedIndex.Value[closedArrayPos] != closedIndex &&
          (linkedRegion.type & traversableRegionTypes) != RegionType.None &&
          (entryCondition is null || entryCondition(region, linkedRegion)))
        {
          QueueNewOpenRegion(linkedRegion);
        }
      }
    }
  }
}