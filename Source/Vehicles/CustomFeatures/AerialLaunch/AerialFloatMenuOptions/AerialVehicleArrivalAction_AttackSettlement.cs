using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class AerialVehicleArrivalAction_AttackSettlement : AerialVehicleArrivalAction
	{
		public LaunchProtocol launchProtocol;
		public Settlement settlement;
		public AerialVehicleArrivalModeDef arrivalModeDef;

		public AerialVehicleArrivalAction_AttackSettlement()
		{
		}

		public AerialVehicleArrivalAction_AttackSettlement(VehiclePawn vehicle, LaunchProtocol launchProtocol, Settlement settlement, AerialVehicleArrivalModeDef arrivalModeDef) : base(vehicle)
		{
			this.launchProtocol = launchProtocol;
			this.settlement = settlement;
			this.arrivalModeDef = arrivalModeDef;
		}

		public override bool DestroyOnArrival => true;

		public override FloatMenuAcceptanceReport StillValid(int destinationTile)
		{
			if (settlement != null && settlement.Tile != destinationTile)
			{
				return false;
			}
			return CanAttack(vehicle, settlement);
		}

		public override void Arrived(int tile)
		{
			LongEventHandler.QueueLongEvent(delegate ()
			{
				Map map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, null);
				TaggedString label = "LetterLabelCaravanEnteredEnemyBase".Translate();
				TaggedString text = "LetterTransportPodsLandedInEnemyBase".Translate(settlement.Label).CapitalizeFirst();
				SettlementUtility.AffectRelationsOnAttacked(settlement, ref text);
				if (!settlement.HasMap)
				{
					Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
					PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(map.mapPawns.AllPawns, ref label, ref text, "LetterRelatedPawnsInMapWherePlayerLanded".Translate(Faction.OfPlayer.def.pawnsPlural), true, true);
				}
				Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NeutralEvent, vehicle, settlement.Faction, null, null, null);
				arrivalModeDef.Worker.VehicleArrived(vehicle, launchProtocol, settlement.Map);
			}, "GeneratingMap", false, null, true);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref settlement, "settlement");
			Scribe_Defs.Look(ref arrivalModeDef, "arrivalModeDef");
			Scribe_Deep.Look(ref launchProtocol, "launchProtocol");
		}

		public static FloatMenuAcceptanceReport CanAttack(VehiclePawn vehicle, Settlement settlement)
		{
			if (settlement is null || !settlement.Spawned || !settlement.Attackable)
			{
				return false;
			}
			if (!WorldVehiclePathGrid.Instance.Passable(settlement.Tile, vehicle.VehicleDef))
			{
				return false;
			}
			if (settlement.EnterCooldownBlocksEntering())
			{
				return FloatMenuAcceptanceReport.WithFailReasonAndMessage("EnterCooldownBlocksEntering".Translate(), "MessageEnterCooldownBlocksEntering".Translate(settlement.EnterCooldownTicksLeft().ToStringTicksToPeriod(true, false, true, true)));
			}
			return true;
		}

		public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(VehiclePawn vehicle, LaunchProtocol launchProtocol, Settlement settlement)
		{
			if (vehicle.CompVehicleLauncher.ControlInFlight)
			{
				foreach (FloatMenuOption floatMenuOption2 in VehicleArrivalActionUtility.GetFloatMenuOptions(() => CanAttack(vehicle, settlement), () => new AerialVehicleArrivalAction_AttackSettlement(vehicle, launchProtocol, settlement, AerialVehicleArrivalModeDefOf.TargetedLanding),
				"AttackAndTargetLanding".Translate(settlement.Label), vehicle, settlement.Tile, null))
				{
					yield return floatMenuOption2;
				}
			}
			foreach (FloatMenuOption floatMenuOption2 in VehicleArrivalActionUtility.GetFloatMenuOptions(() => CanAttack(vehicle, settlement), () => new AerialVehicleArrivalAction_AttackSettlement(vehicle, launchProtocol, settlement, AerialVehicleArrivalModeDefOf.EdgeDrop),
					"AttackAndDropAtEdge".Translate(settlement.Label), vehicle, settlement.Tile, null))
			{
				yield return floatMenuOption2;
			}
			foreach (FloatMenuOption floatMenuOption2 in VehicleArrivalActionUtility.GetFloatMenuOptions(() => CanAttack(vehicle, settlement), () => new AerialVehicleArrivalAction_AttackSettlement(vehicle, launchProtocol, settlement, AerialVehicleArrivalModeDefOf.CenterDrop),
				"AttackAndDropInCenter".Translate(settlement.Label), vehicle, settlement.Tile, null))
			{
				yield return floatMenuOption2;
			}
		}
	}
}
