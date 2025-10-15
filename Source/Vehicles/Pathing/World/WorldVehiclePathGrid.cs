using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Performance;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.World;

/// <summary>
/// WorldGrid for vehicles
/// </summary>
public class WorldVehiclePathGrid : WorldComponent
{
	public const float ImpassableMovementDifficulty = 1000f;

	private static readonly Func<Hilliness, float> HillinessMovementDifficultyOffset;

	public event ReachabilityGridDirtyed OnReachabilityDirty;

	/// <summary>
	/// Store entire pathGrid for each <see cref="VehicleDef"/>
	/// </summary>
	public readonly PathGrid[] pathGrids;

	public readonly WorldVehicleReachability reachability;

	private readonly float[] winter;

	private int allPathCostsRecalculatedDayOfYear = -1;
	private CancellationTokenSource cts;
	private Task curTask;

	public delegate void ReachabilityGridDirtyed(VehicleDef def, CancellationToken token);

	static WorldVehiclePathGrid()
	{
		// Removes singleton references, we shouldn't rely on reference being overwritten on
		// subsequent playthroughs.
		// TODO - This should be cleaned up and not reference a static property at some point.
		GameEvent.OnWorldRemoved += () => Instance = null;
		GameEvent.OnGameDisposing += CancelGridRequests;

		MethodInfo hillinessMethod =
			AccessTools.Method(typeof(WorldPathGrid), "HillinessMovementDifficultyOffset");
		HillinessMovementDifficultyOffset =
			(Func<Hilliness, float>)Delegate.CreateDelegate(typeof(Func<Hilliness, float>),
				hillinessMethod);
	}

	public WorldVehiclePathGrid(RimWorld.Planet.World world) : base(world)
	{
		this.world = world;
		pathGrids = new PathGrid[DefDatabase<VehicleDef>.DefCount];
		winter = new float[Find.WorldGrid.TilesCount];
		ResetPathGrid();
		Initialized = false;
		Instance = this;
		reachability = new WorldVehicleReachability(this);
	}

	/// <summary>
	/// Singleton getter
	/// </summary>
	public static WorldVehiclePathGrid Instance { get; private set; }

	private bool Recalculating { get; set; }

	public bool Initialized { get; private set; }

	public PathGrid this[VehicleDef vehicleDef] => pathGrids[vehicleDef.DefIndex];

	/// <summary>
	/// Day of year at 0 longitude for recalculating pathGrids
	/// </summary>
	private static int DayOfYearAt0Long => GenDate.DayOfYear(GenTicks.TicksAbs, 0f);

	private void ResetPathGrid()
	{
		// TODO - implement piggybacking for path grids
		foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
		{
			pathGrids[vehicleDef.DefIndex] =
				new PathGrid(vehicleDef, Find.WorldGrid.TilesCount);
		}
	}

	/// <summary>
	/// Recalculate all perceived path costs at <see cref="DayOfYearAt0Long"/>
	/// </summary>
	public override void WorldComponentTick()
	{
		if (!Recalculating && allPathCostsRecalculatedDayOfYear != DayOfYearAt0Long)
		{
#if DEV_TOOLS
			// Run synchronously if conducting unit tests, as inconsistent timing can invalidate test results.
			if (TestWatcher.RunningTests)
				RecalculateAllPerceivedPathCosts(CancellationToken.None);
			else
#endif
				RecalculateAllPathCostsAsync();
		}

		if (Prefs.DevMode)
			FlashWorldGrid();
	}

