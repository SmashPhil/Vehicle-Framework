using Verse;

namespace SmashTools;

/// <summary>
/// Helper methods for querying active mods by their package identifier since Ludeon's implementation of resolving
/// package ids is very fragile. This is a local implementation that can be updated easily if further changes are made.
/// </summary>
public static class Ext_Mods
{
  /// <summary>
  /// Retrieves the metadata for the active mod with the given package identifier.
  /// </summary>
  /// <remarks><paramref name="packageId"/> ignores postfix in the active mod list.</remarks>
  /// <param name="packageId">
  /// The unique identifier of the mod package to look up (e.g. "SmashPhil.VehicleFramework").
  /// </param>
  /// <returns>
  /// The <see cref="ModMetaData"/> for the active mod if found; otherwise, <see langword="null"/>
  /// </returns>
  public static ModMetaData GetActiveMod(string packageId)
  {
    return ModLister.GetActiveModWithIdentifier(packageId, ignorePostfix: true);
  }

  /// <summary>
  /// Checks whether a mod with the specified package id is currently active.
  /// </summary>
  /// <remarks><paramref name="packageId"/> ignores postfix in the active mod list.</remarks>
  /// <param name="packageId">
  /// The unique id of the mod to check.
  /// </param>
  /// <returns>
  /// <see langword="true"/> if an active mod matches that identifier; otherwise, <see langword="false"/>.
  /// </returns>
  public static bool HasActiveMod(string packageId)
  {
    return GetActiveMod(packageId) != null;
  }
}