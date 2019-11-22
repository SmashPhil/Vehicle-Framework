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
    public static class FleshTypeDefOf_Ships
    {
        static FleshTypeDefOf_Ships()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(FleshTypeDefOf_Ships));
        }

        public static FleshTypeDef WoodenShip;

        public static FleshTypeDef MetalShip;
    }
}
