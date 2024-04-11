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
	[StaticConstructorOnStartup]
	public class ITab_Vehicle_Upgrades : ITab
	{
		public const float UpgradeNodeDim = 40;

		//public const float UpgradeNodesX = 20;
		//public const float UpgradeNodesY = 11;

		public const float TopPadding = 20;
		public const float EdgePadding = 5f;
		public const float SideDisplayedOffset = 40f;
		public const float BottomDisplayedOffset = 20f;

		public const float ScreenWidth = 880f;
		public const float ScreenHeight = 520f;

		public const float InfoScreenWidth = 325;
		public const float InfoScreenHeight = 150;
		public const float InfoScreenExtraGUIHeight = InfoScreenHeight;
		public const float InfoPanelRowHeight = 20f;
		public const float InfoPanelMoreDetailButtonSize = 24;
		public const float InfoPanelArrowPointerSize = 45;

		public const float BottomWindowEdge = ScreenHeight - TopPadding * 2;
		public const float TotalIconSizeScalar = 6000f;

		public static readonly Color ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);
		public static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);

		public static readonly Color NotUpgradedColor = new Color(0, 0, 0, 0.3f);
		public static readonly Color UpgradingColor = new Color(1, 0.75f, 0, 1);
		public static readonly Color DisabledColor = new Color(0.25f, 0.25f, 0.25f, 1);
		public static readonly Color DisabledLineColor = new Color(0.3f, 0.3f, 0.3f, 1);

		public static readonly Vector2 GridSpacing = new Vector2(20, 20);
		public static readonly Vector2 GridOrigin = new Vector2(30, 30);

		public static int totalLinesAcross = Mathf.FloorToInt(ScreenWidth / GridSpacing.x) - 2;
		public static int totalLinesDown = Mathf.FloorToInt(ScreenHeight / GridSpacing.y) - 3;

		public static readonly List<string> replaceNodes = new List<string>();

		private UpgradeNode selectedNode;
		private UpgradeNode highlightedNode;

		private Rect detailRect;
		private bool moreDetail = false;

		private static Vector2 scrollPosition;
		private static Vector2 resize;
		private bool resizeCheck;

		public UpgradeNode InfoNode => selectedNode ?? highlightedNode;

		public UpgradeNode SelectedNode
		{
			get
			{
				return selectedNode;
			}
			set
			{
				if (selectedNode != value)
				{
					selectedNode = value;
					moreDetail = false;
				}
			}
		}

		public int TotalLinesDown
		{
			get
			{
				int maxCoord = totalLinesDown;
				if (!Vehicle.CompUpgradeTree.Props.def.nodes.NullOrEmpty())
				{
					foreach (UpgradeNode node in Vehicle.CompUpgradeTree.Props.def.nodes)
					{
						if (node.GridCoordinate.z > maxCoord)
						{
							maxCoord = node.GridCoordinate.z;
						}
					}
				}
				return maxCoord;
			}
		}

		public ITab_Vehicle_Upgrades()
		{
			size = new Vector2(ScreenWidth, ScreenHeight);
			labelKey = "VF_TabUpgrades";
		}

		private VehiclePawn Vehicle
		{
			get
			{
				if (SelPawn is VehiclePawn vehicle && vehicle.CompUpgradeTree != null)
				{
					return SelPawn as VehiclePawn;
				}
				if (SelPawn is null)
				{
					return null;
				}    
				throw new InvalidOperationException("Upgrade ITab on Pawn without CompUpgradeTree: " + SelThing);
			}
		}

		public override void OnOpen()
		{
			base.OnOpen();
			SelectedNode = null;
			highlightedNode = null;
			resizeCheck = false;
			scrollPosition = Vector2.zero;
		}

		private Vector2 GridCoordinateToScreenPos(IntVec2 coord)
		{
			float x = GridOrigin.x + (GridSpacing.x * coord.x) - (GridSpacing.x / 2);
			float y = GridOrigin.y + (GridSpacing.y * coord.z) - (GridSpacing.y / 2) + TopPadding;
			return new Vector2(x, y);
		}

		private Vector2 GridCoordinateToScreenPosAdjusted(IntVec2 coord, Vector2 drawSize)
		{
			float x = GridOrigin.x + (GridSpacing.x * coord.x) - (GridSpacing.x / 2);
			float y = GridOrigin.y + (GridSpacing.y * coord.z) - (GridSpacing.y / 2) + TopPadding;

			float offsetX = drawSize.x / 2;
			float offsetY = drawSize.y / 2;

			return new Vector2(x - offsetX, y - offsetY);
		}

		protected override void FillTab()
		{
			Rect rect = new Rect(0f, TopPadding, size.x, size.y - TopPadding);
			Rect innerRect = rect.ContractedBy(5f);

			if (DebugSettings.ShowDevGizmos)
			{
				string debugGridLabel = "VF_DevMode_DebugDrawUpgradeNodeGrid".Translate();
				Vector2 debugGridLabelSize = Text.CalcSize(debugGridLabel);
				Rect devGridCheckboxRect = new Rect(rect.xMax - 80 - debugGridLabelSize.x, 0, debugGridLabelSize.x + 30, debugGridLabelSize.y);
				bool showGrid = VehicleMod.settings.debug.debugDrawNodeGrid;
				Widgets.CheckboxLabeled(devGridCheckboxRect, debugGridLabel, ref showGrid);
				VehicleMod.settings.debug.debugDrawNodeGrid = showGrid;
			}

			GUIState.Push();

			highlightedNode = null;
			
			//DrawButtons();

			DrawGrid(innerRect);

			Rect labelRect = new Rect(innerRect.x, 5, innerRect.width - 16f, 20f);
			Widgets.Label(labelRect, Vehicle.Label);

			GUIState.Reset();

			GUI.color = new Color(0.3f, 0.3f, 0.3f, 1f);

			if (!VehicleMod.settings.debug.debugDrawNodeGrid)
			{
				Widgets.DrawLineHorizontal(innerRect.x, innerRect.y, innerRect.width);
				Widgets.DrawLineHorizontal(innerRect.x, innerRect.yMax, innerRect.width);

				Widgets.DrawLineVertical(innerRect.x, innerRect.y, innerRect.height);
				Widgets.DrawLineVertical(innerRect.xMax, innerRect.y, innerRect.height);
			}

			GUIState.Pop();
		}

		private void DrawButtons(Rect rect)
		{
			if (Vehicle.CompUpgradeTree.NodeUnlocking == SelectedNode)
			{
				if (Widgets.ButtonText(rect, "CancelButton".Translate()))
				{
					Vehicle.CompUpgradeTree.ClearUpgrade();
					SelectedNode = null;
				}
			}
			else if (Vehicle.CompUpgradeTree.NodeUnlocked(SelectedNode) && Vehicle.CompUpgradeTree.LastNodeUnlocked(SelectedNode))
			{
				if (Widgets.ButtonText(rect, "VF_RemoveUpgrade".Translate()))
				{
					SoundDefOf.Click.PlayOneShotOnCamera(Vehicle.Map);
					if (DebugSettings.godMode)
					{
						Vehicle.CompUpgradeTree.ResetUnlock(SelectedNode);
					}
					else
					{
						Vehicle.CompUpgradeTree.RemoveUnlock(SelectedNode);
					}
					SelectedNode = null;
				}
			}
			else if (Widgets.ButtonText(rect, "VF_Upgrade".Translate()) && !Vehicle.CompUpgradeTree.NodeUnlocked(SelectedNode))
			{
				if (Vehicle.CompUpgradeTree.Disabled(SelectedNode))
				{
					Messages.Message("VF_DisabledFromOtherNode".Translate(), MessageTypeDefOf.RejectInput, false);
				}
				else if (Vehicle.CompUpgradeTree.PrerequisitesMet(SelectedNode))
				{
					SoundDefOf.ExecuteTrade.PlayOneShotOnCamera(Vehicle.Map);

					if (DebugSettings.godMode)
					{
						Vehicle.CompUpgradeTree.FinishUnlock(SelectedNode);
						SoundDefOf.Building_Complete.PlayOneShot(Vehicle);
					}
					else
					{
						Vehicle.CompUpgradeTree.StartUnlock(SelectedNode);
					}
					SelectedNode = null;
				}
				else
				{
					Messages.Message("VF_MissingPrerequisiteUpgrade".Translate(), MessageTypeDefOf.RejectInput, false);
				}
			}
		}

		private void DrawGrid(Rect rect)
		{
			float startY = TopPadding + GridOrigin.y;

			//Rect outRect = new Rect(rect.x, rect.y + startY, ScreenWidth - UpgradeNodeDim, ScreenHeight - startY);
			//Rect viewRect = new Rect(outRect.x, outRect.y, outRect.width - 21, outRect.y + TotalLinesDown * UpgradeNodeDim + UpgradeNodeDim / 2);

			GUIState.Push();

			//Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

			if (DebugSettings.ShowDevGizmos && VehicleMod.settings.debug.debugDrawNodeGrid)
			{
				DrawBackgroundGridTop();
				DrawBackgroundGridLeft();
			}

			if (SelectedNode != null && !Vehicle.CompUpgradeTree.Upgrading)
			{
				Vector2 selectedRectPos = GridCoordinateToScreenPosAdjusted(SelectedNode.GridCoordinate, SelectedNode.drawSize);
				Rect selectedRect = new Rect(selectedRectPos, SelectedNode.drawSize);
				selectedRect = selectedRect.ExpandedBy(2f);
				GUI.DrawTexture(selectedRect, BaseContent.WhiteTex);
			}
			foreach (UpgradeNode upgradeNode in Vehicle.CompUpgradeTree.Props.def.nodes)
			{
				if (!upgradeNode.prerequisiteNodes.NullOrEmpty() && !upgradeNode.hidden)
				{
					foreach (UpgradeNode prerequisite in Vehicle.CompUpgradeTree.Props.def.nodes.FindAll(prereqNode => !prereqNode.hidden && upgradeNode.prerequisiteNodes.Contains(prereqNode.key)))
					{
						Vector2 start = GridCoordinateToScreenPos(upgradeNode.GridCoordinate);
						Vector2 end = GridCoordinateToScreenPos(prerequisite.GridCoordinate);

						Color color = DisabledLineColor;
						if (!string.IsNullOrEmpty(upgradeNode.disableIfUpgradeNodeEnabled) && Vehicle.CompUpgradeTree.Props.def.GetNode(upgradeNode.disableIfUpgradeNodeEnabled) is UpgradeNode prereqNode)
						{
							Rect prerequisiteRect = new Rect(GridCoordinateToScreenPosAdjusted(prereqNode.GridCoordinate, prereqNode.drawSize), prerequisite.drawSize);
							if (!Vehicle.CompUpgradeTree.Upgrading && Mouse.IsOver(prerequisiteRect))
							{
								color = Color.red;
							}
						}
						else if (Vehicle.CompUpgradeTree.NodeUnlocked(upgradeNode))
						{
							color = Color.white;
						}
						Widgets.DrawLine(start, end, color, 2f);
					}
				}
			}
			GUIState.Reset();

			foreach (UpgradeNode upgradeNode in Vehicle.CompUpgradeTree.Props.def.nodes)
			{
				if (upgradeNode.hidden)
				{
					continue;
				}

				Vector2 upgradeRectPos = GridCoordinateToScreenPosAdjusted(upgradeNode.GridCoordinate, upgradeNode.drawSize);
				Rect upgradeRect = new Rect(upgradeRectPos, upgradeNode.drawSize);

				bool colored = false;
				if (Vehicle.CompUpgradeTree.Disabled(upgradeNode) || !Vehicle.CompUpgradeTree.PrerequisitesMet(upgradeNode))
				{
					colored = true;
					GUI.color = DisabledColor;
				}
				Widgets.DrawTextureFitted(upgradeRect, Command.BGTex, 1);

				Widgets.DrawTextureFitted(upgradeRect, upgradeNode.UpgradeImage, 1);

				GUIState.Reset();

				DrawNodeCondition(upgradeRect, upgradeNode, colored);

				GUIState.Reset();

				if (upgradeNode.displayLabel)
				{
					float textWidth = Text.CalcSize(upgradeNode.label).x;
					Rect nodeLabelRect = new Rect(upgradeRect.x - (textWidth - upgradeRect.width) / 2, upgradeRect.y - 20f, 10f * upgradeNode.label.Length, 25f);
					Widgets.Label(nodeLabelRect, upgradeNode.label);
				}

				if (Mouse.IsOver(upgradeRect))
				{
					highlightedNode = upgradeNode;
					if (Vehicle.CompUpgradeTree.PrerequisitesMet(upgradeNode) && !Vehicle.CompUpgradeTree.NodeUnlocked(upgradeNode))
					{
						GUI.DrawTexture(upgradeRect, TexUI.HighlightTex);
					}
				}

				if (Widgets.ButtonInvisible(upgradeRect, true))
				{
					if (SelectedNode != upgradeNode)
					{
						SelectedNode = upgradeNode;
						SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
					}
					else
					{
						SelectedNode = null;
						SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
					}
				}
			}

			float maxedInfoScreenHeight = InfoScreenHeight;
			if (InfoNode != null)
			{
				Vector2 upgradeRectPos = GridCoordinateToScreenPosAdjusted(InfoNode.GridCoordinate, InfoNode.drawSize);
				Rect upgradeRect = new Rect(upgradeRectPos, InfoNode.drawSize);

				float ingredientsHeight = 0;
				if (!InfoNode.ingredients.NullOrEmpty())
				{
					ingredientsHeight = InfoNode.ingredients.Count * InfoPanelRowHeight;
				}

				float textHeight = Text.CalcHeight(InfoNode.label, InfoScreenWidth) + Text.CalcHeight(InfoNode.description, InfoScreenWidth);
				maxedInfoScreenHeight = Mathf.Max(InfoScreenHeight, textHeight) + ingredientsHeight;

				float infoScreenX = upgradeRect.x + InfoNode.drawSize.x / 2f - InfoScreenWidth / 2f;
				float windowRectX = Mathf.Clamp(infoScreenX, rect.x + 5, Screen.width - InfoScreenWidth - 5);

				float windowRectY = upgradeRect.yMax + 5f;
				if (windowRectY + maxedInfoScreenHeight >= ScreenHeight)
				{
					windowRectY -= maxedInfoScreenHeight + 10f + InfoNode.drawSize.y;
				}

				detailRect = new Rect(windowRectX, windowRectY, InfoScreenWidth, maxedInfoScreenHeight);
			}

			GUIState.Reset();

			Rect selectedLabelRect = new Rect(5f, BottomWindowEdge, ScreenWidth, 150f);

			if (InfoNode != null)
			{
				Rect infoRect = new Rect(TabRect.x + detailRect.x, TabRect.y + detailRect.y, detailRect.width, detailRect.height);
				Find.WindowStack.ImmediateWindow(InfoNode.GetHashCode(), infoRect, WindowLayer.SubSuper, delegate ()
				{
					if (Vehicle != null && InfoNode != null)
					{
						GUIState.Push();

						Rect menuRect = new Rect(0, 0, infoRect.width, infoRect.height);
						Widgets.DrawMenuSection(menuRect);
						
						Rect innerInfoRect = menuRect.ContractedBy(5);

						Rect moreDetailRect = new Rect(innerInfoRect.xMax - InfoPanelMoreDetailButtonSize, innerInfoRect.y, InfoPanelMoreDetailButtonSize, InfoPanelMoreDetailButtonSize);

						Color baseColor = !moreDetail ? Color.white : Color.green;
						Color mouseoverColor = !moreDetail ? GenUI.MouseoverColor : new Color(0f, 0.5f, 0f);
						if (!InfoNode.graphicOverlays.NullOrEmpty() && Widgets.ButtonImageFitted(moreDetailRect, CaravanThingsTabUtility.SpecificTabButtonTex, baseColor, mouseoverColor))
						{
							moreDetail = !moreDetail;

							if (moreDetail)
							{
								SoundDefOf.TabOpen.PlayOneShotOnCamera(null);
							}
							else
							{
								SoundDefOf.TabClose.PlayOneShotOnCamera(null);
							}
						}
						GUIState.Reset();

						Rect detailRect = innerInfoRect;
						Text.Font = GameFont.Medium;
						float textHeight = Text.CalcHeight(InfoNode.label, detailRect.width);
						Rect labelRect = new Rect(detailRect.x, detailRect.y, detailRect.width, textHeight);
						Widgets.Label(labelRect, InfoNode.label);

						detailRect.y += textHeight;
						detailRect.height -= textHeight;

						Text.Font = GameFont.Small;

						float costY = DrawCostItems(detailRect);

						detailRect.y += costY;
						detailRect.height -= costY + 30;

						Widgets.Label(detailRect, InfoNode.description);

						if (Vehicle.CompUpgradeTree.NodeUnlocking == InfoNode)
						{
							textHeight = Text.CalcHeight(InfoNode.description, detailRect.width);
							detailRect.y += textHeight;
							detailRect.height -= textHeight;

							string workLabel = $"{"WorkLeft".Translate()}: {Vehicle.CompUpgradeTree.upgrade.WorkLeft.ToStringWorkAmount()}";
							textHeight = Text.CalcHeight(workLabel, detailRect.width);
							Rect workLabelRect = new Rect(detailRect.x, detailRect.y, detailRect.width, textHeight);
							Widgets.Label(workLabelRect, workLabel);

							detailRect.y += textHeight;
							detailRect.height -= textHeight;
						}

						if (SelectedNode != null)
						{
							Rect buttonRect = new Rect(detailRect.xMax - 125f, menuRect.yMax - 35, 120, 30);
							DrawButtons(buttonRect);
						}

						GUIState.Pop();
					}
				}, doBackground: false);

				if (moreDetail)
				{
					float infoScreenExtraGUIWidth = InfoScreenExtraGUIHeight * 2 + InfoPanelArrowPointerSize;
					float heightDifference = (maxedInfoScreenHeight - InfoScreenHeight) / 2;
					Rect moreDetailRect = new Rect(TabRect.x + detailRect.xMax + 5, TabRect.y + detailRect.y + heightDifference, infoScreenExtraGUIWidth, InfoScreenExtraGUIHeight);
					Find.WindowStack.ImmediateWindow(InfoNode.GetHashCode() ^ Vehicle.GetHashCode(), moreDetailRect, WindowLayer.SubSuper, delegate ()
					{
						if (Vehicle != null && InfoNode != null)
						{
							GUIState.Push();

							Rect menuRect = new Rect(0, 0, moreDetailRect.width, moreDetailRect.height);
							Widgets.DrawMenuSection(menuRect);

							Rect innerInfoRect = menuRect.ContractedBy(5);

							Rect vehicleOriginalRect = new Rect(innerInfoRect.x, innerInfoRect.y, innerInfoRect.height, innerInfoRect.height);
							VehicleGraphics.DrawVehicle(vehicleOriginalRect, Vehicle);

							float arrowSize = InfoPanelArrowPointerSize;
							Rect arrowPointerRect = new Rect(vehicleOriginalRect.xMax + 5, vehicleOriginalRect.y + vehicleOriginalRect.height / 2 - arrowSize / 2, arrowSize, arrowSize);
							Widgets.DrawTextureFitted(arrowPointerRect, TexData.TutorArrowRight, 1);

							Rect vehicleNewRect = new Rect(arrowPointerRect.xMax + 5, vehicleOriginalRect.y, vehicleOriginalRect.width, vehicleOriginalRect.height);
							VehicleGraphics.DrawVehicle(vehicleNewRect, Vehicle, extraOverlays: Vehicle.CompUpgradeTree.Props.TryGetOverlays(InfoNode));

							GUIState.Pop();
						}
					}, doBackground: false);
				}
			}

			//Widgets.EndScrollView();

			GUIState.Pop();
		}

		protected override void UpdateSize()
		{
			base.UpdateSize();
			if (resizeCheck)
			{
				size = resize;
			}
			else if (size.x != ScreenWidth || size.y != ScreenHeight)
			{
				size = new Vector2(ScreenWidth, ScreenHeight);
			}
		}

		private void DrawBackgroundGridTop()
		{
			for (int i = 0; i <= totalLinesAcross; i++)
			{
				GUI.color = new Color(0.3f, 0.3f, 0.3f, 1f);
				Widgets.DrawLineVertical(GridSpacing.x + GridSpacing.x * i, TopPadding + GridSpacing.y, totalLinesDown * GridSpacing.y);
				if (i % 5 == 0)
				{
					GUI.color = Color.white;
					Widgets.Label(new Rect(GridSpacing.x + GridSpacing.x * i - 5f, TopPadding + GridSpacing.y - 20f, 20f, 20f), i.ToString());
					GUIState.Reset();
				}
			}
			GUIState.Reset();
		}

		private void DrawBackgroundGridLeft()
		{
			for (int i = 0; i <= totalLinesDown; i++)
			{
				GUI.color = new Color(0.3f, 0.3f, 0.3f, 1f);
				Widgets.DrawLineHorizontal(GridSpacing.x, TopPadding + GridSpacing.y + GridSpacing.y * i, totalLinesAcross * GridSpacing.x);
				if (i % 5 == 0)
				{
					GUI.color = Color.white;
					Widgets.Label(new Rect(GridSpacing.x - 20f, TopPadding + GridSpacing.y + GridSpacing.y * i - 10f, 20f, 20f), i.ToString());
					GUIState.Reset();
				}
				GUIState.Reset();
			}
			GUIState.Reset();
		}

		private float DrawCostItems(Rect rect)
		{
			GUIState.Push();

			Rect costRect = rect;
			float currentY = 0;
			foreach (ThingDefCountClass thingDefCountClass in InfoNode.ingredients)
			{
				Rect iconRect = new Rect(costRect.x, costRect.y + currentY, InfoPanelRowHeight, InfoPanelRowHeight);
				GUI.DrawTexture(iconRect, thingDefCountClass.thingDef.uiIcon);

				string label = thingDefCountClass.thingDef.LabelCap;
				if (Vehicle.Map.resourceCounter.GetCount(thingDefCountClass.thingDef) < thingDefCountClass.count)
				{
					GUI.color = Color.red;
				}
				Rect itemCountRect = new Rect(iconRect.x + 26, iconRect.y, 50, InfoPanelRowHeight);
				Widgets.Label(itemCountRect, $"{thingDefCountClass.count}");

				Rect itemLabelRect = new Rect(iconRect.x + 60, itemCountRect.y, costRect.width - itemCountRect.width - 25, InfoPanelRowHeight);
				Widgets.Label(itemLabelRect, label);
				currentY += InfoPanelRowHeight;

				GUIState.Reset();
			}
			GUIState.Pop();
			return currentY;
		}

		private void DrawNodeCondition(Rect rect, UpgradeNode node, bool colored)
		{
			if (DisabledAndMouseOver())
			{
				DrawBorder(rect, Color.red);
			}
			else if (Vehicle.CompUpgradeTree.NodeUnlocking == node)
			{
				DrawBorder(rect, UpgradingColor);
			}
			else if (Vehicle.CompUpgradeTree.NodeUnlocked(node))
			{
				DrawBorder(rect, Color.white);
			}
			else if (!colored)
			{
				Widgets.DrawBoxSolid(rect, NotUpgradedColor);
			}
			else
			{
				DrawBorder(rect, NotUpgradedColor);
			}

			bool DisabledAndMouseOver()
			{
				if (!string.IsNullOrEmpty(node.disableIfUpgradeNodeEnabled) && Vehicle.CompUpgradeTree.Props.def.GetNode(node.disableIfUpgradeNodeEnabled) is UpgradeNode prereqNode)
				{
					Rect prerequisiteRect = new Rect(GridCoordinateToScreenPosAdjusted(prereqNode.GridCoordinate, prereqNode.drawSize), prereqNode.drawSize);
					if (!Vehicle.CompUpgradeTree.Upgrading && Mouse.IsOver(prerequisiteRect))
					{
						return true;
					}
				}
				return false;
			}
		}

		private void DrawBorder(Rect rect, Color color)
		{
			Vector2 topLeft = new Vector2(rect.x, rect.y);
			Vector2 topRight = new Vector2(rect.xMax, rect.y);

			Vector2 bottomLeft = new Vector2(rect.x, rect.yMax);
			Vector2 bottomRight = new Vector2(rect.xMax, rect.yMax);

			Vector2 leftTop = new Vector2(rect.x, rect.y + 2);
			Vector2 leftBottom = new Vector2(rect.x, rect.yMax + 2);

			Vector2 rightTop = new Vector2(rect.xMax, rect.y + 2);
			Vector2 rightBottom = new Vector2(rect.xMax, rect.yMax + 2);

			Widgets.DrawLine(topLeft, topRight, color, 1);
			Widgets.DrawLine(bottomLeft, bottomRight, color, 1);
			Widgets.DrawLine(leftTop, leftBottom, color, 1);
			Widgets.DrawLine(rightTop, rightBottom, color, 1);
		}
	}
}
