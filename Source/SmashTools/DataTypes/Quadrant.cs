using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace SmashTools
{
	/// <summary>
	/// Quadrant of grid or map
	/// </summary>
	public struct Quadrant
	{
		private byte quadInt;

		public Quadrant(byte q)
		{
			quadInt = q;
		}

		public Quadrant(int q)
		{
			quadInt = (byte)q.Clamp(1, 4);
		}

		public static Quadrant Q1 => new Quadrant(1);

		public static Quadrant Q2 => new Quadrant(2);

		public static Quadrant Q3 => new Quadrant(3);

		public static Quadrant Q4 => new Quadrant(4);

		public static Quadrant Invalid => new Quadrant { quadInt = 100 };

		public int AsInt
		{
			get
			{
				return quadInt;
			}
			set
			{
				quadInt = (byte)value.Clamp(1, 4);
			}
		}

		public static Quadrant QuadrantOfIntVec3(IntVec3 c, Map map)
		{
			if (c.x > map.Size.x / 2 && c.z >= map.Size.z / 2)
			{
				return Q1;
			}
			else if (c.x >= map.Size.x / 2 && c.z < map.Size.z / 2)
			{
				return Q2;
			}
			else if (c.x < map.Size.x / 2 && c.z <= map.Size.z / 2)
			{
				return Q3;
			}
			else if (c.x <= map.Size.x / 2 && c.z > map.Size.z / 2)
			{
				return Q4;
			}

			if (c.x == map.Size.x / 2 && c.z == map.Size.z / 2)
			{
				return Q1;
			}
			return Invalid;
		}

		public static Quadrant QuadrantRelativeToPoint(IntVec3 c, IntVec3 point, Map map)
		{
			if (c.x > point.x && c.z >= point.z)
			{
				return Q1;
			}
			else if (c.x >= point.x && c.z < point.z)
			{
				return Q2;
			}
			else if (c.x < point.x && c.z <= point.z)
			{
				return Q3;
			}
			else if (c.x <= point.x && c.z > point.z)
			{
				return Q4;
			}
			if (c.x == point.x && c.z == point.z)
			{
				return Q1;
			}
			return Invalid;
		}

		public static IEnumerable<IntVec3> CellsInQuadrant(Quadrant q, Map map)
		{
			switch (q.AsInt)
			{
				case 1:
					return CellRect.WholeMap(map).Cells.Where(c2 => c2.x > map.Size.x / 2 && c2.z >= map.Size.z / 2);
				case 2:
					return CellRect.WholeMap(map).Cells.Where(c2 => c2.x <= map.Size.x / 2 && c2.z < map.Size.z / 2);
				case 3:
					return CellRect.WholeMap(map).Cells.Where(c2 => c2.x < map.Size.x / 2 && c2.z <= map.Size.z / 2);
				case 4:
					return CellRect.WholeMap(map).Cells.Where(c2 => c2.x <= map.Size.x / 2 && c2.z > map.Size.z / 2);
				default:
					throw new NotImplementedException("Quadrant Int is not valid.");
			}
		}

		public override string ToString()
		{
			return quadInt.ToString();
		}

		public static Quadrant FromString(string innerText)
		{
			if (byte.TryParse(innerText, out byte num))
			{
				return new Quadrant(num);
			}
			Log.Error($"Unable to parse Quadrant: {innerText}");
			return Invalid;
		}
	}
}
