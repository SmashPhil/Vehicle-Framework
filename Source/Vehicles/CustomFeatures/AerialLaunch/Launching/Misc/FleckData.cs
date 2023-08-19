using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using SmashTools;
using RimWorld;

namespace Vehicles
{
	public class FleckData
	{
		public FleckDef def;
		public FloatRange angle = new FloatRange(0, 360);
		[Obsolete]
		public PositionStart position = PositionStart.Position;

		public Vector3 originOffset = Vector3.zero;

		public bool runOutOfStep = true;

		public bool lockFleckX = true;
		public bool lockFleckZ = true;

		[GraphEditable]
		public LinearCurve drawOffset;
		[GraphEditable]
		public LinearCurve airTime;
		[GraphEditable]
		public LinearCurve frequency;
		[GraphEditable]
		public LinearCurve speed;
		[GraphEditable]
		public LinearCurve rotationRate;
		[GraphEditable]
		public LinearCurve size;

		public FleckData()
		{
		}

		public FleckData(FleckDef def, FloatRange angle, LinearCurve speed = null, LinearCurve size = null)
		{
			this.def = def ?? FleckDefOf.DustPuff;
			this.angle = angle;
			this.speed = speed;
			this.size = size;
		}

		public enum PositionStart
		{
			DrawPos,
			Position,
		}
	}
}
