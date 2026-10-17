using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace SmashTools
{
	public class WidgetRowButBetter
	{
		public const float IconSize = 24f;
		public const float DefaultGap = 4f;
		private const float DefaultMaxWidth = 99999f;
		public const float LabelGap = 2f;
		public const float ButtonExtraSpace = 16f;

		private float startX;
		private float startY;
		private int rows = 1;
		private float maxWidth = DefaultMaxWidth;
		private UIDirection growDirection = UIDirection.RightThenUp;

		public virtual float CurX { get; protected set; }

		public virtual float CurY { get; protected set; }

		public virtual float CellGap { get; protected set; }

		public WidgetRowButBetter()
		{
		}
		
		public WidgetRowButBetter(float x, float y, UIDirection growDirection = UIDirection.RightThenUp, float maxWidth = DefaultMaxWidth, float gap = DefaultGap)
		{
			Init(x, y, growDirection, maxWidth, gap);
		}

		public virtual void Init(float x, float y, UIDirection growDirection = UIDirection.RightThenUp, float maxWidth = DefaultMaxWidth, float gap = DefaultGap)
		{
			this.growDirection = growDirection;
			startX = x;
			startY = y;
			CurX = x;
			CurY = y;
			this.maxWidth = maxWidth;
			CellGap = gap;
		}

		public float LeftX(float elementWidth)
		{
			if (growDirection == UIDirection.RightThenUp || growDirection == UIDirection.RightThenDown)
			{
				return CurX;
			}
			return CurX - elementWidth;
		}

		public void IncrementPosition(float amount)
		{
			if (growDirection == UIDirection.RightThenUp || growDirection == UIDirection.RightThenDown)
			{
				CurX += amount;
			}
			else
			{
				CurX -= amount;
			}
			if (Mathf.Abs(CurX - startX) > maxWidth)
			{
				IncrementY();
			}
		}

		public void IncrementY()
		{
			if (growDirection == UIDirection.RightThenUp || growDirection == UIDirection.LeftThenUp)
			{
				CurY -= IconSize + CellGap;
			}
			else
			{
				CurY += IconSize + CellGap;
			}
			rows++;
			CurX = startX;
		}

		public void IncrementYIfWillExceedMaxWidth(float width)
		{
			if (Mathf.Abs(CurX - startX) + Mathf.Abs(width) > maxWidth)
			{
				IncrementY();
			}
		}

		public void Gap(float width)
		{
			if (CurX != startX)
			{
				IncrementPosition(width);
			}
		}

		public bool RowSelect(bool selected, string tooltip = null, Color? mouseoverBackgroundColor = null, Color? backgroundColor = null, bool doMouseoverSound = true, float fixedWidth = -1)
		{
			Rect rect = new Rect(startX, startY, fixedWidth > 0 ? fixedWidth : maxWidth, rows * IconSize);

			if (!tooltip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, tooltip);
			}

			if (doMouseoverSound)
			{
				MouseoverSounds.DoRegion(rect);
			}
			if (Mouse.IsOver(rect) || selected)
			{
				Widgets.DrawRectFast(rect, mouseoverBackgroundColor ?? ListingExtension.LightHighlightColor);
			}
			else if (backgroundColor != null && !Mouse.IsOver(rect))
			{
				Widgets.DrawRectFast(rect, backgroundColor.Value);
			}
			return Widgets.ButtonInvisible(rect);
		}

		public bool ButtonIcon(Texture2D tex, string tooltip = null, Color? mouseoverColor = null, Color? backgroundColor = null, Color? mouseoverBackgroundColor = null, bool doMouseoverSound = true, float overrideSize = -1f)
		{
			float num = (overrideSize > 0f) ? overrideSize : IconSize;
			float num2 = (IconSize - num) / LabelGap;
			IncrementYIfWillExceedMaxWidth(num);
			Rect rect = new Rect(LeftX(num) + num2, CurY + num2, num, num);
			if (doMouseoverSound)
			{
				MouseoverSounds.DoRegion(rect);
			}
			if (mouseoverBackgroundColor != null && Mouse.IsOver(rect))
			{
				Widgets.DrawRectFast(rect, mouseoverBackgroundColor.Value, null);
			}
			else if (backgroundColor != null && !Mouse.IsOver(rect))
			{
				Widgets.DrawRectFast(rect, backgroundColor.Value, null);
			}
			bool result = Widgets.ButtonImage(rect, tex, Color.white, mouseoverColor ?? GenUI.MouseoverColor);
			IncrementPosition(num);
			if (!tooltip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, tooltip);
			}
			return result;
		}

		public bool ButtonIconWithBG(Texture2D texture, float width = -1f, string tooltip = null, bool doMouseoverSound = true)
		{
			if (width < 0f)
			{
				width = IconSize;
			}
			width += ButtonExtraSpace;
			IncrementYIfWillExceedMaxWidth(width);
			Rect rect = new Rect(LeftX(width), CurY, width, 26f);
			if (doMouseoverSound)
			{
				MouseoverSounds.DoRegion(rect);
			}
			bool result = Widgets.ButtonImageWithBG(rect, texture, new Vector2?(Vector2.one * IconSize));
			IncrementPosition(width + CellGap);
			if (!tooltip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, tooltip);
			}
			return result;
		}

		public void ToggleableIcon(ref bool toggleable, Texture2D tex, string tooltip, SoundDef mouseoverSound = null, string tutorTag = null)
		{
			IncrementYIfWillExceedMaxWidth(IconSize);
			Rect rect = new Rect(LeftX(IconSize), CurY, IconSize, IconSize);
			bool flag = Widgets.ButtonImage(rect, tex, true);
			IncrementPosition(IconSize + CellGap);
			if (!tooltip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, tooltip);
			}
			Rect position = new Rect(rect.x + rect.width / LabelGap, rect.y, rect.height / LabelGap, rect.height / LabelGap);
			Texture2D image = toggleable ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex;
			GUI.DrawTexture(position, image);
			if (mouseoverSound != null)
			{
				MouseoverSounds.DoRegion(rect, mouseoverSound);
			}
			if (flag)
			{
				toggleable = !toggleable;
				if (toggleable)
				{
					SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
				}
				else
				{
					SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
				}
			}
			if (tutorTag != null)
			{
				UIHighlighter.HighlightOpportunity(rect, tutorTag);
			}
		}

		public Rect Icon(Texture tex, string tooltip = null)
		{
			IncrementYIfWillExceedMaxWidth(IconSize);
			Rect rect = new Rect(LeftX(IconSize), CurY, IconSize, IconSize);
			GUI.DrawTexture(rect, tex);
			if (!tooltip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, tooltip);
			}
			IncrementPosition(IconSize + CellGap);
			return rect;
		}

		public Rect DefIcon(ThingDef def, string tooltip = null)
		{
			IncrementYIfWillExceedMaxWidth(IconSize);
			Rect rect = new Rect(LeftX(IconSize), CurY, IconSize, IconSize);
			Widgets.DefIcon(rect, def, null, 1f, null, false, null);
			if (!tooltip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, tooltip);
			}
			IncrementPosition(IconSize + CellGap);
			return rect;
		}

		public bool ButtonText(string label, string tooltip = null, bool drawBackground = true, bool doMouseoverSound = true, bool active = true, float? fixedWidth = null)
		{
			Rect rect = ButtonRect(label, fixedWidth);
			bool result = Widgets.ButtonText(rect, label, drawBackground, doMouseoverSound, active);
			if (!tooltip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, tooltip);
			}
			return result;
		}

		public Rect ButtonRect(string label, float? fixedWidth = null)
		{
			Vector2 vector = (fixedWidth != null) ? new Vector2(fixedWidth.Value, IconSize) : Text.CalcSize(label);
			vector.x += ButtonExtraSpace;
			vector.y += LabelGap;
			IncrementYIfWillExceedMaxWidth(vector.x);
			Rect result = new Rect(LeftX(vector.x), CurY, vector.x, vector.y);
			IncrementPosition(result.width + CellGap);
			return result;
		}

		public bool Checkbox(ref bool checkOn, string tooltip = null)
		{
			IncrementYIfWillExceedMaxWidth(IconSize);
			Rect rect = new Rect(LeftX(IconSize), CurY, IconSize, IconSize);
			if (!tooltip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, tooltip);
			}
			bool checkBefore = checkOn;
			Widgets.Checkbox(rect.position, ref checkOn, IconSize);
			IncrementPosition(IconSize + CellGap);
			return checkBefore != checkOn;
		}

		public Rect Label(string text, float width = -1f, string tooltip = null, float height = -1f)
		{
			if (height < 0f)
			{
				height = IconSize;
			}
			if (width < 0f)
			{
				width = Text.CalcSize(text).x;
			}
			IncrementYIfWillExceedMaxWidth(width + LabelGap);
			IncrementPosition(LabelGap);
			Rect rect = new Rect(LeftX(width), CurY + CellGap / 2, width, height);
			Widgets.Label(rect, text);
			if (!tooltip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, tooltip);
			}
			IncrementPosition(LabelGap);
			IncrementPosition(rect.width);
			return rect;
		}

		public Rect TextFieldNumeric(ref int val, ref string buffer, float width = -1f)
		{
			if (width < 0f)
			{
				width = Text.CalcSize(val.ToString()).x;
			}
			IncrementYIfWillExceedMaxWidth(width + LabelGap);
			IncrementPosition(LabelGap);
			Rect rect = new Rect(LeftX(width), CurY, width, IconSize);
			Widgets.TextFieldNumeric(rect, ref val, ref buffer, 0f, 1E+09f);
			IncrementPosition(LabelGap);
			IncrementPosition(rect.width);
			return rect;
		}

		public Rect FillableBar(float width, float height, float fillPct, string label, Texture2D fillTex, Texture2D bgTex = null)
		{
			IncrementYIfWillExceedMaxWidth(width);
			Rect rect = new Rect(LeftX(width), CurY, width, height);
			Widgets.FillableBar(rect, fillPct, fillTex, bgTex, false);
			if (!label.NullOrEmpty())
			{
				Rect rect2 = rect;
				rect2.xMin += LabelGap;
				rect2.xMax -= LabelGap;
				if (Text.Anchor >= TextAnchor.UpperLeft)
				{
					rect2.height += 14f;
				}
				Text.Font = GameFont.Tiny;
				Text.WordWrap = false;
				Widgets.Label(rect2, label);
				Text.WordWrap = true;
			}
			IncrementPosition(width);
			return rect;
		}
	}
}
