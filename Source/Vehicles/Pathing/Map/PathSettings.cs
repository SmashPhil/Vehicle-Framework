using System;
using System.Runtime.CompilerServices;
using System.Threading;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using SmashTools.Burst;
using Verse;

namespace Vehicles;

[PublicAPI, SkipLocalsInit]
public struct PathSettings
{
  public const int DefaultTurnFactor = 10;

  public required VehicleDef vehicleDef;

  [CanBeNull]
  public VehiclePawn vehicle = null;

  public Rot8 rotation = Rot8.Invalid;
  public GridSetting search = GridSetting.None;
  public TurnData turnData = TurnData.Linear(DefaultTurnFactor);
  public float scalar = 0.15f;

  public CancellationToken token = CancellationToken.None;

  public Heuristic heuristic = new()
  {
    normal = 10,
    prefer = 1,
    avoid = 20
  };

  public PathSettings()
  {
  }

  public static PathSettings For(VehicleDef def)
  {
    return new PathSettings
    {
      vehicleDef = def
    };
  }

  public static PathSettings For(VehicleDef def, [NotNull] Map map, [NotNull] Faction faction)
  {
    PathSettings settings = For(def);
    ApplyFactionSettings(map, faction, ref settings);
    return settings;
  }

  public static PathSettings For(VehiclePawn vehicle)
  {
    PathSettings settings = For(vehicle.VehicleDef) with
    {
      vehicle = vehicle,
      rotation = vehicle.FullRotation
    };
    if (vehicle.Spawned)
    {
      ApplyFactionSettings(vehicle.Map, vehicle.Faction, ref settings);
    }
    return settings;
  }

  private static void ApplyFactionSettings(Map map, Faction faction, ref PathSettings settings)
  {
    Faction hostFaction = map.ParentFaction ?? Faction.OfPlayer;
    if (faction.HostileTo(hostFaction))
    {
      settings.search = GridSetting.BreachWalls | GridSetting.UseAvoidGrid;
    }
  }

  // TODO VF-343 - Implement BreachDestructibles for chunk vs. wall based breaching
  [Flags]
  public enum GridSetting
  {
    None = 0,
    BreachWalls = 1 << 0,
    BreachDestructibles = 1 << 1,
    UseAvoidGrid = 1 << 2
  }
}
