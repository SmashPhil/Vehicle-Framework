using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SmashTools
{
  public class LagrangeCurve : LinearCurve
  {
    public LagrangeCurve() : base()
    {
    }

    public LagrangeCurve(List<CurvePoint> points) : base(points)
    {
    }

    /// <summary>
    /// Interpolation using Lagrange polynomial
    /// </summary>
    /// <remarks>
    /// See <see href="https://paulbourke.net/miscellaneous/interpolation"/>
    /// and <see href="https://en.wikipedia.org/wiki/Lagrange_polynomial"/> for reference
    /// </remarks>
    /// <param name="coordinates"></param>
    /// <param name="x"></param>
    private static Vector2 LagrangeFunction(List<CurvePoint> coordinates, float x)
    {
      float y = 0;
      // Σ{n-1:i=0} y * ∏{n-1:j=0,j≠i} (x - xj) / (xi - xj)
      // Summation from 0 to n-1, and multiplicative of 0 to n-1, given an arbitrary set of points
      for (int i = 0; i < coordinates.Count; i++)
      {
        CurvePoint point_i = coordinates[i];
        float numerator = point_i.y;
        float denominator = 1;
        for (int j = 0; j < coordinates.Count; j++)
        {
          if (i != j)
          {
            CurvePoint point_j = coordinates[j];
            numerator *= x - point_j.x;
            denominator *= point_i.x - point_j.x;
          }
        }

        y += numerator / denominator;
      }

      return new Vector2(x, y);
    }

    public override Vector2 Function(float x)
    {
      if (points.Count < 2)
      {
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

      return LagrangeFunction(points, x);
    }

    public override Vector2 EvaluateT(float t)
    {
      if (t <= 0)
        return LeftBound;

      if (t >= 1)
        return RightBound;

      return LagrangeFunction(points, t);
    }
  }
}