using System;
using RimWorldWaylith.Imgui;
using UnityEngine;

namespace SmashTools.Rendering;

internal static class Backend
{
  extension (ImGuiGuiBackend backend)
  {
    public Pass BeginPassScope(bool drawing, Vector2 mousePos)
    {
      return new Pass(backend, drawing, mousePos);
    }

    public WindowContent BeginWindowContentScope(Rect windowRect)
    {
      return new WindowContent(backend, windowRect);
    }

    public Frame BeginFrameScope()
    {
      return new Frame(backend);
    }
  }

  internal readonly struct Pass : IDisposable
  {
    private readonly ImGuiGuiBackend backend;

    public Pass(ImGuiGuiBackend backend, bool drawing, Vector2 mousePos)
    {
      this.backend = backend;
      backend.BeginPass(drawing, mousePos);
    }

    public void Dispose()
    {
      backend.EndPass();
    }
  }

  internal readonly struct Frame : IDisposable
  {
    private readonly ImGuiGuiBackend backend;

    public Frame(ImGuiGuiBackend backend)
    {
      this.backend = backend;
      backend.BeginFrame();
    }

    public void Dispose()
    {
      backend.EndFrame();
    }
  }

  internal readonly struct WindowContent : IDisposable
  {
    private readonly ImGuiGuiBackend backend;

    public WindowContent(ImGuiGuiBackend backend, Rect windowRect)
    {
      this.backend = backend;
      backend.BeginWindowContent(windowRect);
    }

    public void Dispose()
    {
      backend.EndWindowContent();
    }
  }
}
