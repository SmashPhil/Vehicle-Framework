using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld.Planet;

namespace Vehicles
{
	public class CompProjectileExitMap : ThingComp
	{
		public AntiAircraftDef airDefenseDef;
		public AerialVehicleInFlight target;
		public Vector3 spawnPos;

		public CompProjectileExitMap(ThingWithComps parent)
		{
			this.parent = parent;
		}

		public void LeaveMap()
		{
			AntiAircraft projectile = (AntiAircraft)Activator.CreateInstance(airDefenseDef.worldObjectClass);
			projectile.def = airDefenseDef;
			projectile.ID = Find.UniqueIDsManager.GetNextWorldObjectID();
			projectile.creationGameTicks = Find.TickManager.TicksGame;
			projectile.Tile = parent.Map.Tile;
			projectile.Initialize(parent.Map.Parent, target, spawnPos);
			projectile.PostMake();
			Find.WorldObjects.Add(projectile);
		}
	}
}
