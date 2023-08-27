using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleTurretRender : ITweakFields
	{
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		private Vector2? north;// = RotationalOffset.Default;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		private Vector2? east;// = RotationalOffset.Default;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		private Vector2? south;// = RotationalOffset.Default;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		private Vector2? west;// = RotationalOffset.Default;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		private Vector2? northEast;// = RotationalOffset.Default;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		private Vector2? southEast;// = RotationalOffset.Default;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		private Vector2? southWest;// = RotationalOffset.Default;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		private Vector2? northWest;// = RotationalOffset.Default;

		string ITweakFields.Label => "Render Properties";

		string ITweakFields.Category => string.Empty;

		public Vector2 North => north ?? Vector2.zero;
		public Vector2 East => east ?? Vector2.zero;
		public Vector2 South => south ?? Vector2.zero;
		public Vector2 West => west ?? Vector2.zero;
		public Vector2 NorthEast => northEast ?? Vector2.zero;
		public Vector2 SouthEast => southEast ?? Vector2.zero;
		public Vector2 SouthWest => southWest ?? Vector2.zero;
		public Vector2 NorthWest => northWest ?? Vector2.zero;

		/// <summary>
		/// Init from CompProperties
		/// </summary>
		public VehicleTurretRender()
		{
		}

		public VehicleTurretRender(VehicleTurretRender reference)
		{
			if (reference != null)
			{
				north = reference.north;
				east = reference.east;
				south = reference.south;
				west = reference.west;
				northEast = reference.northEast;
				southEast = reference.southEast;
				southWest = reference.southWest;
				northWest = reference.northWest;
			}
			PostLoad();
		}

		public void OnFieldChanged()
		{
			RecacheOffsets();
		}

		/// <summary>
		/// Reflection call from vanilla
		/// </summary>
		public void PostLoad()
		{
			RecacheOffsets();
		}

		private void RecacheOffsets()
		{
			if (!north.HasValue)
			{
				if (south.HasValue)
				{
					north = Rotate(south.Value, 180);
				}
				else
				{
					north = Vector2.zero;
				}
			}
			if (!south.HasValue)
			{
				south = Rotate(north.Value, 180);
			}
			if (!east.HasValue)
			{
				if (west.HasValue)
				{
					east = Flip(west.Value, true, false);
				}
				else
				{
					east = Rotate(north.Value, 90);
				}
			}
			if (!west.HasValue)
			{
				if (east.HasValue)
				{
					west = Flip(east.Value, true, false);
				}
				else
				{
					west = Rotate(north.Value, 270);
				}
			}
			if (!northEast.HasValue)
			{
				northEast = Rotate(north.Value, -45);
			}
			if (!northWest.HasValue)
			{
				northWest = Rotate(north.Value, 45);
			}
			if (!southEast.HasValue)
			{
				southEast = Rotate(south.Value, 45);
			}
			if (!southWest.HasValue)
			{
				southWest = Rotate(south.Value, -45);
			}

			Log.Message($"--OFFSETS--");
			Log.Message($"North: {North}");
			Log.Message($"East: {East}");
			Log.Message($"South: {South}");
			Log.Message($"West: {West}");
			Log.Message($"NorthEast: {NorthEast}");
			Log.Message($"SouthEast: {SouthEast}");
			Log.Message($"SouthWest: {SouthWest}");
			Log.Message($"NorthWest: {NorthWest}");
		}

		public Vector2 Rotate(Vector2 offset, float angle)
		{
			if (angle % 45 != 0)
			{
				SmashLog.Error($"Cannot rotate <type>VehicleTurretRender.offset</type> with an angle non-multiple of 45.");
				return offset;
			}
			return offset.RotatedBy(angle);
		}

		public Vector2 Flip(Vector2 offset, bool flipX, bool flipY)
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
			return newOffset;
		}

		public Vector2 OffsetFor(Rot8 rot)
		{
			return rot.AsInt switch
			{
				0 => North,
				1 => East,
				2 => South,
				3 => West,
				4 => NorthEast,
				5 => SouthEast,
				6 => SouthWest,
				7 => NorthWest,
				_ => Vector2.zero,
			};
		}

		public override string ToString()
		{
			return $"north: {north} east: {east} south: {south} west: {west} NE: {northEast} SE: {southEast} SW: {southWest} NW: {northWest}";
		}

		public class RotationalOffset : ITweakFields
		{
			[TweakField(SettingsType = UISettingsType.FloatBox)]
			private Vector2? offset;

			public RotationalOffset(Vector2? offset)
			{
				this.offset = offset;
				Rot = Rot8.Invalid;
			}

			public Rot8 Rot { get; set; }

			public Vector2 Offset => offset ?? Vector2.zero;

			public bool IsValid => offset != null;

			public static RotationalOffset Default => new RotationalOffset(null);

			string ITweakFields.Label => Rot.ToStringNamed();

			string ITweakFields.Category => string.Empty;

			public void OnFieldChanged()
			{
			}

			public RotationalOffset Rotate(float angle)
			{
				if (angle % 45 != 0)
				{
					SmashLog.Error($"Cannot rotate <type>RotationalOffset</type> with angle a non-multiple of 45.");
					return Default;
				}
				offset = Offset.RotatedBy(angle);
				return new RotationalOffset(offset)
				{
					Rot = angle < 0 ? Rot.Rotated(Mathf.Abs(angle), RotationDirection.Counterclockwise) : Rot.Rotated(angle, RotationDirection.Clockwise)
				};
			}

			public RotationalOffset Flip(bool flipX, bool flipY)
			{
				Vector2 newOffset = Offset;
				if (flipX)
				{
					newOffset.x *= -1;
				}
				if (flipY)
				{
					newOffset.y *= -1;
				}
				return new RotationalOffset(newOffset);
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
