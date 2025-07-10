using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using Verse;

namespace Vehicles.World;

[PublicAPI]
public static class EnterMapUtilityVehicles
{
  public static void EnterAndSpawn(VehicleCaravan caravan, Map map, CaravanEnterMode enterMode,
    CaravanDropInventoryMode dropInventoryMode = CaravanDropInventoryMode.DoNotDrop,
    bool draftColonists = false, Predicate<IntVec3> extraValidator = null)
  {
    if (enterMode == CaravanEnterMode.None)
    {
      Log.Error(
        $"VehicleCaravan {caravan} tried to enter map {map} with no enter mode. Defaulting to edge.");
      enterMode = CaravanEnterMode.Edge;
    }

    IntVec3 enterCell = GetEnterCellVehicle(caravan, map, enterMode, extraValidator);
    Rot4 edge = enterMode == CaravanEnterMode.Edge ?
      CellRect.WholeMap(map).GetClosestEdge(enterCell) :
      Rot4.North;
    SpawnVehicles(caravan, caravan.PawnsListForReading.Where(p => !p.InVehicle()).ToList(), map,
      enterCell, edge, draftColonists);
  }

  private static void SpawnVehicles(VehicleCaravan caravan, List<Pawn> pawns, Map map,
    IntVec3 enterCell, Rot4 edge, bool draftColonists)
  {
    bool coastalSpawn = caravan.HasBoat();
    foreach (Pawn pawn in pawns)
    {
      IntVec3 cell = CellFinderExtended.RandomSpawnCellForPawnNear(enterCell, map, pawn,
        cell => cell.StandableUnknown(pawn, map), coastalSpawn);
      IntVec3 loc = pawn.ClampToMap(cell, map, extraOffset: 2);
      GenSpawn.Spawn(pawn, loc, map, edge.Opposite);
      Trace.IsTrue(pawn.Spawned);

      if (pawn.IsColonist && !pawn.InMentalState)
      {
        pawn.drafter.Drafted = draftColonists;
      }

      if (pawn is VehiclePawn vehicle)
      {
        vehicle.Angle = 0;
        vehicle.ignition.Drafted = draftColonists;
      }
    }

    caravan.RemoveAllPawns();
    if (caravan.Spawned)
    {
      Find.WorldObjects.Remove(caravan);
    }
  }

  private static Rot4 CalculateEdgeToSpawnBoatOn(Map map)
  {
    if (Find.World.CoastDirectionAt(map.Tile) is { IsValid: true } coastDir)
      return coastDir;

    SurfaceTile surfaceTile = Find.WorldGrid.Surface[map.Tile];
    if (surfaceTile is null || surfaceTile.Rivers.NullOrEmpty())
      return Rot4.Invalid;

    float angle = Find.WorldGrid.GetHeadingFromTo(map.Tile,
      surfaceTile.Rivers.OrderBy(link => link.river.degradeThreshold).First().neighbor);
    return angle.ClampAngle() switch
    {
      < 45  => Rot4.South,
      < 135 => Rot4.East,
      < 225 => Rot4.North,
      < 315 => Rot4.West,
      _     => throw new ArgumentException("ClampAndWrap did not return valid 0:360 value")
    };
  }

  private static IntVec3 FindCenterCell(Map map, VehicleDef vehicleDef,
    Predicate<IntVec3> extraCellValidator)
  {
    if (RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith(
      cell => Validator(map, vehicleDef, cell, extraCellValidator), map, out IntVec3 result))
      return result;
    Log.Warning("Could not find any valid cell.");
    return CellFinder.RandomCell(map);

    static bool Validator(Map map, VehicleDef vehicleDef, IntVec3 cell,
      Predicate<IntVec3> extraCellValidator)
    {
      if (extraCellValidator != null && !extraCellValidator(cell))
        return false;
      return cell.Standable(vehicleDef, map) && !cell.Fogged(map) &&
        map.reachability.CanReachMapEdge(cell, TraverseParms.For(TraverseMode.NoPassClosedDoors));
    }
  }

