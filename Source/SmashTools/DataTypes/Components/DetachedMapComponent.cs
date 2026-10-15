using System;
using JetBrains.Annotations;
using Verse;

namespace SmashTools;

/// <summary>
/// Can be retrieved with map component indexing, but doesn't piggyback off <see cref="MapComponent"/>
/// and is not cached by Map.
/// </summary>
/// <remarks>
/// This is useful for behavior that only needs to exist alongside a map reference. i.e. does not
/// need to tick, save, init on load, etc.
/// </remarks>
[PublicAPI]
public abstract class DetachedMapComponent : IDisposable
{
  protected readonly Map map;

  protected DetachedMapComponent(Map map)
  {
    this.map = map;
  }

  public virtual void Dispose()
  {
  }

  [Obsolete("Currently never invoked and is safe to remove. Override Dispose instead for pre-removal actions.", error: true)]
  protected virtual void PreMapRemoval()
  {
  }
}