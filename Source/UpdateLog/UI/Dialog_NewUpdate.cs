using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace UpdateLogTool;

public class Dialog_NewUpdate : Window
{
	public const float DialogWidth = 700;
	public const float DialogHeight = 740;
	public const float Footer = 25;

	public const float PreviewImageHeight = 200;
	public const float PaginationButtonHeight = 15;
	public const int BarIconSize = 25;

	private readonly UpdateLog[] logs;
	private int selectedLogIndex;

	private Vector2 scrollPosition = new Vector2(0, 0);
	private float cachedViewHeight;
	private bool cachedHeightDirty;

	/* Currently Displayed */
	private ModContentPack mod;
	private ModMetaData metaData;
	private UpdateLog log;
	/* ------------------- */

	private Listing_Rich lister = new Listing_Rich();
	private List<DescriptionData> segments = new List<DescriptionData>();

	private DescriptionData versionSegment;

	private readonly List<Tuple<string, string, Texture2D>> cachedLeftIconBar =
		new List<Tuple<string, string, Texture2D>>();

	private readonly List<Tuple<string, string, Texture2D>> cachedRightIconBar =
		new List<Tuple<string, string, Texture2D>>();

	public Dialog_NewUpdate(HashSet<UpdateLog> logs)
	{
		if (logs.EnumerableNullOrEmpty())
		{
			Close();
			return;
		}
		this.logs = logs.ToArray();
		selectedLogIndex = 0;
		CurrentLog = logs.FirstOrDefault();
		forcePause = true;
		doCloseX = true;
		closeOnClickedOutside = true;
		absorbInputAroundWindow = true;
	}

	public override Vector2 InitialSize => new Vector2(DialogWidth, DialogHeight + Footer);

	public float DialogHeightFinal => DialogHeight - (Margin / 2);

	public UpdateLog CurrentLog
	{
		get { return log; }
		set
		{
			if (log == value)
			{
				return;
			}
			if (log != null)
			{
				log.Dispose(); //Dispose old log
			}
			log = value;
			mod = log.Mod;
			metaData = ModLister.GetActiveModWithIdentifier(mod.PackageId, ignorePostfix: false);
			segments = EnhancedText.ParseDescriptionData(log).ToList();

			//Newlines will end in \n even on windows, xml reader string isn't able to use string.EndsWith for Environment.NewLine
			if (segments.LastOrDefault() is DescriptionData data && (data.text.NullOrEmpty() || data.text.Last() != '\n'))
			{
				segments.Add(new DescriptionData(Environment.NewLine)); //Append new line to description if not present.
			}

			versionSegment = new DescriptionData($"<b>Version {CurrentLog.UpdateData.currentVersion}</b>");
			log.Open();
			RecacheHyperlinks();
			cachedHeightDirty = true;
			lister.CurrentLog = CurrentLog;
		}
	}

	public override void PostClose()
	{
		log.Dispose();
	}

	private void RecacheHyperlinks()
	{
		cachedRightIconBar.Clear();
		cachedLeftIconBar.Clear();
		if (!CurrentLog.UpdateData.rightIconBar.NullOrEmpty())
		{
			foreach (var iconObj in CurrentLog.UpdateData.rightIconBar)
			{
				if (!CurrentLog.cachedTextures.TryGetValue(iconObj.icon, out Texture2D texture))
				{
					texture = BaseContent.BadTex;
				}
				cachedRightIconBar.Add(new Tuple<string, string, Texture2D>(iconObj.name, iconObj.url, texture));
			}
		}
		if (!CurrentLog.UpdateData.leftIconBar.NullOrEmpty())
		{
			foreach (var iconObj in CurrentLog.UpdateData.leftIconBar)
			{
				if (!CurrentLog.cachedTextures.TryGetValue(iconObj.icon, out Texture2D texture))
				{
					texture = BaseContent.BadTex;
				}
				cachedLeftIconBar.Add(new Tuple<string, string, Texture2D>(iconObj.name, iconObj.url, texture));
			}
		}
	}

