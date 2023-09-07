using System;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public static class AltitudeMeter
	{
		public const float MinimumAltitude = 0;
		public const float MaximumAltitude = 210000;
		
		public const float MinAltitudeScreenHeight = 16;
		public const float MaxAltitudeScreenHeight = 49;
		public const float WindowHeight = 200;
		public const float InfoWindoHeight = 75;
		public static Vector2 MeterSize => new Vector2(75, 710);
		public static Vector2 AltitudeScreenPos => new Vector2(0, PaneTopY - (WindowHeight + InfoWindoHeight));
		public static Vector2 scrollPos = Vector2.zero;

		public static readonly Color WindowBGBorderColor = new ColorInt(97, 108, 122).ToColor;

		public static float PaneTopY
		{
			get
			{
				float num = Verse.UI.screenHeight - 165f;
				if (Current.ProgramState == ProgramState.Playing)
				{
					num -= 35f;
				}
				return num;
			}
		}

		public static void DrawAltitudeMeter(AerialVehicleInFlight aerialVehicle)
		{
			try
			{
				Rect rect = new Rect(AltitudeScreenPos, MeterSize);
				Rect windowRect = new Rect(rect)
				{
					width = rect.width * 3 + 10,
					height = WindowHeight + InfoWindoHeight
				};
				float elevation = (MeterSize.y - (aerialVehicle.Elevation / MaximumAltitude * MeterSize.y)).Clamp(MaxAltitudeScreenHeight, MeterSize.y - MinAltitudeScreenHeight);
				Find.WindowStack.ImmediateWindow(aerialVehicle.GetHashCode(), windowRect, WindowLayer.GameUI, delegate ()
				{
					var anchor = Text.Anchor;
					var font = Text.Font;
					var color = GUI.color;

					Rect viewRect = rect.AtZero();
					windowRect.x = rect.width + 5;
					windowRect.y = 5;
					windowRect.height = WindowHeight;

					GUI.BeginScrollView(windowRect, new Vector2(windowRect.x, elevation - WindowHeight / 2), viewRect, GUIStyle.none, GUIStyle.none);

					GUI.DrawTexture(viewRect, VehicleTex.AltitudeMeter);

					if (elevation <= MaximumAltitude)
					{
						Rect lineRect = new Rect(0, windowRect.y + elevation, viewRect.width, 1f);
						GUI.DrawTexture(lineRect, elevation >= MeterSize.y / 2 ? BaseContent.BlackTex : BaseContent.WhiteTex);
					}

					GUI.color = WindowBGBorderColor;
					Widgets.DrawLineHorizontal(0, windowRect.y + elevation + MeterSize.y / 2, viewRect.width);
					Widgets.DrawLineVertical(viewRect.width, windowRect.y, MeterSize.y);
					GUI.color = color;

					Text.Font = GameFont.Small;
					float textHeight = Text.CalcHeight(aerialVehicle.Elevation.ToString(), viewRect.width);
					Rect labelRect = new Rect(viewRect.width + 5, windowRect.y + elevation - textHeight / 2, viewRect.width - 5, textHeight);
					Widgets.DrawMenuSection(labelRect);

					Text.Font = GameFont.Tiny;
					Text.Anchor = TextAnchor.MiddleCenter;
					int elevationRounded = Mathf.RoundToInt(aerialVehicle.Elevation);
					GUI.Label(labelRect, elevationRounded.ToString(), Text.CurFontStyle);

					GUI.EndScrollView(false);

					Text.Anchor = anchor;
					Text.Font = font;
					GUI.color = color;
				}, true, false, 0);
			}
			catch (Exception ex)
			{
				SmashLog.Error($"Exception thrown while trying to draw <type>AltitudeMeter</type> for {aerialVehicle?.Label ?? "NULL"}. Exception=\"{ex}\"");
			}
		}
	}
}
