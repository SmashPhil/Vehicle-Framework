using System;
using System.Threading.Tasks;
using DevTools;
using DevTools.UnitTesting;
using HarmonyLib;
using Verse;

namespace SmashTools.UnitTesting;

// ReSharper disable all
#pragma warning disable CS8500

[UnitTest(TestType.MainMenu)]
[Disabled]
internal class UnitTest_UnsafeReadOnly
{
  [Test]
  private void Reflection()
  {
    TestClass.A();
    TestClass.B();
    ParallelReflection();

    AccessTools.Field(typeof(TestClass), nameof(TestClass.text))
     .SetValue(null, new SomeObject("Hello World"));
    AccessTools.Field(typeof(TestClass), nameof(TestClass.number)).SetValue(null, 5);
    AccessTools.Field(typeof(TestClass), nameof(TestClass.cell))
     .SetValue(null, new IntVec3(5, 5, 5));
    unsafe
    {
      fixed (SomeObject* obj = &TestClass.text)
        *obj = new SomeObject("Hello World");
      fixed (int* number = &TestClass.number)
        *number = 5;
      fixed (IntVec3* cell = &TestClass.cell)
        *cell = new IntVec3(5, 5, 5);
    }

    TestClass.A();
    TestClass.B();
    ParallelReflection();
  }

  [Test]
  private void UnsafeReadOnly()
  {
    TestClass2.A();
    TestClass2.B();
    ParallelNormal();

    unsafe
    {
      fixed (SomeObject* obj = &TestClass2.text)
        *obj = new SomeObject("Bar");
      fixed (int* number = &TestClass2.number)
        *number = 9;
      fixed (IntVec3* cell = &TestClass2.cell)
        *cell = new IntVec3(6, 6, 6);
    }

    TestClass2.A();
    TestClass2.B();
    ParallelNormal();
  }

  public static void ParallelReflection()
  {
    Action action = () => DevLog.Write($"{TestClass.text} {TestClass.number} {TestClass.cell}");

    Action[] actions = [action, action, action, action];
    Parallel.Invoke(actions);
  }

  public static void ParallelNormal()
  {
    Action action = () => DevLog.Write($"{TestClass2.text} {TestClass2.number} {TestClass2.cell}");

    Action[] actions = [action, action, action, action];
    Parallel.Invoke(actions);
  }

  private static class TestClass
  {
    public static readonly SomeObject text = new("Bye");
    public static readonly int number = 7;
    public static readonly IntVec3 cell = new(1, 1, 1);

    public static void A()
    {
      DevLog.Write($"{text} {number} {cell}");
    }

    public static void B() => DevLog.Write($"{C()} {D()} {E()}");

    public static SomeObject C() => text;

    public static int D() => number;

    public static IntVec3 E() => cell;
  }

  private static class TestClass2
  {
    public static readonly SomeObject text = new("Foo");
    public static readonly int number = 8;
    public static readonly IntVec3 cell = new(2, 2, 2);

    public static void A()
    {
      DevLog.Write($"{text} {number} {cell}");
    }

    public static void B() => DevLog.Write($"{C()} {D()} {E()}");

    public static SomeObject C() => text;

    public static int D() => number;

    public static IntVec3 E() => cell;
  }

  private class SomeObject(string name)
  {
    private readonly string name = name;

    public override string ToString()
    {
      return name;
    }
  }
}