using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles
{
	public class JobDriver_PrepareVehicleCaravan_GatheringItems : JobDriver
	{
		public Thing ToHaul
		{
			get
			{
				return job.GetTarget(TargetIndex.A).Thing;
			}
		}

		public VehiclePawn Carrier
		{
			get
			{
				return job.GetTarget(TargetIndex.B).Thing as VehiclePawn;
			}
		}

		private List<TransferableOneWay> Transferables
		{
			get
			{
				return ((LordJob_FormAndSendVehicles)job.lord.LordJob).transferables;
			}
		}

		private TransferableOneWay Transferable
		{
			get
			{
				TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatchingDesperate(ToHaul, Transferables,
					TransferAsOneMode.PodsOrCaravanPacking);
				if(!(transferableOneWay is null))
				{
					return transferableOneWay;
				}
				throw new InvalidOperationException("Could not find any matching transferable.");
			}
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			Pawn pawn = this.pawn;
			LocalTargetInfo target = ToHaul;
			Job job = this.job;
			return pawn.Reserve(target, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOn(() => !Map.lordManager.lords.Contains(job.lord));
			Toil reserve = Toils_Reserve.Reserve(TargetIndex.A, 1, -1, null).FailOnDespawnedOrNull(TargetIndex.A);
			yield return reserve;
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			yield return DetermineNumToHaul();
			yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, true, false);
			yield return AddCarriedThingToTransferables();
			yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserve, TargetIndex.A, TargetIndex.None, true, (Thing x) =>
				Transferable.things.Contains(x));
			Toil findCarrier = FindCarrier();
			yield return findCarrier;
			yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).JumpIf(() => !IsUsableCarrier(Carrier, pawn),
				findCarrier);
			yield return Toils_General.Wait(25, TargetIndex.None).JumpIf(() => !IsUsableCarrier(Carrier, pawn),
				findCarrier).WithProgressBarToilDelay(TargetIndex.B, false, -0.5f);
			yield return PlaceTargetInCarrierInventory();
		}

		private Toil DetermineNumToHaul()
		{
			return new Toil
			{
				initAction = delegate ()
				{
					int num = GatherItemsForVehicleCaravanUtility.CountLeftToTransfer(pawn, Transferable, job.lord);
					if (pawn.carryTracker.CarriedThing != null)
					{
						num -= pawn.carryTracker.CarriedThing.stackCount;
					}
					if (num <= 0)
					{
						pawn.jobs.EndCurrentJob(JobCondition.Succeeded, true);
					}
					else
					{
						job.count = num;
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant,
				atomicWithPrevious = true
			};
		}

		private Toil AddCarriedThingToTransferables()
		{
			return new Toil
			{
				initAction = delegate ()
				{
					TransferableOneWay transferable = Transferable;
					if (!transferable.things.Contains(pawn.carryTracker.CarriedThing))
					{
						transferable.things.Add(pawn.carryTracker.CarriedThing);
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant,
				atomicWithPrevious = true
			};
		}

		private Toil FindCarrier()
		{
			return new Toil
			{
				initAction = delegate ()
				{
					Pawn pawn = FindBestCarrierShips();
					if (pawn is null)
					{
						bool flag = this.pawn.GetLord() == job.lord;
						if (flag && !MassUtility.IsOverEncumbered(this.pawn))
						{
							pawn = this.pawn;
						}
						else
						{
							if (pawn is null)
							{
								if (flag)
								{
									pawn = this.pawn;
								}
								else
								{
									IEnumerable<Pawn> source = from x in job.lord.ownedPawns
															   where x is VehiclePawn v && IsUsableCarrier(v, this.pawn)
															   select x;
									if(!source.Any())
									{
										EndJobWith(JobCondition.Incompletable);
										return;
									}
									pawn = source.RandomElement();
								}
							}
						}
					}
					job.SetTarget(TargetIndex.B, pawn);
				}
			};
		}

		private Toil PlaceTargetInCarrierInventory()
		{
			return new Toil
			{
				initAction = delegate ()
				{
					Pawn_CarryTracker carryTracker = pawn.carryTracker;
					Thing carriedThing = carryTracker.CarriedThing;
					Transferable.AdjustTo(Mathf.Max(Transferable.CountToTransfer - carriedThing.stackCount, 0));
					Carrier.AddOrTransfer(carriedThing, carriedThing.stackCount);
					//carryTracker.innerContainer.TryTransferToContainer(carriedThing, Carrier.inventory.innerContainer,
						//carriedThing.stackCount, true);
				}
			};
		}

		public static bool IsUsableCarrier(VehiclePawn vehicle, Pawn forPawn)
		{
			return vehicle.IsFormingVehicleCaravan() && (!vehicle.DestroyedOrNull() && vehicle.Spawned) && vehicle.Faction == forPawn.Faction 
				&& !vehicle.IsBurning() && vehicle.movementStatus != VehicleMovementStatus.Offline
				&& !MassUtility.IsOverEncumbered(vehicle);
		}

		private float GetCarrierScore(Pawn pawn)
		{
			return (1f - MassUtility.EncumbrancePercent(pawn)) - (pawn.Position - this.pawn.Position).LengthHorizontal / 10f * 0.2f;
		}

		private VehiclePawn FindBestCarrierShips()
		{
			Lord lord = job.lord;
			VehiclePawn pawn = null;
			float num = 0f;
			if(!(lord is null))
			{
				foreach(Pawn p in lord.ownedPawns)
				{
					if(p != this.pawn && p is VehiclePawn vehicle)
					{
						if(IsUsableCarrier(vehicle, this.pawn))
						{
							float carrierScore = GetCarrierScore(vehicle);
							if(pawn is null || carrierScore > num)
							{
								pawn = vehicle;
								num = carrierScore;
							}
						}
					}
				}
			}
			return pawn;
		}
	}
}
