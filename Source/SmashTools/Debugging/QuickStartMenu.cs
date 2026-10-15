using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace SmashTools
{
	public enum QuickStartOption
	{
		None,
		QuickStart_Load,
		QuickStart_New,
		QuickStart_Preset
	}

	public class QuickStartMenu : Window
	{
		public const float RowSize = 24;

		private static List<string> presets = new List<string>();
		private static Listing_Standard lister = new Listing_Standard();

		public override Vector2 InitialSize => new Vector2(500, 500);

		public QuickStartMenu() : base()
		{
			closeOnClickedOutside = true;
			closeOnCancel = true;
			closeOnAccept = true;
			doCloseX = true;
		}

		static QuickStartMenu()
		{
			LongEventHandler.ExecuteWhenFinished(QuickStart);
		}

		public static void RegisterPreset()
		{
			//register presets here for quick start
		}

		public override void PostClose()
		{
			base.PostClose();
			SmashMod.Serialize();
		}

		public override void DoWindowContents(Rect inRect)
		{
			lister.Begin(inRect);
			{
				QuickStartRow("None", QuickStartOption.None);
				//Rect rect = QuickStartRow("Load Save", QuickStartOption.QuickStart_Load, delegate()
				//{
				//	List<FloatMenuOption> options = new List<FloatMenuOption>();
				//	GenFilePaths.AllSavedGameFiles.ForEach(fileInfo => options.Add(new FloatMenuOption(Path.GetFileNameWithoutExtension(fileInfo.Name), delegate ()
				//	{
				//		SmashSettings.quickStartFile = Path.GetFileNameWithoutExtension(fileInfo.Name);
				//	})));
				//});
				//if (SmashSettings.quickStartOption == QuickStartOption.QuickStart_Load)
				//{
				//	Widgets.Label(rect, SmashSettings.quickStartFile);
				//}
				QuickStartRow("New Game", QuickStartOption.QuickStart_New);
				//if (Widgets.RadioButtonLabeled(rowRect, "Preset", option == QuickStartOption.QuickStart_Preset))
				//{
				//	option = QuickStartOption.QuickStart_Preset;
				//}
			}
			lister.End();
		}

		private static Rect QuickStartRow(string label, QuickStartOption quickStartOption, Action onClick = null)
		{
			Rect rect = lister.GetRect(Text.LineHeight);
			if (lister.ReverseRadioButton(label, SmashSettings.quickStartOption == quickStartOption))
			{
				SmashSettings.quickStartOption = quickStartOption;
				onClick?.Invoke();
			}
			//lister.Gap(12);

			return rect;
		}

		private static void QuickStart()
		{
			switch (SmashSettings.quickStartOption)
			{
				case QuickStartOption.None:
					break;
				case QuickStartOption.QuickStart_New:
					{
						LongEventHandler.QueueLongEvent(delegate ()
						{
							Root_Play.SetupForQuickTestPlay();
							PageUtility.InitGameStart();
						}, "GeneratingMap", true, new Action<Exception>(GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap), true);
					}
					break;
				case QuickStartOption.QuickStart_Load:
					{
						FileInfo quickStartFile = GenFilePaths.AllSavedGameFiles.FirstOrDefault((FileInfo fileInfo) => Path.GetFileNameWithoutExtension(fileInfo.Name).ToLower() == SmashSettings.quickStartFile);
						if (quickStartFile != null)
						{
							GameDataSaveLoader.LoadGame(quickStartFile);
						}
					}
					break;
				case QuickStartOption.QuickStart_Preset:
					{

					}
					break;
			}
		}
	}
}
