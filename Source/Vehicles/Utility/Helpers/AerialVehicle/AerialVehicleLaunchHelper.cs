using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public static class AerialVehicleLaunchHelper
	{
		public static AerialVehicleInFlight GetOrMakeAerialVehicle(VehiclePawn vehicle)
		{
			AerialVehicleInFlight aerialVehicle = VehicleWorldObjectsHolder.Instance.AerialVehicleObject(vehicle);
			if (aerialVehicle == null)
			{
				VehicleCaravan vehicleCaravan = VehicleWorldObjectsHolder.Instance.VehicleCaravanObject(vehicle);
				if (vehicleCaravan == null)
				{
					Log.Error($"Unable to launch aerial vehicle to empty tile. No existing aerial vehicle or caravan found to launch from.");
					return null;
				}
				bool autoSelect = false;
				if (Find.WorldSelector.SelectedObjects.Contains(vehicleCaravan))
				{
					autoSelect = true;
				}

				aerialVehicle = AerialVehicleInFlight.Create(vehicle, vehicleCaravan.Tile);
				vehicleCaravan.RemoveAllPawns();
				vehicleCaravan.Destroy();

				if (autoSelect)
				{
					Find.WorldSelector.Select(aerialVehicle, playSound: false);
				}
			}
			return aerialVehicle;
		}

		public static bool ChoseTargetOnMap(VehiclePawn vehicle, int fromTile, GlobalTargetInfo target, float fuelCost)
		{
			return vehicle.CompVehicleLauncher.launchProtocol.ChoseWorldTarget(target, Find.WorldGrid.GetTileCenter(fromTile), 
				(GlobalTargetInfo target, Vector3 pos, Action<int, AerialVehicleArrivalAction, bool> launchAction) => CanTarget(vehicle, fuelCost, target, pos, launchAction), 
				(int destinationTile, AerialVehicleArrivalAction arrivalAction, bool recon) => NewDestination(vehicle, fromTile, destinationTile, arrivalAction, recon));
		}

		private static void NewDestination(VehiclePawn vehicle, int fromTile, int destinationTile, AerialVehicleArrivalAction arrivalAction, bool recon = false)
		{
			vehicle.CompVehicleLauncher.inFlight = true;
			AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(vehicle, fromTile);
			aerialVehicle.recon = recon;
			aerialVehicle.OrderFlyToTiles(LaunchTargeter.FlightPath, Find.WorldGrid.GetTileCenter(fromTile), arrivalAction: arrivalAction);
		}

		private static bool CanTarget(VehiclePawn vehicle, float fuelCost, GlobalTargetInfo target, Vector3 pos, Action<int, AerialVehicleArrivalAction, bool> launchAction)
		{
			if (!target.IsValid)
			{
				Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
				return false;
			}
			else if (Ext_Math.SphericalDistance(pos, WorldHelper.GetTilePos(target.Tile)) > vehicle.CompVehicleLauncher.MaxLaunchDistance || fuelCost > vehicle.CompFueledTravel.Fuel)
			{
				Messages.Message("TransportPodDestinationBeyondMaximumRange".Translate(), MessageTypeDefOf.RejectInput, false);
				return false;
			}
			IEnumerable<FloatMenuOption> source = vehicle.CompVehicleLauncher.launchProtocol.GetFloatMenuOptionsAt(target.Tile);
			if (!source.Any())
			{
				if (!WorldVehiclePathGrid.Instance.Passable(target.Tile, vehicle.VehicleDef))
				{
					Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
					return false;
				}
				launchAction(target.Tile, null, false);
				return true;
			}
			else
			{
				if (source.Count() != 1)
				{
					Find.WindowStack.Add(new FloatMenuTargeter(source.ToList()));
					return false;
				}
				if (!source.First().Disabled)
				{
					source.First().action();
					return true;
				}
				return false;
			}
		}
	}
}
