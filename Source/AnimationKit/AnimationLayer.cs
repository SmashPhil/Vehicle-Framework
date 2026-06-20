using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace AnimationKit;

public sealed class AnimationLayer(IntPtr handle) : IDisposable
{
  private readonly List<AnimationState> states = [];

  internal IntPtr Handle => handle;

  public bool Disposed { get; private set; }

  public string Name
  {
    get;
    internal set
    {
      field = value;
    }
  }

  public static implicit operator bool(AnimationLayer layer)
  {
    return !layer.Disposed;
  }

  public void AddState(string name, AnimationState.Type type)
  {

  }

  public void Dispose()
  {
    _ = handle; // TODO - remove

    // layer is owned by the parent controller, we only need to flag it as freed so we don't
    // accidentally cause UAF crash.
    Disposed = true;
  }

  [MustUseReturnValue]
  [DllImport("animation_kit", CallingConvention = CallingConvention.Cdecl)]
  private static extern IntPtr CreateState(IntPtr layerPtr, string name, byte type);

  [DllImport("animation_kit", CallingConvention = CallingConvention.Cdecl)]
  private static extern void DeleteState(IntPtr layerPtr, IntPtr statePtr);
}
