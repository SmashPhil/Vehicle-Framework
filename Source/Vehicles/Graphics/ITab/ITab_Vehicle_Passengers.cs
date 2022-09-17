using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;
using SmashTools;

namespace Vehicles
{
	public class ITab_Vehicle_Passengers : ITab
	{
		public const float PawnRowHeight = 50;

		private static List<Need> tmpNeeds = new List<Need>();

		private Vector2 scrollPosition;
		private Vector2 thoughtScrollPosition;
		private float scrollViewHeight;

		private VehicleHandler editingPawnOverlayRenderer;
		private Pawn specificNeedsTabForPawn;
		private Pawn draggedPawn;
		private VehicleHandler transferToHandler;
		private Pawn hoveringOverPawn;

		public ITab_Vehicle_Passengers()
		{
			size = new Vector2(520f, 450f);
			labelKey = "TabPassengers";
		}

		public VehiclePawn Vehicle => SelPawn as VehiclePawn;

		private float SpecificNeedsTabWidth => specificNeedsTabForPawn.DestroyedOrNull() ? 0f : NeedsCardUtility.GetSize(specificNeedsTabForPawn).x;

		public override bool IsVisible
		{
			get
			{
				return !Vehicle.beached;
			}
		}

		private List<Pawn> Passengers
		{
			get
			{
				return Vehicle.Passengers;
			}
		}

		private List<Pawn> AllAboard
		{
			get
			{
				return Vehicle.AllPawnsAboard;
			}
		}

		private List<VehicleHandler> Handlers
		{
			get
			{
				return Vehicle.handlers;
			}
		}
		
		protected override void FillTab()
		{
			GUIState.Push();

			EnsureSpecificNeedsTabForPawnValid();
			
			Text.Font = GameFont.Small;

			Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
			Rect viewRect = new Rect(0f, 0f, rect.width - 16f, scrollViewHeight);

			Widgets.BeginScrollView(rect, ref scrollPosition, viewRect, true);
			{
				float num = 0f;
				bool flag = false;

				bool overHandler = false;
				for (int i = 0; i < Handlers.Count; i++)
				{
					VehicleHandler handler = Handlers[i];
					List<Pawn> pawns = handler.handlers.InnerListForReading;
					Rect handlerRect = new Rect(0, num, viewRect.width - 48, 25f + (PawnRowHeight * pawns.Count));
					if (draggedPawn != null && Mouse.IsOver(handlerRect))
					{
						transferToHandler = handler;
						overHandler = true;
						Widgets.DrawHighlight(handlerRect);
					}
					Rect editPawnOverlayRect = new Rect(viewRect.width - 15, num + 3, 15, 15);
					Widgets.ListSeparator(ref num, viewRect.width, handler.role.label);
					if (handler.role.pawnRenderer != null && Prefs.DevMode)
					{
						TooltipHandler.TipRegionByKey(editPawnOverlayRect, "VF_EditPawnOverlayRendererTooltip");
						Color baseColor = (editingPawnOverlayRenderer != handler) ? Color.white : Color.green;
						Color mouseoverColor = (editingPawnOverlayRenderer != handler) ? GenUI.MouseoverColor : new Color(0f, 0.5f, 0f);
						if (false && Widgets.ButtonImage(editPawnOverlayRect, VehicleTex.Settings, baseColor, mouseoverColor)) //TEMP DISABLED UNTIL UI FIXED
						{
							if (editingPawnOverlayRenderer == null || editingPawnOverlayRenderer != handler)
							{
								SoundDefOf.TabOpen.PlayOneShotOnCamera(null);
								editingPawnOverlayRenderer = handler;
							}
							else
							{
								SoundDefOf.TabClose.PlayOneShotOnCamera(null);
								editingPawnOverlayRenderer = null;
							}
						}
						GUIState.Reset();
					}
					foreach (Pawn pawn in pawns)
					{
						if (DoRow(num, viewRect, rect, pawn, ref specificNeedsTabForPawn, draggedPawn == null))
						{
							hoveringOverPawn = pawn;
						}
						num += PawnRowHeight;
					}
				}

				if (!overHandler)
				{
					transferToHandler = null;
				}

				if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
				{
					if (draggedPawn != null && transferToHandler != null)
					{
						if (!transferToHandler.AreSlotsAvailable)
						{
							if (hoveringOverPawn != null && draggedPawn.ParentHolder is VehicleHandler curHandler && curHandler != transferToHandler && transferToHandler.CanOperateRole(draggedPawn) && curHandler.CanOperateRole(hoveringOverPawn))
							{
								Vehicle.EventRegistry[VehicleEventDefOf.PawnChangedSeats].ExecuteEvents();
								curHandler.handlers.Swap(transferToHandler.handlers, draggedPawn, hoveringOverPawn);
							}
							else
							{
								Messages.Message(TranslatorFormattedStringExtensions.Translate("Vehicles_HandlerNotEnoughRoom", transferToHandler.role.label, draggedPawn), MessageTypeDefOf.RejectInput);
							}
						}
						else if (draggedPawn.ParentHolder is VehicleHandler curHandler && curHandler != transferToHandler)
						{
							if (transferToHandler.handlers.TryAddOrTransfer(draggedPawn, false))
							{
								Vehicle.EventRegistry[VehicleEventDefOf.PawnChangedSeats].ExecuteEvents();
							}
							else
							{
								Messages.Message($"Unable to add {draggedPawn} to {transferToHandler.role.label}.", MessageTypeDefOf.RejectInput);
							}
						}
					}
					draggedPawn = null;
				}

				foreach (Pawn pawn in Passengers)
				{
					if (!pawn.IsColonist)
					{
						if (!flag)
						{
							Widgets.ListSeparator(ref num, viewRect.width, "CaravanPrisonersAndAnimals".Translate());
							flag = true;
						}
						if (DoRow(num, viewRect, rect, pawn, ref specificNeedsTabForPawn, true))
						{
							hoveringOverPawn = pawn;
						}
						num += PawnRowHeight;
					}
				}
				if (Event.current.type is EventType.Layout)
				{
					scrollViewHeight = num + 30f;
				}
			}
			Widgets.EndScrollView();

			GUIState.Pop();
		}

