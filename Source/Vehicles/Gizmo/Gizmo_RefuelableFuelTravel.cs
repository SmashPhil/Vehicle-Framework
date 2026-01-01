using System.Text;
using CoreLib;
using RimWorld;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.Steam;

namespace Vehicles.Rendering;

[StaticConstructorOnStartup]
public class Gizmo_RefuelableFuelTravel : Gizmo_Slider
{
	private const float FuelIconSize = 24;

	private static readonly StringBuilder TooltipBuilder = new();

	private readonly CompFueledTravel refuelable;
	private readonly bool showVehicleLabel;

	private float fuelAvailable;
	private bool refuelFromInventoryDisabled;
	private string refuelFromInventoryDisabledReason;

	public Gizmo_RefuelableFuelTravel(CompFueledTravel refuelable, bool showVehicleLabel)
	{
		this.refuelable = refuelable;
		this.showVehicleLabel = showVehicleLabel;
		Order = -100f;
	}

	protected override float Target
	{
		get { return refuelable.TargetFuelPercent; }
		set { refuelable.TargetFuelPercent = value; }
	}

	protected override float ValuePercent => refuelable.FuelPercent;

	protected override string Title =>
		showVehicleLabel ? refuelable.Vehicle.LabelCap : refuelable.Props.GizmoLabel;

	protected override bool IsDraggable => !refuelable.Props.ElectricPowered;

	protected override string BarLabel
	{
		get
		{
			return
				$"{refuelable.Fuel.ToStringDecimalIfSmall()} / {refuelable.FuelCapacity.ToStringDecimalIfSmall()}";
		}
	}

	private KeyBindingDef KeyBindDef
	{
		get
		{
			if (refuelable.Props.ElectricPowered)
				return KeyBindingDefOf.Command_TogglePower;
			return KeyBindingDefOf.Command_ItemForbid;
		}
	}

	protected override bool DraggingBar { get; set; }

	protected override string GetTooltip()
	{
		return $"{refuelable.TargetFuelLevel:F0} {refuelable.Props.fuelType.LabelCap}";
	}

	private void UpdateDisableStatus()
	{
		refuelFromInventoryDisabled = false;
		refuelFromInventoryDisabledReason = null;
		fuelAvailable = 0;
		if (Mathf.Approximately(refuelable.FuelPercent, 1))
		{
			refuelFromInventoryDisabled = true;
			refuelFromInventoryDisabledReason = "VF_VehicleFullyFueled".Translate(refuelable.Vehicle.LabelCap);
			return;
		}
		fuelAvailable = FuelInVehicle(refuelable.Vehicle);
		if (fuelAvailable == 0)
		{
			refuelFromInventoryDisabled = true;
			refuelFromInventoryDisabledReason = "VF_NoFuelInVehicle".Translate(refuelable.Vehicle.LabelCap);
		}
		if (refuelable.Vehicle.InAerialVehicle())
		{
			refuelFromInventoryDisabled = true;
			refuelFromInventoryDisabledReason = "VF_CantRefuelWhileFlying".Translate(refuelable.Vehicle.LabelCap);
		}
		return;

		static float FuelInVehicle(VehiclePawn vehicle)
		{
			float fuel = 0;
			foreach (Thing thing in CompFueledTravel.AllFuelFromInventory(vehicle))
				fuel += thing.stackCount;
			return fuel;
		}
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		if (SteamDeck.IsSteamDeckInNonKeyboardMode)
			return base.GizmoOnGUI(topLeft, maxWidth, parms);

		UpdateDisableStatus();

		KeyCode hotKeyCode = KeyBindDef.MainKey;
		if (hotKeyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(hotKeyCode))
		{
			if (KeyBindDef.KeyDownEvent)
			{
				ToggleSwitch();
				Event.current.Use();
			}
		}
		return base.GizmoOnGUI(topLeft, maxWidth, parms);
	}

