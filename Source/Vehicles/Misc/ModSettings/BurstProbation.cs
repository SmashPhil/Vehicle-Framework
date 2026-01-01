using System;

namespace Vehicles.Config;

/// <summary>
/// Provides a scope-based mechanism to enable and disable burst probation mode for external burst lib.
/// </summary>
/// <remarks>
/// Used for handling crashes while running test job immediately after loading burst lib. If program crashes,
/// probation will be set and burst loading will be disabled next startup.
/// </remarks>
internal struct BurstProbation : IDisposable
{
  public BurstProbation()
  {
    VehicleMod.settings.main.burstProbation = true;
    VehicleMod.settings.Write();
  }

  void IDisposable.Dispose()
  {
    VehicleMod.settings.main.burstProbation = false;
    VehicleMod.settings.Write();
  }
}
