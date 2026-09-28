using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CoreLib;
using CoreLib.Collections;
using JetBrains.Annotations;

namespace AnimationKit;

internal sealed class AnimationClip : IDisposable
{
  private List<AnimationPropertyGroup> groups = [];
  private readonly Ref<IntPtr> handle;

  public AnimationClip()
  {
    handle = new Ref<IntPtr> { Value = CreateAnimationClip() };
  }

  public AnimationClip(IntPtr handle)
  {
    this.handle = new Ref<IntPtr> { Value = handle };
  }

  ~AnimationClip()
  {
    Dispose(disposing: false);
  }

  public bool Disposed => UnsafePtr == IntPtr.Zero;

  internal IntPtr UnsafePtr => handle?.Value ?? IntPtr.Zero;

  public ReadOnlyList<AnimationPropertyGroup> PropertyGroups => new(groups);

  public string FileName
  {
    get => GetFileName(UnsafePtr);
    set => SetFileName(UnsafePtr, value);
  }

  public string FilePath
  {
    get => GetFilePath(UnsafePtr);
    set => SetFilePath(UnsafePtr, value);
  }

  public uint FrameCount
  {
    get => GetFrameCount(UnsafePtr);
    set => SetFrameCount(UnsafePtr, value);
  }

  public List<AnimationEvent> Events
  {
    get => []; // TODO
  }

  public void RemoveProperty(AnimationPropertyGroup group)
  {
    group.RemoveAll();
  }

  private void Dispose(bool disposing)
  {
    if (Disposed)
      return;

    DeleteAnimationClip(UnsafePtr);
    handle.Value = IntPtr.Zero;
  }

  public void Dispose()
  {
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }

  [MustUseReturnValue]
  [DllImport("animation_kit", CallingConvention = CallingConvention.Cdecl)]
  private static extern IntPtr CreateAnimationClip();

  [DllImport("animation_kit", CallingConvention = CallingConvention.Cdecl)]
  private static extern void DeleteAnimationClip(IntPtr clipPtr);

  [DllImport("animation_kit", EntryPoint = "AnimationClipGetFrameCount", CallingConvention = CallingConvention.Cdecl)]
  private static extern uint GetFrameCount(IntPtr clipPtr);

  [DllImport("animation_kit", EntryPoint = "AnimationClipSetFrameCount", CallingConvention = CallingConvention.Cdecl)]
  private static extern void SetFrameCount(IntPtr clipPtr, uint frameCount);

  [DllImport("animation_kit", EntryPoint = "AnimationClipGetFileName", CallingConvention = CallingConvention.Cdecl)]
  private static extern string GetFileName(IntPtr clipPtr);

  [DllImport("animation_kit", EntryPoint = "AnimationClipSetFileName", CallingConvention = CallingConvention.Cdecl)]
  private static extern void SetFileName(IntPtr clipPtr, string fileName);

  [DllImport("animation_kit", EntryPoint = "AnimationClipGetFilePath", CallingConvention = CallingConvention.Cdecl)]
  private static extern string GetFilePath(IntPtr clipPtr);

  [DllImport("animation_kit", EntryPoint = "AnimationClipSetFilePath", CallingConvention = CallingConvention.Cdecl)]
  private static extern void SetFilePath(IntPtr clipPtr, string filePath);
}
