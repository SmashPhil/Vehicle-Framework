using System.Collections.Generic;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using Vehicles.AI;


namespace Vehicles
{
	public static class PathingHelper
	{
		private static readonly Dictionary<ThingDef, List<VehicleDef>> regionEffecters = new Dictionary<ThingDef, List<VehicleDef>>();

		/// <summary>
		/// Register <paramref name="thingDef"/> as a potential object that will effect vehicle regions
		/// </summary>
		/// <param name="thingDef"></param>
		public static void RegisterRegionEffecter(ThingDef thingDef)
		{
			regionEffecters.Add(thingDef, new List<VehicleDef>());
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefs)
			{
				if (vehicleDef.properties.customThingCosts.TryGetValue(thingDef, out int value))
				{
					if (value < 0 || value >= VehiclePathGrid.ImpassableCost)
					{
						regionEffecters[thingDef].Add(vehicleDef);
					}
				}
				else if (thingDef.AffectsRegions)
				{
					regionEffecters[thingDef].Add(vehicleDef);
				}
			}
		}

		/// <summary>
		/// Notify <paramref name="thing"/> has been spawned. Mark regions dirty if <paramref name="thing"/> affects passability
		/// </summary>
		/// <param name="thing"></param>
		/// <param name="map"></param>
		public static void ThingAffectingRegionsSpawned(Thing thing, Map map)
		{
			if (regionEffecters.TryGetValue(thing.def, out List<VehicleDef> vehicleDefs))
			{
				VehicleMapping mapping = map.GetCachedMapComponent<VehicleMapping>();
				foreach (VehicleDef vehicleDef in vehicleDefs)
				{
					mapping[vehicleDef].VehicleRegionDirtyer.Notify_ThingAffectingRegionsSpawned(thing);
				}
			}
		}

		/// <summary>
		/// Notify <paramref name="thing"/> has been despawned. Mark regions dirty if <paramref name="thing"/> affects passability
		/// </summary>
		/// <param name="thing"></param>
		/// <param name="map"></param>
		public static void ThingAffectingRegionsDeSpawned(Thing thing, Map map)
		{
			if (regionEffecters.TryGetValue(thing.def, out List<VehicleDef> vehicleDefs))
			{
				VehicleMapping mapping = map.GetCachedMapComponent<VehicleMapping>();
				foreach (VehicleDef vehicleDef in vehicleDefs)
				{
					mapping[vehicleDef].VehicleRegionDirtyer.Notify_ThingAffectingRegionsDespawned(thing);
				}
			}
		}

		/// <summary>
		/// Check if cell is currently claimed by a vehicle
		/// </summary>
		/// <param name="map"></param>
		/// <param name="cell"></param>
		public static bool VehicleInCell(Map map, IntVec3 cell)
		{
			return map.GetCachedMapComponent<VehiclePositionManager>().PositionClaimed(cell);
		}

		/// <see cref="VehicleInCell(Map, IntVec3)"/>
		public static bool VehicleInCell(Map map, int x, int z)
		{
			return VehicleInCell(map, new IntVec3(x, 0, z));
		}

		/// <summary>
		/// Calculate angle of Vehicle
		/// </summary>
		/// <param name="pawn"></param>
		public static float CalculateAngle(this VehiclePawn vehicle, out bool northSouthRotation)
		{
			northSouthRotation = false;
			if (vehicle is null) return 0f;
			if (vehicle.vPather.Moving)
			{
				IntVec3 c = vehicle.vPather.nextCell - vehicle.Position;
				if (c.x > 0 && c.z > 0)
				{
					vehicle.Angle = -45f;
				}
				else if (c.x > 0 && c.z < 0)
				{
					vehicle.Angle = 45f;
				}
				else if (c.x < 0 && c.z < 0)
				{
					vehicle.Angle = -45f;
				}
				else if (c.x < 0 && c.z > 0)
				{
					vehicle.Angle = 45f;
				}
				else
				{
					vehicle.Angle = 0f;
				}
			}
			if (vehicle.VehicleGraphic.EastDiagonalRotated && (vehicle.FullRotation == Rot8.NorthEast || vehicle.FullRotation == Rot8.SouthEast) ||
				(vehicle.VehicleGraphic.WestDiagonalRotated && (vehicle.FullRotation == Rot8.NorthWest || vehicle.FullRotation == Rot8.SouthWest)))
			{
				northSouthRotation = true;
			}
			return vehicle.Angle;
		}
	}
}
