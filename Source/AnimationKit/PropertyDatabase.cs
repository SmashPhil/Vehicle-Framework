using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SmashTools;
using UnityEngine;
using Verse;

namespace AnimationKit;

internal static class PropertyDatabase
{
  private static readonly Dictionary<Type, List<FieldInfo>> fieldRegistry = [];

  private static readonly Dictionary<Type, HashSet<FieldInfo>> properties = [];
  private static readonly Dictionary<FieldInfo, string> propertyNames = [];

  static PropertyDatabase()
  {
    RegisterType<Color>(nameof(Color.r), nameof(Color.g), nameof(Color.b), nameof(Color.a));
    RegisterType<Vector2>(nameof(Vector2.x), nameof(Vector2.y));
    RegisterType<Vector3>(nameof(Vector3.x), nameof(Vector3.y), nameof(Vector3.z));
  }

  public static string PropertyName(FieldInfo field)
  {
    return propertyNames.TryGetValue(field);
  }

  private static bool IsSupportedPrimitive(Type type)
  {
    return type == typeof(float) || type == typeof(int) || type == typeof(bool);
  }

  public static bool HandlesType(Type type)
  {
    return IsSupportedPrimitive(type) || fieldRegistry.ContainsKey(type);
  }

  public static void RegisterType<T>(params string[] fieldNames) where T : unmanaged
  {
    if (fieldNames.NullOrEmpty())
      throw new ArgumentNullException(nameof(fieldNames));
    
    if (fieldRegistry.ContainsKey(typeof(T)))
    {
      Log.Error($"{typeof(T)} has already been registered.");
      return;
    }

    foreach (string name in fieldNames)
    {
      FieldInfo fieldInfo = AccessTools.Field(typeof(T), name);
      if (fieldInfo == null)
      {
        Log.Error($"Can't find field '{typeof(T)}.{name}'");
        continue;
      }
      fieldRegistry.AddOrAppend(typeof(T), fieldInfo);
    }
  }

  public static IEnumerable<FieldInfo> GetProperties(IAnimator animator)
  {
    return properties.TryGetValue(animator.GetType());
  }

  public static void SerializeProperties(this IAnimator animator)
  {
    Type type = animator.GetType();
    if (!properties.TryGetValue(type, out HashSet<FieldInfo> fields))
    {
      fields = [];
      properties[type] = fields;
    }
    SerializePropertiesRecursive(animator, type, fields);
  }

  private static void SerializePropertiesRecursive(object parent, Type type, HashSet<FieldInfo> fields)
  {
    foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
    {
      if (field.TryGetAttribute<AnimationPropertyAttribute>() is { } prop)
      {
        Type fieldType = field.FieldType;
        if (fieldType.IsIList())
        {
          Type genericType = fieldType.GetGenericArguments()[0];
          if (!genericType.IsClass)
            continue;

          IList list = (IList)field.GetValue(parent);
          if (list != null)
          {
            foreach (object obj in list)
            {
              SerializePropertiesRecursive(obj, genericType, fields);
            }
          }
        }
        else if (fields.Add(field))
        {
          string name = !prop.Name.NullOrEmpty() ? prop.Name : field.Name;
          propertyNames[field] = name;
        }
      }
    }
  }
}
