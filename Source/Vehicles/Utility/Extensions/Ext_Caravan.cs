using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using RimWorld.Planet;
using SmashTools;
using Verse;

namespace Vehicles.World;

/// <summary>
/// Extension methods for <see cref="Caravan"/> and related world vehicle functionality.
/// </summary>
[PublicAPI]
public static class Ext_Caravan
{
  /// <summary>
  /// Determines whether all vehicles in the caravan are autonomous.
  /// </summary>
  /// <param name="caravan">The vehicle caravan to evaluate.</param>
  /// <returns><see langword="true"/> if every vehicle in the caravan is autonomous, otherwise <see langword="false"/>.</returns>
  public static bool IsAutonomousCaravan([NotNull] this VehicleCaravan caravan)
  {
    foreach (VehiclePawn vehicle in caravan.VehiclesListForReading)
    {
      if (!vehicle.IsAutonomousVehicle())
        return false;
    }
    return true;
  }

  /// <summary>
  /// Determines whether the caravan contains one or more vehicles.
  /// </summary>
  /// <param name="caravan">The caravan to inspect.</param>
  /// <returns><see langword="true"/> if the caravan has at least one vehicle, otherwise <see langword="false"/>.</returns>
  public static bool HasVehicle(this Caravan caravan)
	{
		return caravan is VehicleCaravan vehicleCaravan && vehicleCaravan.VehiclesListForReading.Count > 0;
	}

	/// <summary>
	/// Determines whether the caravan contains one or more boats.
	/// </summary>
	/// <param name="caravan">The caravan to inspect.</param>
	/// <returns><see langword="true"/> if the caravan has at least one boat, otherwise <see langword="false"/>.</returns>
	public static bool HasBoat(this Caravan caravan)
	{
		return caravan is VehicleCaravan vehicleCaravan &&
			vehicleCaravan.VehiclesListForReading.Exists(Ext_Vehicles.IsBoat);
	}

	/// <summary>
	/// Retrieves a set of unique vehicle definitions present in the caravan.
	/// </summary>
	/// <param name="caravan">The caravan from which to extract vehicles.</param>
	/// <returns>
	/// A new <see cref="HashSet{VehicleDef}"/> containing each distinct <see cref="VehicleDef"/> found in the caravan.
	/// </returns>
	public static HashSet<VehicleDef> UniqueVehicleDefsInCaravan(this Caravan caravan)
	{
		HashSet<VehicleDef> vehicleSet = [];
		foreach (Pawn pawn in caravan.PawnsListForReading)
		{
			if (pawn is VehiclePawn vehicle)
				vehicleSet.Add(vehicle.VehicleDef);
		}
		return vehicleSet;
	}

