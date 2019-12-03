using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace RimShips
{
    public class CompProperties_Cannons : CompProperties
    {
        public CompProperties_Cannons()
        {
            this.compClass = typeof(CompCannons);
        }

        public List<CannonHandler> cannons = new List<CannonHandler>();
    }
}
