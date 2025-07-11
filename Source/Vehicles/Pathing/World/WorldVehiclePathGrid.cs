using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using JetBrains.Annotations;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Verse;

namespace Vehicles.World;

/// <summary>
/// WorldGrid for vehicles
/// </summary>
[PublicAPI]
public class WorldVehiclePathGrid : WorldComponent
{
  public const float ImpassableMovementDifficulty = 1000f;

  private static readonly Func<Hilliness, float> HillinessMovementDifficultyOffset;

  public event Action<VehicleDef> OnPathGridRecalculated;

  /// <summary>
  /// Store entire pathGrid for each <see cref="VehicleDef"/>
  /// </summary>
  public readonly PathGrid[] pathGrids;

  public readonly WorldVehicleReachability reachability;

  private readonly float[] winter;

  private int allPathCostsRecalculatedDayOfYear = -1;

  static WorldVehiclePathGrid()
  {
    // Remove singleton reference, we shouldn't rely on reference being overwritten on
    // subsequent playthroughs.
    GameEvent.OnWorldRemoved += () => Instance = null;

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

  public bool Recalculating { get; private set; }

  public bool Initialized { get; private set; }

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
      if (!TestWatcher.RunningUnitTests)
        RunTaskRecalculateAllPathCosts();
      else
        RecalculateAllPerceivedPathCosts();
#else
        RunTaskRecalculateAllPathCosts();
#endif
    }

    if (Prefs.DevMode)
      FlashWorldGrid();
  }

  private void FlashWorldGrid()
  {
    if (DebugHelper.World.VehicleDef != null && Find.WorldSelector.SelectedTile >= 0 &&
      Find.TickManager.TicksGame % 30 == 0) //Twice per second at 60fps
    {
      if (DebugHelper.World.DebugType == WorldPathingDebugType.PathCosts)
      {
        PlanetTile tile = Find.WorldSelector.SelectedTile;
        List<PlanetTile> neighbors = [];
        Find.WorldGrid.GetTileNeighbors(tile, neighbors);

        float cost = pathGrids[DebugHelper.World.VehicleDef.DefIndex][tile];
        Find.World.debugDrawer.FlashTile(tile, colorPct: cost * 10 / ImpassableMovementDifficulty,
          text: cost.ToString(), duration: 15);
        foreach (int neighborTile in neighbors)
        {
          Find.World.debugDrawer.FlashTile(neighborTile,
            text: pathGrids[DebugHelper.World.VehicleDef.DefIndex][neighborTile]
             .ToString(), duration: 30);
        }
      }
      else if (DebugHelper.World.DebugType == WorldPathingDebugType.Reachability)
      {
        PlanetTile tile = Find.WorldSelector.SelectedTile;
        List<int> neighbors = [];
        Ext_World.Bfs(tile, neighbors, radius: 10);

        Find.World.debugDrawer.FlashTile(tile, colorPct: 0.8f, text: IdStringAt(tile), 15);
        foreach (int neighbor in neighbors)
        {
          bool canReach =
            Instance.reachability.CanReach(vehicleDef: DebugHelper.World.VehicleDef,
              tile, neighbor);
          float colorPct = canReach ? 0.65f : 0f;
          Find.World.debugDrawer.FlashTile(neighbor, colorPct: colorPct,
            text: IdStringAt(neighbor),
            duration: 30);
        }

        static string IdStringAt(int t) => Instance.reachability.GetRegionId(
          DebugHelper.World.VehicleDef,
          t).ToString();
      }
      else if (DebugHelper.World.DebugType == WorldPathingDebugType.WinterPct)
      {
        PlanetTile tile = Find.WorldSelector.SelectedTile;
        List<int> neighbors = [];
        Ext_World.Bfs(tile, neighbors, radius: 10);

        float winterPct = WinterPercentAt(tile);
        Find.World.debugDrawer.FlashTile(tile, colorPct: winterPct,
          text: winterPct.ToString("#.00"), duration: 15);
        foreach (int neighbor in neighbors)
        {
          winterPct = WinterPercentAt(neighbor);
          Find.World.debugDrawer.FlashTile(neighbor, colorPct: winterPct,
            text: winterPct.ToString("#.00"), duration: 30);
        }
      }
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
  /// Recalculate pathCost at <paramref name="tile"/> for <paramref name="vehicleDef"/>
  /// </summary>
  private void RecalculatePerceivedMovementDifficultyAt(PlanetTile tile, VehicleDef vehicleDef,
    int? ticksAbs = null)
  {
    if (!Find.WorldGrid.InBounds(tile))
    {
      return;
    }
    pathGrids[vehicleDef.DefIndex][tile] =
      CalculatedMovementDifficultyAt(tile, vehicleDef, ticksAbs);
  }

  /// <summary>
  /// Recalculate all path costs for all VehicleDefs
  /// </summary>
  private void RunTaskRecalculateAllPathCosts()
  {
    if (Recalculating)
    {
      Trace.Fail(
        "Attempting to regenerate world path grid for all vehicles but it is already running.");
      return;
    }
    allPathCostsRecalculatedDayOfYear = DayOfYearAt0Long;
    TaskManager.RunAsync(RecalculateAllAsync);
  }

  // Shorthand method for async task on method with 1 optional parameter
  private void RecalculateAllAsync()
  {
    RecalculateAllPerceivedPathCosts(ticksAbs: null);
  }

  /// <summary>
  /// Recalculate all path costs for all VehicleDefs
  /// </summary>
  /// <param name="ticksAbs"></param>
  private void RecalculateAllPerceivedPathCosts(int? ticksAbs = null)
  {
    allPathCostsRecalculatedDayOfYear = DayOfYearAt0Long;

    using GridInitializerState gis = new(this);
    foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
    {
      for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
      {
        RecalculatePerceivedMovementDifficultyAt(i, vehicleDef, ticksAbs);
      }
      OnPathGridRecalculated?.Invoke(vehicleDef);
    }

    // Only needs to be done once and not for every grid owner
    for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
    {
      RecalculateWinterPercentAt(i, ticksAbs);
    }
  }

  private void RecalculateWinterPercentAt(PlanetTile tile, int? ticksAbs = null)
  {
    winter[tile] = WinterPathingHelper.GetWinterPercent(tile, ticksAbs: ticksAbs);
  }

  /// <summary>
  /// Calculate path cost for <paramref name="vehicleDef"/> at <paramref name="tile"/>
  /// </summary>
  public static float CalculatedMovementDifficultyAt(PlanetTile tile, VehicleDef vehicleDef,
    int? ticksAbs = null, StringBuilder explanation = null, bool coastalTravel = true)
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
        if (vehicleDef.properties.defaultImpassable.HasFlag(DefaultImpassable.Rivers))
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
    if (vehicleDef.properties.defaultImpassable.HasFlag(DefaultImpassable.Biomes))
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
      if (vehicleDef.properties.defaultImpassable.HasFlag(DefaultImpassable.Roads))
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
      if (vehicleDef.properties.defaultImpassable.HasFlag(DefaultImpassable.Roads) &&
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
    return Mathf.Max(CalculatedMovementDifficultyAt(tile, vehicleDef, null, null, false),
      CalculatedMovementDifficultyAt(neighbor, vehicleDef, null, null, false));
  }

  [DebugAction(VehicleHarmony.VehiclesLabel, name = "Regen WorldGrid",
    allowedGameStates = AllowedGameStates.PlayingOnWorld)]
  private static void RecalculatePathGrid()
  {
    Instance.RunTaskRecalculateAllPathCosts();
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
}