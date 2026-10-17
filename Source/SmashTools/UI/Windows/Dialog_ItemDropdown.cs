using RimWorld;
using SmashTools.Animations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public abstract class Dialog_ItemDropdown<T> : Window
	{
		private const int MaxCountShown = 6;
		private const float EntryHeight = 30;
		private const float Padding = 2;
		private const float LabelPadding = 3;

		private readonly Color highlightColor = new ColorInt(150, 150, 150, 40).ToColor;

		private readonly float width;
		private readonly Action<T> onItemPicked;
		private readonly Func<T, string> itemName;
		private readonly Func<T, string> itemTooltip;
		private readonly Func<T, bool> isSelected;
		private readonly CreateItemButton createItem;

		private QuickSearchFilter filter = new QuickSearchFilter();
		private Vector2 windowSize;
		private Vector2 position;
		private Vector2 scrollPos;
		private float fullHeight;

		private readonly List<T> items;

		public Dialog_ItemDropdown(Rect rect, List<T> items, Action<T> onItemPicked, Func<T, string> itemName, Func<T, bool> isSelected, Func<T, string> itemTooltip = null,
			CreateItemButton createItem = null)
		{
			this.items = items;
			this.width = rect.width;
			this.position = rect.position;
			this.onItemPicked = onItemPicked;
			this.itemName = itemName;
			this.itemTooltip = itemTooltip;
			this.isSelected = isSelected;

			this.createItem = createItem;

			this.closeOnClickedOutside = true;
			this.absorbInputAroundWindow = false;
			this.preventCameraMotion = false;
			this.doWindowBackground = false;
			this.layer = WindowLayer.Super;

			this.resizeable = ShowSearchBox;
		}

		public override Vector2 InitialSize => windowSize;

		protected override float Margin => 0;

		private bool ShowSearchBox => items.Count > MaxCountShown;

		private float SearchBoxHeight => ShowSearchBox ? EntryHeight : 0;

		private float CreateButtonHeight => createItem != null ? EntryHeight + LabelPadding * 3 : 0;

		private float ResizeableBtnHeight => ShowSearchBox ? EntryHeight : 0;

		public override void PreOpen()
		{
			items.SortBy(item => itemName(item));
			windowSize = CalculateWindowSize();
			base.PreOpen();
		}

		public override void Notify_ClickOutsideWindow()
		{
			base.Notify_ClickOutsideWindow();
			Close();
		}

		private Vector2 CalculateWindowSize()
		{
			int count = Mathf.Min(MaxCountShown, items.Count);
			RecacheHeight();
			return new Vector2(width, EntryHeight * count + CreateButtonHeight + SearchBoxHeight + ResizeableBtnHeight);
		}

		private void RecacheHeight()
		{
			int count = 0;
			foreach (T item in items)
			{
				if (!ShowSearchBox || filter.Matches(itemName(item)))
				{
					count++;
				}
			}
			fullHeight = EntryHeight * count + CreateButtonHeight;
		}

		protected override void SetInitialSizeAndPosition()
		{
			if (position.x + InitialSize.x > UI.screenWidth)
			{
				position.x = UI.screenWidth - InitialSize.x;
			}
			if (position.y + InitialSize.y > UI.screenHeight)
			{
				position.y = UI.screenHeight - InitialSize.y;
			}
			windowRect = new Rect(position.x, position.y, InitialSize.x, InitialSize.y);
		}

		public override void DoWindowContents(Rect inRect)
		{
			Widgets.DrawMenuSection(inRect);

			inRect = inRect.ContractedBy(Padding);
			using var textFont = new TextBlock(GameFont.Small);

			Rect rowRect = new Rect(inRect.x, inRect.y, inRect.width, EntryHeight);
			if (ShowSearchBox)
			{
				string text = Widgets.TextField(rowRect.ContractedBy(2), filter.Text);
				if (text != filter.Text)
				{
					filter.Text = text;
					RecacheHeight();
				}
				rowRect.y += rowRect.height;
			}

			int columns = Mathf.FloorToInt(inRect.width / width);

			Rect outRect = new Rect(inRect.x, inRect.y + SearchBoxHeight, inRect.width, inRect.height - SearchBoxHeight);
			Rect viewRect = new Rect(outRect.x, outRect.y, outRect.width - 16, fullHeight - Padding * 2);
			Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);

			for (int i = 0; i < items.Count; i++)
			{
				T item = items[i];
				if (ShowSearchBox && !filter.Matches(itemName(item)))
				{
					continue;
				}

				Rect entryRect = rowRect.ContractedBy(3);
				entryRect.SplitVertically(EntryHeight, out Rect leftRect, out Rect fileLabelRect);

				Rect checkboxRect = new Rect(leftRect.x, leftRect.y, leftRect.height, leftRect.height).ContractedBy(LabelPadding);
				if (isSelected(item))
				{
					GUI.DrawTexture(checkboxRect, Widgets.CheckboxOnTex);
				}
				if (Widgets.ButtonText(fileLabelRect, itemName(item), drawBackground: false))
				{
					onItemPicked.Invoke(item);
					Close();
				}

				if (Mouse.IsOver(entryRect))
				{
					Widgets.DrawBoxSolid(fileLabelRect, highlightColor);
					if (itemTooltip != null)
					{
						TooltipHandler.TipRegion(fileLabelRect, itemTooltip(item));
					}
				}
				rowRect.y += rowRect.height;
			}
			
			if (createItem != null)
			{
				Rect createItemRect = rowRect.ContractedBy(LabelPadding);
				createItemRect.SplitVertically(EntryHeight, out Rect _, out Rect createItemBtnRect);

				if (!items.NullOrEmpty())
				{
					UIElements.DrawLineHorizontalGrey(createItemBtnRect.x, createItemBtnRect.y, createItemBtnRect.width);
					createItemBtnRect.y += LabelPadding + 1;
				}
				if (Widgets.ButtonText(createItemBtnRect, createItem.labelKey.Translate(), drawBackground: false))
				{
					T item = createItem.onClick();
					onItemPicked?.Invoke(item);
					Close();
				}
				if (Mouse.IsOver(createItemBtnRect))
				{
					Widgets.DrawBoxSolid(createItemBtnRect, highlightColor);
				}
			}
			Widgets.EndScrollView();
		}

		public class CreateItemButton
		{
			public readonly string labelKey;
			public readonly Func<T> onClick;

			public CreateItemButton(string labelKey, Func<T> onClick)
			{
				this.labelKey = labelKey;
				this.onClick = onClick;
			}

			public static implicit operator CreateItemButton((string labelKey, Func<T> onClick) tuple)
			{
				return new CreateItemButton(tuple.labelKey, tuple.onClick);
			}
		}
	}
}
