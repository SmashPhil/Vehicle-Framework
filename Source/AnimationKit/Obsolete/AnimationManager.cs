using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Verse;
using PropertyObjectMap = System.Collections.Generic.Dictionary
<SmashTools.Animations.AnimationState,
  SmashTools.Animations.IAnimationObject[]>;
using StateType = SmashTools.Animations.AnimationState.StateType;

namespace SmashTools.Animations
{
  public class AnimationManager : IExposable
  {
    public readonly IAnimator animator;
    public readonly AnimationController controller;

    private LayerData[] layerDatas;

    private Dictionary<ushort, float> parameters = [];

    public AnimationManager(IAnimator animator, AnimationController controller)
    {
      this.animator = animator;
      this.controller = controller;
      Init(animator, controller);
    }

    private void Init(IAnimator animator, AnimationController controller)
    {
      layerDatas = new LayerData[controller.layers.Count];

      for (int i = 0; i < controller.layers.Count; i++)
      {
        layerDatas[i] = new LayerData(animator, controller.layers[i]);
      }
      foreach (AnimationParameterDef paramDef in DefDatabase<AnimationParameterDef>
       .AllDefsListForReading)
      {
        parameters[paramDef.shortHash] = 0;
      }
      if (!controller.parameters.NullOrEmpty())
      {
        foreach (AnimationParameter parameter in controller.parameters)
        {
          parameters[parameter.Id] = parameter.Value;
        }
      }
    }

    public void PostLoad()
    {
      foreach (LayerData layerData in layerDatas)
      {
        layerData.PostLoad();
      }
    }


    public void AnimationTick()
    {
      for (int i = 0; i < controller.layers.Count; i++)
      {
        LayerData layerData = layerDatas[i];
        if (!layerData.IsValid)
        {
          StartNextState(layerData);
          continue;
        }

        // Check if transition is needed
        if (layerData.frame >= layerData.state.clip.frameCount)
        {
          StartNextState(layerData);
        }
        else
        {
          layerData.Update();
        }
      }
    }

    void IExposable.ExposeData()
    {
      Scribe_Collections.Look(ref parameters, nameof(parameters), keyLookMode: LookMode.Value,
        valueLookMode: LookMode.Value);
      Scribe_Array.Look(ref layerDatas, nameof(layerDatas), lookMode: LookMode.Deep);
    }

    // TODO - transitions need exitTime and blending implemented
    private void Transition(LayerData layerData)
    {
      if (!layerData.Transitioning)
      {
        layerData.EvaluateTransition();
      }
      if (layerData.TransitionTick >= layerData.transition.exitTicks)
      {
        StartNextState(layerData);
      }
    }

    private void StartNextState(LayerData layerData)
    {
      Assert.IsNotNull(layerData.state.transitions);

      foreach (AnimationTransition transition in layerData.state.transitions)
      {
        if (transition.conditions.NullOrEmpty())
        {
          layerData.SetState(transition.ToState);
          return;
        }

        foreach (AnimationCondition condition in transition.conditions)
        {
          float value = parameters[condition.Def.shortHash];
          if (condition.ConditionMet(value))
          {
            layerData.SetState(transition.ToState);
            return;
          }
        }
      }
      if (layerData.IsValid && layerData.state.loop)
      {
        layerData.frame = 0;
      }
      // End of animation chain, state will wait for transition till one becomes available
    }

    internal (AnimationState state, int frame) CurrentFrame(AnimationLayer layer)
    {
      foreach (LayerData layerData in layerDatas)
      {
        if (layerData.layer == layer)
        {
          return (layerData.state, layerData.frame);
        }
      }
      return (null, 0);
    }

    internal void SetFrame(AnimationClip clip, int frame)
    {
      foreach (LayerData layerData in layerDatas)
      {
        layerData.SetFrame(clip, frame);
      }
    }

#region Properties

    public void SetFloat(string name, float value)
    {
      SetFloat(DefDatabase<AnimationParameterDef>.GetNamed(name), value);
    }

    public void SetFloat(AnimationParameterDef paramDef, float value)
    {
      Assert.IsNotNull(paramDef);
      SetFloat(paramDef.shortHash, value);
    }

    public void SetFloat(ushort id, float value)
    {
      // All ids should be precached but this isn't error-causing so this
      // is strictly just for notifying the operation is useless.
      Assert.IsTrue(parameters.ContainsKey(id), "Parameter Id not precached");
      parameters[id] = value;
    }

    public void SetInt(AnimationParameterDef paramDef, int value)
    {
      SetInt(paramDef.shortHash, value);
    }

    public void SetInt(ushort id, int value)
    {
      SetFloat(id, value);
    }

    public void SetBool(AnimationParameterDef paramDef, bool value)
    {
      SetBool(paramDef.shortHash, value);
    }

    public void SetBool(ushort id, bool value)
    {
      SetFloat(id, value ? 1 : 0);
    }

    public void SetTrigger(AnimationParameterDef paramDef, bool value)
    {
      SetTrigger(paramDef.shortHash, value);
    }

    public void SetTrigger(ushort id, bool value)
    {
      SetFloat(id, value ? 1 : 0);
    }

