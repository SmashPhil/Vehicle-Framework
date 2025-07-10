using System.Collections.Generic;
using LudeonTK;
using SmashTools;
using Verse;

namespace Vehicles.World;

public static class AerialVehicleLaunchHelper
{
  public static AerialVehicleInFlight GetOrMakeAerialVehicle(this VehiclePawn vehicle)
  {
    if (vehicle.CompVehicleLauncher is null)
    {
      Trace.Fail($"Trying to launch {vehicle} which is not launchable.");
      return null;
    }
    AerialVehicleInFlight aerialVehicle =
      VehicleWorldObjectsHolder.Instance.AerialVehicleObject(vehicle);
    if (aerialVehicle == null)
    {
      VehicleCaravan vehicleCaravan =
        VehicleWorldObjectsHolder.Instance.VehicleCaravanObject(vehicle);
      if (vehicleCaravan == null)
      {
        Log.Error(
          "Unable to launch aerial vehicle to empty tile. No existing aerial vehicle or caravan found to launch from.");
        return null;
      }
      aerialVehicle = AerialVehicleInFlight.Create(vehicle, vehicleCaravan.Tile);
      bool autoSelect = Find.WorldSelector.SelectedObjects.Contains(vehicleCaravan);

      // Pawns not boarded will be transfered to the converted vanilla Caravan
      // and left behind. Board as many pawns as possible, a pop-up should have
      // confirmed user intent already.
      for (int i = vehicleCaravan.pawns.Count - 1; i >= 0; i--)
      {
        Pawn pawn = vehicleCaravan.pawns.InnerListForReading[i];
        if (pawn.InVehicle())
          continue;

        if (vehicle.TryAddPawn(pawn))
          vehicleCaravan.RemovePawn(pawn);
      }
      // Removing vehicle will convert back to a vanilla Caravan and
      // destroy this instance.
      vehicleCaravan.RemovePawn(vehicle);

      if (autoSelect)
        Find.WorldSelector.Select(aerialVehicle, playSound: false);
    }
    return aerialVehicle;
  }

  [DebugAction(category = VehicleHarmony.VehiclesLabel, name = "Lock Camera to Thing",
    actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
  private static void LockCameraToThing()
  {
    Map map = Find.CurrentMap;
    if (map == null)
    {
      Log.Error("Attempting to use LockCameraToThing with null map.");
      return;
    }
    IntVec3 cell = UI.MouseCell();
    if (cell.InBounds(map))
    {
      List<Thing> thingList = map.thingGrid.ThingsListAtFast(cell);
      if (!thingList.NullOrEmpty())
      {
        List<FloatMenuOption> options = new List<FloatMenuOption>();
        foreach (Thing thing in thingList)
        {
          options.Add(new FloatMenuOption(thing.Label,
            delegate() { CameraAttacher.Create(thing); }));
        }
        Find.WindowStack.Add(new FloatMenu(options));
      }
    }
  }
}