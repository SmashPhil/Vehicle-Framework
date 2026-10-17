using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine.SceneManagement;
using Verse;

namespace SmashTools;

[StaticConstructorOnModInit]
public static class ComponentCache
{
  private static readonly List<Type> PriorityComponentTypes;
  private static readonly List<Type> DetachedComponentTypes;

  static ComponentCache()
  {
    PriorityComponentTypes = typeof(MapComponent).AllSubclassesNonAbstract().ToList();
    DetachedComponentTypes = typeof(DetachedMapComponent).AllSubclassesNonAbstract().ToList();
  }

  internal static int PriorityComponentTypeCount => PriorityComponentTypes.Count;

  internal static int DetachedComponentTypeCount => DetachedComponentTypes.Count;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static T GetCachedMapComponent<T>(this Map map) where T : MapComponent
  {
    return MapComponentCache<T>.GetComponent(map);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static T GetDetachedMapComponent<T>(this Map map) where T : DetachedMapComponent
  {
    return DetachedMapComponentCache<T>.GetComponent(map);
  }

  internal static void PreCacheInst(Map __instance)
  {
    if (Scribe.mode != LoadSaveMode.ResolvingCrossRefs)
      return;

    foreach (Type type in DetachedComponentTypes)
    {
      GenGeneric.InvokeStaticMethodOnGenericType(typeof(DetachedMapComponentCache<>), type,
        nameof(DetachedMapComponentCache<>.AddComponent), __instance);
    }
  }

  internal static void PreCache(Map map)
  {
    foreach (Type type in DetachedComponentTypes)
    {
      GenGeneric.InvokeStaticMethodOnGenericType(typeof(DetachedMapComponentCache<>), type,
        nameof(DetachedMapComponentCache<>.AddComponent), map);
    }
  }

  internal static void ClearMap(Map map)
  {
    foreach (Type type in PriorityComponentTypes)
    {
      GenGeneric.InvokeStaticMethodOnGenericType(typeof(MapComponentCache<>), type,
        nameof(MapComponentCache<>.ClearMap), map);
    }
    foreach (Type type in DetachedComponentTypes)
    {
      GenGeneric.InvokeStaticMethodOnGenericType(typeof(DetachedMapComponentCache<>), type,
        nameof(DetachedMapComponentCache<>.ClearMap), map);
    }
  }

  internal static void OnSceneChange(Scene scene, LoadSceneMode mode)
  {
    ClearAll();
  }

  internal static void ClearAll()
  {
    foreach (Type type in PriorityComponentTypes)
    {
      GenGeneric.InvokeStaticMethodOnGenericType(typeof(MapComponentCache<>), type,
        nameof(MapComponentCache<>.ClearAll));
    }
    foreach (Type type in DetachedComponentTypes)
    {
      GenGeneric.InvokeStaticMethodOnGenericType(typeof(DetachedMapComponentCache<>), type,
        nameof(DetachedMapComponentCache<>.ClearAll));
    }
  }

  public static int PriorityComponentCount()
  {
    int count = 0;
    foreach (Type type in PriorityComponentTypes)
    {
      count += (int)GenGeneric.InvokeStaticMethodOnGenericType(typeof(MapComponentCache<>), type,
        nameof(MapComponentCache<>.Count));
    }
    return count;
  }

  public static int DetachedComponentCount()
  {
    int count = 0;
    foreach (Type type in DetachedComponentTypes)
    {
      count += (int)GenGeneric.InvokeStaticMethodOnGenericType(typeof(DetachedMapComponentCache<>),
        type,
        nameof(DetachedMapComponentCache<>.Count));
    }
    return count;
  }
}