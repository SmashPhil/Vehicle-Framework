using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Annotations;
using SmashTools;
using SmashTools.Burst;
using Unity.Collections;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

/// <summary>
/// Reservation manager for positions of vehicle, reserves entire hitbox of vehicle
/// </summary>
/// <remarks>
/// <para>
/// <b>Thread-safety:</b> Reads are thread-safe; updates to claims <em>must</em> be performed on the main thread.</para>
/// </remarks>
[PublicAPI]
public class VehiclePositionManager : DetachedMapComponent
{
  private const string PositionId = "VehiclePosition";

  private const int VehicleCostOffset = VehiclePathGrid.ImpassableCost;

  private readonly ConcurrentDictionary<IntVec3, VehiclePawn> occupiedCells = [];
  private readonly ConcurrentDictionary<VehiclePawn, CellRect> occupiedRects = [];

  private readonly VehiclePathingSystem pathingSystem;

  private NativeArray<int> thingIdGrid;

  // Read / written from the main thread. Only position claim status is accessed concurrently.
  private readonly List<VehiclePawn> claimants = [];

  public VehiclePositionManager(Map map) : base(map)
  {
    pathingSystem = map.GetCachedMapComponent<VehiclePathingSystem>();
    thingIdGrid = new NativeArray<int>(map.Size.x * map.Size.z, Allocator.Persistent,
      options: NativeArrayOptions.UninitializedMemory);
    NativeArrayUtility.SetAll(thingIdGrid, 0xFF);
  }

  internal NativeArray<int>.ReadOnly ThingIdGrid => thingIdGrid.AsReadOnly();

  /// <summary>
  /// Gets the list of vehicles that are tracking claim state via this manager.
  /// </summary>
  /// <remarks>
  /// Access and mutation are expected on the main thread. This collection is provided for
  /// hot-path access patterns and is <b>not</b> synchronized.
  /// <para>Main-thread only.</para>
  /// </remarks>
  public List<VehiclePawn> AllClaimants
  {
    get
    {
      Assert.IsTrue(UnityData.IsInMainThread);
      return claimants;
    }
  }

  /// <summary>
  /// Determines whether the <see cref="cell"/> is claimed by any vehicle's occupied rect.
  /// </summary>
  /// <param name="cell">The cell to check.</param>
  /// <returns><see langword="true"/> if the cell is claimed; otherwise, <see langword="false"/>.</returns>
  /// <remarks>Safe to call concurrently from worker threads.</remarks>
  public bool PositionClaimed(IntVec3 cell)
  {
    return ClaimedBy(cell) != null;
  }

  /// <summary>
  /// Gets the current claimant of <see cref="cell"/>.
  /// </summary>
  /// <param name="cell">The cell to check.</param>
  /// <returns>
  /// The <see cref="Vehicles.VehiclePawn"/> that claims the cell; otherwise <see langword="null"/> if unclaimed.
  /// </returns>
  /// <remarks>Safe to call concurrently from worker threads.</remarks>
  public VehiclePawn ClaimedBy(IntVec3 cell)
  {
    return occupiedCells.TryGetValue(cell);
  }

  /// <summary>
  /// Gets the current occupied rect claimed by the <paramref name="vehicle"/>.
  /// </summary>
  /// <param name="vehicle">The vehicle to check.</param>
  /// <returns>
  /// The <see cref="Verse.CellRect"/> claimed by <paramref name="vehicle"/>; if the vehicle has no active claim,
  /// returns a default CellRect (empty).
  /// </returns>
  /// <remarks>Safe to call concurrently from worker threads.</remarks>
  public CellRect ClaimedBy(VehiclePawn vehicle)
  {
    return occupiedRects.TryGetValue(vehicle);
  }

  /// <summary>
  /// Claims the <paramref name="vehicle"/>'s current hitbox.
  /// </summary>
  /// <param name="vehicle">The vehicle whose current footprint should be claimed.</param>
  /// <remarks>Main-thread only.</remarks>
  public void ClaimPosition(VehiclePawn vehicle)
  {
    ClaimPosition(vehicle, vehicle.Position, vehicle.Rotation);
  }

  /// <summary>
  /// Claims the <paramref name="vehicle"/>'s hitbox at <paramref name="cell"/> given rotation <paramref name="rot"/> and updates internal occupancy maps.
  /// </summary>
  /// <param name="vehicle">The vehicle to claim the position for.</param>
  /// <param name="cell">The cell to calculate the full hitbox claim at.</param>
  /// <param name="rot">The rotation of the vehicle for calculating the full hitbox claim.</param>
  /// <remarks>Main-thread only.</remarks>
  /// <exception cref="UnityEngine.Assertions.AssertionException">Thrown if invoked off the main thread.</exception>
  public void ClaimPosition(VehiclePawn vehicle, IntVec3 cell, Rot4 rot)
  {
    // NOTE - Updating position manager is done from VehiclePathFollower. This allows us to have a thread-unsafe list
    // accessor for hot paths. Reading claims can be done concurrently, but writing must be from the main thread.
    Assert.IsTrue(UnityData.IsInMainThread);
    ReleaseClaimed(vehicle);
    CellRect occupiedRect = vehicle.VehicleRect(cell, rot);
    occupiedRects[vehicle] = occupiedRect;
    CellIndices indices = map.cellIndices;
    foreach (IntVec3 occupiedCell in occupiedRect)
    {
      if (occupiedCells.TryAdd(occupiedCell, vehicle))
      {
        thingIdGrid[indices.CellToIndex(occupiedCell)] = vehicle.thingIDNumber;
      }
    }
    claimants.Add(vehicle);
    if (vehicle.Spawned)
    {
      vehicle.RecalculateFollowerCell();
      if (ClaimedBy(vehicle.FollowerCell) is { } blockedVehicle)
      {
        blockedVehicle.RecalculateFollowerCell();
      }
    }
  }

  /// <summary>
  /// Releases any active claim for the specified <paramref name="vehicle"/>.
  /// </summary>
  /// <param name="vehicle">The vehicle whose claim should be released.</param>
  /// <remarks>Main-thread only.</remarks>
  /// <exception cref="UnityEngine.Assertions.AssertionException">Thrown if invoked off the main thread.</exception>
  public void ReleaseClaimed(VehiclePawn vehicle)
  {
    Assert.IsTrue(UnityData.IsInMainThread);
    if (occupiedRects.TryGetValue(vehicle, out CellRect rect))
    {
      CellIndices indices = map.cellIndices;
      foreach (IntVec3 cell in rect)
      {
        if (occupiedCells.TryGetValue(cell, out VehiclePawn claimant) && claimant != vehicle)
        {
          // NOTE: When diagonal hitboxes are converted away from improperly mapped horizontals, we can
          // assert against overlapping claims. But because of the inconsistency with diagonal turns and
          // rotating on the first node, overlapping claims are both possible and expected.
          continue;
        }
        if (occupiedCells.TryRemove(cell, out _))
        {
          thingIdGrid[indices.CellToIndex(cell)] = -1;
        }
      }
    }
    occupiedRects.TryRemove(vehicle, out _);
    claimants.Remove(vehicle);
  }
}