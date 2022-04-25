using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public static class PathingHelper
	{
		public const string AllowTerrainWithTag = "PassableVehicles";
		public const string DisallowTerrainWithTag = "ImpassableVehicles";

		private static readonly Dictionary<ThingDef, List<VehicleDef>> regionEffecters = new Dictionary<ThingDef, List<VehicleDef>>();
		private static readonly Dictionary<TerrainDef, List<VehicleDef>> terrainEffecters = new Dictionary<TerrainDef, List<VehicleDef>>();

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
				if (terrainDef.tags.NotNullAndAny(tag => tag == AllowTerrainWithTag))
				{
					foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefs)
					{
						vehicleDef.properties.customTerrainCosts[terrainDef] = 1;
					}
				}
				else if (terrainDef.tags.NotNullAndAny(tag => tag == DisallowTerrainWithTag))
				{
					foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefs)
					{
						vehicleDef.properties.customTerrainCosts[terrainDef] = VehiclePathGrid.ImpassableCost;
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
						vehicleDef.properties.customTerrainCosts[terrainDef] = pathCost;
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
				if (def is VehicleDef)
				{
					Debug.Warning($"Attempting to set custom path cost for {def.defName} when vehicles should not be pathing over other vehicles to begin with. Please do not add this DefModExtension to VehicleDefs.");
					continue;
				}
				if (def.GetModExtension<CustomCostDefModExtension>() is CustomCostDefModExtension customCost)
				{
					foreach (VehicleDef vehicleDef in customCost.vehicles)
					{
						if (def is TerrainDef terrainDef)
						{
							vehicleDef.properties.customTerrainCosts[terrainDef] = customCost.pathCost;
						}
						if (def is ThingDef thingDef)
						{
							vehicleDef.properties.customThingCosts[thingDef] = customCost.pathCost;
						}
						if (def is BiomeDef biomeDef)
						{
							vehicleDef.properties.customBiomeCosts[biomeDef] = customCost.pathCost;
						}
						if (def is RiverDef riverDef)
						{
							vehicleDef.properties.customRiverCosts[riverDef] = customCost.pathCost;
						}
						if (def is RoadDef roadDef)
						{
							vehicleDef.properties.customRoadCosts[roadDef] = customCost.pathCost;
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

			foreach (TerrainDef terrainDef in DefDatabase<TerrainDef>.AllDefs)
			{
				RegisterTerrainEffecter(terrainDef);
			}
		}

		/// <summary>
		/// Register <paramref name="thingDef"/> as a potential object that will effect vehicle regions
		/// </summary>
		/// <param name="thingDef"></param>
		public static void RegisterRegionEffecter(ThingDef thingDef)
		{
			regionEffecters[thingDef] = new List<VehicleDef>();
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
		/// Register <paramref name="terrainDef"/> as a potential terrain that will effect vehicle regions
		/// </summary>
		/// <param name="terrainDef"></param>
		public static void RegisterTerrainEffecter(TerrainDef terrainDef)
		{
			terrainEffecters[terrainDef] = new List<VehicleDef>();
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefs)
			{
				if (vehicleDef.properties.customTerrainCosts.TryGetValue(terrainDef, out int value))
				{
					if (value < 0 || value >= VehiclePathGrid.ImpassableCost)
					{
						terrainEffecters[terrainDef].Add(vehicleDef);
					}
				}
				else if (terrainDef.passability == Traversability.Impassable || vehicleDef.properties.defaultTerrainImpassable)
				{
					terrainEffecters[terrainDef].Add(vehicleDef);
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
				foreach (VehicleDef vehicleDef in vehicleDefs)
				{
					mapping[vehicleDef].VehicleRegionDirtyer.Notify_ThingAffectingRegionsDespawned(thing);
					mapping[vehicleDef].VehiclePathGrid.RecalculatePerceivedPathCostUnderThing(thing);
				}
			}
		}

		public static void RecalculatePerceivedPathCostAt(IntVec3 cell, Map map)
		{
			TerrainDef terrainDef = map.terrainGrid.TerrainAt(cell);
			if (terrainEffecters.TryGetValue(terrainDef, out List<VehicleDef> vehicleDefs))
			{
				VehicleMapping mapping = map.GetCachedMapComponent<VehicleMapping>();
				foreach (VehicleDef vehicleDef in vehicleDefs)
				{
					mapping[vehicleDef].VehiclePathGrid.RecalculatePerceivedPathCostAt(cell);
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

		public static void ExitMapForVehicle(VehiclePawn vehicle, Job job)
		{
			if (job.failIfCantJoinOrCreateCaravan && !CaravanExitMapUtility.CanExitMapAndJoinOrCreateCaravanNow(vehicle))
			{
				return;
			}
			ExitMap(vehicle, true, CellRect.WholeMap(vehicle.Map).GetClosestEdge(vehicle.Position));
		}

		public static void ExitMap(VehiclePawn vehicle, bool allowedToJoinOrCreateCaravan, Rot4 exitDir)
		{
			if (vehicle.IsWorldPawn())
			{
				Log.Warning($"Called ExitMap() on world pawn {vehicle}");
				return;
			}
			Ideo ideo = vehicle.Ideo;
			if (ideo != null)
			{
				ideo.Notify_MemberLost(vehicle, vehicle.Map);
			}
			if (allowedToJoinOrCreateCaravan && CaravanExitMapUtility.CanExitMapAndJoinOrCreateCaravanNow(vehicle))
			{
				CaravanExitMapUtility.ExitMapAndJoinOrCreateCaravan(vehicle, exitDir);
				return;
			}
			Lord lord = vehicle.GetLord();
			if (lord != null)
			{
				lord.Notify_PawnLost(vehicle, PawnLostCondition.ExitedMap, null);
			}
			if (vehicle.carryTracker != null && vehicle.carryTracker.CarriedThing != null)
			{
				Pawn pawn = vehicle.carryTracker.CarriedThing as Pawn;
				if (pawn != null)
				{
					if (vehicle.Faction != null && vehicle.Faction != pawn.Faction)
					{
						vehicle.Faction.kidnapped.Kidnap(pawn, vehicle);
					}
					else
					{
						if (!vehicle.teleporting)
						{
							vehicle.carryTracker.innerContainer.Remove(pawn);
						}
						pawn.ExitMap(false, exitDir);
					}
				}
				else
				{
					vehicle.carryTracker.CarriedThing.Destroy(DestroyMode.Vanish);
				}
				if (!vehicle.teleporting || pawn == null)
				{
					vehicle.carryTracker.innerContainer.Clear();
				}
			}
			bool flag = !vehicle.IsCaravanMember() && !vehicle.teleporting && !PawnUtility.IsTravelingInTransportPodWorldObject(vehicle) && (!vehicle.IsPrisoner || vehicle.ParentHolder == null || vehicle.ParentHolder is CompShuttle || (vehicle.guest != null && vehicle.guest.Released));

			if (vehicle.Faction != null)
			{
				vehicle.Faction.Notify_MemberExitedMap(vehicle, flag);
			}
			if (vehicle.Faction == Faction.OfPlayer && vehicle.IsSlave && vehicle.SlaveFaction != null && vehicle.SlaveFaction != Faction.OfPlayer && vehicle.guest.Released)
			{
				vehicle.SlaveFaction.Notify_MemberExitedMap(vehicle, flag);
			}
			if (vehicle.Spawned)
			{
				vehicle.DeSpawn(DestroyMode.Vanish);
			}
			vehicle.inventory.UnloadEverything = false;
			if (flag)
			{
				vehicle.vPather.StopDead();
				vehicle.jobs.StopAll(false, true);
				vehicle.VerifyReservations();
			}
			Find.WorldPawns.PassToWorld(vehicle, PawnDiscardDecideMode.Decide);
			QuestUtility.SendQuestTargetSignals(vehicle.questTags, "LeftMap", vehicle.Named("SUBJECT"));
			Find.FactionManager.Notify_PawnLeftMap(vehicle);
			Find.IdeoManager.Notify_PawnLeftMap(vehicle);
		}
	}
}
