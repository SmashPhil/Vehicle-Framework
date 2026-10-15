using System;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreLib;

/// <summary>
/// Destroys the Object when this struct goes out of scope.
/// </summary>
[PublicAPI]
public readonly struct DestroyOnDispose(Object obj) : IDisposable
{
  void IDisposable.Dispose()
  {
    if (obj is RenderTexture renderTex && renderTex.IsCreated())
    {
      renderTex.Release();
    }
    Object.Destroy(obj);
  }
}