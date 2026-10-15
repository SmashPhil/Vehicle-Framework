using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace SmashTools.Patching;

internal class Compatibility_RimHUD : IConditionalPatch
{
	string IConditionalPatch.PackageId => "Jaxe.RimHUD";

	string IConditionalPatch.SourceId => ProjectSetup.HarmonyId;

	PatchSequence IConditionalPatch.PatchAt => PatchSequence.Async;

	void IConditionalPatch.PatchAll(ModMetaData mod)
	{
		Type classType = AccessTools.TypeByName("RimHUD.Interface.Screen.InspectPaneButtons");
		HarmonyPatcher.Patch(AccessTools.Method(classType, "Draw"),
			postfix: new HarmonyMethod(typeof(Compatibility_RimHUD),
				nameof(DrawButtonsOnRimHUD)));
	}

	public static void DrawButtonsOnRimHUD(Rect bounds, IInspectPane pane, ref float offset)
	{
		if (Find.Selector.SingleSelectedThing is IInspectable inspectable)
		{
			offset += inspectable.DoInspectPaneButtons(bounds.width - offset);
		}
	}
}