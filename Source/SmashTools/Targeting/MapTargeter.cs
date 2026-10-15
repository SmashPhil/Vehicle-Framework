using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SmashTools.Targeting;

// TODO VF-337 - Complete and replace LandingTargeter with more thorough testing.
public class MapTargeter<TPayload> : Targeter<LocalTargetInfo> where TPayload : ITargetOption
{
  private readonly ITargeterSource<LocalTargetInfo, TPayload> source;

  private LocalTargetInfo curTarget;
  private TargetValidation curResult;

  private bool closeWorldTabWhenFinished;

  public MapTargeter(ITargeterSource<LocalTargetInfo, TPayload> source) : base(null)
  {
    this.source = source;
  }

  public MapTargeter(ITargeterSource<LocalTargetInfo, TPayload> source,
    ITargeterUpdate<LocalTargetInfo> updater) : base(updater)
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
    TargeterResult result = source.Select(curTarget);
    if (targetData.targets.Count > 0 && curTarget == targetData.targets[^1])
      return TargeterResult.Submit with { options = result.options };
    if (result.action is TargeterAction.Accept or TargeterAction.Submit)
      targetData.targets.Add(curTarget);
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
    base.OnGUI();
    if (!curResult.isValid)
      return;

    const float OffsetFromMouse = 8f;
    const float MouseIconSize = 32f;

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
    curTarget = LocalTargetInfo.Invalid;
    curResult = TargetValidation.Failed;

    IntVec3 cell = UI.MouseCell();
    if (!cell.IsValid)
      return;

    curTarget = new LocalTargetInfo(cell);
    curResult = source.CanTarget(curTarget);
  }
}