using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimShips.Jobs
{
    public class JobDriver_Disembark : JobDriver
    {
        public IntVec3 LandingPosition
        {
            get
            {
                return this.job.GetTarget(TargetIndex.A).Cell;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            if(!ShipHarmony.IsShip(this.pawn))
            {
                Log.Error("JobDriver_Disembark is only meant for boats.");
                yield break;
            }
            yield return Toils_Disembark.DisembarkWithLord(this.pawn);
        }
    }
}
