using System.Linq;
using RimWorld;
using SmashTools;
using SmashTools.Rendering;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Vehicles.Rendering;

public class Command_CooldownAction : Command_Turret
{
  private const float IdlerTimeExpiry = 5; // seconds

  protected const float TurretGizmoPadding = 5;

  protected const float GizmoHeight = 75;
  protected const float ConfigureButtonSize = 20;
  protected const float SubIconSize = GizmoHeight / 2.25f;

  protected const float CooldownBarWidth = 18;
  protected const float PaddingBetweenElements = 2;

  private bool textureDirty = true;

  private RenderTextureIdler rtIdler;

  protected override float RecalculateWidth()
  {
    float minWidth = GizmoHeight;
    if (turret.CanOverheat)
    {
      minWidth += CooldownBarWidth + TurretGizmoPadding;
    }
    minWidth += turret.SubGizmos.Count() * SubIconSize;
    return minWidth;
  }

  public override void FireTurret(VehicleTurret turret)
  {
    if (turret.ReloadTicks <= 0)
    {
      Vector3 target =
        turret.TurretLocation.PointFromAngle(turret.MaxRange, turret.TurretRotation);
      turret.SetTarget(target.ToIntVec3());
      turret.PushTurretToQueue();
      turret.ResetPrefireTimer();
    }
  }

  public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
  {
    using TextBlock textBlock = new(GameFont.Tiny);
    Rect rect = new(topLeft.x, topLeft.y, GetWidth(maxWidth), GizmoHeight);

    Material material = disabled ? TexUI.GrayscaleGUI : null;
    Material cooldownMaterial = turret.OnCooldown ? TexUI.GrayscaleGUI : material;

    Widgets.DrawWindowBackground(rect);
    // padding between all contents inside gizmo background
    rect = rect.ContractedBy(TurretGizmoPadding);

    Rect gizmoRect = new(rect.x, rect.y, rect.height, rect.height);
    float gizmoWidth = DrawGizmoButton(gizmoRect, cooldownMaterial, out bool mouseOver,
      out _, out bool fireTurret, out bool haltTurret);

    Text.Font = GameFont.Small;

    Rect cooldownRect = new(gizmoRect.x + gizmoWidth + TurretGizmoPadding, gizmoRect.y,
      CooldownBarWidth, gizmoRect.height);
    float cooldownWidth = DrawCooldownBar(cooldownRect);

    float topIconSize = gizmoRect.height / 2;
    float barWidth = rect.width - (gizmoWidth + cooldownWidth + TurretGizmoPadding);
    Rect topBarRect = new(cooldownRect.x + cooldownWidth, gizmoRect.y, barWidth, topIconSize);
    VehicleTurret.SubGizmo subGizmo = DrawTopBar(topBarRect, ref mouseOver);
    Rect bottomBarRect = new(topBarRect.x, topBarRect.y + topIconSize, topBarRect.width,
      topIconSize);
    DrawBottomBar(bottomBarRect, ref mouseOver);

    if (!LabelCap.NullOrEmpty())
    {
      using (new TextBlock(GameFont.Tiny))
      {
        float textWidth = gizmoRect.width + TurretGizmoPadding * 2;
        float textHeight = Text.CalcHeight(LabelCap, textWidth);
        Rect labelRect = new(gizmoRect.x, rect.yMax - textHeight + 12f, textWidth, textHeight);
        GUI.DrawTexture(labelRect, TexUI.GrayTextBG);

        Text.Anchor = TextAnchor.UpperCenter;
        Widgets.Label(labelRect, LabelCap);
      }
    }

    if (DoTooltip)
    {
      string tooltip = Desc;
      if (!disabledReason.NullOrEmpty())
      {
        tooltip = $"{Desc}\n\n{"DisabledCommand".Translate()}: {disabledReason}";
      }
      TooltipHandler.TipRegion(gizmoRect, tooltip);
    }

    if (!HighlightTag.NullOrEmpty() && (Find.WindowStack.FloatMenu == null ||
      !Find.WindowStack.FloatMenu.windowRect.Overlaps(gizmoRect)))
    {
      UIHighlighter.HighlightOpportunity(gizmoRect, HighlightTag);
    }

    if (hotKey is { } keyBindDef && keyBindDef.MainKey != KeyCode.None &&
      !GizmoGridDrawer.drawnHotKeys.Contains(keyBindDef.MainKey))
    {
      Rect hotkeyRect = new(gizmoRect.x + 5f, rect.y + 5f, gizmoRect.width - 10f, 18f);
      Widgets.Label(hotkeyRect, keyBindDef.MainKey.ToStringReadable());
      GizmoGridDrawer.drawnHotKeys.Add(keyBindDef.MainKey);
      if (hotKey.KeyDownEvent)
      {
        fireTurret = true;
        Event.current.Use();
      }
    }

    if (subGizmo is { IsValid: true }) subGizmo.onClick();

    if (haltTurret)
    {
      if (disabled)
      {
        if (!disabledReason.NullOrEmpty())
        {
          Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
        }
        return new GizmoResult(GizmoState.Mouseover);
      }
      turret.SetTarget(LocalTargetInfo.Invalid);
      return new GizmoResult(GizmoState.Clear);
    }

    if (fireTurret && !turret.OnCooldown)
    {
      if (disabled)
      {
        if (!disabledReason.NullOrEmpty())
        {
          Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
        }
        return new GizmoResult(GizmoState.Mouseover);
      }
      if (!TutorSystem.AllowAction(TutorTagSelect))
      {
        return new GizmoResult(GizmoState.Mouseover);
      }
      GizmoResult result = new(GizmoState.Interacted, Event.current);
      TutorSystem.Notify_Event(TutorTagSelect);
      return result;
    }
    return new GizmoResult(mouseOver ? GizmoState.Mouseover : GizmoState.Clear);
  }

