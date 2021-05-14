using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public static class SkyfallerHelper
	{
		public static Vector3 DrawPos_Accelerate(Vector3 center, int ticks, float angle, float speed)
		{
			float dist = Mathf.Pow(ticks, 0.95f) * 1.7f * speed;
			return PosAtDist(center, dist, angle);
		}

		public static Vector3 DrawPos_ConstantSpeed(Vector3 center, int ticks, float angle, float speed)
		{
			float dist = ticks * speed;
			return PosAtDist(center, dist, angle);
		}

		public static Vector3 DrawPos_Decelerate(Vector3 center, int ticks, float angle, float speed)
		{
			float dist = (ticks * ticks) * 0.00721f * speed;
			return PosAtDist(center, dist, angle);
		}

		private static Vector3 PosAtDist(Vector3 center, float dist, float angle)
		{
			return center + Vector3Utility.FromAngleFlat(angle - 90f) * dist;
		}
	}
}
