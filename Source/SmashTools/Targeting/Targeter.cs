using System.Collections.Generic;
using CoreLib.Collections;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Verse.Sound;

namespace SmashTools.Targeting;

[PublicAPI]
public abstract class Targeter<T> : ITargeter
{
  private readonly ITargeterUpdate<T> updater;

  protected readonly TargetData<T> targetData;

  protected Targeter([CanBeNull] ITargeterUpdate<T> updater)
  {
    targetData = new TargetData<T>();
    this.updater = updater;
  }

  protected abstract TargeterResult PrimaryClick();

  protected virtual TargeterResult SecondaryClick()
  {
    return TargeterResult.Cancel;
  }

  public virtual void OnStart()
  {
  }

  public virtual void OnStop()
  {
  }

  public virtual void OnGUI()
  {
    UI.ApplyUIScale();
    ProcessInput();
    updater?.TargeterOnGUI();
  }

  public virtual void Update()
  {
    updater?.TargeterUpdate(in targetData);
  }

  public TargetValidation ValidateTargets()
  {
    if (updater == null)
      return TargetValidation.Success;

    ReadOnlyList<T> targets = new ReadOnlyList<T>(targetData.targets);
    return updater.ValidateTargets(targets);
  }

  protected abstract void Submit(ITargetOption option);

  private void Finalize(List<ITargetOption> options)
  {
    if (options.NullOrEmpty())
    {
      Trace.Fail("Finalizing results with no options to choose.");
      this.Stop();
      return;
    }
    if (options.Count == 1)
    {
      ChooseOption(options[0]);
      return;
    }
    List<FloatMenuOption> floatMenuOptions = [];
    foreach (ITargetOption option in options)
    {
      floatMenuOptions.Add(new FloatMenuOption(option.Label, () => ChooseOption(option)));
    }
    Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
    return;

    void ChooseOption(ITargetOption option)
    {
      Submit(option);
      this.Stop();
    }
  }

  private void ProcessInput()
  {
    if (Event.current is { type: EventType.MouseDown })
    {
      TargeterResult result = Event.current.button switch
      {
        0 => PrimaryClick(),
        1 => SecondaryClick(),
        _ => TargeterResult.None
      };

      switch (result.action)
      {
        case TargeterAction.Reject:
          SoundDefOf.ClickReject.PlayOneShotOnCamera();
        break;
        case TargeterAction.Cancel:
          SoundDefOf.CancelMode.PlayOneShotOnCamera();
          this.Stop();
        break;
        case TargeterAction.Submit:
          Assert.IsFalse(result.options.NullOrEmpty());
          Finalize(result.options);
        break;
        case TargeterAction.Accept:
        case TargeterAction.None:
        default:
        break;
      }
      Event.current.Use();
    }
    if (KeyBindingDefOf.Cancel.KeyDownEvent)
    {
      SoundDefOf.CancelMode.PlayOneShotOnCamera();
      this.Stop();
      Event.current.Use();
    }
  }
}