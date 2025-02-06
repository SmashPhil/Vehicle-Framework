using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Performance;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

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
		public static bool RegionWorking(Map map) => (bool)AccessTools.Field(typeof(RegionAndRoomUpdater), "working").GetValue(map.regionAndRoomUpdater);

		public static bool ShouldCreateRegions(VehicleDef vehicleDef)
		{
			return SettingsCache.TryGetValue(vehicleDef, typeof(VehicleDef), nameof(vehicleDef.vehicleMovementPermissions), vehicleDef.vehicleMovementPermissions) > VehiclePermissions.NotAllowed;
		}

		public static bool TryGetStandableCell(VehiclePawn vehicle, IntVec3 cell)
		{
			int num = GenRadial.NumCellsInRadius(2.9f);
			IntVec3 curLoc;
			for (int i = 0; i < num; i++)
			{
				curLoc = GenRadial.RadialPattern[i] + cell;
				if (GenGridVehicles.Standable(curLoc, vehicle, vehicle.Map) && 
					(!VehicleMod.settings.main.fullVehiclePathing || vehicle.DrivableRectOnCell(curLoc)))
				{
					if (curLoc == vehicle.Position || vehicle.beached)
					{
						return false;
					}
					return true;
				}
			}
			return false;
		}
		/// <summary>
		/// Register any <seealso cref="TerrainDef"/>s with tags "PassableVehicles" or "ImpassableVehicles"
		/// </summary>
		public static void LoadTerrainDefaults()
		{
			foreach (TerrainDef terrainDef in DefDatabase<TerrainDef>.AllDefsListForReading)
			{
				if (terrainDef.tags.NotNullAndAny(tag => tag == AllowTerrainWithTag))
				{
					foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
					{
						vehicleDef.properties.customTerrainCosts[terrainDef] = 1;
					}
				}
				else if (terrainDef.tags.NotNullAndAny(tag => tag == DisallowTerrainWithTag))
				{
					foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
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
		public static void LoadDefModExtensionCosts<T>(Func<VehicleDef, Dictionary<T, int>> dictFromVehicle) where T : Def
		{
			foreach (T def in DefDatabase<T>.AllDefsListForReading)
			{
				if (def.GetModExtension<CustomCostDefModExtension>() is CustomCostDefModExtension customCost)
				{
					if (def is VehicleDef)
					{
						Debug.Warning($"Attempting to set custom path cost for {def.defName} when vehicles should not be pathing over other vehicles to begin with. Please do not add this DefModExtension to VehicleDefs.");
						continue;
					}
					List<VehicleDef> vehicles = customCost.vehicles;
					if (vehicles.NullOrEmpty()) //If no vehicles are specified, apply to all
					{
						vehicles = DefDatabase<VehicleDef>.AllDefsListForReading;
					}
					foreach (VehicleDef vehicleDef in vehicles)
					{
						dictFromVehicle(vehicleDef)[def] = Mathf.RoundToInt(customCost.cost);
					}
				}
			}
		}

		public static void LoadDefModExtensionCosts<T>(Func<VehicleDef, Dictionary<T, float>> dictFromVehicle) where T : Def
		{
			foreach (T def in DefDatabase<T>.AllDefsListForReading)
			{
				if (def.GetModExtension<CustomCostDefModExtension>() is CustomCostDefModExtension customCost)
				{
					if (def is VehicleDef)
					{
						Debug.Warning($"Attempting to set custom path cost for {def.defName} when vehicles should not be pathing over other vehicles to begin with. Please do not add this DefModExtension to VehicleDefs.");
						continue;
					}
					List<VehicleDef> vehicles = customCost.vehicles;
					if (vehicles.NullOrEmpty()) //If no vehicles are specified, apply to all
					{
						vehicles = DefDatabase<VehicleDef>.AllDefsListForReading;
					}
					foreach (VehicleDef vehicleDef in vehicles)
					{
						dictFromVehicle(vehicleDef)[def] = customCost.cost;
					}
				}
			}
		}

		/// <summary>
		/// Register <seealso cref="ThingDef"/> region effectors for all <seealso cref="VehicleDef"/>s
		/// </summary>
		public static void CacheVehicleRegionEffecters()
		{
			foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
			{
				RegisterRegionEffecter(thingDef);
			}

			foreach (TerrainDef terrainDef in DefDatabase<TerrainDef>.AllDefsListForReading)
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
			foreach (VehicleDef vehicleDef in VehicleHarmony.AllMoveableVehicleDefs)
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
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
			{
				if (vehicleDef.properties.customTerrainCosts.TryGetValue(terrainDef, out int value))
				{
					if (value >= VehiclePathGrid.ImpassableCost)
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
		/// Notify <paramref name="thing"/> has been despawned. Mark regions dirty if <paramref name="thing"/> affects passability
		/// </summary>
		/// <param name="thing"></param>
		/// <param name="map"></param>
		public static void ThingAffectingRegionsStateChange(Thing thing, Map map, bool spawned)
		{
			if (regionEffecters.TryGetValue(thing.def, out List<VehicleDef> vehicleDefs) && !vehicleDefs.NullOrEmpty())
			{
				VehicleMapping mapping = MapComponentCache<VehicleMapping>.GetComponent(map);
				if (mapping.ThreadAvailable)
				{
					CellRect occupiedRect = thing.OccupiedRect();
					AsyncRegionAction asyncAction = AsyncPool<AsyncRegionAction>.Get();
					asyncAction.Set(mapping, vehicleDefs, occupiedRect, spawned);
					mapping.dedicatedThread.Queue(asyncAction);
				}
				else
				{
					CellRect occupiedRect = thing.OccupiedRect();
					if (spawned)
					{
						ThingInRegionSpawned(occupiedRect, mapping, vehicleDefs);
					}
					else
					{
						ThingInRegionDespawned(occupiedRect, mapping, vehicleDefs);
					}
				}
			}
		}

		/// <summary>
		/// Thread safe event for triggering dirtyer events
		/// </summary>
		internal static void ThingInRegionSpawned(CellRect occupiedRect, VehicleMapping mapping, List<VehicleDef> vehicleDefs)
		{
			foreach (VehicleDef vehicleDef in vehicleDefs)
			{
				mapping[vehicleDef].VehiclePathGrid.RecalculatePerceivedPathCostUnderRect(occupiedRect);
				if (VehicleHarmony.gridOwners.IsOwner(vehicleDef))
				{
					mapping[vehicleDef].VehicleRegionDirtyer.Notify_ThingAffectingRegionsSpawned(occupiedRect);
					mapping[vehicleDef].VehicleReachability.ClearCache();
				}
			}
		}

		internal static void ThingInRegionDespawned(CellRect occupiedRect, VehicleMapping mapping, List<VehicleDef> vehicleDefs)
		{
			foreach (VehicleDef vehicleDef in vehicleDefs)
			{
				mapping[vehicleDef].VehiclePathGrid.RecalculatePerceivedPathCostUnderRect(occupiedRect);
				if (VehicleHarmony.gridOwners.IsOwner(vehicleDef))
				{
					mapping[vehicleDef].VehicleRegionDirtyer.Notify_ThingAffectingRegionsDespawned(occupiedRect);
					mapping[vehicleDef].VehicleReachability.ClearCache();
				}
			}
		}

		public static void ThingAffectingRegionsOrientationChanged(Thing thing, Map map)
		{
			if (regionEffecters.TryGetValue(thing.def, out List<VehicleDef> vehicleDefs) && !vehicleDefs.NullOrEmpty())
			{
				VehicleMapping mapping = MapComponentCache<VehicleMapping>.GetComponent(map);
				if (mapping.ThreadAvailable)
				{
					AsyncReachabilityCacheAction asyncAction = AsyncPool<AsyncReachabilityCacheAction>.Get();
					asyncAction.Set(mapping, vehicleDefs);
					mapping.dedicatedThread.Queue(asyncAction);
				}
				else
				{
					ThingInRegionOrientationChanged(mapping, vehicleDefs);
				}
			}
		}

		private static void ThingInRegionOrientationChanged(VehicleMapping mapping, List<VehicleDef> vehicleDefs)
		{
			foreach (VehicleDef vehicleDef in vehicleDefs)
			{
				if (VehicleHarmony.gridOwners.IsOwner(vehicleDef))
				{
					mapping[vehicleDef].VehicleReachability.ClearCache();
				}
			}
		}

		public static void RecalculateAllPerceivedPathCosts(Map map)
		{
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				VehicleMapping mapping = MapComponentCache<VehicleMapping>.GetComponent(map);
				if (!VehicleHarmony.AllVehicleOwners.NullOrEmpty())
				{
					RecalculateAllPerceivedPathCosts(mapping);
				}
			});
		}

		private static void RecalculateAllPerceivedPathCosts(VehicleMapping mapping)
		{
			foreach (IntVec3 cell in mapping.map.AllCells)
			{
				foreach (VehicleDef vehicleDef in VehicleHarmony.AllVehicleOwners)
				{
					mapping[vehicleDef].VehiclePathGrid.RecalculatePerceivedPathCostAt(cell);
				}
			}
		}

		/// <summary>
		/// Recalculate perceived path cost for all vehicles at <paramref name="cell"/>
		/// </summary>
		/// <remarks>Requires null check on TerrainDef in case region grid has not been initialized</remarks>
		/// <param name="cell"></param>
		/// <param name="map"></param>
		public static void RecalculatePerceivedPathCostAt(IntVec3 cell, Map map)
		{
			VehicleMapping mapping = MapComponentCache<VehicleMapping>.GetComponent(map);
			if (!VehicleHarmony.AllVehicleOwners.NullOrEmpty())
			{
				if (mapping.ThreadAvailable)
				{
					AsyncPathingAction asyncAction = AsyncPool<AsyncPathingAction>.Get();
					asyncAction.Set(mapping, cell);
					mapping.dedicatedThread.Queue(asyncAction);
				}
				else
				{
					RecalculatePerceivedPathCostAtFor(mapping, cell);
				}
			}
		}

		internal static void RecalculatePerceivedPathCostAtFor(VehicleMapping mapping, IntVec3 cell)
		{
			foreach (VehicleDef vehicleDef in VehicleHarmony.AllMoveableVehicleDefs)
			{
				mapping[vehicleDef].VehiclePathGrid.RecalculatePerceivedPathCostAt(cell);
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

		public static bool TryFindNearestStandableCell(VehiclePawn vehicle, IntVec3 cell, out IntVec3 result, float radius = -1)
		{
			if (radius < 0)
			{
				radius = Mathf.Min(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z) * 2;
			}
			int radialCount = GenRadial.NumCellsInRadius(radius);
			result = IntVec3.Invalid;
			IntVec3 curLoc;
			for (int i = 0; i < radialCount; i++)
			{
				curLoc = GenRadial.RadialPattern[i] + cell;
				if (GenGridVehicles.Standable(curLoc, vehicle, vehicle.Map) && (!VehicleMod.settings.main.fullVehiclePathing || vehicle.DrivableRectOnCell(curLoc, maxPossibleSize: true)))
				{
					if (curLoc == vehicle.Position || vehicle.beached)
					{
						result = curLoc;
						return true;
					}
					if (ThreadHelper.AnyVehicleBlockingPathAt(curLoc, vehicle) != null)
					{
						continue; //If another vehicle occupies the cell, skip
					}
					if (!VehicleReachabilityUtility.CanReachVehicle(vehicle, curLoc, PathEndMode.OnCell, Danger.Deadly, TraverseMode.ByPawn))
					{
						continue; //If unreachable (eg. wall), skip
					}
					result = curLoc;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Calculate angle of Vehicle
		/// </summary>
		/// <param name="pawn"></param>
		public static float CalculateAngle(this VehiclePawn vehicle, out bool northSouthRotation)
		{
			northSouthRotation = false;
			if (vehicle is null) return 0f;
			if (vehicle.vehiclePather.Moving)
			{
				IntVec3 c = vehicle.vehiclePather.nextCell - vehicle.Position;
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
			bool free = !vehicle.IsCaravanMember() && !vehicle.teleporting && !PawnUtility.IsTravelingInTransportPodWorldObject(vehicle) && (!vehicle.IsPrisoner || vehicle.ParentHolder == null || vehicle.ParentHolder is CompShuttle || (vehicle.guest != null && vehicle.guest.Released));

			if (vehicle.Faction != null)
			{
				vehicle.Faction.Notify_MemberExitedMap(vehicle, free);
			}
			if (vehicle.Faction == Faction.OfPlayer && vehicle.IsSlave && vehicle.SlaveFaction != null && vehicle.SlaveFaction != Faction.OfPlayer && vehicle.guest.Released)
			{
				vehicle.SlaveFaction.Notify_MemberExitedMap(vehicle, free);
			}
			if (vehicle.Spawned)
			{
				vehicle.DeSpawn(DestroyMode.Vanish);
			}
			vehicle.inventory.UnloadEverything = false;
			if (free)
			{
				vehicle.vehiclePather.StopDead();
				vehicle.jobs.StopAll(false, true);
				vehicle.VerifyReservations();
			}
			Find.WorldPawns.PassToWorld(vehicle);
			foreach (Thing thing in vehicle.inventory.innerContainer)
			{
				if (thing is Pawn pawn && !pawn.IsWorldPawn())
				{
					Find.WorldPawns.PassToWorld(pawn);
				}
			}
			QuestUtility.SendQuestTargetSignals(vehicle.questTags, "LeftMap", vehicle.Named("SUBJECT"));
			Find.FactionManager.Notify_PawnLeftMap(vehicle);
			Find.IdeoManager.Notify_PawnLeftMap(vehicle);
		}

		public static void DisableAllRegionUpdaters(Map map)
		{
			VehicleMapping mapping = map.GetCachedMapComponent<VehicleMapping>();
			foreach (VehicleDef vehicleDef in VehicleHarmony.gridOwners.Owners)
			{
				VehicleMapping.VehiclePathData pathData = mapping[vehicleDef];
				pathData.VehicleRegionAndRoomUpdater.Disable();
			}
		}
	}
}
