using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SmashTools;
using Verse;

namespace AnimationKit;

internal static class PropertyDatabase
{
  private static readonly Dictionary<Type, HashSet<FieldInfo>> properties = [];
  private static readonly Dictionary<FieldInfo, string> propertyNames = [];

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
