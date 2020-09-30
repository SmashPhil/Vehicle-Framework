using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
    public class VehicleComp : ThingComp
    {
        public virtual void AITick()
        {
            if (Find.TickManager.TicksGame % TickRareInterval == 0)
            {
                AIAutoCheck();
            }
        }

        public virtual void AIAutoCheck()
        {

        }

        private const int TickRareInterval = 250;
    }
}
