using System;
using JetBrains.Annotations;
using SmashTools;
using SmashTools.Rendering;
using UnityEngine;
using Verse;
using static Vehicles.Config.FeatureFlags;

namespace Vehicles.Rendering;

[PublicAPI]
public sealed class VehiclePortrait : IDisposable
{
  private RenderTexture renderTex;
  private RenderTextureIdler idler;

  private Config config;

  public struct Config()
  {
    public float iconScale = 1;
    public float expiryTime = -1;
    public bool forceCentering = false;
  }

  public VehiclePortrait()
  {
    config = new Config();
  }

  public VehiclePortrait(in Config config)
  {
    this.config = config;
  }

  private RenderTexture RenderTexture => idler != null ? idler.RenderTex : renderTex;

  public bool RedrawPortrait
  {
    get
    {
      return field || RenderTexture == null;
    }
    private set;
  } = true;

  public void MarkDirty()
  {
    RedrawPortrait = true;
  }

  public void Dispose()
  {
    renderTex?.ReleaseAndDestroy();
    renderTex = null;
    idler?.Dispose();
    idler = null;
  }

  private void CreateRenderTexture(Rect rect, ref readonly BlitRequest request)
  {
    if (RenderTexture != null)
      return;

    if (config.expiryTime > 0)
    {
      idler = new RenderTextureIdler(VehicleGui.CreateRenderTexture(rect, request), config.expiryTime);
    }
    else
    {
      renderTex = VehicleGui.CreateRenderTexture(rect, request);
    }
  }

  /// <summary>
  /// Draw vehicle portrait
  /// </summary>
  /// <param name="rect">Rect to draw the vehicle portrait inside. Contents will be clipped to the rect.</param>
  /// <param name="request">BlitRequest if render texture needs to be redrawn.</param>
  public void Draw(Rect rect, in BlitRequest request)
  {
    if (Event.current.type != EventType.Repaint)
      return;

    using WidgetGroupScope group = new(rect);
    Rect vehicleRect = rect.AtZero();
    if (IsFeatureEnabled(BlitTexturePortraits))
    {
      if (RedrawPortrait)
      {
        CreateRenderTexture(vehicleRect, in request);
        VehicleGui.Blit(RenderTexture, vehicleRect, request, iconScale: config.iconScale, config.forceCentering);
        RedrawPortrait = false;
      }
      GUI.DrawTexture(vehicleRect, RenderTexture);
    }
    else
    {
      VehicleGui.DrawVehicleOnGUI(vehicleRect, request);
    }
  }
}
