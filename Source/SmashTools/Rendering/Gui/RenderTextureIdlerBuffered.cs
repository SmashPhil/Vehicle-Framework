using System;
using CoreLib.Performance;
using JetBrains.Annotations;
using UnityEngine;

namespace SmashTools.Rendering;

/// <summary>
/// Wrapper class for binding the lifetime of a <see cref="RenderTextureBuffered"/> to a timer.
/// <para/>
/// Each time the render texture is read from, the timer will reset to 0. If the timer reaches the expiry
/// threshold — meaning the resources acquired haven't been accessed for that amount of time — all resources will be
/// freed and its timer will stop.
/// </summary>
[PublicAPI]
public sealed class RenderTextureIdlerBuffered : IDisposable
{
  private readonly RenderTextureBuffered buffer;

  private readonly float expiryTime;
  private float timeSinceRead;

  private RenderTextureIdlerBuffered(float expiryTime)
  {
    this.expiryTime = expiryTime;
    UnityThread.StartUpdate(Update);
  }

  /// <param name="buffer">RenderTextureBuffer used in this wrapper. Will be freed when timer expires.</param>
  /// <param name="expiryTime">Time till resources contained in this wrapper class are destroyed. Time will reset
  /// every time a resource is read.</param>
  public RenderTextureIdlerBuffered(RenderTextureBuffered buffer, float expiryTime) : this(expiryTime)
  {
    this.buffer = buffer;
  }

  /// <param name="rtA">RenderTexture used in this wrapper. Will be freed when timer expires.</param>
  /// <param name="rtB">Other RenderTexture used in this wrapper. Will be freed when timer expires.</param>
  /// <param name="expiryTime">Time till resources contained in this wrapper class are destroyed. Time will reset
  /// every time a resource is read.</param>
  public RenderTextureIdlerBuffered(RenderTexture rtA, RenderTexture rtB, float expiryTime) : this(
    new RenderTextureBuffered(rtA, rtB), expiryTime)
  {
  }

  /// <summary>
  /// TestFixture hook for OnUpdate function reference.
  /// </summary>
  internal UnityThread.OnUpdate UpdateLoop => Update;

  public bool Disposed => !buffer;

  public RenderTexture Read
  {
    get
    {
      timeSinceRead = 0;
      return buffer.Read;
    }
  }

  public RenderTexture Write
  {
    get
    {
      timeSinceRead = 0;
      return buffer.Write;
    }
  }

  public RenderTexture GetWrite()
  {
    timeSinceRead = 0;
    return buffer.GetWrite();
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
    buffer?.Dispose();
  }
}