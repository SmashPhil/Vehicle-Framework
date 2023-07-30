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
		public const float UpgradeNodeDim = 30;

		public const float TopPadding = 20;
		public const float EdgePadding = 5f;
		public const float SideDisplayedOffset = 40f;
		public const float BottomDisplayedOffset = 20f;

		public const float LeftWindowEdge = screenWidth - leftWindowWidth;

		public const float screenWidth = 850f;
		public const float screenHeight = 520f;

		public const float leftWindowWidth = 250;
		public const float infoScreenHeight = 200;
		public const float infoScreenWidth = 400;

		public const float BottomWindowEdge = screenHeight - TopPadding * 2;
		public const float TotalIconSizeScalar = 6000f;

		public static int TotalLinesAcross = Mathf.FloorToInt(LeftWindowEdge / UpgradeNodeDim) - 1;
		public int TotalLinesDown => Vehicle.CompUpgradeTree.upgradeList.NullOrEmpty() ? 0 : Vehicle.CompUpgradeTree.upgradeList.Max(u => u.GridCoordinate.z);

		public static readonly Color ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);
		public static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);
		public static readonly Color NotUpgradedColor = new Color(0, 0, 0, 0.5f);
		public static readonly Color PrerequisitesNotMetColor = new Color(0, 0, 0, 0.8f);
		public static readonly Color LockScreenColor = new Color(0, 0, 0, 0.6f);
		public static readonly Vector2 GridSpacing = new Vector2(UpgradeNodeDim, UpgradeNodeDim);
		public static readonly Vector2 GridOrigin = new Vector2(UpgradeNodeDim, UpgradeNodeDim);

		public static readonly List<string> replaceNodes = new List<string>();

		private SelectedNode selectedNode;

		private static Vector2 scrollPosition;
		private static Vector2 resize;
		private bool resizeCheck;

		public ITab_Vehicle_Upgrades()
		{
			size = new Vector2(screenWidth, screenHeight);
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
				if(SelPawn is null)
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
			resizeCheck = false;
			scrollPosition = Vector2.zero;
		}

		private float GridCoordinateToScreenPosX(int node) => GridOrigin.x + (GridSpacing.x * node) - (UpgradeNodeDim / 2);

		private float GridCoordinateToScreenPosY(int node) => GridOrigin.y + (GridSpacing.y * node) - (UpgradeNodeDim / 2) + TopPadding;

		protected override void FillTab()
		{
			Rect rect = new Rect(0f, TopPadding, size.x, size.y - TopPadding);
			Rect rect2 = rect.ContractedBy(10f);

			Widgets.BeginGroup(rect2);
			Text.Font = GameFont.Small;
			GUI.color = Color.white;

			UpgradeNode additionalStatNode = null;
			UpgradeNode highlightedPreMetNode = null;

			Rect upgradeButtonRect = new Rect(screenWidth - BottomDisplayedOffset - 80f, BottomWindowEdge - 30f, 75f, 25f);
			Rect cancelButtonRect = new Rect(LeftWindowEdge + 5f, BottomWindowEdge - 30f, 75f, 25f);

			GUI.enabled = selectedNode != null && Vehicle.CompUpgradeTree.PrerequisitesMet(selectedNode.node);
			if (Widgets.ButtonText(upgradeButtonRect, "VF_Upgrade".Translate()) && !selectedNode.node.upgradeActive)
			{
				if (Vehicle.CompUpgradeTree.Disabled(selectedNode.node))
				{
					Messages.Message("VF_DisabledFromOtherNode".Translate(), MessageTypeDefOf.RejectInput, false);
				}
				else if (Vehicle.CompUpgradeTree.PrerequisitesMet(selectedNode.node))
				{
					SoundDefOf.ExecuteTrade.PlayOneShotOnCamera(Vehicle.Map);
					SoundDefOf.Building_Complete.PlayOneShot(Vehicle);

					Vehicle.ignition.Drafted = false;
					if (DebugSettings.godMode)
					{
						Vehicle.CompUpgradeTree.FinishUnlock(selectedNode.node);
					}
					else
					{
						Vehicle.CompUpgradeTree.StartUnlock(selectedNode.node);
					}
					selectedNode.node.upgradePurchased = true;
					selectedNode = null;
				}
				else
				{
					Messages.Message("VF_MissingPrerequisiteUpgrade".Translate(), MessageTypeDefOf.RejectInput, false);
				}
			}
			GUI.enabled = true;
			if (Vehicle.CompUpgradeTree.CurrentlyUpgrading)
			{
				if (Widgets.ButtonText(cancelButtonRect, "CancelButton".Translate()))
				{
					Vehicle.CompUpgradeTree.CancelUpgrade();
				}
			}
			else if (selectedNode != null && Vehicle.CompUpgradeTree.NodeListed(selectedNode.node).upgradeActive && Vehicle.CompUpgradeTree.LastNodeUnlocked(selectedNode.node))
			{
				if (Widgets.ButtonText(cancelButtonRect, "RefundUpgrade".Translate()))
				{
					Vehicle.CompUpgradeTree.RefundUnlock(Vehicle.CompUpgradeTree.NodeListed(selectedNode.node));
					selectedNode = null;
				}
			}
			else
			{
				Widgets.ButtonText(cancelButtonRect, string.Empty);
			}
			float startY = TopPadding + GridSpacing.y;
			Rect outRect = new Rect(0, startY, LeftWindowEdge, screenHeight - startY);
			Rect viewRect = new Rect(outRect.x, outRect.y, outRect.width, outRect.y + TotalLinesDown * UpgradeNodeDim + UpgradeNodeDim / 2);

			var colorGrid = GUI.color;
			GUI.color = new Color(0.3f, 0.3f, 0.3f, 1f);
			if (VehicleMod.settings.debug.debugDrawNodeGrid)
			{
				DrawBackgroundGridTop();
			}
			GUI.color = colorGrid;
			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
			GUI.color = new Color(0.3f, 0.3f, 0.3f, 1f);
			if (VehicleMod.settings.debug.debugDrawNodeGrid)
			{
				DrawBackgroundGridLeft();
			}
			GUI.color = colorGrid;

			if (selectedNode != null && !Vehicle.CompUpgradeTree.CurrentlyUpgrading)
			{
				Rect selectedRect = new Rect(GridCoordinateToScreenPosX(selectedNode.node.GridCoordinate.x), GridCoordinateToScreenPosY(selectedNode.node.GridCoordinate.z), UpgradeNodeDim, UpgradeNodeDim);
				selectedRect = selectedRect.ExpandedBy(2f);
				GUI.DrawTexture(selectedRect, BaseContent.WhiteTex);
			}
			foreach (UpgradeNode upgradeNode in Vehicle.CompUpgradeTree.upgradeList)
			{
				if (!upgradeNode.prerequisiteNodes.NullOrEmpty())
				{
					foreach (UpgradeNode prerequisite in Vehicle.CompUpgradeTree.upgradeList.FindAll(x => upgradeNode.prerequisiteNodes.Contains(x.upgradeID)))
					{
						Vector2 start = new Vector2(GridOrigin.x + (GridSpacing.x * prerequisite.GridCoordinate.x), GridOrigin.y + (GridSpacing.y * prerequisite.GridCoordinate.z) + TopPadding);
						Vector2 end = new Vector2(GridOrigin.x + (GridSpacing.x * upgradeNode.GridCoordinate.x), GridOrigin.y + (GridSpacing.y * upgradeNode.GridCoordinate.z) + TopPadding);
						Color color = Color.grey;
						if (upgradeNode.upgradeActive)
						{
							color = Color.white;
						}
						else if (!string.IsNullOrEmpty(upgradeNode.disableIfUpgradeNodeEnabled))
						{
							try
							{
								UpgradeNode preUpgrade = Vehicle.CompUpgradeTree.NodeListed(upgradeNode.disableIfUpgradeNodeEnabled);
								float imageWidth = TotalIconSizeScalar / preUpgrade.UpgradeImage.width;
								float imageHeight = TotalIconSizeScalar / preUpgrade.UpgradeImage.height;
								Rect preUpgradeRect = new Rect(GridOrigin.x + (GridSpacing.x * preUpgrade.GridCoordinate.x) - (imageWidth/2), GridOrigin.y + (GridSpacing.y * preUpgrade.GridCoordinate.z) - (imageHeight/2) + (TopPadding*2), imageWidth, imageHeight);
								if (!Vehicle.CompUpgradeTree.CurrentlyUpgrading)
								{
									if (preUpgrade.upgradePurchased)
									{
										color = Color.black;
									}
									else if (Mouse.IsOver(preUpgradeRect))
									{
										color = Color.red;
									}
								}
							}
							catch
							{
								Log.Error($"Unable to find UpgradeNode {upgradeNode.disableIfUpgradeNodeEnabled} on iteration. Are you referencing the proper upgradeID?");
							}
						}
						Widgets.DrawLine(start, end, color, 2f);
					}
				}
			}
			
			bool preDrawingDescriptions = false;
			for(int i = 0; i < Vehicle.CompUpgradeTree.upgradeList.Count; i++)
			{
				UpgradeNode upgradeNode = Vehicle.CompUpgradeTree.upgradeList[i];

				Rect upgradeRect = new Rect(GridCoordinateToScreenPosX(upgradeNode.GridCoordinate.x), GridCoordinateToScreenPosY(upgradeNode.GridCoordinate.z), UpgradeNodeDim, UpgradeNodeDim);
				Widgets.DrawTextureFitted(upgradeRect, upgradeNode.UpgradeImage, 1);
				
				if (!Vehicle.CompUpgradeTree.PrerequisitesMet(upgradeNode))
				{
					Widgets.DrawBoxSolid(upgradeRect, PrerequisitesNotMetColor);
				}
				else if (!upgradeNode.upgradeActive || !upgradeNode.upgradePurchased)
				{
					Widgets.DrawBoxSolid(upgradeRect, NotUpgradedColor);
				}

				if (upgradeNode.displayLabel)
				{
					float textWidth = Text.CalcSize(upgradeNode.label).x;
					Rect nodeLabelRect = new Rect(upgradeRect.x - (textWidth - upgradeRect.width) / 2, upgradeRect.y - 20f, 10f * upgradeNode.label.Length, 25f);
					Widgets.Label(nodeLabelRect, upgradeNode.label);
				}
				Rect infoLabelRect = new Rect(5f, BottomWindowEdge, LeftWindowEdge, 150f);
				GUIStyle infoLabelFont = new GUIStyle(Text.CurFontStyle);
				infoLabelFont.fontStyle = FontStyle.Bold;
				
				if (!Vehicle.CompUpgradeTree.CurrentlyUpgrading)
				{
					if (Mouse.IsOver(upgradeRect))
					{
						preDrawingDescriptions = true;

						if (!upgradeNode.upgradePurchased)
						{
							additionalStatNode = upgradeNode;
							highlightedPreMetNode = upgradeNode;
						}
						UIElements.LabelStyled(infoLabelRect, upgradeNode.label, infoLabelFont);

						Widgets.Label(new Rect(infoLabelRect.x, infoLabelRect.y + 20f, infoLabelRect.width, 140f), upgradeNode.informationHighlighted);
					}

					if (Mouse.IsOver(upgradeRect) && Vehicle.CompUpgradeTree.PrerequisitesMet(upgradeNode) && !upgradeNode.upgradeActive)
					{
						GUI.DrawTexture(upgradeRect, TexUI.HighlightTex);
					}

					if (Widgets.ButtonInvisible(upgradeRect, true))
					{

						if (selectedNode?.node != upgradeNode)
						{
							float textHeight = Text.CalcHeight(upgradeNode.label, infoScreenWidth) + Text.CalcHeight(upgradeNode.informationHighlighted, infoScreenWidth);
							float maxedInfoScreenHeight = Mathf.Max(infoScreenHeight, textHeight);
							Rect infoPanelRect = new Rect(upgradeRect.x, upgradeRect.y + upgradeRect.height + 10f, infoScreenWidth, maxedInfoScreenHeight);
							float leftPanelEnd = LeftWindowEdge - 10;
							if (infoPanelRect.x + infoPanelRect.width > leftPanelEnd)
							{
								infoPanelRect.x -= infoPanelRect.x + infoPanelRect.width - leftPanelEnd;
							}
							if (infoPanelRect.y + infoPanelRect.height + TopPadding > screenHeight - TopPadding)
							{
								infoPanelRect.y -= infoPanelRect.height + 20f + UpgradeNodeDim;
							}
							selectedNode = new SelectedNode(upgradeNode, infoPanelRect);
							SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
						}
						else
						{
							selectedNode = null;
							SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
						}
					}
				}
			}
			Rect selectedLabelRect = new Rect(5f, BottomWindowEdge, LeftWindowEdge, 150f);
			GUIStyle selectedLabelFont = new GUIStyle(Text.CurFontStyle);
			selectedLabelFont.fontStyle = FontStyle.Bold;

			if (selectedNode != null)
			{
				Rect detailRect = new Rect(selectedNode.detailRect)
				{
					y = selectedNode.detailRect.y + TabRect.y + UpgradeNodeDim
				};
				Find.WindowStack.ImmediateWindow(selectedNode.node.nodeID ^ selectedNode.node.GetHashCode(), detailRect, WindowLayer.SubSuper, delegate()
				{
					if (selectedNode != null)
					{
						UpgradeNode detailNode = selectedNode.node;
						Rect rect = new Rect(0, 0, detailRect.width, detailRect.height);
						Widgets.DrawMenuSection(rect);
						rect = rect.ContractedBy(5);

						Widgets.Label(rect, $"<b>{selectedNode.node.label}</b>");
						float textHeight = Text.CalcHeight(selectedNode.node.label, rect.width) + 2;
						rect.y += textHeight;
						rect.height -= textHeight;
						Widgets.Label(rect, selectedNode.node.informationHighlighted);
						DrawCostItems(selectedNode);
					}
				});
			}

			Widgets.EndScrollView();

			if (selectedNode != null && !preDrawingDescriptions)
			{
				if (!selectedNode.node.upgradePurchased)
				{
					additionalStatNode = selectedNode.node;
				}
					
				UIElements.LabelStyled(selectedLabelRect, selectedNode.node.label, selectedLabelFont);

				Widgets.Label(new Rect(selectedLabelRect.x, selectedLabelRect.y + 20f, selectedLabelRect.width, 140f), selectedNode.node.informationHighlighted);
			}
			
			Rect labelRect = new Rect(0f, 0f, rect2.width - 16f, 20f);

			if (!VehicleMod.settings.debug.debugDrawNodeGrid)
			{
				Widgets.Label(labelRect, Vehicle.Label);
			}
			
			Color lineColor = GUI.color;
			GUI.color = new Color(0.3f, 0.3f, 0.3f, 1f);

			Widgets.DrawLineHorizontal(0, TopPadding, screenWidth);
			Widgets.DrawLineHorizontal(0, screenHeight - SideDisplayedOffset - 1f, screenWidth);

			Widgets.DrawLineVertical(0, TopPadding, screenHeight);
			Widgets.DrawLineVertical(screenWidth - BottomDisplayedOffset - 1f, TopPadding, screenHeight);

			if (VehicleMod.settings.main.drawUpgradeInformationScreen)
			{
				Widgets.DrawLineVertical(LeftWindowEdge, TopPadding, screenHeight);
			}
			GUI.color = lineColor;

			if (VehicleMod.settings.main.drawUpgradeInformationScreen)
			{
				if(Vehicle != null)
				{
					try
					{
						replaceNodes.Clear();
						if (selectedNode != null && !selectedNode.node.replaces.NullOrEmpty())
						{
							replaceNodes.AddRange(selectedNode.node.replaces);
						}
						else if (highlightedPreMetNode != null && !highlightedPreMetNode.replaces.NullOrEmpty())
						{
							replaceNodes.AddRange(highlightedPreMetNode.replaces);
						}

						Rect vehicleDisplayRect = new Rect();
						Widgets.BeginGroup(vehicleDisplayRect);

						Rect displayRect = new Rect(0, 0, leftWindowWidth - 5, leftWindowWidth - 5);
						//RenderHelper.DrawVehicle(displayRect, Vehicle, Vehicle.Pattern, true, Vehicle.DrawColor, Vehicle.DrawColorTwo, Vehicle.DrawColorThree);

						if (selectedNode != null)
						{
							selectedNode.node.DrawExtraOnGUI(displayRect);
						}
						else if (highlightedPreMetNode != null)
						{
							highlightedPreMetNode.DrawExtraOnGUI(displayRect);
						}
						Vehicle.CompUpgradeTree.upgradeList.Where(u => u.upgradeActive && u.upgradePurchased && !replaceNodes.Contains(u.upgradeID)).ForEach(u => u.DrawExtraOnGUI(displayRect));

						Widgets.EndGroup();

						if (VehicleMod.settings.debug.debugDrawCannonGrid)
						{
							Widgets.DrawLineHorizontal(displayRect.x, displayRect.y, displayRect.width);
							Widgets.DrawLineHorizontal(displayRect.x, displayRect.y + displayRect.height, displayRect.width);
							Widgets.DrawLineVertical(displayRect.x, displayRect.y, displayRect.height);
							Widgets.DrawLineVertical(displayRect.x + displayRect.width, displayRect.y, displayRect.height);
							if (screenWidth - (displayRect.x + displayRect.width) < 0)
							{
								resize = new Vector2(screenWidth + displayRect.x, screenHeight);
								resizeCheck = true;
							}
						}
						if (VehicleMod.settings.debug.debugDrawNodeGrid)
						{
							float topLineStart = TopPadding + 2;
							Widgets.DrawLineHorizontal(LeftWindowEdge, topLineStart, screenWidth - LeftWindowEdge);
							int lineCount = (int)(screenWidth - LeftWindowEdge) / 10;

							for(int i = 1; i <= lineCount; i++)
							{
								Widgets.DrawLineVertical(LeftWindowEdge + 10 * i, topLineStart, (i % 5 == 0) ? 10 : 5);
							}
							var color = GUI.color;
							GUI.color = Color.red;
							Widgets.DrawLineVertical(LeftWindowEdge + (((screenWidth - BottomDisplayedOffset) - LeftWindowEdge) / 2), topLineStart, 12f);
							GUI.color = color;
						}
					}
					catch(Exception ex)
					{
						Log.ErrorOnce($"Unable to display {Vehicle.def.LabelCap} Texture on Upgrade Screen. Error: {ex.Message} \n StackTrace: {ex.StackTrace}", Vehicle.thingIDNumber);
					}
				}
			}
			
			Widgets.EndGroup();

			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
		}

		protected override void UpdateSize()
		{
			base.UpdateSize();
			if (resizeCheck )
			{
				size = resize;
			}
			else if(size.x != screenWidth || size.y != screenHeight)
			{
				size = new Vector2(screenWidth, screenHeight);
			}
		}

		private void DrawBackgroundGridTop()
		{
			for (int i = 0; i < TotalLinesAcross; i++)
			{
				Widgets.DrawLineVertical(GridSpacing.x + GridSpacing.x * i, TopPadding + GridSpacing.y, TotalLinesDown * UpgradeNodeDim);
				Widgets.Label(new Rect(GridSpacing.x + GridSpacing.x * i - 5f, TopPadding + GridSpacing.y - 20f, 20f, 20f), i.ToString());
			}
		}

		private void DrawBackgroundGridLeft()
		{
			for (int i = 0; i < TotalLinesDown; i++)
			{
				Widgets.DrawLineHorizontal(GridSpacing.x, TopPadding + GridSpacing.y + GridSpacing.y * i, LeftWindowEdge - 1 - GridSpacing.x);
				Widgets.Label(new Rect(GridSpacing.x - 20f, TopPadding + GridSpacing.y + GridSpacing.y * i - 10f, 20f, 20f), i.ToString());
			}
		}

		private void DrawCostItems(SelectedNode selectedNode)
		{
			int i = 0;
			float offset = 0;
			float height = 20;
			float contract = 5;
			Rect nodeRect = selectedNode.detailRect;
			Rect timeRect = new Rect(nodeRect.x + contract, nodeRect.y + nodeRect.height - height * 2, nodeRect.width, height);
			Widgets.Label(timeRect, $"{StatDefOf.WorkToBuild.LabelCap}: {Mathf.CeilToInt(selectedNode.node.Work)}");
			foreach (IngredientFilter ingredient in selectedNode.node.ingredients)
			{
				Rect itemCostRect = new Rect(timeRect)
				{
					x = timeRect.x + offset,
					y = timeRect.y + height - contract,
					width = height
				};
				GUI.DrawTexture(itemCostRect, ingredient.FixedIngredient.uiIcon);
				TooltipHandler.TipRegion(itemCostRect, ingredient.FixedIngredient.LabelCap);
				offset += itemCostRect.width;
				string itemCount = "x" + ingredient.count;
				Vector2 textSize = Text.CalcSize(itemCount);
				Rect itemLabelRect = new Rect(nodeRect.x + offset + contract, itemCostRect.y, textSize.x, height);
				Widgets.Label(itemLabelRect, itemCount);
				i++;
				offset += textSize.x + contract * 2;
			}
		}
		
		private IEnumerable<StatUpgradeCategoryDef> ValidCategories()
		{
			foreach (StatUpgradeCategoryDef statUpgrade in DefDatabase<StatUpgradeCategoryDef>.AllDefsListForReading)
			{
				if (statUpgrade.AppliesToVehicle(Vehicle.VehicleDef))
				{
					yield return statUpgrade;
				}
			}
		}

		private class SelectedNode
		{
			public readonly UpgradeNode node;
			public readonly Rect detailRect;

			public SelectedNode(UpgradeNode node, Rect detailRect)
			{
				this.node = node;
				this.detailRect = detailRect;
			}
		}
	}
}
