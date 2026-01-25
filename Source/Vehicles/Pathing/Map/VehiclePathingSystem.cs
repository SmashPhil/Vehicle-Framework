using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreLib.Performance;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using static Vehicles.Config.FeatureFlags;
using PathFinder = SmashTools.Burst.PathFinder;

namespace Vehicles;

/// <summary>
/// MapComponent container for all pathing related subcomponents for vehicles
/// </summary>
[StaticConstructorOnStartup]
public sealed class VehiclePathingSystem : MapComponent, IDisposable, IPathGridNotifier
{
  private const int EventMapThreadId = 25;

  private const GridSelection DefaultGrids = GridSelection.All;
  private const GridDeferment DefaultDeferment = GridDeferment.Lazy;

  private VehiclePathData[] vehicleData;

  private VehicleDef buildingFor;
  private int ownerCleanIndex;

  public DedicatedThread dedicatedThread;
  public DeferredGridGeneration deferredGridGeneration;

  private int defGridCalculatedDayOfYear;

  public VehiclePathingSystem(Map map) : base(map)
  {
    deferredGridGeneration = new DeferredGridGeneration(this);
    GridOwners = new MapGridOwners(this);
    GridOwners.OnOwnershipTransfer += SwapRegionManagerOwners;
    GridOwners.Init();
    ConstructComponents();
  }

  public MapGridOwners GridOwners { get; }

  internal GridDebouncer PathGridDebouncer { get; private set; }

  internal ModifierGrid ModifierGrid { get; private set; }

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
  /// <see cref="VehiclePathData"/> for <paramref name="vehicleDef"/>.
  /// </summary>
  public VehiclePathData this[VehicleDef vehicleDef]
  {
    get
    {
      if (vehicleDef == null)
        throw new ArgumentNullException(nameof(vehicleDef));

#if DEBUG
      if (buildingFor == vehicleDef)
      {
        Trace.Fail(
          "Trying to pull VehiclePathData by indexing when it's currently in the middle of " +
          "generation. Recursion is not supported here.");
        return null;
      }
#endif
      return vehicleData[vehicleDef.DefIndex];
    }
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
      InitThread();
    RegenerateGrids();
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
    for (int i = 0; i < vehicleData.Length; i++)
    {
      VehiclePathData vehiclePathData = vehicleData[i];
      LongEventHandler.SetCurrentEventText(
        $"{"VF_GeneratingPathGrids".Translate()} {i}/{vehicleData.Length}");
      //using VehicleRegionConnector.Disabler disabler = new(vehiclePathData.VehicleRegionConnector);
      vehiclePathData.VehiclePathGrid.RecalculateAllPerceivedPathCosts();
    }
  }

  private void GenerateRegions()
  {
    int total = GridOwners.AllOwners.Length;
    for (int i = 0; i < total; i++)
    {
      VehicleDef vehicleDef = GridOwners.AllOwners[i];
      LongEventHandler.SetCurrentEventText($"{"VF_GeneratingRegions".Translate()} {i}/{total}");

      VehiclePathData vehiclePathData = this[vehicleDef];
      vehiclePathData.VehicleRegionAndRoomUpdater.Init();
      vehiclePathData.VehicleRegionAndRoomUpdater.RebuildAllVehicleRegions();
    }
  }

