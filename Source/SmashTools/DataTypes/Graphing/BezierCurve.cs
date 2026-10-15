using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public class BezierCurve : LinearCurve
	{
		public BezierCurve() : base()
		{
		}

		public BezierCurve(List<CurvePoint> points) : base(points)
		{
		}

		/// <summary>
		/// Interpolation using Bernstein polynomial
		/// </summary>
		/// <remarks>See https://en.wikipedia.org/wiki/Bernstein_polynomial for reference</remarks>
		/// <param name="controlPoints"></param>
		/// <param name="t"></param>
		private static Vector2 BezierFunction(List<CurvePoint> controlPoints, float t)
		{
			int n = controlPoints.Count - 1;
			if (n > 16)
			{
				Log.Error("Max number of control points is 16, factorials are precalculated.");
				n = 16;
			}

			Vector2 lerp = Vector2.zero;
			for (int i = 0; i < controlPoints.Count; i++)
			{
				lerp += Ext_Math.Bernstein(n, i, t) * controlPoints[i].Loc;
			}
			return lerp;
		}

		public override Vector2 Function(float x)
		{
			if (points.Count < 3)
			{
				//No control points -> linear curve
				return base.Function(x);
			}
			if (ValueLimit(x, out float y))
			{
				return new Vector2(x, y);
			}
			if (x <= LeftBound.x)
			{
				return LeftBound;
			}
			else if (x >= RightBound.x)
			{
				return RightBound;
			}
			float t = (x - LeftBound.x) / (RightBound.x - LeftBound.x);
			return BezierFunction(points, t);
		}

		public override Vector2 EvaluateT(float t)
		{
			if (t <= 0)
			{
				return LeftBound;
			}
			if (t >= 1)
			{
				return RightBound;
			}
			return BezierFunction(points, t);
		}
	}
}
