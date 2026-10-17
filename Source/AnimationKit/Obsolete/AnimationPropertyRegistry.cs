using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;
using UnityEngine;
using System.Collections;
using System.Linq;

namespace SmashTools.Animations
{
  public static class AnimationPropertyRegistry
  {
    private static readonly Dictionary<IAnimator, List<AnimationPropertyParent>> cachedProperties =
      [];

    private static readonly Dictionary<Type, List<FieldInfo>> fieldRegistry = [];
    private static readonly Dictionary<Type, List<PropertyInfo>> propertyRegistry = [];

    // Enqueue context for recursive call and iterate instead
    private static readonly Queue<SearchContext> recursionQueue = [];

    // Faster type look-up and handles UnityEngine types which are not parseable by RimWorld
    private static readonly Dictionary<string, Type> typeNames = [];

    // Avoids circular references when processing reference types
    private static readonly HashSet<object> processedObjects = [];

    static AnimationPropertyRegistry()
    {
      RegisterType<Color>(nameof(Color.r), nameof(Color.g), nameof(Color.b), nameof(Color.a));
      RegisterType<Vector2>(nameof(Vector2.x), nameof(Vector2.y));
      RegisterType<Vector3>(nameof(Vector3.x), nameof(Vector3.y), nameof(Vector3.z));
      RegisterType<IntVec2>(nameof(IntVec2.x), nameof(IntVec2.z));
      RegisterType<IntVec3>(nameof(IntVec3.x), nameof(IntVec3.y), nameof(IntVec3.z));
    }

    public static bool CachedTypeByName(string name, out Type type)
    {
      return typeNames.TryGetValue(name, out type);
    }

    internal static void ClearCache()
    {
      processedObjects.Clear();
      cachedProperties.Clear();
    }

    private static List<AnimationPropertyParent> RunQueue(IAnimator animator)
    {
      List<AnimationPropertyParent> result = [];
      recursionQueue.Enqueue(new(animator, []));
      while (!recursionQueue.NullOrEmpty())
      {
        SearchContext context = recursionQueue.Dequeue();
        GetAnimationProperties(in context, result);
      }
      return result;
    }

    public static List<AnimationPropertyParent> GetAnimationProperties(IAnimator animator)
    {
      if (!cachedProperties.TryGetValue(animator, out List<AnimationPropertyParent> result))
      {
        result = RunQueue(animator);
        cachedProperties.Add(animator, result);
        processedObjects.Clear();
      }
      return result;
    }

    private static void GetAnimationProperties(ref readonly SearchContext context,
      List<AnimationPropertyParent> result)
    {
      // Do not reprocess the same instances
      if (!processedObjects.Add(context.parent)) return;

      // We'll want to copy the array when enqueueing new context, we don't want to constantly be appending
      // more depth to the hierarchy path. It should only persist within the scope of this parent.
      Type type = context.parent.GetType();
      foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Instance))
      {
        if (!fieldInfo.TryGetAttribute<AnimationPropertyAttribute>(out var animPropAttr))
        {
          continue;
        }
        if (fieldInfo.FieldType.IsClass)
        {
          object child = fieldInfo.GetValue(context.parent);
          if (child is IList list)
          {
            Type innerListType = fieldInfo.FieldType.GetGenericArguments()[0];
            if (!innerListType.HasInterface(typeof(IAnimationObject)))
            {
              Log.Error(
                $@"{innerListType} must implement IAnimationObject if it's to be animated from a list.");
              continue;
            }
            for (int i = 0; i < list.Count; i++)
            {
              object listObj = list[i];
              recursionQueue.Enqueue(new SearchContext(listObj,
                [.. context.path, new ObjectPath(fieldInfo, index: i)], index: i));
            }
            continue;
          }
          recursionQueue.Enqueue(new SearchContext(child,
            [.. context.path, new ObjectPath(fieldInfo)]));
          continue;
        }
        // Type must be registered or supported primitive type
        if (!HandlesType(fieldInfo.FieldType))
        {
          Log.Error(
            $@"Type {fieldInfo.FieldType} is not supported as an animation property. It must be registered 
in the AnimationPropertyRegistry or be a class type for recursion.");
          continue;
        }
        // Add property info to registry
        string label = animPropAttr.Name;
        if (label.NullOrEmpty())
        {
          label = fieldInfo.Name;
        }
        // Parent must be IAnimationObject to get to this point
        string identifier = context.Indexer ? ((IAnimationObject)context.parent).ObjectId : null;
        AnimationPropertyParent container =
          AnimationPropertyParent.Create(identifier, label, fieldInfo, context.path.ToList());
        if (IsSupportedPrimitive(fieldInfo.FieldType))
        {
          AnimationProperty property = AnimationProperty.Create(type, label, fieldInfo, null);
          container.SetSingle(property);
          result.Add(container);
        }
        else if (IsContainerProperty(fieldInfo.FieldType))
        {
          foreach (FieldInfo innerFieldInfo in fieldInfo.FieldType.GetFields(BindingFlags.Public |
            BindingFlags.NonPublic
            | BindingFlags.Instance))
          {
            if (!HandlesType(innerFieldInfo.FieldType) ||
              !IsSupportedPrimitive(innerFieldInfo.FieldType))
            {
              Log.Error(
                $@"Type {innerFieldInfo.FieldType} is not supported as an animation property. Nested fields must be a 
supported primitive type {{ int, float, bool }}");
              continue;
            }
            AnimationProperty property =
              AnimationProperty.Create(type, innerFieldInfo.Name, innerFieldInfo, fieldInfo);
            container.Add(property);
          }
          result.Add(container);
        }
      }
    }

    private static bool IsSupportedPrimitive(Type type)
    {
      return type == typeof(float) || type == typeof(int) || type == typeof(bool);
    }

    private static bool IsContainerProperty(Type type)
    {
      return fieldRegistry.ContainsKey(type) || propertyRegistry.ContainsKey(type);
    }

    public static bool HandlesType(Type type)
    {
      if (IsSupportedPrimitive(type))
      {
        return true;
      }
      return fieldRegistry.ContainsKey(type) || propertyRegistry.ContainsKey(type);
    }

    public static void RegisterType<T>(params string[] fieldNames)
    {
      if (fieldRegistry.ContainsKey(typeof(T)))
      {
        Log.Error(
          $"{typeof(T)} has already been registered. Skipping to avoid duplicate field entries.");
        return;
      }
      if (fieldNames.NullOrEmpty())
      {
        Log.Error($"Trying to register AnimationProperty in registry with no fields.");
        return;
      }
      typeNames[GenTypes.GetTypeNameWithoutIgnoredNamespaces(typeof(T))] = typeof(T);
      foreach (string name in fieldNames)
      {
        FieldInfo fieldInfo = AccessTools.Field(typeof(T), name);
        if (fieldInfo == null)
        {
          Log.Error($"Unable to locate {typeof(T)}.{name}");
          continue;
        }
        fieldRegistry.AddOrAppend(typeof(T), fieldInfo);
      }
    }

    private readonly struct SearchContext(object parent, ObjectPath[] path, int index = -1)
    {
      public readonly object parent = parent;
      public readonly ObjectPath[] path = path;
      public readonly int index = index;

      public readonly bool Indexer => index >= 0;
    }
  }
}