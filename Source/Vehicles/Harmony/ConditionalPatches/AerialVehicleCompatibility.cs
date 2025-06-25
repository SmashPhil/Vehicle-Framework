using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles.Compatibility;

public static class AerialVehicleCompatibility
{
  private static readonly HashSet<Type> canLandInWorldObjects = [];

  public static void AddObject(Type type)
  {
    canLandInWorldObjects.Add(type);
  }

  public static bool CanLandIn(MapParent mapParent)
  {
    return canLandInWorldObjects.Contains(mapParent.GetType());
  }
}