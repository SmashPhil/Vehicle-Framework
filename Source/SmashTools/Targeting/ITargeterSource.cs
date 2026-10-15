using JetBrains.Annotations;

namespace SmashTools.Targeting;

/// <summary>
/// Defines a source that manages target selection and validation.
/// </summary>
/// <typeparam name="TTarget">The type of the target that the targeter operates on.</typeparam>
/// <typeparam name="TPayload">The type of the payload containing targeting options.</typeparam>
[PublicAPI]
public interface ITargeterSource<TTarget, in TPayload> where TPayload : ITargetOption
{
  /// <summary>
  /// The targeter is valid for use.
  /// </summary>
  /// <remarks>If <see langword="false"/> is returned while the targeter is active, it will be terminated.</remarks>
  bool TargeterValid { get; }

  /// <summary>
  /// Object can launch right now with its current configuration.
  /// </summary>
  TargetValidation CanTarget(TTarget target);

  /// <summary>
  /// Selects target
  /// </summary>
  /// <param name="target"></param>
  /// <returns>Targeter can close and dequeue from the update loop.</returns>
  TargeterResult Select(TTarget target);

  /// <summary>
  /// Target selection confirmed event.
  /// </summary>
  void OnTargetingFinished(TargetData<TTarget> targetData, TPayload payload);
}