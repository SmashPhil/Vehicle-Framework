using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Patching;

[PublicAPI]
public static class PatchUtils
{
  [Pure]
  public static Type GetStateMachineType(this MethodBase method)
  {
    if (method.TryGetAttribute<IteratorStateMachineAttribute>() is not {} iterator)
    {
      Log.Error($"Trying to get state machine class from non-iterator method {method.Name}.");
      return null;
    }
    return iterator.StateMachineType;
  }

  [Pure]
  public static MethodBase GetIteratorMethod(this Type type)
  {
    Assert.IsTrue(type.HasInterface(typeof(IEnumerator)));
    return AccessTools.Method(type, "MoveNext");
  }

  [Pure]
  public static Type GetIteratorDataType(this Type type, Func<FieldInfo, bool> predicate)
  {
    return GetIteratorDataField(type, predicate).FieldType;
  }

  [Pure]
  public static FieldInfo GetIteratorDataField(this Type type, Func<FieldInfo, bool> predicate)
  {
    foreach (FieldInfo field in type.GetDeclaredFields())
    {
      if (!field.FieldType.IsClass)
        continue;

      if (predicate(field))
      {
        return field;
      }
    }
    throw new InvalidOperationException($"Unable to find data field for {type.Name}");
  }

  [Pure]
  public static MethodBase FindDelegateMethod(this MethodBase method, Type delegateType)
  {
    return FindDelegateMethod(method, fieldInfo => fieldInfo.FieldType == delegateType);
  }

  [Pure]
  public static MethodBase FindDelegateMethod(this MethodBase method, FieldInfo fieldInfo)
  {
    return FindDelegateMethod(method, fi => fi == fieldInfo);
  }

  [Pure]
  public static MethodBase FindDelegateMethod(this MethodBase method, string fieldName)
  {
    return FindDelegateMethod(method, fieldInfo => fieldInfo.Name == fieldName);
  }

  [Pure]
  public static MethodBase FindDelegateMethod(this MethodBase method, Func<FieldInfo, bool> predicate)
  {
    var body = PatchProcessor.ReadMethodBody(method).ToList();

    int fieldIndex = body.FindIndex(code =>
    {
      if (code.Key != OpCodes.Stfld && code.Key != OpCodes.Stsfld)
        return false;

      return code.Value is FieldInfo fieldInfo && predicate(fieldInfo);
    });

    if (fieldIndex < 0)
    {
      throw new InvalidOperationException($"Unable to find delegate field in '{method.Name}'");
    }

    int methodIndex = body.FindLastIndex(fieldIndex, static code => code.Key == OpCodes.Ldftn);

    if (methodIndex < 0)
    {
      throw new InvalidOperationException($"No function found with assignment to delegate field " +
                                          $"in method '{method.Name}'");
    }
    return (MethodBase)body[methodIndex].Value;
  }
}