		private bool DoRow(float curY, Rect viewRect, Rect scrollOutRect, Pawn pawn, ref Pawn specificNeedsTabForPawn, bool highlight)
		{
			float minY = scrollPosition.y - PawnRowHeight;
			float maxY = scrollPosition.y + scrollOutRect.height;
			bool isDraggingPawn = pawn == draggedPawn;
			if (!isDraggingPawn && (curY <= minY || curY >= maxY))
			{
				return false;
			}

			float nonRefY = isDraggingPawn ? (Event.current.mousePosition.y - PawnRowHeight / 2) : curY;
			Rect rect = new Rect(0f, nonRefY, viewRect.width, PawnRowHeight);

			Widgets.BeginGroup(rect);

			Rect fullRect = rect.AtZero();
			Rect rowRect = new Rect(0, 0, fullRect.width - 48, PawnRowHeight);
			bool mouseOver = Mouse.IsOver(rowRect);
			if (draggedPawn == null && mouseOver && Event.current.type == EventType.MouseDown && Event.current.button == 0)
			{
				draggedPawn = pawn;
				Event.current.Use();
				SoundDefOf.Click.PlayOneShotOnCamera(null);
			}
			Widgets.InfoCardButton(fullRect.width - 24f, (rect.height - 24f) / 2f, pawn);
			fullRect.width -= 24f;
			if (!pawn.Dead)
			{
				OpenSpecificTabButton(fullRect, pawn, ref specificNeedsTabForPawn);
				fullRect.width -= 24f;
			}
			if (highlight)
			{
				Widgets.DrawHighlightIfMouseover(fullRect);
			}
			Rect rect3 = new Rect(4f, (rect.height - 27f) / 2f, 27f, 27f);
			Widgets.ThingIcon(rect3, pawn, 1f);
			Rect bgRect = new Rect(rect3.xMax + 4f, 16f, 100f, 18f);
			GenMapUI.DrawPawnLabel(pawn, bgRect, 1f, 100f, null, GameFont.Small, false, false);

			tmpNeeds.Clear();
			List<Need> allNeeds = pawn.needs.AllNeeds;
			foreach (Need n in allNeeds)
			{
				if (n.def.showForCaravanMembers) // Change for all needs?
				{
					tmpNeeds.Add(n);
				}
			}
			PawnNeedsUIUtility.SortInDisplayOrder(tmpNeeds);

			float xMax = bgRect.xMax;
			foreach (Need need in tmpNeeds)
			{
				int maxThresholdMarkers = 0;
				bool doTooltip = true;
				Rect rect4 = new Rect(xMax, 0f, 100f, PawnRowHeight);
				if (need is Need_Mood mood)
				{
					maxThresholdMarkers = 1;
					doTooltip = false;
					//TooltipHandler.TipRegion(rect4, new TipSignal(() => CaravanNeedsTabUtility.CustomMoodNeed)) //Add better way to make stringbuilder
				}
				need.DrawOnGUI(rect4, maxThresholdMarkers, 10f, false, doTooltip);
				xMax = rect4.xMax;
			}
			
			if (pawn.Downed)
			{
				GUI.color = new Color(1f, 0f, 0f, 0.5f);
				Widgets.DrawLineHorizontal(0f, rect.height / 2f, rect.width);
				GUI.color = Color.white;
			}

			Widgets.EndGroup();
			return mouseOver && !isDraggingPawn;
		}