  protected virtual float DrawGizmoButton(Rect rect, Material cooldownMaterial,
    out bool mouseOver, out bool ammoLoaded, out bool fireTurret, out bool haltTurret)
  {
    rect = rect.ContractedBy(2);

    ammoLoaded = true;
    mouseOver = false;
    fireTurret = false;

    Widgets.BeginGroup(rect);

    Rect gizmoRect = rect.AtZero();
    // top right
    Rect subIconRect = new(gizmoRect.xMax - SubIconSize, gizmoRect.y, SubIconSize, SubIconSize);
    if ((turret.loadedAmmo is null || turret.shellCount <= 0) && turret.def.ammunition != null)
    {
      Disable("VF_NoAmmoLoadedTurret".Translate());
      ammoLoaded = false;
    }
    else if (turret.ProjectileDef is { projectile.flyOverhead: true } &&
      vehicle.Position.Roofed(vehicle.Map))
    {
      Disable($"{"CannotFire".Translate()}: {"Roofed".Translate().CapitalizeFirst()}");
    }
    else if (!turret.OnCooldown && Mouse.IsOver(gizmoRect) &&
      (!Mouse.IsOver(subIconRect) || !turret.targetInfo.IsValid))
    {
      if (!disabled)
      {
        GUI.color = GenUI.MouseoverColor;
      }
      mouseOver = true;
      turret.GizmoHighlighted = true;
    }
    else
    {
      turret.GizmoHighlighted = false;
    }

    GenUI.DrawTextureWithMaterial(gizmoRect, BGTex, cooldownMaterial);
    MouseoverSounds.DoRegion(gizmoRect, SoundDefOf.Mouseover_Command);
    GUI.color = IconDrawColor;

    DrawVehicleTurret(gizmoRect);

    if (!ammoLoaded)
    {
      Widgets.DrawBoxSolid(gizmoRect, DarkGrey);
    }

    if (turret.ReloadTicks > 0)
    {
      float percent = turret.ReloadTicks / (float)turret.MaxTicks;
      UIElements.VerticalFillableBar(gizmoRect, percent, UIData.FillableBarTexture,
        UIData.ClearBarTexture);
    }

    if (DrawSubIcons(subIconRect, cooldownMaterial, out haltTurret))
    {
      mouseOver = false;
      fireTurret = false;
    }
    else if (ammoLoaded && Widgets.ButtonInvisible(gizmoRect))
    {
      fireTurret = true;
    }

    Widgets.EndGroup();

    return rect.width;
  }

  private void DrawVehicleTurret(Rect rect)
  {
    using TextBlock colorBlock = new(Color.white);
    Rect turretRect = rect.ContractedBy(2);
    if (!turret.def.gizmoIconTexPath.NullOrEmpty())
    {
      Widgets.BeginGroup(turretRect);
      Rect iconRect = turretRect.AtZero()
       .ExpandedBy(turretRect.width * (turret.def.gizmoIconScale * iconDrawScale - 1));
      UIElements.DrawTextureWithMaterialOnGUI(iconRect, turret.GizmoIcon, null, 0);
      Widgets.EndGroup();
    }
    else
    {
      if (rtIdler is { Disposed: true })
        rtIdler = null;

      if (Event.current.type == EventType.Repaint)
      {
        if (textureDirty || rtIdler == null)
        {
          BlitRequest request = new(vehicle)
            { rot = Rot8.North };
          request.blitTargets.Add(turret);
          rtIdler ??=
            new RenderTextureIdler(VehicleGui.CreateRenderTextureBuffer(turretRect, request),
              IdlerTimeExpiry);
          VehicleGui.Blit(rtIdler.GetWrite(), turretRect, request,
            iconScale: turret.def.gizmoIconScale * iconDrawScale, forceCentering: false);
          textureDirty = false;
        }
        GUI.DrawTexture(turretRect, rtIdler.Read);
      }
    }
  }

