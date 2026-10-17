using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Verse;
using SmashTools.Performance;

namespace SmashTools
{
	public class SmashSettings : IExposable
	{
		public static string startupAction;
		public static QuickStartOption quickStartOption = QuickStartOption.None;
		public static string quickStartFile;
		public static HashSet<string> profileAssemblies = new HashSet<string>();

		public static string FullPath => Path.Combine(GenFilePaths.ConfigFolderPath, "SmashTools.xml");

		public void ExposeData()
		{
#if DEBUG
			Scribe_Values.Look(ref startupAction, nameof(startupAction));
			Scribe_Values.Look(ref quickStartOption, nameof(quickStartOption), QuickStartOption.None);
			Scribe_Values.Look(ref quickStartFile, nameof(quickStartFile));
			Scribe_Collections.Look(ref profileAssemblies, nameof(profileAssemblies));
#endif
		}
	}

	public class SmashMod : Mod
	{
		public static SmashSettings settings;
		public static SmashMod mod;

		public SmashMod(ModContentPack modContentPack) : base(modContentPack)
		{
			mod = this;
			settings = new SmashSettings();
		}

		public static void Serialize()
		{
			Scribe.saver.InitSaving(SmashSettings.FullPath, "SmashSettings");
			try
			{
				Scribe_Deep.Look(ref settings, "SmashSettings");
			}
			finally
			{
				Scribe.saver.FinalizeSaving();
			}
		}

		public static void LoadFromSettings()
		{
			if (File.Exists(SmashSettings.FullPath))
			{
				Scribe.loader.InitLoading(SmashSettings.FullPath);
				try
				{
					Scribe_Deep.Look(ref settings, "SmashSettings");
				}
				finally
				{
					Scribe.loader.FinalizeLoading();
				}
			}
		}
	}
}
