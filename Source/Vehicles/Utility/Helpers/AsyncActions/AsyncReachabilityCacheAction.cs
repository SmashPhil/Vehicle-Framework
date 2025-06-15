using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;
using SmashTools.Performance;

namespace Vehicles
{
  public class AsyncReachabilityCacheAction : AsyncAction
  {
    private VehiclePathingSystem mapping;
    private List<VehicleDef> vehicleDefs;

    public override bool IsValid => mapping?.map?.Index > -1;

    public void Set(VehiclePathingSystem mapping, List<VehicleDef> vehicleDefs)
    {
      this.mapping = mapping;
      this.vehicleDefs = vehicleDefs;
    }

    public override void Invoke()
    {
      foreach (VehicleDef vehicleDef in vehicleDefs)
      {
        if (mapping.GridOwners.IsOwner(vehicleDef))
        {
          mapping[vehicleDef].VehicleReachability.ClearCache();
        }
      }
    }

    public override void ReturnToPool()
    {
      mapping = null;
      vehicleDefs = null;
      AsyncPool<AsyncReachabilityCacheAction>.Return(this);
    }
  }
}