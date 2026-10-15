using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SmashTools
{
	public class Dialog_FilePicker : Window
	{
		private float EntryHeight = 24;
		private float IndentSpace = 24;

		private (string text, Action<DirectoryInfo> onClick) confirmBtn;
		private string rootDir;

		private DirectoryEntry selectedEntry;
		private float height = 0;
		private Vector2 scrollPos;

		public Dialog_FilePicker((string text, Action<DirectoryInfo> onClick) confirmBtn,
			string rootDir = null)
		{
			this.confirmBtn = confirmBtn;
			this.rootDir = rootDir;

			this.layer = WindowLayer.Super;
			this.closeOnAccept = false;
			this.closeOnClickedOutside = false;
			this.doWindowBackground = false;
			this.drawShadow = false;
			this.forcePause = false;
			this.onlyOneOfTypeAllowed = true;
			this.preventCameraMotion = false;
			this.resizeable = true;
		}

		public override Vector2 InitialSize => new Vector2(640, 640);

		protected override float Margin => 0;

		private DirectoryInfo RootDirectory { get; set; }

		private DirectoryEntry RootEntry { get; set; }

		public override void PreOpen()
		{
			base.PreOpen();
			if (!Directory.Exists(rootDir))
			{
				rootDir = GenFilePaths.ModsFolderPath;
			}
			RootDirectory = new DirectoryInfo(rootDir);
			RootEntry = new DirectoryEntry(RootDirectory);
			RootEntry.Expanded = true;
		}

		private void GetSubDirectories(DirectoryEntry entry)
		{
			entry.Fetched = true;
			try
			{
				DirectoryInfo[] subDirs =
					entry.directory.GetDirectories("*", SearchOption.TopDirectoryOnly);
				foreach (DirectoryInfo subDir in subDirs)
				{
					DirectoryEntry subEntry = new DirectoryEntry(subDir);
					entry.subDirectories.Add(subEntry);
				}
			}
			catch (UnauthorizedAccessException)
			{
				entry.Unauthorized = true;
			}
		}

		public override void DoWindowContents(Rect inRect)
		{
			using var textBlock = new TextBlock(Color.white);

			Widgets.DrawMenuSection(inRect);

			Rect rect = inRect.ContractedBy(5);
			rect.yMax -= 40;
			Widgets.DrawLineHorizontal(rect.x, rect.yMax, rect.width,
				UIElements.MenuSectionBgBorderColor);

			Rect outRect = rect;
			Rect viewRect = new Rect(outRect.x, outRect.y, outRect.width - 16, height);
			Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);

			Rect rowRect = rect.AtZero();
			rowRect.height = EntryHeight;
			height = DoRow(ref rowRect, RootEntry);

			Widgets.EndScrollView();


			Rect buttonRect = new Rect(inRect.xMax - 105, inRect.yMax - 40, 90, 30);
			if (Widgets.ButtonText(buttonRect, "Cancel".Translate()))
			{
				SoundDefOf.Click.PlayOneShotOnCamera();
				Close();
			}
			buttonRect.x -= buttonRect.width + 10;
			if (Widgets.ButtonText(buttonRect, confirmBtn.text))
			{
				if (selectedEntry == null)
				{
					SoundDefOf.ClickReject.PlayOneShotOnCamera();
				}
				else
				{
					confirmBtn.onClick(selectedEntry.directory);
					Close();
				}
			}
		}

		private float DoRow(ref Rect rect, DirectoryEntry entry)
		{
			if (!entry.Fetched)
			{
				GetSubDirectories(entry);
			}
			bool expanded = entry.Expanded;
			Rect collapseBtnRect = new Rect(rect.x, rect.y, rect.height, rect.height);
			if (!entry.Unauthorized && entry.subDirectories.Count > 0 &&
				UIElements.CollapseButton(collapseBtnRect.ContractedBy(2), ref expanded))
			{
				entry.Expanded = expanded;
				PlayTabSound(entry.Expanded);
			}

			Text.Anchor = TextAnchor.MiddleLeft;
			Rect labelRect = rect;
			labelRect.xMin = collapseBtnRect.xMax;
			string label = !entry.Unauthorized ?
				entry.directory.Name :
				$"{entry.directory.Name} (UNAUTHORIZED)";
			if (Widgets.ButtonText(labelRect, label, drawBackground: false, doMouseoverSound: false))
			{
				if (selectedEntry == entry)
				{
					entry.Expanded = !entry.Expanded;
					PlayTabSound(entry.Expanded);
				}
				selectedEntry = entry;
			}
			if (selectedEntry == entry)
			{
				Widgets.DrawBoxSolid(rect, Widgets.HighlightTextBgColor);
			}

			float extraHeight = 0;
			if (entry.Expanded)
			{
				rect.xMin += IndentSpace;
				foreach (DirectoryEntry subEntry in entry.subDirectories)
				{
					rect.y += EntryHeight;
					extraHeight += DoRow(ref rect, subEntry);
				}
				rect.xMin -= IndentSpace;
			}
			return EntryHeight + extraHeight;

			static void PlayTabSound(bool open)
			{
				if (open)
				{
					SoundDefOf.TabOpen.PlayOneShotOnCamera();
				}
				else
				{
					SoundDefOf.TabClose.PlayOneShotOnCamera();
				}
			}
		}

		private class DirectoryEntry
		{
			public readonly DirectoryInfo directory;
			public readonly List<DirectoryEntry> subDirectories = new List<DirectoryEntry>();

			public DirectoryEntry(DirectoryInfo directory)
			{
				this.directory = directory;
			}

			public bool Expanded { get; set; }

			public bool Unauthorized { get; set; }

			public bool Fetched { get; set; }
		}
	}
}