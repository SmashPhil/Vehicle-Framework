using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using HarmonyLib;
using Vehicles.Defs;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace Vehicles
{

    public class HarmonyPatches_PreLoad : Mod
    {
        public HarmonyPatches_PreLoad(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("rimworld.vehicles_preload.smashphil");

            harmony.Patch(original: AccessTools.Property(type: typeof(RaceProperties), name: nameof(RaceProperties.IsFlesh)).GetGetMethod(),
                prefix: new HarmonyMethod(typeof(HarmonyPatches_PreLoad),
                nameof(BoatsNotFlesh)));
        }

        public static bool BoatsNotFlesh(ref bool __result, RaceProperties __instance)
        {
            if (__instance.FleshType == FleshTypeDefOf_Ships.MetalVehicle || __instance.FleshType == FleshTypeDefOf_Ships.SpacerVehicle || __instance.FleshType == FleshTypeDefOf_Ships.WoodenVehicle)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
