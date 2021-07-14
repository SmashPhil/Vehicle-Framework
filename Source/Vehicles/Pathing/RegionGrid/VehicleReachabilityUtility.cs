using System.Collections.Generic;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles.AI
{
	/// <summary>
	/// Reachability utility methods
	/// </summary>
	public static class VehicleReachabilityUtility
	{
		/// <summary>
		/// <paramref name="vehicle"/> can reach <paramref name="dest"/> given parameters
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="dest"></param>
		/// <param name="peMode"></param>
		/// <param name="maxDanger"></param>
		/// <param name="canBash"></param>
		/// <param name="mode"></param>
		public static bool CanReachVehicle(this VehiclePawn vehicle, LocalTargetInfo dest, PathEndMode peMode, Danger maxDanger, bool canBash = false, TraverseMode mode = TraverseMode.ByPawn)
		{
			return vehicle.Spawned && vehicle.Map.GetCachedMapComponent<VehicleMapping>()[vehicle.VehicleDef].VehicleReachability.CanReachVehicle(vehicle.Position, dest, peMode, 
				TraverseParms.For(vehicle, maxDanger, mode, canBash));
		}

		/// <summary>
		/// <paramref name="vehicle"/> can reach <paramref name="dest"/> given parameters
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="dest"></param>
		/// <param name="peMode"></param>
		/// <param name="maxDanger"></param>
		/// <param name="canBash"></param>
		/// <param name="mode"></param>
		public static bool CanReachVehicleNonLocal(this VehiclePawn vehicle, TargetInfo dest, PathEndMode peMode, Danger maxDanger, bool canBash = false, TraverseMode mode = TraverseMode.ByPawn)
		{
			return vehicle.Spawned && vehicle.Map.GetCachedMapComponent<VehicleMapping>()[vehicle.VehicleDef].VehicleReachability.CanReachVehicleNonLocal(vehicle.Position, dest, peMode, 
				TraverseParms.For(vehicle, maxDanger, mode, canBash));
		}

		/// <summary>
		/// <paramref name="vehicle"/> can reach any map edge
		/// </summary>
		/// <param name="vehicle"></param>
		public static bool CanReachVehicleMapEdge(this VehiclePawn vehicle)
		{
			return vehicle.Spawned && vehicle.Map.GetCachedMapComponent<VehicleMapping>()[vehicle.VehicleDef].VehicleReachability.CanReachMapEdge(vehicle.Position, 
				TraverseParms.For(vehicle, Danger.Deadly, TraverseMode.ByPawn, false));
		}

		/// <summary>
		/// Clear cache for <paramref name="vehicle"/>
		/// </summary>
		/// <param name="vehicle"></param>
		public static void ClearCacheFor(VehiclePawn vehicle)
		{
			List<Map> maps = Find.Maps;
			for (int i = 0; i < maps.Count; i++)
			{
				maps[i].reachability.ClearCacheFor(vehicle);
				maps[i].GetCachedMapComponent<VehicleMapping>()[vehicle.VehicleDef].VehicleReachability.ClearCacheFor(vehicle);
			}
		}
	}
}
