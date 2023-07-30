using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles
{
	public static class AerialVehicleTraderHelper
	{
		private static AerialVehicleInFlight aerialVehicle;

		private static readonly List<TransferableUIUtility.ExtraInfo> tmpInfo = new List<TransferableUIUtility.ExtraInfo>();

		private static readonly MethodInfo massUsagePropertyInfo = AccessTools.PropertyGetter(typeof(Dialog_Trade), "MassUsage");

		private static readonly List<Pair<float, Color>> MassColor = new List<Pair<float, Color>>
		{
			new Pair<float, Color>(0.37f, Color.green),
			new Pair<float, Color>(0.82f, Color.yellow),
			new Pair<float, Color>(1f, new Color(1f, 0.6f, 0f))
		};

		public static void SetupAerialVehicleTrade(ref List<Thing> playerCaravanAllPawnsAndItems)
		{
			AerialVehicleInFlight negotiatorsAerialVehicle = TradeSession.playerNegotiator?.GetAerialVehicle();
			aerialVehicle = negotiatorsAerialVehicle;
			if (aerialVehicle != null)
			{
				playerCaravanAllPawnsAndItems = new List<Thing>();
				foreach (Pawn pawn in aerialVehicle.vehicle.AllPawnsAboard)
				{
					playerCaravanAllPawnsAndItems.Add(pawn);
				}
				playerCaravanAllPawnsAndItems.AddRange(aerialVehicle.vehicle.inventory.innerContainer);
			}
		}

		public static float DrawAerialVehicleInfo(Dialog_Trade tradeDialog, Rect rect, bool lerpMassColor = true)
		{
			if (aerialVehicle != null)
			{
				tmpInfo.Clear();
				{
					//Mass Usage
					float massCapacity = aerialVehicle.vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity);
					float massUsage = (float)massUsagePropertyInfo.Invoke(tradeDialog, new object[] { });
					TaggedString massUsageReadout = $"{massUsage.ToStringEnsureThreshold(massCapacity, 0)} / {massCapacity:F0} {"kg".Translate()}";
					string massTip = GetMassTip(massUsage, massCapacity);
					tmpInfo.Add(new TransferableUIUtility.ExtraInfo("Mass".Translate(), massUsageReadout, GetMassColor(massUsage, massCapacity, lerpMassColor: lerpMassColor), massTip));

					//Flight Speed
					float flightSpeed = aerialVehicle.vehicle.GetStatValue(VehicleStatDefOf.FlightSpeed);
					//tmpInfo.Add(new TransferableUIUtility.ExtraInfo(""))

					TransferableUIUtility.DrawExtraInfo(tmpInfo, rect);
				}
				tmpInfo.Clear();

				return 52;
			}
			return 0;
		}

		private static string GetMassTip(float massUsage, float massCapacity)
		{
			TaggedString taggedString = "MassCarriedSimple".Translate() + ": " + massUsage.ToStringEnsureThreshold(massCapacity, 2) + " " + "kg".Translate() + "\n" + "MassCapacity".Translate() + ": " + massCapacity.ToString("F2") + " " + "kg".Translate();
			return taggedString;
		}

		private static Color GetMassColor(float massUsage, float massCapacity, bool lerpMassColor)
		{
			if (massCapacity == 0f)
			{
				return Color.white;
			}
			if (massUsage > massCapacity)
			{
				return Color.red;
			}
			if (lerpMassColor)
			{
				return GenUI.LerpColor(MassColor, massUsage / massCapacity);
			}
			return Color.white;
		}
	}
}
