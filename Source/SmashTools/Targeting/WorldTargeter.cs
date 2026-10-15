using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SmashTools.Targeting;

public class WorldTargeter<TPayload> : Targeter<GlobalTargetInfo> where TPayload : ITargetOption
{
  private readonly ITargeterSource<GlobalTargetInfo, TPayload> source;

  private GlobalTargetInfo curTarget;
  private TargetValidation curResult;

  private bool closeWorldTabWhenFinished;

  public WorldTargeter(ITargeterSource<GlobalTargetInfo, TPayload> source) : base(null)
  {
    this.source = source;
  }

  public WorldTargeter(ITargeterSource<GlobalTargetInfo, TPayload> source,
    ITargeterUpdate<GlobalTargetInfo> updater) : base(updater)
  {
    this.source = source;
  }

  public Texture2D TargetTexture { get; init; }

  public override void OnStart()
  {
    closeWorldTabWhenFinished = !WorldRendererUtility.WorldRendered;
  }

  public override void OnStop()
  {
    if (closeWorldTabWhenFinished)
      CameraJumper.TryHideWorld();
  }

  protected override TargeterResult PrimaryClick()
  {
    if (!curResult.isValid)
      return TargeterResult.Reject;

    TargeterResult result = source.Select(curTarget);

    if (targetData.targets.Count > 0 && curTarget == targetData.targets[^1])
      return TargeterResult.Submit with { options = result.options };

    if (result.action is TargeterAction.Accept or TargeterAction.Submit)
    {
      targetData.targets.Add(curTarget);
    }
    return result;
  }

  protected override TargeterResult SecondaryClick()
  {
    if (!targetData.targets.NullOrEmpty())
    {
      targetData.targets.Pop();
      return TargeterResult.None;
    }
    return base.SecondaryClick();
  }

  protected override void Submit(ITargetOption option)
  {
    SoundDefOf.Tick_High.PlayOneShotOnCamera();
    source.OnTargetingFinished(targetData, (TPayload)option);
  }

  public override void OnGUI()
  {
    const float OffsetFromMouse = 8f;
    const float MouseIconSize = 32f;

    base.OnGUI();

    string tooltip = curResult.Tooltip;
    if (!tooltip.NullOrEmpty())
    {
      Vector2 mousePosition = Event.current.mousePosition;
      Rect iconRect = new(mousePosition.x + OffsetFromMouse, mousePosition.y + OffsetFromMouse,
        MouseIconSize, MouseIconSize);
      if (TargetTexture)
      {
        GUI.DrawTexture(iconRect, TargetTexture);
      }
      Vector2 labelGetterText = Text.CalcSize(tooltip);
      Rect rect = new(iconRect.xMax, iconRect.y, 9999f, 100f);
      Rect bgRect = new(rect.x - labelGetterText.x * 0.1f, rect.y, labelGetterText.x * 1.2f,
        labelGetterText.y);
      GUI.DrawTexture(bgRect, TexUI.GrayTextBG);
      Widgets.Label(rect, tooltip);
    }
  }

  public override void Update()
  {
    if (!source.TargeterValid || !WorldRendererUtility.WorldRendered)
    {
      this.Stop();
      return;
    }

    base.Update();
    UpdateTargetUnderMouse();
  }

  private void UpdateTargetUnderMouse()
  {
    curTarget = GlobalTargetInfo.Invalid;
    curResult = ValidateTargets();
    if (!curResult)
      return;

    List<WorldObject> objects = GenWorldUI.WorldObjectsUnderMouse(UI.MousePositionOnUI);
    if (objects.Count > 0)
    {
      TargetValidation objResult = TargetValidation.Failed;
      foreach (WorldObject obj in objects)
      {
        TargetValidation targetResult = source.CanTarget(obj);
        curTarget = obj;
        objResult = targetResult;

        if (targetResult.isValid)
          break;
      }
      if (!objResult || !objResult.Tooltip.NullOrEmpty())
      {
        curResult = objResult;
      }
      return;
    }
    PlanetTile tile = GenWorld.MouseTile();
    if (!tile.Valid)
      return;

    curTarget = new GlobalTargetInfo(tile);
    TargetValidation sourceResult = source.CanTarget(curTarget);
    if (!sourceResult || !sourceResult.Tooltip.NullOrEmpty())
    {
      curResult = sourceResult;
    }
  }
}