	private void RecacheHeight(Rect rect)
	{
		float height = 0;
		var font = Text.Font;
		var anchor = Text.Anchor;
		var color = GUI.color;

		foreach (DescriptionData data in segments)
		{
			if (data.tag is TaggedSegment tag)
			{
				height += tag.HeightOccupied(CurrentLog, data.text);
			}
			else
			{
				height += Text.CalcHeight(data.text, rect.width);
			}
		}
		height += Text.CalcHeight("BottomPadding", rect.width);

		GUI.color = color;
		Text.Anchor = anchor;
		Text.Font = font;
		cachedViewHeight = height;
	}

	public override void DoWindowContents(Rect inRect)
	{
		if (cachedHeightDirty)
		{
			RecacheHeight(inRect);
			cachedHeightDirty = false;
		}
		var anchor = Text.Anchor;
		Text.Anchor = TextAnchor.MiddleCenter;
		var font = Text.Font;
		Text.Font = GameFont.Medium;
		var color = GUI.color;

		Texture2D previewImage = metaData.PreviewImage;

		float pWidth = previewImage?.width ?? 0;
		float pHeight = previewImage?.height ?? 0;
		float imageWidth = ((float)pWidth / pHeight) * PreviewImageHeight;
		Rect previewRect = new Rect(inRect)
		{
			x = (inRect.width - imageWidth) / 2,
			height = PreviewImageHeight,
			width = imageWidth
		};
		if (previewImage != null)
		{
			GUI.DrawTexture(previewRect, previewImage);
		}

		Rect modLabelRect = new Rect(inRect)
		{
			y = previewRect.yMax + 5,
			height = Text.CalcHeight(mod.Name, inRect.width)
		};
		Widgets.Label(modLabelRect, mod.Name);

		Widgets.DrawLineHorizontal(0, modLabelRect.yMax, modLabelRect.width);

		Rect rightIconBarRect = new Rect(inRect.width - BarIconSize, modLabelRect.y, BarIconSize, BarIconSize);
		foreach (var barIcon in cachedRightIconBar)
		{
			if (!barIcon.Item2.NullOrEmpty())
			{
				if (Mouse.IsOver(rightIconBarRect))
				{
					GUI.color = GenUI.MouseoverColor;
					if (!barIcon.Item1.NullOrEmpty())
					{
						TooltipHandler.TipRegion(rightIconBarRect, barIcon.Item1);
					}
				}
				if (Widgets.ButtonInvisible(rightIconBarRect))
				{
					Application.OpenURL(barIcon.Item2);
				}
			}
			Widgets.DrawTextureFitted(rightIconBarRect, barIcon.Item3, 1);
			rightIconBarRect.x -= BarIconSize + 10;
			GUI.color = color;
		}

		Rect leftIconBarRect = new Rect(0, modLabelRect.y, BarIconSize, BarIconSize);
		foreach (var barIcon in cachedLeftIconBar)
		{
			if (!barIcon.Item2.NullOrEmpty())
			{
				if (Mouse.IsOver(leftIconBarRect))
				{
					GUI.color = GenUI.MouseoverColor;
					if (!barIcon.Item1.NullOrEmpty())
					{
						TooltipHandler.TipRegion(leftIconBarRect, barIcon.Item1);
					}
				}
				if (Widgets.ButtonInvisible(leftIconBarRect))
				{
					Application.OpenURL(barIcon.Item2);
				}
			}
			Widgets.DrawTextureFitted(leftIconBarRect, barIcon.Item3, 1);
			leftIconBarRect.x += BarIconSize + 10;
			GUI.color = color;
		}

		Rect lowerRect = new Rect(modLabelRect.x, modLabelRect.yMax, inRect.width,
			DialogHeightFinal - modLabelRect.yMax - Footer);
		Rect viewRect = new Rect(lowerRect.x, lowerRect.y, lowerRect.width - 20,
			Mathf.Max(cachedViewHeight, lowerRect.height));

		Widgets.BeginScrollView(lowerRect, ref scrollPosition, viewRect);
		lister.Begin(viewRect);

		Text.Anchor = TextAnchor.MiddleCenter;
		lister.RichText(versionSegment);

		Text.Anchor = TextAnchor.MiddleLeft;
		foreach (DescriptionData segment in segments)
		{
			if (segment.tag is TaggedSegment tag)
			{
				tag.SegmentAction(lister, segment.text);
			}
			else
			{
				lister.RichText(segment);
			}
		}
		lister.End();
		Widgets.EndScrollView();

		Rect bottomButtonsRect = new Rect(lowerRect.x, lowerRect.yMax, inRect.width, Footer);
		Text.Font = GameFont.Small;
		if (DrawPagination(bottomButtonsRect, ref selectedLogIndex, logs.Length))
		{
			CurrentLog = logs[selectedLogIndex];
		}
		Text.Anchor = anchor;
		Text.Font = font;
		GUI.color = color;
	}

