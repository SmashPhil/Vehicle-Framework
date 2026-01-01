using System;
using System.Linq.Expressions;
using System.Reflection;
using DevTools.Benchmarking;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace SmashTools.Performance;

[BenchmarkClass("FieldRef")]
[Measurement(Benchmark.Measurement.Microseconds)]
internal class Benchmark_FieldRef
{
  private static readonly FieldInfo StaticFieldInfo;
  private static readonly FieldInfo InstanceFieldInfo;
  private static readonly FieldInfo StructFieldInfo;

  private static readonly AccessTools.FieldRef<TestObject, int> StaticFieldRef;
  private static readonly AccessTools.FieldRef<TestObject, int> InstanceFieldRef;
  private static readonly AccessTools.StructFieldRef<TestStruct, int> StructFieldRef;

  private static readonly Func<int> StaticExpression;
  private static readonly Func<TestObject, int> InstanceExpression;
  private static readonly Func<TestStruct, int> StructExpression;

  static Benchmark_FieldRef()
  {
    StaticFieldInfo = AccessTools.Field(typeof(TestObject), "somePrivateStaticInt");
    InstanceFieldInfo = AccessTools.Field(typeof(TestObject), "somePrivateInt");
    StructFieldInfo = AccessTools.Field(typeof(TestStruct), "somePrivateInt");

    StaticFieldRef = AccessTools.FieldRefAccess<TestObject, int>(StaticFieldInfo);
    InstanceFieldRef = AccessTools.FieldRefAccess<TestObject, int>(InstanceFieldInfo);
    StructFieldRef = AccessTools.StructFieldRefAccess<TestStruct, int>(StructFieldInfo);

    StaticExpression = CreateStaticExpression<int>(StaticFieldInfo);
    InstanceExpression = CreateExpression<TestObject, int>(InstanceFieldInfo);
    StructExpression = CreateExpression<TestStruct, int>(StructFieldInfo);
  }

  private static Func<F> CreateStaticExpression<F>(FieldInfo field)
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
    StaticFieldRef.Invoke() = context.assignee;
  }

  [Benchmark(Label = "Static Reflection")]
  private static void Static_Reflection(ref FieldSetterContext context)
  {
    StaticFieldInfo.SetValue(null, context.assignee);
  }

  [Benchmark(Label = "Static Expression")]
  private static void Static_Expression([UsedImplicitly] ref FieldSetterContext context)
  {
    _ = StaticExpression();
  }

  [Benchmark(Label = "Instance FieldRef")]
  private static void FieldRef_Instance(ref FieldSetterContext context)
  {
    InstanceFieldRef.Invoke(context.classObj) = context.assignee;
  }

  [Benchmark(Label = "Instance Reflection")]
  private static void Instance_Reflection(ref FieldSetterContext context)
  {
    InstanceFieldInfo.SetValue(context.classObj, context.assignee);
  }

  [Benchmark(Label = "Instance Expression")]
  private static void Instance_Expression(ref FieldSetterContext context)
  {
    _ = InstanceExpression(context.classObj);
  }

  [Benchmark(Label = "Struct FieldRef")]
  private static void Struct_FieldRef(ref FieldSetterContext context)
  {
    StructFieldRef.Invoke(ref context.structObj) = context.assignee;
  }

  [Benchmark(Label = "Struct Reflection")]
  private static void Struct_Reflection(ref FieldSetterContext context)
  {
    StructFieldInfo.SetValue(context.structObj, context.assignee);
  }

  [Benchmark(Label = "Struct Expression")]
  private static void Struct_Expression(ref FieldSetterContext context)
  {
    _ = StructExpression(context.structObj);
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

    public readonly TestObject classObj;
    public TestStruct structObj;

    public FieldSetterContext()
    {
      assignee = Rand.Int;
      classObj = new TestObject();
      structObj = new TestStruct();
    }
  }
}