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
	//REDO - Rivers
	public static class WorldHelper
	{
		public static bool RiverIsValid(int tile, List<Pawn> vehicles) => true;

		public static float RiverCostAt(int tile, VehiclePawn vehicle)
		{
			BiomeDef biome = Find.WorldGrid[tile].biome;
			RiverDef river = Find.WorldGrid[tile].Rivers.MaxBy(r => r.river.widthOnWorld).river;
			if (vehicle.VehicleDef.properties.customRiverCosts.TryGetValue(river, out float cost))
			{
				return cost;
			}
			return WorldVehiclePathGrid.ImpassableMovementDifficulty;
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
		/// Get Heading between 2 points on World
		/// </summary>
		/// <param name="map"></param>
		/// <param name="target"></param>
		public static float TryFindHeading(Vector3 source, Vector3 target)
		{
			float heading = Find.WorldGrid.GetHeadingFromTo(source, target);
			return heading;
		}

		public static WorldObject WorldObjectAt(int tile)
		{
			List<WorldObject> worldObjects = Find.WorldObjects.AllWorldObjects;
			for (int i = 0; i < worldObjects.Count; i++)
			{
				WorldObject worldObject = worldObjects[i];
				if (worldObject.Tile == tile)
				{
					return worldObject;
				}
			}
			return null;
		}

		public static (WorldObject sourceObject, WorldObject destObject) WorldObjectsAt(int source, int destination)
		{
			WorldObject sourceObject = null;
			WorldObject destObject = null;
			List<WorldObject> worldObjects = Find.WorldObjects.AllWorldObjects;
			for (int i = 0; i < worldObjects.Count && (sourceObject == null || destObject == null); i++)
			{
				WorldObject worldObject = worldObjects[i];
				if (worldObject.Tile == source)
				{
					sourceObject = worldObject;
				}
				if (worldObject.Tile == destination)
				{
					destObject = worldObject;
				}
			}
			return (sourceObject, destObject);
		}

		public static Vector3 GetTilePos(int tile)
		{
			WorldObject worldObject = WorldObjectAt(tile);
			return GetTilePos(tile, worldObject, out _);
		}

		public static Vector3 GetTilePos(int tile, out bool spaceObject)
		{
			WorldObject worldObject = WorldObjectAt(tile);
			return GetTilePos(tile, worldObject, out spaceObject);
		}

		public static Vector3 GetTilePos(int tile, WorldObject worldObject, out bool spaceObject)
		{
			Vector3 pos = Find.WorldGrid.GetTileCenter(tile);
			spaceObject = false;
			if (worldObject != null && worldObject.def.HasModExtension<SpaceObjectDefModExtension>())
			{
				spaceObject = true;
				pos = worldObject.DrawPos;
			}
			return pos;
		}

		public static float GetTileDistance(int source, int destination)
		{
			(WorldObject sourceObject, WorldObject destObject) = WorldObjectsAt(source, destination);

			Vector3 sourcePos = GetTilePos(source, sourceObject, out _);
			Vector3 destPos = GetTilePos(destination, destObject, out _);

			return Ext_Math.SphericalDistance(sourcePos, destPos);
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
		/// <param name="caravan"></param>
		/// <param name="tile"></param>
		public static int BestGotoDestForVehicle(VehicleCaravan caravan, int tile)
		{
			bool CaravanReachable(int t) => caravan.UniqueVehicleDefsInCaravan().All(v => WorldVehiclePathGrid.Instance.Passable(t, v)) &&
				WorldVehicleReachability.Instance.CanReach(caravan, t);
			if (CaravanReachable(tile))
			{
				return tile;
			}
			GenWorldClosest.TryFindClosestTile(tile, CaravanReachable, out int result, 50, true);
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
				pawnValidator = delegate (Pawn p)
				{
					AcceptanceReport report = p.CanTradeWith(faction, trader);
					return report.Accepted;
				};
			}
			return vehicle.FindPawnWithBestStat(StatDefOf.TradePriceImprovement, pawnValidator);
		}

		/// <summary>
		/// Find best negotiator in Vehicle for trading on the World Map
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="faction"></param>
		/// <param name="trader"></param>
		public static Pawn FindBestNegotiator(VehicleCaravan caravan, Faction faction = null, TraderKindDef trader = null)
		{
			Predicate<Pawn> pawnValidator = null;
			if (faction != null)    
			{
				pawnValidator = delegate (Pawn p)
				{
					AcceptanceReport report = p.CanTradeWith(faction, trader);
					return report.Accepted;
				};
			}
			return BestCaravanPawnUtility.FindPawnWithBestStat(caravan, StatDefOf.TradePriceImprovement, pawnValidator: pawnValidator);
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
		/// Change <paramref name="tile"/> if tile is within CoastRadius of a coast <see cref="VehiclesModSettings"/>
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="faction"></param>
		/// <returns>new tileID if a nearby coast is found or <paramref name="tile"/> if not found</returns>
		public static int PushSettlementToCoast(int tile, Faction faction)
		{
			if (VehicleMod.CoastRadius <= 0)
			{
				return tile;
			}

			if (Find.World.CoastDirectionAt(tile).IsValid)
			{
				if (Find.WorldGrid[tile].biome.canBuildBase && !(faction is null))
				{
					VehicleHarmony.tiles.Add(new Pair<int, int>(tile, 0));
				}
				return tile;
			}

			List<int> neighbors = new List<int>();
			return Ext_World.BFS(tile, neighbors, VehicleMod.CoastRadius, result: delegate (int currentTile, int currentRadius)
			{
				if (Find.World.CoastDirectionAt(currentTile).IsValid)
				{
					if (Find.WorldGrid[currentTile].biome.canBuildBase && Find.WorldGrid[currentTile].biome.implemented && Find.WorldGrid[currentTile].hilliness != Hilliness.Impassable)
					{
						if (VehicleHarmony.debug && !(faction is null))
						{
							DebugHelper.DebugDrawSettlement(tile, currentTile);
						}
						if (faction != null)
						{
							VehicleHarmony.tiles.Add(new Pair<int, int>(currentTile, currentRadius));
						}
						return true;
					}
				}
				return false;
			});
		}

		/// <summary>
		/// Convert <paramref name="pos"/> to matrix in World space
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="size"></param>
		/// <param name="altOffset"></param>
		/// <param name="counterClockwise"></param>
		public static Matrix4x4 GetWorldQuadAt(Vector3 pos, float size, float altOffset, bool counterClockwise = false)
		{
			Vector3 normalized = pos.normalized;
			Vector3 vector;
			if (counterClockwise)
			{
				vector = -normalized;
			}
			else
			{
				vector = normalized;
			}
			Quaternion q = Quaternion.LookRotation(Vector3.Cross(vector, Vector3.up), vector);
			Vector3 s = new Vector3(size, 1f, size);
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(pos + normalized * altOffset, q, s);
			return matrix;
		}
	}
}
