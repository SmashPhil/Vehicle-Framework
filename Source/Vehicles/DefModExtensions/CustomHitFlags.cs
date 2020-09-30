using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
    public class CustomHitFlags : Def
    {
        public float minFillPercent = -1f;

        public bool flyPastTarget;
        public bool ricochet;

        public bool hitThroughPawns;
        //Add condition to explode or ricochet on ground? Angled?
    }
}
