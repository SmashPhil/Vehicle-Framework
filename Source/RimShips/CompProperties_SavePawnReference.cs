using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace RimShips
{
    public class CompProperties_SavePawnReference : CompProperties
    {
        public CompProperties_SavePawnReference()
        {
            this.compClass = typeof(CompSavePawnReference);
        }
    }
}
