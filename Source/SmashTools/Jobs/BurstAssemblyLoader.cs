using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Unity.Burst;
using UnityEngine.Assertions;
using Verse;
using static CoreLib.OSUtils;

namespace SmashTools.Burst;

// TODO 1.8 - Remove
[PublicAPI]
public static class BurstAssemblyLoader
{
  /// <summary>
  /// Loads burst compiled assemblies at runtime.
  /// </summary>
  /// <param name="mod">Mod to load burst assemblies for.</param>
  [Obsolete("Burst is not supported in Waylith", error: true)]
  public static bool LoadAllFor(ModContentPack mod)
  {
    // Not supported for 32bit builds
    if (!UnityData.Is64BitBuild)
      return false;

    string folderName = GetPlatformFolder(GetOSPlatform());
    if (folderName.NullOrEmpty())
    {
      if (Prefs.DevMode)
      {
        Log.Warning($"{RuntimeInformation.OSDescription} is not supported for burst lib.");
      }
      return false;
    }

    bool anyLoaded = false;
    string folderPath = Path.Combine("Burst", folderName);
    List<string> folders = mod.foldersToLoadDescendingOrder;
    for (int i = folders.Count; --i >= 0;)
    {
      string folder = folders[i];
      DirectoryInfo directoryInfo = new(Path.Combine(folder, folderPath));
      if (!directoryInfo.Exists)
        continue;

      foreach (FileInfo fileInfo in directoryInfo.EnumerateFiles("*.*", SearchOption.AllDirectories))
      {
        try
        {
          Assert.IsTrue(IsValidFileExtension(fileInfo.Extension.ToLowerInvariant()));
          if (!BurstRuntime.LoadAdditionalLibrary(fileInfo.FullName))
          {
            Log.Error($"Unable to load file {fileInfo.Name} into BurstRuntime.");
            return false;
          }
          anyLoaded = true;
          if (Prefs.LogVerbose)
          {
            Log.Message($"[{mod.Name}] Loading {fileInfo.Name}");
          }
        }
        catch (Exception ex)
        {
          Log.Error($"Exception thrown loading {fileInfo.Name} into BurstRuntime.\n{ex}");
        }
      }
    }
    return anyLoaded;
  }

  [Obsolete("Burst is not supported in Waylith", error: true)]
  public static bool IsValidFileExtension(string extension)
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      return extension == ".dll";
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
      return extension == ".so";
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      return extension == ".bundle";

    return false;
  }
}