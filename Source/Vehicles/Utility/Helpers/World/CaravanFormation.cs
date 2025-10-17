using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Performance;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Vehicles.World;

public static class CaravanFormation
{
	// Having globals like this is ugly but much of the vanilla logic shared across patches is a result of RimWorld
	// duplicating logic between Dialog_SplitCaravan and Dialog_FormCaravan. It's easier to access from here than it
	// is trying to pass around proper data structures between all the patches.
	public static SplitInfo splitter;
	public static FormationInfo formation;

	public static ICaravanInfo Current => formation != null ? formation : splitter;

	public static bool TryShowConfirmLeaveVehiclesDialog(Dialog_FormCaravan formCaravan)
	{
		Assert.IsNotNull(formation);
		formation.RecacheTransferables();
		if (formation.unselectedVehicles.Count > 0 && !formCaravan.transferables.Exists(PawnLeftBehind))
		{
			string vehicles = "";
			foreach (VehiclePawn vehicle in formation.unselectedVehicles)
			{
				vehicles += vehicle.LabelShort;
			}

			Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
				"VF_LeaveVehicleBehindCaravan".Translate(vehicles), delegate
				{
					if (!CheckForErrors())
						return;

					formation.AddItemsFromTransferablesToRandomInventories(formation.AllPawnsAndVehicles);
					VehicleCaravan caravan = CaravanHelper.ExitMapAndCreateVehicleCaravan(formation.AllPawnsAndVehicles,
						Faction.OfPlayer, formCaravan.CurrentTile, formCaravan.CurrentTile,
						formation.DestinationTile, false);
					formation.Map.Parent.CheckRemoveMapNow();
					TaggedString taggedString = "MessageReformedCaravan".Translate();
					if (caravan.vehiclePather.Moving && caravan.vehiclePather.ArrivalAction != null)
					{
						taggedString += " " + "MessageFormedCaravan_Orders".Translate() + ": " +
							caravan.vehiclePather.ArrivalAction.Label + ".";
					}
					Messages.Message(taggedString, caravan, MessageTypeDefOf.TaskCompletion, false);
				}));
			return true;
		}
		return false;

		static bool PawnLeftBehind(TransferableOneWay transferable)
		{
			return transferable.AnyThing is Pawn and not VehiclePawn && transferable.CountToTransfer == 0;
		}
	}

	public static void TrySendVehicleCaravan(Dialog_FormCaravan formCaravan)
	{
		Assert.IsNotNull(formation);
		formation.RecacheTransferables();
		if (formation.Reform)
		{
			ReformInstantly();
			return;
		}

		StringBuilder warningBuilder = new();
		(float days, float tillRot) daysWorthOfFood = formation.DaysWorthOfFood;
		if (daysWorthOfFood.days < 5f)
		{
			warningBuilder.AppendLine(daysWorthOfFood.days < 0.1f ?
				"DaysWorthOfFoodWarningDialog_NoFood".Translate().ToString() :
				"DaysWorthOfFoodWarningDialog".Translate(daysWorthOfFood.days.ToString("0.#")).Resolve());
		}
		else if (formation.MostFoodWillRotSoon)
		{
			warningBuilder.AppendLine("CaravanFoodWillRotSoonWarningDialog".Translate());
		}
		if (!formation.pawns.Any(pawn => CaravanUtility.IsOwner(pawn, Faction.OfPlayer) &&
			pawn.skills?.GetSkill(SkillDefOf.Social) is not { TotallyDisabled: false }))
		{
			warningBuilder.AppendLine("CaravanIncapableOfSocial".Translate());
		}
		if (formation.ShouldShowWarningForUndesirableFood())
		{
			warningBuilder.AppendLine("DaysWorthOfFoodDietWarningDialog".Translate());
		}
		if (formation.ShouldShowWarningForMechWithoutMechanitor())
		{
			warningBuilder.AppendLine("CaravanLacksMechMechanitorWarning".Translate());
		}
		if (ModsConfig.BiotechActive)
		{
			bool header = false;
			foreach (Pawn pawn in formation.pawns)
			{
				Hediff_PsychicBond bond =
					pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicBond) as Hediff_PsychicBond;
				if (bond == null)
					continue;
				if (!ThoughtWorker_PsychicBondProximity.NearPsychicBondedPerson(pawn, bond))
					continue;
				if (formation.pawns.Contains(bond.target))
					continue;

				if (!header)
				{
					header = true;
					warningBuilder.AppendLine("PsychicBondDistanceWillBeActive_Caravan".Translate() + ":");
				}
				warningBuilder.AppendLine(
					$"  - {pawn.NameFullColored.Resolve()} ({"Partner".Translate(bond.target).Resolve().CapitalizeFirst()})");
			}
		}
		if (warningBuilder.Length > 0 && CheckForErrors())
		{
			warningBuilder.AppendLine("CaravanAreYouSure".Translate());
			Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(warningBuilder.ToString(), delegate
			{
				if (TryFormAndSendCaravan())
				{
					formCaravan.Close(false);
				}
			}));
		}
		else if (TryFormAndSendCaravan())
		{
			SoundDefOf.Tick_High.PlayOneShotOnCamera();
			formCaravan.Close(false);
		}
		return;

		bool TryFormAndSendCaravan()
		{
			foreach (Pawn pawn in formation.pawns)
			{
				if (pawn is VehiclePawn vehicle)
				{
					vehicle.DisembarkAll();
				}
			}
			if (!CheckForErrors())
			{
				return false;
			}
			Direction8Way direction8WayFromTo =
				Find.WorldGrid.GetDirection8WayFromTo(formation.Dialog.CurrentTile, formation.StartingTile);
			if (!TryFindExitSpot(formation.pawns, reachableForEveryColonist: true, out IntVec3 intVec))
			{
				if (!TryFindExitSpot(formation.pawns, reachableForEveryColonist: false, out intVec))
				{
					Messages.Message(
						"CaravanCouldNotFindExitSpot".Translate(direction8WayFromTo.LabelShort()),
						MessageTypeDefOf.RejectInput, false);
					return false;
				}
				Messages.Message(
					"CaravanCouldNotFindReachableExitSpot".Translate(direction8WayFromTo.LabelShort()),
					new GlobalTargetInfo(intVec, formation.Map), MessageTypeDefOf.CautionInput, false);
			}
			if (!TryFindRandomPackingSpot(intVec, out IntVec3 meetingPoint))
			{
				Messages.Message(
					"CaravanCouldNotFindPackingSpot".Translate(direction8WayFromTo.LabelShort()),
					new GlobalTargetInfo(intVec, formation.Map), MessageTypeDefOf.RejectInput, false);
				return false;
			}
			formation.RecacheTransferables();
			VehicleCaravanFormingUtility.StartFormingCaravan(formation.Dialog.transferables,
				meetingPoint, intVec, formation.StartingTile, formation.DestinationTile);
			Messages.Message("CaravanFormationProcessStarted".Translate(), formation.pawns[0],
				MessageTypeDefOf.PositiveEvent, false);
			return true;
		}
	}

	private static void ReformInstantly()
	{
		if (!CheckForErrors())
		{
			SoundDefOf.ClickReject.PlayOneShotOnCamera();
			return;
		}
		CaravanHelper.BoardAllAssignedPawns();
		formation.AddItemsFromTransferablesToRandomInventories(formation.AllPawnsAndVehicles);

		List<CaravanGrouper.Group> vehicles =
			CaravanGrouper.ExtractIncompatibleCaravanGroups(formation.vehicles, formation.pawns);
		foreach (CaravanGrouper.Group group in vehicles)
		{
			VehicleCaravan caravan = CaravanHelper.ExitMapAndCreateVehicleCaravan(group.AllPawns,
				Faction.OfPlayer, formation.Dialog.CurrentTile, formation.Dialog.CurrentTile, formation.DestinationTile,
				sendMessage: false);
			TaggedString taggedString = "MessageReformedCaravan".Translate();
			if (caravan.vehiclePather.Moving && caravan.vehiclePather.ArrivalAction != null)
			{
				taggedString += $" {"MessageFormedCaravan_Orders".Translate()}: {caravan.vehiclePather.ArrivalAction.Label}.";
			}
			Messages.Message(taggedString, caravan, MessageTypeDefOf.TaskCompletion, false);
		}
		formation.Map.Parent.CheckRemoveMapNow();
		formation.Dialog.Close(doCloseSound: false);
	}

	private static bool CheckForErrors()
	{
		Assert.IsNotNull(formation);
		if (formation.MustChooseRoute && !formation.DestinationTile.Valid)
		{
			Messages.Message("MessageMustChooseRouteFirst".Translate(), MessageTypeDefOf.RejectInput,
				false);
			return false;
		}
		if (!formation.Reform && !formation.StartingTile.Valid)
		{
			Messages.Message("MessageNoValidExitTile".Translate(), MessageTypeDefOf.RejectInput, false);
			return false;
		}
		if (!formation.pawns.Any(pawn =>
			CaravanUtility.IsOwner(pawn, Faction.OfPlayer) && !pawn.Downed))
		{
			Messages.Message(
				ModsConfig.IdeologyActive ?
					"CaravanMustHaveAtLeastOneNonSlaveColonist".Translate() :
					"CaravanMustHaveAtLeastOneColonist".Translate(),
				MessageTypeDefOf.RejectInput, false);
			return false;
		}
		if (!formation.Reform && formation.Dialog.MassUsage > formation.Dialog.MassCapacity)
		{
			formation.FlashMass();
			Messages.Message("TooBigCaravanMassUsage".Translate(), MessageTypeDefOf.RejectInput, false);
			return false;
		}

		if (!CaravanHelper.CanStartCaravan(formation.pawns))
			return false;

		if (formation.pawns.Count > 0)
		{
			foreach (VehiclePawn vehicle in formation.vehicles)
			{
				foreach (Pawn pawn in formation.pawns)
				{
					if (pawn.Spawned && pawn.IsColonist &&
						!pawn.CanReach(vehicle, PathEndMode.Touch, Danger.Deadly))
					{
						Messages.Message("CaravanPawnIsUnreachable".Translate(pawn.LabelShort, pawn), pawn,
							MessageTypeDefOf.RejectInput, historical: false);
						return false;
					}
				}
			}
		}

		if (formation.vehicles.Any(v => v.CountAssignedToVehicle() < v.PawnCountToOperate))
		{
		}
		foreach (TransferableOneWay transferable in formation.Dialog.transferables)
		{
			if (transferable.ThingDef.category == ThingCategory.Item)
			{
				int countToTransfer = transferable.CountToTransfer;
				int countAvailable = 0;
				if (countToTransfer <= 0)
					continue;

				foreach (Thing thing in transferable.things)
				{
					if (!thing.Spawned || formation.pawns.NotNullAndAny(pawn => pawn.IsColonist &&
						(pawn.CanReach(thing, PathEndMode.Touch, Danger.Deadly) ||
							CanReachUnspawned(pawn, thing))))
					{
						countAvailable += thing.stackCount;
						if (countAvailable >= countToTransfer)
							break;
					}
				}
				if (countAvailable >= countToTransfer)
					continue;

				Messages.Message(countToTransfer == 1 ?
						"CaravanItemIsUnreachableSingle".Translate(transferable.ThingDef.label) :
						"CaravanItemIsUnreachableMulti".Translate(countToTransfer, transferable.ThingDef.label),
					MessageTypeDefOf.RejectInput, false);
				return false;
			}
		}
		return true;

		static bool CanReachUnspawned(Pawn pawn, Thing thing, PathEndMode peMode = PathEndMode.Touch,
			TraverseMode mode = TraverseMode.PassDoors, Danger maxDanger = Danger.Deadly)
		{
			VehiclePawn vehicle = pawn.GetVehicle();
			if (vehicle is null)
				return false;

			return formation.Map.reachability.CanReach(vehicle.Position, thing.Position, peMode,
				new TraverseParms
				{
					maxDanger = maxDanger,
					mode = mode,
					canBashDoors = false,
					canBashFences = false,
					alwaysUseAvoidGrid = false,
					fenceBlocked = false
				});
		}
	}

	private static bool TryFindExitSpot(List<Pawn> pawns, bool reachableForEveryColonist,
		out IntVec3 spot)
	{
		CaravanExitMapUtility.GetExitMapEdges(Find.WorldGrid, formation.Dialog.CurrentTile,
			formation.StartingTile,
			out Rot4 primary, out Rot4 secondary);


		bool result = primary != Rot4.Invalid &&
			TryFindExitSpot(pawns, reachableForEveryColonist, primary, out spot) ||
			secondary != Rot4.Invalid &&
			TryFindExitSpot(pawns, reachableForEveryColonist, secondary, out spot) ||
			TryFindExitSpot(pawns, reachableForEveryColonist,
				primary.Rotated(RotationDirection.Clockwise), out spot) ||
			TryFindExitSpot(pawns, reachableForEveryColonist,
				primary.Rotated(RotationDirection.Counterclockwise), out spot);
		formation.LeadVehicle.ClampToMap(ref spot, formation.Map);
		return result;
	}

	private static bool TryFindExitSpot(List<Pawn> pawns, bool reachableForEveryColonist, Rot4 exitDirection,
		out IntVec3 spot)
	{
		spot = IntVec3.Invalid;
		if (!formation.StartingTile.Valid)
		{
			Log.Error("Can't find exit spot because startingTile is not set.");
			return spot.IsValid;
		}
		return TryFindExitSpot(formation.Map, pawns, reachableForEveryColonist, exitDirection,
			out spot, false);
	}

	// TODO 1.6.2091 - stub to prevent breaking VehicleMapFramework patch.
	private static bool TryFindExitSpot(Map map, List<Pawn> pawns,
		bool reachableForEveryColonist, Rot4 exitDirection, out IntVec3 spot, bool _)
	{
		return TryFindExitSpot(map, pawns, reachableForEveryColonist, exitDirection, out spot);
	}

	private static bool TryFindExitSpot(Map map, List<Pawn> pawns,
		bool reachableForEveryColonist, Rot4 exitDirection, out IntVec3 spot)
	{
		if (reachableForEveryColonist)
		{
			return CellFinderExtended.TryFindRandomEdgeCellWith(CellValidator, map, exitDirection,
				formation.LeadVehicle.VehicleDef, CellFinder.EdgeRoadChance_Always,
				out spot);
		}
		// Finding exit point that might not reachable for everyone
		IntVec3 cell = IntVec3.Invalid;
		int numberCanReach = -1;
		using var cps = GlobalObjectPool.Get(out List<IntVec3> workingList);
		foreach (IntVec3 edgeCell in CellRect.WholeMap(map).GetEdgeCells(exitDirection).InRandomOrder(workingList))
		{
			IntVec3 paddedCell = edgeCell.PadForHitbox(map, formation.LeadVehicle);
			if (!ValidForAllVehicles(map, paddedCell))
				continue;

			int currentCount = 0;
			foreach (Pawn pawn in pawns)
			{
				if (!pawn.IsColonist || pawn.Downed)
					continue;
				if (CaravanHelper.assignedSeats.IsAssigned(pawn))
					continue;

				if (pawn.CanReach(paddedCell, PathEndMode.Touch, Danger.Deadly))
				{
					currentCount++;
				}
			}
			if (currentCount > numberCanReach)
			{
				numberCanReach = currentCount;
				cell = paddedCell;
			}
		}
		spot = cell;
		return cell.IsValid;

		bool CellValidator(IntVec3 exitSpot)
		{
			foreach (VehiclePawn vehicle in formation.vehicles)
			{
				if (!ValidVehicleExitSpot(exitSpot, vehicle, map))
					return false;
			}
			foreach (Pawn pawn in pawns)
			{
				if (!pawn.IsColonist)
					continue;
				if (CaravanHelper.assignedSeats.IsAssigned(pawn))
					continue;

				if (!pawn.CanReach(exitSpot, PathEndMode.Touch, Danger.Deadly))
				{
					return false;
				}
			}
			return true;
		}

		static bool ValidForAllVehicles(Map map, IntVec3 cell)
		{
			foreach (VehiclePawn vehicle in formation.vehicles)
			{
				if (!ValidVehicleExitSpot(cell, vehicle, map))
					return false;
			}
			return true;
		}

		static bool ValidVehicleExitSpot(IntVec3 cell, VehiclePawn vehicle, Map map)
		{
			return !cell.Fogged(map) &&
				vehicle.CanReachVehicle(cell, PathEndMode.OnCell, Danger.Deadly) &&
				vehicle.DrivableRectOnCell(cell, Ext_Vehicles.DestinationHitboxReq.AnyRotation);
		}
	}

	private static bool TryFindRandomPackingSpot(IntVec3 exitSpot, out IntVec3 packingSpot)
	{
		const int SqrRadiusSmall = 15;
		const int SqrRadiusMed = 25;

		using var cps = GlobalObjectPool.Get(out List<Thing> tmpPackingSpots);
		List<Thing> packingSpots =
			formation.Map.listerThings.ThingsOfDef(ThingDefOf.CaravanPackingSpot);
		if (formation.Dialog.transferables.NotNullAndAny(x =>
			x.ThingDef.category is ThingCategory.Pawn && x.AnyThing.IsBoat()))
		{
			TraverseParms traverseParms = TraverseParms.For(TraverseMode.PassDoors);
			foreach (Thing packingSpotThing in packingSpots)
			{
				foreach (VehiclePawn vehicle in formation.vehicles)
				{
					if (formation.Map.reachability.CanReach(vehicle.Position, packingSpotThing,
						PathEndMode.OnCell,
						traverseParms))
						tmpPackingSpots.Add(packingSpotThing);
				}
			}
			if (tmpPackingSpots.Count > 0)
			{
				Thing thing = tmpPackingSpots.RandomElement();
				packingSpot = thing.Position;
				return true;
			}

			bool found = CellFinder.TryFindRandomCellNear(formation.LeadVehicle.Position, formation.Map,
				SqrRadiusSmall, Validator, out packingSpot);
			if (!found)
			{
				found = CellFinder.TryFindRandomCellNear(formation.LeadVehicle.Position, formation.Map,
					SqrRadiusMed, ValidatorRelaxed, out packingSpot);
			}

			if (!found)
			{
				Messages.Message("VF_PackingSpotNotFound".Translate(), MessageTypeDefOf.CautionInput,
					false);
				found = RCellFinder.TryFindRandomSpotJustOutsideColony(formation.LeadVehicle.Position,
					formation.Map, out packingSpot);
			}
			return found;
		}
		TraverseParms traverseParams = TraverseParms.For(TraverseMode.PassDoors);
		foreach (Thing packingSpotThing in packingSpots)
		{
			if (formation.Map.reachability.CanReach(exitSpot, packingSpotThing, PathEndMode.OnCell,
				traverseParams))
			{
				tmpPackingSpots.Add(packingSpotThing);
			}
		}
		if (tmpPackingSpots.Count > 0)
		{
			Thing packingSpotThing = tmpPackingSpots.RandomElement();
			packingSpot = packingSpotThing.Position;
			return true;
		}
		return RCellFinder.TryFindRandomSpotJustOutsideColony(exitSpot, formation.Map, out packingSpot);

		static bool Validator(IntVec3 cell)
		{
			return cell.InBounds(formation.Map) && cell.Standable(formation.Map) &&
				NotUnderVehicle(cell) && !formation.Map.terrainGrid.TerrainAt(cell).IsWater;
		}

		static bool ValidatorRelaxed(IntVec3 cell)
		{
			return cell.InBounds(formation.Map) && cell.Standable(formation.Map);
		}

		static bool NotUnderVehicle(IntVec3 cell)
		{
			List<Thing> thingList = cell.GetThingList(formation.Map);
			if (thingList == null)
				return false;
			return thingList.Exists(ThingIsVehicle);

			static bool ThingIsVehicle(Thing thing)
			{
				return thing is VehiclePawn;
			}
		}
	}
}