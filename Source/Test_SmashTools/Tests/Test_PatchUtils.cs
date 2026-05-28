using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using DevTools.Testing;
using HarmonyLib;
using SmashTools.Patching;
using UnityEngine.Assertions;

namespace SmashTools.Testing;

[TestFixture(TestType.MainMenu)]
[TestCategory(TestCategoryNames.Utils)]
[TestDescription("Various utility methods for Harmony")]
internal class Test_PatchUtils
{
  private static readonly MethodBase EnumerableMethod =
    AccessTools.Method(typeof(Test_PatchUtils), nameof(MockEnumerable));
  private static readonly MethodBase EnumerableDelegateMethod =
    AccessTools.Method(typeof(Test_PatchUtils), nameof(MockEnumerableWithDelegate));

  private Func<int> action;

  [Test]
  private void GetStateMachineType()
  {
    var body = PatchProcessor.ReadMethodBody(EnumerableMethod).ToList();
    var constructor = (ConstructorInfo)body.First(code => code.Key == OpCodes.Newobj).Value;
    Type type = EnumerableMethod.GetStateMachineType();
    Assert.AreEqual(constructor.DeclaringType, type);
  }

  [Test]
  private void FindDelegateMethodByType()
  {
    Type enumeratorType = EnumerableDelegateMethod.GetStateMachineType();
    MethodBase iteratorMethod = enumeratorType.GetIteratorMethod();
    MethodBase func = iteratorMethod.FindDelegateMethod(typeof(Func<int>));
    MethodBase method = AccessTools.Method(typeof(Test_PatchUtils), nameof(Foo));
    Assert.AreEqual(method, func);
  }

  [Test]
  private void FindDelegateMethodByField()
  {
    Type enumeratorType = EnumerableDelegateMethod.GetStateMachineType();
    MethodBase iteratorMethod = enumeratorType.GetIteratorMethod();
    FieldInfo field = AccessTools.Field(typeof(Test_PatchUtils), nameof(action));
    MethodBase func = iteratorMethod.FindDelegateMethod(field);
    MethodBase method = AccessTools.Method(typeof(Test_PatchUtils), nameof(Foo));
    Assert.AreEqual(method, func);
  }

  [Test]
  private void FindDelegateMethodByName()
  {
    Type enumeratorType = EnumerableDelegateMethod.GetStateMachineType();
    MethodBase iteratorMethod = enumeratorType.GetIteratorMethod();
    MethodBase func = iteratorMethod.FindDelegateMethod("action");
    MethodBase method = AccessTools.Method(typeof(Test_PatchUtils), nameof(Foo));
    Assert.AreEqual(method, func);
  }

  private IEnumerable<int> MockEnumerable()
  {
    yield return 1;
    yield return 2;
    yield return 3;
  }

  [MethodImpl(MethodImplOptions.NoOptimization)]
  private IEnumerable<int> MockEnumerableWithDelegate()
  {
    action = Foo;
    yield return 1;
    yield return 2;
    yield return 3;
    yield return action();
  }

  private static int Foo()
  {
    return 4;
  }
}
