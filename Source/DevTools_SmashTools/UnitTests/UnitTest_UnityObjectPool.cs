using System.Collections.Generic;
using DevTools.Testing;
using SmashTools.Performance;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.UnitTesting;

[UnitTest(TestType.MainMenu)]
[TestDescription(
  "Unity object pool for objects used within the same context.")]
internal class UnitTest_UnityObjectPool
{
  private readonly List<GameObject> objectsToDestroy = [];

  [SetUp]
  private void ClearObjectList()
  {
    objectsToDestroy.Clear();
  }

  [Test]
  private void ItemExchange()
  {
    const int PreWarmCount = 5;

    UnityObjectPool<GameObject> pool = new(NewObject, 10);

    Assert.IsTrue(pool.Count == 0);
    Assert.IsTrue(UnityData.IsInMainThread);
    // PreWarm
    {
      using ObjectCountWatcher<TestBehaviour> ocw = new();

      pool.PreWarm(PreWarmCount);
      Expect.AreEqual(pool.Count, PreWarmCount, "PreWarm Init");
      Expect.AreEqual(ocw.Count, PreWarmCount, "New Objects");
    }

    // Create new object before we start watching object count, in practice
    // this object would've already been in use before being returned to pool.
    GameObject testObject = NewObject();

    // Return
    {
      using ObjectCountWatcher<TestBehaviour> ocw = new();

      pool.Return(testObject);
      Expect.AreEqual(pool.Count, PreWarmCount + 1, "Return Head++");
      Expect.AreEqual(ocw.Count, 0, "Return New Objects");
    }

    // Get
    {
      using ObjectCountWatcher<TestBehaviour> ocw = new();

      GameObject fetchedObject = pool.Get();
      Expect.ReferencesAreEqual(testObject, fetchedObject, "Get Head");
      Expect.IsNotNull(fetchedObject, "Not Destroyed");
      Expect.AreEqual(pool.Count, PreWarmCount, "Get Head--");
      Expect.AreEqual(ocw.Count, 0, "Get New Objects");
      Object.Destroy(fetchedObject);
    }

    // Dump
    {
      using ObjectCountWatcher<TestBehaviour> ocw = new();
      pool.Dump();
      Expect.AreEqual(pool.Count, 0, "Dump Head");
      Expect.AreEqual(ocw.Count, 0, "Dump New Objects");
    }

    // Get (Create New)
    {
      using ObjectCountWatcher<TestBehaviour> ocw = new();
      GameObject obj = pool.Get();
      Expect.AreEqual(pool.Count, 0, "Item not added to pool on new.");
      Expect.AreEqual(ocw.Count, 1, "Get New Objects");
      Object.Destroy(obj);
    }
  }

  private static GameObject NewObject()
  {
    GameObject newObj = new();
    newObj.AddComponent<TestBehaviour>();
    return newObj;
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

  private class TestBehaviour : MonoBehaviour
  {
    private void Awake()
    {
      ObjectCounter.Increment<TestBehaviour>();
    }
  }
}