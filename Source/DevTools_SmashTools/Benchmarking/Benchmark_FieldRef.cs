using System;
using System.Linq.Expressions;
using System.Reflection;
using DevTools.Benchmarking;
using HarmonyLib;
using Verse;

// ReSharper disable all

namespace SmashTools.Performance;

[BenchmarkClass("FieldRef"), SampleSize(1000000)]
internal class Benchmark_FieldRef
{
  private static readonly FieldInfo staticFieldInfo;
  private static readonly FieldInfo instanceFieldInfo;
  private static readonly FieldInfo structFieldInfo;

  private static readonly AccessTools.FieldRef<TestObject, int> staticFieldRef;
  private static readonly AccessTools.FieldRef<TestObject, int> instanceFieldRef;
  private static readonly AccessTools.StructFieldRef<TestStruct, int> structFieldRef;

  private static readonly Func<int> staticExpression;
  private static readonly Func<TestObject, int> instanceExpression;
  private static readonly Func<TestStruct, int> structExpression;

  static Benchmark_FieldRef()
  {
    staticFieldInfo = AccessTools.Field(typeof(TestObject), "somePrivateStaticInt");
    instanceFieldInfo = AccessTools.Field(typeof(TestObject), "somePrivateInt");
    structFieldInfo = AccessTools.Field(typeof(TestStruct), "somePrivateInt");

    staticFieldRef = AccessTools.FieldRefAccess<TestObject, int>(staticFieldInfo);
    instanceFieldRef = AccessTools.FieldRefAccess<TestObject, int>(instanceFieldInfo);
    structFieldRef = AccessTools.StructFieldRefAccess<TestStruct, int>(structFieldInfo);

    staticExpression = CreateStaticExpression<TestObject, int>(staticFieldInfo);
    instanceExpression = CreateExpression<TestObject, int>(instanceFieldInfo);
    structExpression = CreateExpression<TestStruct, int>(structFieldInfo);
  }

  private static Func<F> CreateStaticExpression<T, F>(FieldInfo field)
  {
    if (!field.IsStatic)
      throw new ArgumentException("Must be static field.");
    MemberExpression expression = Expression.MakeMemberAccess(null, field);
    return Expression.Lambda<Func<F>>(expression).Compile();
  }

  private static Func<T, F> CreateExpression<T, F>(FieldInfo field)
  {
    ParameterExpression parameter = Expression.Variable(typeof(T), "instance");
    MemberExpression expression = Expression.MakeMemberAccess(parameter, field);
    return Expression.Lambda<Func<T, F>>(expression, field.IsStatic ? null : parameter).Compile();
  }

  [Benchmark(Label = "Static FieldRef")]
  private static void Static_FieldRef(ref FieldSetterContext context)
  {
    staticFieldRef.Invoke() = context.assignee;
  }

  [Benchmark(Label = "Static Reflection")]
  private static void Static_Reflection(ref FieldSetterContext context)
  {
    staticFieldInfo.SetValue(null, context.assignee);
  }

  [Benchmark(Label = "Static Expression")]
  private static void Static_Expression(ref FieldSetterContext context)
  {
    _ = staticExpression();
  }

  [Benchmark(Label = "Instance FieldRef")]
  private static void FieldRef_Instance(ref FieldSetterContext context)
  {
    instanceFieldRef.Invoke(context.classObj) = context.assignee;
  }

  [Benchmark(Label = "Instance Reflection")]
  private static void Instance_Reflection(ref FieldSetterContext context)
  {
    instanceFieldInfo.SetValue(context.classObj, context.assignee);
  }

  [Benchmark(Label = "Instance Expression")]
  private static void Instance_Expression(ref FieldSetterContext context)
  {
    _ = instanceExpression(context.classObj);
  }

  [Benchmark(Label = "Struct FieldRef")]
  private static void Struct_FieldRef(ref FieldSetterContext context)
  {
    structFieldRef.Invoke(ref context.structObj) = context.assignee;
  }

  [Benchmark(Label = "Struct Reflection")]
  private static void Struct_Reflection(ref FieldSetterContext context)
  {
    structFieldInfo.SetValue(context.structObj, context.assignee);
  }

  [Benchmark(Label = "Struct Expression")]
  private static void Struct_Expression(ref FieldSetterContext context)
  {
    _ = structExpression(context.structObj);
  }

  #pragma warning disable CS0169
  private class TestObject
  {
    private static int somePrivateStaticInt;
    private int somePrivateInt;
  }

  private struct TestStruct
  {
    private int somePrivateInt;
  }

  private struct FieldSetterContext
  {
    public readonly int assignee;

    public TestObject classObj;
    public TestStruct structObj;

    public FieldSetterContext()
    {
      assignee = Rand.Int;
      classObj = new TestObject();
      structObj = new TestStruct();
    }
  }
}