	private void FlashWorldGrid()
	{
		// Twice per second at 60 tps
		if (DebugHelper.World.VehicleDef == null || !Find.WorldSelector.SelectedTile.Valid ||
			Find.TickManager.TicksGame % 30 != 0)
		{
			return;
		}

		switch (DebugHelper.World.DebugType)
		{
			case WorldPathingDebugType.PathCosts:
			{
				PlanetTile tile = Find.WorldSelector.SelectedTile;
				List<PlanetTile> neighbors = [];
				Find.WorldGrid.GetTileNeighbors(tile, neighbors);

				float cost = pathGrids[DebugHelper.World.VehicleDef.DefIndex][tile];
				Find.World.debugDrawer.FlashTile(tile, colorPct: cost * 10 / ImpassableMovementDifficulty,
					text: cost.ToString(), duration: 15);
				foreach (PlanetTile neighborTile in neighbors)
				{
					Find.World.debugDrawer.FlashTile(neighborTile,
						text: pathGrids[DebugHelper.World.VehicleDef.DefIndex][neighborTile]
						 .ToString(), duration: 30);
				}
			}
			break;
			case WorldPathingDebugType.Reachability:
			{
				PlanetTile tile = Find.WorldSelector.SelectedTile;
				List<PlanetTile> neighbors = [];
				Ext_World.Bfs(tile, neighbors.Add, radius: 10);

				Find.World.debugDrawer.FlashTile(tile, colorPct: 0.8f, text: IdStringAt(tile), 15);
				foreach (PlanetTile neighbor in neighbors)
				{
					bool canReach =
						Instance.reachability.CanReach(vehicleDef: DebugHelper.World.VehicleDef,
							tile, neighbor);
					float colorPct = canReach ? 0.65f : 0f;
					Find.World.debugDrawer.FlashTile(neighbor, colorPct: colorPct,
						text: IdStringAt(neighbor),
						duration: 30);
				}

				static string IdStringAt(PlanetTile t) => Instance.reachability.GetRegionId(
					DebugHelper.World.VehicleDef,
					t).ToString();
			}
			break;
			case WorldPathingDebugType.WinterPct:
			{
				PlanetTile tile = Find.WorldSelector.SelectedTile;
				List<PlanetTile> neighbors = [];
				Ext_World.Bfs(tile, neighbors.Add, radius: 10);

				float winterPct = WinterPercentAt(tile);
				Find.World.debugDrawer.FlashTile(tile, colorPct: winterPct,
					text: winterPct.ToString("#.00"), duration: 15);
				foreach (PlanetTile neighbor in neighbors)
				{
					winterPct = WinterPercentAt(neighbor);
					Find.World.debugDrawer.FlashTile(neighbor, colorPct: winterPct,
						text: winterPct.ToString("#.00"), duration: 30);
				}
			}
			break;
			default:
				throw new NotImplementedException(nameof(WorldPathingDebugType));
		}
	}

	/// <summary>
	/// <paramref name="tile"/> is passable for <paramref name="vehicleDef"/>
	/// </summary>
	/// <param name="tile"></param>
	/// <param name="vehicleDef"></param>
	public bool Passable(PlanetTile tile, VehicleDef vehicleDef)
	{
		return Find.WorldGrid.InBounds(tile) && pathGrids[vehicleDef.DefIndex][tile] <
			ImpassableMovementDifficulty;
	}

	/// <summary>
	/// <paramref name="tile"/> is passable for <paramref name="vehicleDef"/> (no bounds check)
	/// </summary>
	/// <param name="tile"></param>
	/// <param name="vehicleDef"></param>
	public bool PassableFast(PlanetTile tile, VehicleDef vehicleDef)
	{
		return pathGrids[vehicleDef.DefIndex][tile] < ImpassableMovementDifficulty;
	}

	/// <summary>
	/// pathCost for <paramref name="vehicleDef"/> at <paramref name="tile"/>
	/// </summary>
	/// <param name="tile"></param>
	/// <param name="vehicleDef"></param>
	public float PerceivedMovementDifficultyAt(PlanetTile tile, VehicleDef vehicleDef)
	{
		return pathGrids[vehicleDef.DefIndex][tile];
	}

	public float WinterPercentAt(PlanetTile tile)
	{
		return winter[tile];
	}

