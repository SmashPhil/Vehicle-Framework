using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public static class VehicleArrivalActionUtility
	{
		public static IEnumerable<FloatMenuOption> GetFloatMenuOptions<T>(Func<FloatMenuAcceptanceReport> acceptanceReportGetter, Func<T> arrivalActionGetter, string label, VehiclePawn vehicle, int destinationTile, Action<Action> uiConfirmationCallback = null) where T : AerialVehicleArrivalAction
		{
			FloatMenuAcceptanceReport floatMenuAcceptanceReport = acceptanceReportGetter();
			if (!floatMenuAcceptanceReport.Accepted && floatMenuAcceptanceReport.FailReason.NullOrEmpty() && floatMenuAcceptanceReport.FailMessage.NullOrEmpty())
			{
				yield break;
			}
			if (!floatMenuAcceptanceReport.FailReason.NullOrEmpty())
			{
				yield return new FloatMenuOption(label + " (" + floatMenuAcceptanceReport.FailReason + ")", null);
				yield break;
			}
			yield return new FloatMenuOption(label, delegate
			{
				FloatMenuAcceptanceReport floatMenuAcceptanceReport2 = acceptanceReportGetter();
				if (floatMenuAcceptanceReport2.Accepted)
				{
					if (uiConfirmationCallback == null)
					{
						if (!vehicle.Spawned && AerialVehicleLaunchHelper.GetOrMakeAerialVehicle(vehicle) is AerialVehicleInFlight aerialVehicle)
						{
							aerialVehicle.OrderFlyToTiles(LaunchTargeter.FlightPath, aerialVehicle.DrawPos, arrivalActionGetter());
						}
						else
						{
							vehicle.CompVehicleLauncher.TryLaunch(destinationTile, arrivalActionGetter());
						}
					}
					else
					{
						if (!vehicle.Spawned && AerialVehicleLaunchHelper.GetOrMakeAerialVehicle(vehicle) is AerialVehicleInFlight aerialVehicle)
						{
							uiConfirmationCallback(delegate
							{
								aerialVehicle.OrderFlyToTiles(LaunchTargeter.FlightPath, aerialVehicle.DrawPos, arrivalActionGetter());
							});
						}
						else
						{
							uiConfirmationCallback(delegate
							{
								vehicle.CompVehicleLauncher.TryLaunch(destinationTile, arrivalActionGetter());
							});
						}
					}
				}
				else if (!floatMenuAcceptanceReport2.FailMessage.NullOrEmpty())
				{
					Messages.Message(floatMenuAcceptanceReport2.FailMessage, new GlobalTargetInfo(destinationTile), MessageTypeDefOf.RejectInput, historical: false);
				}
			});
		}
	}
}
