using System;
using JetBrains.Annotations;
using UnityEngine;

namespace CoreLib.Performance;

/// <summary>
/// Schedules an action to be executed after a specified delay, resetting the delay if invoked again before expiration.
/// </summary>
[PublicAPI]
public sealed class Debouncer
{
  private readonly float timeDelay; // seconds
  private readonly TimerImpl timer;

  /// <summary>
  /// Initializes a new <see cref="Debouncer"/> action.
  /// </summary>
  /// <remarks>
  /// Does not immediately start the timer. <see cref="Invoke"/> must be called at least once to trigger the debouncer.
  /// </remarks>
  /// <param name="action">The action to execute after the delay.</param>
  /// <param name="milliseconds">The delay duration in milliseconds.</param>
  /// <exception cref="ArgumentNullException"/>
  public Debouncer(Action action, int milliseconds)
  {
    if (action == null)
      throw new ArgumentNullException(nameof(action));

    timeDelay = milliseconds / 1000f;

    timer = new TimerImpl(action);
    timer.Reset(timeDelay);
  }

  /// <summary>
  /// Remaining time left before the timer expires and the action is invoked.
  /// </summary>
  public float TimeRemaining => timer.TimeLeft;

  /// <summary>
  /// Resets the debounce timer.
  /// </summary>
  public void Invoke()
  {
    timer.Reset(timeDelay);
    timer.EnsureScheduled();
  }

  /// <summary>
  /// Cancels the scheduled action if it hasn't executed yet.
  /// </summary>
  public void Cancel()
  {
    timer.Cancel();
  }

  /// <summary>
  /// Internal timer that tracks elapsed time and executes the action once expired.
  /// </summary>
  private class TimerImpl(Action action)
  {
    private bool active;
    private float timeLeft;

    /// <summary>
    /// Time remaining before the timer expires.
    /// </summary>
    public float TimeLeft => timeLeft;

    /// <summary>
    /// Indicates whether the timer has expired.
    /// </summary>
    private bool Expired => !active || timeLeft <= 0;

    /// <summary>
    /// Ensures the update loop is registered.
    /// </summary>
    public void EnsureScheduled()
    {
      if (active)
        return;

      active = true;
      UnityThread.StartUpdate(Update);
    }

    /// <summary>
    /// Resets the internal timer to the specified delay.
    /// </summary>
    /// <param name="timeDelay">The new delay in seconds.</param>
    public void Reset(float timeDelay)
    {
      timeLeft = timeDelay;
    }

    /// <summary>
    /// Cancels the timer and removes it from scheduling if active.
    /// </summary>
    /// <remarks>Safe to call repeatedly.</remarks>
    public void Cancel()
    {
      active = false;
      timeLeft = 0;
    }

    /// <summary>
    /// Advances the timer and triggers the action if expired.
    /// </summary>
    /// <remarks>Enqueued as <see cref="UnityThread.OnUpdate"/> and called as part of the <see cref="UnityThread.Update"/> loop.</remarks>
    /// <returns><see langword="true"/> if this update method should remain scheduled, otherwise dequeue from <see cref="UnityThread"/>.</returns>
    private bool Update()
    {
      if (!active)
        return false;

      timeLeft -= Time.deltaTime;
      if (Expired)
      {
        timeLeft = 0;
        active = false;
        action();
        return false;
      }
      return true;
    }
  }
}