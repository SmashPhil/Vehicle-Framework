using System;
using System.Collections;
using CoreLib.Performance;
using DevTools.Testing;
using SmashTools.Rendering;
using UnityEngine;
using UnityEngine.Assertions;

namespace SmashTools.Testing;

[TestFixture(TestType.MainMenu)]
[TestDescription("Render texture utils.")]
internal sealed class Test_RenderTexture
{
  [Test, ExecutionPriority(Priority.First)]
  private void CreateFormatted()
  {
    RenderTexture renderTexture = RenderTextureUtil.CreateRenderTexture(2, 2);

    Expect.IsTrue(renderTexture.IsCreated());
    Expect.AreEqual(expected: 0, renderTexture.depth);
    Expect.IsTrue(SystemInfo.SupportsRenderTextureFormat(renderTexture.format));
    Expect.Throws<ArgumentException>(delegate { _ = RenderTextureUtil.CreateRenderTexture(0, 0); });
    renderTexture.ReleaseAndDestroy();
  }

  [Test]
  private IEnumerator DoubleBuffer()
  {
    RenderTexture rtA = RenderTextureUtil.CreateRenderTexture(2, 2);
    RenderTexture rtB = RenderTextureUtil.CreateRenderTexture(2, 2);
    Assert.IsNotNull(rtA);
    Assert.IsNotNull(rtB);
    RenderTextureBuffered buffer = new(rtA, rtB);

    Assert.IsTrue(rtA.IsCreated());
    Assert.IsTrue(rtB.IsCreated());

    Expect.ReferencesAreEqual(rtA, buffer.Read);
    Expect.ReferencesAreEqual(rtA, buffer.Read);
    Expect.ReferencesAreEqual(rtB, buffer.GetWrite());
    Expect.ReferencesAreEqual(rtA, buffer.Write);
    Expect.ReferencesAreEqual(rtB, buffer.Read);
    Expect.ReferencesAreNotEqual(buffer.Read, buffer.Write);

    // Cause a swap by fetching writable texture
    _ = buffer.GetWrite();

    Expect.ReferencesAreEqual(rtA, buffer.Read);
    Expect.ReferencesAreEqual(rtB, buffer.Write);

    // Dispose will queue the texture object for destruction, but we still have 1 frame to verify
    // GPU allocations were released.
    buffer.Dispose();

    Expect.AreEqual(expected: IntPtr.Zero, buffer.Read.GetNativeDepthBufferPtr());
    Expect.AreEqual(expected: IntPtr.Zero, buffer.Write.GetNativeDepthBufferPtr());

    // Allow RenderTextures to be destroyed, then verify
    yield return new WaitForEndOfFrame();

    Expect.IsFalse(rtA);
    Expect.IsFalse(rtB);
  }

  [Test]
  private IEnumerator Idler()
  {
    const float ExpiryTime = 999; // seconds

    RenderTexture renderTex = RenderTextureUtil.CreateRenderTexture(2, 2);
    Assert.IsNotNull(renderTex);
    Assert.IsTrue(renderTex.IsCreated());
    RenderTextureIdler idler = new(renderTex, ExpiryTime);
    Assert.AreEqual(renderTex, idler.RenderTex);

    Expect.IsTrue(UnityThread.InUpdateQueue(idler.UpdateLoop));
    
    idler.SetTimeDirect(100);
    bool continue100 = idler.UpdateLoop();
    Expect.IsTrue(continue100);

    idler.SetTimeDirect(9999);
    bool continue9999 = idler.UpdateLoop();
    Expect.IsFalse(continue9999);
    UnityThread.RemoveUpdate(idler.UpdateLoop);

    Expect.AreEqual(expected: IntPtr.Zero, renderTex.GetNativeDepthBufferPtr());

    // Allow RenderTextures to be destroyed, then verify
    yield return new WaitForEndOfFrame();

    Expect.IsFalse(renderTex);
  }

  [Test]
  private IEnumerator IdlerBuffered()
  {
    const float ExpiryTime = 999; // seconds

    RenderTexture rtA = RenderTextureUtil.CreateRenderTexture(2, 2);
    RenderTexture rtB = RenderTextureUtil.CreateRenderTexture(2, 2);
    Assert.IsNotNull(rtA);
    Assert.IsNotNull(rtB);
    Assert.IsTrue(rtA.IsCreated());
    Assert.IsTrue(rtB.IsCreated());
    RenderTextureIdlerBuffered idler = new(rtA, rtB, ExpiryTime);

    Expect.IsTrue(UnityThread.InUpdateQueue(idler.UpdateLoop));
    Expect.ReferencesAreNotEqual(idler.Read, idler.GetWrite());

    idler.SetTimeDirect(100);
    bool continue100 = idler.UpdateLoop();
    Expect.IsTrue(continue100);
    Expect.IsTrue(idler.Read.IsCreated());
    Expect.IsTrue(idler.Write.IsCreated());

    idler.SetTimeDirect(9999);
    bool continue9999 = idler.UpdateLoop();
    Expect.IsFalse(continue9999);
    UnityThread.RemoveUpdate(idler.UpdateLoop);

    Expect.AreEqual(expected: IntPtr.Zero, idler.Read.GetNativeDepthBufferPtr());
    Expect.AreEqual(expected: IntPtr.Zero, idler.Write.GetNativeDepthBufferPtr());

    // Allow RenderTextures to be destroyed, then verify
    yield return new WaitForEndOfFrame();

    Expect.IsFalse(rtA);
    Expect.IsFalse(rtB);
  }
}