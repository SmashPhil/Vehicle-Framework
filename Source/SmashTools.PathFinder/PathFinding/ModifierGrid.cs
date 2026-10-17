
using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using Unity.Collections;
using UnityEngine.Assertions;

namespace SmashTools.PathFinder
{
  [PublicAPI]
  [DebuggerDisplay("Length = {modifiers.Length}")]
  public sealed class ModifierGrid : IDisposable
  {
    private readonly IPathGridSync sync;

    private ModifierList[] modifierLists;
    private NativeArray<Modifier> modifiers;

    public ModifierGrid(int size, [CanBeNull] IPathGridSync sync = null)
    {
      this.sync = sync;
      modifierLists = new ModifierList[size];
      modifiers = new NativeArray<Modifier>(size, Allocator.Persistent);
    }

    internal NativeArray<Modifier>.ReadOnly InnerGrid => modifiers.AsReadOnly();

    public Modifier GetModifier(int index)
    {
      return modifiers[index];
    }

    public void AddModifier(string uniqueId, int index, Modifier modifier)
    {
      AddModifier(uniqueId, index, modifier, priority: int.MaxValue);
    }

    public void AddModifier(string uniqueId, int index, Modifier modifier, int priority)
    {
      sync?.NotifyWritingToGrid();
      ModifierList modList = modifierLists[index];
      if (modList == null)
      {
        modifierLists[index] = new ModifierList();
        modList = modifierLists[index];
      }
      modList.Add(uniqueId, modifier, priority);
      modifiers[index] = modList.HighestPriorityModifier;
    }

    public void RemoveModifier(string uniqueId, int index)
    {
      ModifierList modList = modifierLists[index];
      Assert.IsNotNull(modList);
      modList.Remove(uniqueId);
      modifiers[index] = modList.HighestPriorityModifier;
      if (modList.IsEmpty)
      {
        modifierLists[index] = null;
      }
    }

    public void Dispose()
    {
      modifiers.Dispose();
    }

    private class ModifierList
    {
      private readonly List<ModifierData> list = new();

      public bool IsEmpty => list.Count == 0;

      public Modifier HighestPriorityModifier => list.Count > 0 ? list[0].modifier : new Modifier();

      public void Add(string uniqueId, Modifier modifier, int priority)
      {
        list.Add(new ModifierData
        {
          uniqueId = uniqueId,
          modifier = modifier,
          priority = priority
        });
        list.Sort();
      }

      public void Remove(string uniqueId)
      {
        for (int i = 0; i < list.Count; i++)
        {
          if (list[i].uniqueId == uniqueId)
          {
            list.RemoveAt(i);
            break;
          }
        }
        list.Sort();
      }

      private struct ModifierData : IComparable<ModifierData>
      {
        public string uniqueId;
        public Modifier modifier;
        public int priority;

        int IComparable<ModifierData>.CompareTo(ModifierData other)
        {
          return priority.CompareTo(other.priority);
        }
      }
    }
  }
}