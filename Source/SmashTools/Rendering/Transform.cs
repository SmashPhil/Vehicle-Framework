using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace SmashTools.Rendering;

[PublicAPI]
public sealed class Transform : ITweakFields, IExposable
{
  private static readonly Vector3 DefaultScale = Vector3.one;

  [TweakField(SettingsType = UISettingsType.FloatBox)]
  public Vector3 position;

  [TweakField(SettingsType = UISettingsType.SliderFloat)]
  [SliderValues(MinValue = 0, MaxValue = 360, Increment = 1, RoundDecimalPlaces = 0)]
  public float rotation;

  [TweakField(SettingsType = UISettingsType.FloatBox)]
  public Vector3 scale = DefaultScale;

  string ITweakFields.Category => null;

  string ITweakFields.Label => "Transform";

  void IExposable.ExposeData()
  {
    Scribe_Values.Look(ref position, nameof(position));
    Scribe_Values.Look(ref rotation, nameof(rotation));
    Scribe_Values.Look(ref scale, nameof(scale), defaultValue: DefaultScale);
  }

  public void Reset()
  {
    position = Vector3.zero;
    rotation = 0;
    scale = DefaultScale;
  }

  void ITweakFields.OnFieldChanged()
  {
  }
}