using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LudeonTK;
using SmashTools;
using Verse;

namespace Vehicles;

public static class GridOwners
{
  public static WorldGridOwners World { get; } = new();

  internal static void RecacheMoveableVehicleDefs()
  {
    VehicleHarmony.AllMoveableVehicleDefs = DefDatabase<VehicleDef>.AllDefsListForReading
     .Where(PathingHelper.ShouldCreateRegions).ToList();

    World.Init();

    if (!Find.Maps.NullOrEmpty())
    {
      foreach (Map map in Find.Maps)
      {
        VehiclePathingSystem mapping = map.GetCachedMapComponent<VehiclePathingSystem>();
        mapping.GridOwners.Init();
        mapping.ConstructComponents();
      }
    }
  }

  [DebugOutput(VehicleHarmony.VehiclesLabel, name = "Output GridOwners")]
  private static void OutputMapOwners()
  {
    Log.Message("------- GridOwners -------");
    Log.Message($"Vehicles = {DefDatabase<VehicleDef>.AllDefsListForReading.Count}");
    StringBuilder sb = new();
    OutputForGrid(World, sb);
    Log.Message($"World:\n{sb}");
    sb.Clear();
    foreach (Map map in Find.Maps)
    {
      VehiclePathingSystem mapping = map.GetCachedMapComponent<VehiclePathingSystem>();
      sb.AppendLine($"  Id: {map.uniqueID}");
      OutputForGrid(mapping.GridOwners, sb);
    }
    Log.Message($"Map:\n{sb}");
    Log.Message("-------");
    return;

    static void OutputForGrid<T>(GridOwnerList<T> gridOwnerList, StringBuilder stringBuilder)
      where T : IPathConfig
    {
      stringBuilder.AppendLine($"  Total Owners = {gridOwnerList.AllOwners.Length}");
      stringBuilder.AppendLine($"  Total Piggies = {gridOwnerList.AllPiggies.Count()}");

      stringBuilder.AppendLine("  List:");
      foreach (VehicleDef vehicleDef in gridOwnerList.AllOwners)
      {
        stringBuilder.AppendLine($"  Owner: {vehicleDef}");
        stringBuilder.AppendLine(
          $"  Piggies=({string.Join(",", gridOwnerList.GetPiggies(vehicleDef).Select(def => def.defName))}");
      }
    }
  }
}