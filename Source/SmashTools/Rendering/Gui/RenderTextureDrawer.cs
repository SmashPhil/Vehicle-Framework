using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Rendering;

[StaticConstructorOnStartup]
public static class RenderTextureDrawer
{
  private static readonly List<RenderData> RenderDatas = [];

  private static RenderTexture renderTexture;
  private static RenderTexture prevRenderTexture;

  public static bool InUse => renderTexture != null;

  public static void Add(RenderData renderData)
  {
    RenderDatas.Add(renderData);
  }

  public static void Init([NotNull] RenderTexture texture)
  {
    if (texture == null)
      throw new ArgumentNullException(nameof(texture));

    Assert.IsFalse(InUse);
    Assert.IsTrue(RenderDatas.Count == 0);
    renderTexture = texture;
    RenderDatas.Clear();
  }

  public static void Clear()
  {
    RenderDatas.Clear();
    renderTexture = null;
  }

  /// <summary>
  /// Finalize RenderTexture rendering with added render data.
  /// </summary>
  /// <param name="rect">Outer rect containing all the graphics being drawn.</param>
  /// <param name="scale">Zoom factor on all drawn graphics, scaled from the center of the rect.</param>
  /// <param name="center">Set rect position of all render data to center of outer rect. Use for 'icon' images that need all offsets erased.</param>
  public static void Draw(Rect rect, float scale = 1, bool center = false)
  {
    // Waylith discards GL primitives outside of repaint.
    if (Event.current is not { type: EventType.Repaint })
      return;

    if (!renderTexture || !renderTexture.IsCreated())
    {
      Trace.Fail("Trying to blit with null render texture.");
      return;
    }
    RenderDatas.Sort();

    prevRenderTexture = RenderTexture.active;
    RenderTexture.active = renderTexture;
    try
    {
      GL.PushMatrix();
      GL.Viewport(new Rect(0, 0, renderTexture.width, renderTexture.height));
      GL.LoadPixelMatrix(0, renderTexture.width, renderTexture.height, 0);
      GL.Clear(true, true, Color.clear);

      foreach (RenderData renderData in RenderDatas)
      {
        DrawRenderData(rect, renderData, scale: scale, center: center);
      }
    }
    finally
    {
      GL.PopMatrix();
      GL.Flush();
      RenderDatas.Clear();
      RenderTexture.active = prevRenderTexture;
    }
    return;

    static void DrawRenderData(Rect rect, in RenderData renderData, float scale, bool center)
    {
      if (renderData.material == null || !renderData.material.SetPass(0))
      {
        string name = renderData.mainTex ? renderData.mainTex.name : "NULL";
        Log.ErrorOnce($"Failed to render {name} to portrait, material pass not set.",
          renderData.mainTex.GetHashCode());
        return;
      }

      GL.PushMatrix();
      GL.LoadIdentity();
      try
      {
        Rect input = center ? renderData.rect with { center = rect.center } : renderData.rect;
        Rect normalizedRect = NormalizeRect(input, rect);
        Vector3 size = normalizedRect.size * scale;
        Quaternion rotation = Quaternion.Euler(0f, 0f, renderData.angle);
        Matrix4x4 matrix = Matrix4x4.TRS(normalizedRect.center, rotation, size)
          * Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0f));
        GL.MultMatrix(matrix);

        GL.Begin(GL.QUADS);
        GL.Color(Color.white);

        float uLeft = renderData.flip ? 1 : 0;
        float uRight = renderData.flip ? 0 : 1;

        // Top-left
        GL.TexCoord2(uLeft, 1);
        GL.Vertex3(0, 0, 0);

        // Top-right
        GL.TexCoord2(uRight, 1);
        GL.Vertex3(1, 0, 0);

        // Bottom-right
        GL.TexCoord2(uRight, 0);
        GL.Vertex3(1, 1, 0);

        // Bottom-left
        GL.TexCoord2(uLeft, 0);
        GL.Vertex3(0, 1, 0);
      }
      finally
      {
        GL.End();
        GL.PopMatrix();
      }
      return;

      static Rect NormalizeRect(Rect input, Rect rect)
      {
        float scaleX = renderTexture.width / rect.width;
        float scaleY = renderTexture.height / rect.height;

        return new Rect(
          input.x * scaleX,
          input.y * scaleY,
          input.width * scaleX,
          input.height * scaleY
        );
      }
    }
  }
}