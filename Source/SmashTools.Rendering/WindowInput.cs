using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using Verse;
using Waylith.Windowing;
using Mouse = UnityEngine.InputSystem.Mouse;

namespace SmashTools.Windowing;

internal sealed class WindowInput : IDisposable
{
  private const int InputSystemButtonCount = 5;
  private const int TrackedMouseButtonCount = 8;

  private readonly IWindow source;
  private readonly Queue<Event> events = new();
  private readonly bool[] held = new bool[TrackedMouseButtonCount];
  private readonly bool[] renderedHeld = new bool[TrackedMouseButtonCount];
  private readonly int[] clicks = new int[TrackedMouseButtonCount];

  private Vector2 mouse;
  private bool inside;

  public WindowInput(IWindow source)
  {
    this.source = source;
    mouse = new Vector2(-10000, -10000);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static Vector2 ToVector(LogicalPosition position)
  {
    return new Vector2((float)position.X, (float)position.Y);
  }

  private Vector2 ToPixels(Vector2 position)
  {
    LogicalSize size = source.Size;
    PhysicalSize pixels = source.SurfaceSize;
    return new Vector2(
      (float)(position.x * pixels.Width / Math.Max(1d, size.Width)),
      (float)(position.y * pixels.Height / Math.Max(1d, size.Height)));
  }

  private static EventModifiers ToEventModifiers(Modifiers modifiers)
  {
    EventModifiers result = EventModifiers.None;
    if ((modifiers & Modifiers.Shift) != Modifiers.None)
    {
      result |= EventModifiers.Shift;
    }
    if ((modifiers & Modifiers.Control) != Modifiers.None)
    {
      result |= EventModifiers.Control;
    }
    if ((modifiers & Modifiers.Alt) != Modifiers.None)
    {
      result |= EventModifiers.Alt;
    }
    if ((modifiers & Modifiers.Super) != Modifiers.None)
    {
      result |= EventModifiers.Command;
    }
    return result;
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
    return NewEvent(type, position ?? (inside ? ToPixels(mouse) : new Vector2(-10000, -10000)),
      ToEventModifiers(source.Modifiers));
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
          3 => mouse.backButton,
          _ => mouse.forwardButton
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

  internal void OnPointerEntered(in PointerEvent e)
  {
    if (e.Kind != PointerKind.Mouse)
    {
      return;
    }

    mouse = ToVector(e.Position.Logical);
    inside = true;
  }

  internal void OnPointerExited(in PointerEvent e)
  {
    if (e.Kind != PointerKind.Mouse)
    {
      return;
    }

    mouse = ToVector(e.Position.Logical);
    inside = false;
  }

  internal void OnPointerMoved(in PointerEvent e)
  {
    if (e.Kind != PointerKind.Mouse)
    {
      return;
    }

    Vector2 previous = mouse;
    mouse = ToVector(e.Position.Logical);
    inside = Contains(mouse);

    int button = Array.FindIndex(held, down => down);
    Event value = NewEvent(button >= 0 ? EventType.MouseDrag : EventType.MouseMove,
      mouse, ToEventModifiers(e.Modifiers));
    value.button = Math.Max(0, button);
    value.delta = mouse - previous;
    events.Enqueue(value);
  }

  private bool Contains(Vector2 position)
  {
    return position is { x: >= 0, y: >= 0 } &&
      position.x < source.Size.Width && position.y < source.Size.Height;
  }

  internal void OnPointerButton(in PointerEvent e, bool pressed)
  {
    PointerButton? button = e.Button;
    if (e.Kind != PointerKind.Mouse || !button.HasValue)
    {
      return;
    }

    int index = (int)button.Value;
    if ((uint)index >= held.Length)
    {
      return;
    }

    mouse = ToVector(e.Position.Logical);
    inside = Contains(mouse);

    if (!pressed && !held[index])
    {
      return;
    }

    held[index] = pressed;
    if (pressed)
    {
      clicks[index] = Math.Max(1, e.ClickCount);
    }

    Event value = NewEvent(pressed ? EventType.MouseDown : EventType.MouseUp,
      mouse, ToEventModifiers(e.Modifiers));
    value.button = index;
    value.clickCount = clicks[index];
    events.Enqueue(value);
  }

  internal void OnScrolled(in ScrollEvent e)
  {
    mouse = ToVector(e.Position.Logical);

    Event value = NewEvent(EventType.ScrollWheel, mouse,
      ToEventModifiers(e.Modifiers));
    float scale = e.Delta.Unit == ScrollUnit.Pixels ? -0.3f : -3f;
    value.delta = new Vector2((float)e.Delta.X * scale,
      (float)e.Delta.Y * scale);
    events.Enqueue(value);
  }

  internal void OnFocusChanged(bool focused)
  {
    if (!focused)
    {
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
  }

  public void Dispose()
  {
    events.Clear();
  }
}
