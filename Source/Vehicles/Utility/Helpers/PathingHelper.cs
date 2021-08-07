using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using RimWorld;
using SmashTools;
using Vehicles.AI;

namespace Vehicles
{
	public static class PathingHelper
	{
		public const string AllowTerrainWithTag = "PassableVehicles";
		public const string DisallowTerrainWithTag = "ImpassableVehicles";

		private static readonly Dictionary<ThingDef, List<VehicleDef>> regionEffecters = new Dictionary<ThingDef, List<VehicleDef>>();

		/// <summary>
		/// VehicleDef , &lt;TerrainDef Tag,pathCost&gt;
		/// </summary>
		public static readonly Dictionary<string, Dictionary<string, int>> allTerrainCostsByTag = new Dictionary<string, Dictionary<string, int>>();

		/// <summary>
		/// Quick retrieval of region updating status
		/// </summary>
		/// <param name="map"></param>
		/// <returns></returns>
		public static bool RegionWorking(Map map) => (bool)AccessTools.Field(typeof(RegionAndRoomUpdater), "working").GetValue(map.regionAndRoomUpdater);

		/// <summary>
		/// Register any <seealso cref="TerrainDef"/>s with tags "PassableVehicles" or "ImpassableVehicles"
		/// </summary>
		public static void LoadTerrainDefaults()
		{
			foreach (TerrainDef terrainDef in DefDatabase<TerrainDef>.AllDefs)
			{
				if (terrainDef.tags.NotNullAndAny())
				{
					if (terrainDef.tags.Contains(AllowTerrainWithTag))
					{
						int pathCost = 1;
						foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefs)
						{
							vehicleDef.properties.customTerrainCosts.TryAdd(terrainDef, pathCost);
						}
					}
					else if (terrainDef.tags.Contains(DisallowTerrainWithTag))
					{
						int pathCost = -1;
						foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefs)
						{
							vehicleDef.properties.customTerrainCosts.TryAdd(terrainDef, pathCost);
						}
					}
				}
			}
		}

		/// <summary>
		/// Parse TerrainDef costs from registry for custom path costs in <seealso cref="VehicleDef"/>
		/// </summary>
		public static void LoadTerrainTagCosts()
		{
			List<TerrainDef> terrainDefs = DefDatabase<TerrainDef>.AllDefsListForReading;
			foreach (var terrainCostFlipper in allTerrainCostsByTag)
			{
				VehicleDef vehicleDef = DefDatabase<VehicleDef>.GetNamed(terrainCostFlipper.Key);

				foreach (var terrainFlip in terrainCostFlipper.Value)
				{
					string terrainTag = terrainFlip.Key;
					int pathCost = terrainFlip.Value;

					List<TerrainDef> terrainDefsWithTag = terrainDefs.Where(td => td.tags.NotNullAndAny(tag => tag == terrainTag)).ToList();
					foreach (TerrainDef terrainDef in terrainDefsWithTag)
					{
						vehicleDef.properties.customTerrainCosts.TryAdd(terrainDef, pathCost);
					}
				}
			}
		}

		/// <summary>
		/// Load custom path costs from <see cref="CustomCostDefModExtension"/>
		/// </summary>
		public static void LoadDefModExtensionCosts()
		{
			foreach (Def def in DefDatabase<Def>.AllDefs)
			{
				if (def.GetModExtension<CustomCostDefModExtension>() is CustomCostDefModExtension customCost)
				{
					foreach (VehicleDef vehicleDef in customCost.vehicles)
					{
						if (def is TerrainDef terrainDef)
						{
							vehicleDef.properties.customTerrainCosts.TryAdd(terrainDef, customCost.pathCost);
						}
						if (def is ThingDef thingDef)
						{
							vehicleDef.properties.customThingCosts.TryAdd(thingDef, customCost.pathCost);
						}
						if (def is BiomeDef biomeDef)
						{
							vehicleDef.properties.customBiomeCosts.TryAdd(biomeDef, customCost.pathCost);
						}
						if (def is RiverDef riverDef)
						{
							vehicleDef.properties.customRiverCosts.TryAdd(riverDef, customCost.pathCost);
						}
						if (def is RoadDef roadDef)
						{
							vehicleDef.properties.customRoadCosts.TryAdd(roadDef, customCost.pathCost);
						}
					}
				}
			}
		}

		/// <summary>
		/// Register <seealso cref="ThingDef"/> region effectors for all <seealso cref="VehicleDef"/>s
		/// </summary>
		public static void CacheVehicleRegionEffecters()
		{
			foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs)
			{
				RegisterRegionEffecter(thingDef);
			}
		}

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
					mapping[vehicleDef].VehiclePathGrid.RecalculatePerceivedPathCostUnderThing(thing);
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
				foreach (VehicleDef vehicleDef in vehicleDefs.Where(v => v.defName == "Tank"))
				{
					mapping[vehicleDef].VehicleRegionDirtyer.Notify_ThingAffectingRegionsDespawned(thing);
					mapping[vehicleDef].VehiclePathGrid.RecalculatePerceivedPathCostUnderThing(thing);
				}
			}
		}

		/// <summary>
		/// Check if cell is currently claimed by a vehicle
		/// </summary>
		/// <param name="map"></param>
		/// <param name="cell"></param>
		public static bool VehicleImpassableInCell(Map map, IntVec3 cell)
		{
			return map.GetCachedMapComponent<VehiclePositionManager>().ClaimedBy(cell) is VehiclePawn vehicle && vehicle.VehicleDef.passability == Traversability.Impassable;
		}

		/// <see cref="VehicleImpassableInCell(Map, IntVec3)"/>
		public static bool VehicleImpassableInCell(Map map, int x, int z)
		{
			return VehicleImpassableInCell(map, new IntVec3(x, 0, z));
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
