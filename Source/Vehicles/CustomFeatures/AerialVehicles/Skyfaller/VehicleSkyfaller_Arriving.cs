using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class VehicleSkyfaller_Arriving : VehicleSkyfaller
	{
		public const int NotificationSquishInterval = 60;
		public int delayLandingTicks;
		private bool punchedRoof = false;
		public Rot4 rotatePostLanding = Rot4.Invalid;

		private static CompProperties_VehicleLauncher vehicleLauncherProps;
		
		public bool LandingSpotOccupied { get; private set; }

		public Rot4 LandingRotation => rotatePostLanding.IsValid ? rotatePostLanding : Rotation;

		public CompProperties_VehicleLauncher VehicleLauncherProps
		{
			get
			{
				if (vehicleLauncherProps == null)
				{
					vehicleLauncherProps = vehicle.VehicleDef.GetCompProperties<CompProperties_VehicleLauncher>();
				}
				return vehicleLauncherProps;
			}
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			(launchProtocolDrawPos, _) = vehicle.CompVehicleLauncher.launchProtocol.Draw(RootPos, 0);
			DrawLandingGhost();
		}

		public override void Tick()
		{
			base.Tick();
			if (vehicle.CompVehicleLauncher.launchProtocol.FinishedAnimation(this))
			{
				delayLandingTicks--;
				if (delayLandingTicks <= 0 && Position.InBounds(Map))
				{
					FinalizeLanding();
					return;
				}
			}
			else if (!punchedRoof && vehicle.CompVehicleLauncher.launchProtocol.TimeInAnimation >= VehicleLauncherProps.animationPunchAt)
			{
				TryHitRoof();
			}
			if (Find.TickManager.TicksGame % NotificationSquishInterval == 0 && Map != null)
			{
				LandingSpotOccupied = false;
				foreach (Thing thing in Map.thingGrid.ThingsAt(Position))
				{
					LandingSpotOccupied |= thing is VehiclePawn;
				}
				foreach (IntVec3 cell in vehicle.PawnOccupiedCells(Position, LandingRotation))
				{
					VehicleDamager.NotifyNearbyPawnsOfDangerousPosition(Map, cell);
				}
			}
		}

		protected virtual void FinalizeLanding()
		{
			vehicle.CompVehicleLauncher.launchProtocol.Release();
			vehicle.CompVehicleLauncher.inFlight = false;
			if (VehicleReservationManager.AnyVehicleInhabitingCells(vehicle.PawnOccupiedCells(Position, LandingRotation), Map))
			{
				GenExplosion.DoExplosion(Position, Map, Mathf.Max(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z), DamageDefOf.Bomb, vehicle);
			}
			else
			{
				GenSpawn.Spawn(vehicle, Position, Map, LandingRotation);
				vehicle.TryDamageObstructions();
				if (VehicleMod.settings.main.deployOnLanding)
				{
					vehicle.CompVehicleLauncher.SetTimedDeployment();
				}
			}
			Destroy();
		}

		private void DrawLandingGhost()
		{
			if (VehicleMod.settings.main.drawLandingGhost)
			{
				Color ghostcolor = LandingSpotOccupied ? LandingTargeter.GhostOccupiedColor : VehicleGhostUtility.whiteGhostColor;
				GhostDrawer.DrawGhostThing(Position, LandingRotation, vehicle.VehicleDef, vehicle.VehicleGraphic, ghostcolor, AltitudeLayer.Blueprint, vehicle);
			}
		}

		protected virtual void TryHitRoof()
		{
			punchedRoof = true;

			CellRect cellRect = GenAdj.OccupiedRect(Position, LandingRotation, vehicle.VehicleDef.Size);
			if (cellRect.Any(cell => Ext_Vehicles.IsRoofed(cell, Map)))
			{
				RoofDef roofDef = cellRect.Cells.First(cell => Ext_Vehicles.IsRoofed(cell, Map)).GetRoof(Map);
				if (!roofDef.soundPunchThrough.NullOrUndefined())
				{
					roofDef.soundPunchThrough.PlayOneShot(new TargetInfo(Position, Map, false));
				}
				RoofCollapserImmediate.DropRoofInCells(cellRect.ExpandedBy(1).ClipInsideMap(Map).Cells.Where(delegate (IntVec3 c)
				{
					if (!c.InBounds(Map))
					{
						return false;
					}
					if (cellRect.Contains(c))
					{
						return true;
					}
					if (c.GetFirstPawn(Map) != null)
					{
						return false;
					}
					Building edifice = c.GetEdifice(Map);
					return edifice == null || !edifice.def.holdsRoof;
				}), Map, null);

				foreach (IntVec3 cell in cellRect)
				{
					IntVec2 offset = new IntVec2(cell.x - Position.x, cell.z - Position.z);
					vehicle.TakeDamage(new DamageInfo(DamageDefOf.Blunt, 5, 0), offset);
				}
			}
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!respawningAfterLoad)
			{
				vehicle.CompVehicleLauncher.launchProtocol.Prepare(map, Position, Rotation);
				vehicle.CompVehicleLauncher.launchProtocol.OrderProtocol(LaunchProtocol.LaunchType.Landing);
				delayLandingTicks = vehicle.CompVehicleLauncher.launchProtocol.CurAnimationProperties.delayByTicks;
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref delayLandingTicks, nameof(delayLandingTicks));
			Scribe_Values.Look(ref punchedRoof, nameof(punchedRoof));
		}
	}
}
