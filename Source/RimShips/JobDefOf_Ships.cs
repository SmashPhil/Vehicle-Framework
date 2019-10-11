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

namespace RimShips.Defs
{
    [DefOf]
    public static class JobDefOf_Ships
    {
        static JobDefOf_Ships()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf_Ships));
        }

        public static JobDef IdleShip;

        public static JobDef Board;

        //Disembark
    }
}