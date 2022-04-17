using RimWorld;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Path related utility methods for vehicle
	/// </summary>
	public static class GenPathVehicles
	{
		/// <summary>
		/// Determine and resolve PathMode for <paramref name="vehicle"/> following certain conditions
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="dest"></param>
		/// <param name="peMode"></param>
		public static TargetInfo ResolvePathMode(VehicleDef vehicleDef, Map map, TargetInfo dest, ref PathEndMode peMode)
		{
			if (dest.HasThing && dest.Thing.Spawned)
			{
				peMode = PathEndMode.Touch;
				return dest;
			}
			if (peMode == PathEndMode.InteractionCell)
			{
				if(!dest.HasThing)
				{
					Log.Error("Pathed to cell " + dest + " with PathEndMode.InteractionCell.");
				}
				peMode = PathEndMode.OnCell;
				return new TargetInfo(dest.Thing.InteractionCell, dest.Thing.Map, false);
			}
			if (peMode == PathEndMode.ClosestTouch)
			{
				peMode = ResolveClosestTouchPathMode(vehicleDef, map, dest.Cell);
			}
			return dest;
		}

		/// <summary>
		/// PathMode given traversability of <paramref name="target"/>
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="map"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		public static PathEndMode ResolveClosestTouchPathMode(VehicleDef vehicleDef, Map map, IntVec3 target)
		{
			if (ShouldNotEnterCell(vehicleDef, map, target))
			{
				return PathEndMode.Touch;
			}
			return PathEndMode.OnCell;
		}

		/// <summary>
		/// Determine if <paramref name="vehicle"/> is not able to traverse <paramref name="dest"/>
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="map"></param>
		/// <param name="dest"></param>
		private static bool ShouldNotEnterCell(VehicleDef vehicleDef, Map map, IntVec3 dest)
		{
			if (map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehiclePathGrid.PerceivedPathCostAt(dest) > 30)
			{
				return true;
			}
			if (!GenGridVehicles.Walkable(dest, vehicleDef, map))
			{
				return true;
			}
			return false;
		}
	}
}
