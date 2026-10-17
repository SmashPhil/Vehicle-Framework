using System;
using RimWorld;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AnimationKit.Editor;

internal abstract class AnimationEditor
{
  public const float DropdownWidth = 300;

  // Blend
  protected const float FadeSize = 10;
  protected const int FadeLines = 10;
  protected const float FadeHeight = FadeSize / FadeLines;

  protected readonly Color backgroundLightColor = new ColorInt(63, 63, 63).ToColor;
  protected readonly Color backgroundDopesheetColor = new ColorInt(56, 56, 56).ToColor;
  protected readonly Color backgroundCurvesColor = new ColorInt(40, 40, 40).ToColor;
  protected readonly Color separatorColor = new ColorInt(35, 35, 35).ToColor;

  protected readonly Color buttonColor = new ColorInt(88, 88, 88).ToColor;
  protected readonly Color buttonPressedColor = new ColorInt(70, 96, 124).ToColor;

  protected readonly Color selectBoxFillColor = new ColorInt(85, 145, 245, 15).ToColor;
  protected readonly Color selectBoxBorderColor = new ColorInt(125, 175, 245, 75).ToColor;


  protected readonly Window window;
  protected IAnimator animator;

  protected bool draggingSelectionBox;
  private bool hardDisabled;

  /* ----- Left Panel Resizing ----- */
  private bool resizing;
  private float startingWidth;
  /* ------------------------------- */

  /* ----- Selection Box ----- */
  private Vector2 selectionBoxPos;
  /* ------------------------- */

  public event Action OnChangeMade;

  protected AnimationEditor(Window window)
  {
    this.window = window;
  }

  public abstract void DoWindowContents(Rect rect);

  public virtual void Update()
  {
  }

  public virtual void OnClose()
  {
  }

  protected void ChangeMade()
  {
    OnChangeMade?.Invoke();
  }

  // =========== Keybinding Events ===========

  public virtual void OnGUIHighPriority()
  {
    if (Event.current is not { type: EventType.KeyDown })
      return;

    if (InputEvents.IsControlPressed)
    {
      switch (Event.current.keyCode)
      {
        case KeyCode.S:
          Save();
          break;
        case KeyCode.C:
          CopyToClipboard();
          break;
        case KeyCode.V:
          Paste();
          break;
        case KeyCode.X:
          // TODO - Cut
          break;
        case KeyCode.Z:
          // TODO - Undo
          break;
      }
    }

    switch (Event.current.keyCode)
    {
      case KeyCode.Delete or KeyCode.Backspace:
        Delete();
        break;
      case KeyCode.Escape:
        Escape();
        break;
    }

    Event.current.Use();
  }

  public virtual void Save()
  {
  }

  public virtual void CopyToClipboard()
  {
  }

  public virtual void Paste()
  {
  }

  public virtual void Delete()
  {
  }

  public virtual void Escape()
  {
  }

  // ======== End Keybinding Events ==========

  public virtual void AnimatorSelected(IAnimator anim)
  {
    animator = anim;
  }

  public virtual void OnTabOpen()
  {
  }

  public virtual void ResetToCenter()
  {
  }

  protected void DrawBackground(Rect rect)
  {
    Widgets.DrawBoxSolidWithOutline(rect, StyleUtils.BackgroundDopesheetColor, StyleUtils.SeparatorColor);
  }

  protected void DrawBackgroundDark(Rect rect)
  {
    Widgets.DrawBoxSolidWithOutline(rect, StyleUtils.BackgroundCurvesColor, StyleUtils.SeparatorColor);
  }

  protected void DoSeparatorHorizontal(float x, float y, float length)
  {
    UIElements.DrawLineHorizontal(x, y, length, StyleUtils.SeparatorColor);
  }

  protected void DoSeparatorVertical(float x, float y, float height)
  {
    UIElements.DrawLineVertical(x, y, height, StyleUtils.SeparatorColor);
  }

  protected void EnableGUI(bool hardEnable = false)
  {
    if (hardDisabled && !hardEnable)
    {
      return;
    }
    GUIState.Enable();
  }

  protected void DisableGUI(bool hardDisable = false)
  {
    GUIState.Disable();
    hardDisabled = hardDisable;
  }

