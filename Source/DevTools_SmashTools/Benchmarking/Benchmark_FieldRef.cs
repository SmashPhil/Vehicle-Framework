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

  static Benchmark_FieldRef()
  {
    staticFieldInfo = AccessTools.Field(typeof(TestObject), "somePrivateStaticInt");
    instanceFieldInfo = AccessTools.Field(typeof(TestObject), "somePrivateInt");
    structFieldInfo = AccessTools.Field(typeof(TestStruct), "somePrivateInt");

    staticFieldRef = AccessTools.FieldRefAccess<TestObject, int>(staticFieldInfo);
    instanceFieldRef = AccessTools.FieldRefAccess<TestObject, int>(instanceFieldInfo);
    structFieldRef = AccessTools.StructFieldRefAccess<TestStruct, int>(structFieldInfo);
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