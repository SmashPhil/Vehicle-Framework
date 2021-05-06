using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public static class MapHelper
	{
		/// <summary>
		/// Active skyfallers in a map should prevent the map from being closed
		/// </summary>
		/// <param name="map"></param>
		public static bool AnyVehicleSkyfallersBlockingMap(Map map)
		{
			return map?.listerThings?.ThingsInGroup(ThingRequestGroup.ThingHolder)?.Where(t => t is VehicleSkyfaller)?.Any() ?? false;
		}

		/// <summary>
		/// Any active aerial vehicles currently providing recon on the map
		/// </summary>
		/// <param name="map"></param>
		public static bool AnyAerialVehiclesInRecon(Map map)
		{
			foreach (AerialVehicleInFlight aerialVehicle in Find.World.GetCachedWorldComponent<VehicleWorldObjectsHolder>().AerialVehicles)
			{
				if (aerialVehicle.flightPath.InRecon && aerialVehicle.flightPath.Last == map.Tile)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Vehicle is blocked at <paramref name="cell"/> and will not spawn correctly
		/// </summary>
		/// <param name="cell"></param>
		public static bool VehicleBlockedInPosition(VehiclePawn vehicle, Map map, IntVec3 cell, Rot4 rot)
		{
			IEnumerable<IntVec3> cells = vehicle.PawnOccupiedCells(cell, rot);
			return VehicleReservationManager.VehicleInhabitingCells(cells, map) || !vehicle.CellRectStandable(map, cell, rot);
		}

		/// <summary>
		/// Recon option for vehicle targeting
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="parent"></param>
		public static FloatMenuOption ReconFloatMenuOption(VehiclePawn vehicle, MapParent parent)
		{
			if (parent.EnterCooldownBlocksEntering())
			{
				return new FloatMenuOption($"{"AerialReconSite".Translate(parent.Label)} ({"EnterCooldownBlocksEntering".Translate()})", null);
			}
			return new FloatMenuOption("AerialReconSite".Translate(parent.Label), delegate ()
			{
				if (vehicle.Spawned)
				{
					vehicle.CompVehicleLauncher.TryLaunch(parent.Tile, null, true);
				}
				else
				{
					AerialVehicleInFlight aerial = Find.World.GetCachedWorldComponent<VehicleWorldObjectsHolder>().AerialVehicleObject(vehicle);
					if (aerial is null)
					{
						Log.Error($"Attempted to launch into existing map where CurrentMap is null and no AerialVehicle with {vehicle.Label} exists.");
						return;
					}
					List<int> flightPath = new List<int>(LaunchTargeter.FlightPath);
					aerial.OrderFlyToTiles(flightPath, aerial.DrawPos);
					aerial.flightPath.ReconCircleAt(parent.Tile);
					vehicle.inFlight = true;
				}
			});
		}

		/// <summary>
		/// Strafe option for combat aerial vehicles targeting open maps
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="parent"></param>
		public static FloatMenuOption StrafeFloatMenuOption(VehiclePawn vehicle, MapParent parent)
		{
			if (parent.EnterCooldownBlocksEntering())
			{
				return new FloatMenuOption($"{"AerialStrafeRun".Translate(parent.Label)} ({"EnterCooldownBlocksEntering".Translate()})", null);
			}
			return new FloatMenuOption("AerialStrafeRun".Translate(parent.Label), delegate ()
			{
				if (vehicle.Spawned)
				{
					vehicle.CompVehicleLauncher.TryLaunch(parent.Tile, null, true);
				}
				else
				{
					AerialVehicleInFlight aerial = Find.World.GetCachedWorldComponent<VehicleWorldObjectsHolder>().AerialVehicleObject(vehicle);
					if (aerial is null)
					{
						Log.Error($"Attempted to launch into existing map where CurrentMap is null and no AerialVehicle with {vehicle.Label} exists.");
						return;
					}
					List<int> flightPath = new List<int>(LaunchTargeter.FlightPath);
					aerial.OrderFlyToTiles(flightPath, aerial.DrawPos);
					aerial.flightPath.ReconCircleAt(parent.Tile);
					vehicle.inFlight = true;
				}
			});
		}
	}
}
