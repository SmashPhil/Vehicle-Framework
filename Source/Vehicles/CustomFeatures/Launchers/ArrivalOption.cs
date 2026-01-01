using System;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools.Targeting;

namespace Vehicles.World;

[PublicAPI]
public class ArrivalOption : ITargetOption
{
  public readonly string label;
  public readonly IArrivalAction arrivalAction;

  // TODO VF-82: This needs some final cleanup. Rather than some obscure delegate being passed around, we should kick off
  // a continuation targeter that can be configured by the caller. The delegate is so we can use the old targeter for now.
  public readonly Action<TargetData<GlobalTargetInfo>> continueWith;

  public ArrivalOption(string label, IArrivalAction arrivalAction)
  {
    this.label = label;
    this.arrivalAction = arrivalAction;
  }

  public ArrivalOption(string label, Action<TargetData<GlobalTargetInfo>> continueWith)
  {
    this.label = label;
    this.continueWith = continueWith;
  }

  public FloatMenuAcceptanceReport AcceptanceReport { get; init; } = FloatMenuAcceptanceReport.WasAccepted;

  string ITargetOption.Label => label;
}