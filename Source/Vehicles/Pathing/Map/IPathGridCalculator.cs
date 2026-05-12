using JetBrains.Annotations;
using Verse;

namespace Vehicles;

[PublicAPI]
public interface IPathGridCalculator
{
  ushort PathCostAt(Map map, IntVec3 cell, VehicleDef vehicleDef);
}
