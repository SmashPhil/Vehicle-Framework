using System;
using DevTools;
using DevTools.Testing;
using UnityEngine.Assertions;

namespace SmashTools.Tests;

[TestFixture(TestType.MainMenu)]
[TestCategory(TestCategoryNames.Events)]
[TestDescription("Event system for providing more control over event triggers and registration.")]
internal sealed class Test_EventManager
{
  private const int DefaultState = -1;

  private static int integer;

  [SetUp]
  private void ResetFlag()
  {
    integer = DefaultState;
  }

  [Test]
  private void EventFill()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    Assert.AreEqual(obj.EventRegistry.Count, Enum.GetValues<EventType>().Length);
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

    using (new ScopedRollback<int>(ref integer))
    {
      obj.EventRegistry[EventType.One].ExecuteEvents();
      Assert.AreEqual(integer, 1);
    }
    Assert.AreEqual(integer, DefaultState);
    Assert.IsTrue(obj.EventRegistry[EventType.One].Enabled);
    Assert.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 1);

    using (new ScopedRollback<int>(ref integer))
    {
      obj.EventRegistry[EventType.One].ExecuteEvents();
      Assert.AreEqual(integer, 1);
    }
    Assert.AreEqual(integer, DefaultState);
    Assert.IsTrue(obj.EventRegistry[EventType.One].Enabled);
    Assert.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 1);
  }

  [Test]
  private void EventInstance()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    object instance = new();
    obj.AddEvent(instance, EventType.One, SetIntOne);

    Assert.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 1);
    obj.EventRegistry[EventType.One].ExecuteEvents();
    Expect.AreEqual(integer, 1);
  }

  [Test]
  private void RemoveAllEvents()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    object instance = new();
    obj.AddEvent(instance, EventType.One, SetIntOne);
    obj.AddEvent(instance, EventType.Two, SetIntTwo);

    obj.RemoveAllEvents(instance);

    Expect.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 0);
    Expect.AreEqual(obj.EventRegistry[EventType.Two].TotalEventCount, 0);
  }

  [Test]
  private void RemoveAllEventsOtherInstance()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    object instance = new();
    object otherInstance = new();
    obj.AddEvent(instance, EventType.One, SetIntOne);
    obj.AddEvent(otherInstance, EventType.One, SetIntTwo);

    obj.RemoveAllEvents(instance);

    Assert.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 1);
    obj.EventRegistry[EventType.One].ExecuteEvents();
    Expect.AreEqual(integer, 2);
  }

  [Test]
  private void RemoveAllEventsSharedAction()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    object instance = new();
    object otherInstance = new();
    obj.AddEvent(instance, EventType.One, SetIntOne);
    obj.AddEvent(otherInstance, EventType.One, SetIntOne);

    obj.RemoveAllEvents(instance);

    Expect.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 1);
  }

  [Test]
  private void RemoveAllEventsAdditionalActions()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    object instance = new();
    obj.AddEvent(instance, EventType.One, SetIntOne, SetIntTwo);

    obj.RemoveAllEvents(instance);

    Expect.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 0);
  }

  [Test]
  private void RemoveAllEventsWithKey()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    object instance = new();
    obj.EventRegistry.AddEvent(instance, EventType.One, SetIntOne, "key");

    obj.RemoveAllEvents(instance);

    Expect.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 0);
  }

  [Test]
  private void RemoveInstanceActionPreservesOtherAction()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    object instance = new();
    obj.AddEvent(instance, EventType.One, SetIntOne, SetIntTwo);

    obj.EventRegistry[EventType.One].Remove(instance, SetIntOne);

    Assert.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 1);
    obj.EventRegistry[EventType.One].ExecuteEvents();
    Expect.AreEqual(integer, 2);
  }

  [Test]
  private void RemoveAllEventsPreservesUntrackedAction()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    object instance = new();
    obj.AddEvent(instance, EventType.One, SetIntOne);
    obj.AddEvent(EventType.One, SetIntOne);

    obj.RemoveAllEvents(instance);

    Expect.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 1);
  }

  [Test]
  private void RemoveAllEventsPreservesUntrackedSingleEvent()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    object instance = new();
    obj.AddEvent(instance, EventType.One, SetIntOne);
    obj.AddSingleEvent(EventType.One, SetIntOne);

    obj.RemoveAllEvents(instance);

    Assert.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 1);
    obj.EventRegistry[EventType.One].ExecuteEvents();
    Expect.AreEqual(integer, 1);
    Expect.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 0);
  }

  [Test]
  private void RemoveEventByActionRemovesTrackedAndUntracked()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    object instance = new();
    obj.AddEvent(EventType.One, SetIntOne);
    obj.AddEvent(instance, EventType.One, SetIntOne);

    obj.RemoveEvent(EventType.One, SetIntOne);

    Expect.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 0);
  }

  [Test]
  private void RemoveEventByActionPreservesInstanceAction()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    object instance = new();
    obj.AddEvent(EventType.One, SetIntOne);
    obj.AddEvent(instance, EventType.One, SetIntTwo);

    obj.RemoveEvent(EventType.One, SetIntOne);

    Assert.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 1);
    obj.EventRegistry[EventType.One].ExecuteEvents();
    Expect.AreEqual(integer, 2);
  }

  [Test]
  private void RemoveEventByKeyRemovesTrackedAndUntracked()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    object instance = new();
    obj.AddEvent(EventType.One, SetIntOne, "key");
    obj.EventRegistry.AddEvent(instance, EventType.One, SetIntTwo, "key");

    obj.RemoveEvent(EventType.One, "key");

    Expect.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 0);
  }

  [Test]
  private void RemoveEventByKeyPreservesOtherInstanceKey()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    object instance = new();
    obj.AddEvent(EventType.One, SetIntOne, "untracked");
    obj.EventRegistry.AddEvent(instance, EventType.One, SetIntTwo, "tracked");

    obj.RemoveEvent(EventType.One, "untracked");

    Assert.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 1);
    obj.EventRegistry[EventType.One].ExecuteEvents();
    Expect.AreEqual(integer, 2);
  }

  [Test]
  private void RemoveAllEventsWithKeyAdditionalActions()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    object instance = new();
    obj.EventRegistry.AddEvent(instance, EventType.One, SetIntOne, "key", SetIntTwo);

    obj.RemoveAllEvents(instance);

    Expect.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 0);
  }

  [Test]
  private void EventSingle()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    obj.AddSingleEvent(EventType.One, SetIntOne);
    Assert.IsTrue(obj.EventRegistry[EventType.One].Enabled);
    Assert.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 1);

    using (new ScopedRollback<int>(ref integer))
    {
      obj.EventRegistry[EventType.One].ExecuteEvents();
      Assert.AreEqual(integer, 1);
    }
    Assert.AreEqual(integer, DefaultState);
    Assert.IsTrue(obj.EventRegistry[EventType.One].Enabled);
    Assert.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 0);

    using (new ScopedRollback<int>(ref integer))
    {
      obj.EventRegistry[EventType.One].ExecuteEvents();
      Assert.AreEqual(integer, DefaultState);
    }
    Assert.AreEqual(integer, DefaultState);
    Assert.IsTrue(obj.EventRegistry[EventType.One].Enabled);
    Assert.AreEqual(obj.EventRegistry[EventType.One].TotalEventCount, 0);
  }

  [Test]
  private void EventControl()
  {
    IEventControl control = new TestControl();
    Assert.IsTrue(control.Enabled);
    using (new EventDisabler<EventType>(control))
    {
      Expect.IsFalse(control.Enabled);
    }
    Expect.IsTrue(control.Enabled);
  }

  [Test]
  private void DisableGlobal()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    obj.AddEvent(EventType.One, SetIntOne);
    obj.AddEvent(EventType.Two, SetIntTwo);
    obj.AddEvent(EventType.Three, SetIntThree);

    using (new EventDisabler<EventType>(obj.EventRegistry))
    {
      Assert.IsFalse(obj.EventRegistry.Enabled);
      // Individual event triggers don't get disabled, only the manager does
      Expect.IsTrue(obj.EventRegistry[EventType.One].Enabled);
      Expect.IsTrue(obj.EventRegistry[EventType.Two].Enabled);
      Expect.IsTrue(obj.EventRegistry[EventType.Three].Enabled);
      // All events disabled
      using ScopedRollback<int> svr = new(ref integer);
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

    using (new EventDisabler<EventType>(obj.EventRegistry[EventType.Two]))
    {
      Assert.IsTrue(obj.EventRegistry.Enabled);
      Assert.IsTrue(obj.EventRegistry[EventType.One].Enabled);
      Assert.IsFalse(obj.EventRegistry[EventType.Two].Enabled);
      Assert.IsTrue(obj.EventRegistry[EventType.Three].Enabled);
      // Only EventType::Two events are disabled
      using ScopedRollback<int> svr = new(ref integer);
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
    bool IEventControl.Enabled { get; set; } = true;
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
