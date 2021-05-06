using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Vehicles
{
	public class AerialVehicleArrivalAction_CrashSpecificCell : AerialVehicleArrivalAction_LandSpecificCell
	{
		public AerialVehicleArrivalAction_CrashSpecificCell()
		{
		}

		public AerialVehicleArrivalAction_CrashSpecificCell(VehiclePawn vehicle, MapParent mapParent, int tile, LaunchProtocol launchProtocol, IntVec3 landingCell, Rot4 landingRot) : base(vehicle, mapParent, tile, launchProtocol, landingCell, landingRot)
		{
		}

		public override void Arrived(int tile)
		{
			VehicleSkyfaller_Crashing skyfaller = (VehicleSkyfaller_Crashing)ThingMaker.MakeThing(vehicle.CompVehicleLauncher.Props.skyfallerCrashing);
			skyfaller.vehicle = vehicle;
			skyfaller.launchProtocol = launchProtocol;
			skyfaller.rotCrashing = Rot4.East;
			GenSpawn.Spawn(skyfaller, landingCell, mapParent.Map, landingRot);
		}
	}
}
