using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using UnityEngine;

namespace AnimationKit;

public sealed class Transform : IDisposable
{
  private IntPtr handle;

  public Transform()
  {
    handle = CreateNativeTransform();
  }

  ~Transform()
  {
    Dispose(disposing: false);
  }

  public bool Disposed => handle == IntPtr.Zero;

  public Vector3 Position
  {
    get
    {
      NativeObjectGuard.ThrowIfDisposed(handle);
      GetPositionInjected(handle, out Vector3 position);
      return position;
    }
    set
    {
      NativeObjectGuard.ThrowIfDisposed(handle);
      SetPositionInjected(handle, in value);
    }
  }

  public Quaternion Rotation
  {
    get
    {
      NativeObjectGuard.ThrowIfDisposed(handle);
      GetRotationInjected(handle, out Quaternion rotation);
      return rotation;
    }
    set
    {
      NativeObjectGuard.ThrowIfDisposed(handle);
      SetRotationInjected(handle, in value);
    }
  }

  public Vector3 Scale
  {
    get
    {
      NativeObjectGuard.ThrowIfDisposed(handle);
      GetScaleInjected(handle, out Vector3 scale);
      return scale;
    }
    set
    {
      NativeObjectGuard.ThrowIfDisposed(handle);
      SetScaleInjected(handle, in value);
    }
  }

  private void Dispose(bool disposing)
  {
    if (Disposed)
      return;

    if (disposing)
    {
      // TODO: dispose managed state (managed objects)
    }
    DeleteNativeTransform(handle);
    handle = IntPtr.Zero;
  }

  public void Dispose()
  {
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }

  [MustUseReturnValue]
  [DllImport("animation_kit.dll", CallingConvention = CallingConvention.Cdecl)]
  private static extern IntPtr CreateNativeTransform();

  [DllImport("animation_kit.dll", CallingConvention = CallingConvention.Cdecl)]
  private static extern void DeleteNativeTransform(IntPtr ptr);

  [DllImport("animation_kit.dll", CallingConvention = CallingConvention.Cdecl)]
  private static extern void GetPositionInjected(IntPtr ptr, out Vector3 position);

  [DllImport("animation_kit.dll", CallingConvention = CallingConvention.Cdecl)]
  private static extern void SetPositionInjected(IntPtr ptr, ref readonly Vector3 position);

  [DllImport("animation_kit.dll", CallingConvention = CallingConvention.Cdecl)]
  private static extern void GetRotationInjected(IntPtr ptr, out Quaternion rotation);

  [DllImport("animation_kit.dll", CallingConvention = CallingConvention.Cdecl)]
  private static extern void SetRotationInjected(IntPtr ptr, ref readonly Quaternion rotation);

  [DllImport("animation_kit.dll", CallingConvention = CallingConvention.Cdecl)]
  private static extern void GetScaleInjected(IntPtr ptr, out Vector3 scale);

  [DllImport("animation_kit.dll", CallingConvention = CallingConvention.Cdecl)]
  private static extern void SetScaleInjected(IntPtr ptr, ref readonly Vector3 scale);
}