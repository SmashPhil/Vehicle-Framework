using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace Vehicles
{
	public class AirdropDef : ThingDef
	{
		public GraphicData parachuteGraphicData;

		public List<AnchorPoint> ropes;

		public class AnchorPoint
		{
			public Vector2 from;
			public Vector2 to;
			public int layer;
		}
	}
}
