using SmashTools;
using Verse;

namespace Vehicles;

internal static class DebugProperties
{
  #pragma warning disable CS0414
  // Enhanced debugging state which will enable many costly debugging features.
  internal static readonly bool Debug = false;

  internal static readonly bool DrawPaths = false;

  internal static readonly bool DrawAllRegions = false;

  private static readonly (string defName, DebugRegionType regionType) RegionDebugging =
    ("VF_TestMarshal", DebugRegionType.Regions | DebugRegionType.Links);
  #pragma warning restore CS0414

  internal static void Init()
  {
    // Debug settings cannot be allowed in release builds, as there is no way for
    // a user to unset them. Set everything to default as a fail safe, but we should
    // still verify it's not enabled.
#if RELEASE
    Trace.IsFalse(Debug);
    typeof(DebugProperties).SetStaticFieldsDefault();
#else
    if (!Debug)
    {
      typeof(DebugProperties).SetStaticFieldsDefault();
      return;
    }

    DebugHelper.Local.VehicleDef =
      DefDatabase<VehicleDef>.GetNamedSilentFail(RegionDebugging.defName);
    if (DebugHelper.Local.VehicleDef != null)
    {
      DebugHelper.Local.DebugType = RegionDebugging.regionType;
    }
#endif
  }
}