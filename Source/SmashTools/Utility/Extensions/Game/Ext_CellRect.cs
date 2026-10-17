using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SmashTools;
using Verse;

namespace SmashTools
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public static class Ext_CellRect
	{
		/// <summary>
		/// Returns center of cardinal edges spanning all 4 rotations of <paramref name="cellRect"/>
		/// </summary>
		/// <remarks>eg. 3x3 CellRect would return (0,1) (0,-1) (1,0) (-1,0)</remarks>
		public static IEnumerable<IntVec3> Cardinals(this CellRect cellRect)
		{
			if (cellRect.IsEmpty) yield break;

			if (cellRect.Area == 1)
			{
				// Return center position if any dimension is size=1
				yield return new IntVec3(cellRect.minX, 0, cellRect.minZ);
				yield break;
			}

			if (cellRect is { Width: 2, Height: 2 })
			{
				// Return corners if CellRect is 2x2 area.
				// North East
				yield return new IntVec3(cellRect.maxX, 0, cellRect.maxZ);
				// South East
				yield return new IntVec3(cellRect.maxX, 0, cellRect.minZ);
				// South West
				yield return new IntVec3(cellRect.minX, 0, cellRect.minZ);
				// North West
				yield return new IntVec3(cellRect.minX, 0, cellRect.maxZ);
				yield break;
			}

			// Vertical Stretch
			if (cellRect.Height > 1)
			{
				int x = cellRect.minX + cellRect.Width / 2;
				// North
				yield return new IntVec3(x, 0, cellRect.maxZ);
				// South
				yield return new IntVec3(x, 0, cellRect.minZ);
			}

			// Horizontal Stretch
			if (cellRect.Width > 1)
			{
				int z = cellRect.minZ + cellRect.Height / 2;
				// East
				yield return new IntVec3(cellRect.maxX, 0, z);
				// West
				yield return new IntVec3(cellRect.minX, 0, z);
			}
		}

		public static CellRect EdgeCellsSpan(this Map map, Rot4 rot, int size = 1)
		{
			return EdgeCellsSpan(new CellRect(0, 0, map.Size.x, map.Size.z), rot, size: size);
		}

		public static CellRect EdgeCellsSpan(this CellRect cellRect, Rot4 rot,
			int size = 1)
		{
			return rot.AsInt switch
			{
				0 => new CellRect(cellRect.minX, cellRect.maxZ - size + 1, cellRect.Width, size),
				1 => new CellRect(cellRect.maxX - size + 1, cellRect.minZ, size, cellRect.Height),
				2 => new CellRect(cellRect.minX, cellRect.minZ, cellRect.Width, size),
				3 => new CellRect(cellRect.minX, cellRect.minZ, size, cellRect.Height),
				_ => throw new NotImplementedException(),
			};
		}

		public static IEnumerable<IntVec3> CellsNoOverlap(this CellRect cellRect, CellRect excludeRect)
		{
			HashSet<IntVec3> noOverlapCells = excludeRect.Cells.ToHashSet();
			foreach (IntVec3 cell in cellRect)
			{
				if (!noOverlapCells.Contains(cell))
				{
					yield return cell;
				}
			}
		}

		/// <summary>
		/// Enumerate over all cells in <paramref name="cellRect"/> and <paramref name="otherRect"/> without 
		/// repeating cells from overlapping sections.
		/// </summary>
		[Obsolete("Use CellRectOverlap instead.")]
		public static IEnumerable<IntVec3> AllCellsNoRepeat(this CellRect cellRect, CellRect otherRect)
		{
			// Same rects, returning the cells of 1 is fine
			if (cellRect == otherRect)
			{
        foreach (IntVec3 cell in cellRect)
        {
          yield return cell;
        }
				yield break;
			}

			// No overlap, will be faster to return both separately
			if (!cellRect.Overlaps(otherRect))
			{
        foreach (IntVec3 cell in cellRect)
        {
          yield return cell;
        }
        foreach (IntVec3 cell in otherRect)
        {
          yield return cell;
        }
				yield break;
			}

			// CellRect will have 8 boundary lines, we need to separate out which rects extend beyond
			// the overlapping section.
			// NOTE - CellRect subtracts 1 from max limits since max is the indicator of max cell value,
			// not max boundary line which would include the last cell unit. We need to adjust here and
			// decrement 1 when starting at max bounds for cell indices.

			int maxTopZ, maxBotZ; // Top Bounds
			int minTopZ, minBotZ; // Bottom Bounds
			int minLeftX, minRightX; // Left Bounds
			int maxLeftX, maxRightX; // Right Bounds

			// Needs expanded iteration to catch floating corners when rect is not in a cross pattern
			// and only partially overlaps.
			RectEdge edge = RectEdge.None;

			// Top
			if (cellRect.maxZ > otherRect.maxZ)
			{
				maxTopZ = cellRect.maxZ + 1;
				maxBotZ = otherRect.maxZ + 1;
				if (cellRect.Width > otherRect.Width) 
          edge |= RectEdge.Top;
			}
			else
			{
				maxTopZ = otherRect.maxZ + 1;
				maxBotZ = cellRect.maxZ + 1;
				if (otherRect.Width > cellRect.Width) 
          edge |= RectEdge.Top;
			}

			// Bottom
			if (cellRect.minZ > otherRect.minZ)
			{
				minTopZ = cellRect.minZ;
				minBotZ = otherRect.minZ;
				if (cellRect.Width < otherRect.Width) 
          edge |= RectEdge.Bottom;
			}
			else
			{
				minTopZ = otherRect.minZ;
				minBotZ = cellRect.minZ;
				if (otherRect.Width < cellRect.Width) 
          edge |= RectEdge.Bottom;
			}

			// Left
			if (cellRect.minX < otherRect.minX)
			{
				minLeftX = cellRect.minX;
				minRightX = otherRect.minX;
				if (cellRect.Height > otherRect.Height) 
          edge |= RectEdge.Left;
			}
			else
			{
				minLeftX = otherRect.minX;
				minRightX = cellRect.minX;
				if (otherRect.Height > cellRect.Height) 
          edge |= RectEdge.Left;
			}

			// Right
			if (cellRect.maxX < otherRect.maxX)
			{
				maxLeftX = cellRect.maxX + 1;
				maxRightX = otherRect.maxX + 1;
				if (cellRect.Height < otherRect.Height) 
          edge |= RectEdge.Right;
			}
			else
			{
				maxLeftX = otherRect.maxX + 1;
				maxRightX = cellRect.maxX + 1;
				if (otherRect.Height < cellRect.Height) 
          edge |= RectEdge.Right;
			}

			int z, zLimit;
			// Left no-overlap
			int x = minLeftX;
			int xLimit = minRightX;
			for (; x < xLimit; x++)
			{
				z = edge == RectEdge.Left ? maxTopZ - 1 : maxBotZ - 1;
				zLimit = edge == RectEdge.Left ? minBotZ : minTopZ;
				for (; z >= zLimit; z--)
				{
					yield return new IntVec3(x, 0, z);
				}
			}

			// Right no-overlap
			x = maxLeftX;
			xLimit = maxRightX;
			for (; x < xLimit; x++)
			{
				z = edge == RectEdge.Right ? maxTopZ - 1 : maxBotZ - 1;
				zLimit = edge == RectEdge.Right ? minBotZ : minTopZ;
				for (; z >= zLimit; z--)
				{
					yield return new IntVec3(x, 0, z);
				}
			}

			// Top no-overlap
			x = edge == RectEdge.Top ? minLeftX : minRightX;
			xLimit = (edge & RectEdge.Top) == RectEdge.Top ? maxRightX : maxLeftX;
			switch (edge)
			{
				// Corners mirror limits
				case RectEdge.TopLeft or RectEdge.BottomRight:
					x = minLeftX;
					xLimit = maxLeftX;
				break;
				case RectEdge.TopRight or RectEdge.BottomLeft:
					x = minRightX;
					xLimit = maxRightX;
				break;
			}

			for (; x < xLimit; x++)
			{
				z = maxTopZ - 1;
				zLimit = maxBotZ;
				for (; z >= zLimit; z--)
				{
					yield return new IntVec3(x, 0, z);
				}
			}

			// Bottom no-overlap
			x = edge == RectEdge.Bottom ? minLeftX : minRightX;
			xLimit = (edge & RectEdge.Bottom) == RectEdge.Bottom ? maxRightX : maxLeftX;
			switch (edge)
			{
				// Corners mirror limits
				case RectEdge.TopLeft or RectEdge.BottomRight:
					x = minRightX;
					xLimit = maxRightX;
				break;
				case RectEdge.TopRight or RectEdge.BottomLeft:
					x = minLeftX;
					xLimit = maxLeftX;
				break;
			}

			for (; x < xLimit; x++)
			{
				z = minTopZ - 1;
				zLimit = minBotZ;
				for (; z >= zLimit; z--)
				{
					yield return new IntVec3(x, 0, z);
				}
			}

			// Overlapping section
			for (x = minRightX; x < maxLeftX; x++)
			{
				for (z = maxBotZ - 1; z >= minTopZ; z--)
				{
					yield return new IntVec3(x, 0, z);
				}
			}
		}

		[Flags]
		private enum RectEdge
		{
			None,
			Left = 1 << 0,
			Right = 1 << 1,
			Top = 1 << 2,
			Bottom = 1 << 3,

			// NOTE - Edges are parts of the rect that are hanging off. If 2 edges are
			// hanging off then the corner will be opposite to the cardinal edge that
			// is hanging off eg. Top edge + right hanging = top left corner is overlapping.
			BottomLeft = Bottom | Right,
			TopLeft = Top | Right,
			TopRight = Top | Left,
			BottomRight = Bottom | Left,
		}
	}
}