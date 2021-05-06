using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse;
using SmashTools;

namespace Vehicles
{
	public static class VehicleCaravanTicksPerMoveUtility
	{
		private const int MaxPawnTicksPerMove = 150;
		private const int DownedPawnMoveTicks = 450;
		public const float CellToTilesConversionRatio = 340f;
		public const int DefaultTicksPerMove = 3300;
		private const float MoveSpeedFactorAtZeroMass = 2f;

		public static int GetTicksPerMove(Caravan caravan, StringBuilder explanation = null)
		{
			if (caravan == null)
			{
				if (explanation != null)
				{
					AppendUsingDefaultTicksPerMoveInfo(explanation);
				}
				return 3300;
			}
			return GetTicksPerMove(new VehicleInfo(caravan), explanation);
		}

		public static int GetTicksPerMove(VehicleInfo caravanInfo, StringBuilder explanation = null)
		{
			return GetTicksPerMove(caravanInfo.vehicles, caravanInfo.massUsage, caravanInfo.massCapacity, explanation);
		}

		public static int GetTicksPerMove(List<VehiclePawn> pawns, float massUsage, float massCapacity, StringBuilder explanation = null)
		{
			if (pawns.NotNullAndAny())
			{
				if (explanation != null)
				{
					explanation.Append("CaravanMovementSpeedFull".Translate() + ":");
				}
				float num = 0f;
				for (int i = 0; i < pawns.Count; i++)
				{
					float num2 = (float)((pawns[i].Downed || pawns[i].CarriedByCaravan()) ? DownedPawnMoveTicks : pawns[i].TicksPerMoveCardinal);
					num2 = Mathf.Min(num2, MaxPawnTicksPerMove) * 340f;
					float num3 = 60000f / num2;
					if (explanation != null)
					{
						explanation.AppendLine();
						explanation.Append(string.Concat(new string[]
						{
							"  - ",
							pawns[i].LabelShortCap,
							": ",
							num3.ToString("0.#"),
							" "
						}) + "TilesPerDay".Translate());
						if (pawns[i].Downed)
						{
							explanation.Append(" (" + "DownedLower".Translate() + ")");
						}
						else if (pawns[i].CarriedByCaravan())
						{
							explanation.Append(" (" + "Carried".Translate() + ")");
						}
					}
					num += num2 / (float)pawns.Count;
				}
				float moveSpeedFactorFromMass = GetMoveSpeedFactorFromMass(massUsage, massCapacity);
				if (explanation != null)
				{
					float num4 = 60000f / num;
					explanation.AppendLine();
					explanation.Append("  " + "Average".Translate() + ": " + num4.ToString("0.#") + " " + "TilesPerDay".Translate());
					explanation.AppendLine();
					explanation.Append("  " + "MultiplierForCarriedMass".Translate(moveSpeedFactorFromMass.ToStringPercent()));
				}
				int num5 = Mathf.Max(Mathf.RoundToInt(num / moveSpeedFactorFromMass), 1);
				if (explanation != null)
				{
					float num6 = 60000f / (float)num5;
					explanation.AppendLine();
					explanation.Append("  " + "FinalCaravanPawnsMovementSpeed".Translate() + ": " + num6.ToString("0.#") + " " + "TilesPerDay".Translate());
				}
				return num5;
			}
			if (explanation != null)
			{
				AppendUsingDefaultTicksPerMoveInfo(explanation);
			}
			return 3300;
		}

		private static float GetMoveSpeedFactorFromMass(float massUsage, float massCapacity)
		{
			if (massCapacity <= 0f)
			{
				return 1f;
			}
			float t = massUsage / massCapacity;
			return Mathf.Lerp(MoveSpeedFactorAtZeroMass, 1f, t);
		}

		private static void AppendUsingDefaultTicksPerMoveInfo(StringBuilder sb)
		{
			sb.Append("CaravanMovementSpeedFull".Translate() + ":");
			float num = 18.181818f;
			sb.AppendLine();
			sb.Append("  " + "Default".Translate() + ": " + num.ToString("0.#") + " " + "TilesPerDay".Translate());
		}

		public struct VehicleInfo
		{
			public VehicleInfo(List<VehiclePawn> vehicles)
			{
				this.vehicles = vehicles;
				massUsage = vehicles.Sum(v => MassUtility.GearAndInventoryMass(v));
				massCapacity = vehicles.Sum(v => v.CargoCapacity);
			}

			public VehicleInfo(Caravan caravan)
			{
				vehicles = caravan.PawnsListForReading.Where(v => v is VehiclePawn).Cast<VehiclePawn>().ToList();
				massUsage = caravan.MassUsage;
				massCapacity = caravan.MassCapacity;
			}

			public VehicleInfo(Dialog_FormVehicleCaravan formCaravanDialog)
			{
				vehicles = TransferableUtility.GetPawnsFromTransferables(formCaravanDialog.transferables).Where(v => v is VehiclePawn).Cast<VehiclePawn>().ToList();
				massUsage = formCaravanDialog.MassUsage;
				massCapacity = formCaravanDialog.MassCapacity;
			}

			public List<VehiclePawn> vehicles;
			public float massUsage;
			public float massCapacity;
		}
	}
}
