using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using HarmonyLib;
using Vehicles.Defs;

namespace Vehicles
{

    public class HarmonyPatches_Bootstrap : Mod
    {
        public HarmonyPatches_Bootstrap(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("rimworld.boats_bootstrap.smashphil");

            harmony.Patch(original: AccessTools.Property(type: typeof(RaceProperties), name: nameof(RaceProperties.IsFlesh)).GetGetMethod(),
                prefix: new HarmonyMethod(typeof(HarmonyPatches_Bootstrap),
                nameof(BoatsNotFlesh)));
        }

        public static bool BoatsNotFlesh(ref bool __result, RaceProperties __instance)
        {
            if (__instance.FleshType == FleshTypeDefOf_Ships.WoodenShip || __instance.FleshType == FleshTypeDefOf_Ships.MetalShip)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