  protected bool AnimationButton(Rect rect, Texture2D texture, string tooltip)
  {
    var color = GUI.color;
    if (GUI.enabled && Mouse.IsOver(rect))
    {
      GUI.color = GenUI.MouseoverColor;
    }

    Rect imageRect = rect.ContractedBy(3);
    GUI.DrawTexture(imageRect, texture);

    GUI.color = color;

    if (!tooltip.NullOrEmpty())
    {
      TooltipHandler.TipRegion(rect, tooltip);
    }
    bool result = Widgets.ButtonInvisible(rect);
    if (result)
    {
      SoundDefOf.Click.PlayOneShotOnCamera();
    }
    return result;
  }

  /// <returns>xMax</returns>
  protected bool ToggleText(Rect rect, string label, string tooltip, bool enabled)
  {
    bool pressed = false;
    var anchor = Text.Anchor;
    Text.Anchor = TextAnchor.MiddleCenter;

    DoSeparatorHorizontal(rect.x, rect.y, rect.width);
    DoSeparatorHorizontal(rect.x, rect.yMax, rect.width);
    DoSeparatorVertical(rect.x, rect.y, rect.height);
    DoSeparatorVertical(rect.xMax, rect.y, rect.height);

    var color = GUI.color;
    if (GUI.enabled && Mouse.IsOver(rect))
    {
      GUI.color = new Color(0.75f, 0.75f, 0.75f);
    }
    Widgets.Label(rect, label);
    if (enabled)
    {
      Widgets.DrawBoxSolid(rect.ContractedBy(1), new Color(0.75f, 0.75f, 0.75f, 0.25f));
    }
    if (!tooltip.NullOrEmpty())
    {
      TooltipHandler.TipRegion(rect, tooltip);
    }
    if (Widgets.ButtonInvisible(rect))
    {
      pressed = true;
      SoundDefOf.Click.PlayOneShotOnCamera();
    }
    GUI.color = color;
    Text.Anchor = anchor;
    return pressed;
  }

  protected bool ButtonText(Rect rect, string label)
  {
    using var block = new TextBlock(GameFont.Small, TextAnchor.MiddleCenter);

    bool pressed = false;

    Color buttonColor = StyleUtils.ButtonColor;
    if (GUI.enabled && Mouse.IsOver(rect))
    {
      GUI.color = new Color(0.75f, 0.75f, 0.75f);
      if (Input.GetMouseButton(0))
      {
        buttonColor = StyleUtils.ButtonPressedColor;
      }
    }
    Widgets.DrawBoxSolidWithOutline(rect, buttonColor, StyleUtils.SeparatorColor);
    Widgets.Label(rect, label);
    if (Widgets.ButtonInvisible(rect))
    {
      pressed = true;
      SoundDefOf.Click.PlayOneShotOnCamera();
    }
    return pressed;
  }

  public static bool Dropdown(Rect rect, string label, string tooltip)
  {
    using var block = new TextBlock(GameFont.Small, TextAnchor.MiddleCenter);

    bool pressed = false;

    if (GUI.enabled && Mouse.IsOver(rect))
    {
      GUI.color = new Color(0.75f, 0.75f, 0.75f);
    }
    float dropdownSize = rect.height;
    rect.SplitVertically(rect.width - dropdownSize, out Rect labelRect, out Rect dropdownRect);
    if (Text.CalcSize(label).x > labelRect.width)
    {
      Text.Font--;
    }
    Widgets.Label(labelRect, label.Truncate(labelRect.width));

    Matrix4x4 matrix = GUI.matrix;
    {
      UI.RotateAroundPivot(90, dropdownRect.center);
      GUI.DrawTexture(dropdownRect, TexButton.Reveal);
    }
    GUI.matrix = matrix;

    if (!tooltip.NullOrEmpty())
    {
      TooltipHandler.TipRegion(rect, tooltip);
    }
    if (Widgets.ButtonInvisible(rect))
    {
      pressed = true;
      SoundDefOf.Click.PlayOneShotOnCamera();
    }
    return pressed;
  }

