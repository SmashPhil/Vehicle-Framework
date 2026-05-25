using System;
using System.Collections.Generic;
using System.Reflection;
using DevTools.Testing;
using HarmonyLib;
using JetBrains.Annotations;
using SmashTools.Animations;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Testing;

[TestFixture(TestType.MainMenu)]
[TestDescription(
  "DynamicMethod generation and Set / Get delegates used for animation value changes.")]
internal sealed class Test_AnimDynamicMethod
{
  private const int TestCount = 10;

  private TestObject testObject;

  [SetUp]
  private void CreateTestObject()
  {
    testObject = new TestObject();
  }

  [TearDown]
  private void ClearTestObject()
  {
    testObject = null;
  }

  [Test]
  private void Primitive()
  {
    TestField(testObject, typeof(TestObject), nameof(TestObject.tInt), null);
    TestField(testObject, typeof(TestObject), nameof(TestObject.tFloat), null);
    TestField(testObject, typeof(TestObject), nameof(TestObject.tBool), null);
  }

  [Test]
  private void Vector()
  {
    FieldInfo vector3Field = AccessTools.Field(typeof(TestObject), nameof(TestObject.vector));
    ObjectPath vector3Path = new(vector3Field);
    Assert.IsNotNull(vector3Field);
    TestField(testObject, typeof(Vector3), nameof(Vector3.x), vector3Path);
    TestField(testObject, typeof(Vector3), nameof(Vector3.y), vector3Path);
    TestField(testObject, typeof(Vector3), nameof(Vector3.z), vector3Path);
  }

  [Test]
  private void UnityColor()
  {
    FieldInfo colorField = AccessTools.Field(typeof(TestObject), nameof(TestObject.color));
    ObjectPath colorPath = new(colorField);
    Assert.IsNotNull(colorField);
    TestField(testObject, typeof(Color), nameof(Color.r), colorPath);
    TestField(testObject, typeof(Color), nameof(Color.g), colorPath);
    TestField(testObject, typeof(Color), nameof(Color.b), colorPath);
    TestField(testObject, typeof(Color), nameof(Color.a), colorPath);
  }

  [Test]
  private void IntVec()
  {
    FieldInfo intVec3Field = AccessTools.Field(typeof(TestObject), nameof(TestObject.intVec3));
    ObjectPath intVec3Path = new(intVec3Field);
    Assert.IsNotNull(intVec3Field);
    TestField(testObject, typeof(IntVec3), nameof(IntVec3.x), intVec3Path);
    TestField(testObject, typeof(IntVec3), nameof(IntVec3.y), intVec3Path);
    TestField(testObject, typeof(IntVec3), nameof(IntVec3.z), intVec3Path);
  }

  [Test]
  private void NestedObject()
  {
    FieldInfo nestedObjField =
      AccessTools.Field(typeof(TestObject), nameof(TestObject.nestedObject));
    ObjectPath nestedObjPath = new(nestedObjField);
    Assert.IsNotNull(nestedObjField);
    TestField(testObject, typeof(NestedTestObject), nameof(NestedTestObject.tInt),
      nestedObjPath);
    TestField(testObject, typeof(NestedTestObject), nameof(NestedTestObject.tFloat),
      nestedObjPath);
    TestField(testObject, typeof(NestedTestObject), nameof(NestedTestObject.tBool),
      nestedObjPath);
  }

  private static void TestField(IAnimator animator, Type type, string name, ObjectPath objectPath)
  {
    Assert.IsNotNull(animator);
    FieldInfo fieldInfo = AccessTools.Field(type, name);
    Type objType = animator.GetType();
    // Hierarchy and Object paths are the same since TestObject has no depth
    AnimationProperty property =
      AnimationProperty.Create(objType, name, fieldInfo, objectPath);
    (int frame, float value)[] results = new (int frame, float value)[TestCount];
    for (int i = 0; i < results.Length; i++)
    {
      float value = Rand.Range(0, 99999);
      results[i] = (i, value);
      property.curve.Add(i, value);
    }
    // Curve evaluation
    for (int i = 0; i < results.Length; i++)
    {
      // Only testing the actual KeyFrame values to make sure they are assigned
      // to the object. Any tests on curve evaluations should be done separately
      property.Evaluate(animator, i);

      object container = animator;
      if (objectPath != null)
      {
        // Traverse down the object path to get to the property value
        container = objectPath.FieldInfo.GetValue(container);
      }

      object value = fieldInfo.GetValue(container);
      Expect.IsTrue(IsEqual(value, results[i].value), $"CurveEval {type.Name}.{name}");
    }
    // Set / Get
    {
      object container = animator;
      if (objectPath != null)
      {
        // Traverse down the object path to get to the property value
        container = objectPath.FieldInfo.GetValue(container);
      }

      object value = fieldInfo.GetValue(container);
      float getValue = property.GetProperty(animator);
      Expect.IsTrue(IsEqual(value, getValue), $"GetValue {type.Name}.{name}");

      property.SetProperty(animator, 0);
      float setValue = property.GetProperty(animator);
      Expect.AreApproximatelyEqual(setValue, 0f, $"SetValue {type.Name}.{name}");
    }
  }

  private static bool IsEqual(object lhs, float rhs)
  {
    return lhs switch
    {
      int i   => i == (int)rhs,
      float f => Mathf.Approximately(f, rhs),
      bool b  => b == rhs > 0,
      _       => false,
    };
  }

  private class TestObject : IAnimator
  {
    [UsedWithReflection]
    public int tInt;
    [UsedWithReflection]
    public float tFloat;
    [UsedWithReflection]
    public bool tBool;

    public Vector3 vector = new();
    public Color color = new();
    public IntVec3 intVec3 = new();

    public NestedTestObject nestedObject = new();

    public List<NestedTestObject> nestedObjectList = [];

    // None of these should be getting called, these are strictly for allowing this object to pass
    // as an IAnimator object to SetValue and GetValue delegates.

    AnimationManager IAnimator.Manager => throw new NotImplementedException();

    ModContentPack IAnimator.ModContentPack => throw new NotImplementedException();

    Vector3 IAnimator.DrawPos => throw new NotImplementedException();

    string IAnimationObject.ObjectId => throw new NotImplementedException();
  }

  private class NestedTestObject : IAnimationObject
  {
    public int tInt = 0;
    public float tFloat = 0;
    public float tBool = 0;

    public string ObjectId => throw new NotImplementedException();
  }
}