using System;
using System.Diagnostics;
using NUnit.Framework;
using SmashTools.PathFinder.Collections;
using Unity.Collections;
using Unity.Jobs;

namespace SmashTools.PathFinder.Tests
{
  [TestFixture]
  internal class TestNativeBitArraySlim
  {
    // 64bits + 1 to bump it to allocate 2 ulongs for the internal buffer. Allocating 63
    // trailing bits also gives us wiggle room when testing read/write out of bounds.
    private const int Length = 65;

    [Test, Order(0)]
    public void Constructor()
    {
      using NativeBitArraySlim bitArray = new(numBits: Length, Allocator.Temp);
      Assert.IsTrue(bitArray.IsCreated);
      Assert.AreEqual(expected: Length, bitArray.Length);
      Assert.AreEqual(expected: 2, bitArray.ULongCount);
    }

    [Test]
    public void Dispose()
    {
      NativeBitArraySlim bitArray = new(numBits: Length, Allocator.Temp);
      Assert.IsTrue(bitArray.IsCreated);
      bitArray.Dispose();
      Assert.IsFalse(bitArray.IsCreated);
    }

    [Test]
    public void DisposeFromJob()
    {
      NativeBitArraySlim bitArray = new(numBits: Length, Allocator.TempJob);
      Assert.IsTrue(bitArray.IsCreated);
      JobHandle handle = bitArray.Dispose(default);
      Assert.IsFalse(bitArray.IsCreated);
      handle.Complete();
    }

    [Test]
    public void ReadOnly()
    {
      using NativeBitArraySlim bitArray = new(numBits: Length, Allocator.Temp);
      NativeBitArraySlim.ReadOnly readOnly = bitArray.AsReadOnly();
      Assert.IsTrue(readOnly.IsCreated);
    }

    [Test]
    public void Set()
    {
      NativeBitArraySlim bitArray = new(numBits: Length, Allocator.Temp);
      Assert.IsFalse(bitArray.IsSet(pos: 0));
      bitArray.Set(pos: 0, true);
      Assert.IsTrue(bitArray.IsSet(pos: 0));

      Assert.IsFalse(bitArray.IsSet(pos: 5));
      bitArray.Set(pos: 5, true);
      Assert.IsTrue(bitArray.IsSet(pos: 5));
    }

    [Test]
    public void IsSetReadOnly()
    {
      using NativeBitArraySlim bitArray = new(numBits: Length, Allocator.Temp);
      NativeBitArraySlim.ReadOnly readOnly = bitArray.AsReadOnly();
      Assert.IsFalse(readOnly.IsSet(pos: 0));
      Assert.IsFalse(readOnly.IsSet(pos: 5));
      Assert.IsFalse(readOnly.IsSet(Length - 1));
      bitArray.Set(pos: 0, true);
      bitArray.Set(pos: 5, true);
      readOnly.IsSet(pos: Length - 1);
      Assert.IsTrue(readOnly.IsSet(pos: 0));
      Assert.IsTrue(readOnly.IsSet(pos: 5));
      Assert.IsFalse(readOnly.IsSet(Length - 1));
    }

    [Test]
    public void Clear()
    {
      using NativeBitArraySlim bitArray = new(numBits: Length, Allocator.Temp);
      bitArray.Set(0, true);
      bitArray.Set(5, true);
      bitArray.Set(Length - 1, true);

      Assert.IsTrue(bitArray.IsSet(0));
      Assert.IsTrue(bitArray.IsSet(5));
      Assert.IsTrue(bitArray.IsSet(Length - 1));

      bitArray.Clear();

      for (int i = 0; i < bitArray.Length; i++)
      {
        Assert.IsFalse(bitArray.IsSet(i));
      }
    }

    [Test]
    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    public void OutOfBounds()
    {
      using NativeBitArraySlim bitArray = new(numBits: Length, Allocator.Temp);
      var ro = bitArray.AsReadOnly();

      Assert.DoesNotThrow(() => bitArray.IsSet(0));
      Assert.DoesNotThrow(() => bitArray.Set(0, false));
      Assert.DoesNotThrow(() => ro.IsSet(0));

      Assert.Throws<IndexOutOfRangeException>(() => bitArray.IsSet(bitArray.Length));
      Assert.Throws<IndexOutOfRangeException>(() => bitArray.Set(bitArray.Length, false));
      Assert.Throws<IndexOutOfRangeException>(() => ro.IsSet(bitArray.Length));
    }
  }
}