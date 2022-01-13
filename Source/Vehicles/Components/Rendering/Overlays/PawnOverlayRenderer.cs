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
		private const float LayersTotalAllowed = 5;

		public Rot4 north = Rot8.North;
		public Rot4 east = Rot8.East;
		public Rot4 south = Rot8.South;
		public Rot4 west = Rot8.West;
		public Rot4 northEast = Rot4.North;
		public Rot4 southEast = Rot4.South;
		public Rot4 southWest = Rot4.South;
		public Rot4 northWest = Rot4.North;

		public int layer = 0;
		public int? layerNorth;
		public int? layerEast;
		public int? layerSouth;
		public int? layerWest;
		public int? layerNorthEast;
		public int? layerSouthEast;
		public int? layerSouthWest;
		public int? layerNorthWest;

		public Vector3 drawOffset = Vector3.zero;
		public Vector3? drawOffsetNorth;
		public Vector3? drawOffsetEast;
		public Vector3? drawOffsetSouth;
		public Vector3? drawOffsetWest;
		public Vector3? drawOffsetNorthEast;
		public Vector3? drawOffsetSouthEast;
		public Vector3? drawOffsetSouthWest;
		public Vector3? drawOffsetNorthWest;

		public float angle = 0;
		public float? angleNorth;
		public float? angleEast;
		public float? angleSouth;
		public float? angleWest;
		public float? angleNorthEast;
		public float? angleSouthEast;
		public float? angleSouthWest;
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
				1 => angleEast ?? -angleWest ?? (angle + 45 * rot.AsIntCompass),
				2 => angleSouth ?? -angleNorth ?? (angle + 45 * rot.AsIntCompass),
				3 => angleWest ?? -angleEast ?? (angle + 45 * rot.AsIntCompass),
				4 => angleNorthEast ?? -angleNorthWest ?? (angle + 45 * rot.AsIntCompass),
				5 => angleSouthEast ?? -angleSouthWest ?? (angle + 45 * rot.AsIntCompass),
				6 => angleSouthWest ?? -angleSouthEast ?? (angle + 45 * rot.AsIntCompass),
				7 => angleNorthWest ?? -angleNorthEast ?? (angle + 45 * rot.AsIntCompass),
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
				4 => (layerNorthEast ?? layerNorthWest ?? layer) / LayersTotalAllowed,
				5 => (layerSouthEast ?? layerSouthWest ?? layer) / LayersTotalAllowed,
				6 => (layerSouthWest ?? layerSouthEast ?? layer) / LayersTotalAllowed,
				7 => (layerNorthWest ?? layerNorthEast ?? layer) / LayersTotalAllowed,
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
				4 => drawOffsetNorthEast ?? drawOffsetNorthWest?.MirrorHorizontal() ?? drawOffset.RotatedBy(rot.AsAngle),
				5 => drawOffsetSouthEast ?? drawOffsetSouthWest?.MirrorHorizontal() ?? drawOffset.RotatedBy(rot.AsAngle),
				6 => drawOffsetSouthWest ?? drawOffsetSouthEast?.MirrorHorizontal() ?? drawOffset.RotatedBy(rot.AsAngle),
				7 => drawOffsetNorthWest ?? drawOffsetNorthEast?.MirrorHorizontal() ?? drawOffset.RotatedBy(rot.AsAngle),
				_ => throw new NotImplementedException(),
			};
			offset.y = LayerFor(rot);
			return offset;
		}
	}
}
