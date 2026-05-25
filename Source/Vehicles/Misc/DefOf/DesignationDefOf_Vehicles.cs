using RimWorld;
using Verse;

namespace Vehicles;

// ReSharper disable InconsistentNaming
[DefOf]
public static class DesignationDefOf_Vehicles
{
  public static DesignationDef DisassembleVehicle;

  static DesignationDefOf_Vehicles()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(DesignationDefOf_Vehicles));
  }
}