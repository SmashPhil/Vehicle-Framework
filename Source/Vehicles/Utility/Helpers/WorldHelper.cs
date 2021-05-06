using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public static class WorldHelper
	{
		/// <summary>
		/// Tile is completely water covered
		/// </summary>
		/// <param name="tile"></param>
		public static bool WaterCovered(int tile)
		{
			return Find.WorldGrid[tile].biome == BiomeDefOf.Ocean || Find.WorldGrid[tile].biome == BiomeDefOf.Lake;
		}

		/// <summary>
		/// <paramref name="ships"/> are able to travel on the River located in <paramref name="tile"/> on the World Map
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="ships"></param>
		public static bool RiverIsValid(int tile, List<Pawn> ships)
		{
			if (!VehicleMod.settings.main.riverTravel || ships is null || !ships.NotNullAndAny(p => p.IsBoat()))
			{
				return false;
			}
			bool flag = VehicleMod.settings.main.boatSizeMatters ? (!Find.WorldGrid[tile].Rivers.NullOrEmpty()) ? ShipsFitOnRiver(BiggestRiverOnTile(Find.WorldGrid[tile]?.Rivers).river, ships) : false : (Find.WorldGrid[tile].Rivers?.NotNullAndAny() ?? false);
			return flag;
		}

		/// <summary>
		/// Biggest river in a tile
		/// </summary>
		/// <param name="list"></param>
		public static Tile.RiverLink BiggestRiverOnTile(List<Tile.RiverLink> list)
		{
			return list.MaxBy(x => x.river.widthOnMap);
		}

		/// <summary>
		/// Determine if <paramref name="river"/> is large enough for all ships in <paramref name="pawns"/>
		/// </summary>
		/// <param name="river"></param>
		/// <param name="pawns"></param>
		public static bool ShipsFitOnRiver(RiverDef river, List<Pawn> pawns)
		{
			foreach (VehiclePawn vehicle in pawns.Where(p => p.IsBoat()))
			{
				if ((vehicle.VehicleDef.properties.riverTraversability?.widthOnMap ?? int.MaxValue) > river.widthOnMap)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// AerialVehicle <paramref name="vehicle"/> can offer gifts to <paramref name="settlement"/>
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="settlement"></param>
		public static FloatMenuAcceptanceReport CanOfferGiftsTo(AerialVehicleInFlight vehicle, Settlement settlement)
		{
			return settlement != null && settlement.Spawned && !settlement.HasMap && settlement.Faction != null && settlement.Faction != Faction.OfPlayer
				&& !settlement.Faction.def.permanentEnemy && settlement.Faction.HostileTo(Faction.OfPlayer) && settlement.CanTradeNow && vehicle.vehicle.HasNegotiator;
		}

		/// <summary>
		/// Find best tile to snap to when ordering a caravan
		/// </summary>
		/// <param name="c"></param>
		/// <param name="tile"></param>
		public static int BestGotoDestForVehicle(Caravan caravan, int tile)
		{
			Predicate<int> predicate = (int t) => caravan.UniqueVehicleDefsInCaravan().All(v => Find.World.GetCachedWorldComponent<WorldVehiclePathGrid>().Passable(t, v)) && 
				Find.World.GetCachedWorldComponent<WorldVehicleReachability>().CanReach(caravan, t);
			if (predicate(tile))
			{
				return tile;
			}
			GenWorldClosest.TryFindClosestTile(tile, predicate, out int result, 50, true);
			return result;
		}

		/// <summary>
		/// Find best negotiator in VehicleCaravan for trading on the World Map
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="faction"></param>
		/// <param name="trader"></param>
		public static Pawn FindBestNegotiator(VehiclePawn vehicle, Faction faction = null, TraderKindDef trader = null)
		{
			Predicate<Pawn> pawnValidator = null;
			if (faction != null)    
			{
				pawnValidator = ((Pawn p) => p.CanTradeWith(faction, trader));
			}
			return vehicle.FindPawnWithBestStat(StatDefOf.TradePriceImprovement, pawnValidator);
		}

		/// <summary>
		/// Get nearest tile id to <paramref name="worldCoord"/>
		/// </summary>
		/// <param name="worldCoord"></param>
		public static int GetNearestTile(Vector3 worldCoord)
		{
			for (int tile = 0; tile < Find.WorldGrid.TilesCount; tile++)
			{
				Vector3 pos = Find.WorldGrid.GetTileCenter(tile);
				if (Ext_Math.SphericalDistance(worldCoord, pos) <= 0.75f) //0.25 tile length margin of error for quicker calculation
				{
					return tile;
				}
			}
			return -1;
		}

		/// <summary>
		/// Change <paramref name="tileID"/> if tile is within CoastRadius of a coast <see cref="VehiclesModSettings"/>
		/// </summary>
		/// <param name="tileID"></param>
		/// <param name="faction"></param>
		/// <returns>new tileID if a nearby coast is found or <paramref name="tileID"/> if not found</returns>
		public static int PushSettlementToCoast(int tileID, Faction faction)
		{
			if (VehicleMod.CoastRadius <= 0)
			{
				return tileID;
			}

			List<int> neighbors = new List<int>();
			Stack<int> stack = new Stack<int>();
			stack.Push(tileID);
			Stack<int> stackFull = stack;
			List<int> newTilesSearch = new List<int>();
			HashSet<int> allSearchedTiles = new HashSet<int>() { tileID };
			int searchTile;
			int searchedRadius = 0;

			if (Find.World.CoastDirectionAt(tileID).IsValid)
			{
				if (Find.WorldGrid[tileID].biome.canBuildBase && !(faction is null))
				{
					VehicleHarmony.tiles.Add(new Pair<int, int>(tileID, 0));
				}
				return tileID;
			}

			while (searchedRadius < VehicleMod.CoastRadius)
			{
				for (int j = 0; j < stackFull.Count; j++)
				{
					searchTile = stack.Pop();
					Find.WorldGrid.GetTileNeighbors(searchTile, neighbors);
					int count = neighbors.Count;
					for (int i = 0; i < count; i++)
					{
						if (allSearchedTiles.NotNullAndAny(x => x == neighbors[i]))
						{
							continue;
						}
						newTilesSearch.Add(neighbors[i]);
						allSearchedTiles.Add(neighbors[i]);
						if (Find.World.CoastDirectionAt(neighbors[i]).IsValid)
						{
							if (Find.WorldGrid[neighbors[i]].biome.canBuildBase && Find.WorldGrid[neighbors[i]].biome.implemented && Find.WorldGrid[neighbors[i]].hilliness != Hilliness.Impassable)
							{
								if (VehicleHarmony.debug && !(faction is null))
								{
									DebugHelper.DebugDrawSettlement(tileID, neighbors[i]);
								}
								if (faction != null)
								{
									VehicleHarmony.tiles.Add(new Pair<int, int>(neighbors[i], searchedRadius));
								}
								return neighbors[i];
							}
						}
					}
				}
				stack.Clear();
				stack = new Stack<int>(newTilesSearch);
				stackFull = stack;
				newTilesSearch.Clear();
				searchedRadius++;
			}
			return tileID;
		}
	}
}
