using System;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace CoreLib.Performance;

/// <summary>
/// Invoke <typeparam name="T"/>'s parameterless constructor without reflection.
/// </summary>
[PublicAPI]
public static class MethodBuilder
{
  public static Func<T> GetConstructor<T>(ConstructorInfo ctor)
  {
    DynamicMethod dm = new(name: $"Ctor_{typeof(T).Name}", returnType: typeof(T),
      parameterTypes: Type.EmptyTypes, m: typeof(T).Module, skipVisibility: true);

    ILGenerator il = dm.GetILGenerator();
    il.Emit(OpCodes.Newobj, ctor);
    il.Emit(OpCodes.Ret);

    return (Func<T>)dm.CreateDelegate(typeof(Func<T>));
  }
}