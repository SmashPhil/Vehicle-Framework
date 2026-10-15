using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace SmashTools;

/// <summary>
/// Provides extension methods for <see cref="IEventManager{T}"/> to simplify
/// populating and managing event registrations.
/// </summary>
[PublicAPI]
public static class Ext_EventManager
{
  /// <summary>
  /// Clears the existing registry and registers the specified events.
  /// </summary>
  /// <typeparam name="T">The event key type.</typeparam>
  /// <param name="manager">The event manager to fill.</param>
  /// <param name="events">The sequence of events to register.</param>
  public static void FillEvents<T>(this IEventManager<T> manager, IEnumerable<T> events)
  {
    manager.EventRegistry = new EventManager<T>();
    foreach (T value in events)
    {
      manager.RegisterEventType(value);
    }
  }

  /// <summary>
  /// Clears the existing registry and registers all values of the <typeparamref name="T"/> enum type.
  /// </summary>
  /// <typeparam name="T">The enum event key type.</typeparam>
  /// <param name="manager">The event manager to fill.</param>
  /// <exception cref="ArgumentException">If <typeparamref name="T"/> is not an enum type.</exception>
  public static void FillEventsEnum<T>(this IEventManager<T> manager)
  {
    if (!typeof(T).IsEnum)
      throw new ArgumentException(
        $"Tried to fill IEventManager with enum values and non-enum type. Type=\"{typeof(T)}\" Manager=\"{manager}\"");

    manager.EventRegistry = new EventManager<T>();
    foreach (T value in Enum.GetValues(typeof(T)))
    {
      manager.RegisterEventType(value);
    }
  }

  /// <summary>
  /// Clears the existing registry and registers all definitions of type <typeparamref name="T"/> from the DefDatabase.
  /// </summary>
  /// <typeparam name="T">The definition type, derived from <see cref="Def"/>.</typeparam>
  /// <param name="manager">The event manager to fill.</param>
  public static void FillEventsDef<T>(this IEventManager<T> manager) where T : Def
  {
    manager.EventRegistry = new EventManager<T>();
    foreach (T def in DefDatabase<T>.AllDefsListForReading)
    {
      manager.RegisterEventType(def);
    }
  }

  /// <summary>
  /// Registers a new event key in the manager's registry.
  /// </summary>
  /// <typeparam name="T">The event key type.</typeparam>
  /// <param name="manager">The event manager for registration.</param>
  /// <param name="event">The event key to register.</param>
  public static void RegisterEventType<T>(this IEventManager<T> manager, T @event)
  {
    manager.EventRegistry[@event] = new EventTrigger(manager.EventRegistry);
  }

  /// <summary>
  /// Adds one or more actions to be invoked when the specified event fires.
  /// </summary>
  /// <typeparam name="T">The event key type.</typeparam>
  /// <param name="manager">The event manager.</param>
  /// <param name="event">The event key.</param>
  /// <param name="action">The first action to add.</param>
  /// <param name="actions">Additional actions to add.</param>
  public static void AddEvent<T>(this IEventManager<T> manager, T @event, Action action,
    params Action[] actions)
  {
    manager.EventRegistry[@event].Add(null, action);
    if (!actions.NullOrEmpty())
    {
      foreach (Action additionalAction in actions)
      {
        manager.EventRegistry[@event].Add(null, additionalAction);
      }
    }
  }

  /// <summary>
  /// Adds one or more actions under the specified key to be invoked when the event fires.
  /// </summary>
  /// <typeparam name="T">The event key type.</typeparam>
  /// <param name="manager">The event manager.</param>
  /// <param name="event">The event key.</param>
  /// <param name="action">The first action to add.</param>
  /// <param name="key">The grouping key for the actions.</param>
  /// <param name="actions">Additional actions to add.</param>
  public static void AddEvent<T>(this IEventManager<T> manager, T @event, Action action,
    string key, params Action[] actions)
  {
    manager.EventRegistry[@event].Add(key, action);
    if (!actions.NullOrEmpty())
    {
      foreach (Action additionalAction in actions)
      {
        manager.EventRegistry[@event].Add(key, additionalAction);
      }
    }
  }

