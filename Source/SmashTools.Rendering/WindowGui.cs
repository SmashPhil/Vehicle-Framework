using System;
using Brutal.ImGuiApi;
using Brutal.VulkanApi;
using RimWorldWaylith.Imgui;
using UnityEngine;
using UnityStubs.Gui;
using Verse;
using Waylith;
using Waylith.Graphics;
using Waylith.Rendering;

namespace SmashTools.Rendering;

/// <summary>
/// Hosts Gui passes for <see cref="Window"/>'s contents into a glfw window
/// </summary>
internal sealed class WindowGui : IDisposable
{
  private readonly WindowStack windowStack;
  private readonly WindowRenderContext renderContext;

  private readonly ImGuiHost host;
  private readonly ImGuiGuiBackend backend;
  private readonly ITextMeshProBackend textBackend;
  private readonly ImGuiContextPtr imguiContext;

  private int frameNumber;
  private bool disposed;

  public WindowGui(EngineApplication application, WindowStack windowStack, WindowRenderContext renderContext)
  {
    this.windowStack = windowStack;
    this.renderContext = renderContext;
    EngineApplication appClone = (EngineApplication)application.MemberwiseClone();
    appClone.RenderContext = renderContext;

    // ImGuiHost sets current context in constructor
    using ContextScope ctx = new(imguiContext);
    host = new ImGuiHost(appClone, renderContext.FramesInFlight + 1);
    imguiContext = host.context;
    backend = new ImGuiGuiBackend(host, textureBaker: null);
    textBackend = host.TmpFontBackend;
  }

  public ImGuiDrawSnapshot Capture(WindowInput input)
  {
    using WindowInput.MouseScope mouseScope = new();
    using GuiGlobalsScope guiGlobals = new(backend, textBackend);
    using ContextScope ctx = new(imguiContext);

    VkExtent2D extent = renderContext.GetExtent();
    using WindowStackContext stackContext = new(windowStack, extent.Width, extent.Height);

    windowStack.WindowsUpdate();

    using GuiStateScope state = new();

    if (frameNumber == 0)
    {
      GUIUtility.hotControl = 0;
      GUIUtility.keyboardControl = 0;
    }

    using TextBlock textBlock = new(GameFont.Small, TextAnchor.UpperLeft, newWordWrap: true);

    using Host.Frame hostFrame = host.BeginFrameScope(frameNumber++, 1f / 60);
    using Backend.Frame backendFrame = backend.BeginFrameScope();
    // Glfw callbacks only enqueue. Dispatch on the next PrePollEvents after the previous poll has completed with
    // this window's backend and GUI state active.
    while (input.TryDequeue(out Event pending))
    {
      DrawPass(input, mouseScope, input.CreateEvent(EventType.Layout, pending.mousePosition));
      input.Advance(pending);
      DrawPass(input, mouseScope, pending);
    }
    DrawPass(input, mouseScope, input.CreateEvent(EventType.Layout));
    DrawPass(input, mouseScope, input.CreateEvent(EventType.Repaint));
    return hostFrame.Snapshot;
  }

  private void DrawPass(WindowInput input, WindowInput.MouseScope mouseScope, Event guiEvent)
  {
    // Setting GUI.matrix rewrites Event.current's mouse position from its previous pass.
    // Keep the current position until BeginPass.
    Vector2 currentMousePos = guiEvent.mousePosition;
    mouseScope.Apply(input, guiEvent, currentMousePos, UI.screenHeight);
    Event.current = guiEvent;
    GUI.matrix = Matrix4x4.identity;

    GUIUtility.ResetControlIds();
    using var pass = backend.BeginPassScope(guiEvent.type == EventType.Repaint, currentMousePos);

    GUI.DispatchInputToWindows();
    using BehaviourGui behaviourGui = new(0);

    windowStack.WindowStackOnGUI();
    GUI.ProcessPendingWindows();
  }

  public void Record(ImGuiDrawSnapshot snapshot, in FrameContext frame)
  {
    using ContextScope ctx = new(imguiContext);
    host.Render(snapshot, in frame);
  }

  public void Dispose()
  {
    if (disposed)
      return;

    disposed = true;
    // Renderer disposal uses GetPlatformIO, so select our context first.
    using ContextScope ctx = new(imguiContext);
    host.Dispose();
  }

  private readonly struct ContextScope : IDisposable
  {
    private readonly ImGuiContextPtr previous;

    public ContextScope(ImGuiContextPtr context)
    {
      previous = ImGui.GetCurrentContext();
      ImGui.SetCurrentContext(context);
    }

    public void Dispose()
    {
      ImGui.SetCurrentContext(previous);
    }
  }

  private readonly struct GuiStateScope : IDisposable
  {
    private readonly Color color;
    private readonly Color backgroundColor;
    private readonly Color contentColor;
    private readonly bool enabled;
    private readonly bool changed;

    public GuiStateScope()
    {
      color = GUI.color;
      backgroundColor = GUI.backgroundColor;
      contentColor = GUI.contentColor;
      enabled = GUI.enabled;
      changed = GUI.changed;

      GUI.color = Color.white;
      GUI.backgroundColor = Color.white;
      GUI.contentColor = Color.white;
      GUI.enabled = true;
      GUI.changed = false;
    }

    public void Dispose()
    {
      GUI.color = color;
      GUI.backgroundColor = backgroundColor;
      GUI.contentColor = contentColor;
      GUI.enabled = enabled;
      GUI.changed = changed;
    }
  }

  private readonly struct GuiGlobalsScope : IDisposable
  {
    private readonly IGuiBackend previousBackend;
    private readonly ITextMeshProBackend previousTextBackend;
    private readonly Event previousEvent;

    public GuiGlobalsScope(IGuiBackend backend, ITextMeshProBackend textBackend)
    {
      previousBackend = GuiBackend.Current;
      previousTextBackend = TextMeshProBackend.Current;
      previousEvent = Event.current;

      GuiBackend.Current = backend;
      TextMeshProBackend.Current = textBackend;
    }

    public void Dispose()
    {
      Event.current = previousEvent;
      GuiBackend.Current = previousBackend;
      TextMeshProBackend.Current = previousTextBackend;
    }
  }
}