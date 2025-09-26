using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Vehicles;

public class ScenPart_StartingVehicle : ScenPart
{
	private VehicleDef vehicleDef;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Defs.Look(ref vehicleDef, nameof(vehicleDef));
	}

	public override void Randomize()
	{
		vehicleDef = DefDatabase<VehicleDef>.AllDefsListForReading.RandomElement();
	}

	public override void DoEditInterface(Listing_ScenEdit listing)
	{
		Rect scenPartRect = listing.GetScenPartRect(this, 2f * RowHeight + 4f);

		Rect buttonRect = new(scenPartRect.xMin, scenPartRect.yMin, scenPartRect.width, RowHeight);
		string label = vehicleDef?.LabelCap ?? "VF_RandomVehicle".Translate();
		if (Widgets.ButtonText(buttonRect, label))
		{
			List<FloatMenuOption> options = [];
			options.Add(new FloatMenuOption("VF_RandomVehicle".Translate().CapitalizeFirst(),
				delegate { vehicleDef = null; }));
			foreach (VehicleDef vehicleDefOpt in DefDatabase<VehicleDef>.AllDefsListForReading)
			{
				options.Add(new FloatMenuOption(vehicleDefOpt.LabelCap, delegate { vehicleDef = vehicleDefOpt; }));
			}
			Find.WindowStack.Add(new FloatMenu(options));
		}
	}

	public override string Summary(Scenario scen)
	{
		return ScenSummaryList.SummaryWithList(scen, "PlayerStartsWith",
			ScenPart_StartingThing_Defined.PlayerStartWithIntro);
	}

	public override IEnumerable<string> GetSummaryListEntries(string tag)
	{
		if (tag == "PlayerStartsWith")
		{
			yield return vehicleDef.LabelCap;
		}
	}

	public override IEnumerable<Thing> PlayerStartingThings()
	{
		if (vehicleDef == null)
		{
			Randomize();
		}
		VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(vehicleDef, Faction.OfPlayer);
		yield return vehicle;
	}

	public override int GetHashCode()
	{
		// ReSharper disable once NonReadonlyMemberInGetHashCode
		return base.GetHashCode() ^ (vehicleDef?.GetHashCode() ?? 0);
	}
}