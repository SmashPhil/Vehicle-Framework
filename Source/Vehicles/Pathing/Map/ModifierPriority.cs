using JetBrains.Annotations;

namespace Vehicles;

/// <summary>
/// Priority constants for other modders to base their modifier priorities.
/// </summary>
[PublicAPI]
public static class ModifierPriority
{
  public const int Low = 1000;
  public const int High = 10;
  public const int First = 0;
}
