using JetBrains.Annotations;
using Verse;

namespace Vehicles;

[PublicAPI]
public interface IRegionSource
{
  RegionType ExpectedRegionType(IntVec3 cell, IPathingManager manager, VehicleDef vehicleDef);
}