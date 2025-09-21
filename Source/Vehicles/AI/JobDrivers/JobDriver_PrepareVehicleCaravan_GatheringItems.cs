using System.Collections.Generic;
using System.Linq;
using RimWorld;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

public class JobDriver_PrepareVehicleCaravan_GatheringItems : JobDriver
{
	private const int WaitTicks = 25;
	private const int MaxTicksGatherItems = 7500;
	private const int LoopBackstop = 500;

	private int toilLoops;
	private int pickedUpFirstItemTicks = -1;
	private PrepareCaravanGatherState gatherState;

	public Thing ToHaul => job.GetTarget(TargetIndex.A).Thing;

	public Pawn Carrier => job.GetTarget(TargetIndex.B).Thing as Pawn;

	private List<TransferableOneWay> Transferables => ((LordJob_FormAndSendVehicles)job.lord.LordJob).transferables;

	private TransferableOneWay Transferable
	{
		get
		{
			TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatchingDesperate(ToHaul, Transferables,
				TransferAsOneMode.PodsOrCaravanPacking);
			if (transferableOneWay is null)
			{
				Trace.Fail("Could not find any matching transferable.");
				return null;
			}
			return transferableOneWay;
		}
	}

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(ToHaul, job, errorOnFailed: errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		if (gatherState == PrepareCaravanGatherState.Unset)
		{
			gatherState = !pawn.IsFormingVehicleCaravan() ||
				MassUtility.IsOverEncumbered(pawn) && !pawn.inventory.HasAnyUnpackedCaravanItems ?
					PrepareCaravanGatherState.Carry :
					PrepareCaravanGatherState.Haul;
		}
		if (gatherState == PrepareCaravanGatherState.Carry)
		{
			return MakeNewToilsCarry();
		}
		return MakeNewToilsHaulInInventory();
	}

