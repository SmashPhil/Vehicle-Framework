using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace Vehicles
{
    public class CompSavePawnReference : ThingComp
    {
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref pawnReference, "pawnReference");
        }

        public Pawn pawnReference;
    }
}
