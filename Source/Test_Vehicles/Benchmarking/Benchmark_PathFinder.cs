using System.Linq;
using System.Threading;
using CoreLib.Performance;
using DevTools.Benchmarking;
using LudeonTK;
using RimWorld;
using Vehicles;
using Verse;

namespace SmashTools.Burst;

[BenchmarkClass("PathFinder", AllowedGameStates = AllowedGameStates.PlayingOnMap)]
internal class Benchmark_PathFinder
{
  [Prepare]
  private static void EnsureValid()
  {
    if (!BurstTest.IsBurstLibLoaded())
    {
      Log.Error("Burst is not enabled. Benchmark will be comparing against managed job.");
    }
  }

  [Prepare]
  private static void SpawnVehicle(ref PathFinderContext context)
  {
    Map map = context.map;
    CellRect testRect = context.testArea;
    UnityThread.ExecuteOnMainThreadAndWait(delegate
    {
      DebugHelper.DestroyArea(testRect, map, replaceTerrain: TerrainDefOf.Concrete);
    });
    VehiclePawn vehicle = null;
    VehicleDef vehicleDef = context.vehicleDef;
    IntVec3 start = context.start;
    UnityThread.ExecuteOnMainThreadAndWait(delegate
    {
      vehicle = VehicleSpawner.SpawnVehicleRandomized(vehicleDef, start, map, Faction.OfPlayer, rot: Rot4.North);
    });
    context.vehicle = vehicle;
  }

  [OnFinish]
  private static void DestroyVehicle(ref PathFinderContext context)
  {
    VehiclePawn vehicle = context.vehicle;
    UnityThread.ExecuteOnMainThread(() => vehicle.Destroy());
  }

  [Benchmark(Label = "Legacy")]
  private static void Legacy(ref PathFinderContext context)
  {
    using var path = context.legacy.FindPath(context.start, context.end, context.vehicle, CancellationToken.None);
  }

  [Benchmark(Label = "Burst")]
  private static void Burst(ref PathFinderContext context)
  {
    var path = context.burst.FindPath(new PathRequest
    {
      start = context.start,
      end = context.end,
      rotation = 0,
      smoothen = false
    });
  }

  private struct PathFinderContext
  {
    private const int TestRadius = 100;

    public readonly IntVec3 start;
    public readonly IntVec3 end;

    public readonly Map map;
    public readonly CellRect testArea;
    public readonly PathFinder burst;
    public readonly VehiclePathFinder legacy;
    public readonly VehicleDef vehicleDef;

    public VehiclePawn vehicle;

    public PathFinderContext()
    {
      map = Find.CurrentMap;
      vehicleDef = VehicleHarmony.AllMoveableVehicleDefs.First();
      VehiclePathingSystem pathing = map.GetCachedMapComponent<VehiclePathingSystem>();
      burst = pathing[vehicleDef].PathFinder;
      legacy = pathing[vehicleDef].VehiclePathFinder;

      testArea = CellRect.CenteredOn(map.Center, TestRadius);
      start = testArea.Min + vehicleDef.Size.ToIntVec3;
      end = testArea.Max - vehicleDef.Size.ToIntVec3;
    }
  }
}