  /// <summary>
  /// Sub icons rendered in bottom right of Gizmo button
  /// </summary>
  /// <param name="rect"></param>
  /// <param name="material"></param>
  /// <param name="haltTurret"></param>
  /// <returns>Mouse is over SubIcon rect</returns>
  protected virtual bool DrawSubIcons(Rect rect, Material material, out bool haltTurret)
  {
    bool mouseOverSubIcon = false;
    haltTurret = false;

    if (turret.OnCooldown)
    {
      GenUI.DrawTextureWithMaterial(rect, turret.FireIcon,
        CompFireOverlay.FireGraphic.MatAt(Rot4.North));
    }
    else if (turret.targetInfo.IsValid)
    {
      if (!disabled && Widgets.ButtonInvisible(rect))
      {
        SoundDefOf.Click.PlayOneShotOnCamera();
        haltTurret = true;
      }

      if (!disabled && Mouse.IsOver(rect))
      {
        mouseOverSubIcon = true;
        GUI.color = GenUI.MouseoverColor;
      }
      GenUI.DrawTextureWithMaterial(rect, VehicleTex.HaltIcon, material);
    }
    // No MouseOver for Gizmo rect if mouse is over active sub icon
    return mouseOverSubIcon;
  }

  protected virtual float DrawCooldownBar(Rect rect)
  {
    if (turret.CanOverheat)
    {
      float heatPercent = turret.currentHeatRate / VehicleTurret.MaxHeatCapacity;
      UIElements.VerticalFillableBar(rect, heatPercent, TexData.HeatColorPercent(heatPercent),
        BaseContent.BlackTex, doBorder: true);
      return rect.width + TurretGizmoPadding;
    }
    return 0;
  }

  protected virtual VehicleTurret.SubGizmo DrawTopBar(Rect rect, ref bool mouseOver)
  {
    VehicleTurret.SubGizmo clickedGizmo = null;

    Rect subGizmoRect = new(rect.x, rect.y, rect.height, rect.height);
    foreach (VehicleTurret.SubGizmo subGizmo in turret.SubGizmos)
    {
      using TextBlock colorBlock = new(Color.white);
      if (!disabled)
      {
        TooltipHandler.TipRegion(subGizmoRect, subGizmo.tooltip);
        if (subGizmo.canClick() && Mouse.IsOver(subGizmoRect))
        {
          mouseOver = true;
          GUI.color = GenUI.SubtleMouseoverColor;
          if (Widgets.ButtonInvisible(subGizmoRect))
          {
            clickedGizmo = subGizmo;
          }
        }
      }
      subGizmo.drawGizmo(subGizmoRect);
      subGizmoRect.x += SubIconSize;
    }
    return clickedGizmo;
  }

  protected virtual void DrawBottomBar(Rect rect, ref bool mouseOver)
  {
    Widgets.FillableBar(rect, (float)turret.shellCount / turret.def.magazineCapacity,
      TexData.FullBarTex, TexData.EmptyBarTex, true);

    using (new TextBlock(GameFont.Small, TextAnchor.MiddleCenter))
    {
      string ammoCountLabel = $"{turret.shellCount:F0} / {turret.def.magazineCapacity:F0}";
      if (turret.def.magazineCapacity <= 0)
      {
        ammoCountLabel = "\u221E";
        Text.Font = GameFont.Medium;
      }
      Widgets.Label(rect, ammoCountLabel);
    }

    if (turret.def.ammunition != null)
    {
      Rect configureRect = new(rect.xMax - ConfigureButtonSize,
        rect.yMax - ConfigureButtonSize, ConfigureButtonSize, ConfigureButtonSize);
      TooltipHandler.TipRegionByKey(configureRect, "VF_SetReloadLevelTooltip");
      if (Mouse.IsOver(configureRect))
      {
        mouseOver = true;
      }
      if (Widgets.ButtonImageFitted(configureRect, VehicleTex.Settings))
      {
        const int min = 0;

        ThingDef ammoDef = turret.loadedAmmo ??
          turret.def.ammunition.AllowedThingDefs.FirstOrDefault();
        int max = Mathf.RoundToInt(
          vehicle.VehicleDef.GetStatValueAbstract(VehicleStatDefOf.CargoCapacity) /
          ammoDef.GetStatValueAbstract(StatDefOf.Mass));
        Dialog_Slider dialog_Slider = new(TextLabel, min, max,
          delegate(int value) { vehicle.CompVehicleTurrets.SetQuotaLevel(turret, value); },
          vehicle.CompVehicleTurrets.GetQuotaLevel(turret), 5);
        Find.WindowStack.Add(dialog_Slider);
      }
    }
    return;

    static string TextLabel(int amount)
    {
      return "VF_SetReloadLevel".Translate(amount);
    }
  }
}