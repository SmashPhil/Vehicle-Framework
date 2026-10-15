using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace SmashTools.Patching;

internal class Patch_Rendering : IPatchCategory
{
	PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

	void IPatchCategory.PatchMethods()
	{
		// IThingHolderPawnOverlayer
		HarmonyPatcher.Patch(original: AccessTools.Method(typeof(PawnRenderer), "GetBodyPos"),
			transpiler: new HarmonyMethod(typeof(PawnOverlayRenderer),
				nameof(PawnOverlayRenderer.ShowBodyTranspiler)));
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.LayingFacing)),
			prefix: new HarmonyMethod(typeof(PawnOverlayRenderer),
				nameof(PawnOverlayRenderer.LayingFacing)));

		// Input handling
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(WindowStack), nameof(WindowStack.Add)),
			postfix: new HarmonyMethod(typeof(WindowEvents),
				nameof(WindowEvents.WindowAddedToStack)));
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(WindowStack), nameof(WindowStack.TryRemove),
				parameters: [typeof(Window), typeof(bool)]),
			postfix: new HarmonyMethod(typeof(WindowEvents),
				nameof(WindowEvents.WindowRemovedFromStack)));
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(WindowStack),
				nameof(WindowStack.HandleEventsHighPriority)),
			postfix: new HarmonyMethod(typeof(WindowEvents),
				nameof(WindowEvents.HighPriorityOnGUI)));

		// UI
		// NOTE - A few other mods patch DoInspectPaneButtons destructively, but inspectables don't need to show
		// other mods' pins right now. Just show Inspectable's and let those mods work with non-VF pawns.
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(MainTabWindow_Inspect),
				nameof(MainTabWindow_Inspect.DoInspectPaneButtons)),
			prefix: new HarmonyMethod(AccessTools.Method(typeof(Patch_Rendering),
				nameof(InspectablePaneButtons)), priority: Priority.First));
	}

	private static bool InspectablePaneButtons(Rect rect, ref float lineEndWidth)
	{
		if (Find.Selector.SingleSelectedThing is IInspectable inspectable)
		{
			lineEndWidth += 30;
			Widgets.InfoCardButton(rect.width - lineEndWidth, 0f, Find.Selector.SingleSelectedThing);
			lineEndWidth += inspectable.DoInspectPaneButtons(rect.width - lineEndWidth);
			return false;
		}
		return true;
	}
}