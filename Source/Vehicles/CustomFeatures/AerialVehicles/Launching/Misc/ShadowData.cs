using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vehicles
{
	public class ShadowData
	{
		public float width;
		public float height;
		public float alpha;

		public static ShadowData CreateFrom(VehiclePawn vehicle)
		{
			ShadowData shadowData = new ShadowData();
			Vector2 shadowSize = vehicle.VehicleGraphic.data.drawSize;
			shadowData.width = shadowSize.x;
			shadowData.height = shadowSize.y;
			shadowData.alpha = 1;

			return shadowData;
		}

		public bool Invalid => width <= 0 && height <= 0;
	}
}