	private IEnumerable<Toil> MakeNewToilsCarry()
	{
		this.FailOn(() => !Map.lordManager.lords.Contains(job.lord));
		Toil reserve = Toils_Reserve.Reserve(TargetIndex.A).FailOnDespawnedOrNull(TargetIndex.A);
		yield return reserve;
		bool inInventory = HaulAIUtility.IsInHaulableInventory(ToHaul);
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, canGotoSpawnedParent: inInventory);
		yield return DetermineNumToHaul();
		yield return Toils_Haul.StartCarryThing(TargetIndex.A, subtractNumTakenFromJobCount: true,
			canTakeFromInventory: inInventory);
		yield return AddCarriedThingToTransferables();
		yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserve,
			haulableInd: TargetIndex.A,
			storeCellInd: TargetIndex.None,
			takeFromValidStorage: true,
			Transferable.things.Contains);
		Toil findCarrier = FindCarrier();
		yield return findCarrier;
		yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch)
		 .JumpIf(() => !IsUsableCarrier(Carrier, pawn), findCarrier);
		yield return Toils_General.Wait(WaitTicks)
		 .JumpIf(() => !IsUsableCarrier(Carrier, pawn), findCarrier)
		 .WithProgressBarToilDelay(TargetIndex.B);
		yield return PlaceTargetInCarrierInventory();
	}

	private IEnumerable<Toil> MakeNewToilsHaulInInventory()
	{
		this.FailOn(() => !Map.lordManager.lords.Contains(job.lord));
		bool inInventory = HaulAIUtility.IsInHaulableInventory(ToHaul);
		Toil reserve = Toils_Reserve.Reserve(TargetIndex.A).FailOnDestroyedOrNull(TargetIndex.A);
		Toil findCarrier = FindCarrier();
		yield return reserve;
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, inInventory)
		 .JumpIf(IsFinishedCollectingItems, findCarrier);
		yield return DetermineNumToHaul(findCarrier);
		yield return Toils_Haul.StartCarryThing(TargetIndex.A, subtractNumTakenFromJobCount: true,
			canTakeFromInventory: inInventory);
		yield return AddCarriedThingToTransferables();
		yield return Toils_General.Wait(WaitTicks).WithProgressBarToilDelay(TargetIndex.B);
		yield return HaulCaravanItemInInventory(reserve);
		yield return findCarrier;
		yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).JumpIf(
			() => !IsUsableCarrier(Carrier, pawn), findCarrier);
		yield return Toils_General.Wait(WaitTicks)
		 .JumpIf(() => !IsUsableCarrier(Carrier, pawn), findCarrier)
		 .WithProgressBarToilDelay(TargetIndex.B);
		yield return AddHauledItemsToCarrier(findCarrier);
	}

	private bool IsFinishedCollectingItems()
	{
		return MassUtility.IsOverEncumbered(pawn) || pickedUpFirstItemTicks > -1 &&
			Find.TickManager.TicksGame > pickedUpFirstItemTicks + 7500;
	}

	private Toil HaulCaravanItemInInventory(Toil reserve)
	{
		Toil toil = ToilMaker.MakeToil();
		toil.initAction = delegate
		{
			if (pickedUpFirstItemTicks == -1)
			{
				pickedUpFirstItemTicks = Find.TickManager.TicksGame;
			}
			Transferable.AdjustTo(Mathf.Max(Transferable.CountToTransfer - pawn.carryTracker.CarriedThing.stackCount, 0));
			pawn.inventory.AddHauledCaravanItem(pawn.carryTracker.CarriedThing);
			if (!IsFinishedCollectingItems())
			{
				SetNewHaulTargetAndJumpToReserve(reserve);
			}
		};
		return toil;
	}

	private Toil AddHauledItemsToCarrier(Toil findCarrier)
	{
		Toil toil = ToilMaker.MakeToil();
		toil.initAction = delegate
		{
			if (Carrier == pawn)
			{
				pawn.inventory.ClearHaulingCaravanCache();
				return;
			}
			pawn.inventory.TransferCaravanItemsToCarrier(Carrier.inventory);
			if (pawn.inventory.HasAnyUnpackedCaravanItems && CheckToilLoopBackstop())
			{
				pawn.jobs.curDriver.JumpToToil(findCarrier);
			}
		};
		return toil;
	}

	private void SetNewHaulTargetAndJumpToReserve(Toil reserve)
	{
		if (!CheckToilLoopBackstop())
			return;

		Thing thing = GatherItemsForVehicleCaravanUtility.FindThingToHaul(pawn, pawn.GetLord());
		if (thing != null)
		{
			job.SetTarget(TargetIndex.A, thing);
			pawn.jobs.curDriver.JumpToToil(reserve);
		}
	}

	private bool CheckToilLoopBackstop()
	{
		if (++toilLoops <= LoopBackstop)
			return true;

		Log.Error($"Prepare caravan gather items job for pawn {pawn.Label} looped through toils too many times.");
		EndJobWith(JobCondition.Errored);
		return false;
	}

	private Toil DetermineNumToHaul(Toil findCarrier = null)
	{
		Toil toil = ToilMaker.MakeToil();
		toil.initAction = delegate
		{
			int count = GatherItemsForVehicleCaravanUtility.CountLeftToTransfer(pawn, Transferable, job.lord);
			if (pawn.carryTracker.CarriedThing != null)
			{
				count -= pawn.carryTracker.CarriedThing.stackCount;
			}
			if (count > 0)
			{
				job.count = count;
				return;
			}
			if (findCarrier == null || !pawn.inventory.HasAnyUnpackedCaravanItems)
			{
				pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
				return;
			}
			pawn.jobs.curDriver.JumpToToil(findCarrier);
		};
		toil.defaultCompleteMode = ToilCompleteMode.Instant;
		toil.atomicWithPrevious = true;
		return toil;
	}

	private Toil AddCarriedThingToTransferables()
	{
		Toil toil = ToilMaker.MakeToil();
		toil.initAction = delegate
		{
			TransferableOneWay transferable = Transferable;
			if (!transferable.things.Contains(pawn.carryTracker.CarriedThing))
			{
				transferable.things.Add(pawn.carryTracker.CarriedThing);
			}
		};
		toil.defaultCompleteMode = ToilCompleteMode.Instant;
		toil.atomicWithPrevious = true;
		return toil;
	}

	private Toil FindCarrier()
	{
		return new Toil
		{
			initAction = delegate
			{
				if (TryGetBestCarrier(out Pawn carrier))
				{
					job.SetTarget(TargetIndex.B, carrier);
				}
				else
				{
					EndJobWith(JobCondition.Incompletable);
				}
			}
		};

		bool TryGetBestCarrier(out Pawn carrier)
		{
			carrier = FindBestCarrier();
			carrier ??= FindBestBackupCarrier(onlyAnimals: true);
			if (carrier != null)
				return true;

			bool sameLordJob = pawn.GetLord() == job.lord;
			if (sameLordJob && !MassUtility.IsOverEncumbered(pawn))
			{
				carrier = pawn;
				return true;
			}

			carrier = FindBestBackupCarrier(onlyAnimals: false);
			if (carrier != null)
				return true;

			if (sameLordJob)
			{
				carrier = pawn;
				return true;
			}

			List<Pawn> allUsableCarriers = job.lord.ownedPawns.Where(pawn => PawnIsUsableCarrier(pawn, this.pawn)).ToList();
			carrier = allUsableCarriers.RandomElementWithFallback();
			return carrier != null;
		}

		bool PawnIsUsableCarrier(Pawn pawn, Pawn forPawn)
		{
			if (pawn is VehiclePawn vehicle)
			{
				return IsUsableCarrier(vehicle, forPawn);
			}
			return JobDriver_PrepareCaravan_GatherItems.IsUsableCarrier(pawn, forPawn, allowColonists: true);
		}
	}

	private Toil PlaceTargetInCarrierInventory()
	{
		Toil toil = ToilMaker.MakeToil();
		toil.initAction = delegate
		{
			Pawn_CarryTracker carryTracker = pawn.carryTracker;
			Thing carriedThing = carryTracker.CarriedThing;
			if (carryTracker.innerContainer.Count == 0)
			{
				carryTracker.pawn.Drawer.renderer.SetAllGraphicsDirty();
			}
			Transferable.AdjustTo(Mathf.Max(Transferable.CountToTransfer - carriedThing.stackCount, 0));
			carryTracker.innerContainer.TryTransferToContainer(carriedThing, Carrier.inventory.innerContainer,
				carriedThing.stackCount, out Thing thing);
			if (thing.TryGetComp<CompForbiddable>() is { } compForbiddable)
			{
				compForbiddable.Forbidden = false;
			}
		};
		return toil;
	}

	public static bool IsUsableCarrier(Pawn carrier, Pawn forPawn, bool allowColonists = true)
	{
		if (carrier is VehiclePawn vehicle)
		{
			return vehicle.IsFormingVehicleCaravan() && (!vehicle.DestroyedOrNull() && vehicle.Spawned) &&
				vehicle.Faction == forPawn.Faction
				&& !vehicle.IsBurning() && vehicle.movementStatus != VehicleMovementStatus.Offline
				&& !MassUtility.IsOverEncumbered(vehicle);
		}
		return !CaravanHelper.assignedSeats.IsAssigned(carrier) &&
			JobDriver_PrepareCaravan_GatherItems.IsUsableCarrier(carrier, forPawn, allowColonists: allowColonists);
	}

	private float GetCarrierScore(Pawn pawn)
	{
		return (1f - MassUtility.EncumbrancePercent(pawn)) -
			(pawn.Position - this.pawn.Position).LengthHorizontal / 10f * 0.2f;
	}

	// Same logic as base game, copied for matching behavior
	private VehiclePawn FindBestCarrier()
	{
		if (job.lord is null)
			return null;

		float highestScore = 0f;
		VehiclePawn carrier = null;
		foreach (Pawn ownedPawn in job.lord.ownedPawns)
		{
			if (ownedPawn != pawn && ownedPawn is VehiclePawn vehicle && IsUsableCarrier(vehicle, pawn))
			{
				float carrierScore = GetCarrierScore(ownedPawn);
				if (carrier == null || carrierScore > highestScore)
				{
					carrier = vehicle;
					highestScore = carrierScore;
				}
			}
		}
		return carrier;
	}

	// Same logic as base game, copied for matching behavior
	private Pawn FindBestBackupCarrier(bool onlyAnimals)
	{
		if (job.lord is null)
			return null;

		float highestScore = 0f;
		Pawn carrier = null;
		foreach (Pawn ownedPawn in job.lord.ownedPawns)
		{
			if (ownedPawn != pawn && (!onlyAnimals || ownedPawn.RaceProps.Animal) &&
				IsUsableCarrier(ownedPawn, pawn, allowColonists: false))
			{
				float carrierScore = GetCarrierScore(ownedPawn);
				if (carrier == null || carrierScore > highestScore)
				{
					carrier = ownedPawn;
					highestScore = carrierScore;
				}
			}
		}
		return carrier;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref pickedUpFirstItemTicks, nameof(pickedUpFirstItemTicks), defaultValue: -1);
		Scribe_Values.Look(ref toilLoops, nameof(toilLoops));
		Scribe_Values.Look(ref gatherState, nameof(gatherState));
	}
}