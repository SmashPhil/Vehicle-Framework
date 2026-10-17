using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using SmashTools.Rendering;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools;

// TODO 1.8 - Move over non-unity specific math functions to MathUtils
[PublicAPI]
public static class Ext_Math
{
	// Precalculated √2
	public static readonly float Sqrt2 = Mathf.Sqrt(2);

	// Precomputed factorials from 0! to 16!
	private static readonly float[] Factorials =
	[
		1,
		1,
		2,
		6,
		24,
		120,
		720,
		5040,
		40320,
		362880,
		3628800,
		39916800,
		479001600,
		6227020800,
		87178291200,
		1307674368000,
		20922789888000,
	];

	public static float Binomial(int n, int i)
	{
		float a1 = Factorials[n];
		float a2 = Factorials[i];
		float a3 = Factorials[n - i];
		return a1 / (a2 * a3);
	}

	public static float Bernstein(int n, int i, float t)
	{
		float t1 = Mathf.Pow(t, i); // t_i
		float t2 = Mathf.Pow(1 - t, n - i); // t_n-i
		return Binomial(n, i) * t1 * t2;
	}

	/// <summary>
	/// De Casteljau's Algorithm
	/// </summary>
	/// <remarks>See https://en.wikipedia.org/wiki/De_Casteljau%27s_algorithm for reference.</remarks>
	public static Vector2 DeCasteljau(List<CurvePoint> points, float t)
	{
		// Only setup for p-count = 4
		Vector2 p0 = Vector2.Lerp(points[0], points[1], t);
		Vector2 p1 = Vector2.Lerp(points[1], points[2], t);
		Vector2 p2 = Vector2.Lerp(points[2], points[3], t);

		Vector2 r0 = Vector2.Lerp(p0, p1, t);
		Vector2 r1 = Vector2.Lerp(p1, p2, t);

		return Vector2.Lerp(r0, r1, t);
	}

	/// <summary>
	/// <see href="https://en.wikipedia.org/wiki/Smoothstep"/>
	/// </summary>
	/// <param name="start"></param>
	/// <param name="end"></param>
	/// <param name="t"></param>
	/// <exception cref="ArgumentException"></exception>
	public static float SmoothStep(float start, float end, float t)
	{
		if (start >= end)
			throw new ArgumentException(nameof(start));

		return t switch
		{
			<= 0 => start,
			>= 1 => end,
			_    => t * t * (3f - 2f * t)
		};
	}

	/// <summary>
	/// Sign for boolean condition
	/// </summary>
	/// <param name="value"></param>
	/// <returns>True = 1, False = -1</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Sign(bool value)
	{
		return value ? 1 : -1;
	}

	/// <summary>
	/// <paramref name="value"/> is odd.
	/// </summary>
	/// <param name="value"></param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsOdd(this int value)
	{
		return (value & 1) != 0;
	}

	/// <summary>
	/// <paramref name="value"/> is even.
	/// </summary>
	/// <param name="value"></param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsEven(this int value)
	{
		return (value & 1) == 0;
	}

	/// <summary>
	/// Calculates t given a and b and the resulting value between the two bounds.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	public static float ReverseInterpolate(float value, float a, float b)
	{
		// value = (1 - t)a + bt
		// value - a = bt - at
		// (value - a) / (b - a) = t
		return (value - a) / (b - a);
	}

	/// <summary>
	/// Arithmetic series. <see cref="http://en.wikipedia.org/wiki/Arithmetic_progression"/>
	/// </summary>
	/// <exception cref="InvalidOperationException">If n is less than 0.</exception>
	/// <returns>The sum of numbers from <paramref name="a"/> to <paramref name="k"/> given <paramref name="n"/> terms.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ArithmeticSeries(int n, int a, int k)
	{
		if (n < 0)
			throw new InvalidOperationException("n cannot be negative.");

		return n * ((a + k) / 2);
	}

