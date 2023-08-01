using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace Vehicles
{
	public partial class VehiclePawn
	{
		public void CheckForCollisions()
		{
			return;
			if (VehicleMod.settings.main.runOverPawns)
			{
				CellRect occupiedRect = this.OccupiedRect();
				foreach (IntVec3 cell in occupiedRect)
				{
					if (Map.thingGrid.ThingAt(cell, ThingCategory.Pawn) is Pawn pawn && !(pawn is VehiclePawn))
					{
						if (pawn.Faction.HostileTo(Faction) || Rand.Chance(VehicleMod.settings.main.chanceRunOverFriendly))
						{
							float speed = GetStatValue(VehicleStatDefOf.MoveSpeed);
							float kineticEnergy = 0.5f * GetStatValue(VehicleStatDefOf.Mass) * Mathf.Pow(speed, 2);
							float impactForce = (kineticEnergy / 100) * (1 / pawn.RaceProps.baseBodySize);
							Pawn culprit = GetPriorityHandlers(HandlingTypeFlags.Movement)?.FirstOrDefault(handler => handler.handlers.Any)?.handlers.InnerListForReading.FirstOrDefault();
							IntVec3 position = pawn.Position;
							DamageWorker.DamageResult result = pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, impactForce, instigator: culprit));
							float vehicleImpactForce = (kineticEnergy / 100) * pawn.RaceProps.baseBodySize;
							TryTakeDamage(new DamageInfo(DamageDefOf.Blunt, impactForce, instigator: pawn, instigatorGuilty: false), position, out _);
						}
					}
				}
			}
		}
	}
}
