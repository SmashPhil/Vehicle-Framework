using System;
using RimWorld;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AnimationKit;

public class AnimationParameter
{
  private const float ContractedBy = 2;

  private string inputBuffer;

  public AnimationParameter()
  {
  }

  public ParamType Type
  {
    get;
    internal set
    {
      field = value;
    }
  }

  public string Name
  {
    get;
    internal set
    {
      field = value;
    }
  }

  public float Value
  {
    get;
    internal set
    {
      field = value;
    }
  }

  public void DrawInput(Rect rect)
  {
    switch (Type)
    {
      case ParamType.Float:
        DrawFloatInput(rect);
        break;
      case ParamType.Int:
        DrawIntInput(rect);
        break;
      case ParamType.Bool:
        DrawBoolInput(rect);
        break;
      case ParamType.Trigger:
        DrawTriggerInput(rect);
        break;
      default:
        throw new NotImplementedException(nameof(ParamType));
    }
  }

  private void DrawFloatInput(Rect rect)
  {
    float value = Value;
    Widgets.TextFieldNumeric(rect, ref value, ref inputBuffer, min: float.MinValue, max: float.MaxValue);
    if (!Mathf.Approximately(Value, value))
    {
      Value = value;
    }
  }

  private void DrawIntInput(Rect rect)
  {
    float value = Value;
    Widgets.TextFieldNumeric(rect, ref value, ref inputBuffer, min: int.MinValue, max: int.MaxValue);
    if (!Mathf.Approximately(Value, value))
    {
      Value = value;
    }
  }

  private void DrawBoolInput(Rect rect)
  {
    bool checkOn = Value != 0;
    Widgets.Checkbox(rect.position, ref checkOn, size: rect.height - ContractedBy * 2);
    bool checkBefore = Value != 0;
    if (checkBefore != checkOn)
    {
      Value = checkOn ? 1 : 0;
    }
  }

  private void DrawTriggerInput(Rect rect)
  {
    bool checkOn = Value != 0;
    Texture2D buttonTex = checkOn ? Widgets.RadioButOnTex : UIData.RadioButOffTex;
    Rect buttonRect = new Rect(rect.x, rect.y, rect.height, rect.height).ContractedBy(ContractedBy);

    using TextBlock block = new(GUI.color);
    if (!GUI.enabled)
    {
      GUI.color = Color.gray;
    }
    GUI.DrawTexture(buttonRect, buttonTex);
    if (Widgets.ButtonInvisible(buttonRect))
    {
      checkOn = !checkOn;
      Value = checkOn ? 1 : 0;
      SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
    }
  }

  public enum ParamType : byte
  {
    Float = 0,
    Int = 1,
    Bool = 2,
    Trigger = 3
  }
}
