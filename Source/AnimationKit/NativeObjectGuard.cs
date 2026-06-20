using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AnimationKit;

internal static class NativeObjectGuard
{
  [Conditional("DEBUG")]
  public static void ThrowIfDisposed(bool disposed, [CallerMemberName] string caller = null)
  {
    if (disposed)
    {
      throw new ObjectDisposedException(caller);
    }
  }

  [Conditional("DEBUG")]
  public static void ThrowIfDisposed(IntPtr handle, [CallerMemberName] string caller = null)
  {
    if (handle == IntPtr.Zero)
    {
      throw new ObjectDisposedException(caller);
    }
  }
}
