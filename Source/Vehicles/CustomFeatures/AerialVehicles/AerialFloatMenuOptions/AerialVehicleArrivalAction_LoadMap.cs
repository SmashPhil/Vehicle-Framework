using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles.World;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class AerialVehicleArrivalAction_LoadMap : AerialVehicleArrivalAction
{
  protected int tile;

  public LaunchProtocol launchProtocol;
  public AerialVehicleArrivalModeDef arrivalModeDef;

  public AerialVehicleArrivalAction_LoadMap()
  {
  }

  public AerialVehicleArrivalAction_LoadMap(VehiclePawn vehicle, LaunchProtocol launchProtocol,
    int tile, AerialVehicleArrivalModeDef arrivalModeDef)
    : base(vehicle)
  {
    this.tile = tile;
    this.launchProtocol = launchProtocol;
    this.arrivalModeDef = arrivalModeDef;
  }

  public override bool DestroyOnArrival => true;

  public override void Arrived(AerialVehicleInFlight aerialVehicle, int tile)
  {
    base.Arrived(aerialVehicle, tile);
    LongEventHandler.QueueLongEvent(delegate
    {
      MapParent mapParent = Find.WorldObjects.MapParentAt(tile);
      if (mapParent == null)
      {
        Log.Error("Trying to arrive at map with null MapParent.");
        return;
      }
      bool mapGenerated = !mapParent.HasMap;

      Site site = Find.WorldObjects.WorldObjectAt<Site>(tile);
      Map map = site != null ?
        GetOrGenerateMapUtility.GetOrGenerateMap(tile, site.PreferredMapSize, null) :
        GetOrGenerateMapUtility.GetOrGenerateMap(tile, null);
      if (mapGenerated)
      {
        MapHelper.UnfogMapFromEdge(map, aerialVehicle.vehicle.VehicleDef);
      }
      MapLoaded(map, mapGenerated);
      ExecuteEvents();
      arrivalModeDef.Worker.VehicleArrived(vehicle, launchProtocol, map);
    }, "GeneratingMap", false, null);
  }

  protected virtual void MapLoaded(Map map, bool hasMap)
  {
  }

  protected virtual void ExecuteEvents()
  {
    vehicle.EventRegistry[VehicleEventDefOf.AerialVehicleLanding].ExecuteEvents();
  }

  public static FloatMenuAcceptanceReport CanLand(VehiclePawn vehicle, MapParent mapParent)
  {
    if (mapParent is null || !mapParent.Spawned)
    {
      return false;
    }
    if (!WorldVehiclePathGrid.Instance.Passable(mapParent.Tile, vehicle.VehicleDef))
    {
      return false;
    }
    if (mapParent.EnterCooldownBlocksEntering())
    {
      return FloatMenuAcceptanceReport.WithFailReasonAndMessage(
        "EnterCooldownBlocksEntering".Translate(),
        "MessageEnterCooldownBlocksEntering".Translate(mapParent.EnterCooldownTicksLeft()
         .ToStringTicksToPeriod()));
    }
    return true;
  }

  public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(VehiclePawn vehicle,
    LaunchProtocol launchProtocol, MapParent mapParent)
  {
    if (vehicle.CompVehicleLauncher.ControlInFlight)
    {
      foreach (FloatMenuOption floatMenuOption2 in
        VehicleArrivalActionUtility.GetFloatMenuOptions(() => CanLand(vehicle, mapParent),
          () => new AerialVehicleArrivalAction_LoadMap(vehicle, launchProtocol, mapParent.Tile,
            AerialVehicleArrivalModeDefOf.TargetedLanding),
          "VF_LandVehicleTargetedLanding".Translate(mapParent.Label), vehicle, mapParent.Tile))
      {
        yield return floatMenuOption2;
      }
    }
    foreach (FloatMenuOption floatMenuOption2 in VehicleArrivalActionUtility.GetFloatMenuOptions(
      () => CanLand(vehicle, mapParent),
      () => new AerialVehicleArrivalAction_LoadMap(vehicle, launchProtocol, mapParent.Tile,
        AerialVehicleArrivalModeDefOf.EdgeDrop),
      "VF_LandVehicleEdge".Translate(mapParent.Label), vehicle, mapParent.Tile))
    {
      yield return floatMenuOption2;
    }
    foreach (FloatMenuOption floatMenuOption2 in VehicleArrivalActionUtility.GetFloatMenuOptions(
      () => CanLand(vehicle, mapParent),
      () => new AerialVehicleArrivalAction_LoadMap(vehicle, launchProtocol, mapParent.Tile,
        AerialVehicleArrivalModeDefOf.CenterDrop),
      "VF_LandVehicleCenter".Translate(mapParent.Label), vehicle, mapParent.Tile))
    {
      yield return floatMenuOption2;
    }
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Values.Look(ref tile, nameof(tile));
    Scribe_Defs.Look(ref arrivalModeDef, nameof(arrivalModeDef));
    Scribe_Deep.Look(ref launchProtocol, nameof(launchProtocol));
  }
}