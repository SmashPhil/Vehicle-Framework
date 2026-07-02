using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using SmashTools.Animations;
using UnityEngine.Assertions;
using Verse;

namespace AnimationKit;

public class Animator : IExposable, IDisposable
{
  private readonly IAnimator animator;
  private readonly AnimationController controller;
  private readonly NativeBuffer buffer;

  public Animator(IAnimator animator, AnimationController controller)
  {
    this.animator = animator;
    this.controller = controller;
    buffer = null;
  }

  public void Dispose()
  {
    buffer?.Dispose();
  }

  public void Tick()
  {
  }

  void IExposable.ExposeData()
  {
    // TODO - save animation state of entity
  }
}