	/// <summary>
	/// Shift <paramref name="cell"/>.
	/// </summary>
	/// <param name="cell"></param>
	/// <param name="dir"></param>
	/// <param name="a">vertical relative to <paramref name="dir"/></param>
	/// <param name="b">horizontal relative to <paramref name="dir"/></param>
	public static IntVec2 Shifted(this IntVec2 cell, Rot4 dir, int a, int b = 0)
	{
		if (!dir.IsValid)
		{
			return cell;
		}
		return dir.AsInt switch
		{
			0 => new IntVec2(cell.x + b, cell.z + a),
			1 => new IntVec2(cell.x + a, cell.z - b),
			2 => new IntVec2(cell.x - b, cell.z - a),
			3 => new IntVec2(cell.x - a, cell.z + b),
			_ => throw new NotImplementedException("Beyond what Rot4 supports")
		};
	}

	/// <summary>
	/// Round to nearest n digits. <paramref name="roundTo"/> is representative of the decimal place. Eg. 0.01 for 2 decimal places.
	/// </summary>
	/// <exception cref="InvalidOperationException">If <paramref name="roundTo"/> is &lt;= 0</exception>
	public static float RoundTo(this float num, float roundTo)
	{
		if (roundTo <= 0)
			throw new InvalidOperationException("roundTo must be greater than 0.");
		return Mathf.Round(num / roundTo) * roundTo;
	}

	/// <summary>
	/// Round to nearest n
	/// </summary>
	/// <exception cref="InvalidOperationException">If <paramref name="roundTo"/> is &lt;= 0</exception>
	public static int RoundTo(this int num, int roundTo)
	{
		if (roundTo <= 0)
			throw new InvalidOperationException("roundTo must be greater than 0.");
		return Mathf.RoundToInt((float)num / roundTo) * roundTo;
	}

	/// <summary>
	/// 2 raised to the power of <paramref name="n"/>
	/// </summary>
	public static int PowTwo(int n)
	{
		return 1 << n;
	}

	/// <summary>
	/// <paramref name="x"/> raised to the power of <paramref name="y"/> for 64bit integers
	/// </summary>
	public static long Pow(this int x, int y)
	{
    Assert.IsTrue(y >= 0);
		return (long)Math.Pow(x, y);
	}

	/// <summary>
	/// Take amount from a value bounded by 0, returning the amount remaining in the value
	/// </summary>
	public static int Take(this int value, int take, out int remaining)
	{
		remaining = 0;
		if (take >= value)
		{
			return value;
		}
		remaining = value - take;
		return take;
	}

	public static Vector2 RotatePointClockwise(this Vector2 coord, float theta)
	{
		return RotatePointClockwise(coord.x, coord.y, theta);
	}

	public static Vector2 RotatePointCounterClockwise(this Vector2 coord, float theta)
	{
		return RotatePointCounterClockwise(coord.x, coord.y, theta);
	}

	/// <summary>
	/// Rotate point clockwise by angle theta
	/// </summary>
	public static Vector2 RotatePointClockwise(float x, float y, float theta)
	{
		return RotatePointCounterClockwise(x, y, -theta);
	}

	/// <summary>
	/// Rotate point counter clockwise by angle theta
	/// </summary>
	public static Vector2 RotatePointCounterClockwise(float x, float y, float theta)
	{
		if (Mathf.Approximately(theta, 0))
			return new Vector2(x, y);
		float radians = theta * Mathf.Deg2Rad;
		float xPrime = x * Mathf.Cos(radians) - y * Mathf.Sin(radians);
		float yPrime = x * Mathf.Sin(radians) + y * Mathf.Cos(radians);
		return new Vector2(xPrime, yPrime);
	}

	/// <summary>
	/// Rotates point around another
	/// </summary>
	/// <param name="point"></param>
	/// <param name="origin"></param>
	/// <param name="angle">In Degrees</param>
	public static Vector3 RotatePoint(Vector3 point, Vector3 origin, float angle)
	{
		float newX = (Mathf.Cos(angle * Mathf.Deg2Rad) * (point.x - origin.x) -
			Mathf.Sin(angle * Mathf.Deg2Rad) * (point.z - origin.z) + origin.x);
		float newZ = (Mathf.Sin(angle * Mathf.Deg2Rad) * (point.x - origin.x) +
			Mathf.Cos(angle * Mathf.Deg2Rad) * (point.z - origin.z) + origin.z);
		return new Vector3(newX, point.y, newZ);
	}

