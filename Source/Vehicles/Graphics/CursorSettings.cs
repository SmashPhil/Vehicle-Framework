using System.Collections.Generic;
using SmashTools;
using UnityEngine;
using Verse;

namespace Vehicles;

[StaticConstructorOnStartup]
public static class CursorSettings
{
	private static readonly Dictionary<Type, Entry> CursorLookup = [];

	static CursorSettings()
	{
		CursorLookup[Type.OpenHand] = new Entry
		{
			type = Type.OpenHand,
			texture = ContentFinder<Texture2D>.Get("UI/Cursors/MouseHandOpen"),
			hotspot = new Vector2(3, 3)
		};
		CursorLookup[Type.CloseHand] = new Entry
		{
			type = Type.CloseHand,
			texture = ContentFinder<Texture2D>.Get("UI/Cursors/MouseHandClosed"),
			hotspot = new Vector2(3, 3)
		};
	}

	public static void SetCursor(Type type)
	{
		if (!CursorLookup.TryGetValue(type, out Entry entry))
		{
			Trace.Fail($"Unable to load cursor for type {type}");
			return;
		}
		Cursor.SetCursor(entry.texture, entry.hotspot, CursorMode.Auto);
	}

	public static void Reset()
	{
		if (Prefs.CustomCursorEnabled)
		{
			CustomCursor.Activate();
		}
		else
		{
			CustomCursor.Deactivate();
		}
	}

	public enum Type
	{
		OpenHand,
		CloseHand
	}

	private record Entry
	{
		public required Type type;
		public required Texture2D texture;
		public Vector2 hotspot = Vector2.zero;
	}
}