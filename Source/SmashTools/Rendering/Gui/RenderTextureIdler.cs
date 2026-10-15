using System;
using CoreLib.Performance;
using JetBrains.Annotations;
using UnityEngine;

namespace SmashTools.Rendering;

/// <summary>
/// Wrapper class for binding the lifetime of a <see cref="RenderTexture"/> to a timer.
/// <para/>
/// Each time the render texture is read from, the timer will reset to 0. If the timer reaches the expiry
/// threshold — meaning the resources acquired haven't been accessed for that amount of time — all resources will be
/// freed and its timer will stop.
/// </summary>
[PublicAPI]
public sealed class RenderTextureIdler : IDisposable
{
  private readonly RenderTexture renderTex;

  private readonly float expiryTime;
  private float timeSinceRead;

  private RenderTextureIdler(float expiryTime)
  {
    this.expiryTime = expiryTime;
    UnityThread.StartUpdate(Update);
  }

  /// <param name="renderTex">RenderTexture used in this wrapper. Will be freed when timer expires.</param>
  /// <param name="expiryTime">Time till resources contained in this wrapper class are destroyed. Time will reset
  /// every time a resource is read.</param>
  public RenderTextureIdler(RenderTexture renderTex, float expiryTime) : this(expiryTime)
  {
    this.renderTex = renderTex;
  }

  /// <summary>
  /// TestFixture hook for OnUpdate function reference.
  /// </summary>
  internal UnityThread.OnUpdate UpdateLoop => Update;

  public bool Disposed => !renderTex;

  public RenderTexture RenderTex
  {
    get
    {
      timeSinceRead = 0;
      return renderTex;
    }
  }

  internal void SetTimeDirect(float time)
  {
    timeSinceRead = time;
  }

  private bool Update()
  {
    timeSinceRead += Time.deltaTime;

    if (timeSinceRead < expiryTime)
      return true;

    Dispose();
    return false; // Dequeues from Update loop
  }

  public void Dispose()
  {
    renderTex?.ReleaseAndDestroy();
  }
}