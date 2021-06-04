using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class GraphicDataLayered : GraphicData
	{
		public float? layer;

		public float DrawLayer => layer.Value;

		public GraphicDataLayered() : base()
		{
			if (layer is null)
			{
				layer = 0;
			}
		}

		public void CopyFrom(GraphicDataLayered graphicData)
		{
			base.CopyFrom(graphicData);
			layer = graphicData.layer;
		}
	}
}
