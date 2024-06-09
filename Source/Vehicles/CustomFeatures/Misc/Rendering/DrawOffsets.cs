using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class DrawOffsets
	{
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		public Vector3 defaultOffset;

		[TweakField(SettingsType = UISettingsType.FloatBox)]
		public Vector3? north;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		public Vector3? east;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		public Vector3? south;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		public Vector3? west;

		//public Vector3? northEast;
		//public Vector3? southEast;
		//public Vector3? southWest;
		//public Vector3? northWest;

		public Vector3 OffsetFor(Rot4 rot)
		{
			switch (rot.AsInt)
			{
				case 0:
					return north ?? defaultOffset;
				case 1:
					if (east == null && west != null)
					{
						return new Vector3(-west.Value.x, west.Value.y, west.Value.z);
					}
					return east ?? defaultOffset.RotatedBy(rot.AsAngle);
				case 2:
					if (south == null && north != null)
					{
						return new Vector3(north.Value.x, north.Value.y, -north.Value.z);
					}
					return south ?? defaultOffset.RotatedBy(rot.AsAngle);
				case 3:
					if (west == null && east != null)
					{
						return new Vector3(-east.Value.x, east.Value.y, east.Value.z);
					}
					return west ?? defaultOffset.RotatedBy(rot.AsAngle);
			}
			return defaultOffset.RotatedBy(rot.AsAngle);
		}
	}
}
