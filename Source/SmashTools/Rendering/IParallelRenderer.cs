using Verse;

namespace SmashTools.Rendering;

public interface IParallelRenderer
{
  /// <summary>
  /// Renderer is dirty and needs <see cref="DrawPhase.EnsureInitialized"/> step next draw cycle.
  /// </summary>
  bool IsDirty { get; set; }

  /// <summary>
  /// interface implementation of <see cref="Thing.DynamicDrawPhaseAt"/>. Must be called from
  /// whichever entity this is being rendered from.
  /// </summary>
  /// <param name="phase">Current draw phase.</param>
  /// <param name="transformData">base Transform values of entity parent.</param>
  /// <param name="forceDraw">Renderer should draw regardless of any local configurations.</param>
  void DynamicDrawPhaseAt(DrawPhase phase, in TransformData transformData, bool forceDraw = false);
}