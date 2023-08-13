using SmashTools;

namespace Vehicles
{
	public class GraphicDataOverlay
	{
		[TweakField]
		public GraphicDataLayered graphicData;
		[TweakField(SettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 360, RoundDecimalPlaces = 0, Increment = 1)]
		public float rotation = 0;

		public bool renderUI = true;
	}
}
