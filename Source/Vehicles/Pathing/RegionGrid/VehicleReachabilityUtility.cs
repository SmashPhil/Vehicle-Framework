using System.Collections.Generic;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
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
		/// <param name="mode"></param>
		public static bool CanReachVehicle(this VehiclePawn vehicle, LocalTargetInfo dest, PathEndMode peMode, Danger maxDanger, TraverseMode mode = TraverseMode.ByPawn)
		{
			if (dest.Cell == vehicle.Position)
			{
				return true;
			}
			return vehicle.Spawned && MapComponentCache<VehicleMapping>.GetComponent(vehicle.Map)[vehicle.VehicleDef].VehicleReachability.CanReachVehicle(vehicle.Position, dest, peMode, 
				TraverseParms.For(vehicle, maxDanger, mode));
		}

		/// <summary>
		/// <paramref name="vehicle"/> can reach <paramref name="dest"/> given parameters
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="dest"></param>
		/// <param name="peMode"></param>
		/// <param name="maxDanger"></param>
		/// <param name="mode"></param>
		public static bool CanReachVehicleNonLocal(this VehiclePawn vehicle, TargetInfo dest, PathEndMode peMode, Danger maxDanger, TraverseMode mode = TraverseMode.ByPawn)
		{
			if (dest.Cell == vehicle.Position)
			{
				return true;
			}
			return vehicle.Spawned && MapComponentCache<VehicleMapping>.GetComponent(vehicle.Map)[vehicle.VehicleDef].VehicleReachability.CanReachVehicleNonLocal(vehicle.Position, dest, peMode, 
				TraverseParms.For(vehicle, maxDanger, mode));
		}

		/// <summary>
		/// <paramref name="vehicle"/> can reach any map edge
		/// </summary>
		/// <param name="vehicle"></param>
		public static bool CanReachVehicleMapEdge(this VehiclePawn vehicle)
		{
			return vehicle.Spawned && MapComponentCache<VehicleMapping>.GetComponent(vehicle.Map)[vehicle.VehicleDef].VehicleReachability.CanReachMapEdge(vehicle.Position, 
				TraverseParms.For(vehicle, Danger.Deadly, TraverseMode.ByPawn));
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
				MapComponentCache<VehicleMapping>.GetComponent(maps[i])[vehicle.VehicleDef].VehicleReachability.ClearCacheFor(vehicle);
			}
		}
	}
}
