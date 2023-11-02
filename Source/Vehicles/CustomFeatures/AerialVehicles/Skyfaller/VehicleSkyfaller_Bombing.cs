using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class VehicleSkyfaller_Bombing : VehicleSkyfaller_FlyOver
	{
		public override void Tick()
		{
			base.Tick();
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
		}

		public override void ExposeData()
		{
			base.ExposeData();
		}
	}
}