	/// <summary>
	/// Rotates angle clockwise in [0:360] range. Used for clamping angle in this range
	/// </summary>
	/// <param name="angle"></param>
	/// <param name="rotation"></param>
	public static float RotateAngle(float angle, float rotation)
	{
		angle += rotation;
		return angle.ClampAngle();
	}

	/// <summary>
	/// Calculate angle from origin to point on map relative to positive x axis
	/// </summary>
	/// <param name="c"></param>
	/// <param name="map"></param>
	public static double AngleThroughOrigin(this IntVec3 c, Map map)
	{
		int xPrime = c.x - map.Size.x / 2;
		int yPrime = c.z - map.Size.z / 2;
		float slope = (float)yPrime / xPrime;
		float angleRadians = Mathf.Atan(slope);
		float angle = Mathf.Abs(angleRadians * Mathf.Deg2Rad);
		return Quadrant.QuadrantOfIntVec3(c, map).AsInt switch
		{
			2 => 360 - angle,
			3 => 180 + angle,
			4 => 180 - angle,
			_ => angle,
		};
	}

	/// <summary>
	/// Calculate angle between 2 points on Cartesian coordinate plane.
	/// </summary>
	public static float AngleToCell(this IntVec3 pos, IntVec3 point)
	{
		Vector3 posVector = pos.ToVector3Shifted();
		Vector3 pointVector = point.ToVector3Shifted();
		return AngleToPoint(posVector, pointVector);
	}

	/// <summary>
	/// Angle between 2 points in the in-game map. 0 is West, 270 is North
	/// </summary>
	public static float AngleToPointRelative(this Vector3 pos, Vector3 point)
	{
		float xPrime = pos.x - point.x;
		float yPrime = pos.z - point.z;
		return (360 + Mathf.Atan2(yPrime, xPrime) * Mathf.Rad2Deg) % 360;
	}

	/// <summary>
	/// Angle between 2 points
	/// </summary>
	public static float AngleToPoint(float x1, float y1, float x2, float y2)
	{
		return (180 + Mathf.Atan2(x1 - x2, y1 - y2) * Mathf.Rad2Deg) % 360;
	}

	/// <summary>
	/// Angle between 2 points
	/// </summary>
	public static float AngleToPoint(this Vector2 pos, Vector2 point)
	{
		return AngleToPoint(pos.x, pos.y, point.x, point.y);
	}

	/// <summary>
	/// Angle between 2 points relative to <see cref="Vector3.forward"/>
	/// </summary>
	public static float AngleToPoint(this Vector3 pos, Vector3 point)
	{
		return AngleToPoint(pos.x, pos.z, point.x, point.z);
	}

	/// <summary>
	/// Angle between 2 cells in the in-game map. 0 is North, 270 is West
	/// </summary>
	public static float AngleToPoint(this IntVec3 start, IntVec3 end)
	{
		return AngleToPoint(start.ToVector3Shifted(), end.ToVector3Shifted());
	}

	/// <summary>
	/// Returns point from origin given radius and angle
	/// </summary>
	public static Vector3 PointFromAngle(this Vector3 pos, float distance, float angle)
	{
		float x = pos.x + distance * Mathf.Sin(angle * Mathf.Deg2Rad);
		float z = pos.z + distance * Mathf.Cos(angle * Mathf.Deg2Rad);
		return new Vector3(x, pos.y, z);
	}

	/// <summary>
	/// Returns point from origin given radius and angle
	/// </summary>
	public static IntVec3 PointFromAngle(this IntVec3 pos, float distance, float angle)
	{
		int x = Mathf.CeilToInt(pos.x + distance * Mathf.Sin(angle * Mathf.Deg2Rad));
		int z = Mathf.CeilToInt(pos.z + distance * Mathf.Cos(angle * Mathf.Deg2Rad));
		return new IntVec3(x, pos.y, z);
	}

