using System;
using JetBrains.Annotations;

namespace AnimationKit;

[PublicAPI, UsedWithReflection]
[AttributeUsage(AttributeTargets.Field)]
public class AnimationPropertyAttribute : Attribute
{
  public string Name { get; set; }
}