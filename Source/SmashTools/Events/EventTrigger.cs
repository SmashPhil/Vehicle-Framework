using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace SmashTools;

/// <summary>
/// Manages persistent and single-use <see cref="Action"/> callbacks for a given event key.
/// </summary>
[PublicAPI]
public class EventTrigger : IEventControl
{
  private readonly List<Trigger> persistents = [];
  private readonly List<Trigger> singles = [];

  private readonly IEventControl manager;

  public EventTrigger(IEventControl manager)
  {
    this.manager = manager;
  }

  /// <summary>
  /// Gets or sets whether event execution is enabled. When <see langword="false"/>, <see cref="ExecuteEvents"/> will do nothing.
  /// </summary>
  public bool Enabled { get; private set; } = true;

  /// <summary>
  /// Gets or sets whether event execution is enabled. When <see langword="false"/>, <see cref="ExecuteEvents"/> will do nothing.
  /// </summary>
  bool IEventControl.Enabled
  {
    get => Enabled;
    set => Enabled = value;
  }

  /// <summary>
  /// Gets the total number of registered triggers (persistent + single-use).
  /// </summary>
  public int TotalEventCount => singles.Count + persistents.Count;

  /// <summary>
  /// Determines whether any persistent trigger exists with the specified key.
  /// </summary>
  /// <param name="key">The grouping key to search for.</param>
  /// <returns><see langword="true"/> if a persistent trigger with the given key is registered; otherwise, <see langword="false"/>.</returns>
  public bool Contains(string key)
  {
    foreach (Trigger trigger in persistents)
    {
      if (key != null && trigger.key != null && key == trigger.key)
        return true;
    }
    return false;
  }

  /// <summary>
  /// Determines whether any persistent trigger exists for the specified action.
  /// </summary>
  /// <param name="action">The callback delegate to search for.</param>
  /// <returns><see langword="true"/> if the action is registered persistently; otherwise, <see langword="false"/>.</returns>
  public bool Contains(Action action)
  {
    foreach (Trigger trigger in persistents)
    {
      if (trigger.action == action)
        return true;
    }
    return false;
  }

  /// <summary>
  /// Adds a persistent callback under the given key.
  /// </summary>
  /// <param name="key">A grouping key for the callback.</param>
  /// <param name="action">The callback to invoke on execution.</param>
  public void Add(string key, Action action)
  {
    persistents.Add(new Trigger(key, action));
  }

  /// <summary>
  /// Adds a single-use callback under the given key. It will be removed after its first execution.
  /// </summary>
  /// <param name="key">A grouping key for the callback.</param>
  /// <param name="action">The callback to invoke once on execution.</param>
  public void AddSingle(string key, Action action)
  {
    singles.Add(new Trigger(key, action));
  }

  /// <summary>
  /// Removes all persistent callbacks matching the specified key.
  /// </summary>
  /// <param name="key">The grouping key whose callbacks will be removed.</param>
  /// <returns>The number of callbacks removed.</returns>
  public int Remove(string key)
  {
    int count = 0;
    for (int i = persistents.Count - 1; i >= 0; i--)
    {
      Trigger trigger = persistents[i];
      if (trigger.key == key)
      {
        persistents.RemoveAt(i);
        count++;
      }
    }

    return count;
  }

  /// <summary>
  /// Removes all persistent callbacks matching the specified action.
  /// </summary>
  /// <param name="action">The callback to remove.</param>
  /// <returns>The number of callbacks removed.</returns>
  public int Remove(Action action)
  {
    int count = 0;
    for (int i = persistents.Count - 1; i >= 0; i--)
    {
      Trigger trigger = persistents[i];
      if (trigger.action == action)
      {
        persistents.RemoveAt(i);
        count++;
      }
    }
    return count;
  }

  /// <summary>
  /// Removes all single-use callbacks matching the specified key.
  /// </summary>
  /// <param name="key">The grouping key whose single-use callbacks will be removed.</param>
  /// <returns>The number of callbacks removed.</returns>
  public int RemoveSingle(string key)
  {
    int count = 0;
    for (int i = singles.Count - 1; i >= 0; i--)
    {
      Trigger trigger = singles[i];
      if (trigger.key == key)
      {
        singles.RemoveAt(i);
        count++;
      }
    }
    return count;
  }

  /// <summary>
  /// Removes all single-use callbacks matching the specified action.
  /// </summary>
  /// <param name="action">The callback to remove.</param>
  /// <returns>The number of callbacks removed.</returns>
  public int RemoveSingle(Action action)
  {
    int count = 0;
    for (int i = singles.Count - 1; i >= 0; i--)
    {
      Trigger trigger = singles[i];
      if (trigger.action == action)
      {
        singles.RemoveAt(i);
        count++;
      }
    }

    return count;
  }

  /// <summary>
  /// Clears all registered persistent and single-use callbacks.
  /// </summary>
  public void ClearAll()
  {
    singles.Clear();
    persistents.Clear();
  }

  /// <summary>
  /// Executes all registered callbacks.  Persistent callbacks run every time; single-use callbacks
  /// run once and are then removed.  If <see cref="IEventControl.Enabled"/> is <see langword="false"/>, this method does nothing.
  /// </summary>
  public void ExecuteEvents()
  {
    if (!Enabled || !manager.Enabled)
      return;

    foreach (Trigger trigger in persistents)
    {
      trigger.action();
    }

    for (int i = singles.Count - 1; i >= 0; i--)
    {
      Trigger trigger = singles[i];
      trigger.action();
      singles.RemoveAt(i);
    }
  }

  /// <summary>
  /// Internal struct representing a single registered callback and its optional grouping key.
  /// </summary>
  private readonly struct Trigger(string key, Action action)
  {
    /// <summary>The grouping key for this callback; may be <see langword="null"/>.</summary>
    public readonly string key = key;

    /// <summary>The callback delegate to invoke.</summary>
    public readonly Action action = action;
  }
}