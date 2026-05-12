using JetBrains.Annotations;

namespace Vehicles;

/// <summary>
/// Represents a source of grid updates that can temporarily batch index changes while a map is
/// being generated or otherwise updated in bulk.
/// </summary>
[PublicAPI]
public interface IGridDebouncerSource
{
  /// <summary>
  /// Gets the active debouncer, or null if none has been initialized.
  /// </summary>
  GridDebouncer ActiveDebouncer { set; }

  /// <summary>
  /// Executes the update for the grid index.
  /// </summary>
  /// <param name="index">The grid index to update.</param>
  void Execute(int index);
}