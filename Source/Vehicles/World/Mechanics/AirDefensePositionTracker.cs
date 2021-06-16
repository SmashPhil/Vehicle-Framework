using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class AirDefensePositionTracker : WorldComponent 
	{
		public const float RotationRate = 0.35f;

		private static Dictionary<AerialVehicleInFlight, HashSet<AirDefense>> searchingDefenses = new Dictionary<AerialVehicleInFlight, HashSet<AirDefense>>();
		private static HashSet<AirDefense> defensesToDraw = new HashSet<AirDefense>();

		public static Dictionary<WorldObject, AirDefense> airDefenseCache = new Dictionary<WorldObject, AirDefense>();

		private static List<WorldObject> saveableWorldObjects;
		private static List<AirDefense> saveableAirDefenses;
		
		public AirDefensePositionTracker(World world) : base(world)
		{
		}

		public static AirDefensePositionTracker Instance { get; private set; }

		public override void WorldComponentUpdate()
		{
			foreach (AirDefense airDefense in defensesToDraw)
			{
				airDefense.DrawSpotlightOverlay();
			}
		}

		public override void WorldComponentTick()
		{
			foreach (var defense in searchingDefenses)
			{
				AerialVehicleInFlight aerialVehicleSearchingFor = defense.Key;
				for (int j = defense.Value.Count - 1; j >= 0; j--)
				{
					AirDefense airDefense = defense.Value.ElementAt(j);
					float distance = Ext_Math.SphericalDistance(airDefense.parent.DrawPos, aerialVehicleSearchingFor.DrawPos);
					bool withinMaxDistance = distance <= airDefense.MaxDistance;
					if (airDefense.CurrentTarget != aerialVehicleSearchingFor)
					{
						airDefense.angle = (airDefense.angle + RotationRate * airDefense.searchDirection).ClampAndWrap(0, 360);
						float angleToTarget = airDefense.parent.DrawPos.AngleToPoint(aerialVehicleSearchingFor.DrawPos);
						if (withinMaxDistance && Mathf.Abs(angleToTarget - airDefense.angle) <= (airDefense.Arc / 2))
						{
							airDefense.activeTargets.Add(aerialVehicleSearchingFor);
						}
					}
					else
					{
						float headingToTarget = WorldHelper.TryFindHeading(airDefense.parent.DrawPos, airDefense.CurrentTarget.DrawPos);
						int dirSignMultiplier = headingToTarget < airDefense.angle ? -2 : 2;
						if (Mathf.Abs(headingToTarget - airDefense.angle) < 1 || Mathf.Abs(headingToTarget - airDefense.angle) > 359)
						{
							airDefense.angle = headingToTarget;
							airDefense.Attack();
						}
						else
						{
							airDefense.angle = (airDefense.angle + RotationRate * dirSignMultiplier).ClampAndWrap(0, 360);
						}
						if (!withinMaxDistance)
						{
							airDefense.activeTargets.Remove(aerialVehicleSearchingFor);
						}
					}
				}
			}
		}

		public override void FinalizeInit()
		{
			searchingDefenses ??= new Dictionary<AerialVehicleInFlight, HashSet<AirDefense>>();
			airDefenseCache ??= new Dictionary<WorldObject, AirDefense>();
		}

		public static void RegisterAerialVehicle(AerialVehicleInFlight aerialVehicle, HashSet<AirDefense> newDefenses)
		{
			HashSet<AirDefense> oldDefenses = new HashSet<AirDefense>();
			if (searchingDefenses.TryGetValue(aerialVehicle, out var defenses))
			{
				oldDefenses.AddRange(defenses);
				defenses.Clear();
				defenses.AddRange(newDefenses);
			}
			else
			{
				searchingDefenses.Add(aerialVehicle, newDefenses);
			}
			defensesToDraw.RemoveWhere(d => oldDefenses.Contains(d));
			foreach (AirDefense airDefense in newDefenses)
			{
				if (defensesToDraw.Add(airDefense) && !oldDefenses.Contains(airDefense))
				{
					airDefense.angle = ((Find.TickManager.TicksGame + airDefense.parent.GetHashCode()) * RotationRate) % 360;
				}
			}
		}

		public static void DeregisterAerialVehicle(AerialVehicleInFlight aerialVehicle)
		{
			searchingDefenses.Remove(aerialVehicle);
			RecacheAirDefenseDrawers();
		}

		public static void RecacheAirDefenseDrawers()
		{
			defensesToDraw.Clear();
			foreach (var registeredDefenses in searchingDefenses)
			{
				foreach (AirDefense airDefense in registeredDefenses.Value)
				{
					defensesToDraw.Add(airDefense);
				}
			}
		}

		public static IEnumerable<AirDefense> CheckNearbyObjects(AerialVehicleInFlight aerialVehicle, float speedPctPerTick)
		{
			float halfTicksPerTileTraveled = Ext_Math.RoundTo(speedPctPerTick * 100, 0.001f);
			Vector3 start = aerialVehicle.DrawPos;
			for (int i = 0; i < aerialVehicle.flightPath.Path.Count; i++)
			{
				int destination = aerialVehicle.flightPath[i].tile;
				Vector3 destinationPos = Find.WorldGrid.GetTileCenter(destination);
				Vector3 position = start;
				for (float transition = 0; transition < 1; transition += halfTicksPerTileTraveled)
				{
					Vector3 partition = Vector3.Slerp(position, destinationPos, transition);
					foreach (KeyValuePair<WorldObject, AirDefense> defenseCache in airDefenseCache)
					{
						float distance = Ext_Math.SphericalDistance(partition, defenseCache.Key.DrawPos);
						if (distance < defenseCache.Value.MaxDistance)
						{
							yield return defenseCache.Value;
						}
					}
				}
				start = destinationPos;
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref airDefenseCache, "airDefenseCache", LookMode.Reference, LookMode.Deep, ref saveableWorldObjects, ref saveableAirDefenses);
		}
	}
}
