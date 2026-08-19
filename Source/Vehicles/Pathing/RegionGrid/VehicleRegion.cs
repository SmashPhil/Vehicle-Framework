using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CoreLib.Performance;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

/// <summary>
/// Vehicle specific region for improved pathing
/// </summary>
public sealed class VehicleRegion : IPoolable
{
  public const int ChunkSize = 12;

  public RegionType type;

  private VehicleDef vehicleDef;
  private int referenceCount;

  private VehicleRegionGrid regionGrid;

  private readonly List<VehicleRegionLink> links = [];
  private readonly object linksLock = new();

  public ThreadLocal<uint[]>
    closedIndex = new(() => new uint[VehicleRegionTraverser.WorkerCount]);

  // TODO 1.7 - Change access modifiers, these should not be modifiable outside of this class
  public CellRect extentsClose;
  public CellRect extentsLimit;

  public bool touchesMapEdge;
  public bool valid = true;

  public uint reachedIndex;
  public int newRegionGroupIndex = -1;
  public int mark;

  private int debugMakeTick = -1000;

  public VehicleRegion()
  {
#if DEBUG
    ObjectCounter.Increment<VehicleRegion>();
#endif
  }

  public int Id { get; private set; }

  /// <summary>
  /// Debug draw is &lt; 1 second old
  /// </summary>
  private bool DebugIsNew => debugMakeTick > Find.TickManager.TicksGame - 60;

  private int ReferenceCount => referenceCount;

  /// <summary>
  /// Region is currently inside the object pool and should not be referenced unless
  /// in the context of fetching this region from the object pool for renewal.
  /// </summary>
  public bool InPool { get; set; }

  /// <summary>
  /// Object pool to return to when this region's ref count reaches 0.
  /// </summary>
  internal IObjectPool<VehicleRegion> ObjectPool { get; set; }

  // NOTE - Only used for seed generation, doesn't matter if list count is stale at time of
  // reading. No need for a lock here since List<T>::_size does not access the internal array.
  // ReSharper disable once InconsistentlySynchronizedField
  public int LinksCount => links.Count;

  /// <summary>
  /// Gets the type of region grid this region belongs to.
  /// </summary>
  /// <remarks>The grid type determines how regions are stored and accessed within the manager.</remarks>
  public RegionGridType GridType
  {
    get;
    private set
    {
      if (field == value)
        return;

      field = value;
      if (field == RegionGridType.Invalid)
      {
        regionGrid = null;
        return;
      }
      regionGrid = PathingManager.GetRegionGridManager(vehicleDef)[GridType];
    }
  } = RegionGridType.Invalid;

  /// <summary>
  /// Fetch a pooled List object and copy all link references over to the list snapshot.
  /// <para/>
  /// Allows for thread-safe enumeration of a region's links without interrupting region
  /// updating.
  /// <para/>
  /// Should be used with dispose pattern to allow for List object to be returned
  /// to async object pool after ListSnapshot goes out of scope.
  /// </summary>
  public ListSnapshot<VehicleRegionLink> Links
  {
    get
    {
      lock (linksLock)
      {
        return new ListSnapshot<VehicleRegionLink>(links);
      }
    }
  }

  /// <summary>
  /// Get the current map this region belongs to.
  /// </summary>
  /// <remarks></remarks>
  public Map Map { get; private set; }

  /// <summary>
  /// Gets the current pathing manager this region belongs to.
  /// </summary>
  public IPathingManager PathingManager
  {
    get;
    private set
    {
      if (field == value)
        return;

      field = value;
      if (field == null)
      {
        Map = null;
        return;
      }
      Map = PathingManager.Map;
    }
  }

  public int CellCount { get; private set; }

  /// <summary>
  /// Yield all cells in the region
  /// </summary>
  public IEnumerable<IntVec3> Cells
  {
    get
    {
      Assert.IsFalse(InPool, "Should never be enumerating cells while pooled.");
      Assert.IsNotNull(regionGrid, "Should never be enumerating cells while invalid.");
      for (int z = extentsClose.minZ; z <= extentsClose.maxZ; z++)
      {
        for (int x = extentsClose.minX; x <= extentsClose.maxX; x++)
        {
          IntVec3 cell = new(x, 0, z);
          if (regionGrid.GetRegionAt(cell) == this)
          {
            yield return cell;
          }
        }
      }
    }
  }

  /// <summary>
  /// Get neighboring regions
  /// </summary>
  private IEnumerable<VehicleRegion> Neighbors
  {
    get
    {
      lock (linksLock)
      {
        foreach (VehicleRegionLink link in links)
        {
          if (link.regionA != null && link.regionA != this && link.regionA.valid)
            yield return link.regionA;

          if (link.regionB != null && link.regionB != this && link.regionB.valid)
            yield return link.regionB;
        }
      }
    }
  }

