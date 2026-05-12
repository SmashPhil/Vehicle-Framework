using JetBrains.Annotations;
using Verse;

namespace Vehicles;

/// <summary>
/// Pathfinding-related data manager
/// </summary>
[PublicAPI]
public interface IPathingManager
{
  /// <summary>
  /// Local Map this manager handles path behavior for.
  /// </summary>
  Map Map { get; }

  /// <summary>
  /// Registry for shared ownership of grids.
  /// </summary>
  MapGridOwners GridOwners { get; }

  /// <summary>
  /// Determines whether path data processing is currently suspended for the <paramref name="vehicleDef"/>.
  /// </summary>
  /// <param name="vehicleDef">The <see cref="VehicleDef"/> to check for suspended path data.</param>
  /// <returns><see langword="true"/> if path data is suspended, <see langword="false"/> if otherwise.</returns>
  bool IsPathDataSuspended([NotNull] VehicleDef vehicleDef);

  // TODO 1.7 - Decouple from an explicit path grid, right now too many systems depend on this relationship.
  VehiclePathGrid GetPathGrid(VehicleDef vehicleDef);

  // TODO 1.7 - Decouple from an explicit grid manager, right now too many systems depend on this relationship.
  VehicleRegionGridManager GetRegionGridManager(VehicleDef vehicleDef);
}