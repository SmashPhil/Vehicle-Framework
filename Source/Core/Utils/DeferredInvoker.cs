using System;
using JetBrains.Annotations;
using CoreLib.Performance;
using UnityEngine;

namespace CoreLib;

[PublicAPI]
public sealed class DeferredInvoker
{
  private readonly Settings settings;
  private readonly TimerImpl timer;

  public struct Settings()
  {
    public int timeoutMs = -1;
    public bool invokeOnExpiry = false;
  }

  /// <summary>
  /// Initializes a new <see cref="DeferredInvoker"/> action.
  /// </summary>
  /// <param name="action">The action to execute after <paramref name="condition"/> is met.</param>
  /// <param name="condition">Defers invoking the action until true.</param>
  /// <param name="settings">Configurable settings for the invoker.</param>
  /// <exception cref="ArgumentNullException"/>
  public DeferredInvoker(Action action, Func<bool> condition, in Settings settings)
  {
    if (action == null)
      throw new ArgumentNullException(nameof(action));
    if (condition == null)
      throw new ArgumentNullException(nameof(condition));

    this.settings = settings;

    timer = new TimerImpl(action, condition, settings);
    timer.EnsureScheduled();
  }

  /// <summary>
  /// Remaining time left before the timer expires and the action is invoked.
  /// </summary>
  public float TimeRemaining => timer.TimeLeft;

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
  private class TimerImpl(Action action, Func<bool> condition, Settings settings)
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
      timeLeft = settings.timeoutMs / 1000f;
      UnityThread.StartUpdate(Update);
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
    /// Advances the timer and triggers the action if expired or condition is met.
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
        Cancel();
        if (settings.invokeOnExpiry)
        {
          action();
        }

        return false;
      }

      if (condition())
      {
        Cancel();
        action();
        return false;
      }

      return true;
    }
  }
}