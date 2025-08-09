using System;
using Verse;

namespace Vehicles.UnitTesting;

public readonly struct SetTerrainOnDispose(Map map, TerrainDef terrainDef, CellRect area) : IDisposable
{
  void IDisposable.Dispose()
  {
    DebugHelper.DestroyArea(area, map, terrainDef);
  }
}