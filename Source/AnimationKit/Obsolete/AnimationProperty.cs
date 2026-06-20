using System;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using SmashTools.Xml;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Animations;

public class AnimationProperty : IXmlExport, ISelectableUI
{
  /// <summary>
  /// Path from object parent to field.
  /// e.g. Vector3.x
  /// </summary>
  private readonly ObjectPath objectPath;

  private readonly string label;
  private readonly string name;
  private PropertyType propertyType;

  // Strictly used for serialization, UnityEngine types are not supported by RimWorld's parser
  // so the types are resolved post-load w/ these strings.
  [AssignedFromXml]
  private readonly string type;
  [AssignedFromXml]
  private readonly string animatorType;
  [AssignedFromXml]
  public AnimationCurve curve = new();

  [Unsaved]
  private Type loadedType;

  [Unsaved]
  private Type loadedAnimatorType;

  /// <summary>
  /// Color of property in animation curve tab
  /// </summary>
  [Unsaved]
  private Color color;

  /// <summary>
  /// Sets value of field given value from curve evaluation
  /// </summary>
  [Unsaved]
  private SetValue evaluateValue;

  /// <summary>
  /// Sets value of field passed in from function.
  /// </summary>
  /// <remarks>Note: Must be able to convert to field's type</remarks>
  [Unsaved]
  private SetValue setValue;

  /// <summary>
  /// Gets value of field from property instance
  /// </summary>
  [Unsaved]
  private GetValue getValue;

  public delegate float GetValue(IAnimationObject animator);

  public delegate void SetValue(IAnimationObject animator, float value);

  public AnimationProperty()
  {
  }

  private AnimationProperty(Type animatorType, string label, string name, Type type,
    ObjectPath objectPath)
  {
    loadedAnimatorType = animatorType;
    this.label = label;
    this.name = name;
    loadedType = type;
    this.objectPath = objectPath;
  }

  public string Label => label;

  public string Name => name;

  public Type Type => loadedType;

  public Type AnimatorType => loadedAnimatorType;

  public PropertyType PropType => propertyType;

  public bool IsValid => curve != null;

  public GetValue GetProperty => getValue;

  public SetValue SetProperty => setValue;

  public FieldInfo FieldInfo { get; private set; }

  public Color Color
  {
    get
    {
      if (color == Color.clear)
      {
        color = UnityEngine.Random.ColorHSV(0f, 1, 1, 1, 0.75f, 1);
      }
      return color;
    }
  }

  public void Evaluate(IAnimationObject obj, int frame) => evaluateValue.Invoke(obj, frame);

  public static AnimationProperty Create(Type animatorType, string label, FieldInfo fieldInfo,
    ObjectPath objectPath)
  {
    AnimationProperty animationProperty = new(animatorType, label, fieldInfo.Name,
      fieldInfo.DeclaringType, objectPath);
    animationProperty.propertyType = PropertyTypeFrom(fieldInfo.FieldType);
    animationProperty.ResolveReferences();
    return animationProperty;
  }

  internal void ResolveReferences()
  {
    // Unity types are not parsed by RimWorld so must first check cached string -> type map.
    if (loadedType == null && !AnimationPropertyRegistry.CachedTypeByName(type, out loadedType))
    {
      loadedType = ParseHelper.ParseType(type);
    }
    if (loadedAnimatorType == null &&
      !AnimationPropertyRegistry.CachedTypeByName(animatorType, out loadedAnimatorType))
    {
      loadedAnimatorType = ParseHelper.ParseType(animatorType);
    }

    try
    {
      Assert.IsTrue(propertyType > PropertyType.Invalid,
        "AnimationProperty has not been properly initialized");
      FieldInfo = AccessTools.Field(Type, name);
      if (FieldInfo != null)
      {
        GenerateEvaluateCurveMethod();
        GenerateSetValueMethod();
        GenerateGetValueMethod();
      }
      else
      {
        Log.Error($"Unable to load {Type.Name}.{name} for animation.");
      }
    }
    catch (Exception ex)
    {
      Log.Error($"Exception caught while generating dynamic methods. Exception={ex}");
    }
  }

