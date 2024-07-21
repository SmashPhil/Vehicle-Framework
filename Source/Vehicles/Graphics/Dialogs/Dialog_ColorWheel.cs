using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public class Dialog_ColorWheel : Window
	{
		private const int ButtonWidth = 90;
		private const float ButtonHeight = 30f;

		public Color color = Color.white;
		public Action<Color> onComplete;

		public static float hue;
		public static float saturation;
		public static float value;

		public Dialog_ColorWheel(Color color, Action<Color> onComplete)
		{
			this.color = color;
			this.onComplete = onComplete;
			doCloseX = true;
			closeOnClickedOutside = true;
		}

		public override Vector2 InitialSize => new Vector2(375, 350 + ButtonHeight);

		public override void DoWindowContents(Rect inRect)
		{
			Rect colorContainerRect = new Rect(inRect)
			{
				height = inRect.width - 25
			};
			RenderHelper.DrawColorPicker(colorContainerRect, ref hue, ref saturation, ref value, SetColor);

			Rect buttonRect = new Rect(0f, inRect.height - ButtonHeight, ButtonWidth, ButtonHeight);
			DoBottomButtons(buttonRect);
		}

		private void DoBottomButtons(Rect rect)
		{
			if (Widgets.ButtonText(rect, "VF_ApplyButton".Translate()))
			{
				onComplete(color);
				Close(true);
			}
			rect.x += ButtonWidth;
			if (Widgets.ButtonText(rect, "CancelButton".Translate()))
			{
				Close(true);
			}
		}

		private void SetColor(float h, float s, float b)
		{
			color = new ColorInt(Color.HSVToRGB(h, s, b)).ToColor;
		}
	}
}
