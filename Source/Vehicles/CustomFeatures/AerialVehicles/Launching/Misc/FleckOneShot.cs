using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace Vehicles
{
	public class FleckOneShot
	{
		public FleckDef def;
		public FloatRange angle = new FloatRange(0, 360);
		
		public Vector3 originOffset = Vector3.zero;
		public Range<Vector3> originOffsetRange;

		public int emitAtTick = -1; //Emits 1 fleck at this tick

		public bool lockFleckX = true;
		public bool lockFleckZ = true;

		public FloatRange? airTime;
		public FloatRange? speed;
		public FloatRange? rotationRate;
		public FloatRange? size;
	}
}
