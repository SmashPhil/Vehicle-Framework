using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public class Graphic_Propeller : Graphic_Rotator
	{
		public const string Key = "Propeller";

		public override string RegistryKey => Key;

		public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
		{
			base.DrawWorker(loc, rot, thingDef, thing, extraRotation);
		}
	}
}
