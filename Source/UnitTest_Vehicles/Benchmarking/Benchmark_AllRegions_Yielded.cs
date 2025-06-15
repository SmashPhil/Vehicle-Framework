using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DevTools.Benchmarking;
using LudeonTK;
using SmashTools;
using Verse;

// ReSharper disable all

namespace Vehicles.Benchmarking;

[BenchmarkClass("yield_AllRegions", AllowedGameStates = AllowedGameStates.PlayingOnMap)]
[SampleSize(1000)]
internal class Benchmark_AllRegions_Yielded
{
  [Benchmark(Label = "Parallel")]
  [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
  private static void AllRegions_Parallel(ref RegionContext context)
  {
    VehicleRegionGrid regionGrid = context.regionGrid;
    ConcurrentSet<VehicleRegion> allRegions = context.allRegionsConcurrent;
    List<VehicleRegion> regions = context.regions;
    Parallel.For(0, context.mapping.map.cellIndices.NumGridCells, delegate(int index)
    {
      VehicleRegion region = regionGrid.GetRegionAt(index);
      if (region != null && region.valid && allRegions.Add(region))
      {
        regions.Add(region);
      }
    });

    foreach (VehicleRegion region in regions)
    {
    }
    regions.Clear();
  }

  [Benchmark(Label = "Partitioned")]
  [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
  private static void AllRegions_Partitioned(ref RegionContext context)
  {
    VehicleRegionGrid regionGrid = context.regionGrid;
    ConcurrentSet<VehicleRegion> allRegions = context.allRegionsConcurrent;
    List<VehicleRegion> regions = context.regions;
    Parallel.ForEach(Partitioner.Create(0, context.mapping.map.cellIndices.NumGridCells),
      (range, _) =>
      {
        for (int i = range.Item1; i < range.Item2; i++)
        {
          VehicleRegion region = regionGrid.GetRegionAt(i);
          if (region != null && region.valid && allRegions.Add(region))
          {
            regions.Add(region);
          }
        }
      });

    foreach (VehicleRegion region in regions)
    {
    }
    regions.Clear();
  }

  [Benchmark(Label = "Sequential")]
  [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
  private static void AllRegions_Sequential(ref RegionContext context)
  {
    VehicleRegionGrid regionGrid = context.regionGrid;
    HashSet<VehicleRegion> allRegions = context.allRegions;
    for (int i = 0; i < context.mapping.map.cellIndices.NumGridCells; i++)
    {
      VehicleRegion region = regionGrid.GetRegionAt(i);
      if (region != null && region.valid && allRegions.Add(region))
      {
      }
    }
  }

  private readonly struct RegionContext
  {
    public readonly VehiclePathingSystem mapping;
    public readonly VehicleRegionGrid regionGrid;
    public readonly HashSet<VehicleRegion> allRegions = [];
    public readonly ConcurrentSet<VehicleRegion> allRegionsConcurrent = [];
    public readonly List<VehicleRegion> regions = [];

    public RegionContext()
    {
      this.mapping = Find.CurrentMap.GetCachedMapComponent<VehiclePathingSystem>();
      VehicleDef vehicleDef = mapping.GridOwners.AllOwners.FirstOrDefault(def =>
        !Find.CurrentMap.GetCachedMapComponent<VehiclePathingSystem>()[def].Suspended);
      this.regionGrid = mapping[vehicleDef].VehicleRegionGrid;
    }
  }
}