using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class GraphicOverlay
	{
		//[TweakField]
		//public GraphicData graphicData;
		//[TweakField(SettingsType = UISettingsType.SliderFloat)]
		//[SliderValues(MinValue = 0, MaxValue = 360, RoundDecimalPlaces = 0, Increment = 1)]
		//public float rotation = 0;

		[TweakField]
		public GraphicDataOverlay data;

		public GraphicOverlay(GraphicDataOverlay graphicDataOverlay)
		{
			this.data = graphicDataOverlay;
		}
	}
}
