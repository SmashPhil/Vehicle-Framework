using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleTurretRender
	{
		public RotationalOffset north = RotationalOffset.Default;
		public RotationalOffset east = RotationalOffset.Default;
		public RotationalOffset south = RotationalOffset.Default;
		public RotationalOffset west = RotationalOffset.Default;
		public RotationalOffset northEast = RotationalOffset.Default;
		public RotationalOffset southEast = RotationalOffset.Default;
		public RotationalOffset southWest = RotationalOffset.Default;
		public RotationalOffset northWest = RotationalOffset.Default;

		/// <summary>
		/// Init from CompProperties
		/// </summary>
		public VehicleTurretRender()
		{
		}

		public VehicleTurretRender(VehicleTurretRender reference)
		{
			north = reference.north;
			east = reference.east;
			south = reference.south;
			west = reference.west;
			northEast = reference.northEast;
			southEast = reference.southEast;
			southWest = reference.southWest;
			northWest = reference.northWest;
			PostInit();
		}

		public void PostInit()
		{
			north.Rot = Rot8.North;
			east.Rot = Rot8.East;
			south.Rot = Rot8.South;
			west.Rot = Rot8.West;
			northEast.Rot = Rot8.NorthEast;
			southEast.Rot = Rot8.SouthEast;
			southWest.Rot = Rot8.SouthWest;
			northWest.Rot = Rot8.NorthWest;
			if (!north.IsValid && south.IsValid)
			{
				north = south.Rotate(180);
			}
			if (!south.IsValid)
			{
				south = north.Rotate(180);
			}
			if (!east.IsValid)
			{
				if (west.IsValid)
				{
					east = west.Flip(true, false);
				}
				else
				{
					east = north.Rotate(270);
				}
			}
			if (!west.IsValid)
			{
				west = east.Flip(true, false);
			}
			if (!northEast.IsValid)
			{
				northEast = north.Rotate(-45);
			}
			if (!northWest.IsValid)
			{
				northWest = north.Rotate(45);
			}
			if (!southEast.IsValid)
			{
				southEast = south.Rotate(45);
			}
			if (!southWest.IsValid)
			{
				southWest = south.Rotate(-45);
			}
		}

		public RotationalOffset OffsetFor(Rot8 rot)
		{
			return rot.AsIntCompass switch
			{
				0 => north,
				1 => northEast,
				2 => east,
				3 => southEast,
				4 => south,
				5 => southWest,
				6 => west,
				7 => northWest,
				_ => RotationalOffset.Default,
			};
		}

		public override string ToString()
		{
			return $"north: {north} east: {east} south: {south} west: {west} NE: {northEast} SE: {southEast} SW: {southWest} NW: {northWest}";
		}

		public struct RotationalOffset
		{
			private Vector2 offset;

			public RotationalOffset(Vector2 offset)
			{
				this.offset = offset;
				Rot = Rot8.Invalid;
			}

			public Rot8 Rot { get; set; }

			public Vector2 Offset => offset;

			public bool IsValid => offset != Vector2.zero;

			public static RotationalOffset Default => new RotationalOffset(Vector2.zero);

			public RotationalOffset Rotate(float angle)
			{
				if (angle % 45 != 0)
				{
					SmashLog.Error($"Cannot rotate <type>RotationalOffset</type> with angle a non-multiple of 45.");
					return Default;
				}
				return new RotationalOffset()
				{
					offset = offset.RotatedBy(angle),
					Rot = angle < 0 ? Rot.Rotated(Mathf.Abs(angle), RotationDirection.Counterclockwise) : Rot.Rotated(angle, RotationDirection.Clockwise)
				};
			}

			public RotationalOffset Flip(bool flipX, bool flipY)
			{
				Vector2 newOffset = offset;
				if (flipX)
				{
					newOffset.x *= -1;
				}
				if (flipY)
				{
					newOffset.y *= -1;
				}
				return new RotationalOffset()
				{
					offset = newOffset
				};
			}

			public static RotationalOffset FromString(string entry)
			{
				entry = entry.TrimStart(new char[] { '(' }).TrimEnd(new char[] { ')' });
				string[] data = entry.Split(new char[] { ',' });
				if (data.Length != 2)
				{
					Log.Warning($"RotationalOffset parses into Vector2. xml = {entry}");
				}
				try
				{
					CultureInfo invariantCulture = CultureInfo.InvariantCulture;
					float offsetX = Convert.ToSingle(data[0], invariantCulture);
					float offsetY = Convert.ToSingle(data[1], invariantCulture);
					return new RotationalOffset(new Vector2(offsetX, offsetY));
				}
				catch (Exception ex)
				{
					SmashLog.Error($"{entry} is not a valid <struct>RotationalOffset</struct> format. Exception: {ex}");
					return Default;
				}
			}

			public override string ToString()
			{
				return offset.ToString();
			}
		}
	}
}
