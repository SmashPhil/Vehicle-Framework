using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class FlakWorker : AntiAircraftWorker
	{
		protected readonly List<Vector3> spawnLocations = new List<Vector3>();

		protected List<Thing> turretsCachedInMap = new List<Thing>();

		protected List<int> flakTurrets = new List<int>();
		protected int turretFiring = 0;

		public FlakWorker(AirDefense airDefense, AntiAircraftDef def) : base(airDefense, def)
		{
			flakTurrets = new List<int>();
			for (int i = 0; i < airDefense.defenseBuildings; i++)
			{
				flakTurrets.Add(CooldownRange);
			}
			turretsCachedInMap ??= new List<Thing>();
		}

		public virtual int CooldownRange => Mathf.CeilToInt(Rand.Range(airDefense.antiAircraftDef.ticksBetweenShots * 0.75f, airDefense.antiAircraftDef.ticksBetweenShots * 1.25f));

		public override bool ShouldDrawSearchLight => airDefense.parent is MapParent mapParent && (mapParent.Map is null || !turretsCachedInMap.NullOrEmpty());

		public virtual List<Vector3> SpawnLocations
		{
			get
			{
				if (spawnLocations.NullOrEmpty())
				{
					IEnumerable<Vector3> positions = Building_Artillery.RandomWorldPosition(airDefense.parent.Tile, airDefense.defenseBuildings);
					spawnLocations.AddRange(positions);
				}
				return spawnLocations;
			}
		}

		public override void Tick()
		{
			if (CurrentTarget != null)
			{
				if (airDefense.parent is MapParent mapParent && mapParent.HasMap)
				{
					if (!CurrentTarget.vehicle.CompVehicleLauncher.inFlight || Ext_Math.SphericalDistance(airDefense.parent.DrawPos, CurrentTarget.DrawPos) > airDefense.MaxDistance)
					{
						foreach (Building_Artillery artillery in turretsCachedInMap)
						{
							artillery.NotifyTargetOutOfRange(CurrentTarget);
						}
					}
					else
					{
						foreach (Building_Artillery artillery in turretsCachedInMap)
						{
							artillery.NotifyTargetInRange(airDefense.activeTargets.FirstOrDefault());
						}
					}
				}
				else
				{
					for (int i = 0; i < flakTurrets.Count; i++)
					{
						flakTurrets[i]--;
						if (flakTurrets[i] <= 0)
						{
							flakTurrets[i] = CooldownRange;
							Launch();
						}
					}
				}
			}
		}

		public override void TickRare()
		{
			if (airDefense.parent is MapParent mapParent && mapParent.HasMap)
			{
				turretsCachedInMap.Clear();
				turretsCachedInMap.AddRange(mapParent.Map.GetCachedMapComponent<ListerAirDefenses>().AllAirDefenses());
			}
		}

		public override void Launch()
		{
			AntiAircraft projectile = (AntiAircraft)Activator.CreateInstance(def.worldObjectClass);
			projectile.def = def;
			projectile.ID = Find.UniqueIDsManager.GetNextWorldObjectID();
			projectile.creationGameTicks = Find.TickManager.TicksGame;
			projectile.Tile = airDefense.parent.Tile;
			projectile.Initialize(airDefense.parent, CurrentTarget, SpawnLocations[turretFiring]);
			projectile.PostMake();
			Find.WorldObjects.Add(projectile);
			turretFiring++;
			if (turretFiring >= flakTurrets.Count)
			{
				turretFiring = 0;
			}
		}
	}
}
