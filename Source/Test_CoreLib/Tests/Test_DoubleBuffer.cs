using System;
using CoreLib.Collections.Concurrent;
using DevTools.Testing;

namespace CoreLib.Tests;

[TestFixture(TestType.MainMenu)]
[TestCategory(TestCategoryNames.Collections)]
[TestDescription("Switches read and write targets between two buffers.")]
internal class Test_DoubleBuffer
{
  [Test]
  private void InitialReadReturnsSecondBuffer()
  {
    DoubleBuffer<int> buffer = new(10, 20);

    Expect.AreEqual(20, buffer.Read);
  }

  [Test]
  private void InitialWriteReturnsFirstBuffer()
  {
    DoubleBuffer<int> buffer = new(10, 20);

    Expect.AreEqual(10, buffer.Write);
  }

  [Test]
  private void SwapMakesFirstBufferReadable()
  {
    DoubleBuffer<int> buffer = new(10, 20);

    buffer.Swap();

    Expect.AreEqual(10, buffer.Read);
  }

  [Test]
  private void SwapMakesSecondBufferWritable()
  {
    DoubleBuffer<int> buffer = new(10, 20);

    buffer.Swap();

    Expect.AreEqual(20, buffer.Write);
  }

  [Test]
  private void TwoSwapsRestoreInitialReadBuffer()
  {
    DoubleBuffer<int> buffer = new(10, 20);

    buffer.Swap();
    buffer.Swap();

    Expect.AreEqual(20, buffer.Read);
  }

  [Test]
  private void DisposeDisposesBothBuffers()
  {
    DisposableBuffer first = new();
    DisposableBuffer second = new();
    DoubleBuffer<DisposableBuffer> buffer = new(first, second);

    buffer.Dispose();

    Expect.AreEqual(1, first.DisposeCount);
    Expect.AreEqual(1, second.DisposeCount);
  }

  [Test]
  private void DisposeAllowsNonDisposableBuffers()
  {
    DoubleBuffer<int> buffer = new(10, 20);

    buffer.Dispose();
  }

  private sealed class DisposableBuffer : IDisposable
  {
    public int DisposeCount { get; private set; }

    public void Dispose()
    {
      DisposeCount++;
    }
  }
}
