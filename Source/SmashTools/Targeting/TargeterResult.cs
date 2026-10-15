using System.Collections.Generic;
using JetBrains.Annotations;

namespace SmashTools.Targeting;

/// <summary>
/// The result of a targeting source, including the action to take and any associated options.
/// </summary>
[PublicAPI]
public struct TargeterResult
{
  /// <summary>
  /// Targeter result
  /// </summary>
  public required TargeterAction action;

  /// <summary>
  /// A list of available target options.
  /// </summary>
  public List<ITargetOption> options;

  /// <summary>
  /// No action taken.
  /// </summary>
  public static TargeterResult None => new() { action = TargeterAction.None };

  /// <summary>
  /// Target was rejected.
  /// </summary>
  public static TargeterResult Reject => new() { action = TargeterAction.Reject };

  /// <summary>
  /// Cancel targeter
  /// </summary>
  /// <remarks>
  /// Returning this from <see cref="ITargeterSource{TTarget,TPayload}.Select"/> will stop the active targeter.
  /// </remarks>
  public static TargeterResult Cancel => new() { action = TargeterAction.Cancel };

  /// <summary>
  /// Accept the current action and stops the active targeter.
  /// </summary>
  public static TargeterResult Submit => new() { action = TargeterAction.Submit };

  /// <summary>
  /// Accept the current action and continue targeting.
  /// </summary>
  /// <remarks>
  /// The <paramref name="options"/> will only be shown after the user selects the target a 2nd time.
  /// </remarks>
  /// <typeparam name="T">Target option type.</typeparam>
  /// <param name="options">List of options to show the user if targeter is finalized.</param>
  public static TargeterResult Accept<T>(List<T> options) where T : ITargetOption
  {
    return new TargeterResult { action = TargeterAction.Accept, options = [.. options] };
  }
}