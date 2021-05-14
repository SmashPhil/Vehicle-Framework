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
		public LaunchProtocol launchProtocol;
		public MapParent parent;

		public IntVec3 start;
		public IntVec3 end;

		public AerialVehicleArrivalAction_StrafeMap()
		{
		}

		public AerialVehicleArrivalAction_StrafeMap(VehiclePawn vehicle, MapParent parent, IntVec3 start, IntVec3 end) : base(vehicle)
		{
			launchProtocol = vehicle.CompVehicleLauncher.SelectedLaunchProtocol;
			this.parent = parent;
			this.start = start;
			this.end = end;
		}

		public override FloatMenuAcceptanceReport StillValid(int destinationTile)
		{
			if (parent != null && parent.Tile != destinationTile)
			{
				return false;
			}
			return CanAttack(vehicle, parent);
		}

		public override void Arrived(int tile)
		{
			LongEventHandler.QueueLongEvent(delegate ()
			{
				Map map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, null);
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
				VehicleSkyfaller_FlyOver skyfaller = VehicleSkyfallerMaker.MakeSkyfallerFlyOver(vehicle.CompVehicleLauncher.Props.skyfallerFlyOver, vehicle, start, end);
				Thing thing = GenSpawn.Spawn(skyfaller, start, parent.Map, Rot8.North); //REDO - Other rotations?
				AerialVehicleInFlight aerialVehicle = vehicle.GetAerialVehicle();
				aerialVehicle.Destroy();
			}, "GeneratingMap", false, null, true);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref parent, "parent");
			Scribe_Deep.Look(ref launchProtocol, "launchProtocol");
			Scribe_Values.Look(ref start, "start");
			Scribe_Values.Look(ref end, "end");
		}

		public static FloatMenuAcceptanceReport CanAttack(VehiclePawn vehicle, MapParent parent)
		{
			if (parent is null)
			{
				return false;
			}
			if (!Find.World.GetCachedWorldComponent<WorldVehiclePathGrid>().Passable(parent.Tile, vehicle.VehicleDef))
			{
				return false;
			}
			if (parent.EnterCooldownBlocksEntering())
			{
				return FloatMenuAcceptanceReport.WithFailReasonAndMessage("EnterCooldownBlocksEntering".Translate(), "MessageEnterCooldownBlocksEntering".Translate(parent.EnterCooldownTicksLeft().ToStringTicksToPeriod(true, false, true, true)));
			}
			return true;
		}

		public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(VehiclePawn vehicle, LaunchProtocol launchProtocol, MapParent parent)
		{
			yield break;
		}
	}
}
