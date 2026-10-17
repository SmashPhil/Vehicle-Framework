using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Brutal.GlfwApi;
using Brutal.Numerics;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using Verse;
using Mouse = UnityEngine.InputSystem.Mouse;

namespace SmashTools.Rendering;

internal sealed class WindowInput : IDisposable
{
  private const int InputSystemButtonCount = 5;
  private const int TrackedMouseButtonCount = 8;

  private readonly GlfwWindow source;
  private readonly Queue<Event> events = new();
  private readonly bool[] held = new bool[TrackedMouseButtonCount];
  private readonly bool[] renderedHeld = new bool[TrackedMouseButtonCount];
  private readonly int[] clicks = new int[TrackedMouseButtonCount];
  private readonly double[] lastClickTime = new double[TrackedMouseButtonCount];
  private readonly Vector2[] lastClickPosition = new Vector2[TrackedMouseButtonCount];

  private Vector2 mouse;
  private bool inside;

  public WindowInput(GlfwWindow source)
  {
    this.source = source;
    mouse = DoubleToVector(source.CursorPos);
    inside = mouse is { x: >= 0, y: >= 0 } && mouse.x < source.Size.X && mouse.y < source.Size.Y;

    source.OnMouseButton += OnMouseButton;
    source.OnCursorPos += OnCursorPos;
    source.OnCursorEnter += OnCursorEnter;
    source.OnScroll += OnScroll;
    source.OnFocus += OnFocus;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static Vector2 DoubleToVector(double2 value)
  {
    return new Vector2((float)value.X, (float)value.Y);
  }

  private Vector2 ToPixels(Vector2 position)
  {
    int2 size = source.Size;
    int2 pixels = source.FramebufferSize;
    return new Vector2(position.x * pixels.X / Math.Max(1, size.X),
      position.y * pixels.Y / Math.Max(1, size.Y));
  }

  private EventModifiers Modifiers()
  {
    EventModifiers result = EventModifiers.None;
    if (KeyDown(GlfwKey.LeftShift) || KeyDown(GlfwKey.RightShift))
    {
      result |= EventModifiers.Shift;
    }
    if (KeyDown(GlfwKey.LeftControl) || KeyDown(GlfwKey.RightControl))
    {
      result |= EventModifiers.Control;
    }
    if (KeyDown(GlfwKey.LeftAlt) || KeyDown(GlfwKey.RightAlt))
    {
      result |= EventModifiers.Alt;
    }
    if (KeyDown(GlfwKey.LeftSuper) || KeyDown(GlfwKey.RightSuper))
    {
      result |= EventModifiers.Command;
    }
    return result;
  }

  private bool KeyDown(GlfwKey key)
  {
    return source.GetKey(key) is GlfwKeyAction.Press or GlfwKeyAction.Repeat;
  }

  private bool IsHeld(int button)
  {
    return button >= 0 && button < renderedHeld.Length && renderedHeld[button];
  }

  private static Event NewEvent(EventType type, Vector2 position, EventModifiers modifiers)
  {
    return new Event
    {
      type = type,
      rawType = type,
      mousePosition = position,
      modifiers = modifiers,
      shift = (modifiers & EventModifiers.Shift) != 0,
      alt = (modifiers & EventModifiers.Alt) != 0
    };
  }

  public Event CreateEvent(EventType type, Vector2? position = null)
  {
    return NewEvent(type, position ?? (inside ? ToPixels(mouse) : new Vector2(-10000, -10000)), Modifiers());
  }

  public bool TryDequeue(out Event value)
  {
    if (!events.TryDequeue(out value))
      return false;

    value.mousePosition = ToPixels(value.mousePosition);
    if (value.type is EventType.MouseMove or EventType.MouseDrag)
    {
      value.delta = ToPixels(value.delta);
    }
    return true;
  }

  public void Advance(Event value)
  {
    // Unsupported button type
    if ((uint)value.button >= renderedHeld.Length)
      return;

    renderedHeld[value.button] = value.rawType switch
    {
      EventType.MouseDown => true,
      EventType.MouseUp => false,
      _ => renderedHeld[value.button]
    };
  }

  internal sealed class MouseScope : IDisposable
  {
    private readonly Mouse mouse = Mouse.current;
    private readonly ButtonControl[] buttons = new ButtonControl[InputSystemButtonCount];
    private readonly bool[] pressed = new bool[InputSystemButtonCount];
    private readonly bool[] wasPressed = new bool[InputSystemButtonCount];
    private readonly bool[] wasReleased = new bool[InputSystemButtonCount];
    private readonly bool[] wasRepeated = new bool[InputSystemButtonCount];
    private readonly Vector2 position;

    public MouseScope()
    {
      position = mouse.position.ReadValue();
      for (int i = 0; i < buttons.Length; i++)
      {
        ButtonControl button = i switch
        {
          0 => mouse.leftButton,
          1 => mouse.rightButton,
          2 => mouse.middleButton,
          3 => mouse.forwardButton,
          _ => mouse.backButton
        };
        buttons[i] = button;
        pressed[i] = button.isPressed;
        wasPressed[i] = button._wasPressedThisFrame;
        wasReleased[i] = button._wasReleasedThisFrame;
        wasRepeated[i] = button._wasRepeatedThisFrame;
      }
    }

    public void Apply(WindowInput input, Event guiEvent, Vector2 mousePos, int height)
    {
      for (int i = 0; i < buttons.Length; i++)
      {
        buttons[i].SetPressed(input.IsHeld(i));
        buttons[i]._wasPressedThisFrame = guiEvent.rawType == EventType.MouseDown && guiEvent.button == i;
        buttons[i]._wasReleasedThisFrame = guiEvent.rawType == EventType.MouseUp && guiEvent.button == i;
        buttons[i]._wasRepeatedThisFrame = false;
      }
      float scale = Prefs.UIScale;
      mouse.SetPosition(new Vector2(mousePos.x * scale, (height - mousePos.y) * scale));
    }

    public void Dispose()
    {
      for (int i = 0; i < buttons.Length; i++)
      {
        buttons[i].SetPressed(pressed[i]);
        buttons[i]._wasPressedThisFrame = wasPressed[i];
        buttons[i]._wasReleasedThisFrame = wasReleased[i];
        buttons[i]._wasRepeatedThisFrame = wasRepeated[i];
      }
      mouse.SetPosition(position);
    }
  }

  private void OnMouseButton(GlfwWindow sender, GlfwMouseButton button,
    GlfwButtonAction action, GlfwModifier modifiers)
  {
    int index = (int)button;
    if ((uint)index >= held.Length)
      return;

    mouse = DoubleToVector(sender.CursorPos);
    inside = Contains(mouse);
    bool pressed = action == GlfwButtonAction.Press;
    if (!pressed && !held[index])
      return;

    held[index] = pressed;
    if (pressed)
    {
      const float MultiClickInterval = 0.5f; // seconds
      const float MaxMultiClickDistance = 25;
      double now = (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency;
      clicks[index] = now - lastClickTime[index] <= MultiClickInterval &&
        (mouse - lastClickPosition[index]).sqrMagnitude <= MaxMultiClickDistance ?
        clicks[index] + 1 : 1;
      lastClickTime[index] = now;
      lastClickPosition[index] = mouse;
    }
    const GlfwModifier EventModifierMask = GlfwModifier.Shift | GlfwModifier.Control |
                                           GlfwModifier.Alt | GlfwModifier.Super;
    Event value = NewEvent(pressed ? EventType.MouseDown : EventType.MouseUp, mouse,
      (EventModifiers)(modifiers & EventModifierMask));
    value.button = index;
    value.clickCount = clicks[index];
    events.Enqueue(value);
  }

  private void OnCursorPos(GlfwWindow sender, double2 position)
  {
    Vector2 previous = mouse;
    mouse = DoubleToVector(position);
    inside = Contains(mouse);
    int button = Array.FindIndex(held, down => down);
    if (button == -1)
      return;

    Event value = NewEvent(button >= 0 ? EventType.MouseDrag : EventType.MouseMove, mouse, Modifiers());
    value.button = Math.Max(0, button);
    value.delta = mouse - previous;
    events.Enqueue(value);
  }

  private void OnCursorEnter(GlfwWindow sender, bool entered)
  {
    inside = entered;
    mouse = DoubleToVector(sender.CursorPos);
  }

  private bool Contains(Vector2 position)
  {
    return position is { x: >= 0, y: >= 0 } && position.x < source.Size.X && position.y < source.Size.Y;
  }

  private void OnScroll(GlfwWindow sender, double2 offset)
  {
    mouse = DoubleToVector(sender.CursorPos);
    Event value = NewEvent(EventType.ScrollWheel, mouse, Modifiers());
    value.delta = DoubleToVector(offset) * -3f;
    events.Enqueue(value);
  }

  private void OnFocus(GlfwWindow sender, GlfwFocusEvent focus)
  {
    if (focus != GlfwFocusEvent.LostFocus)
      return;

    events.Clear();
    for (int i = 0; i < held.Length; i++)
    {
      if (!held[i])
        continue;

      held[i] = false;
      Event value = NewEvent(EventType.MouseUp, new Vector2(-10000, -10000), EventModifiers.None);
      value.button = i;
      events.Enqueue(value);
    }
  }

  public void Dispose()
  {
    source.OnMouseButton -= OnMouseButton;
    source.OnCursorPos -= OnCursorPos;
    source.OnCursorEnter -= OnCursorEnter;
    source.OnScroll -= OnScroll;
    source.OnFocus -= OnFocus;
    events.Clear();
  }
}