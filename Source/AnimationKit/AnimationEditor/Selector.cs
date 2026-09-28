using System;
using System.Collections.Generic;

namespace SmashTools.Animations;

public class Selector
{
  private readonly Dictionary<Type, HashSet<ISelectableUI>> selected = [];

  public bool AnySelected<T>() where T : ISelectableUI
  {
    if (!selected.ContainsKey(typeof(T))) selected[typeof(T)] = [];
    return selected[typeof(T)].Count > 0;
  }

  public bool IsSelected(ISelectableUI item)
  {
    Type itemType = item.GetType();
    if (!selected.ContainsKey(itemType))
    {
      selected[itemType] = [];
      return false;
    }
    return selected[itemType].Contains(item);
  }

  public HashSet<ISelectableUI> GetSelected<T>()
  {
    if (!selected.ContainsKey(typeof(T))) selected[typeof(T)] = [];
    return selected[typeof(T)];
  }

  public void Select(ISelectableUI item, bool clear = true)
  {
    Type type = item.GetType();
    if (!selected.ContainsKey(type)) selected[type] = [];

    if (clear)
    {
      selected[type].Clear();
    }
    selected[type].Add(item);
  }

  public void Deselect(ISelectableUI item)
  {
    Type type = item.GetType();
    if (!selected.ContainsKey(type)) return;
    selected[type].Remove(item);
  }

  public void DeselectAll<T>()
  {
    if (!selected.ContainsKey(typeof(T))) return;
    selected[typeof(T)].Clear();
  }
}