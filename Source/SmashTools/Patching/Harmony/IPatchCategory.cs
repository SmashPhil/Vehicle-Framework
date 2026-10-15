using JetBrains.Annotations;

namespace SmashTools.Patching;

/// <summary>
/// Interface for declaring category of Patch methods. Used for organization purposes only.
/// </summary>
[PublicAPI, UsedWithReflection]
public interface IPatchCategory
{
  /// <summary>
  /// Doesn't make any real difference since Harmony has a lock on the internals of patching but
  /// it will at least be done inside the long event, leaving the game responsive.
  /// </summary>
  PatchSequence PatchAt { get; }

  /// <summary>
  /// Run all patches
  /// </summary>
  void PatchMethods();
}