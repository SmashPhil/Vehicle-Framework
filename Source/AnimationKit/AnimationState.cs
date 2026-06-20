using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace AnimationKit;

public class AnimationState : IDisposable
{
  public List<AnimationTransition> transitions = [];
  public List<AnimationTransition> transitionsIncoming = [];

  internal AnimationState(IntPtr layerPtr, string name, Type stateType)
  {
    Name = name;
    StateType = stateType;
    UnsafePtr = CreateState(layerPtr, name, (byte)stateType);
  }

  internal IntPtr UnsafePtr { get; private set; }

  public bool Disposed { get; private set; }

  public string Name
  {
    get;
    internal set
    {
      field = value;
    }
  }

  public Type StateType
  {
    get;
    internal set
    {
      field = value;
    }
  }

  public bool IsPermanent => StateType is Type.Entry or Type.Exit or Type.Any;

  public void AddTransition(AnimationState to)
  {
    //AnimationTransition transition = new(this, to);
    //transitions.Add(transition);
    //to.transitionsIncoming.Add(transition);
  }

  public void Dispose()
  {
    for (int i = transitions.Count - 1; i >= 0; i--)
    {
      transitions[i].Dispose();
    }
    for (int i = transitionsIncoming.Count - 1; i >= 0; i--)
    {
      transitionsIncoming[i].Dispose();
    }

    // AnimationLayer parent owns this object, deletion of parent will delete this object
    Disposed = true;
    UnsafePtr = IntPtr.Zero;
  }

  [MustUseReturnValue]
  [DllImport("animation_kit", CallingConvention = CallingConvention.Cdecl)]
  private static extern IntPtr CreateState(IntPtr layerPtr, string name, byte type);

  [DllImport("animation_kit", CallingConvention = CallingConvention.Cdecl)]
  private static extern void DeleteState(IntPtr layerPtr, IntPtr statePtr);

  public enum Type
  {
    None,
    Entry,
    Default,
    Exit,
    Any
  }
}