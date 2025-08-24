using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Vehicles;

public class Command_TransferToVehicle_Order : Command_Target
{
	public static readonly Command_TransferToVehicle_Order Command = new()
	{
		Order = 1000f
	};

	private Command_TransferToVehicle_Order()
	{
		defaultLabel = "VF_TransferToVehicle_Order".Translate();
		defaultDesc = "VF_TransferToVehicle_Order_Desc".Translate();
		icon = VehicleTex.PackCargoIcon[(uint)VehicleType.Land];
		hotKey = KeyBindingDefOf.Misc2;
		action = Action;
		targetingParams = TargetingParameters.ForPawns();
		targetingParams.validator = IsValidVehicle;
		onUpdate = DrawLineTo;
	}

	private static bool IsValidVehicle(TargetInfo target)
	{
		if (target.Thing is not VehiclePawn vehicle)
			return false;

		if (vehicle.CompUpgradeTree is { Upgrading: true })
			return false;

		return vehicle.Faction == Faction.OfPlayer;
	}

	private static bool TransferableObject(object obj, out Thing thing)
	{
		thing = obj as Thing;
		return thing != null && thing.CanBeHauledToVehicle();
	}

	private static void DrawLineTo(LocalTargetInfo target)
	{
		if (!Find.Targeter.IsTargeting)
			return;

		foreach (object obj in Find.Selector.SelectedObjects)
		{
			if (!TransferableObject(obj, out Thing thing))
				return;
			Vector3 targetPos = target.Thing?.DrawPos ?? UI.MouseMapPosition();
			GenDraw.DrawLineBetween(thing.DrawPos, targetPos);
		}
	}

	public static IEnumerable<Thing> GetSelectedTransferableThings()
	{
		foreach (object obj in Find.Selector.SelectedObjects)
		{
			if (TransferableObject(obj, out Thing thing))
				yield return thing;
		}
	}

	private static void Action(LocalTargetInfo target)
	{
		if (target.Thing is VehiclePawn vehicle)
		{
			GetSelectedTransferableThings().TransferToVehicle(vehicle);
		}
	}
}