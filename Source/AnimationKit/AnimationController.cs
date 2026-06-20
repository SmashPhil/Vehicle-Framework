using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace AnimationKit;

public sealed class AnimationController : IDisposable
{
  private readonly List<AnimationParameter> parameters = [];
  private readonly List<AnimationLayer> layers = [];

  public AnimationController()
  {
    UnsafePtr = CreateController();
  }

  ~AnimationController()
  {
    Dispose(disposing: false);
  }

  public bool Disposed => UnsafePtr != IntPtr.Zero;

  private IntPtr UnsafePtr { get; set; }

  public AnimationLayer AddLayer(string name)
  {
    var layer = new AnimationLayer(UnsafePtr, name);
    layers.Add(layer);
    return layer;
  }

  public bool DeleteLayer(string name)
  {
    for (int i = 0; i < layers.Count; i++)
    {
      AnimationLayer layer = layers[i];
      if (layer.Name == name)
      {
        layers.RemoveAt(i);
        DeleteLayer(UnsafePtr, layer.UnsafePtr);
        layer.Dispose();
        return true;
      }
    }
    return false;
  }

  public bool DeleteLayer(AnimationLayer layer)
  {
    if (!layers.Remove(layer))
      return false;

    DeleteLayer(UnsafePtr, layer.UnsafePtr);
    layer.Dispose();
    return true;
  }

  private void Dispose(bool disposing)
  {
    if (Disposed)
      return;

    if (disposing)
    {
      foreach (AnimationLayer layer in layers)
      {
        layer.Dispose();
      }
      layers.Clear();
    }

    DeleteController(UnsafePtr);
    UnsafePtr = IntPtr.Zero;
  }

  public void Dispose()
  {
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }

  [MustUseReturnValue]
  [DllImport("animation_kit", CallingConvention = CallingConvention.Cdecl)]
  private static extern IntPtr CreateController();

  [DllImport("animation_kit", CallingConvention = CallingConvention.Cdecl)]
  private static extern void DeleteController(IntPtr controllerPtr);

  [DllImport("animation_kit", CallingConvention = CallingConvention.Cdecl)]
  private static extern void DeleteLayer(IntPtr controllerPtr, IntPtr layerPtr);

  [DllImport("animation_kit", CallingConvention = CallingConvention.Cdecl)]
  private static extern void DeleteLayerByName(IntPtr controllerPtr, string name);
}
