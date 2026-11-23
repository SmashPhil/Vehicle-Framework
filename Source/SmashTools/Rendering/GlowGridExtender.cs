using System;
using System.Collections.Generic;
using CoreLib;
using CoreLib.Collections.Concurrent;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using static SmashTools.Rendering.GlowEmitter;

namespace SmashTools.Rendering;

internal struct GlowCell
{
  public required int index;
  public required Color32 contribution;
}

/// <summary>
/// Manages glow emitters and blends their light into a map's glow grid.
/// </summary>
[PublicAPI]
public sealed class GlowGridExtender : DetachedMapComponent
{
  private readonly List<GlowEmitter> emitters = [];
  private readonly List<GlowEmitter> dirtyEmitters = [];
  private readonly List<GlowEmitter> pendingDestruction = [];

  private readonly List<int> dirtyVanillaCells = [];
  private NativeArray<ushort> visited;
  private ushort closeStatus = 1;
  private readonly ColorInt[] colorSums;
  private readonly ConcurrentBuffer<int> blendCells;

  private JobHandle jobHandle;

  /// <summary>
  /// Initializes the glow grid extender for the given map.
  /// </summary>
  /// <param name="map">Map receiving the emitters' light.</param>
  public GlowGridExtender(Map map) : base(map)
  {
    int cellCount = map.cellIndices.NumGridCells;
    visited = new NativeArray<ushort>(cellCount, Allocator.Persistent);
    colorSums = new ColorInt[cellCount];
    blendCells = new ConcurrentBuffer<int>(cellCount);
  }

  private bool AnyDirty => dirtyEmitters.Count > 0;

  /// <summary>
  /// Registers an emitter and schedules its first computation.
  /// </summary>
  /// <param name="emitter">Emitter to register.</param>
  public void RegisterGlowEmitter(GlowEmitter emitter)
  {
    Assert.IsTrue(!emitters.Contains(emitter));
    emitters.Add(emitter);
    emitter.MarkDirty();
  }

  /// <summary>
  /// Deregisters an emitter and schedules its glow contribution for removal.
  /// </summary>
  /// <param name="emitter">Emitter to deregister.</param>
  /// <returns>
  /// <see langword="true"/> if the emitter was previously registered;
  /// otherwise, <see langword="false"/>.
  /// </returns>
  public bool DeregisterGlowEmitter(GlowEmitter emitter)
  {
    bool removed = emitters.Remove(emitter);
    if (removed)
    {
      emitter.IsOn = false;
      emitter.MarkDirty();
    }
    return removed;
  }

  /// <summary>
  /// Queues an emitter for disposal after pending glow computation.
  /// </summary>
  /// <param name="emitter">Emitter to dispose.</param>
  /// <remarks>Deregister a registered emitter before queuing it for disposal.</remarks>
  public void Destroy(GlowEmitter emitter)
  {
    pendingDestruction.Add(emitter);
  }

  internal void Dirty(GlowEmitter emitter)
  {
    Assert.IsFalse(dirtyEmitters.Contains(emitter));
    dirtyEmitters.Add(emitter);
  }

  internal void PreUpdate(NativeBitArray lightBlockers)
  {
    if (AnyDirty)
    {
      Assert.IsTrue(jobHandle.IsCompleted);
      Assert.IsTrue(blendCells.Count == 0);
      jobHandle = new ComputeEmittersJob
      {
        emitters = dirtyEmitters,
        blockers = lightBlockers
      }.ScheduleParallel(dirtyEmitters.Count,
        UnityData.GetIdealBatchCount(dirtyEmitters.Count),
        dependency: default);
      jobHandle = new ComputeDeltasJob
      {
        emitters = dirtyEmitters,
        visited = visited,
        closeStatus = closeStatus,
        blendCells = blendCells,
        colorSums = colorSums
      }.Schedule(dirtyEmitters.Count, dependency: jobHandle);

      jobHandle.Complete();
      foreach (int index in blendCells)
      {
        IntVec3 cell = map.cellIndices.IndexToCell(index);
        map.glowGrid.DirtyCell(cell);
      }
    }
    DestroyPending();
  }

  private void DestroyPending()
  {
    foreach (GlowEmitter emitter in pendingDestruction)
    {
      try
      {
        emitter.Dispose();
      }
      catch (Exception ex)
      {
        Log.Error($"Exception thrown disposing {emitter.GetType()}.\n{ex}");
      }
    }
    pendingDestruction.Clear();
  }

  internal void Update(NativeArray<Color32> accumulatedGlow, NativeArray<Color32> accumulatedGlowNoCavePlants)
  {
    if (!AnyDirty && dirtyVanillaCells.Count == 0)
      return;

    foreach (int index in blendCells)
    {
      BlendCell(index);
    }
    foreach (int index in dirtyVanillaCells)
    {
      BlendCell(index);
    }
    CleanUpComputeState();
    return;

    void BlendCell(int index)
    {
      ColorInt extra = colorSums[index];
      if (extra is { r: 0, g: 0, b: 0 })
        return;

      accumulatedGlow[index] = Blend(accumulatedGlow[index], extra);
      accumulatedGlowNoCavePlants[index] = Blend(accumulatedGlowNoCavePlants[index], extra);
    }

    static Color32 Blend(Color32 color, ColorInt extra)
    {
      return new Color32(
        r: (byte)Mathf.Clamp(color.r + extra.r, 0, 255),
        g: (byte)Mathf.Clamp(color.g + extra.g, 0, 255),
        b: (byte)Mathf.Clamp(color.b + extra.b, 0, 255),
        a: color.a == 0 && extra.a > 0 ? (byte)1 : color.a);
    }
  }

  internal void RecordDirtyCell(IntVec3 cell)
  {
    int index = map.cellIndices.CellToIndex(cell);
    if (visited[index] == closeStatus)
      return;

    ColorInt extra = colorSums[index];
    if (extra is { r: 0, g: 0, b: 0 })
      return;

    visited[index] = closeStatus;
    dirtyVanillaCells.Add(index);
  }

  private void CleanUpComputeState()
  {
    dirtyEmitters.Clear();
    blendCells.Clear();
    dirtyVanillaCells.Clear();
    if (++closeStatus == ushort.MaxValue)
    {
      NativeUtils.MemClear(visited);
      closeStatus = 1;
    }
  }

  internal void BlockerChanged(IntVec3 cell)
  {
    foreach (GlowEmitter emitter in emitters)
    {
      if (emitter.AffectedRect.Contains(cell))
      {
        emitter.MarkDirty();
      }
    }
  }

  /// <summary>
  /// Completes pending work and releases all unmanaged resources.
  /// </summary>
  public override void Dispose()
  {
    jobHandle.Complete();

    if (emitters.Count > 0)
    {
      Log.Error("Disposing GlowGridExtender before lights were removed. Forcing cleanup.");
      for (int i = emitters.Count - 1; i >= 0; i--)
      {
        GlowEmitter emitter = emitters[i];
        DeregisterGlowEmitter(emitter);
        Destroy(emitter);
      }
    }
    DestroyPending();
    visited.Dispose();
  }

  private struct ComputeEmittersJob : IJobFor
  {
    public required List<GlowEmitter> emitters;
    public required NativeBitArray blockers;

    public readonly void Execute(int index)
    {
      GlowEmitter emitter = emitters[index];
      emitter.ComputeStarted();
      if (emitter.IsOn && !emitter.Destroyed)
      {
        emitter.Compute(blockers);
      }
      emitter.ComputeFinished();
    }
  }
}
