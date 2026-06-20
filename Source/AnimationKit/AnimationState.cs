using System;
using System.Collections.Generic;

namespace AnimationKit;

public class AnimationState : IDisposable
{
  public List<AnimationTransition> transitions = [];
  public List<AnimationTransition> transitionsIncoming = [];

  /// <summary>
  /// For XML Deserialization
  /// </summary>
  public AnimationState()
  {
  }

  public AnimationState(string name, Type stateType)
  {
    Name = name;
    StateType = stateType;
  }

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

  public AnimationLayer Layer { get; internal set; }

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
  }

  public enum Type
  {
    None,
    Entry,
    Default,
    Exit,
    Any
  }
}