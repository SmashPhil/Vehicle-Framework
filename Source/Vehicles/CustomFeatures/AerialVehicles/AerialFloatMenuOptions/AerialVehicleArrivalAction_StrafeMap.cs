using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class AerialVehicleArrivalAction_StrafeMap : AerialVehicleArrivalAction
	{
		public MapParent parent;

		public AerialVehicleArrivalAction_StrafeMap()
		{
		}

		public AerialVehicleArrivalAction_StrafeMap(VehiclePawn vehicle, MapParent parent) : base(vehicle)
		{
			this.parent = parent;
		}

		public override FloatMenuAcceptanceReport StillValid(int destinationTile)
		{
			if (parent != null && parent.Tile != destinationTile)
			{
				return false;
			}
			return CanAttack(vehicle, parent);
		}
		
		//NOTE - Needs Unfogger called if map is generated
		public override void Arrived(AerialVehicleInFlight aerialVehicle, int tile)
		{
			// TODO - add base call so aerial vehicle is destroyed before long event is queued.
			LongEventHandler.QueueLongEvent(delegate ()
			{
				Map map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, null); //MAP INDEX BUG
				TaggedString label = "LetterLabelCaravanEnteredEnemyBase".Translate();
				TaggedString text = "LetterTransportPodsLandedInEnemyBase".Translate(parent.Label).CapitalizeFirst();
				if (parent is Settlement settlement)
				{
					SettlementUtility.AffectRelationsOnAttacked(settlement, ref text);
				}
				if (!parent.HasMap)
				{
					Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
					PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(map.mapPawns.AllPawns, ref label, ref text, "LetterRelatedPawnsInMapWherePlayerLanded".Translate(Faction.OfPlayer.def.pawnsPlural), true, true);
				}
				AerialVehicleInFlight aerialVehicle = vehicle.GetAerialVehicle();
				CameraJumper.TryJump(map.Center, map);
				StrafeTargeter.Instance.BeginTargeting(vehicle, vehicle.CompVehicleLauncher.launchProtocol, delegate (IntVec3 start, IntVec3 end)
				{
					VehicleSkyfaller_FlyOver skyfaller = VehicleSkyfallerMaker.MakeSkyfallerFlyOver(vehicle.CompVehicleLauncher.Props.skyfallerStrafing, vehicle, start, end);
					skyfaller.aerialVehicle = aerialVehicle;
					Thing thing = GenSpawn.Spawn(skyfaller, start, parent.Map, Rot8.North); //REDO - Other rotations?
				}, null, null, null, true);
				aerialVehicle.Destroy(); // TODO - This is redundant, should not be needed for skyfaller or targeting. Let base destroy
			}, "GeneratingMap", false, null, true);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref parent, "parent");
		}

		public static FloatMenuAcceptanceReport CanAttack(VehiclePawn vehicle, MapParent parent)
		{
			if (parent is null)
			{
				return false;
			}
			if (!WorldVehiclePathGrid.Instance.Passable(parent.Tile, vehicle.VehicleDef))
			{
				return false;
			}
			if (parent.EnterCooldownBlocksEntering())
			{
				return FloatMenuAcceptanceReport.WithFailReasonAndMessage("EnterCooldownBlocksEntering".Translate(), "MessageEnterCooldownBlocksEntering".Translate(parent.EnterCooldownTicksLeft().ToStringTicksToPeriod(true, false, true, true)));
			}
			return true;
		}
	}
}