  private void GenerateGridConnections()
  {
    int total = GridOwners.AllOwners.Length;
    for (int i = 0; i < total; i++)
    {
      VehicleDef vehicleDef = GridOwners.AllOwners[i];
      LongEventHandler.SetCurrentEventText($"{"VF_GeneratingRegions".Translate()} {i}/{total}");

      VehiclePathData vehiclePathData = this[vehicleDef];
      vehiclePathData.VehicleRegionConnector.RebuildAllConnections();
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
      VehiclePathData vehiclePathData = this[vehicleDef];
      vehiclePathData.VehicleRegionAndRoomUpdater.Init();
      vehiclePathData.VehicleRegionAndRoomUpdater.RebuildAllVehicleRegions();
    });
    DeepProfiler.End();
  }

  /// <summary>
  /// Construct and cache <see cref="VehiclePathData"/> for each moveable <see cref="VehicleDef"/> 
  /// </summary>
  public void ConstructComponents()
  {
    int size = DefDatabase<VehicleDef>.DefCount;
    vehicleData = new VehiclePathData[size];

    if (IsFeatureEnabled(PathFinderV2))
    {
      ModifierGrid = new ModifierGrid(map.Size.x * map.Size.z, this);
    }

    GenerateAllPathData();
    DisableAllRegionUpdaters();
  }

  public void DisableAllRegionUpdaters()
  {
    foreach (VehicleDef vehicleDef in GridOwners.AllOwners)
    {
      VehiclePathData pathData = this[vehicleDef];
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
    foreach (VehiclePathData pathData in vehicleData)
    {
      if (pathData.VehiclePathGrid is { Enabled: true })
      {
        sources.Add(pathData.VehiclePathGrid);
      }
    }
    if (sources.Count > 0)
    {
      PathGridDebouncer = new GridDebouncer(map, sources);
    }
  }

  internal void EndCapturingPathGridDirtying()
  {
    if (!DebouncingPathGridDirtying)
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
      VehiclePathData pathData = this[vehicleDef];
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

  private void GenerateAllPathData()
  {
    // All vehicles need path data (even aerial vehicles for landing)
    foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
    {
      GeneratePathData(vehicleDef);
    }
  }

  /// <summary>
  /// Generate new <see cref="VehiclePathData"/> for <paramref name="vehicleDef"/>
  /// </summary>
  /// <param name="vehicleDef"></param>
  private void GeneratePathData(VehicleDef vehicleDef)
  {
    VehiclePathData vehiclePathData = new();
    vehicleData[vehicleDef.DefIndex] = vehiclePathData;
    bool isOwner = GridOwners.IsOwner(vehicleDef);

    buildingFor = vehicleDef;
    {
      vehiclePathData.VehiclePathGrid = new VehiclePathGrid(this, vehicleDef);
      vehiclePathData.VehicleRegionConnector = new VehicleRegionConnector(this, vehicleDef);
      vehiclePathData.VehiclePathFinder = new VehiclePathFinder(this, vehicleDef);

      if (IsFeatureEnabled(PathFinderV2))
      {
        vehiclePathData.PathFinder = new PathFinder(new PathFinder.Settings
        {
          mapSize = new int2(map.Size.x, map.Size.z),
          hitbox = new int2(vehicleDef.size.x, vehicleDef.size.z),
          pathGrid = vehiclePathData.VehiclePathGrid.CostGrid,
          modifierGrid = ModifierGrid,
          poolObjects = true
        });
      }

      if (isOwner)
      {
        vehiclePathData.ReachabilityData =
          new VehicleReachabilitySettings(this, vehicleDef);
      }
      else
      {
        // Will return itself if it's an owner
        VehicleDef ownerDef = GridOwners.GetOwner(vehicleDef);
        vehiclePathData.ReachabilityData = vehicleData[ownerDef.DefIndex].ReachabilityData;
      }
    }
    buildingFor = null;

    vehiclePathData.VehiclePathGrid.PostInit();
    vehiclePathData.VehicleRegionConnector.PostInit();
    vehiclePathData.VehiclePathFinder.PostInit();
    if (isOwner)
    {
      vehiclePathData.ReachabilityData.PostInit();
    }
  }

  private void SwapRegionManagerOwners(VehicleDef fromVehicleDef, VehicleDef toVehicleDef)
  {
    VehiclePathData pathData = this[fromVehicleDef];
    // Should be same instance for ownership to be transferable.
    Assert.IsTrue(pathData.ReachabilityData == this[toVehicleDef].ReachabilityData);
    pathData.ReachabilityData.ChangeOwner(toVehicleDef);
  }

  void IPathGridNotifier.NotifyWritingToGrid()
  {
    foreach (VehiclePathData pathData in vehicleData)
    {
      if (!pathData.Suspended)
      {
        pathData.PathFinder.NotifyWritingToGrid();
      }
    }
  }

  public void Dispose()
  {
    ModifierGrid?.Dispose();
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

  /// <summary>
  /// Container for all path related subcomponents specific to a <see cref="VehicleDef"/>.
  /// </summary>
  /// <remarks>Stores data strictly for deviations from vanilla regarding impassable values</remarks>
  public class VehiclePathData
  {
    // Region grid is currently disabled.
    public bool Suspended => !VehicleRegionAndRoomUpdater.Enabled;

    public VehicleReachabilitySettings ReachabilityData { get; set; }

    public VehiclePathGrid VehiclePathGrid { get; set; }

    public VehicleRegionConnector VehicleRegionConnector { get; set; }

    public VehiclePathFinder VehiclePathFinder { get; set; }

    public PathFinder PathFinder { get; set; }

    public VehicleReachability VehicleReachability => ReachabilityData.reachability;

    // TODO 1.6.2144
    [Obsolete("Fetch from region grid manager.")]
    public VehicleRegionGrid VehicleRegionGrid => VehicleRegionGridManager[RegionGridType.Normal];

    public VehicleRegionGridManager VehicleRegionGridManager => ReachabilityData.regionGridManager;

    public VehicleRegionMaker VehicleRegionMaker => ReachabilityData.regionMaker;

    public VehicleRegionAndRoomUpdater VehicleRegionAndRoomUpdater => ReachabilityData.regionAndRoomUpdater;

    public VehicleRegionDirtyer VehicleRegionDirtyer => ReachabilityData.regionDirtyer;
  }

  public class VehicleReachabilitySettings
  {
    internal readonly VehicleRegionGridManager regionGridManager;
    internal readonly VehicleRegionMaker regionMaker;
    internal readonly VehicleRegionAndRoomUpdater regionAndRoomUpdater;
    internal readonly VehicleRegionDirtyer regionDirtyer;
    internal readonly VehicleReachability reachability;

    public VehicleReachabilitySettings(VehiclePathingSystem pathingSystem, VehicleDef vehicleDef)
    {
      regionGridManager = new VehicleRegionGridManager(pathingSystem, vehicleDef);
      regionMaker = new VehicleRegionMaker(pathingSystem, vehicleDef);
      regionAndRoomUpdater = new VehicleRegionAndRoomUpdater(pathingSystem, vehicleDef);
      regionDirtyer = new VehicleRegionDirtyer(pathingSystem, vehicleDef);
      reachability = new VehicleReachability(pathingSystem, vehicleDef);
    }

    public void PostInit()
    {
      regionGridManager.PostInit();
      regionMaker.PostInit();
      regionAndRoomUpdater.PostInit();
      regionDirtyer.PostInit();
      reachability.PostInit();
    }

    public void ChangeOwner(VehicleDef vehicleDef)
    {
      regionGridManager.createdFor = vehicleDef;
      regionMaker.createdFor = vehicleDef;
      regionAndRoomUpdater.createdFor = vehicleDef;
      regionDirtyer.createdFor = vehicleDef;
      reachability.createdFor = vehicleDef;
    }
  }
}