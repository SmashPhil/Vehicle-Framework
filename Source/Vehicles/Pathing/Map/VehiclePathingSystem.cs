using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreLib.PathFinding;
using CoreLib.Performance;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine.Assertions;
using Verse;
using static Vehicles.Config.FeatureFlags;

namespace Vehicles;

/// <summary>
/// MapComponent container for all pathing related subcomponents for vehicles
/// </summary>
[StaticConstructorOnStartup] // TODO 1.7 - Verify if SCOS is still needed
public sealed class VehiclePathingSystem : MapComponent, IDisposable, IPathingManager
{
  private const GridSelection DefaultGrids = GridSelection.All;
  private const GridDeferment DefaultDeferment = GridDeferment.Lazy;

  private int ownerCleanIndex;

  // TODO 1.7 - set access modifier to private
  public DedicatedThread dedicatedThread;
  public DeferredGridGeneration deferredGridGeneration;

  private int defGridCalculatedDayOfYear;

  // TODO 1.6.2144 - VehicleMapFramework has a direct patch on this.
#pragma warning disable CS0618
  [UsedImplicitly]
  private VehiclePathData[] vehicleData;
#pragma warning restore CS0618

  public VehiclePathingSystem(Map map) : base(map)
  {
    deferredGridGeneration = new DeferredGridGeneration(this);
    GridOwners = new MapGridOwners(this, DefDatabase<VehicleDef>.AllDefsListForReading);
    GridOwners.OnOwnershipTransfer += SwapRegionManagerOwners;
    ConstructComponents();
  }

  public Map Map => map;

  public MapGridOwners GridOwners { get; }

  public PathDataContainer PathData { get; private set; }

  internal GridDebouncer PathGridDebouncer { get; private set; }

  public IPathFinder<PathSettings> PathFinder => IsFeatureEnabled(PathFinderV2) ? PathFinderManager : VehiclePathFinder;

  internal PathFinderManager PathFinderManager { get; private set; }

  internal VehiclePathFinder VehiclePathFinder { get; private set; }

  [Obsolete]
  public bool DebouncingPathGridDirtying => PathGridDebouncer != null;

  /// <summary>
  /// <see cref="dedicatedThread"/> is initialized and running.
  /// </summary>
  public bool ThreadAlive => dedicatedThread is
  {
    State: not DedicatedThread.ThreadState.Uninitialized
    and not DedicatedThread.ThreadState.Terminated
  };

  /// <summary>
  /// <see cref="dedicatedThread"/> is alive, not suspended, and not in a long operation.
  /// </summary>
  /// <remarks>Verify this is true before queueing up a method, otherwise you may just be sending it to the void 
  /// where it will never be executed.</remarks>
  public bool ThreadAvailable => ThreadAlive && !dedicatedThread.IsSuspended;

  /// <summary>
  /// Generates all path data if they haven't been already and fetches
  /// <see cref="PathData"/> for <paramref name="vehicleDef"/>.
  /// </summary>
  public PathData this[VehicleDef vehicleDef]
  {
    get
    {
      if (vehicleDef == null)
        throw new ArgumentNullException(nameof(vehicleDef));

      return PathData[vehicleDef.DefIndex];
    }
  }

  bool IPathingManager.IsPathDataSuspended(VehicleDef vehicleDef)
  {
    return this[vehicleDef].Suspended;
  }

  VehiclePathGrid IPathingManager.GetPathGrid(VehicleDef vehicleDef)
  {
    return this[vehicleDef].VehiclePathGrid;
  }

  VehicleRegionGridManager IPathingManager.GetRegionGridManager(VehicleDef vehicleDef)
  {
    return this[vehicleDef].VehicleRegionGridManager;
  }

  internal void InitThread()
  {
    if (dedicatedThread != null)
    {
      Log.Warning(
        "Reinitializing dedicatedThread. It should only be done once on map generation.");
      ReleaseThread();
    }

    if (!VehicleMod.settings.debug.debugUseMultithreading)
    {
      Log.Warning(
        $"Loading map without DedicatedThread. This will cause performance issues. Map={map}.");
      return;
    }

    if (map.info?.parent == null)
    {
      // MapParent won't have reference resolved when loading from save, GetDedicatedThread will
      // be called a 2nd time on PostLoadInit
      return;
    }

    dedicatedThread = GetDedicatedThread(map);
  }

