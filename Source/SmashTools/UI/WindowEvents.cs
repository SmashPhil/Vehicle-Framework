using System;
using System.Collections.Generic;
using CoreLib;
using Verse;

namespace SmashTools;

public static class WindowEvents
{
	private static readonly EventDataCache EventCache = new();
	private static readonly List<IHighPriorityOnGUI> HighPriorityOnGui = [];

	public delegate void OnEvent();

	public static void Register(this IWindowEventListener listener, Window sender, OnEvent onEvent, Event ev)
	{
		if (sender == null)
			throw new ArgumentNullException(nameof(sender));

		EventCache.RegisterImpl(listener, sender, onEvent, ev);
	}

	public static void Deregister(this IWindowEventListener listener)
	{
		EventCache.DeregisterImpl(listener);
	}

	internal static void WindowAddedToStack(Window window)
	{
		if (window is IHighPriorityOnGUI highPriorityOnGUI)
		{
			HighPriorityOnGui.Add(highPriorityOnGUI);
		}
		EventCache.Raise(Event.Opened);
	}

	internal static void WindowRemovedFromStack(Window window, bool __result)
	{
		if (__result)
		{
			if (window is IHighPriorityOnGUI highPriorityOnGUI)
			{
				HighPriorityOnGui.Remove(highPriorityOnGUI);
			}
			EventCache.Raise(Event.Closed);
		}
	}

	internal static void HighPriorityOnGUI()
	{
		for (int i = HighPriorityOnGui.Count - 1; i >= 0; i--)
		{
			IHighPriorityOnGUI highPriorityOnGUI = HighPriorityOnGui[i];
			highPriorityOnGUI.OnGUIHighPriority();
		}
	}

	public enum Event
	{
		Closed,
		Opened
	}

	private class EventDataCache
	{
		private readonly List<Data> observerData = [];
		private readonly List<IWindowEventListener> listenersToRemove = [];

		public void RegisterImpl(IWindowEventListener listener, Window sender, OnEvent onEvent, Event ev)
		{
			Data data = observerData.FirstOrDefault(data => data.listener == listener);
			if (data == null)
			{
				data = new Data(listener, sender);
				observerData.Add(data);
			}
			data.events[ev] = onEvent;
		}

		public void DeregisterImpl(IWindowEventListener listener)
		{
			for (int i = observerData.Count - 1; i >= 0; i--)
			{
				if (observerData[i].listener == listener)
				{
					observerData.RemoveAt(i);
					return;
				}
			}
		}

		private void FlagForRemoval(IWindowEventListener listener)
		{
			listenersToRemove.Add(listener);
		}

		public void Raise(Event ev)
		{
      foreach (Data data in observerData)
      {
        if (data.events.TryGetValue(ev, out OnEvent onEvent))
        {
          onEvent();
        }
      }
      RemoveListeners();
		}

		private void RemoveListeners()
		{
			foreach (IWindowEventListener listener in listenersToRemove)
			{
				Deregister(listener);
			}
			listenersToRemove.Clear();
		}

		private class Data(IWindowEventListener listener, Window sender)
		{
			public readonly IWindowEventListener listener = listener;
			public readonly Window sender = sender;

			public readonly Dictionary<Event, OnEvent> events = [];
		}
	}
}