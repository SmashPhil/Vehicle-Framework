using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreLib.PathFinding;
using JetBrains.Annotations;
using SmashTools;
using SmashTools.Burst;
using SmashTools.Performance;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;
using Verse;
using BurstPathFinder = SmashTools.Burst.PathFinder;
using PathRequest = SmashTools.Burst.PathRequest;

namespace Vehicles;

[PublicAPI]
public sealed class PathFinderManager : IDisposable, IPathFinder<PathSettings>, IPathGridSync
{
  private readonly Map map;
  private readonly int width;
  private readonly IPathingManager pathing;

  // - Additional considerations:
  //    - Blueprint cost
  //    - Avoid grid
  private IPathCostGrid[] costGrids;
  private DirtyGrid[] dirtyGrids;

  private PathFinderImpl[] pathFinders;

  private int currentSource;

  private bool disposed;

  public PathFinderManager(IPathingManager pathing)
  {
    this.pathing = pathing;
    map = pathing.Map;

    width = map.Size.x;
    ModifierGrid = new ModifierGrid(map.Size.x * map.Size.z, this);
    HeuristicGrid = new RoadHeuristic(map, this);
    ScalarGrid = new PathGridScalar(new int2(map.Size.x, map.Size.z));
    pathFinders = new PathFinderImpl[DefDatabase<VehicleDef>.DefCount];
  }

  internal ModifierGrid ModifierGrid { get; private set; }

  internal RoadHeuristic HeuristicGrid { get; private set; }

  internal PathGridScalar ScalarGrid { get; private set; }

  // TODO 1.7 - FIX
  // There is a circular reference between this, VehiclePathGrid, and VehicleReachability, so the order of
  // initialization is very touchy. Will need to refactor how path grid handles initial map generation.
  internal void Init()
  {
    CreateSources();
    CreatePathFinders();
    ForceUpdateAll();
  }

  public T GetSource<T>() where T : IPathCostGrid
  {
    foreach (IPathCostGrid costGrid in costGrids)
    {
      if (costGrid is T source)
        return source;
    }
    return default;
  }

  private void CreatePathFinders()
  {
    foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
    {
      Assert.IsNull(pathFinders[vehicleDef.DefIndex]);
      pathFinders[vehicleDef.DefIndex] = new PathFinderImpl(this, vehicleDef);
    }
  }

  internal void Tick()
  {
    UpdateSingle();
  }

  private void CreateSources()
  {
    List<Type> types = typeof(IPathCostGrid).AllAssignableOfNonAbstract();
    if (types.NullOrEmpty())
      return;

    costGrids = new IPathCostGrid[types.Count];
    dirtyGrids = new DirtyGrid[types.Count];
    int index = 0;
    foreach (Type type in types)
    {
      IPathCostGrid source = (IPathCostGrid)Activator.CreateInstance(type, map, this);
      costGrids[index] = source;
      dirtyGrids[index] = new DirtyGrid(map);
      source.Index = index++;
    }
  }

  public void SetDirty(IPathCostGrid source, IntVec3 cell)
  {
    dirtyGrids[source.Index].SetDirty(cell);
  }

  public void ForceUpdateAll()
  {
    Parallel.ForEach(costGrids, delegate(IPathCostGrid source)
    {
      foreach (IntVec3 cell in map.AllCells)
      {
        int index = CellIndicesUtility.CellToIndex(cell, width);
        source.Update(index);
      }
      dirtyGrids[source.Index].dirtyCells.Clear();
    });
  }

  [Profile]
  internal void UpdateSingle()
  {
    if (costGrids == null)
      return;

    int srcIndex = currentSource;
    Queue<IntVec3> dirtyCells = dirtyGrids[srcIndex].dirtyCells;
    if (++currentSource >= costGrids.Length)
    {
      currentSource = 0;
    }

    if (dirtyCells.Count == 0)
      return;

    IntVec3 cell = dirtyCells.Dequeue();
    int index = CellIndicesUtility.CellToIndex(cell, width);
    costGrids[srcIndex].Update(index);
  }

  [Profile]
  internal void UpdateAll()
  {
    if (costGrids == null)
      return;

    foreach (IPathCostGrid costGrid in costGrids)
    {
      Queue<IntVec3> dirtyCells = dirtyGrids[costGrid.Index].dirtyCells;
      if (dirtyCells.Count == 0)
        continue;

      foreach (IntVec3 cell in dirtyCells)
      {
        int index = CellIndicesUtility.CellToIndex(cell, width);
        costGrid.Update(index);
      }
      dirtyCells.Clear();
    }
  }

  void IPathGridSync.NotifyWritingToGrid()
  {
    foreach (PathFinderImpl pathFinder in pathFinders)
    {
      pathFinder.NotifyWritingToGrid();
    }
  }

  /// <summary>
  /// Find a path from start to end position
  /// </summary>
  /// <param name="start">The starting position of the path</param>
  /// <param name="end">The destination of the path.</param>
  /// <param name="settings">PathFinder settings for this request.</param>
  /// <returns>
  /// A completed Path object containing the list of nodes from <paramref name="start"/> to <paramref name="end"/>
  /// </returns>
  public Path FindPath(Path.Node start, Path.Node end, PathSettings settings)
  {
    if (settings.vehicleDef == null)
      throw new ArgumentNullException(nameof(settings.vehicleDef));

    UpdateAll();
    Path path = pathFinders[settings.vehicleDef.DefIndex].FindPath(MakeRequest(start, end, settings));
    return path;
  }

