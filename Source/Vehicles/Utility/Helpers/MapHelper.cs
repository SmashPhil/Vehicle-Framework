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
			List<Thing> thingHolders = map?.listerThings?.ThingsInGroup(ThingRequestGroup.ThingHolder);
			if (thingHolders.NullOrEmpty())
			{
				return false;
			}
			foreach (Thing thing in thingHolders)
			{
				if (thing is VehicleSkyfaller)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Any active aerial vehicles currently providing recon on the map
		/// </summary>
		/// <param name="map"></param>
		public static bool AnyAerialVehiclesInRecon(Map map)
		{
			foreach (AerialVehicleInFlight aerialVehicle in VehicleWorldObjectsHolder.Instance.AerialVehicles)
			{
				if (aerialVehicle.flightPath.InRecon && aerialVehicle.flightPath.Last.tile == map.Tile)
				{
					return true; //Keep open while performing recon
				}
				if (aerialVehicle.arrivalAction != null && aerialVehicle.flightPath.Last.tile == map.Tile)
				{
					return true; //Keep open if aerial vehicle has active arrival action on tile
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
			return VehicleReservationManager.AnyVehicleInhabitingCells(vehicle.PawnOccupiedCells(cell, rot), map) || !vehicle.CellRectStandable(map, cell, rot);
		}

		/// <summary>
		/// Vehicle that has reserved <paramref name="cell"/>
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="map"></param>
		/// <param name="cell"></param>
		/// <param name="rot"></param>
		public static VehiclePawn VehicleInPosition(VehiclePawn vehicle, Map map, IntVec3 cell, Rot4 rot)
		{
			IEnumerable<IntVec3> cells = vehicle.PawnOccupiedCells(cell, rot);
			return VehicleReservationManager.VehicleInhabitingCells(cells, map);
		}

		public static VehicleSkyfaller VehicleSkyfallerInPosition(VehiclePawn vehicle, Map map, IntVec3 cell, Rot4 rot)
		{
			IEnumerable<IntVec3> cells = vehicle.PawnOccupiedCells(cell, rot);
			foreach (IntVec3 hitboxCell in cells)
			{
				VehicleSkyfaller vehicleSkyfaller = map.thingGrid.ThingAt<VehicleSkyfaller>(hitboxCell);
				if (vehicleSkyfaller != null)
				{
					return vehicleSkyfaller;
				}
			}
			return null;
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
					AerialVehicleInFlight aerial = AerialVehicleLaunchHelper.GetOrMakeAerialVehicle(vehicle);
					if (aerial is null)
					{
						Log.Error($"Attempted to launch into existing map where CurrentMap is null and no AerialVehicle with {vehicle.Label} exists.");
						return;
					}
					List<FlightNode> flightPath = new List<FlightNode>(LaunchTargeter.FlightPath);
					aerial.OrderFlyToTiles(flightPath, aerial.DrawPos);
					aerial.flightPath.ReconCircleAt(parent.Tile);
					vehicle.CompVehicleLauncher.inFlight = true;
				}
			});
		}
	}
}
