using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles.AI
{
	/// <summary>
	/// Grid related method helpers
	/// </summary>
	public static class GenGridVehicles
	{
		/// <summary>
		/// <paramref name="cell"/> is able to be traversed by <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="map"></param>
		public static bool Walkable(this IntVec3 cell, VehicleDef vehicleDef, Map map)
		{
			return map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehiclePathGrid.Walkable(cell);
		}

		/// <summary>
		/// Check if <paramref name="cell"/> is standable on <paramref name="map"/> for <paramref name="pawn"/> unknown to be a <see cref="VehiclePawn"/> or not
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="pawn"></param>
		/// <param name="map"></param>
		/// <returns></returns>
		public static bool StandableUnknown(this IntVec3 cell, Pawn pawn, Map map)
		{
			if (pawn is VehiclePawn vehicle)
			{
				return Standable(cell, vehicle.VehicleDef, map);
			}
			return GenGrid.Standable(cell, map);
		}

		/// <summary>
		/// <paramref name="cell"/> is able to be stood on for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="map"></param>
		public static bool Standable(this IntVec3 cell, VehicleDef vehicleDef, Map map)
		{
			if (!map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehiclePathGrid.Walkable(cell))
			{
				return false;
			}
			List<Thing> list = map.thingGrid.ThingsListAt(cell);
			foreach (Thing t in list)
			{
				if (t.def.passability != Traversability.Standable)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// <paramref name="cell"/> is impassable for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="map"></param>
		public static bool Impassable(IntVec3 cell, Map map, VehicleDef vehicleDef)
		{
			List<Thing> list = map.thingGrid.ThingsListAt(cell);
			foreach (Thing t in list)
			{
				if (vehicleDef.properties.customThingCosts.TryGetValue(t.def, out int value) && (value >= VehiclePathGrid.ImpassableCost || value < 0))
				{
					return true;
				}
				else if (t.def.passability is Traversability.Impassable)
				{
					return true;
				}
			}
			return false;
		}
	}
}