  private void GenerateEvaluateCurveMethod()
  {
    FieldInfo curveField = AccessTools.Field(typeof(AnimationProperty), nameof(curve));
    Assert.IsNotNull(curveField);
    MethodInfo curveFunction =
      AccessTools.Method(typeof(AnimationCurve), nameof(AnimationCurve.Function));
    Assert.IsNotNull(curveFunction);

    DynamicMethod method = new DynamicMethod("EvaluateCurveForProperty",
      typeof(void), // Return type
      [
        typeof(AnimationProperty), typeof(IAnimationObject), typeof(float)
      ], // this*, parent, frame
      typeof(AnimationProperty).Module, // SmashTools.dll
      true); // Skip visibility checks

    ILGenerator ilg = method.GetILGenerator();

    // (T)animator
    ilg.Emit(OpCodes.Ldarg_1);
    ilg.Emit(OpCodes.Castclass, AnimatorType);

    if (objectPath != null)
    {
      FieldInfo field = objectPath.FieldInfo;
      if (field.FieldType.IsValueType)
      {
        ilg.Emit(OpCodes.Ldflda, field);
      }
      else
      {
        ilg.Emit(OpCodes.Ldfld, field);
      }
    }

    // this.curve.Function(frame)
    ilg.Emit(OpCodes.Ldarg_0);
    ilg.Emit(OpCodes.Ldfld, curveField);
    ilg.Emit(OpCodes.Ldarg_2); // frame
    ilg.Emit(OpCodes.Callvirt, curveFunction);

    switch (propertyType)
    {
      // (int)value
      case PropertyType.Int:
        Assert.IsTrue(FieldInfo.FieldType == typeof(int));
        ilg.Emit(OpCodes.Conv_I4);
        break;
      // value != 0
      case PropertyType.Bool:
        Assert.IsTrue(FieldInfo.FieldType == typeof(bool));
        ilg.Emit(OpCodes.Ldc_R4, 0f);
        ilg.Emit(OpCodes.Ceq);
        ilg.Emit(OpCodes.Ldc_I4_0);
        ilg.Emit(OpCodes.Ceq);
        break;
      default:
        Assert.IsTrue(FieldInfo.FieldType == typeof(float));
        break;
    }
    // parent.field = value
    ilg.Emit(OpCodes.Stfld, FieldInfo);
    ilg.Emit(OpCodes.Ret);
    evaluateValue = (SetValue)method.CreateDelegate(typeof(SetValue), this);
  }

  private void GenerateGetValueMethod()
  {
    DynamicMethod method = new DynamicMethod("GetValueForProperty",
      typeof(float), // Return type
      [typeof(AnimationProperty), typeof(IAnimationObject)], // this*, parent
      typeof(AnimationProperty).Module, // SmashTools.dll
      true); // Skip visibility checks

    ILGenerator ilg = method.GetILGenerator();

    // (T)animator
    ilg.Emit(OpCodes.Ldarg_1);
    ilg.Emit(OpCodes.Castclass, AnimatorType);

    if (objectPath != null)
    {
      FieldInfo field = objectPath.FieldInfo;
      if (field.FieldType.IsValueType)
      {
        ilg.Emit(OpCodes.Ldflda, field);
      }
      else
      {
        ilg.Emit(OpCodes.Ldfld, field);
      }
    }

    // [parameter] value
    ilg.Emit(OpCodes.Ldfld, FieldInfo);

    switch (propertyType)
    {
      // (int)value
      case PropertyType.Int:
        Assert.IsTrue(FieldInfo.FieldType == typeof(int));
        ilg.Emit(OpCodes.Conv_R4);
        break;
      // value = bool ? 1f : 0f
      case PropertyType.Bool:
        Assert.IsTrue(FieldInfo.FieldType == typeof(bool));
        Label brTrueLabel = ilg.DefineLabel();
        Label brLabel = ilg.DefineLabel();
        ilg.Emit(OpCodes.Brtrue_S, brTrueLabel);
        ilg.Emit(OpCodes.Ldc_I4_0);
        ilg.Emit(OpCodes.Br_S, brLabel);
        ilg.MarkLabel(brTrueLabel);
        ilg.Emit(OpCodes.Ldc_I4_1);
        ilg.MarkLabel(brLabel);
        ilg.Emit(OpCodes.Conv_R4);
        break;
      default:
        Assert.IsTrue(FieldInfo.FieldType == typeof(float));
        break;
    }

    ilg.Emit(OpCodes.Ret);
    getValue = (GetValue)method.CreateDelegate(typeof(GetValue), this);
  }

