using System;
using System.Collections;
using System.Collections.Generic;
using CoreLib.Performance;
using DevTools.Testing;
using SmashTools.Rendering;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace SmashTools.UnitTesting;

[UnitTest(TestType.MainMenu)]
[TestDescription("Render texture utils.")]
internal sealed class UnitTest_RenderTexture
{
  private readonly List<RenderTexture> objectsToDestroy = [];

  [SetUp]
  private void ClearObjectList()
  {
    objectsToDestroy.Clear();
  }

  [TearDown]
  private void DestroyAllObjects()
  {
    for (int i = objectsToDestroy.Count - 1; i >= 0; i--)
    {
      RenderTexture rTex = objectsToDestroy[i];
      // RenderTextures should already be destroyed here but by gathering all newly instantiated
      // test objects separate from the object pool we can verify independently that all
      // objects created from the object pool will be destroyed when dumped.
      Expect.IsFalse(rTex);
      if (rTex)
      {
        rTex.Release();
        Object.Destroy(rTex);
      }
    }
    objectsToDestroy.Clear();
  }

  [Test, ExecutionPriority(Priority.First)]
  private void CreateFormatted()
  {
    RenderTexture renderTexture = RenderTextureUtil.CreateRenderTexture(2, 2);

    Expect.IsTrue(renderTexture.IsCreated(), "RenderTexture GPU Allocated");
    Expect.AreEqual(renderTexture.depth, 0, "No Depth");
    Expect.IsTrue(SystemInfo.SupportsRenderTextureFormat(renderTexture.format),
      "RenderTexture Format Supported");
    Expect.Throws<ArgumentException>(delegate { _ = RenderTextureUtil.CreateRenderTexture(0, 0); },
      "Invalid RenderTexture Throws");

    renderTexture.Release();
    Object.Destroy(renderTexture);
  }

  [Test]
  private IEnumerator DoubleBuffer()
  {
    RenderTexture rtA = RenderTextureUtil.CreateRenderTexture(2, 2);
    RenderTexture rtB = RenderTextureUtil.CreateRenderTexture(2, 2);
    RenderTextureBuffer buffer = new(rtA, rtB);

    Assert.IsTrue(rtA.IsCreated());
    Assert.IsTrue(rtB.IsCreated());

    Expect.ReferencesAreEqual(buffer.Read, rtA, "Read No Swap Before");
    Expect.ReferencesAreEqual(buffer.Read, rtA, "Read No Swap After");
    Expect.ReferencesAreEqual(buffer.GetWrite(), rtB, "GetWrite Swap and Return");
    Expect.ReferencesAreEqual(buffer.Write, rtA, "Write Swapped");
    Expect.ReferencesAreEqual(buffer.Read, rtB, "Read Swapped");
    Expect.ReferencesAreNotEqual(buffer.Read, buffer.Write, "No Overwritten Assignment");
    _ = buffer.GetWrite();
    Expect.ReferencesAreEqual(buffer.Read, rtA, "Swap 2 Reset Read");
    Expect.ReferencesAreEqual(buffer.Write, rtB, "Swap 2 Reset Write");

    // Dispose will queue the texture object for destruction but we still have 1 frame to verify
    // GPU allocations were released.
    buffer.Dispose();

    Expect.AreEqual(buffer.Read.GetNativeDepthBufferPtr(), IntPtr.Zero, "Read GPU Memory Freed");
    Expect.AreEqual(buffer.Write.GetNativeDepthBufferPtr(), IntPtr.Zero, "Write GPU Memory Freed");

    // Allow RenderTextures to be destroyed, then verify
    yield return new WaitForEndOfFrame();

    Expect.IsFalse(rtA);
    Expect.IsFalse(rtB);
  }

  [Test]
  private IEnumerator Idler()
  {
    const float ExpiryTime = 999; // seconds

    RenderTexture rtA = RenderTextureUtil.CreateRenderTexture(2, 2);
    RenderTexture rtB = RenderTextureUtil.CreateRenderTexture(2, 2);
    Assert.IsNotNull(rtA);
    Assert.IsNotNull(rtB);
    RenderTextureIdler idler = new(rtA, rtB, ExpiryTime);

    Expect.IsTrue(UnityThread.InUpdateQueue(idler.UpdateLoop), "Idler Timer Started");
    Expect.IsTrue(idler.Read.IsCreated(), "Read Allocated");
    Expect.IsTrue(idler.Write.IsCreated(), "Write Allocated");
    Expect.ReferencesAreNotEqual(idler.Read, idler.GetWrite(), "Read/Write NotEqual");

    idler.SetTimeDirect(100);
    bool continue100 = idler.UpdateLoop();
    Expect.IsTrue(continue100, "UpdateLoop Continuing");
    Expect.IsTrue(idler.Read.IsCreated(), "SetTimer 100 Read Retained");
    Expect.IsTrue(idler.Write.IsCreated(), "SetTimer 100 Write Retained");

    idler.SetTimeDirect(9999);
    bool continue9999 = idler.UpdateLoop();
    Expect.IsFalse(continue9999, "UpdateLoop Stopping");
    UnityThread.RemoveUpdate(idler.UpdateLoop);

    Expect.AreEqual(idler.Read.GetNativeDepthBufferPtr(), IntPtr.Zero,
      "Idler Read GPU Memory Freed");
    Expect.AreEqual(idler.Write.GetNativeDepthBufferPtr(), IntPtr.Zero,
      "Idler Write GPU Memory Freed");

    // Allow RenderTextures to be destroyed, then verify
    yield return new WaitForEndOfFrame();

    Expect.IsFalse(rtA);
    Expect.IsFalse(rtB);
  }
}