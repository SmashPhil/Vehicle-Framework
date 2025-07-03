using System;
using RimWorld.Planet;

namespace Vehicles.UnitTesting;

public readonly struct ScopeWorldObject : IDisposable
{
  private readonly WorldObject worldObject;

  public ScopeWorldObject(WorldObject worldObject)
  {
    this.worldObject = worldObject;
  }

  void IDisposable.Dispose()
  {
    if (!worldObject.Destroyed)
      worldObject.Destroy();
  }
}