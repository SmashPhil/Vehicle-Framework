using JetBrains.Annotations;
using Verse;
using Verse.AI;

namespace Vehicles;

[PublicAPI]
public static class TaskSpotFinder
{
  private const int BestSpotSearchRadius = 16;
  private const int MaxSearchRadius = 32;

  public static bool TryFindMeetingSpot(VehiclePawn vehicle, Pawn pawn, out IntVec3 meetingSpot)
  {
    meetingSpot = IntVec3.Invalid;
    Map map = vehicle.Map;

    float bestScore = float.MaxValue;
    int closeCells = GenRadial.NumCellsInRadius(BestSpotSearchRadius);
    for (int i = 0; i < closeCells; i++)
    {
      IntVec3 cell = vehicle.Position + GenRadial.RadialPattern[i];
      if (!cell.InBounds(map) || !IsValidMeetingSpot(cell))
        continue;

      float score = MeetingSpotScore(cell);
      if (score < bestScore)
      {
        bestScore = score;
        meetingSpot = cell;
      }
    }

    return meetingSpot.IsValid ||
      CellFinderExtended.TryRadialSearchForCell(vehicle.Position, map, MaxSearchRadius,
        IsValidMeetingSpot, out meetingSpot);

    bool IsValidMeetingSpot(IntVec3 cell)
    {
      if (!cell.Standable(map))
        return false;

      if (!pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
        return false;

      return map.reachability.CanReach(cell, vehicle.Position, PathEndMode.Touch, TraverseParms.For(pawn));
    }

    float MeetingSpotScore(IntVec3 cell)
    {
      int pathCost = map.pathing.Normal.pathGrid.Cost(cell);
      float pawnDistance = pawn.Position.DistanceTo(cell);
      float vehicleDistance = vehicle.Position.DistanceTo(cell);
      return pathCost * 1000f + vehicleDistance * 25f + pawnDistance;
    }
  }
}
