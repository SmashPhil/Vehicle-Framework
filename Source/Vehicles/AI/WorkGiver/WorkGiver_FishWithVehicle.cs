using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using Vehicles.Compatibility;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

[UsedWithReflection]
public sealed class WorkGiver_FishWithVehicle : WorkGiver_GroupWork
{
  protected override bool CanMergeWith(IVehicleTaskLord lord)
  {
    return lord.Task is FishTask;
  }

  public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
  {
    return pawn.Map.GetDetachedMapComponent<VehiclePositionManager>().AllClaimants;
  }

  protected override bool CanUseVehicle(Pawn pawn, VehiclePawn vehicle)
  {
    return FishingCompatibility.EnabledFor(vehicle.VehicleDef);
  }

  protected override Lord GetLordToJoin(Pawn pawn, VehiclePawn vehicle)
  {
    IntVec3 cell = FindFishingSpot(pawn, vehicle);
    if (!cell.IsValid)
      return null;

    if (vehicle.lord == null)
    {
      if (!TaskSpotFinder.TryFindMeetingSpot(vehicle, pawn, out IntVec3 meetingSpot))
        return null;

      VehicleTaskData data = new()
      {
        vehicle = vehicle,
        task = new FishTask(vehicle),
        destination = cell,
        meetingSpot = meetingSpot
      };
      LordJob_TravelWithVehicle lordJob = new(pawn, data);
      LordMaker.MakeNewLord(vehicle.Faction, lordJob, vehicle.Map, [vehicle, pawn]);
    }
    return vehicle.lord;
  }

  private static IntVec3 FindFishingSpot(Pawn pawn, VehiclePawn vehicle)
  {
    const int ZoneSearchAttempts = WorkGiver_Fish.TriesToFindSpotThatIsntInWater;

    Map map = pawn.Map;

    if (ModsConfig.OdysseyActive)
    {
      Zone_Fishing nearestZone = null;
      float nearestDist = float.MaxValue;

      foreach (Zone zone in map.zoneManager.AllZones)
      {
        if (zone is not Zone_Fishing { ShouldFishNow: true, HasAnyFishableCells: true } fishingZone)
          continue;

        float dist = vehicle.Position.DistanceToSquared(fishingZone.Cells[0]);
        if (nearestZone == null || dist < nearestDist)
        {
          nearestZone = fishingZone;
          nearestDist = dist;
        }
      }

      if (nearestZone != null)
      {
        for (int i = 0; i < ZoneSearchAttempts; i++)
        {
          IntVec3 cell = nearestZone.RandomFishableCell;
          if (IsValidFishingCell(vehicle, cell))
            return cell;
        }
      }
    }

    // Vehicles prefer fishing in zones before random spots nearby. This is to allow players to
    // mark areas for boats to fish further out at sea where they can't normally reach.
    if (IsValidFishingCell(vehicle, vehicle.Position))
      return vehicle.Position;

    for (int i = 0; i < 4; i++)
    {
      IntVec3 cell = vehicle.Position + GenAdj.CardinalDirections[(i + vehicle.thingIDNumber) % 4];
      if (IsValidFishingCell(vehicle, cell))
        return cell;
    }

    const int MaxSearchRadius = 128;
    if (CellFinderExtended.TryRandomClosewalkCellNear(vehicle.Position, map, vehicle.VehicleDef, MaxSearchRadius,
          out IntVec3 result, cell => FishingCompatibility.CanFishAt(vehicle, cell) && vehicle.CellRectStandable(map, cell)))
    {
      return result;
    }
    return IntVec3.Invalid;
  }

  private static bool IsValidFishingCell(VehiclePawn vehicle, IntVec3 cell)
  {
    return cell.IsValid &&
      cell.InBounds(vehicle.Map) &&
      FishingCompatibility.CanFishAt(vehicle, cell) &&
      vehicle.CellRectStandable(vehicle.Map, cell) &&
      vehicle.CanReachVehicle(cell, PathEndMode.OnCell, Danger.Some);
  }
}