	/// <summary>
	/// Recomputes and stores the perceived movement difficulty for a single world tile
	/// and reports which part of the pathing data needs updating.
	/// </summary>
	/// <param name="tile">
	/// The <see cref="PlanetTile"/> whose difficulty to recalculate.
	/// </param>
	/// <param name="vehicleDef">
	/// The <see cref="VehicleDef"/> for which the difficulty is calculated.
	/// </param>
	/// <returns>
	/// A <see cref="GridState"/> flag indicating:
	/// <list type="bullet">
	///   <item>
	///     <term>None</term>
	///     <description>If <paramref name="tile"/> is out of bounds.</description>
	///   </item>
	///   <item>
	///     <term>RegionsDirty</term>
	///     <description>If the tile’s passability crossed the impassable threshold, potentially changing region connectivity.</description>
	///   </item>
	///   <item>
	///     <term>PathGridDirty</term>
	///     <description>If only the movement cost changed but passability remained the same.</description>
	///   </item>
	/// </list>
	/// </returns>
	[PublicAPI]
	public GridState RecalculatePerceivedMovementDifficultyAt(PlanetTile tile, VehicleDef vehicleDef)
	{
		if (!Find.WorldGrid.InBounds(tile))
			return GridState.None;
		PathGrid pathGrid = pathGrids[vehicleDef.DefIndex];
		float before = pathGrid[tile.tileId];
		float after = CalculatedMovementDifficultyAt(tile, vehicleDef);
		pathGrid[tile.tileId] = after;
		if (before >= ImpassableMovementDifficulty ^ after >= ImpassableMovementDifficulty)
			return GridState.RegionsDirty;
		return GridState.PathGridDirty;
	}

	/// <summary>
	/// Recalculate all path costs for all VehicleDefs
	/// </summary>
	private void RecalculateAllPathCostsAsync()
	{
		if (Recalculating)
		{
			Trace.Fail(
				"Attempting to regenerate world path grid for all vehicles but it is already running.");
			return;
		}
		allPathCostsRecalculatedDayOfYear = DayOfYearAt0Long;

		if (curTask is { Status: TaskStatus.Running })
		{
			const int MaxCancelWaitTime = 1000;

			Trace.Fail("Restarting task while it is ongoing. Cancelling before continuing.");
			cts.Cancel();
			Task.WaitAll([curTask], MaxCancelWaitTime);
		}

		cts = new CancellationTokenSource();
		curTask = TaskManager.Run(delegate
		{
			try
			{
				RecalculateAllPerceivedPathCosts(cts.Token);
			}
			finally
			{
				cts.Dispose();
				cts = null;
				curTask = null;
			}
		}, cts.Token);
	}

	/// <summary>
	/// Recalculate all path costs for all VehicleDefs
	/// </summary>
	private void RecalculateAllPerceivedPathCosts(CancellationToken token, int? ticksAbs = null)
	{
		allPathCostsRecalculatedDayOfYear = DayOfYearAt0Long;

		using GridInitializerState gis = new(this);
		foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
		{
			RecalculateAllPerceivedPathCostsFor(vehicleDef, token);
		}

		// Only needs to be done once and not for every grid owner
		for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
		{
			RecalculateWinterPercentAt(i, ticksAbs);
		}
	}

	[Profile]
	internal void RecalculateAllPerceivedPathCostsFor(VehicleDef vehicleDef, CancellationToken token)
	{
		bool dirty = false;
		for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
		{
			if (token.IsCancellationRequested)
				return;

			GridState state = RecalculatePerceivedMovementDifficultyAt(i, vehicleDef);
			dirty |= state == GridState.RegionsDirty;
		}
		if (dirty)
			OnReachabilityDirty?.Invoke(vehicleDef, token);
	}

	private void RecalculateWinterPercentAt(PlanetTile tile, int? ticksAbs = null)
	{
		winter[tile] = WinterPathingHelper.GetWinterPercent(tile, ticksAbs: ticksAbs);
	}

