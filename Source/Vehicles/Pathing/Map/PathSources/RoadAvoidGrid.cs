using JetBrains.Annotations;
using RimWorld;
using SmashTools.Burst;
using Verse;

namespace Vehicles;

public sealed class RoadAvoidGrid : VehicleBitOffsets
{
  public RoadAvoidGrid(Map map, [NotNull] PathFinderManager manager) : base(map, manager)
  {
  }

  protected override Cost CostFor(in PathSettings settings)
  {
    return new Cost
    {
      set = 250,
      unset = 0
    };
  }

  public override bool ShouldApplyFor(in PathSettings settings)
  {
    return settings.vehicle is { } vehicle && vehicle.Faction == Faction.OfPlayer;
  }
}
