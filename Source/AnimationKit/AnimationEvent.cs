using System;
using SmashTools;
using ISelectableUI = AnimationKit.Editor.ISelectableUI;

namespace AnimationKit;

public class AnimationEvent : ISelectableUI, IComparable<AnimationEvent>
{
  public uint frame;
  public DynamicDelegate method;

  int IComparable<AnimationEvent>.CompareTo(AnimationEvent other)
  {
    if (frame < other.frame) return -1;
    if (frame > other.frame) return 1;
    return 0;
  }
}