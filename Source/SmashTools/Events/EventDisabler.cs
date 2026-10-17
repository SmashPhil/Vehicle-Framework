using System;
using JetBrains.Annotations;

namespace SmashTools;

/// <summary>
/// Temporarily disables events on the specified <see cref="IEventControl"/>.
/// When disposed, event dispatching is re-enabled.
/// </summary>
/// <typeparam name="T">The type of event keys managed by the event manager.</typeparam>
[PublicAPI]
public readonly struct EventDisabler<T> : IDisposable
{
  private readonly bool state;
  private readonly IEventControl eventControl;

  /// <summary>
  /// Initializes a new <see cref="EventDisabler{T}"/>, disabling all events or only the specified event keys.
  /// </summary>
  /// <param name="eventControl">The event control whose events will be disabled.</param>
  public EventDisabler(IEventControl eventControl)
  {
    this.eventControl = eventControl;
    state = eventControl.Enabled;
    eventControl.Enabled = false;
  }

  /// <summary>
  /// Restores the previous event-enabled state when the disabler goes out of scope.
  /// </summary>
  void IDisposable.Dispose()
  {
    eventControl.Enabled = state;
  }
}