using System;
using JetBrains.Annotations;
using UnityEngine;

namespace SmashTools.Rendering;

/// <summary>
/// Double buffer implementation for seemless read / write commands
/// </summary>
[PublicAPI]
public sealed class RenderTextureBuffered : IDisposable
{
  private RenderTexture rtA;
  private RenderTexture rtB;

  public RenderTextureBuffered(RenderTexture rtA, RenderTexture rtB)
  {
    this.rtA = rtA;
    this.rtB = rtB;
    Read = rtA;
  }

  /// <summary>
  /// Get current read target without swapping
  /// </summary>
  public RenderTexture Read { get; private set; }

  public RenderTexture Write => Read == rtA ? rtB : rtA;

  /// <summary>
  /// Get current write target and swap
  /// </summary>
  public RenderTexture GetWrite()
  {
    Read = Write;
    return Read;
  }

  public void Dispose()
  {
    rtA.ReleaseAndDestroy();
    rtB.ReleaseAndDestroy();
  }

  /// <summary>
  /// Implicit boolean cast keeping in line with Unity's implicit boolean -&gt; null check.
  /// </summary>
  /// <param name="buffer"></param>
  /// <returns>True if either render texture is not destroyed, false if both are destroyed.</returns>
  public static implicit operator bool(RenderTextureBuffered buffer)
  {
    if (buffer == null)
      return false;
    return buffer.rtA || buffer.rtB;
  }
}