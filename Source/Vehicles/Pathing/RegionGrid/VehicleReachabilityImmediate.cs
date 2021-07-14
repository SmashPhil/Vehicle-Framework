using Verse;
using Verse.AI;


namespace Vehicles.AI
{
	/// <summary>
	/// Quick check reachability methods
	/// </summary>
	public static class VehicleReachabilityImmediate
	{
		/// <summary>
		/// Quick check for <paramref name="vehicle"/> reachability between <paramref name="start"/> and <paramref name="target"/>
		/// </summary>
		/// <param name="start"></param>
		/// <param name="target"></param>
		/// <param name="map"></param>
		/// <param name="peMode"></param>
		/// <param name="vehicle"></param>
		public static bool CanReachImmediateVehicle(IntVec3 start, LocalTargetInfo target, Map map, VehicleDef vehicleDef, PathEndMode peMode)
		{
			if (!target.IsValid) return false;
			target = (LocalTargetInfo)GenPathVehicles.ResolvePathMode(vehicleDef, map, target.ToTargetInfo(map), ref peMode);
			if (!target.HasThing || target.Thing.def.size.x == 1 && target.Thing.def.size.z == 1)
			{
				if (start == target.Cell) return true;
			}
			else if (start.IsInside(target.Thing))
			{
				return true;
			}
			return peMode == PathEndMode.Touch && TouchPathEndModeUtilityVehicles.IsAdjacentOrInsideAndAllowedToTouch(start, target, map, vehicleDef);
		}

		/// <summary>
		/// Quick check for <paramref name="vehicle"/> reachability
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="target"></param>
		/// <param name="peMode"></param>
		public static bool CanReachImmediateVehicle(this VehiclePawn vehicle, LocalTargetInfo target, PathEndMode peMode)
		{
			return vehicle.Spawned && CanReachImmediateVehicle(vehicle.Position, target, vehicle.Map, vehicle.VehicleDef, peMode);
		}

		/// <summary>
		/// Quick check for <paramref name="vehicle"/> reachability with non-local constraints
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="target"></param>
		/// <param name="peMode"></param>
		public static bool CanReachImmediateNonLocalVehicle(this VehiclePawn vehicle, TargetInfo target, PathEndMode peMode)
		{
			return vehicle.Spawned && (target.Map is null || target.Map == vehicle.Map) && vehicle.CanReachImmediateVehicle((LocalTargetInfo)target, peMode);
		}

		/// <summary>
		/// Quick check for <paramref name="vehicle"/> reachability with destination <paramref name="rect"/>
		/// </summary>
		/// <param name="start"></param>
		/// <param name="rect"></param>
		/// <param name="map"></param>
		/// <param name="peMode"></param>
		/// <param name="vehicle"></param>
		public static bool CanReachImmediateVehicle(IntVec3 start, CellRect rect, Map map, PathEndMode peMode, VehiclePawn vehicle)
		{
			IntVec3 c = rect.ClosestCellTo(start);
			return CanReachImmediateVehicle(start, c, map, vehicle.VehicleDef, peMode);
		}
	}
}
