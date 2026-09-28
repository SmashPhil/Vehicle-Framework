using System;
using System.Collections.Generic;
using System.Linq;
using SmashTools.Xml;
using Verse;

namespace SmashTools.Animations;

public class AnimationState : IXmlExport, IDisposable
{
  public string name;
  public Guid guid;
  public IntVec2 position;
  public AnimationClip clip;
  public float speed = 1;
  public bool loop = false;
  public bool writeDefaults = true;

  private StateType stateType = StateType.None;

  public List<AnimationTransition> transitions = [];

  // Linked post-load, we don't need to save the same transition in 2 places.
  [Unsaved]
  public List<AnimationTransition> transitionsIncoming = [];

  /// <summary>
  /// For XML Deserialization
  /// </summary>
  public AnimationState()
  {
  }

  public AnimationState(string name, StateType stateType)
  {
    this.name = name;
    this.stateType = stateType;
    guid = Guid.NewGuid();
  }

  public StateType Type => stateType;

  public bool IsPermanent =>
    Type == StateType.Entry || Type == StateType.Exit || Type == StateType.Any;

  public AnimationLayer Layer { get; internal set; }

  public int PropertyCount
  {
    get
    {
      if (clip == null)
        return 0;
      return clip.properties.Sum(parent => parent.Properties.Count);
    }
  }

  public void AddTransition(AnimationState to)
  {
    AnimationTransition transition = new(this, to);
    transitions.Add(transition);
    to.transitionsIncoming.Add(transition);
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

  internal void ResolveReferences()
  {
    if (!transitions.NullOrEmpty())
    {
      foreach (AnimationTransition transition in transitions)
      {
        transition.FromState = this;
        transition.ResolveReferences();
      }
    }
  }

  void IXmlExport.Export()
  {
    XmlExporter.WriteObject(nameof(name), name);
    XmlExporter.WriteObject(nameof(guid), guid);
    XmlExporter.WriteObject(nameof(position), position);
    XmlExporter.WriteObject(nameof(clip), clip?.Guid);
    XmlExporter.WriteObject(nameof(speed), speed);
    XmlExporter.WriteObject(nameof(loop), loop);
    XmlExporter.WriteObject(nameof(writeDefaults), writeDefaults);

    XmlExporter.WriteObject(nameof(stateType), stateType);

    XmlExporter.WriteCollection(nameof(transitions), transitions);
  }

  // Pass in layer to add to, allowing for copy / pasting across layers
  public AnimationState CreateCopy(AnimationLayer layer)
  {
    AnimationState copy = new();
    copy.name = AnimationLoader.GetAvailableName(layer.states.Select(l => l.name), name);
    copy.guid = Guid.NewGuid();
    copy.position = position;
    copy.clip = clip;
    copy.speed = speed;
    copy.writeDefaults = writeDefaults;
    copy.stateType = stateType;
    copy.transitions = transitions.Select(transition => transition.CreateCopy()).ToList();

    if (stateType == StateType.Default)
    {
      copy.stateType = StateType.None;
    }
    return copy;
  }

  public enum StateType
  {
    None,
    Entry,
    Default,
    Exit,
    Any
  }
}