  protected void DoResizerButton(Rect rect, ref float leftWindowSize, float minLeft,
    float minRight)
  {
    float currentWidth = rect.width;

    Rect resizeButtonRect = new(rect.xMax - 24, rect.yMax - 24, 24, 24);
    Vector2 mousePosition = Event.current.mousePosition;
    if (Input.GetMouseButtonDown(0) && Mouse.IsOver(resizeButtonRect))
    {
      resizing = true;
      startingWidth = mousePosition.x;
    }
    if (resizing)
    {
      rect.width = startingWidth + (mousePosition.x - startingWidth);
      rect.width = Mathf.Clamp(rect.width, minLeft, window.windowRect.width - minRight);

      if (!Input.GetMouseButton(0))
      {
        resizing = false;
      }
    }
    Widgets.ButtonImage(resizeButtonRect, TexUI.WinExpandWidget);

    if (!Mathf.Approximately(rect.width, currentWidth))
    {
      leftWindowSize = rect.width;
    }
  }

  protected void CheckTextFieldControlFocus(Rect rect)
  {
    string name = $"TextField{rect.y:F0}{rect.x:F0}";
    bool focused = GUI.GetNameOfFocusedControl() == name;
    if (focused && Input.GetMouseButtonDown(0) && !Mouse.IsOver(rect))
    {
      UI.UnfocusCurrentControl();
    }
  }

  protected bool DragWindow(Rect rect, Action dragAction, Func<bool> isDragging,
    Action dragStarted = null, Action dragStopped = null, int button = 0)
  {
    if (!GUI.enabled || Event.current == null)
    {
      return false;
    }

    if (Event.current.type == EventType.MouseDown && Event.current.button == button && Mouse.IsOver(rect))
    {
      dragStarted?.Invoke();
      Event.current.Use();
    }
    if (isDragging())
    {
      bool mouseUpEvent = Event.current.type == EventType.MouseUp &&
        Event.current.button == button;

      dragAction();
      if (Input.GetMouseButton(button))
      {
        if (UnityGUIBugsFixer.MouseDrag(button))
        {
          Event.current.Use();
        }
      }
      else
      {
        dragStopped?.Invoke();
        if (mouseUpEvent)
        {
          Event.current.Use();
        }
      }
      return true;
    }
    return false;
  }

  protected bool SelectionBox(Vector2 groupPos, Rect visibleRect, Rect clickArea,
    out Rect dragRect,
    float snapX = 0, float snapY = 0, float snapPaddingX = 0, float snapPaddingY = 0)
  {
    dragRect = Rect.zero;
    if (Input.GetMouseButtonDown(0) && Mouse.IsOver(clickArea))
    {
      draggingSelectionBox = true;
      selectionBoxPos = MouseUIPos(groupPos - visibleRect.position);
    }

    if (draggingSelectionBox)
    {
      dragRect = DragRect(groupPos - visibleRect.position, snapX: snapX, snapY: snapY,
        snapPaddingX: snapPaddingX, snapPaddingY: snapPaddingY);
      Widgets.DrawBoxSolidWithOutline(dragRect, StyleUtils.SelectBoxFillColor, StyleUtils.SelectBoxBorderColor);

      using (new TextBlock(GameFont.Small, TextAnchor.MiddleCenter))
      {
        Vector2 size = Text.CalcSize(dragRect.ToString());
        Rect debugLabelRect =
          new(dragRect.xMax - size.x, dragRect.yMax + size.y, size.x, size.y);
        Widgets.DrawBoxSolid(debugLabelRect, new Color(0, 0, 0, 0.25f));
        Widgets.Label(debugLabelRect, dragRect.ToString());
      }
      if (Input.GetMouseButton(0))
      {
        if (UnityGUIBugsFixer.MouseDrag())
        {
          Event.current.Use();
        }
      }
      else
      {
        if (Input.GetMouseButtonUp(0))
        {
          Event.current.Use();
        }
        draggingSelectionBox = false;
        return true;
      }
    }
    return false;
  }

  protected void InputBox(Rect rect, Type type, ref object value, ref string buffer)
  {
    buffer ??= value?.ToString() ?? type.GetDefaultValue().ToString();
    string text = "InputBox" + rect.y.ToString("F0") + rect.x.ToString("F0");
    GUI.SetNextControlName(text);
    string inputText = Widgets.TextField(rect, buffer);
    if (GUI.GetNameOfFocusedControl() != text)
    {
      ResolveParseNow(type, buffer, ref value, ref buffer);
      return;
    }
    if (inputText != buffer)
    {
      buffer = inputText;
      ResolveParseNow(type, inputText, ref value, ref buffer);
    }
  }

