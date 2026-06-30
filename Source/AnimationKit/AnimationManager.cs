using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using SmashTools.Animations;
using UnityEngine.Assertions;
using Verse;

namespace AnimationKit;

public class AnimationManager : IExposable
{
  //public readonly IAnimator animator;
  public readonly AnimationController controller;

  public AnimationManager(IAnimator animator, AnimationController controller)
  {
    this.controller = controller;
  }

  public void Tick()
  {
  }

  void IExposable.ExposeData()
  {
    // TODO - save animation state of entity
  }
}