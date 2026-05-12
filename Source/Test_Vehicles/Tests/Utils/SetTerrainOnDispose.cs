using System;
using Verse;

namespace Vehicles.Testing;

public readonly struct SetTerrainOnDispose(Map map, TerrainDef terrainDef, CellRect area) : IDisposable
{
  void IDisposable.Dispose()
  {
    DebugHelper.DestroyArea(area, map, terrainDef);
  }
}