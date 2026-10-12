using System.Collections;
using CoreLib.Jobs;
using DevTools.Testing;
using UnityEngine;
using UnityEngine.Assertions;

namespace SmashTools.PathFinder.Tests;

[TestFixture(TestType.MainMenu)]
internal class Test_JobDispatchManager
{
  private const string ObjName = "JobDispatchManagerTest";

  [Test]
  public void Exists()
  {
    GameObject gameObject = new(ObjName);
    try
    {
      gameObject.AddComponent<JobDispatchManager>();
      Assert.IsTrue(JobDispatchManager.Exists);
    }
    finally
    {
      Object.DestroyImmediate(gameObject);
    }
    Assert.IsFalse(JobDispatchManager.Exists);
  }

  [Test]
  public void OnDestroyRegistered()
  {
    GameObject gameObject = new(ObjName);
    MockDispatcher mock = new();
    try
    {
      gameObject.AddComponent<JobDispatchManager>();
      JobDispatchManager.Register(mock);
      Object.DestroyImmediate(gameObject);
      Assert.IsTrue(mock.onDestroyCalled);
    }
    finally
    {
      if (gameObject)
      {
        Object.DestroyImmediate(gameObject);
      }
    }
  }

  [Test]
  public void OnDestroyDeregistered()
  {
    GameObject gameObject = new(ObjName);
    MockDispatcher mock = new();
    try
    {
      gameObject.AddComponent<JobDispatchManager>();
      JobDispatchManager.Register(mock);
      JobDispatchManager.Deregister(mock);

      Object.DestroyImmediate(gameObject);
      Assert.IsFalse(mock.onDestroyCalled);
    }
    finally
    {
      if (gameObject)
      {
        Object.DestroyImmediate(gameObject);
      }
    }
  }

  [Test]
  public IEnumerator LateUpdate()
  {
    GameObject gameObject = new(ObjName);
    MockDispatcher mock = new();
    try
    {
      gameObject.AddComponent<JobDispatchManager>();
      yield return null;
      Assert.AreEqual(expected: 0, actual: mock.lateUpdateCalls);
      JobDispatchManager.Register(mock);
      yield return null;
      Assert.AreEqual(expected: 1, actual: mock.lateUpdateCalls);
    }
    finally
    {
      if (gameObject)
      {
        Object.DestroyImmediate(gameObject);
      }
    }
  }

  private class MockDispatcher : IJobDispatcher
  {
    public int lateUpdateCalls;
    public bool onDestroyCalled;

    public void LateUpdate()
    {
      lateUpdateCalls++;
    }

    public void OnDestroy()
    {
      onDestroyCalled = true;
    }
  }
}
