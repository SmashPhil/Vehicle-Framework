using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace SmashTools.PathFinder.Tests
{
  [TestFixture]
  internal class TestIndexLookupCache
  {
    [Test, Order(0)]
    public void Reserve()
    {
      try
      {
        int2 size = new(5, 5);
        NativeArray<uint>.ReadOnly lookup = IndexLookupCache.Reserve(size);
        Assert.IsTrue(lookup.IsCreated);
        Assert.AreEqual(size.x * size.y, lookup.Length);
      }
      finally
      {
        IndexLookupCache.Clear();
      }
    }

    [Test, Order(0)]
    public void Clear()
    {
      int2 size = new(5, 5);
      _ = IndexLookupCache.Reserve(size);
      Assert.AreEqual(expected: 1, IndexLookupCache.Count);
      IndexLookupCache.Clear();
      Assert.AreEqual(expected: 0, IndexLookupCache.Count);
    }

    [Test, Order(1)]
    public void Release()
    {
      int2 size = new(5, 5);
      NativeArray<uint>.ReadOnly lookup = IndexLookupCache.Reserve(size);
      try
      {
        Assert.IsTrue(lookup.IsCreated);
        Assert.AreEqual(expected: 1, IndexLookupCache.Count);
        Assert.AreEqual(expected: 1, IndexLookupCache.ReferenceCount(size));
        IndexLookupCache.Release(size);
        Assert.AreEqual(expected: 0, IndexLookupCache.Count);
        Assert.AreEqual(expected: 0, IndexLookupCache.ReferenceCount(size));
      }
      finally
      {
        IndexLookupCache.Clear();
      }
    }

    [Test]
    public void ReleaseRef()
    {
      int2 size = new(5, 5);
      _ = IndexLookupCache.Reserve(size);
      try
      {
        Assert.AreEqual(expected: 1, IndexLookupCache.Count);
        Assert.AreEqual(expected: 1, IndexLookupCache.ReferenceCount(size));
        _ = IndexLookupCache.Reserve(size);
        Assert.AreEqual(expected: 1, IndexLookupCache.Count);
        Assert.AreEqual(expected: 2, IndexLookupCache.ReferenceCount(size));
        IndexLookupCache.Release(size);
        Assert.AreEqual(expected: 1, IndexLookupCache.Count);
        Assert.AreEqual(expected: 1, IndexLookupCache.ReferenceCount(size));
        IndexLookupCache.Release(size);
        Assert.AreEqual(expected: 0, IndexLookupCache.Count);
        Assert.AreEqual(expected: 0, IndexLookupCache.ReferenceCount(size));
      }
      finally
      {
        IndexLookupCache.Clear();
      }
    }

    [Test]
    public void ReserveAltSize()
    {
      int2 size = new(5, 5);
      int2 size2 = new(3, 3);
      try
      {
        _ = IndexLookupCache.Reserve(size);
        Assert.AreEqual(expected: 1, IndexLookupCache.Count);
        Assert.AreEqual(expected: 1, IndexLookupCache.ReferenceCount(size));
        _ = IndexLookupCache.Reserve(size2);
        Assert.AreEqual(expected: 2, IndexLookupCache.Count);
        Assert.AreEqual(expected: 1, IndexLookupCache.ReferenceCount(size));
        Assert.AreEqual(expected: 1, IndexLookupCache.ReferenceCount(size2));
      }
      finally
      {
        IndexLookupCache.Clear();
      }
    }

    [Test]
    public void ReleaseAltSize()
    {
      int2 size = new(5, 5);
      int2 size2 = new(3, 3);
      try
      {
        _ = IndexLookupCache.Reserve(size);
        _ = IndexLookupCache.Reserve(size2);
        Assert.AreEqual(expected: 2, IndexLookupCache.Count);
        Assert.AreEqual(expected: 1, IndexLookupCache.ReferenceCount(size));
        Assert.AreEqual(expected: 1, IndexLookupCache.ReferenceCount(size2));
        IndexLookupCache.Release(size);
        Assert.AreEqual(expected: 1, IndexLookupCache.Count);
        Assert.AreEqual(expected: 0, IndexLookupCache.ReferenceCount(size));
        Assert.AreEqual(expected: 1, IndexLookupCache.ReferenceCount(size2));
        IndexLookupCache.Release(size2);
        Assert.AreEqual(expected: 0, IndexLookupCache.Count);
        Assert.AreEqual(expected: 0, IndexLookupCache.ReferenceCount(size));
        Assert.AreEqual(expected: 0, IndexLookupCache.ReferenceCount(size2));
      }
      finally
      {
        IndexLookupCache.Clear();
      }
    }
  }
}