  /// <summary>
  /// Get neighboring regions of the same region type
  /// </summary>
  internal IEnumerable<VehicleRegion> NeighborsOfSameType
  {
    get
    {
      lock (linksLock)
      {
        foreach (VehicleRegionLink link in links)
        {
          if (link.regionA != null && link.regionA != this && link.regionA.type == type &&
            link.regionA.valid)
            yield return link.regionA;

          if (link.regionB != null && link.regionB != this && link.regionB.type == type &&
            link.regionB.valid)
            yield return link.regionB;
        }
      }
    }
  }

  /// <summary>
  /// Get room associated with this region
  /// </summary>
  public VehicleRoom Room
  {
    get;
    set
    {
      if (value == field)
        return;

      field?.RemoveRegion(this);
      field = value;
      field?.AddRegion(this);
    }
  }

  /// <summary>
  /// Get random cell in this region
  /// </summary>
  public IntVec3 RandomCell
  {
    get
    {
      for (int i = 0; i < 1000; i++)
      {
        IntVec3 randomCell = extentsClose.RandomCell;
        if (regionGrid.GetRegionAt(randomCell) == this)
        {
          return randomCell;
        }
      }

      return AnyCell;
    }
  }

  /// <summary>
  /// Get any cell in this region
  /// </summary>
  public IntVec3 AnyCell
  {
    get
    {
      foreach (IntVec3 cell in extentsClose)
      {
        if (regionGrid.GetRegionAt(cell) == this)
          return cell;
      }
      Log.Error("Couldn't find any cell in region " + ToString());
      return extentsClose.RandomCell;
    }
  }

  internal void Init(IPathingManager pathing, VehicleDef def, int id, RegionGridType gridType)
  {
    vehicleDef = def;
    Id = id;

    CellCount = 0;
    debugMakeTick = Find.TickManager.TicksGame;
    type = RegionType.Normal;
    extentsClose = CellRect.Empty;
    extentsLimit = CellRect.Empty;
    touchesMapEdge = false;
    valid = gridType != RegionGridType.Invalid;
    reachedIndex = 0;
    newRegionGroupIndex = -1;

    PathingManager = pathing;
    GridType = gridType;
  }

  public void IncrementRefCount()
  {
    Interlocked.Increment(ref referenceCount);
  }

  public void DecrementRefCount()
  {
    Interlocked.Decrement(ref referenceCount);
    if (ReferenceCount == 0)
    {
      ObjectPool?.Return(this);
    }
  }

  public void AddLink(VehicleRegionLink regionLink)
  {
    lock (linksLock)
    {
      links.Add(regionLink);
    }
  }

  public void Reset()
  {
    // Even though RegionMaker and its regions are per-map, we still need to clear
    // the map and vehicleDef references. This may have gone to buffer and be picked
    // up for a different vehicle on a different map.
    valid = false;
    Room = null;
    CellCount = 0;
    referenceCount = 0;
    extentsClose = CellRect.Empty;
    extentsLimit = CellRect.Empty;

    GridType = RegionGridType.Invalid;
    ClearLinks();
  }

  internal void CountCells()
  {
    // Need to use separate counter w/ atomic primitive assign, regions may be accessed from other threads.
    int cellCount = 0;
    for (int z = extentsClose.minZ; z <= extentsClose.maxZ; z++)
    {
      for (int x = extentsClose.minX; x <= extentsClose.maxX; x++)
      {
        IntVec3 cell = new(x, 0, z);
        if (regionGrid.GetRegionAt(cell) == this)
        {
          cellCount++;
        }
      }
    }
    CellCount = cellCount;
  }

  private void ClearLinks()
  {
    lock (linksLock)
    {
      links.Clear();
    }
  }

  /// <summary>
  /// Doesn't take movement ticks into account
  /// </summary>
  // TODO 1.7 - Remove
  [Obsolete("Will be removed in 1.7"), PublicAPI]
  public static int EuclideanDistance(IntVec3 cell, VehicleRegionLink link)
  {
    IntVec3 diff = cell - link.anchor;
    return Mathf.RoundToInt(Mathf.Sqrt(Mathf.Pow(diff.x, 2) + Mathf.Pow(diff.z, 2)));
  }

  /// <summary>
  /// <paramref name="traverseParms"/> allows this region
  /// </summary>
  public bool Allows(in TraverseParms traverseParms)
  {
    return traverseParms.mode switch
    {
      TraverseMode.PassAllDestroyableThings => true,
      TraverseMode.PassAllDestroyableThingsNotWater => true,
      TraverseMode.PassAllDestroyablePlayerOwnedThings => true,
      _ => type.Passable()
    };
  }

