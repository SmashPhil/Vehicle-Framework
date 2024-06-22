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
	public static class VehicleTabHelper_Passenger
	{
		public const float PawnRowHeight = 50;
		public const float PawnRowPadding = 4;
		public const float ThingIconSize = 27;
		public const float PawnExtraButtonSize = 24;
		public const float LabelWidth = 100;

		private static readonly List<Need> tmpNeeds = new List<Need>();

		private static VehicleHandler editingPawnOverlayRenderer;
		private static Pawn draggedPawn;
		private static IThingHolder transferToHolder;
		private static Pawn hoveringOverPawn;

		private static bool overDropSpot = false;

		public static void Start()
		{
			overDropSpot = false;
		}

		public static void End()
		{
			HandleDragEvent();

			if (!overDropSpot)
			{
				transferToHolder = null;
			}
		}

		/// <summary>
		/// Lists all pawns inside <paramref name="vehicle"/>
		/// </summary>
		/// <param name="viewRect"></param>
		/// <param name="scrollPos"></param>
		/// <param name="vehicle"></param>
		/// <param name="specificNeedsTabForPawn"></param>
		/// <returns>Height used up for list</returns>
		public static void DrawPassengersFor(ref float curY, Rect viewRect, Vector2 scrollPos, VehiclePawn vehicle, ref Pawn moreDetailsForPawn)
		{
			GUIState.Push();
			{
				for (int i = 0; i < vehicle.handlers.Count; i++)
				{
					VehicleHandler handler = vehicle.handlers[i];
					List<Pawn> pawns = handler.handlers.InnerListForReading;

					overDropSpot |= ListPawns(ref curY, viewRect, scrollPos, handler, handler.role.label, pawns, ref moreDetailsForPawn);
				}

				//List out all prisoners, and animals
				bool animalsSeparated = false;
				foreach (Pawn pawn in vehicle.Passengers.Where(pawn => !pawn.IsColonist))
				{
					if (!animalsSeparated)
					{
						Widgets.ListSeparator(ref curY, viewRect.width, "CaravanPrisonersAndAnimals".Translate());
						animalsSeparated = true;
					}
					if (DoRow(curY, viewRect, scrollPos, pawn, ref moreDetailsForPawn, true))
					{
						hoveringOverPawn = pawn;
					}
					curY += PawnRowHeight;
				}
			}
			GUIState.Pop();
		}

		public static bool ListPawns(ref float curY, Rect viewRect, Vector2 scrollPos, IThingHolder holder, string label, List<Pawn> pawns, ref Pawn moreDetailsForPawn)
		{
			bool overHandler = false;
			Rect handlerRect = new Rect(0, curY, viewRect.width - PawnExtraButtonSize * 2, (PawnRowHeight / 2) + (PawnRowHeight * pawns.Count));
			if (draggedPawn != null && Mouse.IsOver(handlerRect) && draggedPawn.ParentHolder != holder)
			{
				transferToHolder = holder;
				overHandler = true;
				Widgets.DrawHighlight(handlerRect);
			}

			Widgets.ListSeparator(ref curY, viewRect.width, label);

			if (false && holder is VehicleHandler handler && handler?.role.PawnRenderer != null && Prefs.DevMode && DebugSettings.godMode) //TODO - implement runtime editing of pawn renderer positions for modders
			{
				Rect editPawnOverlayRect = new Rect(viewRect.width - 15, curY + 3, 15, 15);
				TooltipHandler.TipRegionByKey(editPawnOverlayRect, "VF_EditPawnOverlayRendererTooltip");
				Color baseColor = (editingPawnOverlayRenderer != handler) ? Color.white : Color.green;
				Color mouseoverColor = (editingPawnOverlayRenderer != handler) ? GenUI.MouseoverColor : new Color(0f, 0.5f, 0f);
				if (Widgets.ButtonImage(editPawnOverlayRect, VehicleTex.Settings, baseColor, mouseoverColor))
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
				if (DoRow(curY, viewRect, scrollPos, pawn, ref moreDetailsForPawn, draggedPawn == null))
				{
					hoveringOverPawn = pawn;
				}
				curY += PawnRowHeight;
			}
			return overHandler;
		}

		private static bool DoRow(float curY, Rect viewRect, Vector2 scrollPos, Pawn pawn, ref Pawn moreDetailsForPawn, bool highlight)
		{
			float minY = scrollPos.y - PawnRowHeight;
			float maxY = scrollPos.y + ITab_Vehicle_Passengers.WindowHeight;

			bool isDraggingPawn = pawn == draggedPawn;
			
			if (!isDraggingPawn && (curY <= minY || curY >= maxY))
			{
				return false;
			}

			float nonRefY = isDraggingPawn ? (Event.current.mousePosition.y - PawnRowHeight / 2) : curY;
			float nonRefX = isDraggingPawn ? (Event.current.mousePosition.x - (LabelWidth + ThingIconSize) / 2) : 0;
			Rect pawnRect = new Rect(nonRefX, nonRefY, viewRect.width, PawnRowHeight);

			bool mouseOver;
			Widgets.BeginGroup(pawnRect);
			{
				Rect fullRect = pawnRect.AtZero();
				
				Rect dragRect = new Rect(0, 0, LabelWidth + ThingIconSize + PawnRowPadding, PawnRowHeight);
				mouseOver = Mouse.IsOver(dragRect);
				if (draggedPawn == null && mouseOver && Event.current.type == EventType.MouseDown && Event.current.button == 0)
				{
					draggedPawn = pawn;
					Event.current.Use();
					SoundDefOf.Click.PlayOneShotOnCamera(null);
				}
				
				Widgets.InfoCardButton(fullRect.width - PawnExtraButtonSize, (pawnRect.height - PawnExtraButtonSize) / 2f, pawn);
				fullRect.width -= PawnExtraButtonSize;
				if (!pawn.Dead)
				{
					OpenSpecificTabButton(fullRect, pawn, ref moreDetailsForPawn);
					fullRect.width -= PawnExtraButtonSize;
				}

				if (highlight)
				{
					Widgets.DrawHighlightIfMouseover(dragRect);
				}
				Rect iconRect = new Rect(PawnRowPadding, (pawnRect.height - ThingIconSize) / 2f, ThingIconSize, ThingIconSize);
				Widgets.ThingIcon(iconRect, pawn, 1f);
				Rect bgRect = new Rect(iconRect.xMax + PawnRowPadding, 16f, LabelWidth, 18f);
				GenMapUI.DrawPawnLabel(pawn, bgRect, 1f, LabelWidth, null, GameFont.Small, false, false);

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
					Rect needRect = new Rect(xMax, 0f, LabelWidth, PawnRowHeight);
					if (need is Need_Mood mood)
					{
						//maxThresholdMarkers = 1;
						//doTooltip = false;
						//TooltipHandler.TipRegion(rect4, new TipSignal(() => CaravanNeedsTabUtility.CustomMoodNeed)) //Add better way to make stringbuilder
					}
					need.DrawOnGUI(needRect, maxThresholdMarkers: maxThresholdMarkers, customMargin: 10, drawArrows: false, doTooltip: doTooltip);
					xMax = needRect.xMax;
				}

				if (pawn.Downed)
				{
					GUI.color = new Color(1f, 0f, 0f, 0.5f);
					Widgets.DrawLineHorizontal(0f, pawnRect.height / 2f, pawnRect.width);
					GUIState.Reset();
				}
			}
			Widgets.EndGroup();

			return mouseOver && !isDraggingPawn;
		}

		private static void OpenSpecificTabButton(Rect rowRect, Pawn pawn, ref Pawn moreDetailsForPawn)
		{
			GUIState.Push();
			{
				Color baseColor = (pawn != moreDetailsForPawn) ? Color.white : Color.green;
				Color mouseoverColor = (pawn != moreDetailsForPawn) ? GenUI.MouseoverColor : new Color(0f, 0.5f, 0f);
				Rect rect = new Rect(rowRect.width - PawnExtraButtonSize, (rowRect.height - PawnExtraButtonSize) / 2f, PawnExtraButtonSize, PawnExtraButtonSize);

				if (Widgets.ButtonImage(rect, CaravanThingsTabUtility.SpecificTabButtonTex, baseColor, mouseoverColor))
				{
					if (pawn == moreDetailsForPawn)
					{
						moreDetailsForPawn = null;
						SoundDefOf.TabClose.PlayOneShotOnCamera(null);
					}
					else
					{
						moreDetailsForPawn = pawn;
						SoundDefOf.TabOpen.PlayOneShotOnCamera(null);
					}
				}
				TooltipHandler.TipRegion(rect, "OpenSpecificTabButtonTip".Translate());
			}
			GUIState.Pop();
		}

		public static void HandleDragEvent()
		{
			if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
			{
				if (draggedPawn != null && transferToHolder != null)
				{
					if (transferToHolder is VehicleHandler transferToHandler && !transferToHandler.AreSlotsAvailable)
					{
						if (hoveringOverPawn != null && draggedPawn.ParentHolder is VehicleHandler curHandler && curHandler != transferToHolder && transferToHandler.CanOperateRole(draggedPawn) && curHandler.CanOperateRole(hoveringOverPawn))
						{
							curHandler.handlers.Swap(transferToHandler.handlers, draggedPawn, hoveringOverPawn);
							SoundDefOf.Click.PlayOneShotOnCamera();
							transferToHandler.vehicle.EventRegistry[VehicleEventDefOf.PawnChangedSeats].ExecuteEvents();
						}
						else
						{
							Messages.Message(TranslatorFormattedStringExtensions.Translate("VF_HandlerNotEnoughRoom", draggedPawn, transferToHandler.role.label), MessageTypeDefOf.RejectInput);
						}
					}
					else if (draggedPawn.ParentHolder != transferToHolder)
					{
						IThingHolder previousHolder = draggedPawn.ParentHolder;
						if (transferToHolder.GetDirectlyHeldThings().TryAddOrTransfer(draggedPawn, canMergeWithExistingStacks: false))
						{
							SoundDefOf.Click.PlayOneShotOnCamera();
							if (transferToHolder is VehicleHandler validHandler)
							{
								if (previousHolder is VehicleHandler)
								{
									validHandler.vehicle.EventRegistry[VehicleEventDefOf.PawnChangedSeats].ExecuteEvents();
								}
								else
								{
									if (!draggedPawn.Spawned && draggedPawn.IsWorldPawn())
									{
										Find.WorldPawns.RemovePawn(draggedPawn);
									}
									validHandler.vehicle.EventRegistry[VehicleEventDefOf.PawnEntered].ExecuteEvents();
								}
							}
							else if (previousHolder is VehicleHandler previousHandler)
							{
								if (!draggedPawn.Spawned && !draggedPawn.IsWorldPawn())
								{
									Find.WorldPawns.PassToWorld(draggedPawn);
								}
								previousHandler.vehicle.EventRegistry[VehicleEventDefOf.PawnExited].ExecuteEvents();
							}
						}
						else
						{
							Log.Warning($"Unable to add {draggedPawn} to {transferToHolder}.");
						}
					}
				}
				draggedPawn = null;
			}
		}

		public static Vector2 GetSize(IEnumerable<Pawn> pawns, float paneTopY, bool doNeeds = true)
		{
			float width = LabelWidth;
			if (doNeeds)
			{
				width += MaxNeedsCount(pawns) * LabelWidth;
			}
			width += PawnExtraButtonSize;
			Vector2 result;
			result.x = LabelWidth + width + 16f + 3; //Scrollbar=16 Padding=3
			result.y = Mathf.Min(ITab_Vehicle_Passengers.WindowHeight, paneTopY - 30f);
			return result;
		}

		private static int MaxNeedsCount(IEnumerable<Pawn> pawns)
		{
			int maxNeeds = 0;
			List<Need> pawnNeeds = new List<Need>();
			foreach (Pawn pawn in pawns)
			{
				if (pawn.needs != null)
				{
					foreach (Need need in pawn.needs.AllNeeds)
					{
						if (need.def.showForCaravanMembers)
						{
							pawnNeeds.Add(need);
						}
					}
					maxNeeds = Mathf.Max(maxNeeds, pawnNeeds.Count);
					pawnNeeds.Clear();
				}
			}
			return maxNeeds;
		}
	}
}
