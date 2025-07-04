using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace Vehicles;

[PublicAPI]
public class StatOffset
{
  public readonly VehiclePawn vehicle;
  public readonly StatDef statDef;
  public readonly VehicleStatDef vehicleStatDef;
  public readonly StatUpgradeCategoryDef upgradeCategoryDef;

  private float offset;

  private List<(string key, float value)> overrideValues;

  public StatOffset(VehiclePawn vehicle, StatUpgradeCategoryDef upgradeCategoryDef)
  {
    this.vehicle = vehicle;
    this.upgradeCategoryDef = upgradeCategoryDef;
  }

  public StatOffset(VehiclePawn vehicle, VehicleStatDef vehicleStatDef)
  {
    this.vehicle = vehicle;
    this.vehicleStatDef = vehicleStatDef;
  }

  public StatOffset(VehiclePawn vehicle, StatDef statDef)
  {
    this.vehicle = vehicle;
    this.statDef = statDef;
  }

  public float Offset
  {
    get { return offset; }
    set
    {
      if (!Mathf.Approximately(offset, value))
      {
        offset = value;
        vehicle.statHandler.MarkAllDirty();
      }
    }
  }

  public bool TryGetOverride(out float value)
  {
    value = float.NaN;
    if (overrideValues.NullOrEmpty())
    {
      return false;
    }
    value = overrideValues.LastOrDefault().value;
    return true;
  }

  public void RemoveOverride(string key)
  {
    overrideValues.RemoveWhere(tuple => tuple.key == key);
  }

  public void AddOverride(string key, float value)
  {
    overrideValues ??= [];
    overrideValues.Add((key, value));
  }
}