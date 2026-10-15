using JetBrains.Annotations;
using UnityEngine;

namespace SmashTools;

[PublicAPI]
public class AnimationEvent<T>
{
  public float triggerAt;
  public DynamicDelegate<T> method;
  public AnimationTrigger type = AnimationTrigger.EqualTo;
  public AnimationFrequency frequency = AnimationFrequency.OneShot;

  public bool EventFrame(float t)
  {
    return type switch
    {
      AnimationTrigger.GreaterThan => t >= triggerAt,
      AnimationTrigger.EqualTo     => Mathf.Approximately(t, triggerAt),
      _                            => false,
    };
  }

  public enum AnimationTrigger
  {
    EqualTo,
    GreaterThan,
  }

  public enum AnimationFrequency
  {
    OneShot,
    Continuous
  }
}