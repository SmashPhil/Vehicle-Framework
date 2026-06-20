using System;
using System.Collections.Generic;
using RimWorld;
using SmashTools.Xml;
using UnityEngine;
using Verse;
using Verse.Sound;
using ParamType = SmashTools.Animations.AnimationParameter.ParamType;

namespace SmashTools.Animations
{
  public class AnimationCondition : IXmlExport
  {
    private const float UIDropdownPadding = 5;

    private string def;
    private ComparisonType comparison = ComparisonType.Equal;
    private float value;

    [Unsaved]
    private AnimationParameterDef paramDef;

    [Unsaved]
    private string inputBuffer;

    [Unsaved]
    private AnimationParameter parameter;

    public AnimationTransition Transition { get; internal set; }

    public AnimationParameterDef Def
    {
      get { return paramDef; }
      private set
      {
        if (value == paramDef) return;
        paramDef = value;
        def = paramDef.defName;
      }
    }

    public AnimationParameter Parameter
    {
      get
      {
        if (parameter == null)
        {
          ResolveParameter();
        }
        return parameter;
      }
      internal set
      {
        if (parameter == value) return;
        parameter = value;
        if (parameter != null)
        {
          Def = parameter.def;
        }
      }
    }

    public bool ConditionMet(float value)
    {
      switch (Parameter.Type)
      {
        case ParamType.Float:
        {
          return comparison switch
          {
            ComparisonType.GreaterThan => value > this.value,
            _                          => value < this.value,
          };
        }
        case ParamType.Int:
        {
          return comparison switch
          {
            ComparisonType.GreaterThan => value > this.value,
            ComparisonType.LessThan => value < this.value,
            ComparisonType.Equal => value == this.value,
            ComparisonType.NotEqual => value != this.value,
            _ => throw new NotSupportedException($"Integer comparision type: {comparison}"),
          };
        }
        case ParamType.Bool:
        case ParamType.Trigger:
          return value == this.value;
      }
      throw new NotImplementedException(Parameter.Type.ToString());
    }

    public void DrawConditionInput(Rect rect)
    {
      Rect dragHandleRect = new Rect(rect.x, rect.y, 24, 24);

      rect.xMin += dragHandleRect.width;

      if (Event.current != null && Event.current.type == EventType.MouseDown &&
        Event.current.button == 1 && Mouse.IsOver(rect))
      {
        Event.current.Use();
        List<FloatMenuOption> options =
        [
          new("Delete".Translate(), delegate() { Transition.conditions.Remove(this); })
        ];

        Find.WindowStack.Add(new FloatMenu(options));
      }

      List<AnimationParameter> parameters = Transition.FromState.Layer.Controller.parameters;

      if (Parameter.Type == ParamType.Float)
      {
        FieldFloat(rect);
      }
      else if (Parameter.Type == ParamType.Int)
      {
      }
      else if (Parameter.Type == ParamType.Bool)
      {
        FieldBool(rect);
      }
      else if (Parameter.Type == ParamType.Trigger)
      {
      }
    }

    private void FieldFloat(Rect rect)
    {
      List<AnimationParameter> parameters = Transition.FromState.Layer.Controller.parameters;
      Rect[] rects = rect.SplitVertically([0.4f, 0.3f, 0.3f], UIDropdownPadding);
      Rect parameterRect = rects[0];
      Rect comparisonRect = rects[1];
      Rect inputRect = rects[2];
      if (AnimationEditor.Dropdown(parameterRect, Parameter?.Name ?? string.Empty, null))
      {
        if (!parameters.NullOrEmpty())
        {
          List<FloatMenuOption> options = new List<FloatMenuOption>();
          foreach (AnimationParameter parameter in parameters)
          {
            options.Add(new FloatMenuOption(parameter.Name, delegate() { Parameter = parameter; }));
          }
          Find.WindowStack.Add(new FloatMenu(options));
        }
        else
        {
          SoundDefOf.ClickReject.PlayOneShotOnCamera();
        }
      }
      if (AnimationEditor.Dropdown(comparisonRect, comparison.ToString() ?? string.Empty, null))
      {
        List<FloatMenuOption> options = new List<FloatMenuOption>
        {
          new FloatMenuOption(ComparisonType.LessThan.ToString(),
            () => comparison = ComparisonType.LessThan),
          new FloatMenuOption(ComparisonType.GreaterThan.ToString(),
            () => comparison = ComparisonType.GreaterThan)
        };
        Find.WindowStack.Add(new FloatMenu(options));
      }
      Widgets.TextFieldNumeric(parameterRect, ref value, ref inputBuffer, min: float.MinValue,
        float.MaxValue);
    }

    private void FieldInt(Rect rect)
    {
    }

    private void FieldBool(Rect rect)
    {
      List<AnimationParameterDef> parameters =
        DefDatabase<AnimationParameterDef>.AllDefsListForReading;
      Rect checkboxRect = new Rect(rect.xMax - 24, rect.y, 24, 24);
      Rect parameterRect = new Rect(rect)
      {
        width = rect.width - checkboxRect.width - UIDropdownPadding
      };

      if (AnimationEditor.Dropdown(parameterRect, Parameter?.Name ?? Def?.LabelCap ?? "NULL", null))
      {
        if (!parameters.NullOrEmpty())
        {
          List<FloatMenuOption> options = [];
          foreach (AnimationParameterDef paramDef in parameters)
          {
            options.Add(new FloatMenuOption(paramDef.LabelCap,
              delegate() { Parameter = new AnimationParameter(paramDef); }));
          }
          Find.WindowStack.Add(new FloatMenu(options));
        }
        else
        {
          SoundDefOf.ClickReject.PlayOneShotOnCamera();
        }
      }
      bool checkOn = value != 0;
      //UIElements.CheckboxButton(checkboxRect, ref checkOn);
      value = checkOn ? 1 : 0;
    }

    public void ResolveParameter()
    {
      if (Def == null && !def.NullOrEmpty())
      {
        paramDef = DefDatabase<AnimationParameterDef>.GetNamed(def);
        Parameter = new AnimationParameter(Def);
      }
    }

    internal void ResolveReferences()
    {
      ResolveParameter();
    }

    public void Export()
    {
      XmlExporter.WriteObject(nameof(def), def);
      XmlExporter.WriteObject(nameof(comparison), comparison);
      XmlExporter.WriteObject(nameof(value), value);
    }
  }
}