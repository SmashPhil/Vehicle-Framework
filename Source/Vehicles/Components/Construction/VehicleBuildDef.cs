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
		public PawnKindDef thingToSpawn;
		public SoundDef soundBuilt;

		// <offset, angle, velocity> REDO
		//public Tuple<ThingDef, Vector2, float, float> smokeOnDamaged;
	}
}
