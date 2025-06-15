using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SmashTools;
using Verse;

namespace Vehicles;

/// <summary>
/// Reservation manager for positions of vehicle, reserves entire hitbox of vehicle
/// </summary>
/// <remarks>Only ever read / written to from MainThread</remarks>
public class VehiclePositionManager : DetachedMapComponent
{
  private readonly ConcurrentDictionary<IntVec3, VehiclePawn> occupiedCells = [];
  private readonly ConcurrentDictionary<VehiclePawn, CellRect> occupiedRects = [];

  public VehiclePositionManager(Map map) : base(map)
  {
  }

  public IEnumerable<VehiclePawn> AllClaimants => occupiedRects.Keys;

  public bool PositionClaimed(IntVec3 cell)
  {
    return ClaimedBy(cell) != null;
  }

  public VehiclePawn ClaimedBy(IntVec3 cell)
  {
    return occupiedCells.TryGetValue(cell);
  }

  public CellRect ClaimedBy(VehiclePawn vehicle)
  {
    return occupiedRects.TryGetValue(vehicle);
  }

  public void ClaimPosition(VehiclePawn vehicle)
  {
    ReleaseClaimed(vehicle);
    CellRect occupiedRect = vehicle.VehicleRect();
    occupiedRects[vehicle] = occupiedRect;
    foreach (IntVec3 cell in occupiedRect)
    {
      occupiedCells[cell] = vehicle;
    }

    vehicle.RecalculateFollowerCell();
    if (ClaimedBy(vehicle.FollowerCell) is { } blockedVehicle)
    {
      blockedVehicle.RecalculateFollowerCell();
    }
  }

  public void ReleaseClaimed(VehiclePawn vehicle)
  {
    if (occupiedRects.TryGetValue(vehicle, out CellRect rect))
    {
      foreach (IntVec3 cell in rect)
      {
        occupiedCells.TryRemove(cell, out _);
      }
    }
    occupiedRects.TryRemove(vehicle, out _);
  }
}