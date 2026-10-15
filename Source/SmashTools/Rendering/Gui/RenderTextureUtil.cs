using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;

namespace SmashTools.Rendering;

[PublicAPI]
public static class RenderTextureUtil
{
  /// <summary>
  /// Releases gpu-side memory and destroys the render texture
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void ReleaseAndDestroy(this RenderTexture renderTex)
  {
    if (!renderTex)
      return;

    if (renderTex.IsCreated())
    {
      renderTex.Release();
    }
    UnityEngine.Object.Destroy(renderTex);
  }

  public static RenderTexture CreateRenderTexture(int width, int height)
  {
    // Unity just logs an error if you try to do this, but for clarity w/ RimWorld log window and
    // unit testing, we throw instead.
    if (width <= 0 || height <= 0)
      throw new ArgumentException("RenderTexture size must have dimensions greater than 0.");

    RenderTexture rt = new(width, height, 0, RenderTextureFormat.ARGBFloat.OrNextSupportedFormat())
    {
      filterMode = FilterMode.Point,
      wrapMode = TextureWrapMode.Clamp
    };
    rt.Create();
    return rt;
  }

  public static RenderTextureFormat OrNextSupportedFormat(
    this RenderTextureFormat renderTextureFormat)
  {
    if (SystemInfo.SupportsRenderTextureFormat(renderTextureFormat))
      return renderTextureFormat;

    return renderTextureFormat switch
    {
      RenderTextureFormat.R8
        when SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RG16)
        => RenderTextureFormat.RG16,
      RenderTextureFormat.R8 or RenderTextureFormat.RG16
        when SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32)
        => RenderTextureFormat.ARGB32,
      RenderTextureFormat.R8 or RenderTextureFormat.RHalf or RenderTextureFormat.RFloat
        when SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGFloat)
        => RenderTextureFormat.RGFloat,
      RenderTextureFormat.R8 or RenderTextureFormat.RHalf or RenderTextureFormat.RFloat
        or RenderTextureFormat.RGFloat
        when SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat)
        => RenderTextureFormat.ARGBFloat,
      _ => renderTextureFormat
    };
  }
}