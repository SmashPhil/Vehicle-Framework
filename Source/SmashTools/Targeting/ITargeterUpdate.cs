using CoreLib.Collections;

namespace SmashTools.Targeting;

/// <summary>
/// Target updater for per-frame validation and OnGUI drawing.
/// </summary>
/// <typeparam name="T">The target type.</typeparam>
public interface ITargeterUpdate<T>
{
  /// <summary>
  /// <see cref="TargeterDispatcher.TargeterOnGUI"/> hook for UI rendering while the targeter is active.
  /// </summary>
  void TargeterOnGUI();

  // TODO 1.8 - Change TargetData to ReadOnlyList<T>
  /// <summary>
  /// <see cref="TargeterDispatcher.TargeterUpdate" /> hook for per-frame logic while the targeter is active.
  /// </summary>
  /// <param name="targetData"></param>
  void TargeterUpdate(ref readonly TargetData<T> targetData);

  /// <summary>
  /// Per-frame validation on the current list of targets.
  /// </summary>
  /// <remarks>
  /// This is invoked directly after <see cref="TargeterUpdate"/>.
  /// </remarks>
  /// <param name="targets">Read-only list of confirmed targets.</param>
  /// <returns>TargetValidation result with tooltip to display.</returns>
  TargetValidation ValidateTargets(ReadOnlyList<T> targets);
}