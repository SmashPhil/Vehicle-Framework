using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Vehicles.Compatibility;
using Verse;

namespace Vehicles.World;

public class ArrivalAction_LoadMap : VehicleArrivalAction
{
  public AerialVehicleArrivalModeDef arrivalModeDef;

  public ArrivalAction_LoadMap()
  {
  }

  public ArrivalAction_LoadMap(VehiclePawn vehicle, AerialVehicleArrivalModeDef arrivalModeDef) :
    base(vehicle)
  {
    this.arrivalModeDef = arrivalModeDef;
  }

  public override bool DestroyOnArrival => true;

  public override void Arrived(GlobalTargetInfo target)
  {
    base.Arrived(target);
    LongEventHandler.QueueLongEvent(delegate
    {
      MapParent mapParent = Find.WorldObjects.MapParentAt(target.Tile);
      if (mapParent == null)
      {
        Log.Error("Trying to arrive at map with null MapParent.");
        return;
      }
      bool mapGenerated = !mapParent.HasMap;
			if (AerialVehicleCompatibility.ShouldClaimOnArrival(mapParent))
			{
				mapParent.SetFaction(Faction.OfPlayer);
			}
      Site site = Find.WorldObjects.WorldObjectAt<Site>(target.Tile);
      Map map = site != null ?
        GetOrGenerateMapUtility.GetOrGenerateMap(target.Tile, site.PreferredMapSize, null) :
        GetOrGenerateMapUtility.GetOrGenerateMap(target.Tile, null);
      if (mapGenerated)
      {
        MapHelper.UnfogMapFromEdge(map, vehicle.VehicleDef);

				if (mapParent is EscapeShip)
				{
					Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
					Find.LetterStack.ReceiveLetter("EscapeShipFoundLabel".Translate(), 
						!Find.Storyteller.difficulty.allowBigThreats ? "EscapeShipFoundPeaceful".Translate() : "EscapeShipFound".Translate(), 
						LetterDefOf.PositiveEvent, new GlobalTargetInfo(map.Center, map));
				}
			}
      MapLoaded(map, mapGenerated);
      ExecuteEvents();
      arrivalModeDef.Worker.VehicleArrived(vehicle, vehicle.CompVehicleLauncher.launchProtocol, map);
    }, "GeneratingMap", false, null);
  }

  protected virtual void MapLoaded(Map map, bool hasMap)
  {
  }

  protected virtual void ExecuteEvents()
  {
    vehicle.EventRegistry[VehicleEventDefOf.AerialVehicleLanding].ExecuteEvents();
  }

	// TODO 1.6.2136
	[Obsolete("Deprecated since 1.6 refactor. Currently unused and will be removed.")]
  public static FloatMenuAcceptanceReport CanLand(VehiclePawn vehicle, MapParent mapParent)
  {
    if (mapParent is null || !mapParent.Spawned)
      return false;
    if (!WorldVehiclePathGrid.Instance.Passable(mapParent.Tile, vehicle.VehicleDef))
      return FloatMenuAcceptanceReport.WithFailReason("Impassable".Translate());
    if (mapParent.EnterCooldownBlocksEntering())
    {
      return FloatMenuAcceptanceReport.WithFailReasonAndMessage(
        "EnterCooldownBlocksEntering".Translate(),
        "MessageEnterCooldownBlocksEntering".Translate(mapParent.EnterCooldownTicksLeft()
         .ToStringTicksToPeriod()));
    }
    return true;
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Defs.Look(ref arrivalModeDef, nameof(arrivalModeDef));
  }
}