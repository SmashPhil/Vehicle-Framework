using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace RimShips.Jobs
{
    internal class Toils_Disembark
    {
        public static Toil DisembarkWithLord(Pawn ship)
        {
            Toil toil = new Toil();
            toil.initAction = delegate ()
            {
                ship.GetComp<CompShips>().DisembarkAndAssignLord();
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }
    }
}
