using JetBrains.Annotations;
using Verse;

namespace Vehicles;

/// <summary>
/// A vehicle-specific task for group colonist job behavior.
/// </summary>
[PublicAPI]
public interface IVehicleTask : IExposable
{
  /// <summary>
  /// The task has completed and the toil can transition to the next.
  /// </summary>
  bool ShouldEnd { get; }

  /// <summary>
  /// The task can no longer continue and the lord should fail.
  /// </summary>
  bool ShouldFail { get; }

  /// <summary>
  /// Gets the current report string for <paramref name="pawn"/> while the task is active.
  /// </summary>
  /// <param name="pawn">The pawn whom the report string is for.</param>
  /// <returns>A localized status string describing the task.</returns>
  string GetReportString(Pawn pawn);

  /// <summary>
  /// Advances the task by one tick.
  /// </summary>
  void TaskTick();

  /// <summary>
  /// Pre-task initialization.
  /// </summary>
  void StartTask();

  /// <summary>
  /// Post-task cleanup.
  /// </summary>
  void FinishTask();
}