	protected override void DrawHeader(Rect headerRect, ref bool mouseOverElement)
	{
		const float IconBarPadding = 4;

		headerRect.xMax -= FuelIconSize;
		Rect iconRect = new(headerRect.xMax, headerRect.y, FuelIconSize, FuelIconSize);

		bool electric = refuelable.Props.ElectricPowered;

		Texture fuelTexture = electric ? TexData.FlickerIcon : ThingDefOf.Chemfuel.uiIcon;
		GUI.DrawTexture(iconRect, fuelTexture);
		Rect subIconRect =
			new(iconRect.center.x, iconRect.y, iconRect.width / 2f, iconRect.height / 2f);
		bool checkOn = electric ? refuelable.Charging : refuelable.allowAutoRefuel;
		GUI.DrawTexture(subIconRect, checkOn ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex);

		if (Widgets.ButtonInvisible(iconRect))
		{
			ToggleAutoRefuel();
		}

		if (Mouse.IsOver(iconRect))
		{
			Widgets.DrawHighlight(iconRect);
			TooltipHandler.TipRegion(iconRect,
				electric ? PowerNetTip : RefuelTip,
				electric ? "PowerNetTip".GetHashCode() : "RefuelTip".GetHashCode());
			mouseOverElement = true;
		}

		if (!electric)
		{
			iconRect.x -= iconRect.width + IconBarPadding;
			Rect refuelCargoRect = iconRect.ExpandedBy(3);
			refuelCargoRect.y += 2;
			GenUI.DrawTextureWithMaterial(refuelCargoRect, VehicleTex.RefuelFromCargo,
				refuelFromInventoryDisabled ? TexUI.GrayscaleGUI : null);
			if (Widgets.ButtonInvisible(iconRect))
			{
				if (refuelFromInventoryDisabled)
				{
					Messages.Message(refuelFromInventoryDisabledReason, MessageTypeDefOf.RejectInput);
					return;
				}
				const int MinRefuelCount = 1;
				int maxRefuel = Mathf.FloorToInt(Mathf.Min(fuelAvailable, refuelable.FuelCapacity - refuelable.Fuel));
				if (maxRefuel < MinRefuelCount)
					maxRefuel = MinRefuelCount;
				Dialog_Slider slider = new(
					count => "VF_RefuelFromInventoryCount".Translate(count, refuelable.Props.fuelType.label),
					MinRefuelCount, maxRefuel, refuelable.ConsumeFuelFromInventory);
				Find.WindowStack.Add(slider);
			}
			if (Mouse.IsOver(iconRect))
			{
				Widgets.DrawHighlight(iconRect);
				TooltipHandler.TipRegion(iconRect, RefuelFromInventoryTip, "RefuelFromInventoryTip".GetHashCode());
				mouseOverElement = true;
			}
		}
		base.DrawHeader(headerRect, ref mouseOverElement);
	}

	private void ToggleSwitch()
	{
		if (refuelable.Props.ElectricPowered)
			ToggleCharging();
		else
			ToggleAutoRefuel();
	}

	private void ToggleAutoRefuel()
	{
		refuelable.allowAutoRefuel = !refuelable.allowAutoRefuel;

		if (refuelable.allowAutoRefuel)
			SoundDefOf.Tick_High.PlayOneShotOnCamera();
		else
			SoundDefOf.Tick_Low.PlayOneShotOnCamera();
	}

	private void ToggleCharging()
	{
		if (!refuelable.Charging)
		{
			if (refuelable.TryConnectPower())
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
			else
				SoundDefOf.ClickReject.PlayOneShotOnCamera();
		}
		else
		{
			refuelable.DisconnectPower();
			SoundDefOf.Tick_Low.PlayOneShotOnCamera();
		}
	}

	private string PowerNetTip()
	{
		using ClearStringOnDispose csod = new(TooltipBuilder);
		TooltipBuilder.AppendLine("VF_ElectricFlick".Translate());
		TooltipBuilder.AppendLine();
		TooltipBuilder.AppendLine();
		TooltipBuilder.AppendLine(
			"VF_ElectricFlickDesc".Translate(refuelable.Charging.ToStringYesNo().UncapitalizeFirst()));
		TooltipBuilder.AppendLine();
		TooltipBuilder.AppendLine();
		TooltipBuilder.AppendLine(
			$"{"HotKeyTip".Translate()}: {KeyPrefs.KeyPrefsData.GetBoundKeyCode(KeyBindingDefOf.Command_TogglePower, KeyPrefs.BindingSlot.A).ToStringReadable()}");
		return TooltipBuilder.ToString();
	}

	private string RefuelTip()
	{
		using ClearStringOnDispose csod = new(TooltipBuilder);
		TooltipBuilder.AppendLine("CommandToggleAllowAutoRefuel".Translate());
		TooltipBuilder.AppendLine();
		TooltipBuilder.AppendLine();
		TooltipBuilder.AppendLine("CommandToggleAllowAutoRefuelDesc".Translate(refuelable.TargetFuelLevel
				 .ToString("F0")
				 .Colorize(ColoredText.TipSectionTitleColor),
				(refuelable.allowAutoRefuel ? "On".TranslateSimple() : "Off".TranslateSimple())
			 .UncapitalizeFirst()
			 .Named("ONOFF")
			)
		 .Resolve());
		TooltipBuilder.AppendLine();
		TooltipBuilder.AppendLine();
		TooltipBuilder.AppendLine(
			$"{"HotKeyTip".Translate()}: {KeyPrefs.KeyPrefsData.GetBoundKeyCode(KeyBindingDefOf.Command_ItemForbid, KeyPrefs.BindingSlot.A).ToStringReadable()}");
		return TooltipBuilder.ToString();
	}

	private string RefuelFromInventoryTip()
	{
		using ClearStringOnDispose csod = new(TooltipBuilder);
		TooltipBuilder.AppendLine("VF_RefuelFromInventoryDesc".Translate(refuelable.Vehicle.LabelCap));
		return TooltipBuilder.ToString();
	}
}