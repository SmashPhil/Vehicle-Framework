using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib;
using SmashTools.Xml;
using UnityEngine.Assertions;
using Verse;

namespace AnimationKit;

  public class AnimationTransition : IDisposable
  {
    //public List<AnimationCondition> conditions = new List<AnimationCondition>();

    public AnimationTransition()
    {
    }

    public AnimationTransition(AnimationState from, AnimationState to)
    {
      FromState = from;
      ToState = to;
      //toStateGuid = to.guid;
    }

    public AnimationState FromState { get; internal set; }

    public AnimationState ToState { get; internal set; }

    //public bool DefaultTransition => FromState != null &&
    //  FromState.Type == AnimationState.StateType.Entry &&
    //  ToState != null && ToState.Type == AnimationState.StateType.Default;

    public void Dispose()
    {
      //Trace.IsTrue(FromState.transitions.Remove(this));
      //Trace.IsTrue(ToState.transitionsIncoming.Remove(this));

      //FromState = null;
      //ToState = null;
    }

    public void AddCondition()
    {
    }
  }