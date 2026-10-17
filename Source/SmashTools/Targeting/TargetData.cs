using System.Collections.Generic;

namespace SmashTools.Targeting;

// TODO 1.8 - Change to readonly list for better guarantee over immutability.
public readonly struct TargetData<T>()
{
  public readonly List<T> targets = [];
}