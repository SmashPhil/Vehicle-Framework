using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Vehicles.Jobs
{
    public class JobDriver_Board : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
        
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnDowned(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_Board.BoardShip(pawn, TargetIndex.A);
        }
    }
}
