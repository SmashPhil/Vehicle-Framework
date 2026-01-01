using System.Collections.Generic;
using CoreLib.Performance;
using DevTools.Testing;
using UnityEngine;
using UnityEngine.Assertions;

namespace CoreLib.UnitTesting;

[UnitTest(TestType.MainMenu)]
[TestDescription(
  "Unity object pool for objects used within the same context.")]
internal class UnitTest_UnityObjectPool
{
  private const int MockSize = 10;
  private const int PreWarmCount = 5;

  private readonly List<GameObject> objectsToDestroy = [];

  [SetUp]
  private void ClearObjectList()
  {
    objectsToDestroy.Clear();
  }

  [TearDown]
  private void DestroyAllObjects()
  {
    for (int i = objectsToDestroy.Count - 1; i >= 0; i--)
    {
      GameObject obj = objectsToDestroy[i];
      // Objects should already be destroyed here but by gathering all newly instantiated
      // test objects separate from the object pool we can verify independently that all
      // objects created from the object pool will be destroyed when dumped.
      Expect.IsNull(obj);
      if (obj)
        Object.Destroy(obj);
    }
    objectsToDestroy.Clear();
  }

  [Test, ExecutionPriority(Priority.First)]
  private void Constructor()
  {
    Assert.IsTrue(UnityThread.IsInMainThread);
    UnityObjectPool<GameObject> pool = new(NewObject, MockSize);
    Assert.IsTrue(pool.Count == 0);
  }

  [Test, ExecutionPriority(Priority.First)]
  private void Return()
  {
    // Create new object before we start watching object count, in practice
    // this object would've already been in use before being returned to pool.
    GameObject testObject = NewObject();

    using ObjectCountWatcher<TestBehaviour> ocw = new();
    UnityObjectPool<GameObject> pool = new(NewObject, MockSize);

    pool.Return(testObject);
    Expect.AreEqual(1, pool.Count);
    Expect.AreEqual(0, ocw.Count);
  }

  [Test]
  private void PreWarm()
  {
    using ObjectCountWatcher<TestBehaviour> ocw = new();
    UnityObjectPool<GameObject> pool = new(NewObject, MockSize);
    pool.PreWarm(PreWarmCount);
    Expect.AreEqual(PreWarmCount, pool.Count);
    Expect.AreEqual(PreWarmCount, ocw.Count);
  }

  [Test]
  private void GetNew()
  {
    using ObjectCountWatcher<TestBehaviour> ocw = new();
    UnityObjectPool<GameObject> pool = new(NewObject, MockSize);
    GameObject obj = pool.Get();
    Expect.AreEqual(0, pool.Count);
    Expect.AreEqual(1, ocw.Count);
    Object.Destroy(obj);
  }

  [Test]
  private void GetCached()
  {
    UnityObjectPool<GameObject> pool = new(NewObject, MockSize, preWarm: PreWarmCount);
    Assert.AreEqual(PreWarmCount, pool.Count);
    using ObjectCountWatcher<TestBehaviour> ocw = new();
    GameObject fetchedObject = pool.Get();
    using DestroyOnDispose dod = new(fetchedObject);
    Expect.IsNotNull(fetchedObject);
    Expect.AreEqual(PreWarmCount - 1, pool.Count);
    Expect.AreEqual(0, ocw.Count);
  }

  [Test]
  private void Clear()
  {
    UnityObjectPool<GameObject> pool = new(NewObject, MockSize);
    pool.PreWarm(PreWarmCount);
    Assert.AreEqual(expected: 5, pool.Count);

    using ObjectCountWatcher<TestBehaviour> ocw = new();
    pool.Clear();
    Expect.AreEqual(0, pool.Count);
    Expect.AreEqual(0, ocw.Count);
  }

  private static GameObject NewObject()
  {
    GameObject newObj = new();
    newObj.AddComponent<TestBehaviour>();
    return newObj;
  }

  private class TestBehaviour : MonoBehaviour
  {
    private void Awake()
    {
      ObjectCounter.Increment<TestBehaviour>();
    }
  }
}