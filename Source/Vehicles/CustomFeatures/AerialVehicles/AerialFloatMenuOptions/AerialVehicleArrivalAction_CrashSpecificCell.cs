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

		public AerialVehicleArrivalAction_CrashSpecificCell(VehiclePawn vehicle, MapParent mapParent, 
																												int tile, IntVec3 landingCell, Rot4 landingRot) 
																												: base(vehicle, mapParent, tile, landingCell, landingRot)
		{
		}

		protected override void SpawnSkyfaller()
		{
			VehicleSkyfaller_Crashing skyfaller = (VehicleSkyfaller_Crashing)ThingMaker.MakeThing(vehicle.CompVehicleLauncher.Props.skyfallerCrashing);
			skyfaller.vehicle = vehicle;
			skyfaller.rotCrashing = Rot4.East;
			GenSpawn.Spawn(skyfaller, landingCell, mapParent.Map, landingRot);
		}

		protected override void ExecuteEvents()
		{
			vehicle.EventRegistry[VehicleEventDefOf.AerialVehicleCrashLanding].ExecuteEvents();
		}
	}
}
