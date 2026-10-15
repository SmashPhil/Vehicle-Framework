using Verse;

namespace SmashTools;

/// <summary>
/// Assigns sequential indices
/// </summary>
/// <typeparam name="T">Def type used for grouping indices</typeparam>
public interface IDefIndex<T> where T : Def
{
  /// <summary>
  /// Gets or sets the def index for static caches.
  /// </summary>
  int DefIndex { get; set; }
};