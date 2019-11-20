using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using RimShips.AI;
using RimShips.Lords;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips.Defs
{
    [DefOf]
    public static class DutyDefOf_Ships
    {
        static DutyDefOf_Ships()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DutyDefOf_Ships));
        }

        public static DutyDef PrepareCaravan_BoardShip;

        public static DutyDef PrepareCaravan_GatherShip;

        public static DutyDef PrepareCaravan_WaitShip;

        public static DutyDef PrepareCaravan_GatherDownedPawns;

        public static DutyDef PrepareCaravan_SendSlavesToShip;

        public static DutyDef TravelOrWaitOcean;

        public static DutyDef TravelOrLeaveOcean;
    }

}
