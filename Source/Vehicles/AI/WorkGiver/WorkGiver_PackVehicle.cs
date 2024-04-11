using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	public class WorkGiver_PackVehicle : WorkGiver_CarryToVehicle
	{
		public override ThingOwner<Thing> ThingOwner(VehiclePawn vehicle)
		{
			return vehicle.inventory.innerContainer;
		}

		public override List<TransferableOneWay> Transferables(VehiclePawn vehicle)
		{
			return vehicle.cargoToLoad;
		}
	}
}
