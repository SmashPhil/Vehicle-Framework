using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using AnimationKit.Editor;
using CoreLib;
using CoreLib.Collections;
using JetBrains.Annotations;

namespace AnimationKit;

internal class AnimationPropertyGroup : ISelectableUI
{
  private readonly List<AnimationProperty> properties = [];
  private readonly Dictionary<AnimationProperty, Ref<IntPtr>> handles = [];

  public AnimationPropertyGroup(Ref<IntPtr>.ReadOnly handle, FieldInfo field, string name, bool isContainer)
  {
    UnsafePtr = handle;
    Field = field;
    Name = name;
    Prefix = Type.Name;
    IsContainer = isContainer;
  }

  public string Name { get; }

  public string Prefix { get; }

  public FieldInfo Field { get; }

  public Type Type => Field.FieldType;

  private Ref<IntPtr>.ReadOnly UnsafePtr { get; }

  public bool Destroyed => UnsafePtr.Value == IntPtr.Zero;

  public ReadOnlyList<AnimationProperty> Properties => new(properties);

  public bool IsContainer { get; }

  public bool IsValid => properties.Count > 0;

  public void AddProperty(string name, ParameterType type)
  {
    string fullName = IsContainer ? $"{Prefix}::{name}" : name;
    IntPtr ptr = AddAnimationProperty(UnsafePtr, fullName, type);
    if (ptr != IntPtr.Zero)
    {
      Ref<IntPtr> handle = new() { Value = ptr };
      var property = new AnimationProperty(fullName, handle.View);
      properties.Add(property);
      handles[property] = handle;
    }
  }

  public void RemoveProperty(string name)
  {
    string fullName = IsContainer ? $"{Prefix}::{name}" : name;
    if (RemoveAnimationProperty(UnsafePtr, fullName))
    {
      for (int i = 0; i < properties.Count; i++)
      {
        AnimationProperty property = properties[i];
        if (property.Name == fullName)
        {
          properties.RemoveAt(i);
          handles[property].Value = IntPtr.Zero;
          handles.Remove(property);
          break;
        }
      }
    }
  }

  public void RemoveAll()
  {
    foreach (AnimationProperty property in properties)
    {
      string fullName = IsContainer ? property.Name : $"{Prefix}::{property.Name}";
      RemoveAnimationProperty(UnsafePtr, fullName);
    }
    properties.Clear();
  }

  public bool AllKeyFramesAt(uint frame)
  {
    if (!IsValid) return false;

    foreach (AnimationProperty property in properties)
    {
      if (!property.AnyKeyFrameAt(frame))
      {
        return false;
      }
    }
    return true;
  }

  public bool AnyKeyFrameAt(uint frame)
  {
    foreach (AnimationProperty property in properties)
    {
      if (property.AnyKeyFrameAt(frame))
      {
        return true;
      }
    }
    return false;
  }

  [MustUseReturnValue]
  [DllImport("animation_kit", CallingConvention = CallingConvention.Cdecl)]
  private static extern IntPtr AddAnimationProperty(IntPtr clipPtr, string name, ParameterType type);

  [DllImport("animation_kit", CallingConvention = CallingConvention.Cdecl)]
  private static extern Boolean RemoveAnimationProperty(IntPtr clipPtr, string name);
}