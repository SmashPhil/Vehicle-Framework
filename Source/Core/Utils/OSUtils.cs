using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace CoreLib;

[PublicAPI]
public static class OSUtils
{
  private const string FolderNameWindows = "Windows";
  private const string FolderNameLinux = "Linux";
  private const string FolderNameMac = "OSX";

  private static bool IsArmArchitecture =>
    RuntimeInformation.ProcessArchitecture is Architecture.Arm64;

  public static OSPlatform GetOSPlatform()
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      return OSPlatform.Windows;

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
      return OSPlatform.Linux;

    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      return OSPlatform.OSX;

    Logger.Error("Platform not detected.");
    return default;
  }

  public static string GetPlatformFolder(OSPlatform platform)
  {
    if (platform == OSPlatform.Windows)
      return FolderNameWindows;

    if (platform == OSPlatform.Linux)
      return FolderNameLinux;

    if (platform == OSPlatform.OSX)
      return FolderNameMac;

    return null;
  }

  public static string GetPlatformBinaryFileExt(OSPlatform platform)
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      return ".dll";
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
      return ".so";
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      return ".dylib";

    return null;
  }
}
