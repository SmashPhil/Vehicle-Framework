using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public class Graphic_ReversePropeller : Graphic_Rotator
	{
		public const string Key = "ReversePropeller";

		public override string RegistryKey => Key;

		public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
		{
			base.DrawWorker(loc, rot, thingDef, thing, extraRotation);
		}

		public override float ModifyIncomingRotation(float rotation)
		{
			return -rotation;
		}
	}
}
