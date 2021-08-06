using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleTurretRender : IExposable
	{
		public RotationalOffset north = RotationalOffset.Default;
		public RotationalOffset east = RotationalOffset.Default;
		public RotationalOffset south = RotationalOffset.Default;
		public RotationalOffset west = RotationalOffset.Default;
		public RotationalOffset northEast = RotationalOffset.Default;
		public RotationalOffset southEast = RotationalOffset.Default;
		public RotationalOffset southWest = RotationalOffset.Default;
		public RotationalOffset northWest = RotationalOffset.Default;

		public VehicleTurretRender()
		{
			PostInit();
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
					east = west.Rotate(180);
				}
				else
				{
					east = north.Rotate(270);
				}
			}
			if (!west.IsValid)
			{
				west = east.Rotate(180);
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

		public void ExposeData()
		{
			Scribe_Values.Look(ref north, "north");
			Scribe_Values.Look(ref east, "east");
			Scribe_Values.Look(ref south, "south");
			Scribe_Values.Look(ref west, "west");
			Scribe_Values.Look(ref northEast, "northEast");
			Scribe_Values.Look(ref southEast, "southEast");
			Scribe_Values.Look(ref southWest, "southWest");
			Scribe_Values.Look(ref northWest, "northWest");
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
					offset = Offset.RotatedBy(angle),
					Rot = angle < 0 ? Rot.Rotated(Mathf.Abs(angle), RotationDirection.Counterclockwise) : Rot.Rotated(angle, RotationDirection.Clockwise)
				};
			}

			public static RotationalOffset FromString(string entry)
			{
				entry = entry.TrimStart(new char[] { '(' }).TrimEnd(new char[] { ')' });
				string[] data = entry.Split(new char[] { ',' });
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
