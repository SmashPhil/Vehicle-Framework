using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using SmashTools;

namespace AnimationKit;

[StaticConstructorOnModInit]
internal static class AnimationLoader
{
  [MustUseReturnValue]
  public static AnimationClip Load(string filePath)
  {
    IntPtr handle = LoadAnimationClip(filePath);
    return handle != IntPtr.Zero ? new AnimationClip(handle) : null;
  }

  public static bool Save(AnimationClip clip)
  {
    return SaveAnimationClip(clip.UnsafePtr);
  }

  [MustUseReturnValue]
  [DllImport("animation_kit", CallingConvention = CallingConvention.Cdecl)]
  private static extern IntPtr LoadAnimationClip(string filePath);

  [DllImport("animation_kit", CallingConvention = CallingConvention.Cdecl)]
  private static extern Boolean SaveAnimationClip(IntPtr clipPtr);
}