	public static bool DrawPagination(Rect rect, ref int pageNumber, int pageCount)
	{
		++pageNumber; //Index to 1 base for correct pagination

		var font = Text.Font;
		var anchor = Text.Anchor;
		var color = GUI.color;
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleCenter;
		string leftArrow = "Previous";
		string rightArrow = "Next";
		bool clicked = false;
		Rect leftButtonRect = new Rect(rect.x, rect.y, Text.CalcSize(leftArrow).x, rect.height);
		float nextWidth = Text.CalcSize(rightArrow).x;
		Rect rightButtonRect = new Rect(rect.xMax - nextWidth, rect.y, nextWidth, rect.height);

		//Left Arrow
		if (Mouse.IsOver(leftButtonRect))
		{
			GUI.color = GenUI.MouseoverColor;
		}
		if (pageCount > 1)
		{
			Widgets.Label(leftButtonRect, leftArrow);
			if (Widgets.ButtonInvisible(leftButtonRect))
			{
				SoundDefOf.Click.PlayOneShotOnCamera();
				--pageNumber;
				pageNumber = Mathf.Clamp(pageNumber, 1, pageCount);
				clicked = true;
			}
		}
		GUI.color = color;

		//Right Arrow
		if (Mouse.IsOver(rightButtonRect))
		{
			GUI.color = GenUI.MouseoverColor;
		}
		if (pageCount > 1)
		{
			Widgets.Label(rightButtonRect, rightArrow);
			if (Widgets.ButtonInvisible(rightButtonRect))
			{
				SoundDefOf.Click.PlayOneShotOnCamera();
				++pageNumber;
				pageNumber = Mathf.Clamp(pageNumber, 1, pageCount);
				clicked = true;
			}
		}
		GUI.color = color;

		float numbersLength = rect.width - rect.height * 2f;
		int pageNumbersDisplayedTotal = Mathf.CeilToInt((numbersLength / 1.5f) / rect.width);
		int pageNumbersDisplayedHalf = Mathf.FloorToInt(pageNumbersDisplayedTotal / 2f);

		float pageNumberingOrigin = rect.x + rect.height + numbersLength / 2;
		Rect pageRect = new Rect(pageNumberingOrigin, rect.y, rect.height, rect.height);
		Widgets.ButtonText(pageRect, pageNumber.ToString(), false);

		Text.Font = GameFont.Tiny;
		int offsetRight = 1;
		for (int pageLeftDisplayNum = pageNumber + 1;
			pageLeftDisplayNum <= (pageNumber + pageNumbersDisplayedHalf) && pageLeftDisplayNum <= pageCount;
			pageLeftDisplayNum++, offsetRight++)
		{
			pageRect.x = pageNumberingOrigin + (numbersLength / pageNumbersDisplayedTotal * offsetRight);
			if (Widgets.ButtonText(pageRect, pageLeftDisplayNum.ToString(), false))
			{
				SoundDefOf.Click.PlayOneShotOnCamera();
				pageNumber = pageLeftDisplayNum;
				clicked = true;
			}
		}
		int offsetLeft = 1;
		for (int pageRightDisplayNum = pageNumber - 1;
			pageRightDisplayNum >= (pageNumber - pageNumbersDisplayedHalf) && pageRightDisplayNum >= 1;
			pageRightDisplayNum--, offsetLeft++)
		{
			pageRect.x = pageNumberingOrigin - (numbersLength / pageNumbersDisplayedTotal * offsetLeft);
			if (Widgets.ButtonText(pageRect, pageRightDisplayNum.ToString(), false))
			{
				SoundDefOf.Click.PlayOneShotOnCamera();
				pageNumber = pageRightDisplayNum;
				clicked = true;
			}
		}

		Text.Font = font;
		Text.Anchor = anchor;
		--pageNumber; //Correct first line back to 0 base
		return clicked;
	}
}