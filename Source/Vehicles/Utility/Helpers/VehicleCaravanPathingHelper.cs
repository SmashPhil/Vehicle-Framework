using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Vehicles
{
	public static class VehicleCaravanPathingHelper
	{
		private const int CacheDuration = 100;
		private const int MaxIterations = 10000;

		private static int cacheTicks = -1;
		private static VehicleCaravan cachedForCaravan;
		private static int cachedForDest = -1;
		private static int cachedResult = -1;
		
		private static List<(int, int)> tmpTicksToArrive = new List<(int, int)>();

		/// <summary>
		/// Replaces <see cref="Caravan.NightResting"/> with patch hook in <seealso cref="CaravanHandling.NoRestForVehicles(Caravan, ref bool)"/>
		/// </summary>
		public static bool ShouldRestAt(VehicleCaravan caravan, int tile)
		{
			if (!caravan.Spawned)
			{
				return false;
			}
			bool conditionalResting = !caravan.vPather.Moving || caravan.vPather.nextTile != caravan.vPather.Destination || !Caravan_PathFollower.IsValidFinalPushDestination(caravan.vPather.Destination) ||
						Mathf.CeilToInt(caravan.vPather.nextTileCostLeft / 1f) > 10000;
			 return conditionalResting && ShouldRestAt(caravan.Vehicles, tile);
		}

		public static bool ShouldRestAt(IEnumerable<VehiclePawn> vehicles, int tile)
		{
			foreach (VehiclePawn vehicle in vehicles)
			{
				NavigationCategory navigationCategory = SettingsCache.TryGetValue(vehicle.VehicleDef, typeof(VehicleDef), nameof(VehicleDef.navigationCategory), vehicle.VehicleDef.navigationCategory);
				if (navigationCategory == NavigationCategory.Automatic)
				{
					return false;
				}
			}
			return CaravanNightRestUtility.RestingNowAt(tile);
		}

		public static bool ShouldRestAt(IEnumerable<VehicleDef> vehicleDefs, int tile)
		{
			foreach (VehicleDef vehicleDef in vehicleDefs)
			{
				NavigationCategory navigationCategory = SettingsCache.TryGetValue(vehicleDef, typeof(VehicleDef), nameof(VehicleDef.navigationCategory), vehicleDef.navigationCategory);
				if (navigationCategory == NavigationCategory.Automatic)
				{
					return false;
				}
			}
			return CaravanNightRestUtility.RestingNowAt(tile);
		}

		public static int EstimatedTicksToArrive(VehicleCaravan caravan, bool allowCaching)
		{
			if (allowCaching && caravan == cachedForCaravan && caravan.vPather.Destination == cachedForDest && Find.TickManager.TicksGame - cacheTicks < 100)
			{
				return cachedResult;
			}
			int to = -1;
			int result = 0;
			if (caravan.Spawned && caravan.vPather.Moving && caravan.vPather.curPath != null)
			{
				to = caravan.vPather.Destination;
				List<VehicleDef> vehicleDefs = caravan.Vehicles.Select(vehicle => vehicle.VehicleDef).ToList();
				result = EstimatedTicksToArrive(vehicleDefs, caravan.Tile, to, caravan.vPather.curPath, caravan.vPather.nextTileCostLeft, caravan.TicksPerMove, Find.TickManager.TicksAbs);
			}
			if (allowCaching)
			{
				cacheTicks = Find.TickManager.TicksGame;
				cachedForCaravan = caravan;
				cachedForDest = to;
				cachedResult = result;
			}
			return result;
		}

		public static int EstimatedTicksToArrive(int from, int to, VehicleCaravan caravan)
		{
			int result;
			using (WorldPath worldPath = Find.World.GetComponent<WorldVehiclePathfinder>().FindPath(from, to, caravan))
			{
				if (!worldPath.Found)
				{
					result = 0;
				}
				else
				{
					List<VehicleDef> vehicleDefs = caravan.Vehicles.Select(vehicle => vehicle.VehicleDef).ToList();
					result = EstimatedTicksToArrive(vehicleDefs, from, to, worldPath, 0f, (caravan != null) ? caravan.TicksPerMove : 3300, Find.TickManager.TicksAbs);
				}
			}
			return result;
		}

		public static int EstimatedTicksToArrive(List<VehicleDef> vehicleDefs, int from, int to, WorldPath path, float nextTileCostLeft, int caravanTicksPerMove, int curTicksAbs)
		{
			tmpTicksToArrive.Clear();
			EstimatedTicksToArriveToEvery(vehicleDefs, from, to, path, nextTileCostLeft, caravanTicksPerMove, curTicksAbs, tmpTicksToArrive);
			return EstimatedTicksToArrive(to, tmpTicksToArrive);
		}

		public static void EstimatedTicksToArriveToEvery(List<VehicleDef> vehicleDefs, int from, int to, WorldPath path, float nextTileCostLeft, int caravanTicksPerMove, int curTicksAbs, List<(int, int)> outTicksToArrive)
		{
			outTicksToArrive.Clear();
			outTicksToArrive.Add((from, 0));
			if (from == to)
			{
				outTicksToArrive.Add((to, 0));
				return;
			}
			int result = 0;
			int curTile = from;
			int pathSteps = 0;
			int restDuration = GenDate.TicksPerDay / 3 - 1; //Accounts for resting time in caravans that must rest
			int movementDuration = GenDate.TicksPerDay - restDuration;
			int ticksToMove = 0;
			int nonRestTicks;
			if (ShouldRestAt(vehicleDefs, from) && CaravanNightRestUtility.WouldBeRestingAt(from, curTicksAbs))
			{
				if (VehicleCaravan_PathFollower.IsValidFinalPushDestination(to) && (path.Peek(0) == to || (nextTileCostLeft <= 0f && path.NodesLeftCount >= 2 && path.Peek(1) == to)))
				{
					int num8 = Mathf.CeilToInt(GetCostToMove(vehicleDefs, nextTileCostLeft, path.Peek(0) == to, curTicksAbs, result, caravanTicksPerMove, from, to) / 1f);
					if (num8 <= 10000)
					{
						result += num8;
						outTicksToArrive.Add((to, result));
						return;
					}
				}
				result += CaravanNightRestUtility.LeftRestTicksAt(from, curTicksAbs);
				nonRestTicks = movementDuration;
			}
			else
			{
				nonRestTicks = CaravanNightRestUtility.LeftNonRestTicksAt(from, curTicksAbs);
			}
			for (int i = 0; i < 10000; i++)
			{
				if (ticksToMove <= 0)
				{
					if (curTile == to)
					{
						outTicksToArrive.Add((to, result));
						return;
					}
					bool firstInPath = pathSteps == 0;
					int nextTile = curTile;
					curTile = path.Peek(pathSteps);
					pathSteps++;
					outTicksToArrive.Add((nextTile, result));
					ticksToMove = Mathf.CeilToInt(GetCostToMove(vehicleDefs, nextTileCostLeft, firstInPath, curTicksAbs, result, caravanTicksPerMove, nextTile, curTile));
				}
				if (nonRestTicks < ticksToMove)
				{
					result += nonRestTicks;
					ticksToMove -= nonRestTicks;
					if (curTile == to && ticksToMove <= 10000 && Caravan_PathFollower.IsValidFinalPushDestination(to))
					{
						result += ticksToMove;
						outTicksToArrive.Add((to, result));
						return;
					}
					result += restDuration;
					nonRestTicks = movementDuration;
				}
				else
				{
					result += ticksToMove;
					nonRestTicks -= ticksToMove;
					ticksToMove = 0;
				}
			}
			Log.ErrorOnce("Could not calculate estimated ticks to arrive. Too many iterations.", 1837451324);
			outTicksToArrive.Add((to, result));
		}

		private static float GetCostToMove(List<VehicleDef> vehicleDefs, float initialNextTileCostLeft, bool firstInPath, int initialTicksAbs, int curResult, int caravanTicksPerMove, int curTile, int nextTile)
		{
			if (firstInPath)
			{
				return initialNextTileCostLeft;
			}
			int value = initialTicksAbs + curResult;
			return VehicleCaravan_PathFollower.CostToMove(vehicleDefs, caravanTicksPerMove, curTile, nextTile, ticksAbs: value);
		}

		public static int EstimatedTicksToArrive(int destinationTile, List<(int, int)> estimatedTicksToArriveToEvery)
		{
			if (destinationTile == -1)
			{
				return 0;
			}
			for (int i = 0; i < estimatedTicksToArriveToEvery.Count; i++)
			{
				if (destinationTile == estimatedTicksToArriveToEvery[i].Item1)
				{
					return estimatedTicksToArriveToEvery[i].Item2;
				}
			}
			return 0;
		}

		public static int TileIllBeInAt(int ticksAbs, List<(int, int)> estimatedTicksToArriveToEvery, int ticksAbsUsedToCalculateEstimatedTicksToArriveToEvery)
		{
			if (!estimatedTicksToArriveToEvery.Any())
			{
				return -1;
			}
			for (int i = estimatedTicksToArriveToEvery.Count - 1; i >= 0; i--)
			{
				int num = ticksAbsUsedToCalculateEstimatedTicksToArriveToEvery + estimatedTicksToArriveToEvery[i].Item2;
				if (ticksAbs >= num)
				{
					return estimatedTicksToArriveToEvery[i].Item1;
				}
			}
			return estimatedTicksToArriveToEvery[0].Item1;
		}
	}
}
