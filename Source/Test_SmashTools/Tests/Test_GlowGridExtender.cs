using System;
using System.Collections.Generic;
using DevTools.Testing;
using SmashTools.Rendering;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Tests;

[TestFixture(TestType.Playing)]
[TestCategory(TestCategoryNames.Rendering)]
[TestDescription("Glow emitters update and blend into the map glow grid.")]
internal class Test_GlowGridExtender
{
  private static readonly IntVec3 East = new(1, 0, 0);

  private readonly List<TestEmitter> emitters = [];

  private Map map;
  private GlowGridExtender extender;
  private IntVec3 cell;
  private Color32 originalGlow;

  [SetUp]
  private void SetUp()
  {
    map = Find.CurrentMap;
    Assert.IsNotNull(map);
    extender = map.GetDetachedMapComponent<GlowGridExtender>();
    map.glowGrid.GlowGridUpdate_First();
    cell = FindTestCell();
    originalGlow = map.glowGrid.VisualGlowAt(cell);
  }

  [TearDown]
  private void TearDown()
  {
    foreach (TestEmitter emitter in emitters)
    {
      if (!emitter.IsDisposed && !emitter.Destroyed)
        emitter.Destroy();
    }
    map.glowGrid.GlowGridUpdate_First();
    emitters.Clear();
  }

  [Test]
  private void RegisterBlends()
  {
    TestEmitter emitter = Register(new Color32(20, 30, 40, 0));
    Expect.IsTrue(emitter.IsDirty);

    UpdateGlow();

    Expect.AreEqual(1, emitter.ComputeCalls);
    ExpectGlow(cell, originalGlow, new Color32(20, 30, 40, 0));
  }

  [Test]
  private void MarkDirtyRecomputesOnce()
  {
    TestEmitter emitter = Register(new Color32(20, 0, 0, 0));
    UpdateGlow();

    emitter.MarkDirty();
    emitter.MarkDirty();
    UpdateGlow();

    Expect.AreEqual(2, emitter.ComputeCalls);
    Expect.IsFalse(emitter.IsDirty);
  }

  [Test]
  private void MovingEmitterClearsOldCell()
  {
    TestEmitter emitter = Register(new Color32(20, 0, 0, 0));
    IntVec3 nextCell = cell + East;
    Color32 nextOriginalGlow = map.glowGrid.VisualGlowAt(nextCell);
    UpdateGlow();

    emitter.Cell = nextCell;
    emitter.MarkDirty();
    UpdateGlow();

    ExpectGlow(cell, originalGlow, default);
    ExpectGlow(nextCell, nextOriginalGlow, new Color32(20, 0, 0, 0));
  }

  [Test]
  private void TurningOffClearsLight()
  {
    TestEmitter emitter = Register(new Color32(20, 0, 0, 0));
    UpdateGlow();

    emitter.IsOn = false;
    UpdateGlow();

    ExpectGlow(cell, originalGlow, default);
  }

  [Test]
  private void OverlappingEmittersAccumulate()
  {
    Register(new Color32(20, 0, 0, 0));
    Register(new Color32(0, 30, 0, 0));

    UpdateGlow();

    ExpectGlow(cell, originalGlow, new Color32(20, 30, 0, 0));
  }

  [Test]
  private void DestroyOneEmitterPreservesOther()
  {
    TestEmitter first = Register(new Color32(20, 0, 0, 0));
    Register(new Color32(0, 30, 0, 0));
    UpdateGlow();

    first.Destroy();
    UpdateGlow();

    Expect.IsTrue(first.IsDisposed);
    ExpectGlow(cell, originalGlow, new Color32(0, 30, 0, 0));
  }

  [Test]
  private void BlockerChangedOnlyDirtiesAffectedEmitter()
  {
    TestEmitter emitter = Register(new Color32(20, 0, 0, 0));
    UpdateGlow();

    extender.BlockerChanged(cell + East);
    UpdateGlow();
    Expect.AreEqual(1, emitter.ComputeCalls);

    extender.BlockerChanged(cell);
    UpdateGlow();
    Expect.AreEqual(2, emitter.ComputeCalls);
  }

  [Test]
  private void DeregisterRemovesLight()
  {
    TestEmitter emitter = Register(new Color32(20, 0, 0, 0));
    UpdateGlow();

    Expect.IsTrue(extender.DeregisterGlowEmitter(emitter));
    Expect.IsFalse(extender.DeregisterGlowEmitter(emitter));
    UpdateGlow();

    Expect.IsFalse(emitter.IsDisposed);
    ExpectGlow(cell, originalGlow, default);
  }

