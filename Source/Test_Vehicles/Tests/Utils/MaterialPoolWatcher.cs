using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles.UnitTesting;

internal class MaterialPoolWatcher : IDisposable
{
  private readonly Dictionary<IMaterialCacheTarget, int> materialsFreed = [];
  private readonly Dictionary<IMaterialCacheTarget, int> materialsAllocated = [];

  public MaterialPoolWatcher()
  {
    RGBMaterialPool.OnTargetCached += TargetCached;
    RGBMaterialPool.OnTargetRemoved += TargetDestroyed;
  }

  public int CacheTargets => materialsAllocated.Count;

  public int MaterialsAllocated => materialsAllocated.Values.Sum();

  public int MaterialsFreed => materialsFreed.Values.Sum();

  // MaterialPool cache changes may include freeing up materials that were allocated
  // before the watcher was created. Any new allocations should be equal to the amount
  // we've deallocated, meaning the cache target was restored to its original state.
  public bool AllocationsEqualized => MaterialsAllocated == MaterialsFreed;

  public bool AllFree => CacheTargets == 0 && MaterialsAllocated == 0;

  public int TargetMaterials(IMaterialCacheTarget target)
  {
    return materialsAllocated.TryGetValue(target, -1);
  }

  private void TargetCached(IMaterialCacheTarget target)
  {
    if (!materialsFreed.Remove(target))
    {
      // If target material allocations aren't reversing a destroy action prior,
      // then they are new allocations and must be tracked for end of lifetime count.
      materialsAllocated.Add(target, target.MaterialCount);
    }
  }

  private void TargetDestroyed(IMaterialCacheTarget target)
  {
    if (!materialsAllocated.Remove(target))
    {
      // If target destroyed isn't being tracked at the end of its lifecycle, then
      // it's presumed to be added back after any other allocated materials are destroyed.
      materialsFreed.Add(target, target.MaterialCount);
    }
  }

  void IDisposable.Dispose()
  {
    RGBMaterialPool.OnTargetCached -= TargetCached;
    RGBMaterialPool.OnTargetRemoved -= TargetDestroyed;
  }
}