using System;
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
  /// Internal map for tracking which event triggers a parent has, keyed by the object instance.
  /// </summary>
  private readonly Dictionary<object, List<EventData>> eventOwners = [];

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

  /// <summary>
  /// Adds persistent callbacks for an event and tracks them under the owning instance.
  /// </summary>
  /// <param name="instance">The instance used to remove these callbacks later.</param>
  /// <param name="event">The event key whose trigger receives the callbacks.</param>
  /// <param name="action">The first callback to add.</param>
  /// <param name="actions">Additional callbacks to add.</param>
  public void AddEvent(object instance, T @event, Action action, params Action[] actions)
  {
    this[@event].Add(instance, key: null, action);
    eventOwners.AddOrAppend(instance, new EventData(@event, action));
    if (!actions.NullOrEmpty())
    {
      foreach (Action additionalAction in actions)
      {
        this[@event].Add(instance, key: null, additionalAction);
        eventOwners.AddOrAppend(instance, new EventData(@event, additionalAction));
      }
    }
  }

  /// <summary>
  /// Adds persistent callbacks under a grouping key and tracks them under the owning instance.
  /// </summary>
  /// <param name="instance">The instance used to remove these callbacks later.</param>
  /// <param name="event">The event key whose trigger receives the callbacks.</param>
  /// <param name="action">The first callback to add.</param>
  /// <param name="key">The grouping key for the callbacks.</param>
  /// <param name="actions">Additional callbacks to add.</param>
  public void AddEvent(object instance, T @event, Action action,
    string key, params Action[] actions)
  {
    this[@event].Add(instance, key, action);
    eventOwners.AddOrAppend(instance, new EventData(@event, action));
    if (!actions.NullOrEmpty())
    {
      foreach (Action additionalAction in actions)
      {
        this[@event].Add(instance, key, additionalAction);
        eventOwners.AddOrAppend(instance, new EventData(@event, additionalAction));
      }
    }
  }

  /// <summary>
  /// Removes persistent callbacks tracked under the specified instance across all event keys.
  /// </summary>
  /// <param name="instance">The instance whose callbacks to remove.</param>
  /// <returns><see langword="true"/> if the instance has tracked registrations; otherwise, <see langword="false"/>.</returns>
  public bool RemoveAllEvents(object instance)
  {
    if (!eventOwners.TryGetValue(instance, out List<EventData> events))
      return false;

    foreach (EventData data in events)
    {
      this[data.@event].Remove(instance, data.action);
    }
    return true;
  }

  private readonly struct EventData(T @event, Action action)
  {
    public readonly T @event = @event;
    public readonly Action action = action;
  }
}