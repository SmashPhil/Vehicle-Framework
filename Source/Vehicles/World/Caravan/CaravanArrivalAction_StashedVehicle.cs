using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles.World;

[PublicAPI]
public class CaravanArrivalAction_StashedVehicle : CaravanArrivalAction
{
  private StashedVehicle stashedVehicle;

  public CaravanArrivalAction_StashedVehicle()
  {
  }

  public CaravanArrivalAction_StashedVehicle(StashedVehicle stashedVehicle)
  {
    this.stashedVehicle = stashedVehicle;
  }

  public override string Label => "VF_RecoverVehicles".Translate();

  public override string ReportString => "CaravanVisiting".Translate(stashedVehicle.Label);

  public override FloatMenuAcceptanceReport StillValid(Caravan caravan,
    PlanetTile destinationTile)
  {
    FloatMenuAcceptanceReport floatMenuAcceptanceReport =
      base.StillValid(caravan, destinationTile);
    if (!floatMenuAcceptanceReport)
    {
      return floatMenuAcceptanceReport;
    }
    if (stashedVehicle != null && stashedVehicle.Tile != destinationTile)
    {
      return false;
    }
    return CanVisit(caravan, stashedVehicle);
  }

  public override void Arrived(Caravan caravan)
  {
    stashedVehicle.Notify_CaravanArrived(caravan);
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_References.Look(ref stashedVehicle, "stashedVehicle");
  }

  public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, StashedVehicle stashedVehicle)
  {
    return stashedVehicle is { Spawned: true };
  }

  public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan,
    StashedVehicle stashedVehicle)
  {
    return CaravanArrivalActionUtility.GetFloatMenuOptions(
      () => CanVisit(caravan, stashedVehicle),
      () => new CaravanArrivalAction_StashedVehicle(stashedVehicle),
      "VF_RecoverVehicles".Translate(), caravan, stashedVehicle.Tile,
      stashedVehicle);
  }
}