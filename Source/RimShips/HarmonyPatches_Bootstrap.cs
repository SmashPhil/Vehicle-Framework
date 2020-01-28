using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Harmony;
using RimShips.Defs;

namespace RimShips
{

    public class HarmonyPatches_Bootstrap : Mod
    {
        public HarmonyPatches_Bootstrap(ModContentPack content) : base(content)
        {
            var harmony = HarmonyInstance.Create("rimworld.boats_bootstrap.smashphil");

            harmony.Patch(original: AccessTools.Property(type: typeof(RaceProperties), name: nameof(RaceProperties.IsFlesh)).GetGetMethod(),
                prefix: new HarmonyMethod(type: typeof(HarmonyPatches_Bootstrap),
                name: nameof(BoatsNotFlesh)));
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
