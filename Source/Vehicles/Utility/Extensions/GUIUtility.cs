using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using SmashTools;

namespace Vehicles
{
	public static class GUIUtility
	{
		private static Color guiColor;
		private static GameFont gameFont;
		private static TextAnchor textAnchor;

		private static bool ActiveState { get; set; }

		public static void PushGUIState()
		{
			guiColor = GUI.color;
			gameFont = Text.Font;
			textAnchor = Text.Anchor;
			ActiveState = true;
		}

		public static void ResetGUIState()
		{
			if (!ActiveState)
			{
				SmashLog.Error($"Attempting to reset GUI and Text fields without pushing values.");
				return;
			}
			GUI.color = guiColor;
			Text.Font = gameFont;
			Text.Anchor = textAnchor;
		}

		public static void Close()
		{
			ResetGUIState();
			ActiveState = false;
		}

		public static void DisableGUI()
		{
			if (!ActiveState)
			{
				SmashLog.Error($"Attempting to reset GUI and Text fields without pushing values.");
				return;
			}
			GUI.enabled = false;
			GUI.color = UIElements.InactiveColor;
		}

		public static void EnableGUI()
		{
			if (!ActiveState)
			{
				SmashLog.Error($"Attempting to reset GUI and Text fields without pushing values.");
				return;
			}
			GUI.enabled = true;
			GUI.color = guiColor;
		}
	}
}
