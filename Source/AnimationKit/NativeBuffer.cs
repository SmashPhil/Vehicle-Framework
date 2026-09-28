using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace AnimationKit;

[PublicAPI]
public sealed class NativeBuffer : IDisposable
{
  private unsafe float* buffer;

  public NativeBuffer(uint length)
  {
    Length = length;
    UnsafePtr = CreateNativeBuffer(Length);
    if (UnsafePtr != IntPtr.Zero)
    {
      unsafe
      {
        buffer = GetBufferData(UnsafePtr);
      }
    }
  }

  ~NativeBuffer()
  {
    Dispose(disposing: false);
  }

  public bool Disposed => UnsafePtr == IntPtr.Zero;

  public uint Length { get; }

  private IntPtr UnsafePtr { get; set; }

  public float this[int index]
  {
    get
    {
      NativeObjectGuard.ThrowIfDisposed(UnsafePtr);
      unsafe
      {
        return buffer[index];
      }
    }
  }

  private void Dispose(bool disposing)
  {
    if (Disposed)
      return;

    DestroyNativeBuffer(UnsafePtr);
    UnsafePtr = IntPtr.Zero;
    unsafe
    {
      buffer = null;
    }
  }

  public void Dispose()
  {
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }

  [MustUseReturnValue]
  [DllImport("animation_kit", CallingConvention = CallingConvention.Cdecl)]
  private static extern IntPtr CreateNativeBuffer(uint size);

  [DllImport("animation_kit", CallingConvention = CallingConvention.Cdecl)]
  private static extern void DestroyNativeBuffer(IntPtr bufferPtr);

  [DllImport("animation_kit", CallingConvention = CallingConvention.Cdecl)]
  private static extern unsafe float* GetBufferData(IntPtr bufferPtr);
}
