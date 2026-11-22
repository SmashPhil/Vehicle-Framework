using System;
using JetBrains.Annotations;

namespace CoreLib.Collections.Concurrent;

[PublicAPI]
public sealed class DoubleBuffer<T> : IDisposable
{
  private T a;
  private T b;

  private bool front;

  public DoubleBuffer([NotNull] T a, [NotNull] T b)
  {
    this.a = a;
    this.b = b;
  }

  /// <summary>
  /// Get current read target without swapping
  /// </summary>
  public T Read => front ? a : b;

  /// <summary>
  /// Get the current write target without swapping
  /// </summary>
  public T Write => !front ? a : b;

  public void Dispose()
  {
    (a as IDisposable)?.Dispose();
    (b as IDisposable)?.Dispose();
  }

  /// <summary>
  /// Swap buffers
  /// </summary>
  public void Swap()
  {
    front = !front;
  }
}
