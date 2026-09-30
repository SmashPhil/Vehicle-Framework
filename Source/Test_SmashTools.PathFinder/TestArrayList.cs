using System;
using NUnit.Framework;
using SmashTools.PathFinder.Collections;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace SmashTools.PathFinder.Tests
{
  [TestFixture]
  internal class TestArrayList
  {
    private const int Length = 4;

    [Test, Order(0)]
    public void Create()
    {
      using NativeArray<int> a0 = new(Length, Allocator.Temp);
      using NativeArray<int> a1 = new(Length, Allocator.Temp);
      NativeArray<int>.ReadOnly[] arrays = { a0.AsReadOnly(), a1.AsReadOnly() };
      using NativeArrayList<int> list = NativeArrayList<int>.PackArrayList(arrays, Allocator.Temp);
      Assert.IsTrue(list.IsCreated);
      Assert.AreEqual(expected: arrays.Length, actual: list.Size);
    }

    [Test, Order(0)]
    public void Destroy()
    {
      using NativeArray<int> a0 = new(Length, Allocator.Temp);
      using NativeArray<int> a1 = new(Length, Allocator.Temp);
      NativeArray<int>.ReadOnly[] arrays = { a0.AsReadOnly(), a1.AsReadOnly() };
      NativeArrayList<int> list = NativeArrayList<int>.PackArrayList(arrays, Allocator.Temp);
      try
      {
        Assert.IsTrue(list.IsCreated);
        list.Dispose();
        Assert.IsFalse(list.IsCreated);
      }
      finally
      {
        if (list.IsCreated)
        {
          list.Dispose();
        }
      }
    }

    [Test, Order(1)]
    public void NullListThrows()
    {
      Assert.Throws<ArgumentNullException>(() => NativeArrayList<int>.PackArrayList(null, Allocator.Temp));
    }

    [Test, Order(1)]
    public void ZeroLengthThrows()
    {
      NativeArray<int>.ReadOnly[] empty = { };
      Assert.Throws<ArgumentException>(() => NativeArrayList<int>.PackArrayList(empty, Allocator.Temp));
    }

    [Test]
    public void DestroyKeepsArraysAlive()
    {
      using NativeArray<int> a0 = new(Length, Allocator.Temp);
      using NativeArray<int> a1 = new(Length, Allocator.Temp);
      NativeArray<int>.ReadOnly[] arrays = { a0.AsReadOnly(), a1.AsReadOnly() };
      NativeArrayList<int> list = NativeArrayList<int>.PackArrayList(arrays, Allocator.Temp);
      try
      {
        Assert.IsTrue(a0.IsCreated);
        Assert.IsTrue(a1.IsCreated);
        list.Dispose();
        Assert.IsTrue(a0.IsCreated);
        Assert.IsTrue(a1.IsCreated);
      }
      finally
      {
        if (list.IsCreated)
        {
          list.Dispose();
        }
      }
    }

    [Test]
    public void IndexerArrayBlock()
    {
      NativeArray<int> a0 = new(Length, Allocator.Temp);
      NativeArray<int> a1 = new(Length, Allocator.Temp);
      int[,] expected = new int[2, Length];
      try
      {
        for (int i = 0; i < Length; i++)
        {
          a0[i] = i;
          a1[i] = i + 100;
          expected[0, i] = a0[i];
          expected[1, i] = a1[i];
        }

        NativeArray<int>.ReadOnly[] arrays = { a0.AsReadOnly(), a1.AsReadOnly() };
        using NativeArrayList<int> list = NativeArrayList<int>.PackArrayList(arrays, Allocator.Temp);
        Assert.IsTrue(list.IsCreated);

        for (int buffer = 0; buffer < list.Size; buffer++)
        {
          unsafe
          {
            int* array = list[buffer];
            for (int i = 0; i < Length; i++)
            {
              Assert.AreEqual(expected[buffer, i], array[i]);
            }
          }
        }
      }
      finally
      {
        a0.Dispose();
        a1.Dispose();
      }
    }

    [Test]
    public void IndexerArrayItem()
    {
      NativeArray<int> a0 = new(Length, Allocator.Temp);
      NativeArray<int> a1 = new(Length, Allocator.Temp);
      int[,] expected = new int[2, Length];
      try
      {
        for (int i = 0; i < Length; i++)
        {
          a0[i] = i;
          a1[i] = i + 100;
          expected[0, i] = a0[i];
          expected[1, i] = a1[i];
        }

        NativeArray<int>.ReadOnly[] arrays = { a0.AsReadOnly(), a1.AsReadOnly() };
        using NativeArrayList<int> list = NativeArrayList<int>.PackArrayList(arrays, Allocator.Temp);
        Assert.IsTrue(list.IsCreated);

        for (int buffer = 0; buffer < list.Size; buffer++)
        {
          for (int i = 0; i < Length; i++)
          {
            Assert.AreEqual(expected[buffer, i], list[buffer, i]);
          }
        }
      }
      finally
      {
        a0.Dispose();
        a1.Dispose();
      }
    }

    [Test]
    public void MismatchedLengthsThrows()
    {
      using NativeArray<int> a0 = new(3, Allocator.Temp);
      using NativeArray<int> a1 = new(4, Allocator.Temp);
      NativeArray<int>.ReadOnly[] arrays = { a0.AsReadOnly(), a1.AsReadOnly() };
      Assert.Throws<ArgumentException>(() => NativeArrayList<int>.PackArrayList(arrays, Allocator.Temp));
    }

    [Test]
    public void DisposeFromJob()
    {
      using NativeArray<int> a0 = new(Length, Allocator.TempJob);
      using NativeArray<int> a1 = new(Length, Allocator.TempJob);
      NativeArray<int>.ReadOnly[] arrays = { a0.AsReadOnly(), a1.AsReadOnly() };
      NativeArrayList<int> list = NativeArrayList<int>.PackArrayList(arrays, Allocator.TempJob);

      JobHandle handle = list.Dispose(default);
      Assert.IsFalse(list.IsCreated);
      handle.Complete();
      Assert.IsTrue(a0.IsCreated);
      Assert.IsTrue(a1.IsCreated);
    }
  }
}