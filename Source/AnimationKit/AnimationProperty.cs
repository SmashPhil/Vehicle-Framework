using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AnimationKit.Editor;
using CoreLib;
using UnityEngine;

namespace AnimationKit;

internal sealed class AnimationProperty : ISelectableUI, IEnumerable<KeyFrame>
{
  private int version = 0;

  public AnimationProperty(string name, Ref<IntPtr>.ReadOnly handle)
  {
    Name = name;
    UnsafePtr = handle;
  }

  public string Name { get; }

  private Ref<IntPtr>.ReadOnly UnsafePtr { get; }

  public bool Destroyed => UnsafePtr.Value == IntPtr.Zero;

  public ulong KeyframeCount { get; private set; }

  public Color Color { get; set; }

  public UInt2 FrameBounds
  {
    get
    {
      NativeObjectGuard.ThrowIfDisposed(Destroyed);
      return GetFrameBounds(UnsafePtr);
    }
  }
  private void UpdateCount()
  {
    NativeObjectGuard.ThrowIfDisposed(Destroyed);
    KeyframeCount = Keyframes(UnsafePtr);
  }

  public Parameter GetValue(float time)
  {
    NativeObjectGuard.ThrowIfDisposed(Destroyed);
    return GetValue(UnsafePtr, time);
  }

  public void SetValue(uint frame, float value)
  {
    NativeObjectGuard.ThrowIfDisposed(Destroyed);
    version++;
    SetValue(UnsafePtr, frame, value);
    UpdateCount();
  }

  public void RemoveFrame(uint frame)
  {
    NativeObjectGuard.ThrowIfDisposed(Destroyed);
    version++;
    RemoveFrame(UnsafePtr, frame);
    UpdateCount();
  }

  public KeyFrame KeyFrameAt(uint frame)
  {
    NativeObjectGuard.ThrowIfDisposed(Destroyed);
    var result = KeyframeAt(UnsafePtr, frame);
    return result.error == ErrorType.NoError ? result.keyFrame : KeyFrame.Invalid;
  }

  public KeyFrame KeyFrameAtIndex(uint index)
  {
    NativeObjectGuard.ThrowIfDisposed(Destroyed);
    return KeyframeAtIndex(UnsafePtr, index);
  }

  public bool AnyKeyFrameAt(uint frame)
  {
    NativeObjectGuard.ThrowIfDisposed(Destroyed);
    var result = KeyframeAt(UnsafePtr, frame);
    return result.error == ErrorType.NoError;
  }

  IEnumerator<KeyFrame> IEnumerable<KeyFrame>.GetEnumerator()
  {
    return GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }

  public Enumerator GetEnumerator()
  {
    return new Enumerator(this);
  }

  [DllImport("animation_kit", EntryPoint = "GetAnimationPropertyValue", CallingConvention = CallingConvention.Cdecl)]
  private static extern Parameter GetValue(IntPtr propertyPtr, float time);

  [DllImport("animation_kit", EntryPoint = "SetAnimationPropertyValue", CallingConvention = CallingConvention.Cdecl)]
  private static extern void SetValue(IntPtr propertyPtr, uint frame, float value);

  [DllImport("animation_kit", EntryPoint = "RemoveAnimationPropertyValue", CallingConvention = CallingConvention.Cdecl)]
  private static extern Boolean RemoveFrame(IntPtr propertyPtr, uint frame);

  [DllImport("animation_kit", EntryPoint = "AnimationPropertyKeyframes", CallingConvention = CallingConvention.Cdecl)]
  private static extern ulong Keyframes(IntPtr propertyPtr);

  [DllImport("animation_kit", EntryPoint = "AnimationPropertyKeyframeAt", CallingConvention = CallingConvention.Cdecl)]
  private static extern KeyFrame.Result KeyframeAt(IntPtr propertyPtr, uint frame);

  [DllImport("animation_kit", EntryPoint = "AnimationPropertyKeyframeAtIndex", CallingConvention = CallingConvention.Cdecl)]
  private static extern KeyFrame KeyframeAtIndex(IntPtr propertyPtr, uint frame);

  [DllImport("animation_kit", EntryPoint = "AnimationPropertyFrameBounds", CallingConvention = CallingConvention.Cdecl)]
  private static extern UInt2 GetFrameBounds(IntPtr propertyPtr);

  public struct Enumerator(AnimationProperty property) : IEnumerator<KeyFrame>
  {
    private readonly int version = property.version;

    private uint index;

    public KeyFrame Current { get; private set; }

    object IEnumerator.Current => Current;

    public void Dispose() => throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfVersionMismatch()
    {
      if (version != property.version)
      {
        throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
      }
    }

    public bool MoveNext()
    {
      ThrowIfVersionMismatch();
      if (index < property.KeyframeCount)
      {
        Current = property.KeyFrameAt(index);
        index++;
        return true;
      }
      index = 0;
      Current = default;
      return false;
    }

    void IEnumerator.Reset()
    {
      ThrowIfVersionMismatch();
      index = 0;
      Current = default;
    }
  }
}
