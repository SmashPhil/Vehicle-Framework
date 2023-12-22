using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class PawnOverlayRenderer
	{
		private const float LayersTotalAllowed = 10;

		private static Listing_SplitColumns listing = new Listing_SplitColumns();

		[TweakField(SettingsType = UISettingsType.Checkbox)]
		public bool showBody = true;

		[TweakField(SettingsType = UISettingsType.ToggleLabel)]
		public Rot4 north = Rot8.North;
		[TweakField(SettingsType = UISettingsType.ToggleLabel)]
		public Rot4 east = Rot8.East;
		[TweakField(SettingsType = UISettingsType.ToggleLabel)]
		public Rot4 south = Rot8.South;
		[TweakField(SettingsType = UISettingsType.ToggleLabel)]
		public Rot4 west = Rot8.West;
		[TweakField(SettingsType = UISettingsType.ToggleLabel)]
		public Rot4 northEast = Rot4.North;
		[TweakField(SettingsType = UISettingsType.ToggleLabel)]
		public Rot4 southEast = Rot4.South;
		[TweakField(SettingsType = UISettingsType.ToggleLabel)]
		public Rot4 southWest = Rot4.South;
		[TweakField(SettingsType = UISettingsType.ToggleLabel)]
		public Rot4 northWest = Rot4.North;

		[TweakField(SettingsType = UISettingsType.IntegerBox)]
		public int layer = 1;
		[TweakField(SettingsType = UISettingsType.IntegerBox)]
		public int? layerNorth;
		[TweakField(SettingsType = UISettingsType.IntegerBox)]
		public int? layerEast;
		[TweakField(SettingsType = UISettingsType.IntegerBox)]
		public int? layerSouth;
		[TweakField(SettingsType = UISettingsType.IntegerBox)]
		public int? layerWest;
		[TweakField(SettingsType = UISettingsType.IntegerBox)]
		public int? layerNorthEast;
		[TweakField(SettingsType = UISettingsType.IntegerBox)]
		public int? layerSouthEast;
		[TweakField(SettingsType = UISettingsType.IntegerBox)]
		public int? layerSouthWest;
		[TweakField(SettingsType = UISettingsType.IntegerBox)]
		public int? layerNorthWest;

		[TweakField(SettingsType = UISettingsType.FloatBox)]
		public Vector3 drawOffset = Vector3.zero;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		public Vector3? drawOffsetNorth;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		public Vector3? drawOffsetEast;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		public Vector3? drawOffsetSouth;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		public Vector3? drawOffsetWest;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		public Vector3? drawOffsetNorthEast;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		public Vector3? drawOffsetSouthEast;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		public Vector3? drawOffsetSouthWest;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		public Vector3? drawOffsetNorthWest;

		[TweakField(SettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 360, RoundDecimalPlaces = 0, Increment = 1)]
		public float angle = 0;
		[TweakField(SettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 360, RoundDecimalPlaces = 0, Increment = 1)]
		public float? angleNorth;
		[TweakField(SettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 360, RoundDecimalPlaces = 0, Increment = 1)]
		public float? angleEast;
		[TweakField(SettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 360, RoundDecimalPlaces = 0, Increment = 1)]
		public float? angleSouth;
		[TweakField(SettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 360, RoundDecimalPlaces = 0, Increment = 1)]
		public float? angleWest;
		[TweakField(SettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 360, RoundDecimalPlaces = 0, Increment = 1)]
		public float? angleNorthEast;
		[TweakField(SettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 360, RoundDecimalPlaces = 0, Increment = 1)]
		public float? angleSouthEast;
		[TweakField(SettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 360, RoundDecimalPlaces = 0, Increment = 1)]
		public float? angleSouthWest;
		[TweakField(SettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 360, RoundDecimalPlaces = 0, Increment = 1)]
		public float? angleNorthWest;

		public Rot4 RotFor(Rot8 rot)
		{
			return rot.AsInt switch
			{
				0 => north,
				1 => east,
				2 => south,
				3 => west,
				4 => northEast,
				5 => southEast,
				6 => southWest,
				7 => northWest,
				_ => throw new NotImplementedException(),
			};
		}

		public float AngleFor(Rot8 rot)
		{
			return rot.AsInt switch
			{
				0 => angleNorth ?? angleSouth + 180 ?? angle,
				1 => angleEast ?? -angleWest ?? angle,
				2 => angleSouth ?? -angleNorth ?? angle,
				3 => angleWest ?? -angleEast ?? angle,
				4 => angleNorthEast ?? -angleNorthWest ?? angle + 45,
				5 => angleSouthEast ?? -angleSouthWest ?? angle - 45,
				6 => angleSouthWest ?? -angleSouthEast ?? angle + 45,
				7 => angleNorthWest ?? -angleNorthEast ?? angle - 45,
				_ => throw new NotImplementedException(),
			};
		}

		public float LayerFor(Rot8 rot)
		{
			return rot.AsInt switch
			{
				0 => (layerNorth ?? layerSouth ?? layer) / LayersTotalAllowed,
				1 => (layerEast ?? layerWest ?? layer) / LayersTotalAllowed,
				2 => (layerSouth ?? layerNorth ?? layer) / LayersTotalAllowed,
				3 => (layerWest ?? layerEast ?? layer) / LayersTotalAllowed,
				4 => (layerNorthEast ?? layerNorthWest ?? layerNorth ?? layer) / LayersTotalAllowed,
				5 => (layerSouthEast ?? layerSouthWest ?? layerSouth ?? layer) / LayersTotalAllowed,
				6 => (layerSouthWest ?? layerSouthEast ?? layerSouth ?? layer) / LayersTotalAllowed,
				7 => (layerNorthWest ?? layerNorthEast ?? layerNorth ?? layer) / LayersTotalAllowed,
				_ => throw new NotImplementedException(),
			};
		}

		public Vector3 DrawOffsetFor(Rot8 rot)
		{
			Vector3 offset = rot.AsInt switch
			{
				0 => drawOffsetNorth ?? drawOffsetSouth?.MirrorVertical() ?? drawOffset,
				1 => drawOffsetEast ?? drawOffsetWest?.MirrorHorizontal() ?? drawOffset.RotatedBy(rot.AsAngle),
				2 => drawOffsetSouth ?? drawOffsetNorth?.MirrorVertical() ?? drawOffset.RotatedBy(rot.AsAngle),
				3 => drawOffsetWest ?? drawOffsetEast?.MirrorHorizontal() ?? drawOffset.RotatedBy(rot.AsAngle),
				4 => drawOffsetNorthEast ?? drawOffsetNorthWest?.MirrorHorizontal() ?? drawOffsetNorth?.RotatedBy(45) ?? drawOffset.RotatedBy(rot.AsAngle),
				5 => drawOffsetSouthEast ?? drawOffsetSouthWest?.MirrorHorizontal() ?? drawOffsetSouth?.RotatedBy(-45) ?? drawOffset.RotatedBy(rot.AsAngle),
				6 => drawOffsetSouthWest ?? drawOffsetSouthEast?.MirrorHorizontal() ?? drawOffsetSouth?.RotatedBy(45) ?? drawOffset.RotatedBy(rot.AsAngle),
				7 => drawOffsetNorthWest ?? drawOffsetNorthEast?.MirrorHorizontal() ?? drawOffsetNorth?.RotatedBy(-45) ?? drawOffset.RotatedBy(rot.AsAngle),
				_ => throw new NotImplementedException(),
			};
			offset.y = LayerFor(rot);
			return offset;
		}

		public void RenderEditor(Rect rect)
		{
			listing.Begin(rect, 2);

			//Rotations

			//Layers
			if (layerNorth != null)
			{
				int value = layerNorth.Value;
				listing.SliderLabeled("Layer North", ref value, string.Empty, string.Empty, string.Empty, -5, 5);
				layerNorth = value;
			}
			if (layerEast != null)
			{
				int value = layerEast.Value;
				listing.SliderLabeled("Layer East", ref value, string.Empty, string.Empty, string.Empty, -5, 5);
				layerEast = value;
			}
			if (layerSouth != null)
			{
				int value = layerSouth.Value;
				listing.SliderLabeled("Layer South", ref value, string.Empty, string.Empty, string.Empty, -5, 5);
				layerSouth = value;
			}
			if (layerWest != null)
			{
				int value = layerWest.Value;
				listing.SliderLabeled("Layer West", ref value, string.Empty, string.Empty, string.Empty, -5, 5);
				layerWest = value;
			}
			listing.NextRow();
			//Offsets
			if (drawOffsetNorth != null)
			{
				drawOffsetNorth = listing.Vector3Box("Offset North", drawOffsetNorth.Value, string.Empty);
				listing.NextRow();
			}
			if (drawOffsetEast != null)
			{
				drawOffsetEast = listing.Vector3Box("Offset East", drawOffsetEast.Value, string.Empty);
				listing.NextRow();
			}
			if (drawOffsetSouth != null)
			{
				drawOffsetSouth = listing.Vector3Box("Offset South", drawOffsetSouth.Value, string.Empty);
				listing.NextRow();
			}
			if (drawOffsetWest != null)
			{
				drawOffsetWest = listing.Vector3Box("Offset West", drawOffsetWest.Value, string.Empty);
				listing.NextRow();
			}
			//Angles

			listing.End();
		}
	}
}
