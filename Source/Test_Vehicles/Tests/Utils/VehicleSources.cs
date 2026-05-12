using JetBrains.Annotations;
using Verse;

namespace Vehicles.Testing;

internal static class VehicleSources
{
  [UsedWithReflection]
  public static readonly IntVec2[] GridSizes = [
    // Tall sizes from 1 to 5
    new(1, 1), new(1, 2), new(1, 3), new(1, 4), new(1, 5),
    new(2, 2), new(2, 3), new(2, 4), new(2, 5),
    new(3, 3), new(3, 4), new(3, 5),
    new(4, 4), new(4, 5),
    new(5, 5),
    // Wider than tall
    new(3, 2), new(4, 2), new(4, 3), new(5, 3),
  ];
}
