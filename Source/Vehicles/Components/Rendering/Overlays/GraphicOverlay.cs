using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class GraphicOverlay
	{
		[TweakField]
		public GraphicDataOverlay data;

		public GraphicOverlay(GraphicDataOverlay graphicDataOverlay)
		{
			data = graphicDataOverlay;
		}
	}
}