  public static IntVec3 GetEnterCellVehicle(VehicleCaravan caravan, Map map,
    CaravanEnterMode enterMode, Predicate<IntVec3> extraCellValidator)
  {
    switch (enterMode)
    {
      case CaravanEnterMode.Edge:
        return FindNearEdgeCell(map, caravan.LeadVehicle.VehicleDef, caravan.Faction,
          extraCellValidator);
      case CaravanEnterMode.Center:
        return FindCenterCell(map, caravan.LeadVehicle.VehicleDef, extraCellValidator);
      case CaravanEnterMode.None:
      default:
        throw new NotImplementedException("CaravanEnterMode");
    }
  }

  private static IntVec3 FindNearEdgeCell(Map map, VehicleDef vehicleDef, Faction faction,
    Predicate<IntVec3> extraCellValidator)
  {
    Rot4 rot = Rot4.Random;
    if (vehicleDef.type == VehicleType.Sea)
    {
      rot = CalculateEdgeToSpawnBoatOn(map);
    }

    RoadPreference preference = RoadPreferenceFor(faction);
    while (preference > RoadPreference.Invalid)
    {
      if (TryFindCellWithBestPreference(out IntVec3 root))
        return root;
      preference--;
    }

    Log.Warning("Could not find any valid edge cell.");
    return CellFinder.RandomCell(map);

    bool TryFindCellWithBestPreference(out IntVec3 foundCell)
    {
      foundCell = IntVec3.Invalid;

      if (TryFindNearEdgeCell(map, vehicleDef, rot, preference, extraCellValidator, out foundCell))
        return true;

      if (TryFindNearEdgeCell(map, vehicleDef, rot.Opposite, preference, extraCellValidator,
        out foundCell))
        return true;

      if (TryFindNearEdgeCell(map, vehicleDef, rot.Rotated(RotationDirection.Clockwise),
        preference, extraCellValidator, out foundCell))
        return true;

      if (TryFindNearEdgeCell(map, vehicleDef, rot.Rotated(RotationDirection.Counterclockwise),
        preference, extraCellValidator, out foundCell))
        return true;

      return false;
    }
  }

  private static bool TryFindNearEdgeCell(Map map, VehicleDef vehicleDef, Rot4 rot,
    RoadPreference roadPref, Predicate<IntVec3> extraCellValidator, out IntVec3 root)
  {
    Faction hostFaction = map.ParentFaction;
    if (CellFinderExtended.TryFindRandomEdgeCellWith(cell => Validator(cell) &&
        (extraCellValidator == null || extraCellValidator(cell)) &&
        ((hostFaction != null && map.reachability.CanReachFactionBase(cell, hostFaction)) ||
          (hostFaction == null && map.reachability.CanReachBiggestMapEdgeDistrict(cell))) &&
        AllowsPreference(map, cell, roadPref),
      map, rot, vehicleDef, CellFinder.EdgeRoadChance_Always, out root))
    {
      return true;
    }

    if (CellFinderExtended.TryFindRandomEdgeCellWith(cell => Validator(cell) &&
        (extraCellValidator is null || extraCellValidator(cell)),
      map, rot, vehicleDef, CellFinder.EdgeRoadChance_Always, out root))
    {
      root = CellFinderExtended.RandomClosewalkCellNear(root, map, vehicleDef, 5);
      return true;
    }

    return false;

    bool Validator(IntVec3 cell) => cell.Standable(vehicleDef, map) && !cell.Fogged(map);
  }

  private static RoadPreference RoadPreferenceFor(Faction faction)
  {
    return faction.HostileTo(Faction.OfPlayer) ? RoadPreference.None : RoadPreference.Prioritize;
  }

  private static bool AllowsPreference(Map map, IntVec3 cell, RoadPreference roadPref)
  {
    switch (roadPref)
    {
      case RoadPreference.NoAvoidal:
        Area_RoadAvoidal areaAvoid = map.areaManager.Get<Area_RoadAvoidal>();
        return !areaAvoid[cell];
      case RoadPreference.Prioritize:
        Area_Road areaPrefer = map.areaManager.Get<Area_Road>();
        return areaPrefer[cell];
    }
    return true;
  }

  private enum RoadPreference
  {
    Invalid,
    None,
    NoAvoidal,
    Prioritize,
  }
}