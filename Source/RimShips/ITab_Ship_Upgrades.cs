using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;
using SPExtended;

namespace RimShips.UI
{
    public class ITab_Ship_Upgrades : ITab
    {
        public ITab_Ship_Upgrades()
        {
            this.size = new Vector2(screenWidth, screenHeight);
            this.labelKey = "TabUpgrades";
        }

        private Pawn SelPawnUpgrade
        {
            get
            {
                if(SelPawn is null || SelPawn.TryGetComp<CompUpgradeTree>() != null)
                {
                    return SelPawn;
                }
                throw new InvalidOperationException("Upgrade ITab on Pawn without CompUpgradeTree: " + SelThing);
            }
        }

        private CompUpgradeTree Comp
        {
            get
            {
                return SelPawnUpgrade.GetComp<CompUpgradeTree>();
            }
        }

        private CompShips BoatComp
        {
            get
            {
                return SelPawnUpgrade.GetComp<CompShips>();
            }
        }

        public override void OnOpen()
        {
            base.OnOpen();
            selectedNode = null;
            resizeCheck = false;
        }

        protected override void FillTab()
        {
            Rect rect = new Rect(0f, TopPadding, size.x, size.y - TopPadding);
            Rect rect2 = rect.ContractedBy(10f);

            GUI.BeginGroup(rect2);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            float num = 0f;

            UpgradeNode additionalStatNode = null;
            UpgradeNode highlightedPreMetNode = null;

            Rect upgradeButtonRect = new Rect(screenWidth - BottomDisplayedOffset - 80f, BottomWindowEdge - 30f, 75, 25f);
            Rect displayRect = new Rect(Comp.Props.displayUICoord.x, Comp.Props.displayUICoord.y, Comp.Props.displayUISize.x, Comp.Props.displayUISize.y);

            if(RimShipMod.mod.settings.debugDrawNodeGrid)
            {
                DrawBackgroundGrid();
            }
            if(Prefs.DevMode)
            {
                var font = Text.Font;
                Text.Font = GameFont.Tiny;
                Rect screenInfo = new Rect(LeftWindowEdge + 5f, GridSpacing.y + 3f, screenWidth - LeftWindowEdge - 10f, 50f);
                Widgets.Label(screenInfo, $"Screen Size: ({this.size.x},{this.size.y}) \nBottomEdge: {BottomWindowEdge}px \nLeftWindowEdge: {LeftWindowEdge}px");
                Text.Font = font;
            }

            if (selectedNode != null)
            {
                float imageWidth = TotalIconSizeScalar / selectedNode.UpgradeImage.width;
                float imageHeight = TotalIconSizeScalar / selectedNode.UpgradeImage.height;
                Rect selectedRect = new Rect(GridOrigin.x + (GridSpacing.x * selectedNode.GridCoordinate.x) - (imageWidth / 2), GridOrigin.y + (GridSpacing.y * selectedNode.GridCoordinate.z) - (imageHeight / 2) + (TopPadding * 2), imageWidth, imageHeight);
                selectedRect.ContractedBy(3f);
                GUI.DrawTexture(selectedRect, BaseContent.WhiteTex);
            }

            if(Widgets.ButtonText(upgradeButtonRect, "Upgrade".Translate()) && selectedNode != null && !selectedNode.upgradeActive)
            {
                if(Comp.Disabled(selectedNode))
                {
                    Messages.Message("DisabledFromOtherNode".Translate(), MessageTypeDefOf.RejectInput, false);
                }
                else if(Comp.PrerequisitesMet(selectedNode))
                {
                    SoundDefOf.ExecuteTrade.PlayOneShotOnCamera(SelPawnUpgrade.Map);
                    SoundDefOf.Building_Complete.PlayOneShot(SelPawnUpgrade);

                    Comp.StartUnlock(selectedNode);
                    selectedNode.upgradePurchased = true;
                    selectedNode = null;
                }
                else
                {
                    Messages.Message("MissingPrerequisiteUpgrade".Translate(), MessageTypeDefOf.RejectInput, false);
                }
            }

            foreach(UpgradeNode upgradeNode in Comp.upgradeList)
            {
                if(upgradeNode.prerequisiteNodes?.Any() ?? false)
                {
                    foreach(UpgradeNode prerequisite in Comp.upgradeList.FindAll(x => upgradeNode.prerequisiteNodes.Contains(x.upgradeID)))
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
                                UpgradeNode preUpgrade = Comp.upgradeList.First(x => x.upgradeID == upgradeNode.disableIfUpgradeNodeEnabled);
                                float imageWidth = TotalIconSizeScalar / preUpgrade.UpgradeImage.width;
                                float imageHeight = TotalIconSizeScalar / preUpgrade.UpgradeImage.height;
                                Rect preUpgradeRect = new Rect(GridOrigin.x + (GridSpacing.x * preUpgrade.GridCoordinate.x) - (imageWidth/2), GridOrigin.y + (GridSpacing.y * preUpgrade.GridCoordinate.z) - (imageHeight/2) + (TopPadding*2), imageWidth, imageHeight);
                                if(preUpgrade.upgradePurchased)
                                {
                                    color = Color.black;
                                }
                                else if(Mouse.IsOver(preUpgradeRect))
                                {
                                    color = Color.red;
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
            for(int i = 0; i < Comp.upgradeList.Count; i++)
            {
                UpgradeNode upgradeNode = Comp.upgradeList[i];
                float imageWidth = TotalIconSizeScalar / upgradeNode.UpgradeImage.width;
                float imageHeight = TotalIconSizeScalar / upgradeNode.UpgradeImage.height;

                Rect upgradeRect = new Rect(GridOrigin.x + (GridSpacing.x * upgradeNode.GridCoordinate.x) - (imageWidth/2), GridOrigin.y + (GridSpacing.y * upgradeNode.GridCoordinate.z) - (imageHeight/2) + (TopPadding*2), imageWidth, imageHeight);
                Widgets.DrawTextureFitted(upgradeRect, upgradeNode.UpgradeImage, 1);
                
                if(!upgradeNode.prerequisiteNodes.Any())
                {
                    if(!string.IsNullOrEmpty(upgradeNode.rootNodeLabel))
                    {
                        Rect nodeLabelRect = new Rect(upgradeRect.x, upgradeRect.y - 20f, 10f * upgradeNode.rootNodeLabel.Count(), 25f);
                        Widgets.Label(nodeLabelRect, upgradeNode.rootNodeLabel);
                    }
                }
                Rect buttonRect = new Rect(GridOrigin.x + (GridSpacing.x * upgradeNode.GridCoordinate.x) - (imageWidth/2), GridOrigin.y + (GridSpacing.y * upgradeNode.GridCoordinate.z) - (imageHeight/2) + (TopPadding*2), imageWidth, imageHeight);

                Rect infoLabelRect = new Rect(5f, BottomWindowEdge, LeftWindowEdge, 150f);
                GUIStyle infoLabelFont = new GUIStyle(Text.CurFontStyle);
                infoLabelFont.fontStyle = FontStyle.Bold;
                
                if(Mouse.IsOver(upgradeRect))
                {
                    preDrawingDescriptions = true;

                    if(!upgradeNode.upgradePurchased)
                    {
                        additionalStatNode = upgradeNode;
                        highlightedPreMetNode = upgradeNode;
                    }
                    HelperMethods.LabelStyled(infoLabelRect, upgradeNode.label, infoLabelFont);

                    Widgets.Label(new Rect(infoLabelRect.x, infoLabelRect.y + 20f, infoLabelRect.width, 140f), upgradeNode.informationHighlighted);
                }

                if((Mouse.IsOver(upgradeRect) || upgradeNode.upgradePurchased) && Comp.PrerequisitesMet(upgradeNode))
                {
                    GUI.DrawTexture(upgradeRect, TexUI.HighlightTex);
                }

                if(!upgradeNode.upgradePurchased && Comp.PrerequisitesMet(upgradeNode))
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
            Rect selectedLabelRect = new Rect(5f, BottomWindowEdge, LeftWindowEdge, 150f);
            GUIStyle selectedLabelFont = new GUIStyle(Text.CurFontStyle);
            selectedLabelFont.fontStyle = FontStyle.Bold;

            if(selectedNode != null && !preDrawingDescriptions)
            {
                if(!selectedNode.upgradePurchased)
                    additionalStatNode = selectedNode;
                    
                HelperMethods.LabelStyled(selectedLabelRect, selectedNode.label, selectedLabelFont);

                Widgets.Label(new Rect(selectedLabelRect.x, selectedLabelRect.y + 20f, selectedLabelRect.width, 140f), selectedNode.informationHighlighted);
            }

            Rect labelRect = new Rect(0f, 0f, rect2.width - 16f, 20f);
            num += labelRect.height;
            if(!RimShipMod.mod.settings.debugDrawNodeGrid)
            {
                Widgets.Label(labelRect, SelPawnUpgrade.Label);
            }
            
            Color lineColor = GUI.color;
            GUI.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            Widgets.DrawLineHorizontal(0, num, screenWidth);
            Widgets.DrawLineHorizontal(0, BottomWindowEdge, screenWidth);
            Widgets.DrawLineHorizontal(0, screenHeight - SideDisplayedOffset - 1f, screenWidth);

            Widgets.DrawLineVertical(0, num, screenHeight);
            Widgets.DrawLineVertical(screenWidth - BottomDisplayedOffset - 1f, num, screenHeight);
            
            if(RimShipMod.mod.settings.drawUpgradeInformationScreen)
                Widgets.DrawLineVertical(LeftWindowEdge, num, screenHeight);
            GUI.color = lineColor;

            if(RimShipMod.mod.settings.drawUpgradeInformationScreen)
            {
                if(SelPawnUpgrade != null)
                {
                    try
                    {
                        Texture2D tex = ContentFinder<Texture2D>.Get(SelPawnUpgrade.kindDef.lifeStages.FirstOrDefault().bodyGraphicData.texPath + "_north", true);
                        GUI.DrawTexture(displayRect, tex);

                        if(RimShipMod.mod.settings.debugDrawCannonGrid)
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
                        if(RimShipMod.mod.settings.debugDrawNodeGrid)
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

                        foreach(CannonHandler cannon in Comp.upgradeList.Where(x => x.upgradeActive && (x.cannonsUnlocked?.Any() ?? false)).SelectMany(y => y.cannonsUnlocked))
                        {
                            PawnKindLifeStage biggestStage = SelPawnUpgrade.kindDef.lifeStages.MaxBy(x => x.bodyGraphicData?.drawSize ?? Vector2.zero);
                            float baseWidth = (Comp.Props.displayUISize.x / biggestStage.bodyGraphicData.drawSize.x) * cannon.baseCannonDrawSize.x;
                            float baseHeight = (Comp.Props.displayUISize.y / biggestStage.bodyGraphicData.drawSize.y) * cannon.baseCannonDrawSize.y;

                            float xBase = displayRect.x + (displayRect.width / 2) - (baseWidth / 2) - ( (Comp.Props.displayUISize.x / biggestStage.bodyGraphicData.drawSize.x) * cannon.baseCannonRenderLocation.x);
                            float yBase = displayRect.y + (displayRect.height / 2) - (baseHeight / 2) - ( (Comp.Props.displayUISize.y / biggestStage.bodyGraphicData.drawSize.y) * cannon.baseCannonRenderLocation.y);

                            Rect baseCannonDrawnRect = new Rect(xBase, yBase, baseWidth, baseHeight);
                            GUI.DrawTexture(baseCannonDrawnRect, cannon.CannonBaseTexture);

                            float cannonWidth = (Comp.Props.displayUISize.x / biggestStage.bodyGraphicData.drawSize.x) * cannon.cannonTurretDrawSize.x;
                            float cannonHeight = (Comp.Props.displayUISize.y / biggestStage.bodyGraphicData.drawSize.y) * cannon.cannonTurretDrawSize.y;

                            float xCannon = displayRect.x + (displayRect.width / 2) - (cannonWidth / 2) - ( (Comp.Props.displayUISize.x / biggestStage.bodyGraphicData.drawSize.x) * cannon.cannonRenderLocation.x);
                            float yCannon = displayRect.y + (displayRect.height / 2) - (cannonHeight / 2) - ( (Comp.Props.displayUISize.y / biggestStage.bodyGraphicData.drawSize.y) * cannon.cannonRenderLocation.y);

                            Rect cannonDrawnRect = new Rect(xCannon, yCannon, cannonWidth, cannonHeight);
                            GUI.DrawTexture(cannonDrawnRect, cannon.CannonTexture);

                            if(RimShipMod.mod.settings.debugDrawCannonGrid)
                            {
                                Widgets.DrawLineHorizontal(cannonDrawnRect.x, cannonDrawnRect.y, cannonDrawnRect.width);
                                Widgets.DrawLineHorizontal(cannonDrawnRect.x, cannonDrawnRect.y + cannonDrawnRect.height, cannonDrawnRect.width);
                                Widgets.DrawLineVertical(cannonDrawnRect.x, cannonDrawnRect.y, cannonDrawnRect.height);
                                Widgets.DrawLineVertical(cannonDrawnRect.x + cannonDrawnRect.width, cannonDrawnRect.y, cannonDrawnRect.height);
                            }
                        }
                        if(selectedNode?.cannonsUnlocked?.Any() ?? false)
                        {
                            foreach(CannonHandler cannon in selectedNode.cannonsUnlocked)
                            {
                                PawnKindLifeStage biggestStage = SelPawnUpgrade.kindDef.lifeStages.MaxBy(x => x.bodyGraphicData?.drawSize ?? Vector2.zero);
                                float baseWidth = (Comp.Props.displayUISize.x / biggestStage.bodyGraphicData.drawSize.x) * cannon.baseCannonDrawSize.x;
                                float baseHeight = (Comp.Props.displayUISize.y / biggestStage.bodyGraphicData.drawSize.y) * cannon.baseCannonDrawSize.y;
                                float test = ((Comp.Props.displayUISize.y / biggestStage.bodyGraphicData.drawSize.y) * cannon.baseCannonRenderLocation.y);

                                float xBase = displayRect.x + (displayRect.width / 2) - (baseWidth / 2) - ( (Comp.Props.displayUISize.x / biggestStage.bodyGraphicData.drawSize.x) * cannon.baseCannonRenderLocation.x);
                                float yBase = displayRect.y + (displayRect.height / 2) - (baseHeight / 2) - ( (Comp.Props.displayUISize.y / biggestStage.bodyGraphicData.drawSize.y) * cannon.baseCannonRenderLocation.y);

                                Rect baseCannonDrawnRect = new Rect(xBase, yBase, baseWidth, baseHeight);
                                GUI.DrawTexture(baseCannonDrawnRect, cannon.CannonBaseTexture);

                                float cannonWidth = (Comp.Props.displayUISize.x / biggestStage.bodyGraphicData.drawSize.x) * cannon.cannonTurretDrawSize.x;
                                float cannonHeight = (Comp.Props.displayUISize.y / biggestStage.bodyGraphicData.drawSize.y) * cannon.cannonTurretDrawSize.y;

                                float xCannon = displayRect.x + (displayRect.width / 2) - (cannonWidth / 2) - ( (Comp.Props.displayUISize.x / biggestStage.bodyGraphicData.drawSize.x) * cannon.cannonRenderLocation.x);
                                float yCannon = displayRect.y + (displayRect.height / 2) - (cannonHeight / 2) - ( (Comp.Props.displayUISize.y / biggestStage.bodyGraphicData.drawSize.y) * cannon.cannonRenderLocation.y);

                                Rect cannonDrawnRect = new Rect(xCannon, yCannon, cannonWidth, cannonHeight);
                                GUI.DrawTexture(cannonDrawnRect, cannon.CannonTexture);

                                if(RimShipMod.mod.settings.debugDrawCannonGrid)
                                {
                                    Widgets.DrawLineHorizontal(cannonDrawnRect.x, cannonDrawnRect.y, cannonDrawnRect.width);
                                    Widgets.DrawLineHorizontal(cannonDrawnRect.x, cannonDrawnRect.y + cannonDrawnRect.height, cannonDrawnRect.width);
                                    Widgets.DrawLineVertical(cannonDrawnRect.x, cannonDrawnRect.y, cannonDrawnRect.height);
                                    Widgets.DrawLineVertical(cannonDrawnRect.x + cannonDrawnRect.width, cannonDrawnRect.y, cannonDrawnRect.height);
                                }
                            }
                        }
                        else if((highlightedPreMetNode?.cannonsUnlocked?.Any() ?? false) && !highlightedPreMetNode.upgradeActive)
                        {
                            foreach(CannonHandler cannon in highlightedPreMetNode.cannonsUnlocked)
                            {
                                PawnKindLifeStage biggestStage = SelPawnUpgrade.kindDef.lifeStages.MaxBy(x => x.bodyGraphicData?.drawSize ?? Vector2.zero);
                                float baseWidth = (Comp.Props.displayUISize.x / biggestStage.bodyGraphicData.drawSize.x) * cannon.baseCannonDrawSize.x;
                                float baseHeight = (Comp.Props.displayUISize.y / biggestStage.bodyGraphicData.drawSize.y) * cannon.baseCannonDrawSize.y;

                                float xBase = displayRect.x + (displayRect.width / 2) - (baseWidth / 2) - ( (Comp.Props.displayUISize.x / biggestStage.bodyGraphicData.drawSize.x) * cannon.baseCannonRenderLocation.x);
                                float yBase = displayRect.y + (displayRect.height / 2) - (baseHeight / 2) - ( (Comp.Props.displayUISize.y / biggestStage.bodyGraphicData.drawSize.y) * cannon.baseCannonRenderLocation.y);

                                Rect baseCannonDrawnRect = new Rect(xBase, yBase, baseWidth, baseHeight);
                                GUI.DrawTexture(baseCannonDrawnRect, cannon.CannonBaseTexture);

                                float cannonWidth = (Comp.Props.displayUISize.x / biggestStage.bodyGraphicData.drawSize.x) * cannon.cannonTurretDrawSize.x;
                                float cannonHeight = (Comp.Props.displayUISize.y / biggestStage.bodyGraphicData.drawSize.y) * cannon.cannonTurretDrawSize.y;

                                float xCannon = displayRect.x + (displayRect.width / 2) - (cannonWidth / 2) - ( (Comp.Props.displayUISize.x / biggestStage.bodyGraphicData.drawSize.x) * cannon.cannonRenderLocation.x);
                                float yCannon = displayRect.y + (displayRect.height / 2) - (cannonHeight / 2) - ( (Comp.Props.displayUISize.y / biggestStage.bodyGraphicData.drawSize.y) * cannon.cannonRenderLocation.y);

                                Rect cannonDrawnRect = new Rect(xCannon, yCannon, cannonWidth, cannonHeight);
                                GUI.DrawTexture(cannonDrawnRect, cannon.CannonTexture);

                                if(RimShipMod.mod.settings.debugDrawCannonGrid)
                                {
                                    Widgets.DrawLineHorizontal(cannonDrawnRect.x, cannonDrawnRect.y, cannonDrawnRect.width);
                                    Widgets.DrawLineHorizontal(cannonDrawnRect.x, cannonDrawnRect.y + cannonDrawnRect.height, cannonDrawnRect.width);
                                    Widgets.DrawLineVertical(cannonDrawnRect.x, cannonDrawnRect.y, cannonDrawnRect.height);
                                    Widgets.DrawLineVertical(cannonDrawnRect.x + cannonDrawnRect.width, cannonDrawnRect.y, cannonDrawnRect.height);
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Log.ErrorOnce($"Unable to display {SelPawnUpgrade.def.LabelCap} Texture on Upgrade Screen. Error: {ex.Message} \n StackTrace: {ex.StackTrace}", SelPawnUpgrade.thingIDNumber, true);
                    }
                }
            }

            DrawStats(additionalStatNode);
            
            GUI.EndGroup();

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();
            if(resizeCheck )
            {
                this.size = Resize;
            }
            else if(size.x != screenWidth || size.y != screenHeight)
            {
                this.size = new Vector2(screenWidth, screenHeight);
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

        private void DrawStats(UpgradeNode nodeRect)
        {
            float barWidth = screenWidth - LeftWindowEdge - EdgePadding * 2 - BottomDisplayedOffset - 1f; //219

            float armorAdded = 0f;
            float speedAdded = 0f;
            float cargoAdded = 0f;
            float fuelEffAdded = 0f;
            float fuelCapAdded = 0f;

            if(nodeRect != null)
            {
                foreach(KeyValuePair<StatUpgrade,float> stat in nodeRect.values)
                {
                    switch(stat.Key)
                    {
                        case StatUpgrade.Armor:
                            armorAdded = stat.Value;
                            break;
                        case StatUpgrade.Speed:
                            speedAdded = stat.Value;
                            break;
                        case StatUpgrade.CargoCapacity:
                            cargoAdded = stat.Value;
                            break;
                        case StatUpgrade.FuelConsumptionRate:
                            fuelEffAdded = stat.Value;
                            break;
                        case StatUpgrade.FuelCapacity:
                            fuelCapAdded = stat.Value;
                            break;
                    }
                }
            }
            
            Rect armorStatRect = new Rect(LeftWindowEdge + EdgePadding, BottomWindowEdge + EdgePadding, barWidth, 20f);
            HelperMethods.FillableBarLabeled(armorStatRect,  BoatComp.ArmorPoints / MaxArmorValueDisplayed, "BoatMaxArmor".Translate(), StatUpgrade.Armor, HelperMethods.FillableBarInnerTex, HelperMethods.FillableBarBackgroundTex, BoatComp.ArmorPoints, armorAdded,  armorAdded / MaxArmorValueDisplayed);

            Rect speedStatRect = new Rect(LeftWindowEdge + EdgePadding, armorStatRect.y + 25f, barWidth, 20f);
            HelperMethods.FillableBarLabeled(speedStatRect, BoatComp.ActualMoveSpeed / MaxSpeedValueDisplayed, "BoatMaxSpeed".Translate(), StatUpgrade.Speed, HelperMethods.FillableBarInnerTex, HelperMethods.FillableBarBackgroundTex, BoatComp.ActualMoveSpeed, speedAdded, speedAdded / MaxSpeedValueDisplayed);

            Rect cargoCapacityStatRect = new Rect(LeftWindowEdge + EdgePadding, speedStatRect.y + 25f, barWidth, 20f);
            HelperMethods.FillableBarLabeled(cargoCapacityStatRect, BoatComp.CargoCapacity / MaxCargoValueDisplayed, "BoatCargoCapacity".Translate(), StatUpgrade.CargoCapacity, HelperMethods.FillableBarInnerTex, HelperMethods.FillableBarBackgroundTex, BoatComp.CargoCapacity, cargoAdded, cargoAdded / MaxCargoValueDisplayed);

            if(SelPawnUpgrade.TryGetComp<CompFueledTravel>() != null)
            {
                Rect fuelEfficiencyStatRect = new Rect(LeftWindowEdge + EdgePadding, cargoCapacityStatRect.y + 25f, barWidth, 20f);
                HelperMethods.FillableBarLabeled(fuelEfficiencyStatRect, SelPawnUpgrade.GetComp<CompFueledTravel>().FuelEfficiency / MaxFuelEffValueDisplayed, 
                    "BoatFuelCost".Translate(), StatUpgrade.FuelConsumptionRate, HelperMethods.FillableBarInnerTex, HelperMethods.FillableBarBackgroundTex, SelPawnUpgrade.GetComp<CompFueledTravel>().FuelEfficiency, fuelEffAdded, fuelEffAdded  / MaxFuelEffValueDisplayed);

                Rect fuelCapacityStatRect = new Rect(LeftWindowEdge + EdgePadding, fuelEfficiencyStatRect.y + 25f, barWidth, 20f);
                HelperMethods.FillableBarLabeled(fuelCapacityStatRect, SelPawnUpgrade.GetComp<CompFueledTravel>().FuelCapacity / MaxFuelCapValueDisplayed,
                    "BoatFuelCapacity".Translate(), StatUpgrade.FuelCapacity, HelperMethods.FillableBarInnerTex, HelperMethods.FillableBarBackgroundTex, SelPawnUpgrade.GetComp<CompFueledTravel>().FuelCapacity, fuelCapAdded, fuelCapAdded / MaxFuelCapValueDisplayed);
            }
            
        }

        private UpgradeNode selectedNode;

        private const float TopPadding = 20f;

        private const float EdgePadding = 5f;

        private const float SideDisplayedOffset = 40f;

        private const float BottomDisplayedOffset = 20f;

        private const float LeftWindowEdge = 600;

        private const float screenWidth = 850f;

        private const float screenHeight = 700f;

        private const float BottomWindowEdge = screenHeight - 200f;

        public static readonly Color ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        public static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private const float TotalIconSizeScalar = 6000f;

        private readonly Vector2 GridSpacing = new Vector2(20f, 20f);

        private readonly Vector2 GridOrigin = new Vector2(20f, -20f);

        private static Vector2 Resize;
        private bool resizeCheck;

        private const float MaxArmorValueDisplayed = 120;
        private const float MaxSpeedValueDisplayed = 10;
        private const float MaxCargoValueDisplayed = 10000;
        private const float MaxFuelEffValueDisplayed = 100;
        private const float MaxFuelCapValueDisplayed = 2000;
    }
}