		private static void OpenSpecificTabButton(Rect rowRect, Pawn p, ref Pawn specificTabForPawn)
		{
			GUIState.Push();
			Color baseColor = (p != specificTabForPawn) ? Color.white : Color.green;
			Color mouseoverColor = (p != specificTabForPawn) ? GenUI.MouseoverColor : new Color(0f, 0.5f, 0f);
			Rect rect = new Rect(rowRect.width - 24f, (rowRect.height - 24f) / 2f, 24f, 24f);
			
			if (Widgets.ButtonImage(rect, CaravanThingsTabUtility.SpecificTabButtonTex, baseColor, mouseoverColor))
			{
				if(p == specificTabForPawn)
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
			TooltipHandler.TipRegion(rect, "OpenSpecificTabButtonTip".Translate());
			GUIState.Pop();
		}

		protected override void UpdateSize()
		{
			EnsureSpecificNeedsTabForPawnValid();
			base.UpdateSize();

			size = GetSize(AllAboard, PaneTopY, true);
			size.y = Mathf.Max(size.y, NeedsCardUtility.FullSize.y);
		}

		private static Vector2 GetSize(List<Pawn> pawns, float paneTopY, bool doNeeds = true)
		{
			float num = 100f;
			if (doNeeds)
			{
				num += MaxNeedsCount(pawns) * 100f;
			}
			num += 24f;
			Vector2 result;
			result.x = 103f + num + 16f;
			result.y = Mathf.Min(550f, paneTopY - 30f);
			return result;
		}

		private static int MaxNeedsCount(List<Pawn> pawns)
		{
			int num = 0;
			foreach(Pawn p in pawns)
			{
				List<Need> pawnNeeds = new List<Need>();
				foreach (Need need in p.needs.AllNeeds)
				{
					if(need.def.showForCaravanMembers)
					{
						pawnNeeds.Add(need);
					}
				}
				num = Mathf.Max(num, pawnNeeds.Count);
			}
			return num;
		}

		protected override void ExtraOnGUI()
		{
			EnsureSpecificNeedsTabForPawnValid();
			base.ExtraOnGUI();
			if (specificNeedsTabForPawn != null)
			{
				Rect tabRect = TabRect;
				float specificNeedsTabWidth = SpecificNeedsTabWidth;
				Rect rect = new Rect(tabRect.xMax - 1f, tabRect.yMin, specificNeedsTabWidth, tabRect.height);
				Find.WindowStack.ImmediateWindow(1439870015, rect, WindowLayer.GameUI, delegate
				{
					if (specificNeedsTabForPawn.DestroyedOrNull())
					{
						return;
					}
					NeedsCardUtility.DoNeedsMoodAndThoughts(rect.AtZero(), specificNeedsTabForPawn, ref thoughtScrollPosition);
					if (Widgets.CloseButtonFor(rect.AtZero()))
					{
						specificNeedsTabForPawn = null;
						SoundDefOf.TabClose.PlayOneShotOnCamera(null);
					}
				}, true, false, 1f);
			}
			else if (editingPawnOverlayRenderer != null)
			{
				Rect pawnOverlayRect = new Rect(size.x + 1, TabRect.yMin, 600, size.y);
				Find.WindowStack.ImmediateWindow(editingPawnOverlayRenderer.role.GetHashCode() ^ Vehicle.GetHashCode(), pawnOverlayRect, WindowLayer.GameUI, delegate ()
				{
					if (editingPawnOverlayRenderer is null || editingPawnOverlayRenderer.vehicle.DestroyedOrNull())
					{
						return;
					}
					Rect rendererRect = new Rect(0, 0, pawnOverlayRect.width, pawnOverlayRect.height).ContractedBy(5);
					editingPawnOverlayRenderer.role.pawnRenderer.RenderEditor(rendererRect);
					if (Widgets.CloseButtonFor(rendererRect))
					{
						editingPawnOverlayRenderer = null;
						SoundDefOf.TabClose.PlayOneShotOnCamera(null);
					}
				});
			}
		}

		public override void Notify_ClearingAllMapsMemory()
		{
			base.Notify_ClearingAllMapsMemory();
			specificNeedsTabForPawn = null;
		}

		private void EnsureSpecificNeedsTabForPawnValid()
		{
			if (specificNeedsTabForPawn != null && (specificNeedsTabForPawn.Destroyed || !AllAboard.Contains(specificNeedsTabForPawn)))
			{
				specificNeedsTabForPawn = null;
			}
			if (editingPawnOverlayRenderer != null && (specificNeedsTabForPawn != null || editingPawnOverlayRenderer.handlers.Count == 0))
			{
				editingPawnOverlayRenderer = null;
			}
		}
	}
}