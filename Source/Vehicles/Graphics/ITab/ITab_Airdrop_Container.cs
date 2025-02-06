using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using SmashTools;

namespace Vehicles
{
	public class ITab_Airdrop_Container : ITab
	{
		protected const float TopPadding = 20f;
		protected const float ThingIconSize = 28f;
		protected const float ThingRowHeight = 28f;
		protected const float ThingDropButtonSize = 24f;
		protected const float ThingLeftX = 36f;
		protected const float StandardLineHeight = 22f;

		protected Vector2 scrollPosition = Vector2.zero;
		protected float scrollViewHeight;

		public static readonly Color ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);
		public static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);
		public static readonly Color MissingItemColor = new Color(1, 0, 0.1f, 0.75f);

		protected static List<Thing> workingInvList = new List<Thing>();

		public ITab_Airdrop_Container()
		{
			size = new Vector2(300f, 480f);
			labelKey = "TabStorage";
			tutorTag = "Storage";
		}

		protected virtual string InventoryLabelKey => "TabStorage";

		public override bool IsVisible => Inventory != null;

		protected virtual bool AllowDropping => false;

		protected virtual ThingOwner Inventory
		{
			get
			{
				if (SelThing is Pawn pawn)
				{
					return pawn.inventory.innerContainer;
				}
				if (SelThing is IThingHolder thingHolder)
				{
					return thingHolder.GetDirectlyHeldThings();
				}
				return null;
			}
		}

		protected override void FillTab()
		{
			using var textBlock = new TextBlock(GameFont.Small);
			Text.Font = GameFont.Small;
			Rect rect = new Rect(0f, TopPadding, size.x, size.y - TopPadding);
			Rect rect2 = rect.ContractedBy(10f);
			Rect position = new Rect(rect2.x, rect2.y, rect2.width, rect2.height);

			GUI.color = Color.white;
			Rect outRect = new Rect(0f, 0f, position.width, position.height);
			Rect viewRect = new Rect(0f, 0f, position.width - 16f, scrollViewHeight);

			// Start Scrollview
			Widgets.BeginGroup(position);
			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, true);

			float curY = 0f;
			DrawHeader(ref curY, viewRect.width);

			if (IsVisible)
			{
				Widgets.ListSeparator(ref curY, viewRect.width, InventoryLabelKey.Translate());
				workingInvList.Clear();
				workingInvList.AddRange(Inventory);
				foreach (Thing t in workingInvList)
				{
					DrawThingRow(ref curY, viewRect.width, t, null, true);
				}
				workingInvList.Clear();
			}
			if (IsVisible)
			{
				DrawAdditionalRows(ref curY, viewRect);
			}

			if (Event.current.type is EventType.Layout)
			{
				scrollViewHeight = curY + 30f;
			}

			Widgets.EndScrollView();
			Widgets.EndGroup();
			// End Scrollview
		}

		protected virtual void DrawThingRow(ref float y, float width, Thing thing, int? transferStackCount = null, bool inventory = false, bool missingFromInventory = false)
		{
			Rect rect = new Rect(0f, y, width, ThingIconSize);

			using (new TextBlock(Color.white))
			{
				if (missingFromInventory)
				{
					GUI.color = MissingItemColor;
				}
				Widgets.InfoCardButton(rect.width - 24f, y, thing);
				rect.width -= 24f;

				if (inventory && AllowDropping && SelThing.Spawned)
				{
					Rect rectDrop = new Rect(rect.xMax - ThingDropButtonSize, y, ThingDropButtonSize, ThingDropButtonSize);
					TooltipHandler.TipRegion(rectDrop, "DropThing".Translate());
					if (Widgets.ButtonImage(rectDrop, VehicleTex.Drop))
					{
						SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
						InterfaceDrop(thing);
					}
					rect.width -= 24f;
				}

				Rect massRect = rect;
				massRect.xMin = massRect.xMax - 60f;
				CaravanThingsTabUtility.DrawMass(thing, massRect);
				rect.width -= 60f;
			}

			using (new TextBlock(Color.white))
			{
				if (Mouse.IsOver(rect))
				{
					GUI.color = HighlightColor;
					GUI.DrawTexture(rect, TexUI.HighlightTex);
				}
				if (!(thing.def.DrawMatSingle is null) && !(thing.def.DrawMatSingle.mainTexture is null))
				{
					Widgets.ThingIcon(new Rect(4f, y, ThingIconSize, ThingRowHeight), thing, 1f);
				}

				Text.Anchor = TextAnchor.MiddleLeft;
				if (!missingFromInventory)
				{
					GUI.color = ThingLabelColor;
				}
				else
				{
					GUI.color = MissingItemColor;
				}
				Rect rect3 = new Rect(ThingLeftX, y, rect.width - ThingLeftX, rect.height);
				string text = string.Empty;
				if (transferStackCount != null)
				{
					text = thing.LabelCapNoCount + " x" + transferStackCount.Value.ToStringCached();
				}
				else
				{
					text = thing.LabelCap;
				}
				Text.WordWrap = false;
				Widgets.Label(rect3, text.Truncate(rect3.width, null));
				Text.WordWrap = true;
				string text2 = thing.DescriptionDetailed;
				if (thing.def.useHitPoints)
				{
					string text3 = text2;
					text2 = string.Concat(new object[]
					{
				text3, "\n", thing.HitPoints, " / ", thing.MaxHitPoints
					});
				}
				TooltipHandler.TipRegion(rect, text2);
				y += ThingRowHeight;
			}
		}

		protected virtual void DrawAdditionalRows(ref float y, Rect rect)
		{
		}

		protected virtual void DrawHeader(ref float curY, float width)
		{
		}

		protected virtual bool InterfaceDrop(Thing thing)
		{
			return Inventory.TryDropOutsideVehicle(thing, SelThing.Map, SelThing.OccupiedRect());
		}

		protected virtual bool InterfaceDropAll()
		{
			return Inventory.TryDropAllOutsideVehicle(SelThing.Map, SelThing.OccupiedRect());
		}
	}
}
