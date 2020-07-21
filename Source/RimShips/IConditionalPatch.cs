using HarmonyLib;

namespace Vehicles
{
    public interface IConditionalPatch
    {
        void PatchAll(ModPatchable mod, Harmony instance);

        string PackageId { get; }
    }
}
