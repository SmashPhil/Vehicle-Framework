using System;
using System.Collections.Generic;
using RimWorld.Planet;

namespace Vehicles.Compatibility;

// TODO - this is sus, revisit later
public static class AerialVehicleCompatibility
{
  private static readonly HashSet<Type> CanLandInWorldObjects = [];

  public static void AddObject(Type type)
  {
    CanLandInWorldObjects.Add(type);
  }

  public static bool CanLandIn(MapParent mapParent)
  {
    return CanLandInWorldObjects.Contains(mapParent.GetType());
  }
}