  [Test]
  private void DestroyBeforeFirstUpdate()
  {
    TestEmitter emitter = Register(new Color32(20, 0, 0, 0));

    emitter.Destroy();
    Expect.IsTrue(emitter.Destroyed);
    Expect.IsFalse(emitter.IsDisposed);
    UpdateGlow();

    Expect.AreEqual(0, emitter.ComputeCalls);
    Expect.IsTrue(emitter.IsDisposed);
    ExpectGlow(cell, originalGlow, default);
  }

  [Test]
  private void RepeatedDestroyDisposesOnce()
  {
    TestEmitter emitter = Register(new Color32(20, 0, 0, 0));

    emitter.Destroy();
    emitter.Destroy();
    UpdateGlow();

    Expect.AreEqual(1, emitter.DisposeCalls);
  }

  [Test]
  private void DisposedEmitterCannotDirty()
  {
    TestEmitter emitter = Register(new Color32(20, 0, 0, 0));
    emitter.Destroy();
    UpdateGlow();

    emitter.MarkDirty();

    Expect.IsFalse(emitter.IsDirty);
  }

  [Test]
  private void DestroyAfterDeregister()
  {
    TestEmitter emitter = Register(new Color32(20, 0, 0, 0));
    UpdateGlow();
    extender.DeregisterGlowEmitter(emitter);
    UpdateGlow();

    emitter.Destroy();
    UpdateGlow();

    Expect.IsTrue(emitter.IsDisposed);
    ExpectGlow(cell, originalGlow, default);
  }

  [Test]
  private void DestroyPendingEmitter()
  {
    TestEmitter emitter = Create(new Color32(20, 0, 0, 0));
    emitter.MarkDirty();

    extender.Destroy(emitter);
    UpdateGlow();

    Expect.AreEqual(1, emitter.DisposeCalls);
  }

  [Test]
  private void VanillaDirtyCellRetainsLight()
  {
    Register(new Color32(20, 0, 0, 0));
    UpdateGlow();
    Color32 glow = map.glowGrid.VisualGlowAt(cell);

    map.glowGrid.DirtyCell(cell);
    UpdateGlow();

    Color32 actual = map.glowGrid.VisualGlowAt(cell);
    Expect.AreEqual(glow.r, actual.r);
    Expect.AreEqual(glow.g, actual.g);
    Expect.AreEqual(glow.b, actual.b);
  }

  private TestEmitter Create(Color32 color)
  {
    TestEmitter emitter = new(map, cell, color);
    emitters.Add(emitter);
    return emitter;
  }

  private TestEmitter Register(Color32 color)
  {
    TestEmitter emitter = Create(color);
    extender.RegisterGlowEmitter(emitter);
    emitter.IsOn = true;
    return emitter;
  }

  private void UpdateGlow()
  {
    map.glowGrid.GlowGridUpdate_First();
  }

  private IntVec3 FindTestCell()
  {
    foreach (IntVec3 candidate in map.AllCells)
    {
      IntVec3 next = candidate + East;
      if (candidate.x < 2 || candidate.z < 2 || !next.InBounds(map))
        continue;

      Color32 glow = map.glowGrid.VisualGlowAt(candidate);
      Color32 nextGlow = map.glowGrid.VisualGlowAt(next);
      if (glow.r <= 200 && glow.g <= 200 && glow.b <= 200 &&
        nextGlow.r <= 200 && nextGlow.g <= 200 && nextGlow.b <= 200)
        return candidate;
    }
    throw new InvalidOperationException("No suitable cell for glow grid tests.");
  }

  private void ExpectGlow(IntVec3 target, Color32 baseline, Color32 addition)
  {
    Color32 glow = map.glowGrid.VisualGlowAt(target);
    Expect.AreEqual(Math.Min(255, baseline.r + addition.r), glow.r);
    Expect.AreEqual(Math.Min(255, baseline.g + addition.g), glow.g);
    Expect.AreEqual(Math.Min(255, baseline.b + addition.b), glow.b);
  }

  private sealed class TestEmitter : GlowEmitter
  {
    public TestEmitter(Map map, IntVec3 cell, Color32 color) : base(map, initialCapacity: 1)
    {
      Cell = cell;
      this.color = color;
    }

    public IntVec3 Cell { get; set; }

    public int ComputeCalls { get; private set; }

    public int DisposeCalls { get; private set; }

    public bool IsDisposed
    {
      get
      {
        return Disposed;
      }
    }

    protected internal override CellRect AffectedRect
    {
      get
      {
        return CellRect.SingleCell(Cell);
      }
    }

    protected internal override void Compute(NativeBitArray blockers)
    {
      ComputeCalls++;
      Fill(map.cellIndices.CellToIndex(Cell), color);
    }

    public override void Dispose()
    {
      if (Disposed)
        return;

      base.Dispose();
      DisposeCalls++;
    }
  }
}
