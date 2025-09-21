using System.Collections.Generic;
using System.Linq;
using RimWorld;
using SmashTools;
using UnityEngine;
using Vehicles.Rendering;
using Verse;
using Verse.Sound;

namespace Vehicles;

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

	public const float TotalIconSizeScalar = 6000f;

	private const float ScreenWidth = 880f;
	private const float ScreenHeight = 520f;

	private const float InfoScreenWidth = 375;
	private const float InfoScreenHeight = 150;
	private const float InfoPanelRowHeight = 20f;
	private const float InfoPanelMoreDetailButtonSize = 24;
	private const float InfoPanelArrowPointerSize = 45;

	private const float OverlayGraphicHeight = (InfoScreenWidth - InfoPanelArrowPointerSize) / 2;

	public static readonly Vector2 GridSpacing = new(20, 20);
	public static readonly Vector2 GridOrigin = new(30, 30);

	internal static readonly int MaxLinesAcross = Mathf.FloorToInt(ScreenWidth / GridSpacing.x) - 2;
	private static readonly int MaxLinesDown = Mathf.FloorToInt(ScreenHeight / GridSpacing.y) - 3;

	public static readonly List<string> ReplaceNodes = [];

	private static readonly Color NotUpgradedColor = new(0, 0, 0, 0.3f);
	private static readonly Color UpgradingColor = new(1, 0.75f, 0, 1);
	private static readonly Color DisabledColor = new(0.25f, 0.25f, 0.25f, 1);
	private static readonly Color DisabledLineColor = new(0.3f, 0.3f, 0.3f, 1);
	private static readonly Color GridLineColor = new(0.3f, 0.3f, 0.3f, 1);

	private static readonly Color EffectColorPositive = new(0.1f, 1f, 0.1f);
	private static readonly Color EffectColorNegative = new(0.8f, 0.4f, 0.4f);
	private static readonly Color EffectColorNeutral = new(0.5f, 0.5f, 0.5f, 0.75f);

	private VehiclePawn openedFor;

	private UpgradeNode selectedNode;
	private UpgradeNode highlightedNode;
	private readonly List<UpgradeTextEntry> textEntries = [];
	private float textEntryHeight;

	private readonly List<VehicleTurret> renderTurrets = [];
	private readonly List<string> excludeTurrets = [];

	private bool showDetails;

	private UpgradeNode InfoNode => selectedNode ?? highlightedNode;

	private UpgradeNode SelectedNode
	{
		get { return selectedNode; }
		set
		{
			if (selectedNode != value)
			{
				ClearTurretRenderers(selectedNode);

				selectedNode = value;
				openedFor = Vehicle;

				if (selectedNode != null)
				{
					RecacheTextEntries();
					RecacheTurretRenderers();
				}
			}
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
			if (SelPawn is VehiclePawn { CompUpgradeTree: not null } vehicle)
			{
				if (vehicle != openedFor)
				{
					openedFor = vehicle;
					SelectedNode = null;
				}
				return vehicle;
			}
			CloseTab();
			return null;
		}
	}

	public override void OnOpen()
	{
		base.OnOpen();
		SelectedNode = null;
		highlightedNode = null;
	}

	protected override void CloseTab()
	{
		base.CloseTab();
		openedFor = null;
	}

	private void RecacheTextEntries()
	{
		textEntries.Clear();

		textEntryHeight = 0;

		/*
		if (SelectedNode != null && SelectedNode.upgradeExplanation != null)
		{
		  textEntryHeight = Text.CalcHeight(SelectedNode.upgradeExplanation, InfoScreenWidth - 10);
		}
		else
		{
		  foreach (Upgrade upgrade in InfoNode.upgrades)
		  {
		    foreach (UpgradeTextEntry textEntry in upgrade.UpgradeDescription(Vehicle))
		    {
		      textEntries.Add(textEntry);
		    }
		  }
		  textEntryHeight = textEntries.Count * Text.LineHeightOf(GameFont.Small);
		}
		*/
	}

	private void RecacheTurretRenderers()
	{
		if (SelectedNode.upgrades.NullOrEmpty())
			return;

		foreach (Upgrade upgrade in InfoNode.upgrades)
		{
			if (upgrade is not TurretUpgrade turretUpgrade)
				continue;

			if (!turretUpgrade.turrets.NullOrEmpty())
			{
				foreach (VehicleTurret turret in turretUpgrade.turrets)
				{
					turret.ResolveGraphics(Vehicle.VehicleDef, forceRegen: true);
					renderTurrets.Add(turret);
				}
			}
			if (!turretUpgrade.removeTurrets.NullOrEmpty())
			{
				excludeTurrets.AddRange(turretUpgrade.removeTurrets);
			}
		}
	}

	private void ClearTurretRenderers(UpgradeNode upgradeNode)
	{
		if (upgradeNode != null && !upgradeNode.upgrades.NullOrEmpty())
		{
			foreach (Upgrade upgrade in InfoNode.upgrades)
			{
				if (upgrade is TurretUpgrade turretUpgrade)
				{
					if (!turretUpgrade.turrets.NullOrEmpty())
					{
						foreach (VehicleTurret turret in turretUpgrade.turrets)
						{
							turret.OnDestroy();
						}
					}
				}
			}
		}
		renderTurrets.Clear();
		excludeTurrets.Clear();
	}

	private IEnumerable<UpgradeNode> GetDisablerNodes(UpgradeNode upgradeNode)
	{
		if (!upgradeNode.disableIfUpgradeNodeEnabled.NullOrEmpty())
		{
			UpgradeNode disableNode =
				Vehicle.CompUpgradeTree.Props.def.GetNode(upgradeNode.disableIfUpgradeNodeEnabled);
			if (disableNode != null)
			{
				yield return disableNode;
			}
		}
		if (!upgradeNode.disableIfUpgradeNodesEnabled.NullOrEmpty())
		{
			foreach (string key in upgradeNode.disableIfUpgradeNodesEnabled)
			{
				UpgradeNode disableNode = Vehicle.CompUpgradeTree.Props.def.GetNode(key);
				if (disableNode != null)
				{
					yield return disableNode;
				}
			}
		}
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
		//May occur if sub-window is open while selected entity changes inbetween frames
		if (Vehicle == null)
		{
			return;
		}

		Rect rect = new(0f, TopPadding, size.x, size.y - TopPadding);
		Rect innerRect = rect.ContractedBy(5f);

		if (DebugSettings.ShowDevGizmos)
		{
			string debugGridLabel = "VF_DevMode_DebugDrawUpgradeNodeGrid".Translate();
			Vector2 debugGridLabelSize = Text.CalcSize(debugGridLabel);
			Rect devGridCheckboxRect = new(rect.xMax - 80 - debugGridLabelSize.x, 0,
				debugGridLabelSize.x + 30, debugGridLabelSize.y);
			bool showGrid = VehicleMod.settings.debug.debugDrawNodeGrid;
			Widgets.CheckboxLabeled(devGridCheckboxRect, debugGridLabel, ref showGrid);
			VehicleMod.settings.debug.debugDrawNodeGrid = showGrid;
		}

		highlightedNode = null;

		//DrawButtons();

		DrawGrid(innerRect);

		Rect labelRect = new(innerRect.x, 5, innerRect.width - 16f, 20f);
		Widgets.Label(labelRect, Vehicle.Label);

		if (!VehicleMod.settings.debug.debugDrawNodeGrid)
		{
			using var lineColor = new TextBlock(GridLineColor);
			Widgets.DrawLineHorizontal(innerRect.x, innerRect.y, innerRect.width);
			Widgets.DrawLineHorizontal(innerRect.x, innerRect.yMax, innerRect.width);

			Widgets.DrawLineVertical(innerRect.x, innerRect.y, innerRect.height);
			Widgets.DrawLineVertical(innerRect.xMax, innerRect.y, innerRect.height);
		}
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
		else if (Vehicle.CompUpgradeTree.NodeUnlocked(SelectedNode) &&
			Vehicle.CompUpgradeTree.LastNodeUnlocked(SelectedNode))
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
		else if (Widgets.ButtonText(rect, "VF_Upgrade".Translate()) &&
			!Vehicle.CompUpgradeTree.NodeUnlocked(SelectedNode))
		{
			if (Vehicle.CompUpgradeTree.Disabled(SelectedNode))
			{
				Messages.Message("VF_DisabledFromOtherNode".Translate(), MessageTypeDefOf.RejectInput,
					false);
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
				Messages.Message("VF_MissingPrerequisiteUpgrade".Translate(), MessageTypeDefOf.RejectInput,
					false);
			}
		}
	}

	private static void DrawBackgroundGridTop()
	{
		for (int i = 0; i <= MaxLinesAcross; i++)
		{
			using TextBlock lineColor = new(GridLineColor);
			Widgets.DrawLineVertical(GridSpacing.x + GridSpacing.x * i, TopPadding + GridSpacing.y,
				MaxLinesDown * GridSpacing.y);
			if (i % 5 == 0)
			{
				GUI.color = Color.white;
				Widgets.Label(
					new Rect(GridSpacing.x + GridSpacing.x * i - 5f, TopPadding, GridSpacing.x,
						GridSpacing.y),
					i.ToString());
			}
		}
	}

	private static void DrawBackgroundGridLeft()
	{
		for (int i = 0; i <= MaxLinesDown; i++)
		{
			using TextBlock lineColor = new(GridLineColor);
			Widgets.DrawLineHorizontal(GridSpacing.x, TopPadding + GridSpacing.y * (i + 1),
				MaxLinesAcross * GridSpacing.x);
			if (i % 5 == 0)
			{
				GUI.color = Color.white;
				Widgets.Label(
					new Rect(GridSpacing.x - 20f, TopPadding + GridSpacing.y * (i + 1) - GridSpacing.y / 2, GridSpacing.x,
						GridSpacing.y), i.ToString());
			}
		}
	}

	private void DrawNodeSelection()
	{
		if (SelectedNode == null || Vehicle.CompUpgradeTree.Upgrading)
			return;

		Vector2 selectedRectPos =
			GridCoordinateToScreenPosAdjusted(SelectedNode.GridCoordinate, SelectedNode.drawSize);
		Rect selectedRect = new(selectedRectPos, SelectedNode.drawSize);
		selectedRect = selectedRect.ExpandedBy(2f);
		GUI.DrawTexture(selectedRect, BaseContent.WhiteTex);
	}

	private void DrawPrerequisites()
	{
		using TextBlock colorBlock = new(Color.white);
		foreach (UpgradeNode upgradeNode in Vehicle.CompUpgradeTree.Props.def.nodes)
		{
			if (upgradeNode.prerequisiteNodes.NullOrEmpty() || upgradeNode.hidden)
				continue;

			foreach (UpgradeNode prerequisite in
				Vehicle.CompUpgradeTree.Props.def.nodes.FindAll(prereqNode =>
					!prereqNode.hidden && upgradeNode.prerequisiteNodes.Contains(prereqNode.key)))
			{
				Vector2 start = GridCoordinateToScreenPos(upgradeNode.GridCoordinate);
				Vector2 end = GridCoordinateToScreenPos(prerequisite.GridCoordinate);

				Color color = Vehicle.CompUpgradeTree.NodeUnlocked(upgradeNode) ? Color.white : DisabledLineColor;
				foreach (UpgradeNode disabledNode in GetDisablerNodes(upgradeNode))
				{
					Rect prerequisiteRect =
						new(GridCoordinateToScreenPosAdjusted(disabledNode.GridCoordinate, disabledNode.drawSize),
							prerequisite.drawSize);
					if (!Vehicle.CompUpgradeTree.Upgrading && Mouse.IsOver(prerequisiteRect))
					{
						color = Color.red;
					}
				}
				Widgets.DrawLine(start, end, color, 2f);
			}
		}
	}

	private void DrawUpgradeNodes(Rect rect)
	{
		Rect detailRect = InfoNode != null ? GetDetailRect(rect) : Rect.zero;
		foreach (UpgradeNode upgradeNode in Vehicle.CompUpgradeTree.Props.def.nodes)
		{
			if (upgradeNode.hidden)
				continue;

			using TextBlock colorBlock = new(Color.white);
			Vector2 upgradeRectPos =
				GridCoordinateToScreenPosAdjusted(upgradeNode.GridCoordinate, upgradeNode.drawSize);
			Rect upgradeRect = new(upgradeRectPos, upgradeNode.drawSize);

			bool colored = false;
			if (Vehicle.CompUpgradeTree.Disabled(upgradeNode) ||
				!Vehicle.CompUpgradeTree.PrerequisitesMet(upgradeNode))
			{
				colored = true;
				GUI.color = DisabledColor;
			}
			Widgets.DrawTextureFitted(upgradeRect, Command.BGTex, 1);
			Widgets.DrawTextureFitted(upgradeRect, upgradeNode.UpgradeImage, 1);
			DrawNodeCondition(upgradeRect, upgradeNode, colored);

			if (upgradeNode.displayLabel)
			{
				float textWidth = Text.CalcSize(upgradeNode.label).x;
				Rect nodeLabelRect = new(upgradeRect.x - (textWidth - upgradeRect.width) / 2,
					upgradeRect.y - 20f, 10f * upgradeNode.label.Length, 25f);
				Widgets.Label(nodeLabelRect, upgradeNode.label);
			}

			if (Mouse.IsOver(upgradeRect))
			{
				highlightedNode = upgradeNode;
				if (Vehicle.CompUpgradeTree.PrerequisitesMet(upgradeNode) &&
					!Vehicle.CompUpgradeTree.NodeUnlocked(upgradeNode))
				{
					GUI.DrawTexture(upgradeRect, TexUI.HighlightTex);
				}
			}

			if (InfoNode != null && !Mouse.IsOver(detailRect) && Widgets.ButtonInvisible(upgradeRect))
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
		if (InfoNode != null)
		{
			Widgets.BeginGroup(detailRect);
			{
				DrawInfoPanel(detailRect.AtZero());
			}
			Widgets.EndGroup();
		}
	}

	private void DrawGrid(Rect rect)
	{
		if (DebugSettings.ShowDevGizmos && VehicleMod.settings.debug.debugDrawNodeGrid)
		{
			DrawBackgroundGridTop();
			DrawBackgroundGridLeft();
		}
		DrawNodeSelection();
		DrawPrerequisites();
		DrawUpgradeNodes(rect);
	}

	private Rect GetDetailRect(Rect rect, float padding = 5)
	{
		Vector2 upgradeRectPos =
			GridCoordinateToScreenPosAdjusted(InfoNode.GridCoordinate, InfoNode.drawSize);
		Rect upgradeRect = new(upgradeRectPos, InfoNode.drawSize);

		float ingredientsHeight = 0;
		if (!InfoNode.ingredients.NullOrEmpty())
		{
			ingredientsHeight = InfoNode.ingredients.Count * InfoPanelRowHeight;
		}

		float detailWidth = InfoScreenWidth - padding * 2;

		using TextBlock textFont = new(GameFont.Medium);

		float labelHeight = Text.CalcHeight(InfoNode.label, detailWidth);
		Text.Font = GameFont.Small;
		float descriptionHeight = Text.CalcHeight(InfoNode.description, detailWidth);
		float totalCalculatedHeight =
			labelHeight + descriptionHeight + ingredientsHeight + 30 + padding;
		if (SelectedNode != null)
		{
			totalCalculatedHeight += 10; //5 padding from description to line, and line to bottom rect
			if (SelectedNode.HasGraphics)
			{
				totalCalculatedHeight += OverlayGraphicHeight;
			}

			if (!textEntries.NullOrEmpty())
			{
				totalCalculatedHeight += textEntryHeight + 5;
			}
		}

		float windowRectX = upgradeRect.x + InfoNode.drawSize.x + padding;
		if (windowRectX + detailWidth > rect.xMax - padding)
		{
			windowRectX = upgradeRect.x - detailWidth - padding;
			windowRectX = Mathf.Clamp(windowRectX, rect.x + padding, rect.xMax - detailWidth - padding);
		}

		float maxInfoScreenHeight = Mathf.Max(InfoScreenHeight, totalCalculatedHeight);
		float windowRectY = upgradeRect.y + InfoNode.drawSize.y / 2 - maxInfoScreenHeight / 2;
		windowRectY = Mathf.Clamp(windowRectY, rect.y + padding,
			rect.yMax - maxInfoScreenHeight - padding);

		//if (windowRectY + maxInfoScreenHeight >= ScreenHeight)
		//{
		//	windowRectY -= maxInfoScreenHeight + padding * 2 + InfoNode.drawSize.y;
		//}

		return new Rect(windowRectX, windowRectY, detailWidth, maxInfoScreenHeight);
	}

	private void DrawInfoPanel(Rect rect, float padding = 5)
	{
		Widgets.DrawMenuSection(rect);

		Rect innerInfoRect = rect.ContractedBy(padding);

		using TextBlock textFont = new(GameFont.Medium);

		float labelHeight = Text.CalcHeight(InfoNode.label, innerInfoRect.width);
		Rect labelRect = new(innerInfoRect.x, innerInfoRect.y, innerInfoRect.width, labelHeight);
		Widgets.Label(labelRect, InfoNode.label);

		Text.Font = GameFont.Small;

		Rect costListRect = new(innerInfoRect.x, labelRect.yMax, innerInfoRect.width,
			innerInfoRect.height - labelRect.height);
		float costY = DrawCostItems(costListRect);
		Rect tempRect = costListRect;
		tempRect.height = costY;

		float descriptionHeight = Text.CalcHeight(InfoNode.description, innerInfoRect.width);
		Rect descriptionRect = new(innerInfoRect.x, costListRect.y + costY, innerInfoRect.width,
			descriptionHeight);
		Widgets.Label(descriptionRect, InfoNode.description);

		if (SelectedNode != null)
		{
			bool hasGraphics = SelectedNode.HasGraphics;
			bool showUpgradeList = !SelectedNode.upgrades.NullOrEmpty();

			if (hasGraphics || showUpgradeList)
			{
				Widgets.DrawLineHorizontal(rect.x, descriptionRect.yMax + 5, rect.width,
					UIElements.MenuSectionBgBorderColor);
			}

			Rect textEntryRect = new(innerInfoRect.x, descriptionRect.yMax + 10, innerInfoRect.width,
				textEntryHeight);
			if (showUpgradeList)
			{
				//DrawUpgradeList(textEntryRect);
			}

			if (hasGraphics)
			{
				Rect overlayShowcaseRect = new(innerInfoRect.x, textEntryRect.yMax + 5,
					innerInfoRect.width, (innerInfoRect.width - InfoPanelArrowPointerSize) / 2);
				DrawVehicleGraphicComparison(overlayShowcaseRect);
			}

			Rect buttonRect = new(innerInfoRect.xMax - 125f, innerInfoRect.yMax - 30, 120, 30);
			DrawButtons(buttonRect);

			Rect tempButtonRect = buttonRect;
			tempButtonRect.width = innerInfoRect.width;
		}
	}

	private void DrawVehicleGraphicComparison(Rect rect)
	{
		Rect innerInfoRect = rect.ContractedBy(5);

		Rect vehicleOriginalRect = new(innerInfoRect.x, innerInfoRect.y, innerInfoRect.height,
			innerInfoRect.height);
		float arrowSize = InfoPanelArrowPointerSize;
		Rect arrowPointerRect = new(vehicleOriginalRect.xMax + 5,
			vehicleOriginalRect.y + vehicleOriginalRect.height / 2 - arrowSize / 2, arrowSize, arrowSize);
		Rect vehicleNewRect = new(arrowPointerRect.xMax + 5, vehicleOriginalRect.y,
			vehicleOriginalRect.width, vehicleOriginalRect.height);

		BlitRequest requestBefore = BlitRequest.For(Vehicle);
		VehicleGui.DrawVehicleOnGUI(vehicleOriginalRect, requestBefore);

		BlitRequest requestAfter = new(Vehicle);
		requestAfter.blitTargets.Add(Vehicle.VehicleDef);
		List<GraphicOverlay> extraOverlays = Vehicle.CompUpgradeTree.Props.TryGetOverlays(InfoNode);
		if (!extraOverlays.NullOrEmpty())
			requestAfter.blitTargets.AddRange(extraOverlays);
		if (!Vehicle.DrawTracker.overlayRenderer.AllOverlaysListForReading.NullOrEmpty())
			requestAfter.blitTargets.AddRange(Vehicle.DrawTracker.overlayRenderer
			 .AllOverlaysListForReading);
		if (!renderTurrets.NullOrEmpty())
			requestAfter.blitTargets.AddRange(renderTurrets);
		if (Vehicle.GetCachedComp<CompVehicleTurrets>() is { } compTurrets &&
			!compTurrets.Turrets.NullOrEmpty())
		{
			requestAfter.blitTargets.AddRange(compTurrets.Turrets.Where(turret =>
				excludeTurrets is null || !excludeTurrets.Contains(turret.key)));
		}
		VehicleGui.DrawVehicleOnGUI(vehicleNewRect, requestAfter);

		Widgets.DrawTextureFitted(arrowPointerRect, TexData.TutorArrowRight, 1);
	}

	private void DrawSubIconsBar(Rect rect)
	{
		Rect subIconsRect = new(rect.xMax - InfoPanelMoreDetailButtonSize, rect.y, rect.width,
			InfoPanelMoreDetailButtonSize);

		Color baseColor = !showDetails ? Color.white : Color.green;
		Color mouseoverColor = !showDetails ? GenUI.MouseoverColor : new Color(0f, 0.5f, 0f);

		Rect buttonRect = new(subIconsRect.x, subIconsRect.y, InfoPanelMoreDetailButtonSize,
			InfoPanelMoreDetailButtonSize);
		if (!InfoNode.upgrades.NullOrEmpty())
		{
			if (Widgets.ButtonImageFitted(buttonRect, TexButton.Info, baseColor, mouseoverColor))
			{
				showDetails = !showDetails;

				if (showDetails)
				{
					SoundDefOf.TabOpen.PlayOneShotOnCamera();
				}
				else
				{
					SoundDefOf.TabClose.PlayOneShotOnCamera();
				}
			}
			buttonRect.x -= buttonRect.width;
		}
	}

	protected override void UpdateSize()
	{
		base.UpdateSize();
		if (!Mathf.Approximately(size.x, ScreenWidth) ||
			!Mathf.Approximately(size.y, ScreenHeight))
		{
			size = new Vector2(ScreenWidth, ScreenHeight);
		}
	}

	private float DrawCostItems(Rect rect)
	{
		Rect costRect = rect;
		float currentY = 0;
		foreach (ThingDefCountClass thingDefCountClass in InfoNode.ingredients)
		{
			using TextBlock colorBlock = new(Color.white);
			Rect iconRect = new(costRect.x, costRect.y + currentY, InfoPanelRowHeight,
				InfoPanelRowHeight);
			GUI.DrawTexture(iconRect, thingDefCountClass.thingDef.uiIcon);

			string label = thingDefCountClass.thingDef.LabelCap;
			if (Vehicle.Map.resourceCounter.GetCount(thingDefCountClass.thingDef) <
				thingDefCountClass.count)
			{
				GUI.color = Color.red;
			}
			Rect itemCountRect = new(iconRect.x + 26, iconRect.y, 50, InfoPanelRowHeight);
			Widgets.Label(itemCountRect, $"{thingDefCountClass.count}");

			Rect itemLabelRect = new(iconRect.x + 60, itemCountRect.y,
				costRect.width - itemCountRect.width - 25, InfoPanelRowHeight);
			Widgets.Label(itemLabelRect, label);
			currentY += InfoPanelRowHeight;
		}
		return currentY;
	}

	private void DrawUpgradeList(Rect rect)
	{
		if (InfoNode.upgradeExplanation != null)
		{
			Widgets.Label(rect, InfoNode.upgradeExplanation);
		}
		else if (!InfoNode.upgrades.NullOrEmpty())
		{
			using TextBlock textBlock = new(GameFont.Small, TextAnchor.MiddleLeft);
			// Lists upgrade details if none is included
			foreach (Upgrade upgrade in InfoNode.upgrades)
			{
				foreach (UpgradeTextEntry textEntry in upgrade.UpgradeDescription(Vehicle))
				{
					using TextBlock colorBlock = new(Color.white);
					float labelWidth = rect.width * 0.75f;
					float labelHeight = Text.CalcHeight(textEntry.label, labelWidth);
					float valueWidth = rect.width - labelWidth;
					float valueHeight = Text.CalcHeight(textEntry.description, valueWidth);

					float entryHeight = Mathf.Max(labelHeight, valueHeight);

					Rect leftRect = new(rect.x, rect.y, labelWidth, entryHeight);
					Widgets.Label(leftRect, textEntry.label);

					GUI.color = textEntry.effectType switch
					{
						UpgradeEffectType.Positive => EffectColorPositive,
						UpgradeEffectType.Negative => EffectColorNegative,
						UpgradeEffectType.Neutral  => EffectColorNeutral,
						_                          => Color.white,
					};

					Rect rightRect = new(leftRect.xMax, rect.y, valueWidth, entryHeight);
					Widgets.Label(rightRect, textEntry.description);

					rect.y += entryHeight;
				}
			}
		}
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
		return;

		bool DisabledAndMouseOver()
		{
			foreach (UpgradeNode disabledNode in GetDisablerNodes(node))
			{
				Rect prerequisiteRect = new(
					GridCoordinateToScreenPosAdjusted(disabledNode.GridCoordinate, disabledNode.drawSize),
					disabledNode.drawSize);
				if (!Vehicle.CompUpgradeTree.Upgrading && Mouse.IsOver(prerequisiteRect))
				{
					return true;
				}
			}
			return false;
		}
	}

	private static void DrawBorder(Rect rect, Color color)
	{
		Vector2 topLeft = new(rect.x, rect.y);
		Vector2 topRight = new(rect.xMax, rect.y);

		Vector2 bottomLeft = new(rect.x, rect.yMax);
		Vector2 bottomRight = new(rect.xMax, rect.yMax);

		Vector2 leftTop = new(rect.x, rect.y + 2);
		Vector2 leftBottom = new(rect.x, rect.yMax + 2);

		Vector2 rightTop = new(rect.xMax, rect.y + 2);
		Vector2 rightBottom = new(rect.xMax, rect.yMax + 2);

		Widgets.DrawLine(topLeft, topRight, color, 1);
		Widgets.DrawLine(bottomLeft, bottomRight, color, 1);
		Widgets.DrawLine(leftTop, leftBottom, color, 1);
		Widgets.DrawLine(rightTop, rightBottom, color, 1);
	}
}