using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles
{
	public abstract class WITab_AerialVehicle : WITab
	{
		protected AerialVehicleInFlight SelAerialVehicle => SelObject as AerialVehicleInFlight;

		protected List<Pawn> Pawns
		{
			get
			{
				return SelAerialVehicle.vehicle.AllPawnsAboard;
			}
		}
	}
}
