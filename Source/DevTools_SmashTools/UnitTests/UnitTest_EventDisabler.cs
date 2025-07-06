using System;
using DevTools.UnitTesting;
using UnityEngine.Assertions;

namespace SmashTools.UnitTesting;

[UnitTest(TestType.MainMenu)]
[TestCategory(TestCategoryNames.Events)]
[TestDescription("Expansive event system for providing more control over event triggers and registration.")]
internal sealed class UnitTest_EventManager
{
  private const int DefaultState = -1;

  private static int integer;

  [SetUp, TearDown]
  private void ResetFlag()
  {
    integer = DefaultState;
  }

  [Test]
  private void EventFill()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    Assert.IsNotNull(obj.EventRegistry.map);
    Assert.AreEqual(obj.EventRegistry.map.Count, Enum.GetValues(typeof(EventType)).Length);
    Expect.IsTrue(obj.EventRegistry[EventType.One].Enabled);
    Expect.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 0);
    Expect.IsTrue(obj.EventRegistry[EventType.Two].Enabled);
    Expect.AreEqual(obj.EventRegistry[EventType.Two].TotalEventCount, 0);
    Expect.IsTrue(obj.EventRegistry[EventType.Three].Enabled);
    Expect.AreEqual(obj.EventRegistry[EventType.Three].TotalEventCount, 0);
  }

  [Test]
  private void EventPersistent()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    obj.AddEvent(EventType.One, SetIntOne);
    Assert.IsTrue(obj.EventRegistry[EventType.One].Enabled);
    Assert.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 1);

    using (new ScopedValueRollback<int>(ref integer))
    {
      obj.EventRegistry[EventType.One].ExecuteEvents();
      Assert.AreEqual(integer, 1);
    }
    Assert.AreEqual(integer, DefaultState);
    Assert.IsTrue(obj.EventRegistry[EventType.One].Enabled);
    Assert.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 1);

    using (new ScopedValueRollback<int>(ref integer))
    {
      obj.EventRegistry[EventType.One].ExecuteEvents();
      Assert.AreEqual(integer, 1);
    }
    Assert.AreEqual(integer, DefaultState);
    Assert.IsTrue(obj.EventRegistry[EventType.One].Enabled);
    Assert.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 1);
  }

  [Test]
  private void EventSingle()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    obj.AddSingleEvent(EventType.One, SetIntOne);
    Assert.IsTrue(obj.EventRegistry[EventType.One].Enabled);
    Assert.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 1);

    using (new ScopedValueRollback<int>(ref integer))
    {
      obj.EventRegistry[EventType.One].ExecuteEvents();
      Assert.AreEqual(integer, 1);
    }
    Assert.AreEqual(integer, DefaultState);
    Assert.IsTrue(obj.EventRegistry[EventType.One].Enabled);
    Assert.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 0);

    using (new ScopedValueRollback<int>(ref integer))
    {
      obj.EventRegistry[EventType.One].ExecuteEvents();
      Assert.AreEqual(integer, DefaultState);
    }
    Assert.AreEqual(integer, DefaultState);
    Assert.IsTrue(obj.EventRegistry[EventType.One].Enabled);
    Assert.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 0);
  }

  [Test]
  private void DisableGlobal()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    obj.AddEvent(EventType.One, SetIntOne);
    obj.AddEvent(EventType.Two, SetIntTwo);
    obj.AddEvent(EventType.Three, SetIntThree);

    using (new EventDisabler<EventType>(obj))
    {
      Assert.IsFalse(obj.EventRegistry.Enabled);
      // Individual event triggers don't get disabled, only the manager does
      Expect.IsTrue(obj.EventRegistry[EventType.One].Enabled);
      Expect.IsTrue(obj.EventRegistry[EventType.Two].Enabled);
      Expect.IsTrue(obj.EventRegistry[EventType.Three].Enabled);
      // All events disabled
      using ScopedValueRollback<int> svr = new(ref integer);
      obj.EventRegistry[EventType.One].ExecuteEvents();
      Expect.AreEqual(integer, DefaultState);
      obj.EventRegistry[EventType.Two].ExecuteEvents();
      Expect.AreEqual(integer, DefaultState);
      obj.EventRegistry[EventType.Three].ExecuteEvents();
      Expect.AreEqual(integer, DefaultState);
    }
    Assert.IsTrue(obj.EventRegistry.Enabled);
  }

  [Test]
  private void DisableSingle()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    obj.AddEvent(EventType.One, SetIntOne);
    obj.AddEvent(EventType.Two, SetIntTwo);
    obj.AddEvent(EventType.Three, SetIntThree);

    using (new EventDisabler<EventType>(obj, EventType.Two))
    {
      Assert.IsTrue(obj.EventRegistry.Enabled);
      Assert.IsTrue(obj.EventRegistry[EventType.One].Enabled);
      Assert.IsFalse(obj.EventRegistry[EventType.Two].Enabled);
      Assert.IsTrue(obj.EventRegistry[EventType.Three].Enabled);
      // Only EventType::Two events are disabled
      using ScopedValueRollback<int> svr = new(ref integer);
      obj.EventRegistry[EventType.One].ExecuteEvents();
      Expect.AreEqual(integer, 1);
      obj.EventRegistry[EventType.Two].ExecuteEvents();
      Expect.AreEqual(integer, 1);
      obj.EventRegistry[EventType.Three].ExecuteEvents();
      Expect.AreEqual(integer, 3);
    }
    Expect.IsTrue(obj.EventRegistry.Enabled);
    Expect.IsTrue(obj.EventRegistry[EventType.One].Enabled);
    Expect.IsTrue(obj.EventRegistry[EventType.Two].Enabled);
    Expect.IsTrue(obj.EventRegistry[EventType.Three].Enabled);
  }

  private static void SetIntOne()
  {
    integer = 1;
  }

  private static void SetIntTwo()
  {
    integer = 2;
  }

  private static void SetIntThree()
  {
    integer = 3;
  }

  private class TestControl : IEventControl
  {
    bool IEventControl.Enabled { get; set; }
  }

  private class TestObject : IEventManager<EventType>
  {
    public EventManager<EventType> EventRegistry { get; set; }
  }

  private enum EventType
  {
    One,
    Two,
    Three
  }
}