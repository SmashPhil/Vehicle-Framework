using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using SmashTools;

namespace AnimationKit.Editor;

[PublicAPI]
[StructLayout(LayoutKind.Sequential, Size = 7)]
public readonly struct KeyFrame : IComparable<KeyFrame>
{
  private const float DefaultWeight = 0.333f;

  public readonly uint frame;
  public readonly float value;
  public readonly float inTangent;
  public readonly float outTangent;
  public readonly float inWeight;
  public readonly float outWeight;
  public readonly WeightMode weightedMode;

  [StructLayout(LayoutKind.Sequential, Size = 8)]
  internal struct Result
  {
    public KeyFrame keyFrame;
    public ErrorType error;
  }

  public KeyFrame(uint frame, float value)
    : this(frame, value, 0, 0)
  {
    this.frame = frame;
    this.value = value;
  }

  public KeyFrame(uint frame, float value, float inTangent, float outTangent)
    : this(frame, value, inTangent, outTangent, DefaultWeight, DefaultWeight)
  {
    this.frame = frame;
    this.value = value;
    this.inTangent = inTangent;
    this.outTangent = outTangent;
  }

  public KeyFrame(uint frame, float value, float inTangent, float outTangent, float inWeight, float outWeight)
    : this(frame, value, inTangent, outTangent, inWeight, outWeight, WeightMode.None)
  {
    this.frame = frame;
    this.value = value;
    this.inTangent = inTangent;
    this.outTangent = outTangent;
    this.inWeight = inWeight;
    this.outWeight = outWeight;
  }

  public KeyFrame(uint frame, float value, float inTangent, float outTangent, float inWeight, float outWeight,
    WeightMode weightedMode)
  {
    this.frame = frame;
    this.value = value;
    this.inTangent = inTangent;
    this.outTangent = outTangent;
    this.inWeight = inWeight;
    this.outWeight = outWeight;
    this.weightedMode = weightedMode;
  }

  public static KeyFrame Invalid => new(0, 0);

  public override string ToString()
  {
    return $"({frame},{value.RoundTo(0.0001f)}," +
      $"{inTangent.RoundTo(0.0001f)},{outTangent.RoundTo(0.0001f)}," +
      $"{inWeight.RoundTo(0.0001f)},{outWeight.RoundTo(0.0001f)})";
  }

  int IComparable<KeyFrame>.CompareTo(KeyFrame other)
  {
    return frame.CompareTo(other.frame);
  }
}
