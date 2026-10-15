using Brutal.Numerics;
using JetBrains.Annotations;

namespace SmashTools.Rendering;


[PublicAPI]
public struct WindowContext()
{
  public required string title;

  public int2? size;

  // Widgets::WindowBGFillColor
  public float4 color = new(0.082f, 0.098f, 0.114f, 1f);

  public bool drawTitleBar = true;
}