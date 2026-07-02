using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace AnimationKit;

public sealed class AnimationLayer : IDisposable
{
  private string name;

  private readonly List<AnimationState> states = [];

  internal AnimationLayer(IntPtr ctrlPtr, string name)
  {
    Assert.AreNotEqual(IntPtr.Zero, ctrlPtr);
    UnsafePtr = CreateLayer(ctrlPtr, name);
    this.name = name;
  }

  internal IntPtr UnsafePtr { get; private set; }

  public bool Disposed => UnsafePtr == IntPtr.Zero;

  public string Name
  {
    get => name;
    internal set
    {
      name = value;
    }
  }

  public AnimationState AddState(string stateName, AnimationState.Type type)
  {
    var state = new AnimationState(UnsafePtr, stateName, type);
    states.Add(state);
    return state;
  }

  public void Dispose()
  {
    foreach (AnimationState state in states)
    {
      state.Dispose();
    }
    states.Clear();

    // layer is owned by the parent controller, we only need to flag it as freed so we don't
    // accidentally cause UAF crash.
    UnsafePtr = IntPtr.Zero;
  }

  [MustUseReturnValue]
  [DllImport("animation_kit", CallingConvention = CallingConvention.Cdecl)]
  private static extern IntPtr CreateLayer(IntPtr controllerPtr, string name);
}
