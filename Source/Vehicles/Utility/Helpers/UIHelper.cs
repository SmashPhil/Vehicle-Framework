using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public static class UIHelper
	{
		/// <summary>
		/// Create new Widget for VehicleCaravan with <paramref name="transferables"/>
		/// </summary>
		/// <param name="transferables"></param>
		/// <param name="pawnsTransfer"></param>
		/// <param name="vehiclesTransfer"></param>
		/// <param name="itemsTransfer"></param>
		/// <param name="thingCountTip"></param>
		/// <param name="ignorePawnInventoryMass"></param>
		/// <param name="availableMassGetter"></param>
		/// <param name="ignoreSpawnedCorpsesGearAndInventoryMass"></param>
		/// <param name="tile"></param>
		/// <param name="playerPawnsReadOnly"></param>
		public static void CreateVehicleCaravanTransferableWidgets(List<TransferableOneWay> transferables, out TransferableOneWayWidget pawnsTransfer, out TransferableVehicleWidget vehiclesTransfer, out TransferableOneWayWidget itemsTransfer, string thingCountTip, IgnorePawnsInventoryMode ignorePawnInventoryMass, Func<float> availableMassGetter, bool ignoreSpawnedCorpsesGearAndInventoryMass, int tile, bool playerPawnsReadOnly = false)
		{
			pawnsTransfer = new TransferableOneWayWidget(null, null, null, thingCountTip, true, ignorePawnInventoryMass, false, availableMassGetter, 0f, ignoreSpawnedCorpsesGearAndInventoryMass, tile, true, true, true, false, true, false, playerPawnsReadOnly);
			vehiclesTransfer = new TransferableVehicleWidget(null, null, null, thingCountTip, true, ignorePawnInventoryMass, false, availableMassGetter, 0f, ignoreSpawnedCorpsesGearAndInventoryMass, tile, true, false, false);
			AddVehicleAndPawnSections(pawnsTransfer, vehiclesTransfer, transferables);
			itemsTransfer = new TransferableOneWayWidget(transferables.Where(t => t.ThingDef.category != ThingCategory.Pawn), null, null, thingCountTip, true, ignorePawnInventoryMass, false, availableMassGetter, 0f, ignoreSpawnedCorpsesGearAndInventoryMass, tile, true, false, false, true, false, true, false);
		}

		/// <summary>
		/// Create sections in VehicleCaravan dialog for proper listing
		/// </summary>
		/// <param name="pawnWidget"></param>
		/// <param name="vehicleWidget"></param>
		/// <param name="transferables"></param>
		public static void AddVehicleAndPawnSections(TransferableOneWayWidget pawnWidget, TransferableVehicleWidget vehicleWidget, List<TransferableOneWay> transferables)
		{
			IEnumerable<TransferableOneWay> source = transferables.Where(t => t.ThingDef.category == ThingCategory.Pawn);
			vehicleWidget.AddSection("VehiclesTab".Translate(), source.Where(t => t.AnyThing is VehiclePawn vehicle && vehicle.CanMove));
			pawnWidget.AddSection("ColonistsSection".Translate(), source.Where(t => t.AnyThing is Pawn pawn && pawn.IsFreeColonist));
			pawnWidget.AddSection("PrisonersSection".Translate(), source.Where(t => t.AnyThing is Pawn pawn && pawn.IsPrisoner));
			pawnWidget.AddSection("CaptureSection".Translate(), source.Where(t => t.AnyThing is Pawn pawn && pawn.Downed && CaravanUtility.ShouldAutoCapture(pawn, Faction.OfPlayer)));
			pawnWidget.AddSection("AnimalsSection".Translate(), source.Where(t => t.AnyThing is Pawn pawn && pawn.RaceProps.Animal));
			vehicleWidget.AvailablePawns = source.Where(x => x.AnyThing is Pawn pawn && !(pawn is VehiclePawn) && 
				(pawn.IsColonistPlayerControlled || (pawn.IsColonist && pawn.MentalStateDef == null && (pawn.HostFaction == null || pawn.IsSlave) && pawn.IsInVehicle()))).ToList();
		}

		/// <seealso cref="DoCountAdjustInterfaceInternal(Rect, Transferable, List{TransferableOneWay}, List{TransferableCountToTransferStoppingPoint}, int, int, int, bool, bool)"/>
		public static void DoCountAdjustInterface(Rect rect, Transferable trad, List<TransferableOneWay> pawns, int index, int min, int max, bool flash = false, List<TransferableCountToTransferStoppingPoint> extraStoppingPoints = null, bool readOnly = false)
		{
			var stoppingPoints = new List<TransferableCountToTransferStoppingPoint>();
			if (extraStoppingPoints != null)
			{
				stoppingPoints.AddRange(extraStoppingPoints);
			}
			for (int i = stoppingPoints.Count - 1; i >= 0; i--)
			{
				if (stoppingPoints[i].threshold != 0 && (stoppingPoints[i].threshold <= min || stoppingPoints[i].threshold >= max))
				{
					stoppingPoints.RemoveAt(i);
				}
			}
			bool flag = false;
			for (int j = 0; j < stoppingPoints.Count; j++)
			{
				if (stoppingPoints[j].threshold == 0)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				stoppingPoints.Add(new TransferableCountToTransferStoppingPoint(0, "0", "0"));
			}
			DoCountAdjustInterfaceInternal(rect, trad, pawns, stoppingPoints, index, min, max, flash, readOnly);
		}

		/// <summary>
		/// Create interface for changing amount being transfered in VehicleCaravan dialog
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="trad"></param>
		/// <param name="pawns"></param>
		/// <param name="stoppingPoints"></param>
		/// <param name="index"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <param name="flash"></param>
		/// <param name="readOnly"></param>
		private static void DoCountAdjustInterfaceInternal(Rect rect, Transferable trad, List<TransferableOneWay> pawns, List<TransferableCountToTransferStoppingPoint> stoppingPoints, int index, int min, int max, bool flash, bool readOnly)
		{
			rect = rect.Rounded();
			Rect rect2 = new Rect(rect.center.x - 45f, rect.center.y - 12.5f, 90f, 25f).Rounded();
			if (flash)
			{
				GUI.DrawTexture(rect2, TransferableUIUtility.FlashTex);
			}
			TransferableOneWay transferableOneWay = trad as TransferableOneWay;
			Color color = GUI.color;

			bool setToTransfer = trad.CountToTransfer != 0;
			bool checkOn = setToTransfer;

			Rect checkboxRect = new Rect(rect2.x + 125f, rect2.y, 24f, 24f);
			Widgets.Checkbox(rect.x + 125, rect.y, ref checkOn, disabled: readOnly);
			
			if (checkOn != setToTransfer)
			{
				if (setToTransfer)
				{
					foreach (Pawn pawn in (trad.AnyThing as VehiclePawn).AllPawnsAboard)
					{
						if (CaravanHelper.assignedSeats.ContainsKey(pawn))
						{
							CaravanHelper.assignedSeats.Remove(pawn);
						}
					}
					SoundDefOf.Click.PlayOneShotOnCamera();
					trad.AdjustTo(trad.GetMaximumToTransfer());
				}
				else
				{
					Find.WindowStack.Add(new Dialog_AssignSeats(pawns, transferableOneWay));
				}
				if (!checkOn)
				{
					CaravanHelper.ClearAssignedSeats(trad.AnyThing as VehiclePawn, (Pawn pawn) => pawns.FirstOrDefault(p => (p.AnyThing as Pawn) == pawn)?.ForceTo(0));
					trad.AdjustTo(trad.GetMinimumToTransfer());
				}
				Dialog_FormVehicleCaravan.MarkDirty();
			}
			if (trad.CountToTransfer != 0)
			{
				Rect position = new Rect(rect2.x + rect2.width / 2f - (VehicleTex.TradeArrow.width / 2), rect2.y + rect2.height / 2f - (VehicleTex.TradeArrow.height / 2), 
					VehicleTex.TradeArrow.width, VehicleTex.TradeArrow.height);
				TransferablePositiveCountDirection positiveCountDirection2 = trad.PositiveCountDirection;
				if ((positiveCountDirection2 == TransferablePositiveCountDirection.Source && trad.CountToTransfer > 0) || (positiveCountDirection2 == TransferablePositiveCountDirection.Destination && trad.CountToTransfer < 0))
				{
					position.x += position.width;
					position.width *= -1f;
				}
			}
		}

		/// <summary>
		/// Draw extra information on GUI for Transferable <paramref name="trad"/>
		/// </summary>
		/// <param name="trad"></param>
		/// <param name="idRect"></param>
		/// <param name="labelColor"></param>
		public static void DrawVehicleTransferableInfo(Transferable trad, Rect idRect, Color labelColor)
		{
			if (!trad.HasAnyThing && trad.IsThing)
			{
				return;
			}
			if (Mouse.IsOver(idRect))
			{
				Widgets.DrawHighlight(idRect);
			}
			Rect rect = new Rect(0f, 0f, 27f, 27f);
			//Draw Vehicle Icon
			if (trad.AnyThing is VehiclePawn vehicle)
			{
				try
				{
					Texture2D vehicleIcon = VehicleTex.VehicleTexture(vehicle.VehicleDef, Rot8.East);
					Rect texCoords = new Rect(0, 0, 1, 1);
					Vector2 texProportions = vehicle.VehicleDef.graphicData.drawSize;
					float x = texProportions.x;
					texProportions.x = texProportions.y;
					texProportions.y = x;
					Widgets.DrawTextureFitted(rect, vehicleIcon, GenUI.IconDrawScale(vehicle.VehicleDef), texProportions, 
						texCoords, 0, vehicle.VehicleGraphic.MatAt(Rot8.East, vehicle.Pattern));
					if (vehicle.CompVehicleTurrets is CompVehicleTurrets comp)
					{
						//REDO
						//foreach (VehicleTurret turret in comp.Cannons)
						//{
						//	if (turret.NoGraphic)
						//	{
						//		continue;
						//	}
						//	Vector2 drawSize = turret.turretDef.graphicData.drawSize;
						//	//Rect turretRect = new Rect(0, 0, rect.width / drawSize.x,);
						//	Widgets.DrawTextureFitted(rect, turret.CannonTexture, 1, drawSize, texCoords, Rot8.East.AsAngle + turret.defaultAngleRotated, turret.CannonMaterial);
						//}
					}
				}
				catch (Exception ex)
				{
					Log.ErrorOnce($"Unable to draw {vehicle.Label} for vehicle transferable item. Exception = \"{ex.Message}\"", vehicle.GetHashCode() ^ "TransferableIcon".GetHashCode());
				}
			}
			
			if (trad.IsThing)
			{
				//Widgets.InfoCardButton(40f, 0f, trad.AnyThing);
			}
			Text.Anchor = TextAnchor.MiddleLeft;
			Rect rect2 = new Rect(40f, 0f, idRect.width - 80f, idRect.height);
			Text.WordWrap = false;
			GUI.color = labelColor;
			Widgets.Label(rect2, trad.LabelCap);
			GUI.color = Color.white;
			Text.WordWrap = true;
			if (Mouse.IsOver(idRect))
			{
				Transferable localTrad = trad;
				TooltipHandler.TipRegion(idRect, new TipSignal(delegate()
				{
					if (!localTrad.HasAnyThing && localTrad.IsThing)
					{
						return string.Empty;
					}
					string text = localTrad.LabelCap;
					string tipDescription = localTrad.TipDescription;
					if (!tipDescription.NullOrEmpty())
					{
						text = text + ": " + tipDescription;
					}
					return text;
				}, localTrad.GetHashCode()));
			}
		}

		public static void DrawPagination(Rect rect, ref int pageNumber, int pageCount)
		{
			Rect leftButtonRect = new Rect(rect.x, rect.y, rect.height, rect.height);
			Rect rightButtonRect = new Rect(rect.x + rect.width - rect.height, rect.y, rect.height, rect.height);
			if (Widgets.ButtonImage(leftButtonRect, VehicleTex.LeftArrow))
			{
				pageNumber = (--pageNumber).Clamp(1, pageCount);
			}
			if (Widgets.ButtonImage(rightButtonRect, VehicleTex.RightArrow))
			{
				pageNumber = (++pageNumber).Clamp(1, pageCount);
			}
			float numbersLength = rect.width - rect.height * 2f;
			int pageNumbersDisplayedTotal = Mathf.CeilToInt((numbersLength / 1.5f) / rect.height);
			int pageNumbersDisplayedHalf = Mathf.FloorToInt(pageNumbersDisplayedTotal / 2f);

			var font = Text.Font;
			var anchor = Text.Anchor;
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;
			float pageNumberingOrigin = rect.x + rect.height + numbersLength / 2;
			Rect pageRect = new Rect(pageNumberingOrigin, rect.y, rect.height, rect.height);
			Widgets.ButtonText(pageRect, pageNumber.ToString(), false);

			Text.Font = GameFont.Tiny;
			int offsetRight = 1;
			for (int pageLeftDisplayNum = pageNumber + 1; pageLeftDisplayNum <= (pageNumber + pageNumbersDisplayedHalf) && pageLeftDisplayNum <= pageCount; pageLeftDisplayNum++, offsetRight++)
			{
				pageRect.x = pageNumberingOrigin + (numbersLength / pageNumbersDisplayedTotal * offsetRight);
				if (Widgets.ButtonText(pageRect, pageLeftDisplayNum.ToString(), false))
				{
					pageNumber = pageLeftDisplayNum;
				}
			}
			int offsetLeft = 1;
			for (int pageRightDisplayNum = pageNumber - 1; pageRightDisplayNum >= (pageNumber - pageNumbersDisplayedHalf) && pageRightDisplayNum >= 1; pageRightDisplayNum--, offsetLeft++)
			{
				pageRect.x = pageNumberingOrigin - (numbersLength / pageNumbersDisplayedTotal * offsetLeft);
				if (Widgets.ButtonText(pageRect, pageRightDisplayNum.ToString(), false))
				{
					pageNumber = pageRightDisplayNum;
				}
			}

			Text.Font = font;
			Text.Anchor = anchor;
		}
	}
}
