using System;
using DevTools.UnitTesting;
using UnityEngine.Assertions;

namespace SmashTools.UnitTesting;

[UnitTest(TestType.MainMenu)]
[TestCategory(TestCategoryNames.Events)]
[TestDescription("Event listener that tracks how many times a specific event has been raised for an event manager.")]
internal sealed class UnitTest_EventListener
{
  [Test]
  private void RaisedEvent()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    Assert.IsNotNull(obj.EventRegistry.map);
    Assert.AreEqual(obj.EventRegistry.map.Count, Enum.GetValues(typeof(EventType)).Length);
    using EventListener<EventType> listener = new(obj, EventType.Yes);
    Expect.AreEqual(listener.CountRaised, 0);
    obj.EventRegistry[EventType.Yes].ExecuteEvents();
    Expect.AreEqual(listener.CountRaised, 1);
  }

  [Test]
  private void RaisedDifferentEvent()
  {
    TestObject obj = new();
    obj.FillEventsEnum();
    Assert.IsNotNull(obj.EventRegistry.map);
    Assert.AreEqual(obj.EventRegistry.map.Count, Enum.GetValues(typeof(EventType)).Length);
    using EventListener<EventType> listener = new(obj, EventType.Yes);
    Expect.AreEqual(listener.CountRaised, 0);
    obj.EventRegistry[EventType.No].ExecuteEvents();
    Expect.AreEqual(listener.CountRaised, 0);
  }

  private class TestObject : IEventManager<EventType>
  {
    public EventManager<EventType> EventRegistry { get; set; }
  }

  private enum EventType
  {
    Yes,
    No
  }
}