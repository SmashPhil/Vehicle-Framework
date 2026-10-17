using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace SmashTools
{
	public class LinearCurve : IEnumerable<CurvePoint>, IEnumerable
	{
		public List<CurvePoint> points = new List<CurvePoint>();

		public List<(CurvePoint lhs, CurvePoint rhs)> values = new List<(CurvePoint lhs, CurvePoint rhs)>();

		public LinearCurve()
		{
		}

		public LinearCurve(List<CurvePoint> points)
		{
			this.points = new List<CurvePoint>(points);
		}

		public CurvePoint LeftBound => points.FirstOrDefault();

		public CurvePoint RightBound => points.LastOrDefault();

		public int PointsCount => points.Count;

		public bool IsValid => !points.NullOrEmpty();

		public CurvePoint this[int i]
		{
			get
			{
				return points[i];
			}
			set
			{
				points[i] = value;
			}
		}

		public virtual void Add(CurvePoint curvePoint)
		{
			points.Add(curvePoint);
		}

		public float Evaluate(float x)
		{
			return Function(x).y;
		}

		public virtual bool ValueLimit(float x, out float y)
		{
			y = 0;
			if (!values.NullOrEmpty())
			{
				for (int i = 0; i < values.Count; i++)
				{
					(CurvePoint lhs, CurvePoint rhs) = values[i];
					if (x >= lhs.x && x <= rhs.x)
					{
						y = lhs.y;
						return true;
					}
				}
			}
			return false;
		}

		public virtual Vector2 Function(float x)
		{
			if (points.NullOrEmpty())
			{
				return Vector2.zero;
			}
			if (points.Count == 1)
			{
				return points[0];
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
			CurvePoint rightPoint = points[points.Count - 1];
			for (int i = 0; i < points.Count; i++)
			{
				if (x <= points[i].x)
				{
					rightPoint = points[i];
					if (i > 0)
					{
						leftPoint = points[i - 1];
					}
					break;
				}
			}
			float t = (x - leftPoint.x) / (rightPoint.x - leftPoint.x);
			return new Vector2(x, Mathf.Lerp(leftPoint.y, rightPoint.y, t));
		}

		public virtual Vector2 EvaluateT(float t)
		{
			if (PointsCount == 0)
			{
				return Vector2.zero;
			}
			if (t <= 0)
			{
				return LeftBound;
			}
			if (t >= 1)
			{
				return RightBound;
			}
			(CurvePoint leftPoint, CurvePoint rightPoint) = LerpPair(t);
			return new Vector2(Mathf.Lerp(leftPoint.x, rightPoint.x, t), Mathf.Lerp(leftPoint.y, rightPoint.y, t));
		}

		private (CurvePoint leftPoint, CurvePoint rightPoint) LerpPair(float t)
		{
			if (points.Count <= 1)
			{
				return (LeftBound, RightBound);
			}
			float totalLength = TotalLength();
			float distAcc = 0;
			for (int i = 0; i < points.Count - 1; i++)
			{
				CurvePoint lhs = points[i];
				CurvePoint rhs = points[i + 1];
				distAcc += Vector2.Distance(lhs, rhs);
				if (t * totalLength <= distAcc)
				{
					return (lhs, rhs);
				}
			}
			return (LeftBound, RightBound);
		}

		private float TotalLength()
		{
			float distX = 0;
			for (int i = 0; i < points.Count - 1; i++)
			{
				CurvePoint lhs = points[i];
				CurvePoint rhs = points[i + 1];
				distX = Vector2.Distance(lhs, rhs);
			}
			return distX;
		}

		public virtual void Graph()
		{
			FloatRange xRange = new FloatRange(LeftBound.x, RightBound.x);
			Find.WindowStack.Add(new Dialog_Graph(Function, xRange, points));
		}

		public static implicit operator Graph.Function(LinearCurve curve)
		{
			return curve.Function;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<CurvePoint> GetEnumerator()
		{
			foreach (CurvePoint point in points)
			{
				yield return point;
			}
		}
	}
}
