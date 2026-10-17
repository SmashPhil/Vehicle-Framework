using UnityEngine;

namespace AnimationKit.Editor;

internal static class InputEvents
{
  public static bool SingleSelect => LeftClickDown && !IsControlPressed && !IsShiftPressed;

  public static bool LeftClickDown => Event.current is { type: EventType.MouseDown, button: 0 };

  public static bool RightClickDown => Event.current is { type: EventType.MouseDown, button: 1 };

  public static bool LeftClickUp => Event.current is { type: EventType.MouseUp, button: 0 };

  public static bool RightClickUp => Event.current is { type: EventType.MouseUp, button: 1 };

  public static bool IsShiftPressed => Input.GetKey(KeyCode.LeftShift);

  public static bool IsControlPressed => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand);
}