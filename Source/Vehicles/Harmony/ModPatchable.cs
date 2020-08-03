using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Vehicles
{
    public class ModPatchable
    {
        public string PackageId { get; set; }
        public string FriendlyName { get; set; }
        public bool Active { get; set; }
        public bool Patched { get; set; }
        public Exception ExceptionThrown { get; set; }

        public static ModPatchable GetModPatchable(string packageId)
        {
            ModPatchable mod = ConditionalPatchApplier.patchableModActivators.SingleOrDefault(key => key.Key.EqualsIgnoreCase(packageId)).Value;
            if (mod is null)
                Log.Error($"[Vehicles] Failed to retrieve [{packageId}] for patching.");
            return mod;
        }
    }
}
