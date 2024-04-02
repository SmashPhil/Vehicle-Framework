using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;

namespace Vehicles
{
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

		public const float InfoScreenHeight = 200;
		public const float InfoScreenWidth = 300;
		public const float InfoScreenExtraGUI = 200;
		public const float InfoPanelRowHeight = 20f;

		public const float BottomWindowEdge = ScreenHeight - TopPadding * 2;
		public const float TotalIconSizeScalar = 6000f;

		public static readonly Color ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);
		public static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);

		public static readonly Color NotUpgradedColor = new Color(0, 0, 0, 0.3f);
		public static readonly Color UpgradingColor = new Color(1, 0.75f, 0, 1);
		public static readonly Color DisabledColor = new Color(0.25f, 0.25f, 0.25f, 1);
		public static readonly Color DisabledLineColor = new Color(0.3f, 0.3f, 0.3f, 1);

		public static readonly Vector2 GridSpacing = new Vector2(UpgradeNodeDim, UpgradeNodeDim);
		public static readonly Vector2 GridOrigin = new Vector2(UpgradeNodeDim, UpgradeNodeDim);

		private static readonly Color MenuSectionBGBorderColor = new ColorInt(135, 135, 135).ToColor;

		public static int totalLinesAcross = Mathf.FloorToInt(ScreenWidth / UpgradeNodeDim) - 1;

		public static readonly List<string> replaceNodes = new List<string>();

		private UpgradeNode selectedNode;
		private UpgradeNode highlightedNode;

		private Rect detailRect;

		private static Vector2 scrollPosition;
		private static Vector2 resize;
		private bool resizeCheck;

		public UpgradeNode InfoNode => selectedNode ?? highlightedNode;

		public int TotalLinesDown
		{
			get
			{
				int maxCoord = 11;
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
			selectedNode = null;
			highlightedNode = null;
			resizeCheck = false;
			scrollPosition = Vector2.zero;
		}

		private float GridCoordinateToScreenPosX(int node) => GridOrigin.x + (GridSpacing.x * node) - (UpgradeNodeDim / 2);

		private float GridCoordinateToScreenPosY(int node) => GridOrigin.y + (GridSpacing.y * node) - (UpgradeNodeDim / 2) + TopPadding;

		protected override void FillTab()
		{
			Rect rect = new Rect(0f, TopPadding, size.x, size.y - TopPadding);
			Rect innerRect = rect.ContractedBy(5f);

			GUIState.Push();

			highlightedNode = null;
			
			//DrawButtons();

			DrawGrid(innerRect);

			Rect labelRect = new Rect(innerRect.x, 5, innerRect.width - 16f, 20f);
			Widgets.Label(labelRect, Vehicle.Label);

			GUIState.Reset();

			GUI.color = new Color(0.3f, 0.3f, 0.3f, 1f);

			Widgets.DrawLineHorizontal(innerRect.x, innerRect.y, innerRect.width);
			Widgets.DrawLineHorizontal(innerRect.x, innerRect.yMax, innerRect.width);

			Widgets.DrawLineVertical(innerRect.x, innerRect.y, innerRect.height);
			Widgets.DrawLineVertical(innerRect.xMax, innerRect.y, innerRect.height);

			GUIState.Pop();
		}

		private void DrawButtons(Rect rect)
		{
			if (Vehicle.CompUpgradeTree.NodeUnlocking == selectedNode)
			{
				if (Widgets.ButtonText(rect, "CancelButton".Translate()))
				{
					Vehicle.CompUpgradeTree.ClearUpgrade();
					selectedNode = null;
				}
			}
			else if (Vehicle.CompUpgradeTree.NodeUnlocked(selectedNode) && Vehicle.CompUpgradeTree.LastNodeUnlocked(selectedNode))
			{
				if (Widgets.ButtonText(rect, "VF_RemoveUpgrade".Translate()))
				{
					Vehicle.CompUpgradeTree.ResetUnlock(selectedNode);
					selectedNode = null;
				}
			}
			else if (Widgets.ButtonText(rect, "VF_Upgrade".Translate()) && !Vehicle.CompUpgradeTree.NodeUnlocked(selectedNode))
			{
				if (Vehicle.CompUpgradeTree.Disabled(selectedNode))
				{
					Messages.Message("VF_DisabledFromOtherNode".Translate(), MessageTypeDefOf.RejectInput, false);
				}
				else if (Vehicle.CompUpgradeTree.PrerequisitesMet(selectedNode))
				{
					SoundDefOf.ExecuteTrade.PlayOneShotOnCamera(Vehicle.Map);
					SoundDefOf.Building_Complete.PlayOneShot(Vehicle);

					if (DebugSettings.godMode)
					{
						Vehicle.CompUpgradeTree.FinishUnlock(selectedNode);
					}
					else
					{
						Vehicle.CompUpgradeTree.StartUnlock(selectedNode);
					}
					selectedNode = null;
				}
				else
				{
					Messages.Message("VF_MissingPrerequisiteUpgrade".Translate(), MessageTypeDefOf.RejectInput, false);
				}
			}
		}

		private void DrawGrid(Rect rect)
		{
			float startY = TopPadding + GridSpacing.y;

			//Rect outRect = new Rect(rect.x, rect.y + startY, ScreenWidth - UpgradeNodeDim, ScreenHeight - startY);
			//Rect viewRect = new Rect(outRect.x, outRect.y, outRect.width - 21, outRect.y + TotalLinesDown * UpgradeNodeDim + UpgradeNodeDim / 2);

			GUIState.Push();
			
			if (VehicleMod.settings.debug.debugDrawNodeGrid)
			{
				DrawBackgroundGridTop();
			}

			//Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

			if (VehicleMod.settings.debug.debugDrawNodeGrid)
			{
				DrawBackgroundGridLeft(rect.width);
			}

			if (selectedNode != null && !Vehicle.CompUpgradeTree.CurrentlyUpgrading)
			{
				Rect selectedRect = new Rect(GridCoordinateToScreenPosX(selectedNode.GridCoordinate.x), GridCoordinateToScreenPosY(selectedNode.GridCoordinate.z), UpgradeNodeDim, UpgradeNodeDim);
				selectedRect = selectedRect.ExpandedBy(2f);
				GUI.DrawTexture(selectedRect, BaseContent.WhiteTex);
			}
			foreach (UpgradeNode upgradeNode in Vehicle.CompUpgradeTree.Props.def.nodes)
			{
				if (!upgradeNode.prerequisiteNodes.NullOrEmpty())
				{
					foreach (UpgradeNode prerequisite in Vehicle.CompUpgradeTree.Props.def.nodes.FindAll(x => upgradeNode.prerequisiteNodes.Contains(x.key)))
					{
						Vector2 start = new Vector2(GridOrigin.x + (GridSpacing.x * prerequisite.GridCoordinate.x), GridOrigin.y + (GridSpacing.y * prerequisite.GridCoordinate.z) + TopPadding);
						Vector2 end = new Vector2(GridOrigin.x + (GridSpacing.x * upgradeNode.GridCoordinate.x), GridOrigin.y + (GridSpacing.y * upgradeNode.GridCoordinate.z) + TopPadding);
						Color color = DisabledLineColor;
						if (!string.IsNullOrEmpty(upgradeNode.disableIfUpgradeNodeEnabled) && Vehicle.CompUpgradeTree.Props.def.GetNode(upgradeNode.disableIfUpgradeNodeEnabled) is UpgradeNode prereqNode)
						{
							Rect prerequisiteRect = new Rect(GridCoordinateToScreenPosX(prereqNode.GridCoordinate.x), GridCoordinateToScreenPosY(prereqNode.GridCoordinate.z), UpgradeNodeDim, UpgradeNodeDim);
							if (!Vehicle.CompUpgradeTree.CurrentlyUpgrading && Mouse.IsOver(prerequisiteRect))
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
				Rect upgradeRect = new Rect(GridCoordinateToScreenPosX(upgradeNode.GridCoordinate.x), GridCoordinateToScreenPosY(upgradeNode.GridCoordinate.z), UpgradeNodeDim, UpgradeNodeDim);

				bool colored = false;
				if (Vehicle.CompUpgradeTree.Disabled(upgradeNode) || !Vehicle.CompUpgradeTree.PrerequisitesMet(upgradeNode))
				{
					colored = true;
					GUI.color = DisabledColor;
					//Widgets.DrawBoxSolid(upgradeRect, DisabledColor);
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
					if (selectedNode != upgradeNode)
					{
						selectedNode = upgradeNode;
						SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
					}
					else
					{
						selectedNode = null;
						SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
					}
				}
			}

			if (InfoNode != null)
			{
				Rect upgradeRect = new Rect(GridCoordinateToScreenPosX(InfoNode.GridCoordinate.x), GridCoordinateToScreenPosY(InfoNode.GridCoordinate.z), UpgradeNodeDim, UpgradeNodeDim);

				float ingredientsHeight = 0;
				if (!InfoNode.ingredients.NullOrEmpty())
				{
					ingredientsHeight = InfoNode.ingredients.Count * InfoPanelRowHeight;
				}

				float textHeight = Text.CalcHeight(InfoNode.label, InfoScreenWidth) + Text.CalcHeight(InfoNode.description, InfoScreenWidth);
				float maxedInfoScreenHeight = Mathf.Max(InfoScreenHeight, textHeight) + ingredientsHeight;

				float infoScreenX = upgradeRect.x + UpgradeNodeDim / 2f - InfoScreenWidth / 2f;
				float windowRectX = Mathf.Clamp(infoScreenX, rect.x + 5, Screen.width - InfoScreenWidth - 5);

				float windowRectY = upgradeRect.yMax + 5f;
				if (windowRectY + maxedInfoScreenHeight >= ScreenHeight)
				{
					windowRectY -= maxedInfoScreenHeight + 10f + UpgradeNodeDim;
				}

				float width = InfoScreenWidth;
				if (!InfoNode.overlays.NullOrEmpty())
				{
					width += InfoScreenExtraGUI;
				}
				detailRect = new Rect(windowRectX, windowRectY, width, maxedInfoScreenHeight);
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

						innerInfoRect.SplitVertically(InfoScreenWidth - InfoScreenExtraGUI, out Rect leftRect, out Rect rightRect);

						Rect detailRect = innerInfoRect;
						if (!InfoNode.overlays.NullOrEmpty())
						{
							detailRect = leftRect;

							GUI.color = MenuSectionBGBorderColor;
							{
								Widgets.DrawLineVertical(detailRect.xMax, innerInfoRect.y, innerInfoRect.height);
							}
							GUIState.Reset();

							Rect iconRect = new Rect(rightRect.x, rightRect.y, InfoScreenExtraGUI, InfoScreenExtraGUI);
							iconRect = iconRect.ContractedBy(InfoScreenExtraGUI * 0.2f);
							VehicleGraphics.DrawVehicle(iconRect, Vehicle);
						}
						
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

							string workLabel = $"Work: {Vehicle.CompUpgradeTree.upgrade.WorkLeft}";
							textHeight = Text.CalcHeight(workLabel, detailRect.width);
							Rect workLabelRect = new Rect(detailRect.x, detailRect.y, detailRect.width, textHeight);
							Widgets.Label(workLabelRect, workLabel);

							detailRect.y += textHeight;
							detailRect.height -= textHeight;
						}

						if (selectedNode != null)
						{
							Rect buttonRect = new Rect(detailRect.xMax - 125f, menuRect.yMax - 35, 120, 30);
							DrawButtons(buttonRect);
						}

						GUIState.Pop();
					}
				}, doBackground: false);
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
			GUI.color = new Color(0.3f, 0.3f, 0.3f, 1f);
			for (int i = 0; i < totalLinesAcross; i++)
			{
				Widgets.DrawLineVertical(GridSpacing.x + GridSpacing.x * i, TopPadding + GridSpacing.y, TotalLinesDown * UpgradeNodeDim);
				Widgets.Label(new Rect(GridSpacing.x + GridSpacing.x * i - 5f, TopPadding + GridSpacing.y - 20f, 20f, 20f), i.ToString());
			}
			GUIState.Reset();
		}

		private void DrawBackgroundGridLeft(float width)
		{
			GUI.color = new Color(0.3f, 0.3f, 0.3f, 1f);
			for (int i = 0; i <= TotalLinesDown; i++)
			{
				Widgets.DrawLineHorizontal(GridSpacing.x, TopPadding + GridSpacing.y + GridSpacing.y * i, width - GridSpacing.x);
				Widgets.Label(new Rect(GridSpacing.x - 20f, TopPadding + GridSpacing.y + GridSpacing.y * i - 10f, 20f, 20f), i.ToString());
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
					Rect prerequisiteRect = new Rect(GridCoordinateToScreenPosX(prereqNode.GridCoordinate.x), GridCoordinateToScreenPosY(prereqNode.GridCoordinate.z), UpgradeNodeDim, UpgradeNodeDim);
					if (!Vehicle.CompUpgradeTree.CurrentlyUpgrading && Mouse.IsOver(prerequisiteRect))
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