  private void GenerateSetValueMethod()
  {
    DynamicMethod method = new DynamicMethod("SetValueForProperty",
      typeof(void), // Return type
      [
        typeof(AnimationProperty), typeof(IAnimationObject), typeof(float)
      ], // this*, parent, value
      typeof(AnimationProperty).Module, // SmashTools.dll
      true); // Skip visibility checks

    ILGenerator ilg = method.GetILGenerator();

    // (T)animator
    ilg.Emit(OpCodes.Ldarg_1);
    ilg.Emit(OpCodes.Castclass, AnimatorType);

    if (objectPath != null)
    {
      FieldInfo field = objectPath.FieldInfo;
      if (field.FieldType.IsValueType)
      {
        ilg.Emit(OpCodes.Ldflda, field);
      }
      else
      {
        ilg.Emit(OpCodes.Ldfld, field);
      }
    }

    // [parameter] value
    ilg.Emit(OpCodes.Ldarg_2);

    switch (propertyType)
    {
      // (int)value
      case PropertyType.Int:
        Assert.IsTrue(FieldInfo.FieldType == typeof(int));
        ilg.Emit(OpCodes.Conv_I4);
        break;
      // value != 0
      case PropertyType.Bool:
        Assert.IsTrue(FieldInfo.FieldType == typeof(bool));
        ilg.Emit(OpCodes.Ldc_R4, 0f);
        ilg.Emit(OpCodes.Ceq);
        ilg.Emit(OpCodes.Ldc_I4_0);
        ilg.Emit(OpCodes.Ceq);
        break;
      default:
        Assert.IsTrue(FieldInfo.FieldType == typeof(float));
        break;
    }
    // parent.field = value
    ilg.Emit(OpCodes.Stfld, FieldInfo);
    ilg.Emit(OpCodes.Ret);
    setValue = (SetValue)method.CreateDelegate(typeof(SetValue), this);
  }

  public static PropertyType PropertyTypeFrom(Type type)
  {
    if (type == typeof(float))
    {
      return PropertyType.Float;
    }
    if (type == typeof(int))
    {
      return PropertyType.Int;
    }
    if (type == typeof(bool))
    {
      return PropertyType.Bool;
    }
    throw new NotImplementedException(
      $"{type} is not a supported PropertyType for keyframe-level animation properties.");
  }

  void IXmlExport.Export()
  {
    XmlExporter.WriteElement(nameof(objectPath), objectPath);
    XmlExporter.WriteElement(nameof(label), label);
    XmlExporter.WriteElement(nameof(name), name);
    XmlExporter.WriteElement(nameof(type), GenTypes.GetTypeNameWithoutIgnoredNamespaces(Type));
    XmlExporter.WriteElement(nameof(animatorType),
      GenTypes.GetTypeNameWithoutIgnoredNamespaces(AnimatorType));
    XmlExporter.WriteObject(nameof(propertyType), propertyType);
    XmlExporter.WriteElement(nameof(curve), curve);
  }

  public enum PropertyType
  {
    Invalid,
    Float,
    Int,
    Bool
  }
}