  private static void ResolveParseNow(Type type, string edited, ref object value,
    ref string buffer)
  {
    if (!ParseHelper.CanParse(type, edited))
    {
      buffer = null;
      return;
    }
    value = ParseHelper.FromString(edited, type);
  }

  protected Rect DragRect(Vector2 groupPos, float snapX = 0, float snapY = 0,
    float snapPaddingX = 0, float snapPaddingY = 0)
  {
    Vector2 mousePos = MouseUIPos(groupPos);
    if (snapX > 0)
    {
      mousePos.x = mousePos.x.RoundTo(snapX) + snapPaddingX;
    }
    if (snapY > 0)
    {
      mousePos.y = mousePos.y.RoundTo(snapY) + snapPaddingY;
    }

    Vector2 startPos = selectionBoxPos;
    if (snapX > 0)
    {
      startPos.x = startPos.x.RoundTo(snapX) + snapPaddingX;
    }
    if (snapY > 0)
    {
      startPos.y = startPos.y.RoundTo(snapY) + snapPaddingY;
    }

    Vector2 diff = new Vector2(mousePos.x, mousePos.y) - startPos;
    return new Rect(startPos, diff);
  }

  protected static float DrawBlend(Rect rect, Color colorOne, Color colorTwo)
  {
    for (int i = 0; i < FadeLines; i++)
    {
      float t = (float)i / FadeLines;
      float r = Mathf.Lerp(colorOne.r, colorTwo.r, t);
      float g = Mathf.Lerp(colorOne.g, colorTwo.g, t);
      float b = Mathf.Lerp(colorOne.b, colorTwo.b, t);
      float a = colorOne.a;
      if (!Mathf.Approximately(colorOne.a, colorTwo.a))
      {
        a = Mathf.Lerp(colorOne.a, colorTwo.a, t);
      }
      Color fadeColor = new(r, g, b, a);

      Widgets.DrawBoxSolid(rect, fadeColor);
      rect.y += FadeHeight;
    }
    return rect.y;
  }

  protected Vector2 MouseUIPos(Vector2 groupPos)
  {
    Vector2 mousePos = new(UI.MousePositionOnUIInverted.x, UI.MousePositionOnUIInverted.y);
    Vector2 marginSize = new(Window.StandardMargin, Window.StandardMargin);
    return mousePos - window.windowRect.position - groupPos - marginSize;
  }

  protected Vector2 GetScrollPosNormalized(Rect outRect, Vector2 scrollPos, Rect viewRect)
  {
    float widthMax = viewRect.width - outRect.width;
    if (viewRect.height > outRect.height)
    {
      widthMax -= 16;
    }
    float heightMax = viewRect.height - outRect.height;
    if (viewRect.width > outRect.width)
    {
      //heightMax -= 16;
    }

    float xT = widthMax <= 0 ? 1 : scrollPos.x / widthMax;
    float yT = heightMax <= 0 ? 1 : scrollPos.y / heightMax;
    return new Vector2(xT, yT);
  }

  protected void SetScrollPosNormalized(Rect outRect, ref Vector2 scrollPos, Rect viewRect,
    Vector2 normalizedScrollPos)
  {
    float widthMax = viewRect.width - outRect.width + 16;
    float heightMax = viewRect.height - outRect.height + 16;
    scrollPos = new Vector2(normalizedScrollPos.x * widthMax, normalizedScrollPos.y * heightMax);
  }

  protected void TryResetExtraScrollSize(Rect outRect, Vector2 scrollPos, Rect viewRect,
    ref Vector2 extraSize)
  {
    Vector2 scrollT = GetScrollPosNormalized(outRect, scrollPos, viewRect);
    throw new NotImplementedException();
  }

  protected Rect GetVisibleRect(Rect outRect, Vector2 scrollPos, Rect viewRect)
  {
    Vector2 scrollT = GetScrollPosNormalized(outRect, scrollPos, viewRect);
    float visPosX = Mathf.Lerp(0, viewRect.width - outRect.width, scrollT.x);
    float visPosY = Mathf.Lerp(0, viewRect.height - outRect.height, scrollT.y);
    return new Rect(visPosX, visPosY, outRect.width, outRect.height);
  }
}