using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class AerialVehicleArrivalAction_AttackSettlement : AerialVehicleArrivalAction_LoadMap
	{
		public Settlement settlement;

		public AerialVehicleArrivalAction_AttackSettlement()
		{
		}

		public AerialVehicleArrivalAction_AttackSettlement(VehiclePawn vehicle, LaunchProtocol launchProtocol, int tile, AerialVehicleArrivalModeDef arrivalModeDef) : base(vehicle, launchProtocol, tile, arrivalModeDef)
		{
			settlement = Find.World.worldObjects.SettlementAt(tile);
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

		protected override void MapLoaded(Map map, bool generatedMap)
		{
			base.MapLoaded(map, generatedMap);
			TaggedString letterLabel = "LetterLabelCaravanEnteredEnemyBase".Translate();
			TaggedString letterText = "LetterTransportPodsLandedInEnemyBase".Translate(map.Parent.Label).CapitalizeFirst();
			SettlementUtility.AffectRelationsOnAttacked(settlement, ref letterText);
			if (generatedMap)
			{
				Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
				PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(map.mapPawns.AllPawns, ref letterLabel, ref letterText, "LetterRelatedPawnsInMapWherePlayerLanded".Translate(Faction.OfPlayer.def.pawnsPlural), true, true);
			}
			Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.NeutralEvent, vehicle, settlement.Faction, null, null, null);
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
				foreach (FloatMenuOption floatMenuOption2 in VehicleArrivalActionUtility.GetFloatMenuOptions(() => CanAttack(vehicle, settlement), () => new AerialVehicleArrivalAction_AttackSettlement(vehicle, launchProtocol, settlement.Tile, AerialVehicleArrivalModeDefOf.TargetedLanding),
				"VF_AttackAndTargetLanding".Translate(settlement.Label), vehicle, settlement.Tile, null))
				{
					yield return floatMenuOption2;
				}
			}
			foreach (FloatMenuOption floatMenuOption2 in VehicleArrivalActionUtility.GetFloatMenuOptions(() => CanAttack(vehicle, settlement), () => new AerialVehicleArrivalAction_AttackSettlement(vehicle, launchProtocol, settlement.Tile, AerialVehicleArrivalModeDefOf.EdgeDrop),
					"AttackAndDropAtEdge".Translate(settlement.Label), vehicle, settlement.Tile, null))
			{
				yield return floatMenuOption2;
			}
			foreach (FloatMenuOption floatMenuOption2 in VehicleArrivalActionUtility.GetFloatMenuOptions(() => CanAttack(vehicle, settlement), () => new AerialVehicleArrivalAction_AttackSettlement(vehicle, launchProtocol, settlement.Tile, AerialVehicleArrivalModeDefOf.CenterDrop),
				"AttackAndDropInCenter".Translate(settlement.Label), vehicle, settlement.Tile, null))
			{
				yield return floatMenuOption2;
			}
		}
	}
}
