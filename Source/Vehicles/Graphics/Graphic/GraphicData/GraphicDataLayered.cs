using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class GraphicDataLayered : GraphicData
	{
		protected int layer = 0;

		public Vector3? OriginalDrawOffset { get; private set; }

		public GraphicDataLayered() : base()
		{
		}

		public virtual void CopyFrom(GraphicDataLayered graphicData)
		{
			base.CopyFrom(graphicData);
			layer = graphicData.layer;
		}

		public virtual void Init()
		{
			OriginalDrawOffset ??= drawOffset;
			drawOffset = OriginalDrawOffset.Value;
			float layerOffset = layer * (Altitudes.AltInc / Enum.GetNames(typeof(AltitudeLayer)).EnumerableCount());
			drawOffset.y += layerOffset;
		}
	}
}
