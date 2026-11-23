using System;
using System.Collections.Generic;
using CoreLib.Collections.Concurrent;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Rendering;

/// <summary>
/// Base class for light sources that contribute to a map's glow grid.
/// </summary>
[PublicAPI]
public abstract class GlowEmitter : IDisposable
{
  /// <summary>
  /// Map receiving this emitter's light.
  /// </summary>
  protected readonly Map map;

  /// <summary>
  /// Glow grid extender managing this emitter.
  /// </summary>
  protected readonly GlowGridExtender extender;

  private DoubleBuffer<NativeList<GlowCell>> buffer;

  /// <summary>
  /// Radius within which cells can be marked as overlit.
  /// </summary>
  protected float overlightRadius;

  /// <summary>
  /// Glow color. Alpha does not control opacity, overlighting uses <see cref="overlightRadius"/>.
  /// </summary>
  protected Color32 color = new(255, 255, 255, 0);

  /// <summary>
  /// Initializes an emitter for the given map.
  /// </summary>
  /// <param name="map">Map receiving this emitter's light.</param>
  /// <param name="initialCapacity">Initial capacity of each contribution buffer.</param>
  protected GlowEmitter([NotNull] Map map, int initialCapacity = 256)
  {
    this.map = map;
    extender = map.GetDetachedMapComponent<GlowGridExtender>();
    buffer = new DoubleBuffer<NativeList<GlowCell>>(
      a: new NativeList<GlowCell>(initialCapacity, Allocator.Persistent),
      b: new NativeList<GlowCell>(initialCapacity, Allocator.Persistent));
  }

  /// <summary>
  /// Whether this emitter has released its native buffers.
  /// </summary>
  protected bool Disposed { get; private set; }

  internal bool Destroyed { get; private set; }

  internal bool IsDirty { get; private set; }

  /// <summary>
  /// Whether this emitter contributes light. Changing the value schedules a recomputation.
  /// </summary>
  public bool IsOn
  {
    get;
    set
    {
      if (field == value)
        return;

      field = value;
      MarkDirty();
    }
  } = true;
                      
  /// <summary>
  /// Bounds used to detect blocker changes that require recomputing this emitter.
  /// </summary>
  protected internal abstract CellRect AffectedRect { get; }

  /// <summary>
  /// Adds a cell's contribution to the current computation.
  /// </summary>
  /// <param name="cellIndex">Index of the cell in the map grid.</param>
  /// <param name="contribution">Glow color contributed to the cell.</param>
  protected void Fill(int cellIndex, Color32 contribution)
  {
    buffer.Write.Add(new GlowCell { index = cellIndex, contribution = contribution });
  }

  /// <summary>
  /// Computes the glow contributed by this emitter.
  /// </summary>
  /// <param name="blockers">Map cells that block light.</param>
  protected internal abstract void Compute([ReadOnly] NativeBitArray blockers);

  internal void ComputeStarted()
  {
    buffer.Write.Clear();
  }

  internal void ComputeFinished()
  {
    buffer.Swap();
    IsDirty = false;
  }

  /// <summary>
  /// Schedules this emitter to recompute its glow contribution.
  /// </summary>
  public void MarkDirty()
  {
    if (IsDirty || Disposed)
      return;

    IsDirty = true;
    extender.Dirty(this);
  }

  /// <summary>
  /// Deregisters this emitter and schedules its disposal.
  /// </summary>
  /// <exception cref="ObjectDisposedException">The emitter has already been disposed.</exception>
  public void Destroy()
  {
    ObjectDisposedException.ThrowIf(Disposed, this);

    if (Destroyed)
      return;

    Destroyed = true;
    MarkDirty();
    extender.DeregisterGlowEmitter(this);
    extender.Destroy(this);
  }

  /// <summary>
  /// Releases this emitter's native buffers.
  /// </summary>
  /// <remarks>Call <see cref="Destroy"/> to deregister the emitter from the map before disposal.</remarks>
  public virtual void Dispose()
  {
    if (Disposed)
      return;

    Disposed = true;
    buffer.Dispose();
    GC.SuppressFinalize(this);
  }

  internal struct ComputeDeltasJob : IJobFor
  {
    public required List<GlowEmitter> emitters;
    public required ushort closeStatus;
    public required NativeArray<ushort> visited;

    public required ColorInt[] colorSums;
    public required ConcurrentBuffer<int> blendCells;
    
    public void Execute(int index)
    {
      GlowEmitter emitter = emitters[index];
      // Previous deltas
      foreach (GlowCell cell in emitter.buffer.Write)
      {
        Remove(cell);
        Visit(cell.index);
      }
      // Current deltas
      foreach (GlowCell cell in emitter.buffer.Read)
      {
        Add(cell);
        Visit(cell.index);
      }
    }

    private readonly void Add(GlowCell cell)
    {
      ref ColorInt sum = ref colorSums[cell.index];
      Color32 contribution = cell.contribution;
      sum.r += contribution.r;
      sum.g += contribution.g;
      sum.b += contribution.b;
      sum.a += contribution.a;
    }

    private readonly void Remove(GlowCell cell)
    {
      ref ColorInt sum = ref colorSums[cell.index];
      Color32 contribution = cell.contribution;
      sum.r -= contribution.r;
      sum.g -= contribution.g;
      sum.b -= contribution.b;
      sum.a -= contribution.a;
    }

    private void Visit(int index)
    {
      if (visited[index] == closeStatus)
        return;

      visited[index] = closeStatus;
      bool addedToBuffer = blendCells.TryAdd(index);
      Assert.IsTrue(addedToBuffer);
    }
  }
}
