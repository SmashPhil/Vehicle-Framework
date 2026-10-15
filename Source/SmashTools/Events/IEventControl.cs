namespace SmashTools;

/// <summary>
/// Provides control over the enabled state of an event manager or trigger.
/// </summary>
public interface IEventControl
{
  /// <summary>
  /// Gets or sets a value indicating whether events are permitted to execute.
  /// When <see langword="true"/>, event processing is allowed; when <see langword="false"/>,
  /// event execution should be suppressed.
  /// </summary>
  public bool Enabled { get; set; }
}