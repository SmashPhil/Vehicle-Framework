using SmashTools;

namespace Vehicles
{
  public class GraphicDataOverlay
  {
    public string identifier = null;

    [TweakField]
    public GraphicDataRGB graphicData;
    [TweakField(SettingsType = UISettingsType.SliderFloat)]
    [SliderValues(MinValue = 0, MaxValue = 360, RoundDecimalPlaces = 0, Increment = 1)]
    public float rotation = 0;

    public bool dynamicShadows;

    public ComponentRendering component;

    public bool renderUI = true;

    public class ComponentRendering
    {
      public string key;
      public float healthPercent;
      public ComparisonType comparison = ComparisonType.GreaterThan;
    }
  }
}