	public static Vector3 PointToEdge(this Vector3 origin, Map map, float angle)
	{
		float clampedAngle = angle.ClampAngle().RoundTo(0.01f);
		float maxX = map.Size.x;
		float maxZ = map.Size.z;
		Vector3 edgePoint = Vector3.zero;

		if (clampedAngle == 0)
		{
			edgePoint = new Vector3(origin.x, origin.y, maxZ);
		}
		else if (clampedAngle == 90)
		{
			edgePoint = new Vector3(maxX, origin.y, origin.z);
		}
		else if (clampedAngle == 180)
		{
			edgePoint = new Vector3(origin.x, origin.y, 0);
		}
		else if (clampedAngle == 270)
		{
			edgePoint = new Vector3(0, origin.y, origin.z);
		}
		else if (clampedAngle >= 0 && clampedAngle <= 45) //1
		{
			float relativeAngle = clampedAngle;
			float x = origin.x + ((maxZ - origin.z) * Mathf.Tan(relativeAngle * Mathf.Deg2Rad));
			edgePoint = new Vector3(x, origin.y, maxZ);
		}
		else if (clampedAngle >= 45 && clampedAngle <= 90) //2
		{
			float relativeAngle = 90 - clampedAngle;
			float z = origin.z + ((maxX - origin.x) * Mathf.Tan(relativeAngle * Mathf.Deg2Rad));
			edgePoint = new Vector3(maxX, origin.y, z);
		}
		else if (clampedAngle >= 90 && clampedAngle <= 135) //3
		{
			float relativeAngle = clampedAngle - 90;
			float z = origin.z - ((maxX - origin.x) * Mathf.Tan(relativeAngle * Mathf.Deg2Rad));
			edgePoint = new Vector3(maxX, origin.y, z);
		}
		else if (clampedAngle >= 135 && clampedAngle <= 180) //4
		{
			float relativeAngle = 180 - clampedAngle;
			float x = origin.x + (origin.z * Mathf.Tan(relativeAngle * Mathf.Deg2Rad));
			edgePoint = new Vector3(x, origin.y, 0);
		}
		else if (clampedAngle >= 180 && clampedAngle <= 225) //5
		{
			float relativeAngle = clampedAngle - 180;
			float x = origin.x - (origin.z * Mathf.Tan(relativeAngle * Mathf.Deg2Rad));
			edgePoint = new Vector3(x, origin.y, 0);
		}
		else if (clampedAngle >= 225 && clampedAngle <= 270) //6
		{
			float relativeAngle = 270 - clampedAngle;
			float z = origin.z - (origin.x * Mathf.Tan(relativeAngle * Mathf.Deg2Rad));
			edgePoint = new Vector3(0, origin.y, z);
		}
		else if (clampedAngle >= 270 && clampedAngle <= 315) //7
		{
			float relativeAngle = clampedAngle - 270;
			float z = origin.z + (origin.x * Mathf.Tan(relativeAngle * Mathf.Deg2Rad));
			edgePoint = new Vector3(0, origin.y, z);
		}
		else if (clampedAngle >= 315 && clampedAngle <= 360) //8
		{
			float relativeAngle = 360 - clampedAngle;
			float x = origin.x - ((maxZ - origin.z) * Mathf.Tan(relativeAngle * Mathf.Deg2Rad));
			edgePoint = new Vector3(x, origin.y, maxZ);
		}
		return edgePoint;
	}

