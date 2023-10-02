using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles
{
	//Copy / paste of CaravanItemsTabUtility and CaravanThingsTabUtility. Must work with AerialVehicle but I don't want to stray away from vanilla UI for vanilla mechanics
	[StaticConstructorOnStartup]
	public static class AerialVehicleTabHelper
	{
		public const float MassColumnWidth = 60f;
		public const float SpaceAroundIcon = 4f;
		public const float SpecificTabButtonSize = 24f;
		public const float AbandonButtonSize = 24f;
		public const float AbandonSpecificCountButtonSize = 24f;

		private const float RowHeight = 30f;
		private const float LabelColumnWidth = 300f;

		public static void DoRows(Vector2 size, List<TransferableImmutable> things, AerialVehicleInFlight aerialVehicle, ref Vector2 scrollPosition, ref float scrollViewHeight)
		{
			Text.Font = GameFont.Small;
			Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
			Rect viewRect = new Rect(0f, 0f, rect.width - 16f, scrollViewHeight);
			Widgets.BeginScrollView(rect, ref scrollPosition, viewRect, true);
			float num = 0f;
			Widgets.ListSeparator(ref num, viewRect.width, "CaravanItems".Translate());
			if (things.Any())
			{
				for (int i = 0; i < things.Count; i++)
				{
					DoRow(ref num, viewRect, rect, scrollPosition, things[i], aerialVehicle);
				}
			}
			else
			{
				Widgets.NoneLabel(ref num, viewRect.width, null);
			}
			if (Event.current.type == EventType.Layout)
			{
				scrollViewHeight = num + RowHeight;
			}
			Widgets.EndScrollView();
		}

		public static Vector2 GetSize(List<TransferableImmutable> things, float paneTopY, bool doNeeds = true)
		{
			float num = LabelColumnWidth;
			num += 24f;
			num += 60f;
			Vector2 result;
			result.x = 103f + num + 16f;
			result.y = Mathf.Min(550f, paneTopY - RowHeight);
			return result;
		}

		private static void DoRow(ref float curY, Rect viewRect, Rect scrollOutRect, Vector2 scrollPosition, TransferableImmutable thing, AerialVehicleInFlight aerialVehicle)
		{
			float num = scrollPosition.y - RowHeight;
			float num2 = scrollPosition.y + scrollOutRect.height;
			if (curY > num && curY < num2)
			{
				DoRow(new Rect(0f, curY, viewRect.width, RowHeight), thing, aerialVehicle);
			}
			curY += RowHeight;
		}

		private static void DoRow(Rect rect, TransferableImmutable thing, AerialVehicleInFlight aerialVehicle)
		{
			Widgets.BeginGroup(rect);
			Rect rect2 = rect.AtZero();
			if (thing.TotalStackCount != 1)
			{
				DoAbandonSpecificCountButton(rect2, thing, aerialVehicle);
			}
			rect2.width -= 24f;
			DoAbandonButton(rect2, thing, aerialVehicle);
			rect2.width -= 24f;
			Widgets.InfoCardButton(rect2.width - 24f, (rect.height - 24f) / 2f, thing.AnyThing);
			rect2.width -= 24f;
			Rect rect3 = rect2;
			rect3.xMin = rect3.xMax - 60f;
			CaravanThingsTabUtility.DrawMass(thing, rect3);
			rect2.width -= 60f;
			Widgets.DrawHighlightIfMouseover(rect2);
			Rect rect4 = new Rect(4f, (rect.height - 27f) / 2f, 27f, 27f);
			Widgets.ThingIcon(rect4, thing.AnyThing, 1f, null, false);
			Rect rect5 = new Rect(rect4.xMax + 4f, 0f, LabelColumnWidth, RowHeight);
			Text.Anchor = TextAnchor.MiddleLeft;
			Text.WordWrap = false;
			Widgets.Label(rect5, thing.LabelCapWithTotalStackCount.Truncate(rect5.width, null));
			Text.Anchor = TextAnchor.UpperLeft;
			Text.WordWrap = true;
			Widgets.EndGroup();
		}

		public static void DoAbandonButton(Rect rowRect, Thing thing, AerialVehicleInFlight aerialVehicle)
		{
			Rect rect = new Rect(rowRect.width - 24f, (rowRect.height - 24f) / 2f, 24f, 24f);
			if (Widgets.ButtonImage(rect, CaravanThingsTabUtility.AbandonButtonTex))
			{
				AerialVehicleAbandonOrBanishHelper.TryAbandonOrBanishViaInterface(thing, aerialVehicle);
			}
			if (Mouse.IsOver(rect))
			{
				TooltipHandler.TipRegion(rect, () => AerialVehicleAbandonOrBanishHelper.GetAbandonOrBanishButtonTooltip(thing, false), Gen.HashCombineInt(thing.GetHashCode(), 1383004931));
			}
		}

		public static void DoAbandonButton(Rect rowRect, TransferableImmutable transferable, AerialVehicleInFlight aerialVehicle)
		{
			Rect rect = new Rect(rowRect.width - AbandonButtonSize, (rowRect.height - AbandonButtonSize) / 2f, AbandonButtonSize, AbandonButtonSize);
			if (Widgets.ButtonImage(rect, CaravanThingsTabUtility.AbandonButtonTex, true))
			{
				AerialVehicleAbandonOrBanishHelper.TryAbandonOrBanishViaInterface(transferable, aerialVehicle);
			}
			if (Mouse.IsOver(rect))
			{
				TooltipHandler.TipRegion(rect, () => AerialVehicleAbandonOrBanishHelper.GetAbandonOrBanishButtonTooltip(transferable, false), Gen.HashCombineInt(transferable.GetHashCode(), 8476546));
			}
		}

		public static void DoAbandonSpecificCountButton(Rect rowRect, Thing thing, AerialVehicleInFlight aerialVehicle)
		{
			Rect rect = new Rect(rowRect.width - AbandonSpecificCountButtonSize, (rowRect.height - AbandonSpecificCountButtonSize) / 2f, AbandonSpecificCountButtonSize, AbandonSpecificCountButtonSize);
			if (Widgets.ButtonImage(rect, CaravanThingsTabUtility.AbandonSpecificCountButtonTex, true))
			{
				AerialVehicleAbandonOrBanishHelper.TryAbandonSpecificCountViaInterface(thing, aerialVehicle);
			}
			if (Mouse.IsOver(rect))
			{
				TooltipHandler.TipRegion(rect, () => AerialVehicleAbandonOrBanishHelper.GetAbandonOrBanishButtonTooltip(thing, true), Gen.HashCombineInt(thing.GetHashCode(), 1163428609));
			}
		}

		public static void DoAbandonSpecificCountButton(Rect rowRect, TransferableImmutable transferable, AerialVehicleInFlight aerialVehicle)
		{
			Rect rect = new Rect(rowRect.width - AbandonButtonSize, (rowRect.height - AbandonButtonSize) / 2f, AbandonButtonSize, AbandonButtonSize);
			if (Widgets.ButtonImage(rect, CaravanThingsTabUtility.AbandonSpecificCountButtonTex, true))
			{
				AerialVehicleAbandonOrBanishHelper.TryAbandonSpecificCountViaInterface(transferable, aerialVehicle);
			}
			if (Mouse.IsOver(rect))
			{
				TooltipHandler.TipRegion(rect, () => AerialVehicleAbandonOrBanishHelper.GetAbandonOrBanishButtonTooltip(transferable, true), Gen.HashCombineInt(transferable.GetHashCode(), 1163428609));
			}
		}

		public static void DoOpenSpecificTabButton(Rect rowRect, Pawn p, ref Pawn specificTabForPawn)
		{
			Color baseColor = (p == specificTabForPawn) ? CaravanThingsTabUtility.OpenedSpecificTabButtonColor : Color.white;
			Color mouseoverColor = (p == specificTabForPawn) ? CaravanThingsTabUtility.OpenedSpecificTabButtonMouseoverColor : GenUI.MouseoverColor;
			Rect rect = new Rect(rowRect.width - SpecificTabButtonSize, (rowRect.height - SpecificTabButtonSize) / 2f, SpecificTabButtonSize, SpecificTabButtonSize);
			if (Widgets.ButtonImage(rect, CaravanThingsTabUtility.SpecificTabButtonTex, baseColor, mouseoverColor, true))
			{
				if (p == specificTabForPawn)
				{
					specificTabForPawn = null;
					SoundDefOf.TabClose.PlayOneShotOnCamera(null);
				}
				else
				{
					specificTabForPawn = p;
					SoundDefOf.TabOpen.PlayOneShotOnCamera(null);
				}
			}
			TooltipHandler.TipRegionByKey(rect, "OpenSpecificTabButtonTip");
			GUI.color = Color.white;
		}

		public static void DoOpenSpecificTabButtonInvisible(Rect rect, Pawn pawn, ref Pawn specificTabForPawn)
		{
			if (Widgets.ButtonInvisible(rect, true))
			{
				if (pawn == specificTabForPawn)
				{
					specificTabForPawn = null;
				}
				else
				{
					specificTabForPawn = pawn;
				}
				SoundDefOf.TabClose.PlayOneShotOnCamera(null);
			}
		}

		public static void DrawMass(TransferableImmutable transferable, Rect rect)
		{
			float num = 0f;
			for (int i = 0; i < transferable.things.Count; i++)
			{
				num += transferable.things[i].GetStatValue(StatDefOf.Mass, true, -1) * transferable.things[i].stackCount;
			}
			DrawMass(num, rect);
		}

		public static void DrawMass(Thing thing, Rect rect)
		{
			DrawMass(thing.GetStatValue(StatDefOf.Mass, true, -1) * thing.stackCount, rect);
		}

		private static void DrawMass(float mass, Rect rect)
		{
			GUI.color = TransferableOneWayWidget.ItemMassColor;
			Text.Anchor = TextAnchor.MiddleLeft;
			Text.WordWrap = false;
			Widgets.Label(rect, mass.ToStringMass());
			Text.WordWrap = true;
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
		}
	}
}