  /// <summary>
  /// Adds a single-use action to the specified event.
  /// </summary>
  /// <typeparam name="T">The event key type.</typeparam>
  /// <param name="manager">The event manager.</param>
  /// <param name="event">The event key.</param>
  /// <param name="action">The action to add.</param>
  /// <param name="actions">Additional single-use actions to add.</param>
  public static void AddSingleEvent<T>(this IEventManager<T> manager, T @event, Action action,
    params Action[] actions)
  {
    manager.EventRegistry[@event].AddSingle(null, action);
    if (!actions.NullOrEmpty())
    {
      foreach (Action additionalAction in actions)
      {
        manager.EventRegistry[@event].AddSingle(null, additionalAction);
      }
    }
  }

  /// <summary>
  /// Adds single-use actions under the specified key to the event.
  /// </summary>
  /// <typeparam name="T">The event key type.</typeparam>
  /// <param name="manager">The event manager.</param>
  /// <param name="event">The event key.</param>
  /// <param name="action">The action to add.</param>
  /// <param name="key">The grouping key for the single-use actions.</param>
  /// <param name="actions">Additional single-use actions to add.</param>
  public static void AddSingleEvent<T>(this IEventManager<T> manager, T @event, Action action,
    string key, params Action[] actions)
  {
    manager.EventRegistry[@event].AddSingle(key, action);
    if (!actions.NullOrEmpty())
    {
      foreach (Action additionalAction in actions)
      {
        manager.EventRegistry[@event].AddSingle(key, additionalAction);
      }
    }
  }

  /// <summary>
  /// Removes all actions under the specified key from the event.
  /// </summary>
  /// <typeparam name="T">The event key type.</typeparam>
  /// <param name="manager">The event manager.</param>
  /// <param name="event">The event key.</param>
  /// <param name="key">The key grouping the actions to remove.</param>
  public static void RemoveEvent<T>(this IEventManager<T> manager, T @event, string key)
  {
    manager.EventRegistry[@event].Remove(key);
  }

  /// <summary>
  /// Removes the specified action from the event.
  /// </summary>
  /// <typeparam name="T">The event key type.</typeparam>
  /// <param name="manager">The event manager.</param>
  /// <param name="event">The event key.</param>
  /// <param name="action">The action to remove.</param>
  public static void RemoveEvent<T>(this IEventManager<T> manager, T @event, Action action)
  {
    manager.EventRegistry[@event].Remove(action);
  }

  /// <summary>
  /// Removes a single-use action under the specified key from the event.
  /// </summary>
  /// <typeparam name="T">The event key type.</typeparam>
  /// <param name="manager">The event manager.</param>
  /// <param name="event">The event key.</param>
  /// <param name="key">The key grouping the single-use action to remove.</param>
  public static void RemoveSingleEvent<T>(this IEventManager<T> manager, T @event, string key)
  {
    manager.EventRegistry[@event].RemoveSingle(key);
  }

  /// <summary>
  /// Removes the single-use action from the event.
  /// </summary>
  /// <typeparam name="T">The event key type.</typeparam>
  /// <param name="manager">The event manager.</param>
  /// <param name="event">The event key.</param>
  /// <param name="action">The single-use action to remove.</param>
  public static void RemoveSingleEvent<T>(this IEventManager<T> manager, T @event, Action action)
  {
    manager.EventRegistry[@event].RemoveSingle(action);
  }

  /// <summary>
  /// Clears all registered callbacks for the specified event.
  /// </summary>
  /// <typeparam name="T">The event key type.</typeparam>
  /// <param name="manager">The event manager.</param>
  /// <param name="event">The event key.</param>
  public static void ClearAll<T>(this IEventManager<T> manager, T @event)
  {
    manager.EventRegistry[@event].ClearAll();
  }

  /// <summary>
  /// Determines whether the <see cref="EventManager{T}"/> instance has been initialized
  /// and contains any registered events.
  /// </summary>
  /// <typeparam name="T">The event key type.</typeparam>
  /// <param name="manager">The event manager instance.</param>
  /// <returns><see langword="true"/> if the manager is non-null and has one or more events; otherwise, <c>false</c>.</returns>
  public static bool Initialized<T>(this EventManager<T> manager)
  {
    return manager is { Count: > 0 };
  }
}