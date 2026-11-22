using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace CoreLib.Performance;

[PublicAPI]
public readonly struct TimerBlock : IDisposable
{
  private readonly string label;
  private readonly Stopwatch stopwatch;

  public TimerBlock(string label)
  {
    this.label = label;
    stopwatch = Stopwatch.StartNew();
  }

  public void Dispose()
  {
    stopwatch.Stop();
    Logger.Message($"[{label}] {ToMilliseconds(stopwatch.ElapsedTicks):0.##}ms");
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static double ToMilliseconds(double ticks)
  {
    return ticks * 1000 / Stopwatch.Frequency;
  }
}
