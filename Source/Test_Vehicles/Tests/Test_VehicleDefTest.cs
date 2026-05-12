using System.Collections.Generic;
using DevTools.Testing;
using Verse;

namespace Vehicles.Testing;

internal class Test_VehicleDefTest
{
  protected readonly List<VehicleDef> vehicleDefs = [];

  protected virtual bool ShouldTest(VehicleDef vehicleDef)
  {
    return true;
  }

  [OneTimeSetUp]
  protected void GenerateVehicles()
  {
    foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
    {
      if (!ShouldTest(vehicleDef))
        continue;
      vehicleDefs.Add(vehicleDef);
    }
  }
}