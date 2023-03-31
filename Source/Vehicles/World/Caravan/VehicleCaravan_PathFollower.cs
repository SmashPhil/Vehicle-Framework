using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class VehicleCaravan_PathFollower : IExposable
	{
		public const int MaxMoveTicks = 30000;
		private const int MaxCheckAheadNodes = 20;
		public const float DefaultPathCostToPayPerTick = 1f;
		public const int FinalNoRestPushMaxDurationTicks = 10000;

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

		public VehicleCaravan_PathFollower(VehicleCaravan caravan)
		{
			this.caravan = caravan;
		}

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
				return Moving && !Paused && !caravan.CantMove && !caravan.OutOfFuel;
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
					Log.Error("Tried to pause caravan movement of " + caravan.ToStringSafe<Caravan>() + " but it's not moving.");
				}
				else
				{
					paused = true;
				}
				caravan.Notify_DestinationOrPauseStatusChanged();
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
			if (!WorldVehicleReachability.Instance.CanReach(caravan, destTile))
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
			if (caravan.CantMove || caravan.OutOfFuel || paused)
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
			return caravan.UniqueVehicleDefsInCaravan().All(v => WorldVehiclePathGrid.Instance.Passable(tile, v));
		}

		public bool IsNextTilePassable()
		{
			return caravan.UniqueVehicleDefsInCaravan().All(v => WorldVehiclePathGrid.Instance.Passable(nextTile, v));
		}

		private bool TryRecoverFromUnwalkablePosition()
		{
			if (GenWorldClosest.TryFindClosestTile(caravan.Tile, (int t) => IsPassable(t) && WorldVehicleReachability.Instance.CanReach(caravan, t), out int tile, 2147483647, true))
			{
				Log.Warning($"{caravan} on impassable tile: {caravan.Tile}. Teleporting to {tile}");
				caravan.Tile = tile;
				caravan.Notify_VehicleTeleported();
				return true;
			}
			Log.Error($"{caravan} on impassable tile: {caravan.Tile}. Could not find moveable position nearby. Destroying caravan.");
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
				Log.Error(caravan + " ran out of path nodes. Force-arriving.");
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
				}));
				PatherFailed();
				return;
			}
			nextTile = curPath.ConsumeNextNode();
			previousTileForDrawingIfInDoubt = -1;
			if (!IsPassable(nextTile))
			{
				Log.Error($"{caravan} entering {nextTile} which is impassable");
			}
			int num = CostToMove(caravan.Tile, nextTile);
			nextTileCostTotal = num;
			nextTileCostLeft = num;
		}

		private int CostToMove(int start, int end)
		{
			return CostToMove(caravan, start, end);
		}

		public static int CostToMove(VehicleCaravan caravan, int start, int end, int? ticksAbs = null)
		{
			return CostToMove(caravan.UniqueVehicleDefsInCaravan().ToList(), caravan.TicksPerMove, start, end, ticksAbs);
		}

		public static int CostToMove(List<VehicleDef> vehicleDefs, int ticksPerMove, int start, int end, int? ticksAbs = null, StringBuilder explanation = null, string caravanTicksPerMoveExplanation = null)
		{
			if (start == end)
			{
				return 0;
			}
			explanation?.AppendLine(caravanTicksPerMoveExplanation);
			StringBuilder stringBuilder = (explanation != null) ? new StringBuilder() : null;
			float cost = float.MaxValue;

			foreach (VehicleDef vehicle in vehicleDefs)
			{
				float newCost = WorldVehiclePathGrid.CalculatedMovementDifficultyAt(end, vehicle, ticksAbs, stringBuilder);
				if (newCost < cost)
				{
					cost = newCost;
				}
			}
			
			float roadMovementDifficultyMultiplier = GetRoadMovementDifficultyMultiplier(vehicleDefs, start, end, stringBuilder);
			if (explanation != null)
			{
				explanation.AppendLine();
				explanation.AppendLine("TileMovementDifficulty".Translate() + ":");
				explanation.AppendLine(stringBuilder.ToString().Indented("  "));
				explanation.AppendLine($"  = {cost * roadMovementDifficultyMultiplier:0.#}");
			}
			int finalCost = (int)(ticksPerMove * cost * roadMovementDifficultyMultiplier);
			finalCost = Mathf.Clamp(finalCost, 1, MaxMoveTicks);
			if (explanation != null)
			{
				explanation.AppendLine();
				explanation.AppendLine("FinalCaravanMovementSpeed".Translate() + ":");
				int num3 = Mathf.CeilToInt(finalCost / 1f);
				explanation.Append($"  {GenDate.TicksPerDay / ticksPerMove:0.#} / {cost * roadMovementDifficultyMultiplier:0.#} = {GenDate.TicksPerDay / num3:0.#} {"TilesPerDay".Translate()}");
			}
			return finalCost;
		}

		public static float GetRoadMovementDifficultyMultiplier(VehicleCaravan caravan, int fromTile, int toTile, StringBuilder explanation = null)
		{
			List<VehicleDef> vehicleDefs = caravan.UniqueVehicleDefsInCaravan().ToList();
			return GetRoadMovementDifficultyMultiplier(vehicleDefs, fromTile, toTile, explanation);
		}

		public static float GetRoadMovementDifficultyMultiplier(List<VehicleDef> vehicleDefs, int fromTile, int toTile, StringBuilder explanation = null)
		{
			List<Tile.RoadLink> roads = Find.WorldGrid.tiles[fromTile].Roads;
			if (roads == null)
			{
				return Mathf.Clamp(vehicleDefs.Max(vehicleDef => vehicleDef.properties.offRoadMultiplier), 0.1f, 100);
			}
			if (toTile == -1)
			{
				toTile = Find.WorldGrid.FindMostReasonableAdjacentTileForDisplayedPathCost(fromTile);
			}
			for (int i = 0; i < roads.Count; i++)
			{
				if (roads[i].neighbor == toTile)
				{
					float roadMultiplier = GetRoadMovementDifficultyMultiplier(vehicleDefs, roads[i].road);
					
					if (explanation != null)
					{
						if (explanation.Length > 0)
						{
							explanation.AppendLine();
						}
						explanation.Append($"{roads[i].road.LabelCap}: {roadMultiplier.ToStringPercent()}");
					}
					return roadMultiplier;
				}
			}
			return 1f;
		}

		public static float GetRoadMovementDifficultyMultiplier(List<VehicleDef> vehicleDefs, RoadDef roadDef)
		{
			float roadMultiplier = roadDef.movementCostMultiplier;
			bool customRoadCosts = false;
			foreach (VehicleDef vehicleDef in vehicleDefs)
			{
				if (vehicleDef.properties.customRoadCosts.TryGetValue(roadDef, out float movementCostMultiplier) && (!customRoadCosts || movementCostMultiplier < roadMultiplier))
				{
					customRoadCosts = true;
					roadMultiplier = movementCostMultiplier;
				}
			}
			return roadMultiplier;
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
			WorldPath worldPath = WorldVehiclePathfinder.Instance.FindPath(num, destTile, caravan, null);

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
			for (int i = 0; i < MaxCheckAheadNodes && i < curPath.NodesLeftCount; i++)
			{
				int tileID = curPath.Peek(i);
				if (!IsPassable(tileID))
				{
					return true;
				}
			}
			return false;
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
	}
}
