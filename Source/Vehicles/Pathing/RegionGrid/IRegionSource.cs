using JetBrains.Annotations;
using Verse;

namespace Vehicles;

[PublicAPI]
public interface IRegionSource
{
  RegionType ExpectedRegionType(IntVec3 cell, VehiclePathingSystem pathingSystem, VehicleDef vehicleDef);
}