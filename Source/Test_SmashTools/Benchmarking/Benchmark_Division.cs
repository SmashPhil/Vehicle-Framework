using DevTools.Benchmarking;

namespace SmashTools.Benchmarking;

[BenchmarkClass("Division")]
internal class Benchmark_Division
{
  public int numerator = 14352;
  public int divisor = 15;
  public ulong factor;

  [Prepare]
  public void CalcFactor()
  {
    factor = (1UL << 32) / (ulong)divisor;
  }

  [Benchmark(Label = "Divisor")]
  public int Divisor()
  {
    return numerator / divisor;
  }

  [Benchmark(Label = "BitShift")]
  public int BitShift()
  {
    ulong prod = (ulong)numerator * factor;
    return (int)((prod + (1UL << 31)) >> 32);
  }
}