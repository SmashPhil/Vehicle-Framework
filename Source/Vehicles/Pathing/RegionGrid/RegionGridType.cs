using JetBrains.Annotations;

namespace Vehicles;

[PublicAPI]
public enum RegionGridType
{
  Normal,
  Breach,
  Invalid = 0xFF
}