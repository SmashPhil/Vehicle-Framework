namespace Vehicles.Tests;

internal static class Warnings
{
  public const string AncientVentPlacement =
    @"^TileMutatorWorker_AncientVent failed to find any valid cells to place at least one vent\. Placed \d+ vents, wanted to place \d+\.$";

  public const string GenStepFrozenRuins =
    @"^GenStep_FrozenRuins did not find any valid rects to generate ruins in for bounds \(-?\d+,-?\d+,-?\d+,-?\d+\)\.$";

}
