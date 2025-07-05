using JetBrains.Annotations;
using Verse;

namespace Vehicles;

[PublicAPI]
public interface IVehicleDefGenerator<T> where T : Def
{
  [Pure]
  bool TryGenerateImpliedDef(VehicleDef vehicleDef, out T impliedDef, bool hotReload);
}