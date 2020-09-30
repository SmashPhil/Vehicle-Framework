using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Vehicles
{
    [StaticConstructorOnStartup]
    public static class ConditionalPatchApplier
    {
        static ConditionalPatchApplier()
        {
            var harmony = new Harmony("conditional_patches.rimworld.smashphil");

            PatchAllActiveMods(harmony);
        }

        private static void PatchAllActiveMods(Harmony harmony)
        {
            IEnumerable<ModMetaData> mods = ModLister.AllInstalledMods.Where(m => m.Active);
            IEnumerable<Type> interfaceImplementations = GenTypes.AllTypes.Where(t => t.GetInterfaces().Contains(typeof(IConditionalPatch)));
            foreach(ModMetaData mod in mods)
            {
                if(mod.PackageId.EqualsIgnoreCase("smashphil.vehicles"))
                {
                    VehicleMMD = mod;
                }
                foreach(Type type in interfaceImplementations)
                {
                    IConditionalPatch patch = (IConditionalPatch)Activator.CreateInstance(type, null);
                    if(mod.PackageId.EqualsIgnoreCase(patch.PackageId))
                    {
                        ModPatchable newMod = new ModPatchable()
                        {
                            PackageId = mod.PackageId,
                            FriendlyName = mod.Name,
                            Active = true,
                            Patched = true
                        };
                        
                        patch.PatchAll(newMod, harmony);

                        Log.Message($"{VehicleHarmony.LogLabel} Successfully applied compatibility patches to: {mod.Name}");
                        patchableModActivators.Add(mod.PackageId, newMod);
                    }
                }
            }
        }

        public static Dictionary<string, ModPatchable> patchableModActivators = new Dictionary<string, ModPatchable>();

        internal static ModMetaData VehicleMMD;

        internal const string ExtraTexturesFolder = "Color";
    }
}
