using System.Collections.Generic;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace Vehicles.Jobs
{
    public class JobDriver_CarryPawnToShip : JobDriver
    {
        public VehiclePawn VehicleToEnter => (Pawn)job.GetTarget(TargetIndex.B).Thing as VehiclePawn;

        public Pawn PawnToBoard => (Pawn)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn pawn = this.pawn;
            LocalTargetInfo target = this.job.GetTarget(TargetIndex.A);
            Job job = this.job;
            return pawn.Reserve(target, job, 1, -1, null, errorOnFailed); 
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnAggroMentalState(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.B);

            this.FailOn(() => !VehicleToEnter.GetCachedComp<CompVehicle>().handlers.Find(x => x.role.handlingTypes.NullOrEmpty()).AreSlotsAvailable);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell).FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOn(() =>
                !PawnToBoard.Downed).FailOn(() => !pawn.CanReach(PawnToBoard, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn)).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, false, false);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
            yield return Toils_General.Wait(250, TargetIndex.None).FailOnCannotTouch(TargetIndex.B, PathEndMode.Touch).WithProgressBarToilDelay(TargetIndex.B, false, -0.5f);
            
            yield return PutPawnOnShip(PawnToBoard, VehicleToEnter);
            yield break;
        }

        public static Toil PutPawnOnShip(Pawn pawnToBoard, VehiclePawn vehicle)
        {
            Toil toil = new Toil();
            toil.initAction = delegate ()
            {
                CompVehicle shipComp = vehicle.GetCachedComp<CompVehicle>();
                VehicleHandler handler = shipComp.handlers.Find(x => x.role.handlingTypes.NullOrEmpty());
                shipComp.GiveLoadJob(pawnToBoard, handler);
                shipComp.Notify_Boarded(pawnToBoard);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }
    }
}
