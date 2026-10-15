using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using CoreLib;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Verse;
using Waylith.Logging;
using NativeLibMap = System.Collections.Generic.Dictionary<string, System.IntPtr>;

namespace SmashTools;

[PublicAPI]
public static class DllInjector
{
  private const string NativeAssemblyFolder = "Assemblies-Native";

  private static NativeLibMap nativeLibs = [];
  private static Dictionary<Assembly, NativeLibMap> bindings = [];

  public static bool IsValidFileExtension(string extension)
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      return extension == ".dll";
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
      return extension == ".so";
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      return extension == ".dylib";

    return false;
  }

  public static bool LoadNative(ModContentPack mod, string name, params Assembly[] assembliesToBind)
  {
    OSPlatform platform = OSUtils.GetOSPlatform();
    string folderName = OSUtils.GetPlatformFolder(platform);
    if (folderName.NullOrEmpty())
    {
      if (Prefs.DevMode)
      {
        Log.Warning($"{RuntimeInformation.OSDescription} is not supported for dll injection.");
      }
      return false;
    }
    foreach (string folder in mod.foldersToLoadDescendingOrder)
    {
      string folderPath = Path.Combine(mod.RootDir, folder, NativeAssemblyFolder);
      if (!Directory.Exists(folderPath))
        continue;

      string fullPath = $"{Path.Combine(folderPath, folderName, name)}{OSUtils.GetPlatformBinaryFileExt(platform)}";
      FileInfo file = new(fullPath);
      if (!file.Exists)
        continue;

      bool loaded = LoadDllFromPath(file, out IntPtr handle);
      if (loaded)
      {
        nativeLibs[file.FullName] = handle;
        // Dll's won't live adjacent to RimWorld executable so setting custom resolver is a must.
        BindImportName(name, file.FullName, assembliesToBind);
      }
      return loaded;
    }
    return false;
  }

  private static bool LoadDllFromPath(FileInfo file, out IntPtr handle)
  {
    handle = IntPtr.Zero;
    Assert.IsTrue(IsValidFileExtension(file.Extension.ToLowerInvariant()));
    if (nativeLibs.ContainsKey(file.FullName))
    {
      Trace.Fail($"Trying to load already loaded native lib \"{file.FullName}\".");
      return false;
    }
    VerboseLog.Log($"Loading native library {file.Name} at \"{file.FullName}\"");
    return NativeLibrary.TryLoad(file.FullName, out handle);
  }

  public static void BindImportName(string importName, string filePath, Assembly[] assembliesToBind)
  {
    if (!nativeLibs.TryGetValue(filePath, out IntPtr handle))
    {
      Trace.Fail($"Native lib is not loaded: \"{filePath}\"");
      return;
    }

    foreach (Assembly assembly in assembliesToBind)
    {
      if (!bindings.TryGetValue(assembly, out NativeLibMap map))
      {
        NativeLibrary.SetDllImportResolver(assembly, ResolveImport);
        bindings[assembly] = map = [];
      }
      map[importName] = handle;
    }
  }

  private static IntPtr ResolveImport(string importName, Assembly assembly, DllImportSearchPath? searchPath)
  {
    return bindings.TryGetValue(assembly, out NativeLibMap map) && map.TryGetValue(importName, out IntPtr handle) ?
      handle :
      IntPtr.Zero;
  }
}
