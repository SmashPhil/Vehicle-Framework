using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles
{
	public static class AerialVehicleTraderHelper
	{
		private static bool playerIsInAerialVehicle = false;

		public static void SetupAerialVehicleTrade(ref List<Thing> playerCaravanAllPawnsAndItems)
		{
			playerIsInAerialVehicle = false;
			AerialVehicleInFlight aerialVehicle = TradeSession.playerNegotiator?.GetAerialVehicle();
			if (aerialVehicle != null)
			{
				playerIsInAerialVehicle = true;
				playerCaravanAllPawnsAndItems = new List<Thing>();
				foreach (Pawn pawn in aerialVehicle.vehicle.AllPawnsAboard)
				{
					playerCaravanAllPawnsAndItems.Add(pawn);
				}
				playerCaravanAllPawnsAndItems.AddRange(aerialVehicle.vehicle.inventory.innerContainer);
			}
		}

		public static void DrawAerialVehicleInfo(ref Rect rect)
		{
			if (playerIsInAerialVehicle)
			{
				rect.yMin += 52;
			}
		}
	}
}
