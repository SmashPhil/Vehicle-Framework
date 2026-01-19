using System.Collections.Generic;
using JetBrains.Annotations;
using SmashTools;
using Verse;

namespace Vehicles;

public sealed class StatCache
{
  private readonly VehiclePawn vehicle;

  private readonly float[] cachedValues;
  private readonly bool[] dirty;

  public StatCache(VehiclePawn vehicle)
  {
    this.vehicle = vehicle;
    int cacheSize = DefDatabase<VehicleStatDef>.DefCount;
    cachedValues = new float[cacheSize];
    dirty = new bool[cacheSize].Populate(true);
  }

  public bool IsDirty(VehicleStatDef statDef)
  {
    return dirty[statDef.DefIndex];
  }

  public float this[VehicleStatDef statDef]
  {
    get
    {
      if (dirty[statDef.DefIndex])
      {
        RecacheFor(statDef);
      }
      return cachedValues[statDef.DefIndex];
    }
  }

  public void MarkDirty(VehicleStatDef statDef)
  {
    dirty[statDef.DefIndex] = true;
  }

  /// <summary>
  /// Mark all stats dirty, will recache on next fetch
  /// </summary>
  public void Reset()
  {
    for (int i = 0; i < dirty.Length; i++)
    {
      dirty[i] = true;
    }
  }

  private void RecacheFor(VehicleStatDef statDef)
  {
    cachedValues[statDef.DefIndex] = statDef.Worker.GetValue(vehicle);
    dirty[statDef.DefIndex] = false;
  }

  [PublicAPI]
  public class EventLister
  {
    public VehicleStatDef statDef;
    public List<VehicleEventDef> eventDefs;
  }
}