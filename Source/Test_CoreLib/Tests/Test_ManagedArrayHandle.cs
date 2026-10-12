
using System;
using CoreLib.Collections;
using DevTools.Testing;
using Unity.Jobs;
using UnityEngine.Assertions;

namespace CoreLib.Tests;

[TestFixture(TestType.MainMenu)]
internal class Test_ManagedArrayHandle
{
  [Test]
  public void NullArray()
  {
    Expect.Throws<ArgumentNullException>(() =>
    {
      NativeArrayExtensions.AsNativeArray<int>(null);
    });
  }

  [Test]
  public void CreateArray()
  {
    int[] backing = { 1, 2, 3 };
    ManagedArrayHandle<int> handle = backing.AsNativeArray();
    try
    {
      Assert.IsTrue(handle.IsCreated);
    }
    finally
    {
      handle.Dispose();
    }
    Assert.IsFalse(handle.IsCreated);
  }

  [Test]
  public void Length()
  {
    int[] backing = { 1, 2, 3 };
    ManagedArrayHandle<int> handle = backing.AsNativeArray();
    try
    {
      Assert.IsTrue(handle.Length == 3);
      handle.Dispose();
      Assert.IsTrue(handle.Length == 0);
    }
    finally
    {
      if (handle.IsCreated)
      {
        handle.Dispose();
      }
    }
  }

  [Test]
  public void CaptureBuffer()
  {
    int[] backing = { 1, 2, 3 };
    using ManagedArrayHandle<int> handle = backing.AsNativeArray();
    Assert.AreEqual(expected: backing.Length, actual: handle.Length);
    for (int i = 0; i < backing.Length; i++)
    {
      Assert.AreEqual(expected: backing[i], actual: handle[i]);
    }
  }

  [Test]
  public void UseAfterFree()
  {
    int[] backing = { 1, 2, 3 };
    ManagedArrayHandle<int> handle = backing.AsNativeArray();
    handle.Dispose();
    Expect.Throws<ObjectDisposedException>(() => _ = handle[0]);
  }

  [Test]
  public void DisposeFromJob()
  {
    int[] backing = { 1, 2, 3, 4 };
    ManagedArrayHandle<int> handle = backing.AsNativeArray();
    try
    {
      Assert.IsTrue(handle.IsCreated);
      JobHandle jobHandle = handle.Dispose(default);
      jobHandle.Complete();
      Assert.IsFalse(handle.IsCreated);
    }
    finally
    {
      if (handle.IsCreated)
      {
        handle.Dispose();
      }
    }
  }
}
