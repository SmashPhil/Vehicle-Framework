using System.Collections.Generic;
using System.Text;
using CoreLib;
using RimWorld;
using SmashTools;
using Verse;

namespace Vehicles;

public class Alert_IdleInVehicle : Alert
{
	private readonly List<Pawn> idlePawns = [];

	private readonly StringBuilder explanation = new();

	public Alert_IdleInVehicle()
	{
		defaultLabel = "ColonistsIdle".Translate();
		defaultPriority = AlertPriority.High;
	}

	private List<Pawn> IdlePawns
	{
		get
		{
			idlePawns.Clear();
			foreach (Map map in Find.Maps)
			{
				if (!map.IsPlayerHome)
					continue;

				foreach (VehiclePawn vehicle in map.GetDetachedMapComponent<VehiclePositionManager>().AllClaimants)
				{
					if (vehicle.IdlePawnsInVehicle)
					{
						idlePawns.AddRange(vehicle.AllPawnsAboard);
						idlePawns.AddRange(vehicle.AllInventoryPawns);
					}
				}
			}
			return idlePawns;
		}
	}

	public override string GetLabel()
	{
		List<Pawn> pawns = IdlePawns;
		return pawns.Count == 1 ? "ColonistIdle".Translate() : "ColonistsIdle".Translate(pawns.Count.ToStringCached());
	}

	public override TaggedString GetExplanation()
	{
		using ClearStringOnDispose csod = new(explanation);
		foreach (Pawn pawn in IdlePawns)
		{
			explanation.AppendLine("  - " + pawn.NameShortColored.Resolve());
		}
		return "VF_IdleInVehicle".Translate(explanation.ToString().TrimEndNewlines());
	}

	public override AlertReport GetReport()
	{
		return AlertReport.CulpritsAre(IdlePawns);
	}
}