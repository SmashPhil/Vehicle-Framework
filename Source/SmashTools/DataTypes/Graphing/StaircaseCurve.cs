using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace SmashTools
{
	public class StaircaseCurve : LinearCurve
	{
		public StaircaseCurve() : base()
		{
		}

		public StaircaseCurve(List<CurvePoint> points) : base(points)
		{
		}

		public override Vector2 Function(float x)
		{
			if (points.NullOrEmpty())
			{
				return Vector2.zero;
			}
			if (x <= LeftBound.x)
			{
				return LeftBound;
			}
			else if (x >= RightBound.x)
			{
				return RightBound;
			}
			if (ValueLimit(x, out float y))
			{
				return new Vector2(x, y);
			}
			CurvePoint leftPoint = points[0];
			for (int i = 0; i < points.Count; i++)
			{
				if (x <= points[i].x)
				{
					if (i > 0)
					{
						leftPoint = points[i - 1];
					}
					break;
				}
			}
			return new Vector2(x, leftPoint.y);
		}
	}
}
