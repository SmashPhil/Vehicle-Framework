using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips.Jobs
{
    public class JobDriver_CarryPawnToShip : JobDriver
    {
        public Pawn ShipToBoard => (Pawn)this.job.GetTarget(TargetIndex.B).Thing;

        public Pawn PawnToBoard => (Pawn)this.job.GetTarget(TargetIndex.A).Thing;
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

            this.FailOn(() => !ShipToBoard.GetComp<CompShips>().handlers.Find(x => x.role.handlingType == HandlingTypeFlags.None).AreSlotsAvailable);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell).FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOn(() =>
                !PawnToBoard.Downed).FailOn(() => !this.pawn.CanReach(this.PawnToBoard, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn)).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, false, false);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
            yield return Toils_General.Wait(250, TargetIndex.None).FailOnCannotTouch(TargetIndex.B, PathEndMode.Touch).WithProgressBarToilDelay(TargetIndex.B, false, -0.5f);
            
            yield return PutPawnOnShip(PawnToBoard, ShipToBoard);
            yield break;
        }

        public static Toil PutPawnOnShip(Pawn pawnToBoard, Pawn ship)
        {
            Toil toil = new Toil();
            toil.initAction = delegate ()
            {
                CompShips shipComp = ship.GetComp<CompShips>();
                shipComp.Notify_Boarded(pawnToBoard);
                ShipHandler handler = shipComp.handlers.Find(x => x.role.handlingType == HandlingTypeFlags.None);
                shipComp.GiveLoadJob(pawnToBoard, handler);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }
    }
}