	/// <summary>
	/// Get point on edge of square map given angle (0 to 360) relative to x axis from origin
	/// </summary>
	/// <param name="angle"></param>
	/// <param name="map"></param>
	public static IntVec3 PointFromOrigin(float angle, Map map)
	{
		int a = map.Size.x;
		int b = map.Size.z;

		if (angle < 0 || angle > 360)
		{
			return IntVec3.Invalid;
		}

		Rot4 rayDir = Rot4.Invalid;
		if (angle <= 45 || angle > 315)
		{
			rayDir = Rot4.East;
		}
		else if (angle <= 135 && angle >= 45)
		{
			rayDir = Rot4.North;
		}
		else if (angle <= 225 && angle >= 135)
		{
			rayDir = Rot4.West;
		}
		else if (angle <= 315 && angle >= 225)
		{
			rayDir = Rot4.South;
		}
		else
		{
			return new IntVec3(b / 2, 0, 1);
		}
		float v = Mathf.Tan(angle * Mathf.Deg2Rad);
		return rayDir.AsInt switch
		{
			//North
			0 => new IntVec3((int)(b / (2 * v) + b / 2), 0, b - 1),
			//East
			1 => new IntVec3(a - 1, 0, (int)(a / 2 * v) + a / 2),
			//South
			2 => new IntVec3((int)(b - (b / (2 * v) + b / 2)), 0, 1),
			//West
			3 => new IntVec3(1, 0, (int)(a - ((a / 2 * v) + a / 2))),
			//Fallthrough - should never hit
			_ => IntVec3.Invalid,
		};
	}

	/// <summary>
	/// Spherical distance from <paramref name="source"/> to <paramref name="target"/> on the World Map approximated by tiles
	/// </summary>
	/// <param name="source"></param>
	/// <param name="target"></param>
	public static float SphericalDistance(Vector3 source, Vector3 target)
	{
		float sphericalDistance = GenMath.SphericalDistance(source.normalized, target.normalized);
		return Find.WorldGrid.ApproxDistanceInTiles(sphericalDistance);
	}

	public static List<LineSegment> GetLineSegmentsFromCircle(float radius)
	{
		const float TwoPi = Mathf.PI * 2;

		List<LineSegment> segments = [];
		int segmentCount = Mathf.Clamp(Mathf.RoundToInt(24f * radius), 12, 48);
		float theta = TwoPi / segmentCount;
		float cosT = Mathf.Cos(theta);
		float sinT = Mathf.Sin(theta);

		Vector3 prev = new(radius, 0, 0);
		for (int i = 0; i < segmentCount; i++)
		{
			Vector3 dir = prev;
			// Rotate that vector by θ
			float nextX = dir.x * cosT - dir.z * sinT;
			float nextZ = dir.x * sinT + dir.z * cosT;

			Vector3 next = new(nextX, 0f, nextZ);
			segments.Add(new LineSegment(prev, next));

			prev = next;
		}
		return segments;
	}

	public static List<LineSegment> GetLineSegmentsFromCone(Vector2 coneAngle, float minRange,
		float maxRange)
	{
		List<LineSegment> segments = [];

		Vector3 origin = Vector3.zero;
		int startAngle = Mathf.RoundToInt(coneAngle.x.ClampAngle());
		int endAngle = Mathf.RoundToInt(coneAngle.y.ClampAngle());
		float theta = ((endAngle - startAngle + 360f) % 360f).ClampAngle();
		Assert.IsFalse(Mathf.Approximately(theta, 0));

		// 4 corners
		Vector3 min1 = origin.PointFromAngle(minRange, startAngle);
		Vector3 min2 = origin.PointFromAngle(minRange, endAngle);
		Vector3 max1 = origin.PointFromAngle(maxRange, startAngle);
		Vector3 max2 = origin.PointFromAngle(maxRange, endAngle);

		// Radial boundary lines at the ends of the arc
		segments.Add(new LineSegment(min1, max1));
		segments.Add(new LineSegment(min2, max2));

		// Inner radial lines (no-fire zone), colored red
		if (minRange > 0f)
		{
			segments.Add(new LineSegment(origin, min1, Color.red));
			segments.Add(new LineSegment(origin, min2, Color.red));
		}

		// Build the arc in 1° increments
		Vector3 lastOuter = max1;
		Vector3 lastInner = min1;
		for (int d = startAngle; d <= theta; d++)
		{
			float ang = startAngle + d;
			Vector3 nextOuter = origin.PointFromAngle(maxRange, ang);
			segments.Add(new LineSegment(lastOuter, nextOuter));
			lastOuter = nextOuter;

			if (minRange > 0f)
			{
				Vector3 nextInner = origin.PointFromAngle(minRange, ang);
				segments.Add(new LineSegment(lastInner, nextInner, Color.red));
				lastInner = nextInner;
			}
		}
		return segments;
	}
}