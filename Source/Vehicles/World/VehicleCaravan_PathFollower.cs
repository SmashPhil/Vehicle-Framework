using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Vehicles
{
    public class VehicleCaravan_PathFollower : IExposable
    {
        public int Destination
		{
			get
			{
				return destTile;
			}
		}

		public bool Moving
		{
			get
			{
				return moving && caravan.Spawned;
			}
		}

		public bool MovingNow
		{
			get
			{
				return Moving && !Paused && !caravan.CantMove;
			}
		}

		public CaravanArrivalAction ArrivalAction
		{
			get
			{
				if (!Moving)
				{
					return null;
				}
				return arrivalAction;
			}
		}

		public bool Paused
		{
			get
			{
				return Moving && paused;
			}
			set
			{
				if (value == paused)
				{
					return;
				}
				if (!value)
				{
					paused = false;
				}
				else if (!Moving)
				{
					Log.Error("Tried to pause caravan movement of " + caravan.ToStringSafe<Caravan>() + " but it's not moving.", false);
				}
				else
				{
					paused = true;
				}
				caravan.Notify_DestinationOrPauseStatusChanged();
			}
		}

		public VehicleCaravan_PathFollower(VehicleCaravan caravan)
		{
			this.caravan = caravan;
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref moving, "moving", true, false);
			Scribe_Values.Look(ref paused, "paused", false, false);
			Scribe_Values.Look(ref nextTile, "nextTile", 0, false);
			Scribe_Values.Look(ref previousTileForDrawingIfInDoubt, "previousTileForDrawingIfInDoubt", 0, false);
			Scribe_Values.Look(ref nextTileCostLeft, "nextTileCostLeft", 0f, false);
			Scribe_Values.Look(ref nextTileCostTotal, "nextTileCostTotal", 0f, false);
			Scribe_Values.Look(ref destTile, "destTile", 0, false);
			Scribe_Deep.Look(ref arrivalAction, "arrivalAction", Array.Empty<object>());
			if (Scribe.mode == LoadSaveMode.PostLoadInit && Current.ProgramState != ProgramState.Entry && moving && !StartPath(destTile, arrivalAction, true, false))
			{
				StopDead();
			}
		}

		public bool StartPath(int destTile, CaravanArrivalAction arrivalAction, bool repathImmediately = false, bool resetPauseStatus = true)
		{
			caravan.autoJoinable = false;
			if (resetPauseStatus)
			{
				paused = false;
			}
			if (arrivalAction != null && !arrivalAction.StillValid(caravan, destTile))
			{
				return false;
			}
			if (!IsPassable(caravan.Tile)&& !TryRecoverFromUnwalkablePosition())
			{
				return false;
			}
			if (moving && curPath != null && this.destTile == destTile)
			{
				this.arrivalAction = arrivalAction;
				return true;
			}
			if (!Find.World.GetCachedWorldComponent<WorldVehicleReachability>().CanReach(caravan, destTile))
			{
				PatherFailed();
				return false;
			}
			this.destTile = destTile;
			this.arrivalAction = arrivalAction;
			caravan.Notify_DestinationOrPauseStatusChanged();
			if (nextTile < 0 || !IsNextTilePassable())
			{
				nextTile = caravan.Tile;
				nextTileCostLeft = 0f;
				previousTileForDrawingIfInDoubt = -1;
			}
			if (AtDestinationPosition())
			{
				PatherArrived();
				return true;
			}
			if (curPath != null)
			{
				curPath.ReleaseToPool();
			}
			curPath = null;
			moving = true;
			if (repathImmediately && TrySetNewPath() && nextTileCostLeft <= 0f && moving)
			{
				TryEnterNextPathTile();
			}
			return true;
		}

		public void StopDead()
		{
			if (curPath != null)
			{
				curPath.ReleaseToPool();
			}
			curPath = null;
			moving = false;
			paused = false;
			nextTile = caravan.Tile;
			previousTileForDrawingIfInDoubt = -1;
			arrivalAction = null;
			nextTileCostLeft = 0f;
			caravan.Notify_DestinationOrPauseStatusChanged();
		}

		public void PatherTick()
		{
			if (moving && arrivalAction != null && !arrivalAction.StillValid(caravan, Destination))
			{
				string failMessage = arrivalAction.StillValid(caravan, Destination).FailMessage;
				Messages.Message("MessageCaravanArrivalActionNoLongerValid".Translate(caravan.Name).CapitalizeFirst() + ((failMessage != null) ? (" " + failMessage) : ""), caravan, MessageTypeDefOf.NegativeEvent, true);
				StopDead();
			}
			if (caravan.CantMove || paused)
			{
				return;
			}
			if (nextTileCostLeft > 0f)
			{
				nextTileCostLeft -= CostToPayThisTick();
				return;
			}
			if (moving)
			{
				TryEnterNextPathTile();
			}
		}

		public void Notify_Teleported_Int()
		{
			StopDead();
		}

		public bool IsPassable(int tile)
        {
			return caravan.UniqueVehicleDefsInCaravan().All(v => Find.World.GetCachedWorldComponent<WorldVehiclePathGrid>().Passable(tile, v));
        }

		public bool IsNextTilePassable()
		{
			return caravan.UniqueVehicleDefsInCaravan().All(v => Find.World.GetCachedWorldComponent<WorldVehiclePathGrid>().Passable(nextTile, v));
		}

		private bool TryRecoverFromUnwalkablePosition()
		{
			if (GenWorldClosest.TryFindClosestTile(caravan.Tile, (int t) => IsPassable(t) && Find.World.GetCachedWorldComponent<WorldVehicleReachability>().CanReach(caravan, t), out int num, 2147483647, true))
			{
				Log.Warning(string.Concat(new object[]
				{
					caravan,
					" on unwalkable tile ",
					caravan.Tile,
					". Teleporting to ",
					num
				}), false);
				caravan.Tile = num;
				caravan.Notify_VehicleTeleported();
				return true;
			}
			Log.Error(string.Concat(new object[]
			{
				caravan,
				" on unwalkable tile ",
				caravan.Tile,
				". Could not find walkable position nearby. Removed."
			}), false);
			caravan.Destroy();
			return false;
		}

		private void PatherArrived()
		{
			CaravanArrivalAction caravanArrivalAction = arrivalAction;
			StopDead();
			if (caravanArrivalAction != null && caravanArrivalAction.StillValid(caravan, caravan.Tile))
			{
				caravanArrivalAction.Arrived(caravan);
				return;
			}
			if (caravan.IsPlayerControlled && !caravan.VisibleToCameraNow())
			{
				Messages.Message("MessageCaravanArrivedAtDestination".Translate(caravan.Label), caravan, MessageTypeDefOf.TaskCompletion, true);
			}
		}

		private void PatherFailed()
		{
			StopDead();
		}

		private void TryEnterNextPathTile()
		{
			if (!IsNextTilePassable())
			{
				PatherFailed();
				return;
			}
			caravan.Tile = nextTile;
			if (NeedNewPath() && !TrySetNewPath())
			{
				return;
			}
			if (AtDestinationPosition())
			{
				PatherArrived();
				return;
			}
			if (curPath.NodesLeftCount == 0)
			{
				Log.Error(caravan + " ran out of path nodes. Force-arriving.", false);
				PatherArrived();
				return;
			}
			SetupMoveIntoNextTile();
		}

		private void SetupMoveIntoNextTile()
		{
			if (curPath.NodesLeftCount < 2)
			{
				Log.Error(string.Concat(new object[]
				{
					caravan,
					" at ",
					caravan.Tile,
					" ran out of path nodes while pathing to ",
					destTile,
					"."
				}), false);
				PatherFailed();
				return;
			}
			nextTile = curPath.ConsumeNextNode();
			previousTileForDrawingIfInDoubt = -1;
			if (!IsPassable(nextTile))
			{
				Log.Error(string.Concat(new object[]
				{
					caravan,
					" entering ",
					nextTile,
					" which is unwalkable."
				}), false);
			}
			int num = CostToMove(caravan.Tile, nextTile);
			nextTileCostTotal = num;
			nextTileCostLeft = num;
		}

		private int CostToMove(int start, int end)
		{
			return CostToMove(caravan, start, end, null);
		}

		public static int CostToMove(VehicleCaravan caravan, int start, int end, int? ticksAbs = null)
		{
			return CostToMove(caravan, start, end, ticksAbs, null, null);
		}

		public static int CostToMove(VehicleCaravan caravan, int start, int end, int? ticksAbs = null, StringBuilder explanation = null, string caravanTicksPerMoveExplanation = null)
		{
			int caravanTicksPerMove = caravan.TicksPerMove;
			if (start == end)
			{
				return 0;
			}
			if (explanation != null)
			{
				explanation.Append(caravanTicksPerMoveExplanation);
				explanation.AppendLine();
			}
			StringBuilder stringBuilder = (explanation != null) ? new StringBuilder() : null;
			float num = float.MaxValue;

			foreach(ThingDef vehicle in caravan.UniqueVehicleDefsInCaravan().ToList())
            {
				float numTmp = WorldVehiclePathGrid.CalculatedMovementDifficultyAt(end, vehicle, ticksAbs, stringBuilder);
				if(numTmp < num)
                {
					num = numTmp;
                }
            }
			
			float roadMovementDifficultyMultiplier = GetRoadMovementDifficultyMultiplier(caravan, start, end, stringBuilder);
			if (explanation != null)
			{
				explanation.AppendLine();
				explanation.Append("TileMovementDifficulty".Translate() + ":");
				explanation.AppendLine();
				explanation.Append(stringBuilder.ToString().Indented("  "));
				explanation.AppendLine();
				explanation.Append("  = " + (num * roadMovementDifficultyMultiplier).ToString("0.#"));
			}
			int num2 = (int)(caravanTicksPerMove * num * roadMovementDifficultyMultiplier);
			num2 = Mathf.Clamp(num2, 1, MaxMoveTicks);
			if (explanation != null)
			{
				explanation.AppendLine();
				explanation.AppendLine();
				explanation.Append("FinalCaravanMovementSpeed".Translate() + ":");
				int num3 = Mathf.CeilToInt(num2 / 1f);
				explanation.AppendLine();
				explanation.Append(string.Concat(new string[]
				{
					"  ",
					(60000f / caravanTicksPerMove).ToString("0.#"),
					" / ",
					(num * roadMovementDifficultyMultiplier).ToString("0.#"),
					" = ",
					(60000f / num3).ToString("0.#"),
					" "
				}) + "TilesPerDay".Translate());
			}
			return num2;
		}

		public static float GetRoadMovementDifficultyMultiplier(VehicleCaravan caravan, int fromTile, int toTile, StringBuilder explanation = null)
		{
			List<ThingDef> vehicleDefs = caravan.UniqueVehicleDefsInCaravan().ToList();
			return GetRoadMovementDifficultyMultiplier(vehicleDefs, fromTile, toTile, explanation);
		}

		public static float GetRoadMovementDifficultyMultiplier(List<ThingDef> vehicleDefs, int fromTile, int toTile, StringBuilder explanation = null)
		{
			List<Tile.RoadLink> roads = Find.WorldGrid.tiles[fromTile].Roads;
			if (roads == null)
			{
				return 1f;
			}
			if (toTile == -1)
			{
				toTile = Find.WorldGrid.FindMostReasonableAdjacentTileForDisplayedPathCost(fromTile);
			}
			for (int i = 0; i < roads.Count; i++)
			{
				if (roads[i].neighbor == toTile)
				{
					float movementCostMultiplier = roads[i].road.movementCostMultiplier;
					if(vehicleDefs.AnyNullified(v => !v.GetCompProperties<CompProperties_Vehicle>().customRoadCosts.EnumerableNullOrEmpty()))
                    {
						movementCostMultiplier = vehicleDefs.Min(v => v.GetCompProperties<CompProperties_Vehicle>().customRoadCosts[roads[i].road]);
                    }
					if (explanation != null)
					{
						if (explanation.Length > 0)
						{
							explanation.AppendLine();
						}
						explanation.Append(roads[i].road.LabelCap + ": " + movementCostMultiplier.ToStringPercent());
					}
					return movementCostMultiplier;
				}
			}
			return 1f;
		}

		public static bool IsValidFinalPushDestination(int tile)
		{
			List<WorldObject> allWorldObjects = Find.WorldObjects.AllWorldObjects;
			for (int i = 0; i < allWorldObjects.Count; i++)
			{
				if (allWorldObjects[i].Tile == tile && !(allWorldObjects[i] is Caravan))
				{
					return true;
				}
			}
			return false;
		}

		private float CostToPayThisTick()
		{
			float num = 1f;
			if (DebugSettings.fastCaravans)
			{
				num = 100f;
			}
			if (num < nextTileCostTotal / MaxMoveTicks)
			{
				num = nextTileCostTotal / MaxMoveTicks;
			}
			return num;
		}

		private bool TrySetNewPath()
		{
			WorldPath worldPath = GenerateNewPath();
			if (!worldPath.Found)
			{
				PatherFailed();
				return false;
			}
			if (curPath != null)
			{
				curPath.ReleaseToPool();
			}
			curPath = worldPath;
			return true;
		}

		private WorldPath GenerateNewPath()
		{
			int num = (moving && nextTile >= 0 && IsNextTilePassable()) ? nextTile : caravan.Tile;
			lastPathedTargetTile = destTile;
			WorldPath worldPath = Find.World.GetCachedWorldComponent<WorldVehiclePathfinder>().FindPath(num, destTile, caravan, null);

			if (worldPath.Found && num != caravan.Tile)
			{
				if (worldPath.NodesLeftCount >= 2 && worldPath.Peek(1) == caravan.Tile)
				{
					worldPath.ConsumeNextNode();
					if (moving)
					{
						previousTileForDrawingIfInDoubt = nextTile;
						nextTile = caravan.Tile;
						nextTileCostLeft = nextTileCostTotal - nextTileCostLeft;
					}
				}
				else
				{
					worldPath.AddNodeAtStart(caravan.Tile);
				}
			}
			return worldPath;
		}

		private bool AtDestinationPosition()
		{
			return caravan.Tile == destTile;
		}

		private bool NeedNewPath()
		{
			if (!moving)
			{
				return false;
			}
			if (curPath == null || !curPath.Found || curPath.NodesLeftCount == 0)
			{
				return true;
			}
			int num = 0;
			while (num < MaxCheckAheadNodes && num < curPath.NodesLeftCount)
			{
				int tileID = curPath.Peek(num);
				if (!IsPassable(tileID))
				{
					return true;
				}
				num++;
			}
			return false;
		}

		private VehicleCaravan caravan;

		private bool moving;

		private bool paused;

		public int nextTile = -1;

		public int previousTileForDrawingIfInDoubt = -1;

		public float nextTileCostLeft;

		public float nextTileCostTotal = 1f;

		private int destTile;

		private CaravanArrivalAction arrivalAction;

		public WorldPath curPath;

		public int lastPathedTargetTile;

		public const int MaxMoveTicks = 30000;
		private const int MaxCheckAheadNodes = 20;
		private const int MinCostWalk = 50;
		private const int MinCostAmble = 60;
		public const float DefaultPathCostToPayPerTick = 1f;
		public const int FinalNoRestPushMaxDurationTicks = 10000;
    }
}
