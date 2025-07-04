using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace Vehicles;

[PublicAPI]
public class IndicatorDef : Def
{
  public string iconPath;

  // TODO - add options for additional info panel

  public Texture2D Icon { get; private set; }

  public override void PostLoad()
  {
    if (!string.IsNullOrEmpty(iconPath))
    {
      LongEventHandler.ExecuteWhenFinished(delegate
      {
        Icon = ContentFinder<Texture2D>.Get(iconPath);
      });
    }
  }
}