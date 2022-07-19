using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public class VehicleBuilding : Building
	{
		public VehiclePawn vehicle;

		public VehicleDef VehicleDef
		{
			get
			{
				VehicleBuildDef buildDef = def as VehicleBuildDef;
				return buildDef.thingToSpawn;
			}
		}

		public override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			if (vehicle != null)
			{
				vehicle.DrawAt(drawLoc, flip);
				if (vehicle.CompVehicleTurrets != null)
				{
					vehicle.CompVehicleTurrets.PostDraw();
				}
			}
			else
			{
				Log.ErrorOnce($"VehicleReference for building {LabelShort} is null. This should not happen unless spawning VehicleBuildings in DevMode.", GetHashCode());
				base.DrawAt(drawLoc, flip);
			}
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			vehicle.CompVehicleTurrets?.RevalidateTurrets();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref vehicle, "vehicle", true);
		}
	}
}