  /// <summary>
  /// Find a path from start to end position
  /// </summary>
  /// <param name="start">The starting position of the path</param>
  /// <param name="end">The destination of the path.</param>
  /// <param name="settings">PathFinder settings for this request.</param>
  /// <returns>
  /// A promise object representing the ongoing pathfinding operation. Call <see cref="IPathPromise.GetPath"/>
  /// to complete the operation and retrieve the path.
  /// </returns>
  public IPathPromise RequestPath(Path.Node start, Path.Node end, PathSettings settings)
  {
    UpdateAll();
    PathReceipt receipt = pathFinders[settings.vehicleDef.DefIndex].RequestPath(MakeRequest(start, end, settings));
    return receipt;
  }

  private PathRequest MakeRequest(Path.Node start, Path.Node end, in PathSettings settings)
  {
    PathRequest request = new()
    {
      start = start.ToInt3(),
      end = end.ToInt3(),
      rotation = settings.rotation.IsValid ? settings.rotation.AsInt : 0,
      turnData = settings.turnData
    };
    if (ScalarGrid != null && settings.scalar > 0)
    {
      request.scalar = ScalarGrid.ToData(settings.scalar);
    }

    int thingId = settings.vehicle is { Spawned: true } vehicle ? vehicle.thingIDNumber : -1;
    VehiclePositionManager positionMgr = map.GetDetachedMapComponent<VehiclePositionManager>();
    request.entityCost = new EntityConfig(thingId, positionMgr.ThingIdGrid);

    GatherOffsets(ref request, settings);

    return request;
  }

  private void GatherOffsets(ref PathRequest request, in PathSettings settings)
  {
    if (costGrids == null)
      return;

    if (HeuristicGrid != null && settings.heuristic.IsValid)
    {
      request.heuristic = new HeuristicGrid.Data(HeuristicGrid, settings.heuristic);
    }
    if (ScalarGrid != null && settings.scalar > 0)
    {
      request.scalar = ScalarGrid.ToData(settings.scalar);
    }

    foreach (IPathCostGrid costGrid in costGrids)
    {
      if (!costGrid.ShouldApplyFor(settings))
        continue;

      if (costGrid is VehicleCostOffsets offsetGrid)
      {
        request.costOffsets ??= [];
        request.costOffsets.Add(offsetGrid);
      }
      else if (costGrid is VehicleBitOffsets bitGrid)
      {
        request.bitOffsets ??= [];
        request.bitOffsets.Add(bitGrid.OffsetFor(settings));
      }
      else if (costGrid is BuildingGrid building &&
               (settings.search & PathSettings.GridSetting.BreachWalls) != 0)
      {
        request.mask = building;
      }
      else
      {
        Log.Error($"{costGrid.GetType()} not handled by PathFinderManager");
      }
    }
  }

  public void Dispose()
  {
    if (disposed)
      return;

    disposed = true;
    ModifierGrid.Dispose();
    if (costGrids != null)
    {
      foreach (IPathCostGrid costGrid in costGrids)
      {
        costGrid.Dispose();
      }
      foreach (DirtyGrid dirtyGrid in dirtyGrids)
      {
        dirtyGrid.Dispose();
      }
    }
    foreach (PathFinderImpl pathFinder in pathFinders)
    {
      pathFinder.Dispose();
    }

    ModifierGrid = null;
    costGrids = null;
    pathFinders = null;
  }

  private class DirtyGrid(Map map) : IDisposable
  {
    private NativeBitArray dirtyGrid = new(map.Size.x * map.Size.z, Allocator.Persistent);
    private readonly int width = map.Size.x;

    public readonly Queue<IntVec3> dirtyCells = [];

    public void SetDirty(IntVec3 cell)
    {
      int index = CellIndicesUtility.CellToIndex(cell, width);
      if (!dirtyGrid.IsSet(index))
      {
        dirtyCells.Enqueue(cell);
      }
    }

    public void Dispose()
    {
      dirtyGrid.Dispose();
    }
  }

  private class PathFinderImpl : IDisposable
  {
    private readonly VehicleDef vehicleDef;
    private readonly IPathingManager pathing;

    private readonly BurstPathFinder pathFinder;

    public PathFinderImpl(PathFinderManager manager, VehicleDef vehicleDef)
    {
      pathing = manager.pathing;
      this.vehicleDef = vehicleDef;

      Map map = manager.map;
      pathFinder = new BurstPathFinder(new BurstPathFinder.Settings
      {
        mapSize = new int2(map.Size.x, map.Size.z),
        hitbox = new int2(vehicleDef.size.x, vehicleDef.size.z),
        pathGrid = pathing.GetPathGrid(vehicleDef).CostGrid,
        poolObjects = true
      });
      pathing.GetPathGrid(vehicleDef).OnWritingToGrid += NotifyWritingToGrid;
    }

    internal void NotifyWritingToGrid()
    {
      pathFinder.NotifyWritingToGrid();
    }

    public Path FindPath(in PathRequest request)
    {
      return pathFinder.FindPath(request);
    }

    public PathReceipt RequestPath(in PathRequest request)
    {
      return pathFinder.RequestPath(request);
    }

    public void Dispose()
    {
      pathFinder.Dispose();
      pathing.GetPathGrid(vehicleDef).OnWritingToGrid -= NotifyWritingToGrid;
    }
  }
}
