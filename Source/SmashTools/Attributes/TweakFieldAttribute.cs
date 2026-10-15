using System;

namespace SmashTools;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field)]
public class TweakFieldAttribute : Attribute
{
  public string Category { get; set; }

  public string SubCategory { get; set; }

  public UISettingsType SettingsType { get; set; } = UISettingsType.None;
}