    public float GetFloat(AnimationParameterDef paramDef)
    {
      return GetFloat(paramDef.shortHash);
    }

    public float GetFloat(ushort id)
    {
      return parameters[id];
    }

    public int GetInt(AnimationParameterDef paramDef)
    {
      return GetInt(paramDef.shortHash);
    }

    public int GetInt(ushort id)
    {
      return (int)parameters[id];
    }

    public bool GetBool(AnimationParameterDef paramDef)
    {
      return GetBool(paramDef.shortHash);
    }

    public bool GetBool(ushort id)
    {
      return parameters[id] != 0;
    }

    public bool GetTrigger(AnimationParameterDef paramDef)
    {
      return GetBool(paramDef.shortHash);
    }

    public bool GetTrigger(ushort id)
    {
      return GetBool(id);
    }

    #endregion Properties

    private class LayerData : IExposable
    {
      private readonly IAnimator animator;
      public readonly AnimationLayer layer;
      private readonly AnimationState defaultState;

      [AssignedFromXml]
      public int frame; // Current frame in each layer
      [AssignedFromXml]
      public AnimationState state; // Active state in each layer
      [AssignedFromXml]
      public AnimationState nextState;
      [AssignedFromXml]
      public AnimationTransition transition;
      [AssignedFromXml]
      public bool paused;

      public LayerData(IAnimator animator, AnimationLayer layer)
      {
        this.animator = animator;
        this.layer = layer;
        defaultState = layer.states.FirstOrDefault(state => state.Type == StateType.Default);
        state = defaultState;
      }

      public Dictionary<FieldInfo, float> Defaults { get; private set; } = [];

      private PropertyObjectMap StateObjects { get; } = [];

      // Invalid states are treated as empty. Will immediately transition to the next state
      public bool IsValid => state.clip;

      public int TransitionTick => frame - state.clip.frameCount;

      public bool Transitioning => transition != null;

      public bool WriteDefaults =>
        state.writeDefaults && state.clip && !state.clip.properties.NullOrEmpty();

      public void PostLoad()
      {
        MapAnimationObjects();
      }

      public void Update()
      {
        Assert.IsTrue(IsValid);
        for (int i = 0; i < state.PropertyCount; i++)
        {
          IAnimationObject obj = StateObjects[state][i];
          state.clip.properties[i].EvaluateFrame(obj, frame);
        }
        for (int i = 0; i < state.clip.events.Count; i++)
        {
          AnimationEvent animEvent = state.clip.events[i];
          if (animEvent.frame == frame)
          {
            animEvent.method.Invoke(animator, [animator]);
          }
        }
        frame++;
      }

      internal void SetFrame(AnimationClip clip, int frame)
      {
        for (int i = 0; i < clip.properties.Count; i++)
        {
          AnimationPropertyParent propertyParent = clip.properties[i];
          IAnimationObject obj = propertyParent.ObjectFromHierarchy(animator);
          clip.properties[i].EvaluateFrame(obj, frame);
        }
      }

      public void EvaluateTransition()
      {
        throw new NotImplementedException();
      }

      public void SetState(AnimationState state)
      {
        RestoreDefaults();
        frame = 0;
        if (state.Type == StateType.Exit)
        {
          state = defaultState;
        }
        this.state = state;
        if (IsValid)
        {
          CacheDefaults();
        }
      }

      private void CacheDefaults()
      {
        if (!WriteDefaults || !IsValid) return;

        Defaults.Clear();
        for (int i = 0; i < state.clip.properties.Count; i++)
        {
          AnimationPropertyParent propertyParent = state.clip.properties[i];
          for (int j = 0; j < propertyParent.Properties.Count; j++)
          {
            AnimationProperty property = propertyParent.Properties[j];
            IAnimationObject obj = StateObjects[state][j];
            float startingValue = property.GetProperty(obj);
            Defaults[property.FieldInfo] = startingValue;
          }
        }
      }

      private void MapAnimationObjects()
      {
        StateObjects.Clear();
        for (int s = 0; s < layer.states.Count; s++)
        {
          AnimationState state = layer.states[s];
          if (state.clip == null) continue;

          StateObjects[state] = new IAnimationObject[state.PropertyCount];
          for (int i = 0; i < state.clip.properties.Count; i++)
          {
            AnimationPropertyParent propertyParent = state.clip.properties[i];
            StateObjects[state][i] = propertyParent.ObjectFromHierarchy(animator);
          }
        }
      }

      private void RestoreDefaults()
      {
        if (!WriteDefaults) return;

        for (int i = 0; i < state.clip.properties.Count; i++)
        {
          AnimationPropertyParent propertyParent = state.clip.properties[i];
          for (int j = 0; j < propertyParent.Properties.Count; j++)
          {
            AnimationProperty property = propertyParent.Properties[j];
            IAnimationObject obj = StateObjects[state][j];
            float value = Defaults[property.FieldInfo];
            property.SetProperty(obj, value);
          }
        }
      }

      public void Reset()
      {
        SetState(defaultState);
      }

      void IExposable.ExposeData()
      {
        Scribe_Values.Look(ref frame, nameof(frame));
        Scribe_Values.Look(ref state.guid, nameof(nextState));

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
          MapAnimationObjects();
        }
      }
    }
  }
}