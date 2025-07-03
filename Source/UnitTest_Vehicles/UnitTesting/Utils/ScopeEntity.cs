using System;
using Verse;

namespace Vehicles.UnitTesting;

public readonly struct ScopeEntity : IDisposable
{
  private readonly Thing entity;

  public ScopeEntity(Thing entity)
  {
    this.entity = entity;
  }

  void IDisposable.Dispose()
  {
    if (!entity.Destroyed)
      entity.Destroy();
  }
}