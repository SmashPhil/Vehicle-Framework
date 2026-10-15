using System.IO;
using System.Xml;
using RimWorld.IO;
using Verse;

namespace SmashTools.Performance;

internal static class LanguageLoader
{
	public static void LoadMetaData(ref LoadedLanguage language)
	{
		//if (language.info != null && language.infoIsRealMetadata)
		//	return;

		//language.infoIsRealMetadata = true;
		//foreach (ModContentPack modContentPack in LoadedModManager.RunningMods)
		//{
		//	foreach (string path in modContentPack.foldersToLoadDescendingOrder)
		//	{
		//		string filePath = Path.Combine(path, LoadedLanguage.LanguagesFolderName);
		//		if (!Directory.Exists(filePath))
		//			continue;

		//		LoadDirectory(in language, filePath);
		//	}
		//}
	}

	private static void LoadDirectory(ref readonly LoadedLanguage language, string filePath)
	{
		const string LanguageFile = "LanguageInfo.xml";
		const string FriendlyFile = "FriendlyName.txt";

		foreach (VirtualDirectory virtualDirectory in AbstractFilesystem.GetDirectories(filePath, "*",
			SearchOption.TopDirectoryOnly))
		{
			if (virtualDirectory.Name == language.folderName || virtualDirectory.Name == language.LegacyFolderName)
			{
				language.info =
					DirectXmlLoader.ItemFromXmlFile<LanguageInfo>(virtualDirectory, LanguageFile, false);
				if (language.info.friendlyNameNative.NullOrEmpty() && virtualDirectory.FileExists(FriendlyFile))
				{
					language.info.friendlyNameNative = virtualDirectory.ReadAllText(FriendlyFile);
				}
				if (language.info.friendlyNameNative.NullOrEmpty())
				{
					language.info.friendlyNameNative = language.folderName;
				}
				if (language.info.friendlyNameEnglish.NullOrEmpty())
				{
					language.info.friendlyNameEnglish = language.folderName;
				}
				return;
			}
		}
	}
}