using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;

namespace Vehicles.UI
{
	public class ITab_Vehicle_Upgrades : ITab
	{
		private const float MaxArmorValueDisplayed = 120;
		private const float MaxSpeedValueDisplayed = 10;
		private const float MaxCargoValueDisplayed = 10000;
		private const float MaxFuelEffValueDisplayed = 100;
		private const float MaxFuelCapValueDisplayed = 2000;
		public const float TopPadding = 20f;
		public const float EdgePadding = 5f;
		public const float SideDisplayedOffset = 40f;
		public const float BottomDisplayedOffset = 20f;
		public const float TopViewPadding = 20f;
		public const float LeftWindowEdge = 600;
		public const float screenWidth = 850f;
		public const float screenHeight = 700f;
		public const float BottomWindowEdge = screenHeight - 200f;
		public const float TotalIconSizeScalar = 6000f;

		public static readonly Color ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);
		public static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);
		public static readonly Color NotUpgradedColor = new Color(0, 0, 0, 0.5f);
		public static readonly Color PrerequisitesNotMetColor = new Color(0, 0, 0, 0.8f);
		public static readonly Color LockScreenColor = new Color(0, 0, 0, 0.6f);
		public static readonly Vector2 GridSpacing = new Vector2(20f, 20f);
		public static readonly Vector2 GridOrigin = new Vector2(20f, -20f);

		private UpgradeNode selectedNode;
		private Texture2D mainTex;

		private static Vector2 Resize;
		private bool resizeCheck;

		public ITab_Vehicle_Upgrades()
		{
			size = new Vector2(screenWidth, screenHeight);
			labelKey = "TabUpgrades";
		}

		private VehiclePawn SelPawnUpgrade
		{
			get
			{
				if(SelPawn is VehiclePawn vehicle && vehicle.CompUpgradeTree != null)
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

		private Texture2D VehicleTexture
		{
			get
			{
				mainTex = SelPawnUpgrade.VehicleGraphic.TexAt(Rot8.North);
				return mainTex;
			}
		}

		public override void OnOpen()
		{
			base.OnOpen();
			selectedNode = null;
			resizeCheck = false;
			mainTex = null;
		}

		protected override void FillTab()
		{
			Rect rect = new Rect(0f, TopPadding, size.x, size.y - TopPadding);
			Rect rect2 = rect.ContractedBy(10f);

			GUI.BeginGroup(rect2);
			Text.Font = GameFont.Small;
			GUI.color = Color.white;

			UpgradeNode additionalStatNode = null;
			UpgradeNode highlightedPreMetNode = null;

			Rect upgradeButtonRect = new Rect(screenWidth - BottomDisplayedOffset - 80f, BottomWindowEdge - 30f, 75f, 25f);
			Rect cancelButtonRect = new Rect(LeftWindowEdge + 5f, BottomWindowEdge - 30f, 75f, 25f);
			Rect displayRect = new Rect(SelPawnUpgrade.VehicleDef.drawProperties.upgradeUICoord.x, SelPawnUpgrade.VehicleDef.drawProperties.upgradeUICoord.y, SelPawnUpgrade.VehicleDef.drawProperties.upgradeUISize.x, SelPawnUpgrade.VehicleDef.drawProperties.upgradeUISize.y);

			if(VehicleMod.settings.debug.debugDrawNodeGrid)
			{
				DrawBackgroundGrid();
			}
			if(Prefs.DevMode)
			{
				var font = Text.Font;
				Text.Font = GameFont.Tiny;
				Rect screenInfo = new Rect(LeftWindowEdge + 5f, GridSpacing.y + 3f, screenWidth - LeftWindowEdge - 10f, 50f);
				Widgets.Label(screenInfo, $"Screen Size: ({size.x},{size.y}) \nBottomEdge: {BottomWindowEdge}px \nLeftWindowEdge: {LeftWindowEdge}px");
				Text.Font = font;
			}

			if (selectedNode != null && !SelPawnUpgrade.CompUpgradeTree.CurrentlyUpgrading)
			{
				float imageWidth = TotalIconSizeScalar / selectedNode.UpgradeImage.width;
				float imageHeight = TotalIconSizeScalar / selectedNode.UpgradeImage.height;
				Rect selectedRect = new Rect(GridOrigin.x + (GridSpacing.x * selectedNode.GridCoordinate.x) - (imageWidth / 2), GridOrigin.y + (GridSpacing.y * selectedNode.GridCoordinate.z) - (imageHeight / 2) + (TopPadding * 2), imageWidth, imageHeight);
				selectedRect = selectedRect.ExpandedBy(2f);
				GUI.DrawTexture(selectedRect, BaseContent.WhiteTex);
			}

			if(Widgets.ButtonText(upgradeButtonRect, "Upgrade".Translate()) && selectedNode != null && !selectedNode.upgradeActive)
			{
				if(SelPawnUpgrade.CompUpgradeTree.Disabled(selectedNode))
				{
					Messages.Message("DisabledFromOtherNode".Translate(), MessageTypeDefOf.RejectInput, false);
				}
				else if(SelPawnUpgrade.CompUpgradeTree.PrerequisitesMet(selectedNode))
				{
					SoundDefOf.ExecuteTrade.PlayOneShotOnCamera(SelPawnUpgrade.Map);
					SoundDefOf.Building_Complete.PlayOneShot(SelPawnUpgrade);

					SelPawnUpgrade.drafter.Drafted = false;
					if (DebugSettings.godMode)
					{
						SelPawnUpgrade.CompUpgradeTree.FinishUnlock(selectedNode);
					}
					else
					{
						SelPawnUpgrade.CompUpgradeTree.StartUnlock(selectedNode);
					}
					selectedNode.upgradePurchased = true;
					selectedNode = null;
				}
				else
				{
					Messages.Message("MissingPrerequisiteUpgrade".Translate(), MessageTypeDefOf.RejectInput, false);
				}
			}
			
			if(SelPawnUpgrade.CompUpgradeTree.CurrentlyUpgrading)
			{
				if(Widgets.ButtonText(cancelButtonRect, "CancelUpgrade".Translate()))
					SelPawnUpgrade.CompUpgradeTree.CancelUpgrade();
			}
			else if(selectedNode != null && SelPawnUpgrade.CompUpgradeTree.NodeListed(selectedNode).upgradeActive && SelPawnUpgrade.CompUpgradeTree.LastNodeUnlocked(selectedNode))
			{
				if (Widgets.ButtonText(cancelButtonRect, "RefundUpgrade".Translate()))
				{
					SelPawnUpgrade.CompUpgradeTree.RefundUnlock(SelPawnUpgrade.CompUpgradeTree.NodeListed(selectedNode));
					selectedNode = null;
				}
			}
			else
			{
				Widgets.ButtonText(cancelButtonRect, string.Empty);
			}

			foreach(UpgradeNode upgradeNode in SelPawnUpgrade.CompUpgradeTree.upgradeList)
			{
				if(!upgradeNode.prerequisiteNodes.NullOrEmpty())
				{
					foreach(UpgradeNode prerequisite in SelPawnUpgrade.CompUpgradeTree.upgradeList.FindAll(x => upgradeNode.prerequisiteNodes.Contains(x.upgradeID)))
					{
						Vector2 start = new Vector2(GridOrigin.x + (GridSpacing.x * prerequisite.GridCoordinate.x), GridOrigin.y + (GridSpacing.y * prerequisite.GridCoordinate.z) + (TopPadding*2));
						Vector2 end = new Vector2(GridOrigin.x + (GridSpacing.x * upgradeNode.GridCoordinate.x), GridOrigin.y + (GridSpacing.y * upgradeNode.GridCoordinate.z) + (TopPadding*2));
						Color color = Color.grey;
						if(upgradeNode.upgradeActive)
						{
							color = Color.white;
						}
						else if(!string.IsNullOrEmpty(upgradeNode.disableIfUpgradeNodeEnabled))
						{
							try
							{
								UpgradeNode preUpgrade = SelPawnUpgrade.CompUpgradeTree.NodeListed(upgradeNode.disableIfUpgradeNodeEnabled);
								float imageWidth = TotalIconSizeScalar / preUpgrade.UpgradeImage.width;
								float imageHeight = TotalIconSizeScalar / preUpgrade.UpgradeImage.height;
								Rect preUpgradeRect = new Rect(GridOrigin.x + (GridSpacing.x * preUpgrade.GridCoordinate.x) - (imageWidth/2), GridOrigin.y + (GridSpacing.y * preUpgrade.GridCoordinate.z) - (imageHeight/2) + (TopPadding*2), imageWidth, imageHeight);
								if(!SelPawnUpgrade.CompUpgradeTree.CurrentlyUpgrading)
								{
									if (preUpgrade.upgradePurchased)
									{
										color = Color.black;
									}
									else if(Mouse.IsOver(preUpgradeRect))
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
			for(int i = 0; i < SelPawnUpgrade.CompUpgradeTree.upgradeList.Count; i++)
			{
				UpgradeNode upgradeNode = SelPawnUpgrade.CompUpgradeTree.upgradeList[i];
				float imageWidth = TotalIconSizeScalar / upgradeNode.UpgradeImage.width;
				float imageHeight = TotalIconSizeScalar / upgradeNode.UpgradeImage.height;

				Rect upgradeRect = new Rect(GridOrigin.x + (GridSpacing.x * upgradeNode.GridCoordinate.x) - (imageWidth/2), GridOrigin.y + (GridSpacing.y * upgradeNode.GridCoordinate.z) - (imageHeight/2) + (TopPadding*2), imageWidth, imageHeight);
				Widgets.DrawTextureFitted(upgradeRect, upgradeNode.UpgradeImage, 1);
				
				if(!SelPawnUpgrade.CompUpgradeTree.PrerequisitesMet(upgradeNode))
				{
					Widgets.DrawBoxSolid(upgradeRect, PrerequisitesNotMetColor);
				}
				else if(!upgradeNode.upgradeActive || !upgradeNode.upgradePurchased)
				{
					Widgets.DrawBoxSolid(upgradeRect, NotUpgradedColor);
				}

				if(!upgradeNode.prerequisiteNodes.NotNullAndAny())
				{
					if(!string.IsNullOrEmpty(upgradeNode.rootNodeLabel))
					{
						float textWidth = Text.CalcSize(upgradeNode.rootNodeLabel).x;
						Rect nodeLabelRect = new Rect(upgradeRect.x - (textWidth - upgradeRect.width) / 2, upgradeRect.y - 20f, 10f * upgradeNode.rootNodeLabel.Length, 25f);
						Widgets.Label(nodeLabelRect, upgradeNode.rootNodeLabel);
					}
				}
				Rect buttonRect = new Rect(GridOrigin.x + (GridSpacing.x * upgradeNode.GridCoordinate.x) - (imageWidth/2), GridOrigin.y + (GridSpacing.y * upgradeNode.GridCoordinate.z) - (imageHeight/2) + (TopPadding*2), imageWidth, imageHeight);

				Rect infoLabelRect = new Rect(5f, BottomWindowEdge, LeftWindowEdge, 150f);
				GUIStyle infoLabelFont = new GUIStyle(Text.CurFontStyle);
				infoLabelFont.fontStyle = FontStyle.Bold;
				
				if(!SelPawnUpgrade.CompUpgradeTree.CurrentlyUpgrading)
				{
					if(Mouse.IsOver(upgradeRect))
					{
						preDrawingDescriptions = true;

						if(!upgradeNode.upgradePurchased)
						{
							additionalStatNode = upgradeNode;
							highlightedPreMetNode = upgradeNode;
						}
						UIElements.LabelStyled(infoLabelRect, upgradeNode.label, infoLabelFont);

						Widgets.Label(new Rect(infoLabelRect.x, infoLabelRect.y + 20f, infoLabelRect.width, 140f), upgradeNode.informationHighlighted);
					}

					if((Mouse.IsOver(upgradeRect)) && SelPawnUpgrade.CompUpgradeTree.PrerequisitesMet(upgradeNode) && !upgradeNode.upgradeActive)
					{
						GUI.DrawTexture(upgradeRect, TexUI.HighlightTex);
					}

					if(SelPawnUpgrade.CompUpgradeTree.PrerequisitesMet(upgradeNode))
					{
						if(Widgets.ButtonInvisible(buttonRect,true))
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
				}
			}
			Rect selectedLabelRect = new Rect(5f, BottomWindowEdge, LeftWindowEdge, 150f);
			GUIStyle selectedLabelFont = new GUIStyle(Text.CurFontStyle);
			selectedLabelFont.fontStyle = FontStyle.Bold;

			if(selectedNode != null && !preDrawingDescriptions)
			{
				if(!selectedNode.upgradePurchased)
					additionalStatNode = selectedNode;
					
				UIElements.LabelStyled(selectedLabelRect, selectedNode.label, selectedLabelFont);

				Widgets.Label(new Rect(selectedLabelRect.x, selectedLabelRect.y + 20f, selectedLabelRect.width, 140f), selectedNode.informationHighlighted);
			}

			Rect labelRect = new Rect(0f, 0f, rect2.width - 16f, 20f);

			if(!VehicleMod.settings.debug.debugDrawNodeGrid)
			{
				Widgets.Label(labelRect, SelPawnUpgrade.Label);
			}
			
			Color lineColor = GUI.color;
			GUI.color = new Color(0.3f, 0.3f, 0.3f, 1f);

			Widgets.DrawLineHorizontal(0, TopViewPadding, screenWidth);
			Widgets.DrawLineHorizontal(0, BottomWindowEdge, screenWidth);
			Widgets.DrawLineHorizontal(0, screenHeight - SideDisplayedOffset - 1f, screenWidth);

			Widgets.DrawLineVertical(0, TopViewPadding, screenHeight);
			Widgets.DrawLineVertical(screenWidth - BottomDisplayedOffset - 1f, TopViewPadding, screenHeight);

			if (VehicleMod.settings.main.drawUpgradeInformationScreen)
			{
				Widgets.DrawLineVertical(LeftWindowEdge, TopViewPadding, screenHeight);
			}
			GUI.color = lineColor;

			if(VehicleMod.settings.main.drawUpgradeInformationScreen)
			{
				if(SelPawnUpgrade != null)
				{
					try
					{
						Vector2 display = SelPawnUpgrade.VehicleDef.drawProperties.upgradeUICoord;
						float UISizeX = SelPawnUpgrade.VehicleDef.drawProperties.upgradeUISize.x;
						float UISizeY = SelPawnUpgrade.VehicleDef.drawProperties.upgradeUISize.y;
						RenderHelper.DrawVehicleTex(new Rect(display.x, display.y, UISizeX, UISizeY), VehicleTexture, SelPawnUpgrade, 
							SelPawnUpgrade.pattern, true, SelPawnUpgrade.DrawColor, SelPawnUpgrade.DrawColorTwo, SelPawnUpgrade.DrawColorThree);

						if(VehicleMod.settings.debug.debugDrawCannonGrid)
						{
							Widgets.DrawLineHorizontal(displayRect.x, displayRect.y, displayRect.width);
							Widgets.DrawLineHorizontal(displayRect.x, displayRect.y + displayRect.height, displayRect.width);
							Widgets.DrawLineVertical(displayRect.x, displayRect.y, displayRect.height);
							Widgets.DrawLineVertical(displayRect.x + displayRect.width, displayRect.y, displayRect.height);
							if(screenWidth - (displayRect.x + displayRect.width) < 0)
							{
								Resize = new Vector2(screenWidth + displayRect.x, screenHeight);
								resizeCheck = true;
							}
						}
						if(VehicleMod.settings.debug.debugDrawNodeGrid)
						{
							Widgets.DrawLineHorizontal(LeftWindowEdge, 70f, screenWidth - LeftWindowEdge);
							int lineCount = (int)(screenWidth - LeftWindowEdge) / 10;
							
							for(int i = 1; i <= lineCount; i++)
							{
								Widgets.DrawLineVertical(LeftWindowEdge + 10 * i, 70f, (i % 5 == 0) ? 10 : 5);
							}
							var color = GUI.color;
							GUI.color = Color.red;
							Widgets.DrawLineVertical(LeftWindowEdge + (((screenWidth - BottomDisplayedOffset) - LeftWindowEdge) / 2), 70f, 12f);
							GUI.color = color;
						}

						SelPawnUpgrade.CompUpgradeTree.upgradeList.Where(u => u.upgradePurchased && u.upgradeActive).ForEach(u => u.DrawExtraOnGUI(new Rect(display.x, display.y, UISizeX, UISizeY)));
						if (selectedNode != null)
						{
							selectedNode.DrawExtraOnGUI(new Rect(display.x, display.y, UISizeX, UISizeY));
						}
						if (highlightedPreMetNode != null)
						{
							highlightedPreMetNode.DrawExtraOnGUI(new Rect(display.x, display.y, UISizeX, UISizeY));
						}
					}
					catch(Exception ex)
					{
						Log.ErrorOnce($"Unable to display {SelPawnUpgrade.def.LabelCap} Texture on Upgrade Screen. Error: {ex.Message} \n StackTrace: {ex.StackTrace}", SelPawnUpgrade.thingIDNumber);
					}
				}
			}
			if (additionalStatNode != null)
				DrawCostItems(additionalStatNode);
			else if (selectedNode != null)
				DrawCostItems(selectedNode);

			DrawStats(additionalStatNode as StatUpgrade);
			
			if(SelPawnUpgrade.CompUpgradeTree.CurrentlyUpgrading)
			{
				Rect greyedViewRect = new Rect(0, TopViewPadding, LeftWindowEdge, BottomWindowEdge - TopViewPadding);
				Widgets.DrawBoxSolid(greyedViewRect, LockScreenColor);
				Rect greyedLabelRect = new Rect(LeftWindowEdge / 2 - 17f, (BottomWindowEdge - TopViewPadding) / 2, 100f, 100f);
				string timeFormatted = VehicleMod.settings.main.useInGameTime ? RimWorldTime.TicksToGameTime(SelPawnUpgrade.CompUpgradeTree.TimeLeftUpgrading) : RimWorldTime.TicksToRealTime(SelPawnUpgrade.CompUpgradeTree.TimeLeftUpgrading);
				timeFormatted = SelPawnUpgrade.CompUpgradeTree.TimeLeftUpgrading.ToString();
				Widgets.Label(greyedLabelRect, timeFormatted);

				DrawCostItems(null);
			}

			GUI.EndGroup();

			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
		}

		protected override void UpdateSize()
		{
			base.UpdateSize();
			if(resizeCheck )
			{
				size = Resize;
			}
			else if(size.x != screenWidth || size.y != screenHeight)
			{
				size = new Vector2(screenWidth, screenHeight);
			}
		}

		private void DrawBackgroundGrid()
		{
			var color = GUI.color;
			GUI.color = new Color(0.3f, 0.3f, 0.3f, 1f);
			for(int i = 0; i < 29; i++)
			{
				if (i < 25)
				{
					Widgets.DrawLineHorizontal(GridSpacing.x, GridSpacing.y + GridSpacing.y * i, LeftWindowEdge - 1 - GridSpacing.x);
					Widgets.Label(new Rect(GridSpacing.x - 20f, GridSpacing.y + GridSpacing.y * i - 10f, 20f, 20f), i.ToString());
				}
				Widgets.DrawLineVertical(GridSpacing.x + GridSpacing.x * i, GridSpacing.y, BottomWindowEdge - 1 - GridSpacing.y);
				Widgets.Label(new Rect(GridSpacing.x + GridSpacing.x * i  - 5f, GridSpacing.y - 20f, 20f, 20f), i.ToString());
			}
			GUI.color = color;
		}

		private void DrawCostItems(UpgradeNode node = null)
		{
			int i = 0;
			float offset = 0;

			if(node is null)
			{
				offset = 25f;
				foreach(IngredientFilter ingredient in SelPawnUpgrade.CompUpgradeTree.NodeUnlocking.ingredients.Where(i => i.IsFixedIngredient))
				{
					string itemCount = $"{SelPawnUpgrade.CompUpgradeTree.NodeUnlocking.itemContainer.TotalStackCountOfDef(ingredient.FixedIngredient)}/{ingredient.count}";
					Vector2 textSize = Text.CalcSize(itemCount);

					Rect itemCostRect = new Rect( (LeftWindowEdge / 2) - (textSize.x / 2), ((BottomWindowEdge - TopViewPadding) / 2) + offset, 20f, 20f);
					GUI.DrawTexture(itemCostRect, ingredient.FixedIngredient.uiIcon);
					
					Rect itemLabelRect = new Rect(itemCostRect.x + 25f, itemCostRect.y, textSize.x, 20f);
					Widgets.Label(itemLabelRect, itemCount);
					i++;
					offset += textSize.y + 5;
				}
			}
			else
			{
				Rect timeRect = new Rect(5f, screenHeight - SideDisplayedOffset * 2, LeftWindowEdge, 20f);
				string timeForUpgrade = VehicleMod.settings.main.useInGameTime ? RimWorldTime.TicksToGameTime(node.UpgradeTimeParsed) : RimWorldTime.TicksToRealTime(node.UpgradeTimeParsed);
				Widgets.Label(timeRect, timeForUpgrade);
				foreach(IngredientFilter ingredient in node.ingredients)
				{
					Rect itemCostRect = new Rect(5f + offset, screenHeight - SideDisplayedOffset - 23f, 20f, 20f);
					GUI.DrawTexture(itemCostRect, ingredient.FixedIngredient.uiIcon);
					string itemCount = "x" + ingredient.count;
					Vector2 textSize = Text.CalcSize(itemCount);
					Rect itemLabelRect = new Rect(25f + offset, itemCostRect.y, textSize.x, 20f);
					Widgets.Label(itemLabelRect, itemCount);
					i++;
					offset += textSize.x + 30f;
				}
			}
		}

		private void DrawStats(StatUpgrade node)
		{
			float barWidth = screenWidth - LeftWindowEdge - EdgePadding * 2 - BottomDisplayedOffset - 1f; //219

			foreach (StatUpgradeCategoryDef statUpgrade in ValidCategories())
			{
				float hoveredValue = 0f;

				if (node != null && node.values.TryGetValue(statUpgrade, out float value))
				{
					hoveredValue = value;
				}

				//Rect armorStatRect = new Rect(LeftWindowEdge + EdgePadding, BottomWindowEdge + EdgePadding, barWidth, 20f);
				//HelperMethods.FillableBarLabeled(armorStatRect, SelPawnUpgrade.ArmorPoints / MaxArmorValueDisplayed, "VehicleMaxArmor".Translate(), StatUpgradeCategory.Armor,
				//    HelperMethods.FillableBarInnerTex, HelperMethods.FillableBarBackgroundTex, SelPawnUpgrade.ArmorPoints, armorAdded, armorAdded / MaxArmorValueDisplayed);

				//Rect speedStatRect = new Rect(LeftWindowEdge + EdgePadding, armorStatRect.y + 25f, barWidth, 20f);
				//HelperMethods.FillableBarLabeled(speedStatRect, SelPawnUpgrade.ActualMoveSpeed / MaxSpeedValueDisplayed, "VehicleMaxSpeed".Translate(), StatUpgradeCategory.Speed,
				//    HelperMethods.FillableBarInnerTex, HelperMethods.FillableBarBackgroundTex, SelPawnUpgrade.ActualMoveSpeed, speedAdded, speedAdded / MaxSpeedValueDisplayed);

				//Rect cargoCapacityStatRect = new Rect(LeftWindowEdge + EdgePadding, speedStatRect.y + 25f, barWidth, 20f);
				//HelperMethods.FillableBarLabeled(cargoCapacityStatRect, SelPawnUpgrade.CargoCapacity / MaxCargoValueDisplayed, "VehicleCargoCapacity".Translate(), StatUpgradeCategory.CargoCapacity,
				//    HelperMethods.FillableBarInnerTex, HelperMethods.FillableBarBackgroundTex, SelPawnUpgrade.CargoCapacity, cargoAdded, cargoAdded / MaxCargoValueDisplayed);

				//if (SelPawnUpgrade.CompFueledTravel != null)
				//{
				//    Rect fuelEfficiencyStatRect = new Rect(LeftWindowEdge + EdgePadding, cargoCapacityStatRect.y + 25f, barWidth, 20f);
				//    HelperMethods.FillableBarLabeled(fuelEfficiencyStatRect, SelPawnUpgrade.CompFueledTravel.FuelEfficiency / MaxFuelEffValueDisplayed,
				//        "VehicleFuelCost".Translate(), StatUpgradeCategory.FuelConsumptionRate, HelperMethods.FillableBarInnerTex, HelperMethods.FillableBarBackgroundTex,
				//        SelPawnUpgrade.CompFueledTravel.FuelEfficiency, fuelEffAdded, fuelEffAdded / MaxFuelEffValueDisplayed);

				//    Rect fuelCapacityStatRect = new Rect(LeftWindowEdge + EdgePadding, fuelEfficiencyStatRect.y + 25f, barWidth, 20f);
				//    HelperMethods.FillableBarLabeled(fuelCapacityStatRect, SelPawnUpgrade.CompFueledTravel.FuelCapacity / MaxFuelCapValueDisplayed,
				//        "VehicleFuelCapacity".Translate(), StatUpgradeCategory.FuelCapacity, HelperMethods.FillableBarInnerTex, HelperMethods.FillableBarBackgroundTex,
				//        SelPawnUpgrade.CompFueledTravel.FuelCapacity, fuelCapAdded, fuelCapAdded / MaxFuelCapValueDisplayed);
				//}
			}
		}
		
		private IEnumerable<StatUpgradeCategoryDef> ValidCategories()
		{
			foreach (StatUpgradeCategoryDef statUpgrade in DefDatabase<StatUpgradeCategoryDef>.AllDefs)
			{
				if (statUpgrade.AppliesToVehicle(SelPawnUpgrade.VehicleDef))
				{
					yield return statUpgrade;
				}
			}
		}
	}
}
