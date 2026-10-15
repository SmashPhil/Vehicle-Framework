using System;
using System.Collections.Generic;
using System.Text;
using RimWorldWaylith.Imgui;

namespace SmashTools.Rendering;

internal static class Host
{
  extension(ImGuiHost host)
  {
    public Frame BeginFrameScope(int frameNumber, float deltaTime)
    {
      return new Frame(host, frameNumber, deltaTime);
    }
  }

  internal readonly struct Frame : IDisposable
  {
    private readonly ImGuiHost host;

    public Frame(ImGuiHost host, int frameNumber, float deltaTime)
    {
      this.host = host;
      Snapshot = host.BeginFrame(frameNumber, deltaTime);
    }

    public ImGuiDrawSnapshot Snapshot { get; }

    public void Dispose()
    {
      host.EndFrame(Snapshot);
    }
  }
}