	// NOTE - If we use an instance event listener we would have to deregister it every time the world is removed
	private static void CancelGridRequests()
	{
		WorldVehiclePathGrid pathGrid = Find.World.GetComponent<WorldVehiclePathGrid>();
		if (pathGrid.curTask == null || pathGrid.curTask.IsCompleted || pathGrid.curTask is
			{ Status: TaskStatus.Canceled or TaskStatus.Faulted })
			return;

		const int MaxCancelWaitTime = 5000;

		// We need to explicitly await on the thread exiting to main menu / desktop or it will still race against
		// the cancellation request.
		Assert.IsTrue(ThreadManager.InMainOrEventThread);
		pathGrid.cts.Cancel();
		Task.WaitAll([pathGrid.curTask], MaxCancelWaitTime);
	}

	/// <summary>
	/// Calculate path cost for <paramref name="vehicleDef"/> at <paramref name="tile"/>
	/// </summary>
	[Profile]
	public static float CalculatedMovementDifficultyAt(PlanetTile tile, VehicleDef vehicleDef,
		StringBuilder explanation = null, bool coastalTravel = true)
	{
		Tile worldTile = Find.WorldGrid[tile];
		if (worldTile is not SurfaceTile surfaceTile)
		{
			Log.Error("Attempting to calculate movement difficulty for non-surface tile.");
			return ImpassableMovementDifficulty;
		}

		if (explanation is { Length: > 0 })
			explanation.AppendLine();

		List<SurfaceTile.RiverLink> rivers = surfaceTile.Rivers;
		if (!rivers.NullOrEmpty())
		{
			SurfaceTile.RiverLink riverLink = WorldHelper.BiggestRiverOnTile(rivers);
			if (riverLink.river != null)
			{
				if ((vehicleDef.properties.defaultImpassable & DefaultImpassable.Rivers) != 0)
				{
					explanation?.Append($"{riverLink.river.LabelCap}: Impassable");
					return ImpassableMovementDifficulty;
				}
				if (vehicleDef.properties.customRiverCosts.TryGetValue(riverLink.river, out float riverCost)
					&& !Mathf.Approximately(riverCost, ImpassableMovementDifficulty))
				{
					explanation?.Append($"{riverLink.river.LabelCap}: {riverCost.ToStringWithSign("0.#")}");
					return riverCost;
				}
			}
		}

		float defaultBiomeCost;
		if ((vehicleDef.properties.defaultImpassable & DefaultImpassable.Biomes) != 0)
		{
			defaultBiomeCost = ImpassableMovementDifficulty;
		}
		else
		{
			BiomeDef biomeDef = surfaceTile.PrimaryBiome;
			defaultBiomeCost = biomeDef.impassable ?
				ImpassableMovementDifficulty :
				biomeDef.movementDifficulty;
		}

		if (coastalTravel && vehicleDef.CoastalTravel(tile))
		{
			defaultBiomeCost = Mathf.Min(defaultBiomeCost,
				vehicleDef.properties.customBiomeCosts[BiomeDefOf.Ocean]);
		}

		float biomeCost =
			vehicleDef.properties.customBiomeCosts.TryGetValue(surfaceTile.PrimaryBiome,
				defaultBiomeCost);

		if (!vehicleDef.properties.customHillinessCosts.TryGetValue(surfaceTile.hilliness, out float hillinessCost))
		{
			if ((vehicleDef.properties.defaultImpassable & DefaultImpassable.Roads) != 0)
				return ImpassableMovementDifficulty;

			hillinessCost = HillinessMovementDifficultyOffset(surfaceTile.hilliness);
		}

		if (biomeCost >= ImpassableMovementDifficulty || hillinessCost >= ImpassableMovementDifficulty)
		{
			explanation?.Append("Impassable".Translate());
			return ImpassableMovementDifficulty;
		}

		if (!surfaceTile.Roads.NullOrEmpty())
		{
			if ((vehicleDef.properties.defaultImpassable & DefaultImpassable.Roads) != 0 &&
				!surfaceTile.Roads.Exists(PassableRoad))
			{
				return ImpassableMovementDifficulty;
			}
			if (biomeCost < ImpassableMovementDifficulty && VehicleMod.settings.main.ignoreBiomeCostOnRoads)
			{
				biomeCost = 1;
				hillinessCost = 0;
			}
		}

		explanation?.Append(surfaceTile.PrimaryBiome.LabelCap + ": " +
			biomeCost.ToStringWithSign("0.#"));

		float totalCost = biomeCost + hillinessCost;
		if (explanation != null && !Mathf.Approximately(hillinessCost, 0))
		{
			explanation.AppendLine();
			explanation.Append(surfaceTile.hilliness.GetLabelCap() + ": " +
				hillinessCost.ToStringWithSign("0.#"));
		}
		totalCost +=
			WinterPathingHelper.GetCurrentWinterMovementDifficultyFor(vehicleDef, tile,
				explanation: explanation);
		return totalCost;

		bool PassableRoad(SurfaceTile.RoadLink roadLink)
		{
			return vehicleDef.properties.customRoadCosts.ContainsKey(roadLink.road);
		}
	}

	/// <summary>
	/// Max cost on <paramref name="tile"/> given neighbor tile <paramref name="neighbor"/> for <paramref name="vehicleDef"/>
	/// </summary>
	/// <remarks>
	/// <paramref name="tile"/> must have coast
	/// </remarks>
	/// <param name="tile"></param>
	/// <param name="neighbor"></param>
	/// <param name="vehicleDef"></param>
	public static float ConsistentDirectionCost(PlanetTile tile, PlanetTile neighbor,
		VehicleDef vehicleDef)
	{
		return Mathf.Max(CalculatedMovementDifficultyAt(tile, vehicleDef),
			CalculatedMovementDifficultyAt(neighbor, vehicleDef));
	}

	[DebugAction(VehicleHarmony.VehiclesLabel, name = "Regen WorldGrid",
		allowedGameStates = AllowedGameStates.PlayingOnWorld)]
	private static void RecalculatePathGrid()
	{
		Find.World.GetComponent<WorldVehiclePathGrid>().RecalculateAllPerceivedPathCosts(CancellationToken.None);
	}

	[DebugAction(VehicleHarmony.VehiclesLabel, name = "Regen WorldReachability",
		allowedGameStates = AllowedGameStates.PlayingOnWorld)]
	private static void RecalculateReachabilityGrid()
	{
		WorldVehiclePathGrid pathGrid = Find.World.GetComponent<WorldVehiclePathGrid>();
		pathGrid.cts = new CancellationTokenSource();
		pathGrid.curTask = TaskManager.Run(delegate
		{
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
				pathGrid.OnReachabilityDirty?.Invoke(vehicleDef, pathGrid.cts.Token);
		}, pathGrid.cts.Token);
	}

	[PublicAPI]
	public enum GridState
	{
		None,
		PathGridDirty,
		RegionsDirty
	}

	private readonly struct GridInitializerState : IDisposable
	{
		private readonly WorldVehiclePathGrid pathGrid;

		public GridInitializerState(WorldVehiclePathGrid pathGrid)
		{
			this.pathGrid = pathGrid;
			this.pathGrid.Initialized = false;
			this.pathGrid.Recalculating = true;
		}

		void IDisposable.Dispose()
		{
			pathGrid.Recalculating = false;
			pathGrid.Initialized = true;
		}
	}

	[PublicAPI]
	public class PathGrid
	{
		private VehicleDef owner;
		private readonly float[] costs;

		public float this[int index]
		{
			get => costs[index];
			set => costs[index] = value;
		}

		public PathGrid(VehicleDef owner, int size)
		{
			this.owner = owner;
			costs = new float[size];
		}

		public bool Enabled { get; internal set; }

		public VehicleDef Owner => owner;
	}
}