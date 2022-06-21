using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public class VehicleBuildDef : ThingDef
	{
		public VehicleDef thingToSpawn;
		public SoundDef soundBuilt;

		public SimpleCurve shakeAmountPerAreaCurve = new SimpleCurve
		{
			{
				new CurvePoint(1f, 0.07f),
				true
			},
			{
				new CurvePoint(2f, 0.07f),
				true
			},
			{
				new CurvePoint(4f, 0.1f),
				true
			},
			{
				new CurvePoint(9f, 0.2f),
				true
			},
			{
				new CurvePoint(16f, 0.5f),
				true
			}
		};

		// <offset, angle, velocity> REDO
		//public Tuple<ThingDef, Vector2, float, float> smokeOnDamaged;
	}
}
