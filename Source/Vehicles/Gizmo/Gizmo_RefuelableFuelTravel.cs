using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.Steam;

namespace Vehicles.Rendering;

[StaticConstructorOnStartup]
public class Gizmo_RefuelableFuelTravel : Gizmo_Slider
{
  private const float FuelIconSize = 24;

  private readonly CompFueledTravel refuelable;
  private readonly bool showVehicleLabel;

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
    return "";
  }

  public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
  {
    if (SteamDeck.IsSteamDeckInNonKeyboardMode)
      return base.GizmoOnGUI(topLeft, maxWidth, parms);

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
    headerRect.xMax -= FuelIconSize;
    Rect iconRect = new(headerRect.xMax, headerRect.y, FuelIconSize, FuelIconSize);

    bool electric = refuelable.Props.ElectricPowered;

    GUI.DrawTexture(iconRect, electric ? TexData.FlickerIcon : refuelable.Props.FuelIcon);
    Rect subIconRect =
      new(iconRect.center.x, iconRect.y, iconRect.width / 2f, iconRect.height / 2f);
    bool checkOn = electric ? refuelable.Charging : refuelable.allowAutoRefuel;
    GUI.DrawTexture(subIconRect, checkOn ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex);

    if (Widgets.ButtonInvisible(iconRect))
    {
      ToggleSwitch();
    }

    if (Mouse.IsOver(iconRect))
    {
      Widgets.DrawHighlight(iconRect);
      TooltipHandler.TipRegion(iconRect,
        electric ? PowerNetTip : RefuelTip,
        electric ? "PowerNetTip".GetHashCode() : "RefuelTip".GetHashCode());
      mouseOverElement = true;
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
    StringBuilder tooltip = UIHelper.tooltipBuilder;
    tooltip.Clear();
    tooltip.AppendLine("VF_ElectricFlick".Translate());
    tooltip.AppendLine();
    tooltip.AppendLine();
    tooltip.AppendLine(
      "VF_ElectricFlickDesc".Translate(refuelable.Charging.ToStringYesNo().UncapitalizeFirst()));
    tooltip.AppendLine();
    tooltip.AppendLine();
    tooltip.AppendLine(
      $"{"HotKeyTip".Translate()}: {KeyPrefs.KeyPrefsData.GetBoundKeyCode(KeyBindingDefOf.Command_TogglePower, KeyPrefs.BindingSlot.A).ToStringReadable()}");
    string text = tooltip.ToString();
    tooltip.Clear();
    return text;
  }

  private string RefuelTip()
  {
    StringBuilder tooltip = UIHelper.tooltipBuilder;
    tooltip.Clear();
    tooltip.AppendLine("CommandToggleAllowAutoRefuel".Translate());
    tooltip.AppendLine();
    tooltip.AppendLine();
    tooltip.AppendLine("CommandToggleAllowAutoRefuelDesc".Translate(refuelable.TargetFuelLevel
         .ToString("F0")
         .Colorize(ColoredText.TipSectionTitleColor),
        (refuelable.allowAutoRefuel ? "On".TranslateSimple() : "Off".TranslateSimple())
       .UncapitalizeFirst()
       .Named("ONOFF")
      )
     .Resolve());
    tooltip.AppendLine();
    tooltip.AppendLine();
    tooltip.AppendLine(
      $"{"HotKeyTip".Translate()}: {KeyPrefs.KeyPrefsData.GetBoundKeyCode(KeyBindingDefOf.Command_ItemForbid, KeyPrefs.BindingSlot.A).ToStringReadable()}");
    string text = tooltip.ToString();
    tooltip.Clear();
    return text;
  }
}