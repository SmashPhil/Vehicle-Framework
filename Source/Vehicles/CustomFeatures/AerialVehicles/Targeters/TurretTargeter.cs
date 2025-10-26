using System;
using RimWorld;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Vehicles
{
  public class TurretTargeter : BaseTargeter
  {
    private Action<LocalTargetInfo> action;
    private TargetingParameters targetParams;
    private Map map;

    public static TurretTargeter Instance { get; private set; }

    public static VehicleTurret Turret { get; private set; }

    public override bool IsTargeting => action != null;

    private bool TargeterValid
    {
      get
      {
        if (vehicle is null || vehicle.Map != Find.CurrentMap || vehicle.Destroyed)
          return false;

        if (Turret is null || Turret.ComponentDisabled)
          return false;

        return Find.Selector.IsSelected(vehicle);
      }
    }

    public static void BeginTargeting(TargetingParameters targetParams,
      Action<LocalTargetInfo> action, VehicleTurret turret, Action actionWhenFinished = null,
      Texture2D mouseAttachment = null)
    {
      Instance.action = action;
      Instance.targetParams = targetParams;
      Instance.vehicle = turret.vehicle;
      Turret = turret;
      Turret.SetTarget(LocalTargetInfo.Invalid);
      Instance.actionWhenFinished = actionWhenFinished;
      Instance.mouseAttachment = mouseAttachment;
      Instance.map = turret.vehicle.Map;

      Instance.OnStart();
      Turret.StartTicking();
    }

    public override void StopTargeting()
    {
      actionWhenFinished?.Invoke();

      Turret = null;
      action = null;
      actionWhenFinished = null;
    }

    public void StopTargeting(bool canceled)
    {
      if (canceled && Turret != null)
      {
        Turret.AlignToAngleRestricted(Turret.TurretRotation);
        Turret.SetTarget(LocalTargetInfo.Invalid);
      }

      StopTargeting();
    }

    public override void ProcessInputEvents()
    {
      if (!TargeterValid)
      {
        StopTargeting(true);
        return;
      }

      if (IsTargeting)
      {
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
          Event.current.Use();
          if (action != null)
          {
            LocalTargetInfo obj = CurrentTargetUnderMouse();
            if (obj.Cell.InBounds(map) &&
              TargetingHelper.TargetMeetsRequirements(Turret, obj, out _))
            {
              action(obj);
              SoundDefOf.Tick_High.PlayOneShotOnCamera();
              StopTargeting(false);
            }
            else
            {
              SoundDefOf.ClickReject.PlayOneShotOnCamera();
            }
          }
        }

        if ((Event.current.type == EventType.MouseDown && Event.current.button == 1) ||
          KeyBindingDefOf.Cancel.KeyDownEvent)
        {
          StopTargeting(true);
          SoundDefOf.CancelMode.PlayOneShotOnCamera();
          Event.current.Use();
        }
      }
    }

    public override void TargeterOnGUI()
    {
      if (action != null)
      {
        if (TargetingHelper.TargetMeetsRequirements(Turret, CurrentTargetUnderMouse(), out _))
        {
          Texture2D icon = mouseAttachment ?? TexCommand.Attack;
          GenUI.DrawMouseAttachment(icon);
        }
      }
    }

    public override void TargeterUpdate()
    {
      if (IsTargeting)
      {
				vehicle.ResetIdleTicks();
				LocalTargetInfo mouseTarget = CurrentTargetUnderMouse();
        if (TargetingHelper.TargetMeetsRequirements(Turret, mouseTarget, out _))
        {
          GenDraw.DrawTargetHighlight(mouseTarget);
          if (Turret.CurrentFireMode.forcedMissRadius > 1)
          {
            GenDraw.DrawRadiusRing(mouseTarget.Cell, Turret.CurrentFireMode.forcedMissRadius);
          }

          if (mouseTarget != Turret.vehicle)
          {
            float angle = Turret.TurretLocation.AngleToPoint(mouseTarget.Thing?.DrawPos ??
              mouseTarget.Cell.ToVector3Shifted());
            Turret.AlignToAngleRestricted(angle);
          }
        }
      }
    }

    protected override LocalTargetInfo CurrentTargetUnderMouse()
    {
      if (!IsTargeting)
      {
        return LocalTargetInfo.Invalid;
      }

      return GenUI.TargetsAtMouse(targetParams).FirstOrFallback(LocalTargetInfo.Invalid);
    }

    public override void PostInit()
    {
      Instance = this;
    }
  }
}