  /// <summary>
  /// String output
  /// </summary>
  public override string ToString()
  {
    return $"VehicleRegion_{Id}";
  }

  /// <summary>
  /// Debug draw field edges of this region
  /// </summary>
  // TODO 1.7 - Remove
  [Obsolete("Use DebugDraw overload with DebugRegionType instead."), PublicAPI]
  public void DebugDraw()
  {
    GenDraw.DrawFieldEdges(Cells.ToList(), new Color(0f, 0f, 1f, 0.5f));
  }

  /// <summary>
  /// Debug draw region when mouse is over
  /// </summary>
  public void DebugDraw(DebugRegionType debugRegionType)
  {
    Color color;
    if (!valid)
    {
      color = Color.red;
    }
    else if (DebugIsNew)
    {
      color = Color.yellow;
    }
    else if (!type.Passable())
    {
      color = ColorLibrary.Orange;
    }
    else
    {
      color = Color.green;
    }

    if ((debugRegionType & DebugRegionType.Regions) != 0)
    {
      GenDraw.DrawFieldEdges(Cells.ToList(), color);
      foreach (VehicleRegion region in Neighbors)
      {
        GenDraw.DrawFieldEdges(region.Cells.ToList(), Color.grey);
      }
    }

    if ((debugRegionType & DebugRegionType.Links) != 0)
    {
      using ListSnapshot<VehicleRegionLink> linksSnapshot = Links;
      foreach (VehicleRegionLink regionLink in linksSnapshot)
      {
        // Flash every other second
        if (Mathf.RoundToInt(Time.realtimeSinceStartup * 2f) % 2 == 1)
        {
          Material mat = DebugSolidColorMats.MaterialOf(new Color(1f, 0, 1f, 0.25f));
          List<IntVec3> cells = regionLink.span.Cells.ToList();
          foreach (IntVec3 cell in cells)
          {
            CellRenderer.RenderCell(cell, mat);
          }
          GenDraw.DrawFieldEdges(cells, Color.white);
        }
      }
    }

    if ((debugRegionType & DebugRegionType.EdgeTouch) != 0)
    {
      Material mat = DebugSolidColorMats.MaterialOf(touchesMapEdge ?
        new Color(1, 1, 0, 0.25f) :
        new Color(1, 0, 0, 0.25f));
      List<IntVec3> cells = Cells.ToList();
      foreach (IntVec3 cell in cells)
      {
        CellRenderer.RenderCell(cell, mat);
      }
    }
  }

  /// <summary>
  /// Debug draw region path costs when mouse is over
  /// </summary>
  public void DebugOnGUIMouseover(DebugRegionType debugRegionType)
  {
    if ((debugRegionType & DebugRegionType.PathCosts) != 0)
    {
      if (Find.CameraDriver.CurrentZoom <= CameraZoomRange.Close)
      {
        foreach (IntVec3 intVec in Cells)
        {
          Vector2 vector = intVec.ToUIPosition();
          Rect rect = new(vector.x - 20f, vector.y - 20f, 40f, 40f);
          if (new Rect(0f, 0f, UI.screenWidth, UI.screenHeight).Overlaps(rect))
          {
            var pathCost = PathingManager.GetPathGrid(DebugHelper.Local.VehicleDef).PerceivedPathCostAt(intVec);
            Widgets.Label(rect, pathCost.ToString());
          }
        }
      }
    }
    else if ((debugRegionType & DebugRegionType.References) != 0)
    {
      if (Find.CameraDriver.CurrentZoom <= CameraZoomRange.Close)
      {
        IntVec3 cell = new(extentsClose.minX, 0, extentsClose.minZ);
        Vector2 vector = cell.ToUIPosition();
        Rect rect = new(vector.x - 20f, vector.y - 20f, 40f, 40f);
        if (new Rect(0f, 0f, UI.screenWidth, UI.screenHeight).Overlaps(rect))
        {
          Widgets.Label(rect, ReferenceCount.ToString());
        }
      }
    }
  }

  public static CellRect ChunkAt(IntVec3 cell)
  {
    return new CellRect
    {
      minX = cell.x - cell.x % ChunkSize,
      maxX = cell.x + ChunkSize - (cell.x + ChunkSize) % ChunkSize - 1,
      minZ = cell.z - cell.z % ChunkSize,
      maxZ = cell.z + ChunkSize - (cell.z + ChunkSize) % ChunkSize - 1
    };
  }
}