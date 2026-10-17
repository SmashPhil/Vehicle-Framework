using System;

namespace SmashTools;

/// <summary>
/// Subscribes to an event on an <see cref="IEventManager{T}"/> for the specified key and tracks how many times that
/// event has been raised.
/// </summary>
/// <typeparam name="T">
/// The type used as the event key in the <see cref="IEventManager{T}"/>.
/// </typeparam>
// NOTE - This must be a reference type. Structs will create a copy for the closure when registering EventRaised in the
// event manager.
public class EventListener<T> : IDisposable
{
  private readonly IEventManager<T> eventManager;
  private readonly T key;

  private int eventsRaised;

  /// <summary>
  /// Creates a new <see cref="EventListener{T}"/>, subscribes to the event identified by <paramref name="key"/>,
  /// and begins counting events raised.
  /// </summary>
  /// <param name="eventManager">
  /// The event manager that holds the registry of events to listen on.
  /// </param>
  /// <param name="key">
  /// The key identifying the specific event within the event manager’s registry.
  /// </param>
  public EventListener(IEventManager<T> eventManager, T key)
  {
    this.eventManager = eventManager;
    this.key = key;

    eventsRaised = 0;
    this.eventManager.AddEvent(key, EventRaised);
  }

  /// <summary>
  /// Gets the number of times the target event has been raised since this listener was created.
  /// </summary>
  public int CountRaised => eventsRaised;

  /// <summary>
  /// Unsubscribes from the event, no longer receiving notifications or incrementing its raise count.
  /// </summary>
  public void Dispose()
  {
    eventManager?.RemoveEvent(key, EventRaised);
  }

  /// <summary>
  /// Internal handler invoked by the event manager; increments the invocation counter.
  /// </summary>
  private void EventRaised()
  {
    eventsRaised++;
  }
}