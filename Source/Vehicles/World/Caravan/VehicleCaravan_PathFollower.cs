using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools.Performance;
using UnityEngine;
using Verse;

namespace Vehicles.World;

[PublicAPI]
public sealed class VehicleCaravan_PathFollower : IExposable
{
	private const int MaxMoveTicks = 30000;
	private const int MaxCheckAheadNodes = 20;

	public const float DefaultPathCostToPayPerTick = 1f;
	public const int FinalNoRestPushMaxDurationTicks = 10000;

	private readonly VehicleCaravan caravan;

	private bool moving;
	private bool paused;
	private PlanetTile nextTile = PlanetTile.Invalid;
	public int previousTileForDrawingIfInDoubt = -1;
	public float nextTileCostLeft;
	public float nextTileCostTotal = 1f;
	private PlanetTile destTile;
	private CaravanArrivalAction arrivalAction;
	public WorldPath curPath;

	public VehicleCaravan_PathFollower(VehicleCaravan caravan)
	{
		this.caravan = caravan;
	}

	public PlanetTile Destination => destTile;

	public PlanetTile NextTile => nextTile;

	public bool Moving
	{
		get { return moving && caravan.Spawned; }
	}

	public bool MovingNow
	{
		get
		{
			return Moving && !Paused && !caravan.CantMove && !caravan.OutOfFuel &&
				!caravan.VehicleCantMove;
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
		get { return Moving && paused; }
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
				Log.Error("Tried to pause caravan movement of " + caravan.ToStringSafe<Caravan>() +
					" but it's not moving.");
			}
			else
			{
				paused = true;
			}
			caravan.Notify_DestinationOrPauseStatusChanged();
		}
	}

	public bool StartPath(PlanetTile destTile, CaravanArrivalAction arrivalAction,
		bool repathImmediately = false, bool resetPauseStatus = true)
	{
		caravan.EnsureWorldGridInitialized();
		caravan.autoJoinable = false;
		if (resetPauseStatus)
		{
			paused = false;
		}
		if (arrivalAction != null && !arrivalAction.StillValid(caravan, destTile))
		{
			return false;
		}
		if (!IsPassable(caravan.Tile) && !TryRecoverFromUnwalkablePosition())
		{
			return false;
		}
		if (moving && curPath != null && this.destTile == destTile)
		{
			this.arrivalAction = arrivalAction;
			return true;
		}
		if (!WorldVehiclePathGrid.Instance.reachability.CanReach(caravan, destTile))
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
			Messages.Message(
				"MessageCaravanArrivalActionNoLongerValid".Translate(caravan.Name).CapitalizeFirst() +
				(failMessage != null ? " " + failMessage : ""), caravan,
				MessageTypeDefOf.NegativeEvent);
			StopDead();
		}
		if (caravan.CantMove || caravan.VehicleCantMove || caravan.OutOfFuel || paused)
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

	public bool IsPassable(PlanetTile tile)
	{
		return caravan.UniqueVehicleDefsInCaravan()
		 .All(v => WorldVehiclePathGrid.Instance.Passable(tile, v));
	}

	public bool IsNextTilePassable()
	{
		return caravan.UniqueVehicleDefsInCaravan()
		 .All(v => WorldVehiclePathGrid.Instance.Passable(nextTile, v));
	}

	private bool TryRecoverFromUnwalkablePosition()
	{
		if (caravan.VehiclesListForReading.All(vehicle =>
			vehicle.VehicleDef.type == VehicleType.Air))
		{
			return false;
		}
		if (GenWorldClosest.TryFindClosestTile(caravan.Tile,
			planetTile => IsPassable(planetTile) &&
				WorldVehiclePathGrid.Instance.reachability.CanReach(caravan, planetTile),
			out PlanetTile tile))
		{
			Log.Warning($"{caravan} on impassable tile: {caravan.Tile}. Teleporting to {tile}");
			caravan.Tile = tile;
			caravan.Notify_VehicleTeleported();
			return true;
		}
		Log.Error(
			$"{caravan} on impassable tile: {caravan.Tile}. Could not find moveable position nearby. Destroying caravan.");
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
			Messages.Message("MessageCaravanArrivedAtDestination".Translate(caravan.Label), caravan,
				MessageTypeDefOf.TaskCompletion);
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
			Log.Error($"{caravan} at {caravan.Tile} ran out of path nodes while pathing to {destTile}.");
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

	private int CostToMove(PlanetTile start, PlanetTile end)
	{
		return CostToMove(caravan, start, end);
	}

	public static int CostToMove(VehicleCaravan caravan, PlanetTile start, PlanetTile end,
		int? ticksAbs = null)
	{
		return CostToMove(caravan.VehiclesListForReading, caravan.TicksPerMove, start, end,
			ticksAbs: ticksAbs);
	}

	[Profile]
	public static int CostToMove(List<VehicleDef> vehicleDefs, int ticksPerMove, PlanetTile start,
		PlanetTile end, int? ticksAbs = null, StringBuilder explanation = null,
		string caravanTicksPerMoveExplanation = null)
	{
		if (start == end)
		{
			return 0;
		}
		explanation?.AppendLine(caravanTicksPerMoveExplanation);
		StringBuilder stringBuilder = (explanation != null) ? new StringBuilder() : null;
		float cost = float.MaxValue;

		foreach (VehicleDef vehicleDef in vehicleDefs)
		{
			float newCost =
				WorldVehiclePathGrid.CalculatedMovementDifficultyAt(end, vehicleDef, stringBuilder);
			if (newCost < cost)
			{
				cost = newCost;
			}
		}

		float winterOffset =
			WinterPathingHelper.GetCurrentWinterMovementDifficultyOffset(vehicleDefs, end,
				stringBuilder);
		cost += winterOffset;

		float roadMovementDifficultyMultiplier =
			RoadCostHelper.GetRoadMovementDifficultyMultiplier(vehicleDefs, start, end, stringBuilder);
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
			explanation.Append(
				$"  {GenDate.TicksPerDay / ticksPerMove:0.#} / {cost * roadMovementDifficultyMultiplier:0.#} = {GenDate.TicksPerDay / num3:0.#} {"TilesPerDay".Translate()}");
		}
		return finalCost;
	}

	[Profile]
	public static int CostToMove(List<VehiclePawn> vehicles, int ticksPerMove, PlanetTile start,
		PlanetTile end,
		int? ticksAbs = null, StringBuilder explanation = null,
		string caravanTicksPerMoveExplanation = null)
	{
		if (start == end)
			return 0;
		explanation?.AppendLine(caravanTicksPerMoveExplanation);
		StringBuilder stringBuilder = (explanation != null) ? new StringBuilder() : null;
		float cost = float.MaxValue;

		foreach (VehiclePawn vehicle in vehicles)
		{
			float newCost = WorldVehiclePathGrid.CalculatedMovementDifficultyAt(end, vehicle.VehicleDef, stringBuilder);
			if (newCost < cost)
			{
				cost = newCost;
			}
		}

		float winterOffset =
			WinterPathingHelper.GetCurrentWinterMovementDifficultyOffset(vehicles, end, stringBuilder);
		cost += winterOffset;

		float roadMovementDifficultyMultiplier =
			RoadCostHelper.GetRoadMovementDifficultyMultiplier(vehicles, start, end, stringBuilder);
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
			explanation.Append(
				$"  {GenDate.TicksPerDay / ticksPerMove:0.#} / {cost * roadMovementDifficultyMultiplier:0.#} = {GenDate.TicksPerDay / num3:0.#} {"TilesPerDay".Translate()}");
		}
		return finalCost;
	}

	public static bool IsValidFinalPushDestination(PlanetTile tile)
	{
		foreach (WorldObject worldObject in Find.WorldObjects.AllWorldObjects)
		{
			if (worldObject.Tile == tile && worldObject is not Caravan)
				return true;
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
		int num = moving && nextTile.Valid && IsNextTilePassable() ? nextTile : caravan.Tile;
		WorldPath worldPath = WorldVehiclePathfinder.Instance.FindPath(num, destTile, caravan);

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
		Scribe_Values.Look(ref moving, nameof(moving), true);
		Scribe_Values.Look(ref paused, nameof(paused));
		Scribe_Values.Look(ref nextTile, nameof(nextTile));
		Scribe_Values.Look(ref previousTileForDrawingIfInDoubt,
			nameof(previousTileForDrawingIfInDoubt));
		Scribe_Values.Look(ref nextTileCostLeft, nameof(nextTileCostLeft));
		Scribe_Values.Look(ref nextTileCostTotal, nameof(nextTileCostTotal));
		Scribe_Values.Look(ref destTile, nameof(destTile));
		Scribe_Deep.Look(ref arrivalAction, nameof(arrivalAction));
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			// If vehicles failed to load, it may no longer be a valid VehicleCaravan
			caravan.RecacheVehiclesOrConvertCaravan();
			if (Current.ProgramState != ProgramState.Entry && moving &&
				!StartPath(destTile, arrivalAction, true, false))
			{
				StopDead();
			}
		}
	}
}