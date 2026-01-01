using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevTools.Benchmarking;
using LudeonTK;
using SmashTools;
using Verse;

namespace Vehicles.Benchmarking;

[BenchmarkClass("Region Grid Generation", AllowedGameStates = AllowedGameStates.PlayingOnMap)]
internal class Benchmark_RegionGridGeneration
{
  private const int VehicleTestCount = 5;

  [Prepare]
  private static void RegionGridSetup(ref RegionGridContext context)
  {
    foreach (VehicleDef vehicleDef in context.vehicleDefs)
    {
      VehiclePathingSystem.VehiclePathData pathData = context.mapping[vehicleDef];
      if (!pathData.VehiclePathGrid.Enabled)
        pathData.VehiclePathGrid.RecalculateAllPerceivedPathCosts();
    }
  }


  [Benchmark(Label = "Parallel")]
  private static void RegionGridGen_Parallel(ref RegionGridContext context)
  {
    VehiclePathingSystem mapping = context.mapping;
    // delegate will add a little bit of overhead from but this is how the original method
    // is written so it's accurate.
    Parallel.ForEach(context.vehicleDefs, delegate(VehicleDef vehicleDef)
    {
      VehiclePathingSystem.VehiclePathData vehiclePathData = mapping[vehicleDef];
      vehiclePathData.VehicleRegionAndRoomUpdater.Init();
      vehiclePathData.VehicleRegionAndRoomUpdater.RebuildAllVehicleRegions();
    });
  }

  [Benchmark(Label = "Partitioned")]
  private static void RegionGridGen_Partitioned(ref RegionGridContext context)
  {
    VehiclePathingSystem mapping = context.mapping;
    List<VehicleDef> vehicleDefs = context.vehicleDefs;
    // delegate will add a little bit of overhead from but this is how the original method
    // is written so it's accurate.
    Parallel.ForEach(Partitioner.Create(0, context.vehicleDefs.Count), (range, _) =>
    {
      for (int i = range.Item1; i < range.Item2; i++)
      {
        VehiclePathingSystem.VehiclePathData vehiclePathData = mapping[vehicleDefs[i]];
        vehiclePathData.VehicleRegionAndRoomUpdater.Init();
        vehiclePathData.VehicleRegionAndRoomUpdater.RebuildAllVehicleRegions();
      }
    });
  }

  [Benchmark(Label = "Sequential")]
  private static void RegionGridGen_Sequential(ref RegionGridContext context)
  {
    VehiclePathingSystem mapping = context.mapping;

    foreach (VehicleDef vehicleDef in context.vehicleDefs)
    {
      VehiclePathingSystem.VehiclePathData vehiclePathData = mapping[vehicleDef];
      vehiclePathData.VehicleRegionAndRoomUpdater.Init();
      vehiclePathData.VehicleRegionAndRoomUpdater.RebuildAllVehicleRegions();
    }
  }

  private readonly struct RegionGridContext
  {
    public readonly VehiclePathingSystem mapping;
    public readonly List<VehicleDef> vehicleDefs;

    public RegionGridContext()
    {
      this.mapping = Find.CurrentMap.GetCachedMapComponent<VehiclePathingSystem>();
      this.vehicleDefs =
        mapping.GridOwners.AllOwners.Take(VehicleTestCount).ToList();
    }
  }
}