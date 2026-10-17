using System.Collections.Generic;
using JetBrains.Annotations;

namespace SmashTools;

/// <summary>
/// Maintains a mapping from event keys of type <typeparamref name="T"/> to their <see cref="EventTrigger"/> instances.
/// Allows enabling/disabling of all triggers and lazy initialization of triggers per key.
/// </summary>
/// <typeparam name="T">The type used as the event key.</typeparam>
[PublicAPI]
public class EventManager<T> : IEventControl
{
  /// <summary>
  /// Internal storage for event triggers keyed by <typeparamref name="T"/>.
  /// </summary>
  private readonly Dictionary<T, EventTrigger> map = [];

  /// <summary>
  /// Gets or sets whether all events in this manager are enabled. When <see langword="false"/>, no callbacks will execute.
  /// </summary>
  public bool Enabled { get; private set; } = true;

  /// <summary>
  /// Number of events registered
  /// </summary>
  public int Count => map.Count;

  /// <summary>
  /// Gets or sets whether all events in this manager are enabled. When <see langword="false"/>, no callbacks will execute.
  /// </summary>
  bool IEventControl.Enabled
  {
    get => Enabled;
    set => Enabled = value;
  }

  /// <summary>
  /// Gets or sets the <see cref="EventTrigger"/> for the specified key, creating a new one if none exists.
  /// </summary>
  /// <param name="key">The event key to retrieve or assign a trigger for.</param>
  /// <returns>The <see cref="EventTrigger"/> associated with <paramref name="key"/>.</returns>
  public EventTrigger this[T key]
  {
    get
    {
      if (!map.ContainsKey(key))
      {
        map[key] = new EventTrigger(this);
      }
      return map[key];
    }
    set { map[key] = value; }
  }
}