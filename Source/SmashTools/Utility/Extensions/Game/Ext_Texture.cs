using System;
using System.Collections.Generic;
using CoreLib;
using CoreLib.Performance;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Object = UnityEngine.Object;

namespace SmashTools;

public static class Ext_Texture
{
  /// <summary>
  /// Swaps texture in ContentFinder cache so there are no duplicates after changes are applied.
  /// </summary>
  public static bool TryReplaceInContentFinder<T>(string itemPath, T item) where T : Object
  {
    Assert.IsTrue(UnityThread.IsInMainThread);
    List<ModContentPack> runningModsListForReading = LoadedModManager.RunningModsListForReading;
    for (int i = runningModsListForReading.Count - 1; i >= 0; i--)
    {
      ModContentPack modContentPack = runningModsListForReading[i];
      T itemToDestroy = modContentPack.GetContentHolder<T>().Get(itemPath);
      if (itemToDestroy != null && itemToDestroy != item)
      {
        Object.Destroy(itemToDestroy);
        InjectIntoContentFinder(modContentPack, itemPath, item);
        return true;
      }
    }
    return false;
  }

  private static void CompressAndApply(Texture2D source, Texture2D newTexture)
  {
    // Reapply compression, we had to use Alpha8 since DTX5 is not a valid TextureFormat for ReadPixels
    if (Prefs.TextureCompression && newTexture.width % 4 == 0 &&
      newTexture.height % 4 == 0)
    {
      // This ends up causing severe darkening of patterns in VehicleFramework. Going to just optimize
      // patterns to not duplicate textures rather than go down this rabbit hole.
      //newTexture.Compress(false);
    }
    newTexture.filterMode = source.filterMode;
    newTexture.anisoLevel = source.anisoLevel;
    newTexture.Apply(true, true);
  }

  private static void InjectIntoContentFinder<T>(ModContentPack modContentPack, string itemPath,
    T item) where T : class
  {
    modContentPack.GetContentHolder<T>().contentList[itemPath] = item;
  }

  public static Texture2D CreateReadableTexture(Texture2D source,
    TextureWrapMode? wrapMode = null)
  {
    Assert.IsTrue(UnityThread.IsInMainThread);
    using var rts = TemporaryRenderTexture.Get(source, out RenderTexture renderTex);
    Texture2D readableTexture =
      new(source.width, source.height, TextureFormat.RGBA32, source.mipmapCount > 1)
      {
        name = source.name,
        wrapMode = wrapMode ?? source.wrapMode,
      };
    readableTexture.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
    readableTexture.Apply();
    return readableTexture;
  }

  /// <summary>
  /// Change <seealso cref="TextureWrapMode"/> of <paramref name="source"/> to <paramref name="wrapMode"/>
  /// </summary>
  /// <param name="source"></param>
  /// <param name="wrapMode"></param>
  /// <returns></returns>
  public static Texture2D WrapTexture(Texture2D source, TextureWrapMode wrapMode)
  {
    Assert.IsTrue(UnityThread.IsInMainThread);
    if (source.isReadable)
    {
      source.wrapMode = wrapMode;
      return source;
    }
    using var rts = TemporaryRenderTexture.Get(source, out RenderTexture renderTex);
    Texture2D wrappedTex = CreateReadableTexture(source, wrapMode);
    wrappedTex.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
    CompressAndApply(source, wrappedTex);
    return wrappedTex;
  }

  /// <summary>
  /// Rotate pixels of <paramref name="source"/> by <paramref name="angle"/> (Counter-Clockwise)
  /// </summary>
  /// <param name="source"></param>
  /// <param name="angle"></param>
  public static Texture2D Rotate(this Texture2D source, float angle)
  {
    Assert.IsTrue(UnityThread.IsInMainThread);
    if (!Mathf.Approximately(angle, 90) && !Mathf.Approximately(angle, 180) &&
      !Mathf.Approximately(angle, 270))
    {
      Log.Error(
        $"Unable to rotate {source.name} by angle=\"{angle}\". Angle must equal 90, 180, or 270.");
      return source;
    }
    if (source.width != source.height)
    {
      Log.Warning(
        $"Rotating patterns with non-square dimensions may result in inaccurate conversions. " +
        $"Tex=\"{source.name}\" ({source.width},{source.height})");
    }
    Texture2D readableTex = source.isReadable ? source : CreateReadableTexture(source);
    using DestroyOnDispose dt = new(readableTex);
    int width = source.width;
    int height = source.height;
    Texture2D rotImage = new(width, height, TextureFormat.RGBA32, source.mipmapCount > 1)
    {
      name = source.name
    };
    Color32[] pix2 = readableTex.GetPixels32();
    Color32[] pix3 = RotateSquare(pix2, angle, readableTex);
    rotImage.SetPixels32(pix3);
    CompressAndApply(source, rotImage);
    return rotImage;
  }

  private static Color32[] RotateSquare(Color32[] arr, float angle, Texture2D originTexture)
  {
    int w = originTexture.width;
    int h = originTexture.height;
    Color32[] arr2 = new Color32[arr.Length];
    int normalizedAngle = Mathf.RoundToInt(angle) % 360;
    if (normalizedAngle < 0)
    {
      normalizedAngle += 360;
    }

    for (int j = 0; j < h; j++)
    {
      for (int i = 0; i < w; i++)
      {
        int x;
        int y;
        switch (normalizedAngle)
        {
          case 90:
            x = j;
            y = h - 1 - i;
            break;
          case 180:
            x = w - 1 - i;
            y = h - 1 - j;
            break;
          case 270:
            x = w - 1 - j;
            y = i;
            break;
          default:
            x = i;
            y = j;
            break;
        }
        arr2[j * w + i] = arr[y * w + x];
      }
    }
    return arr2;
  }

  private readonly struct TemporaryRenderTexture : IDisposable
  {
    private readonly RenderTexture renderTex;
    private readonly RenderTexture previous;

    private TemporaryRenderTexture(RenderTexture renderTex)
    {
      this.renderTex = renderTex;
      previous = RenderTexture.active;
      RenderTexture.active = renderTex;
    }

    public static TemporaryRenderTexture Get(Texture2D source, out RenderTexture renderTex)
    {
      renderTex = RenderTexture.GetTemporary(
        source.width,
        source.height,
        0,
        RenderTextureFormat.Default,
        RenderTextureReadWrite.Linear);
      Graphics.Blit(source, renderTex);
      return new TemporaryRenderTexture(renderTex);
    }

    void IDisposable.Dispose()
    {
      RenderTexture.active = previous;
      RenderTexture.ReleaseTemporary(renderTex);
    }
  }
}