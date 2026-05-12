using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CoreLib.Performance;
using JetBrains.Annotations;
using SmashTools;
using Verse;

namespace Vehicles;

/// <summary>
/// Vehicle specific room handler
/// </summary>
[PublicAPI]
public sealed class VehicleRoom
{
  private static int nextRoomID;

  private RegionGridType gridType;

  private readonly VehicleDef vehicleDef;

  private int cellCount = -1;
  private int numRegionsTouchingMapEdge;

  public VehicleRoom(VehicleDef vehicleDef)
  {
    this.vehicleDef = vehicleDef;
#if DEBUG
    ObjectCounter.Increment<VehicleRoom>();
#endif
  }

  public int Id { get; init; }

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

  /// <summary>
  /// Region type with fallback
  /// </summary>
  public RegionType RegionType =>
    Regions.Count == 0 ? RegionType.None : Regions.FirstOrDefault().Key.type;

  /// <summary>
  /// Region getter for regions contained within room
  /// </summary>
  public ConcurrentSet<VehicleRegion> Regions { get; } = [];

  /// <summary>
  /// Region count
  /// </summary>
  public int RegionCount => Regions.Count;

  /// <summary>
  /// Room touches map edge
  /// </summary>
  public bool TouchesMapEdge => numRegionsTouchingMapEdge > 0;

  private IEnumerable<IntVec3> Cells
  {
    get
    {
      foreach (VehicleRegion region in Regions.Keys)
      {
        foreach (IntVec3 cell in region.Cells)
        {
          yield return cell;
        }
      }
    }
  }

  public int CellCount
  {
    get
    {
      if (cellCount < 0)
      {
        cellCount = 0;
        foreach (VehicleRegion region in Regions.Keys)
        {
          cellCount += region.CellCount;
        }
      }
      return cellCount;
    }
  }

  /// <summary>
  /// Create new room for <paramref name="vehicleDef"/>
  /// </summary>
  internal static VehicleRoom MakeNew(IPathingManager pathing, VehicleDef vehicleDef, RegionGridType gridType)
  {
    int id = Interlocked.CompareExchange(ref nextRoomID, 0, 0);
    VehicleRoom room = new(vehicleDef)
    {
      Id = id,
      gridType = gridType,
      PathingManager = pathing
    };
    Interlocked.Increment(ref nextRoomID);
    return room;
  }

  /// <summary>
  /// Add region to room
  /// </summary>
  /// <param name="region"></param>
  public void AddRegion(VehicleRegion region)
  {
    if (Regions.ContainsKey(region))
    {
      Log.Error($"Tried to add the same region twice to Room. region={region} room={this}");
      return;
    }
    Regions.Add(region);
    cellCount = -1;
    if (region.touchesMapEdge)
    {
      numRegionsTouchingMapEdge++;
    }
    if (Regions.Count == 1)
    {
      PathingManager.GetRegionGridManager(vehicleDef)[gridType].allRooms.Add(this);
    }
  }

  /// <summary>
  /// Remove region from room
  /// </summary>
  public void RemoveRegion(VehicleRegion region)
  {
    if (!Regions.ContainsKey(region))
    {
      Log.Warning(
        $"Tried to remove region from Room but this region is not here. region={region} room={this}");
      return;
    }
    Regions.Remove(region);
    cellCount = -1;
    if (region.touchesMapEdge)
    {
      numRegionsTouchingMapEdge--;
    }
    if (Regions.Count == 0)
    {
      VehiclePathingSystem mapping = MapComponentCache<VehiclePathingSystem>.GetComponent(Map);
      mapping?[vehicleDef].VehicleRegionGridManager[gridType].allRooms.Remove(this);
    }
  }

  internal void DebugDraw(DebugRegionType debugRegionType)
  {
    if ((debugRegionType & DebugRegionType.Rooms) != 0)
    {
      float color = Rand.ValueSeeded(GetHashCode());
      foreach (IntVec3 cell in Cells)
      {
        CellRenderer.RenderCell(cell, color);
      }
    }
  }

  /// <summary>
  /// ID based hashcode
  /// </summary>
  /// <returns></returns>
  public override int GetHashCode()
  {
    return Gen.HashCombineInt(Id, vehicleDef.GetHashCode());
  }
}