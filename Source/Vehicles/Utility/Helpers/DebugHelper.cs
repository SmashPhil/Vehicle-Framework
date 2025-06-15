using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles
{
  public static class DebugHelper
  {
    public static readonly PathDebugData<DebugRegionType> Local = new();

    public static readonly PathDebugData<WorldPathingDebugType> World = new();

    internal static List<WorldPath> debugLines = [];

    // (TileID, Cycle)
    internal static List<(PlanetTile tile, int radius)> tiles = [];

    public static bool AnyDebugSettings => Local.DebugType != DebugRegionType.None ||
      World.DebugType != WorldPathingDebugType.None;

    /// <summary>
    /// Indiscriminately destroys all entities and roofs from area.
    /// </summary>
    /// <remarks>If non-destroyables should not be destroyed, use <see cref="GenDebug.ClearArea(CellRect, Map)"/> instead.</remarks>
    public static void DestroyArea(CellRect rect, Map map, TerrainDef replaceTerrain = null)
    {
      Thing.allowDestroyNonDestroyable = true;
      try
      {
        rect.ClipInsideMap(map);
        foreach (IntVec3 cell in rect)
        {
          map.roofGrid.SetRoof(cell, null);
        }

        foreach (IntVec3 cell in rect)
        {
          foreach (Thing thing in cell.GetThingList(map).ToList())
          {
            thing.Destroy();
          }
        }

        if (replaceTerrain != null)
        {
          foreach (IntVec3 cell in rect)
          {
            map.terrainGrid.SetTerrain(cell, replaceTerrain);
          }
        }
      }
      finally
      {
        Thing.allowDestroyNonDestroyable = false;
      }
    }

    /// <summary>
    /// Draw settlement debug lines that show original locations before settlement was pushed to the coastline
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    public static void DebugDrawSettlement(PlanetTile from, PlanetTile to)
    {
      Assert.AreEqual(from.Layer, to.Layer);
      PeaceTalks o =
        (PeaceTalks)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfVehicles.DebugSettlement);
      o.Tile = from;
      o.SetFaction(Faction.OfMechanoids);
      Find.WorldObjects.Add(o);
      if (DebugProperties.drawPaths)
      {
        debugLines.Add(from.Layer.Pather.FindPath(from, to, null));
      }
    }

    public static List<Toggle> DebugToggles<T>(VehicleDef vehicleDef, PathDebugData<T> debugData)
      where T : Enum
    {
      List<Toggle> toggles = [];
      if (Enum.GetUnderlyingType(typeof(T)) != typeof(int))
      {
        Log.Error(
          $"Cannot generate DebugToggles for enum type {typeof(T)}. Must be int32 to avoid overflow.");
        return toggles;
      }

      foreach (T @enum in Enum.GetValues(typeof(T)))
      {
        bool flags = typeof(T).IsDefined(typeof(FlagsAttribute), false);

        // Skip empty flag, this is messy but it's strictly for debugging and I can't imagine
        // the enum count of these flags will exceed 32.
        if (flags && Convert.ToInt32(@enum) == 0)
          continue;

        Toggle toggle = new(@enum.ToString(), stateGetter: delegate
        {
          if (debugData.VehicleDef != vehicleDef)
            return false;
          if (!flags)
            return debugData.DebugType.Equals(@enum);
          return debugData.DebugType.HasFlag(@enum);
        }, stateSetter: delegate(bool value)
        {
          debugData.VehicleDef = vehicleDef;
          if (flags)
          {
            debugData.DebugType = (T)Enum.ToObject(typeof(T),
              value ?
                Convert.ToInt32(debugData.DebugType) | Convert.ToInt32(@enum) :
                Convert.ToInt32(debugData.DebugType) & ~Convert.ToInt32(@enum));
          }
          else if (value)
          {
            debugData.DebugType = @enum;
          }
        });

        toggles.Add(toggle);
      }

      return toggles;
    }

    /// <summary>
    /// Draw water regions to show if they are valid and initialized
    /// </summary>
    /// <param name="map"></param>
    public static void DebugDrawVehicleRegion(Map map)
    {
      if (Local.VehicleDef != null)
      {
        map.GetCachedMapComponent<VehiclePathingSystem>()[Local.VehicleDef].VehicleRegionGrid
         .DebugDraw(Local.DebugType);
      }
    }

    /// <summary>
    /// Draw path costs overlay on GUI
    /// </summary>
    /// <param name="map"></param>
    public static void DebugDrawVehiclePathCostsOverlay(Map map)
    {
      if (Local.VehicleDef != null)
      {
        map.GetCachedMapComponent<VehiclePathingSystem>()[Local.VehicleDef].VehicleRegionGrid
         .DebugOnGUI(Local.DebugType);
      }
    }

    public class PathDebugData<T> where T : Enum
    {
      public VehicleDef VehicleDef { get; set; }

      public T DebugType { get; set; }
    }
  }
}