	/// <summary>
	/// Validates whether adding a vehicle to the caravan would maintain world pathing compatibility.
	/// </summary>
	/// <param name="vehicleCaravan">The target vehicle caravan.</param>
	/// <param name="vehicle">The vehicle pawn to test.</param>
	/// <returns>
	/// <see langword="true"/> if the vehicle can join without breaking world pathing rules, otherwise <see langword="false"/>.
	/// </returns>
	public static bool ViableForCaravan(this VehicleCaravan vehicleCaravan, VehiclePawn vehicle)
	{
		foreach (VehiclePawn caravanVehicle in vehicleCaravan.VehiclesListForReading)
		{
			if (!GridOwners.World.MatchingReachability(caravanVehicle.VehicleDef, vehicle.VehicleDef))
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// Retrieves all pawns in the caravan, including those inside vehicles, without throwing on null.
	/// </summary>
	/// <param name="caravan">The caravan to extract pawns from.</param>
	/// <returns>
	/// A <see cref="List{Pawn}"/> of all pawns, or <see langword="null"/> if the caravan is <see langword="null"/> or has no vehicles.
	/// </returns>
	public static List<Pawn> GrabPawnsFromVehicleCaravanSilentFail(this Caravan caravan)
	{
		if (caravan is null || !caravan.HasVehicle())
			return null;

		List<Pawn> vehicles = [];
		foreach (Pawn p in caravan.PawnsListForReading)
		{
			if (p is VehiclePawn vehicle)
			{
				vehicles.AddRange(vehicle.AllPawnsAboard);
			}
			else
			{
				vehicles.Add(p);
			}
		}
		return vehicles;
	}

	/// <summary>
	/// Transfers a pawn or item into the caravan's owner container.
	/// </summary>
	/// <param name="caravan">The caravan receiving the transfer.</param>
	/// <param name="owner">The <see cref="ThingOwner"/> to receive the thing.</param>
	/// <param name="thing">The pawn or item to transfer.</param>
	public static void TransferPawnOrItem(this Caravan caravan, ThingOwner owner, Thing thing)
	{
		if (thing is Pawn)
		{
			owner.TryTransferToContainer(thing, caravan.pawns, canMergeWithExistingStacks: false);
		}
		else
		{
			Pawn giveToPawn =
				CaravanInventoryUtility.FindPawnToMoveInventoryTo(thing, caravan.PawnsListForReading,
					null);
			if (giveToPawn == null || !giveToPawn.inventory.innerContainer.TryAddOrTransfer(thing))
			{
				Log.Error($"Failed to give item {thing} to caravan {caravan}; item was lost.");
				thing.Destroy();
			}
		}
	}

	/// <summary>
	/// Ensures the world vehicle path grid for the given <see cref="VehicleDef"/> is initialized on the global map,
	/// recalculating path costs if necessary.
	/// </summary>
	/// <param name="vehicleDef">
	/// The vehicle def whose world grid should be initialized.
	/// </param>
	public static void EnsureWorldGridInitialized(this VehicleDef vehicleDef)
	{
		WorldVehiclePathGrid pathGrid = Find.World.GetComponent<WorldVehiclePathGrid>();
		if (!pathGrid[vehicleDef].Enabled)
			pathGrid.RecalculateAllPerceivedPathCostsFor(vehicleDef, CancellationToken.None);
	}

	/// <summary>
	/// Ensures that world vehicle path grids are initialized for all vehicle defs in the specified
	/// <see cref="VehicleCaravan"/>, recalculating path costs if necessary.
	/// </summary>
	/// <param name="caravan">
	/// The vehicle caravan whose vehicle defs should have their world path grids initialized.
	/// </param>
	public static void EnsureWorldGridInitialized(this VehicleCaravan caravan)
	{
		foreach (VehiclePawn vehicle in caravan.VehiclesListForReading)
		{
			vehicle.VehicleDef.EnsureWorldGridInitialized();
		}
	}

	/// <summary>
	/// Ensures that the local map pathing grids for the vehicle def are initialized on the given map.
	/// </summary>
	/// <param name="vehicleDef">The vehicle def to initialize grids for urgently.</param>
	/// <param name="map">The <see cref="Map"/> to prepare.
	/// </param>
	public static void EnsureMapInitialized(this VehicleDef vehicleDef, Map map)
	{
		VehiclePathingSystem pathingSystem = map.GetCachedMapComponent<VehiclePathingSystem>();
		if (pathingSystem[vehicleDef].Suspended || !pathingSystem[vehicleDef].VehiclePathGrid.Enabled)
			pathingSystem.RequestGridsFor(vehicleDef, DeferredGridGeneration.Urgency.Urgent);
	}

	/// <summary>
	/// Ensures that the local map pathing grids for the vehicle defs are initialized on the given map.
	/// </summary>
	/// <param name="vehicles">A list of vehicles to initialize grids for urgently.</param>
	/// <param name="map">The <see cref="Map"/> to prepare.
	/// </param>
	public static void EnsureMapInitialized(List<VehiclePawn> vehicles, Map map)
	{
		VehiclePathingSystem pathingSystem = map.GetCachedMapComponent<VehiclePathingSystem>();
		foreach (VehiclePawn vehicle in vehicles)
		{
			VehicleDef vehicleDef = vehicle.VehicleDef;
			if (pathingSystem[vehicleDef].Suspended || !pathingSystem[vehicleDef].VehiclePathGrid.Enabled)
				pathingSystem.RequestGridsFor(vehicleDef, DeferredGridGeneration.Urgency.Urgent);
		}
	}

	/// <summary>
	/// Ensures that the local map pathing grids for all vehicles in the caravan are initialized on the given map.
	/// </summary>
	/// <param name="caravan">The caravan whose vehicles to initialize.</param>
	/// <param name="map">The <see cref="Map"/> to prepare.</param>
	public static void EnsureMapInitialized(this VehicleCaravan caravan, Map map)
	{
		EnsureMapInitialized(caravan.VehiclesListForReading, map);
	}
}