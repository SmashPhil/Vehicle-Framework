using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class GraphicOverlay //: IMaterialCacheTarget
	{
		[TweakField]
		public GraphicDataOverlay data;

		private VehiclePawn vehicle;

		public GraphicOverlay(GraphicDataOverlay graphicDataOverlay)
		{
			data = graphicDataOverlay;
		}

		public GraphicOverlay(GraphicDataOverlay graphicDataOverlay, VehiclePawn vehicle)
		{
			data = graphicDataOverlay;
			this.vehicle = vehicle;
		}

		public int MaterialCount => vehicle.VehicleDef.MaterialCount;

		public PatternDef PatternDef => PatternDefOf.Default;

		public string Name => $"{vehicle.VehicleDef.Name}_{data.graphicData.texPath}";
	}
}