  private static DedicatedThread GetDedicatedThread(Map map)
  {
    DedicatedThread thread;
    if (map.IsPlayerHome)
    {
      thread = ThreadManager.CreateNew();
      Debug.Message($"Creating thread (id={thread?.id})");
      return thread;
    }

    const int EventMapThreadId = 25;
    thread = ThreadManager.GetOrCreateShared(EventMapThreadId);
    Debug.Message(
      $"Fetching thread with shared ownership (id={thread?.id})");
    return thread;
  }

  /// <summary>
  /// Finalize initialization for map component
  /// </summary>
  public override void FinalizeInit()
  {
    base.FinalizeInit();
    if (!ThreadAlive)
    {
      InitThread();
    }
    RegenerateGrids();
    PathFinderManager?.ForceUpdateAll();
  }

  /// <summary>
  /// Regenerate all region and path grids.
  /// </summary>
  public void RegenerateGrids(GridSelection grids = DefaultGrids,
    GridDeferment deferment = DefaultDeferment)
  {
    using LongEventText text = new();

    // Unit tests need all grids generated before execution. Dedicated thread would also be
    // getting suspended sporadically during unit testing so using deferred grid generation would
    // lead to inconsistent results.
#if DEV_TOOLS
    if (TestWatcher.RunningTests)
      deferment = GridDeferment.Forced;
#endif

    switch (deferment)
    {
      case GridDeferment.Lazy:
        break;
      case GridDeferment.Deferred:
        if ((grids & GridSelection.PathGrids) != 0)
        {
          deferredGridGeneration.GenerateAllPathGrids();
        }

        if ((grids & GridSelection.Regions) != 0)
        {
          deferredGridGeneration.GenerateAllRegionGrids();
        }

        break;
      case GridDeferment.Forced:
        GeneratePathGrids();
        GenerateRegionsParallel();
        break;
      default:
        throw new NotImplementedException();
    }
  }

  private void GeneratePathGrids()
  {
    var allPathData = PathData.AllPathData;
    int i = 0;
    foreach (PathData pathData in allPathData)
    {
      LongEventHandler.SetCurrentEventText(
        $"{"VF_GeneratingPathGrids".Translate()} {i++}/{allPathData.Length}");
      pathData.VehiclePathGrid.RecalculateAllPerceivedPathCosts();
    }
  }

  private void GenerateRegions()
  {
    int total = GridOwners.AllOwners.Length;
    for (int i = 0; i < total; i++)
    {
      VehicleDef vehicleDef = GridOwners.AllOwners[i];
      LongEventHandler.SetCurrentEventText($"{"VF_GeneratingRegions".Translate()} {i}/{total}");

      PathData vehiclePathData = this[vehicleDef];
      vehiclePathData.VehicleRegionAndRoomUpdater.Init();
      vehiclePathData.VehicleRegionAndRoomUpdater.RebuildAllVehicleRegions();
    }
  }

  private void GenerateRegionsParallel()
  {
    if (!GridOwners.AnyOwners) return;

    if (GridOwners.AllOwners.Length <= 3)
    {
      // Generating regions is a lot faster now, so anything below 2~3
      // can just be done synchronously. Will take < 1s regardless.
      GenerateRegions();
      return;
    }

    DeepProfiler.Start("Vehicle Regions");
    Parallel.ForEach(GridOwners.AllOwners, delegate (VehicleDef vehicleDef)
    {
      LongEventHandler.SetCurrentEventText("VF_GeneratingRegions".Translate());
      PathData vehiclePathData = this[vehicleDef];
      vehiclePathData.VehicleRegionAndRoomUpdater.Init();
      vehiclePathData.VehicleRegionAndRoomUpdater.RebuildAllVehicleRegions();
    });
    DeepProfiler.End();
  }

  /// <summary>
  /// Construct and cache <see cref="PathData"/> for each moveable <see cref="VehicleDef"/> 
  /// </summary>
  public void ConstructComponents()
  {
    if (IsFeatureEnabled(PathFinderV2))
    {
      PathFinderManager = new PathFinderManager(this);
    }
    else
    {
      VehiclePathFinder = new VehiclePathFinder(this);
    }

    PathData = new PathDataContainer(this, DefDatabase<VehicleDef>.AllDefsListForReading);
    PathData.GenerateAllPathData(new PathGridCalculator(), PathFinder);

#pragma warning disable CS0618 // Type or member is obsolete
    vehicleData = new VehiclePathData[PathData.AllPathData.Length];
#pragma warning restore CS0618 // Type or member is obsolete

    DisableAllRegionUpdaters();

    PathFinderManager?.Init();
  }

