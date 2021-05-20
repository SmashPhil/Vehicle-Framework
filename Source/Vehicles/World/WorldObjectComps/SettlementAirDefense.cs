using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class SettlementAirDefense : IExposable
	{
		public Settlement settlement;

		public int radarDistance = 5;
		public int ticksPerShot = 10;
		public AntiAircraftDef antiAircraft;

		private int cooldownTimer;

		private RotatingList<AerialVehicleInFlight> targets = new RotatingList<AerialVehicleInFlight>();
		private HashSet<AerialVehicleInFlight> activeTargets = new HashSet<AerialVehicleInFlight>();

		private List<GlobalTargetInfo> targetsForSave = new List<GlobalTargetInfo>();

		public SettlementAirDefense()
		{
		}

		public SettlementAirDefense(Settlement settlement)
		{
			this.settlement = settlement;
			antiAircraft = AntiAircraftDefOf.FlakProjectile;
		}

		public void Attack()
		{
			cooldownTimer--;
			if (cooldownTimer < 0)
			{
				cooldownTimer += ticksPerShot;
				var target = targets.Next;
				AntiAircraft projectile = (AntiAircraft)Activator.CreateInstance(antiAircraft.worldObjectClass);
				projectile.def = antiAircraft;
				projectile.ID = Find.UniqueIDsManager.GetNextWorldObjectID();
				projectile.creationGameTicks = Find.TickManager.TicksGame;
				projectile.Tile = settlement.Tile;
				projectile.Initialize(settlement, target, settlement.DrawPos, 80); //REDO - hardcoded speed
				projectile.PostMake();
				Find.WorldObjects.Add(projectile);

				if (!target.vehicle.CompVehicleLauncher.inFlight || Ext_Math.SphericalDistance(settlement.DrawPos, target.DrawPos) > SettlementPositionTracker.airDefenseCache[settlement].radarDistance)
				{
					RemoveTarget(target);
					targets.PostItemRemove();
				}
			}
		}

		public void PushTarget(AerialVehicleInFlight aerialVehicle)
		{
			if (aerialVehicle.Elevation <= antiAircraft.maxAltitude && activeTargets.Add(aerialVehicle))
			{
				targets.Add(aerialVehicle);
				SettlementPositionTracker.ActivateSettlementDefenses(this);
				Messages.Message("AerialVehicleTargetedAA".Translate(aerialVehicle.Label), MessageTypeDefOf.NegativeEvent);
			}
		}

		public void RemoveTarget(AerialVehicleInFlight aerialVehicle)
		{
			targets.Remove(aerialVehicle);
			activeTargets.Remove(aerialVehicle);
			if (!targets.Any())
			{
				SettlementPositionTracker.DeactivateSettlementDefenses(this);
			}
		}

		public void ExposeData()
		{
			Scribe_References.Look(ref settlement, "settlement");

			Scribe_Values.Look(ref radarDistance, "radarDistance");
			Scribe_Values.Look(ref ticksPerShot, "ticksPerShot");

			Scribe_Defs.Look(ref antiAircraft, "antiAircraft");

			Scribe_Values.Look(ref cooldownTimer, "cooldownTimer");

			if (Scribe.mode == LoadSaveMode.Saving)
			{
				foreach (var target in targets)
				{
					targetsForSave.Add(new GlobalTargetInfo(target));
				}
			}
			Scribe_Collections.Look(ref targetsForSave, "targets", LookMode.GlobalTargetInfo);
			if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				foreach (var gTarget in targetsForSave)
				{
					targets.Add(gTarget.WorldObject as AerialVehicleInFlight);
				}
			}
		}
	}
}