  public void DisableAllRegionUpdaters()
  {
    foreach (VehicleDef vehicleDef in GridOwners.AllOwners)
    {
      PathData pathData = this[vehicleDef];
      pathData.VehicleRegionAndRoomUpdater.Disable();
    }
  }

  public override void ExposeData()
  {
    base.ExposeData();
    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
      if (dedicatedThread == null)
      {
        InitThread();
      }
    }
  }

  public void RequestGridsFor(VehiclePawn vehicle)
  {
    // Try to generate regions immediately for a vehicle being spawned and cut in line
    // in front of any deferred region requests that may have just been queued.
    RequestGridsFor(vehicle.VehicleDef, DeferredGridGeneration.UrgencyFor(map, vehicle));
  }

  public void RequestGridsFor(VehicleDef vehicleDef, DeferredGridGeneration.Urgency urgency)
  {
    deferredGridGeneration.RequestGridsFor(vehicleDef, urgency);
  }

  internal void BeginCapturingPathGridDirtying()
  {
    List<IGridDebouncerSource> sources = [];
    foreach (PathData pathData in PathData.AllPathData)
    {
      if (pathData.VehiclePathGrid is { Enabled: true })
      {
        sources.Add(pathData.VehiclePathGrid);
      }
    }
    PathGridDebouncer = new GridDebouncer(map, sources);
  }

  internal void EndCapturingPathGridDirtying()
  {
    // NOTE: RimWorld's MapGenerator has an unnecessary check after DisableDirtyingScope goes out of
    // scope which 're-enables' incremental dirtying if it wasn't already. This will call into here a
    // 2nd time even though this should logically be impossible.
    if (PathGridDebouncer == null)
      return;

    try
    {
      PathGridDebouncer.ExecuteAll();
    }
    finally
    {
      PathGridDebouncer.Dispose();
      PathGridDebouncer = null;
    }
  }

  public override void MapRemoved()
  {
    ReleaseThread();
  }

  internal void ReleaseThread()
  {
    if (dedicatedThread == null || dedicatedThread.IsTerminated)
      return;

    Debug.Message($"Releasing thread (id={dedicatedThread.id})");
    ThreadManager.ReleaseAndJoin(dedicatedThread);
    dedicatedThread = null;
  }

  public override void MapComponentTick()
  {
    base.MapComponentTick();
    PathFinderManager?.Tick();
    if (ThreadAlive)
    {
      int dayOfYear = GenDate.DayOfYear(GenTicks.TicksAbs, 0f);
      if (defGridCalculatedDayOfYear != dayOfYear)
      {
        deferredGridGeneration.DoIncrementalPass();
        defGridCalculatedDayOfYear = dayOfYear;
      }
    }
  }

  public override void MapComponentDraw()
  {
    FlashGridType flashGridType = SectionDebug.debugDrawFlashGrid;
    if (flashGridType == FlashGridType.None || Find.TickManager.Paused)
      return;
    if (Find.CurrentMap is null || WorldRendererUtility.WorldRendered)
      return;

    switch (flashGridType)
    {
      case FlashGridType.CoverGrid:
        map.FlashCoverGrid();
        break;
      case FlashGridType.GasGrid:
        map.FlashGasGrid();
        break;
      case FlashGridType.PositionManager:
        map.FlashClaimants();
        break;
      case FlashGridType.ThingGrid:
        map.FlashThingGrid();
        break;
      case FlashGridType.ListerThings:
        map.FlashListerThings();
        break;
      case FlashGridType.ModifierGrid:
        map.FlashModifierGrid();
        break;
      default:
        Log.ErrorOnce($"Not Implemented: {flashGridType}", flashGridType.GetHashCode());
        break;
    }
  }

  public override void MapComponentUpdate()
  {
    UpdateRegions();
  }

  private void UpdateRegions()
  {
    if (!GridOwners.AnyOwners)
      return;

    if (ownerCleanIndex < GridOwners.AllOwners.Length)
    {
      VehicleDef vehicleDef = GridOwners.AllOwners[ownerCleanIndex];
      PathData pathData = this[vehicleDef];
      if (!pathData.Suspended && pathData.VehicleRegionDirtyer.AnyDirty)
      {
        if (ThreadAvailable)
        {
          AsyncRebuildRegionsAction action = AsyncPool<AsyncRebuildRegionsAction>.Get();
          action.Set(pathData);
          dedicatedThread.Enqueue(action);
        }
        else
        {
          // TODO - Verify if this can be removed
          // NOTE - This is not executed on the dedicated thread, I don't think this is necessary
          // anymore, but it needs further testing + a unit test to ensure no invalid regions are left
          // behind in the region grid.
          foreach (RegionGridType gridType in VehicleRegionGridManager.AllGridTypes)
          {
            pathData.VehicleRegionGridManager[gridType].UpdateClean();
          }
          pathData.VehicleRegionAndRoomUpdater.TryRebuildVehicleRegions();
        }
      }

      ownerCleanIndex++;
      if (ownerCleanIndex >= GridOwners.AllOwners.Length) ownerCleanIndex = 0;
    }
  }

  private void SwapRegionManagerOwners(VehicleDef fromVehicleDef, VehicleDef toVehicleDef)
  {
    PathData pathData = this[fromVehicleDef];
    // Should be same instance for ownership to be transferable.
    Assert.IsTrue(pathData.RegionData == this[toVehicleDef].RegionData);
    pathData.RegionData.ChangeOwner(toVehicleDef);
  }

  public void Dispose()
  {
    PathFinderManager?.Dispose();
  }

  // TODO 1.6.2144 - Referenced by VehicleMapFramework
  [Obsolete("This reference has been moved to PathData.", error: true)]
  private void GeneratePathData()
  {
  }

  [Flags]
  public enum GridSelection
  {
    None = 0,
    Regions = 1 << 0,
    PathGrids = 1 << 1,

    All = Regions | PathGrids,
  }

  /// <summary>
  /// How grid generation should defer generation.
  /// </summary>
  /// <remarks>
  /// Lazy: Skip grid generation and wait for spawn events to generate grids as needed.<br/>
  /// Deferred: Send grid generation to the dedicated thread.
  /// Forced: Generate grid immediately.
  /// </remarks>
  public enum GridDeferment
  {
    Lazy,
    Deferred,
    Forced,
  }

  [PublicAPI, Obsolete("Use Vehicles.PathData instead.")]
  public class VehiclePathData : PathData
  {
    internal VehiclePathData(IPathingManager manager, VehicleDef vehicleDef)
      : base(manager, vehicleDef)
    {
    }
  }

  [Obsolete("No longer used, this was split out into RegionData, but access to region classes should go through VehiclePathingSystem.")]
  public class VehicleReachabilitySettings
  {
    internal readonly VehicleRegionMaker regionMaker;
    internal readonly VehicleRegionGridManager regionGridManager;
    internal readonly VehicleRegionAndRoomUpdater regionAndRoomUpdater;
    internal readonly VehicleRegionDirtyer regionDirtyer;
    internal readonly VehicleReachability reachability;

    private readonly VehicleGridManager[] gridManagers;

    public VehicleReachabilitySettings(VehiclePathingSystem pathingSystem, VehicleDef vehicleDef)
    {
      regionMaker = new VehicleRegionMaker(pathingSystem, vehicleDef);
      regionDirtyer = new VehicleRegionDirtyer(pathingSystem, vehicleDef, regionMaker);
      regionAndRoomUpdater = new VehicleRegionAndRoomUpdater(pathingSystem, vehicleDef, regionDirtyer);
      regionGridManager = new VehicleRegionGridManager(pathingSystem, vehicleDef, regionMaker, regionDirtyer);
      reachability = new VehicleReachability(pathingSystem, vehicleDef, pathingSystem.PathFinder);

      gridManagers = [regionMaker, regionDirtyer, regionAndRoomUpdater, regionGridManager, reachability];
    }

    internal VehicleReachabilitySettings(RegionData data)
    {
      regionMaker = data.regionMaker;
      regionDirtyer = data.regionDirtyer;
      regionAndRoomUpdater = data.regionAndRoomUpdater;
      regionGridManager = data.regionGridManager;
      reachability = data.reachability;
      gridManagers = [regionMaker, regionDirtyer, regionAndRoomUpdater, regionGridManager, reachability];
    }

    public void PostInit()
    {
      foreach (var gridManager in gridManagers)
      {
        gridManager.PostInit();
      }
    }

    public void ChangeOwner(VehicleDef vehicleDef)
    {
      foreach (var gridManager in gridManagers)
      {
        gridManager.ChangeOwner(vehicleDef);
      }
    }
  }
}