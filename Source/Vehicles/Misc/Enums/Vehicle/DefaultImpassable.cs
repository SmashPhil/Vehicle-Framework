using System;

namespace Vehicles;

[Flags]
public enum DefaultImpassable
{
  None = 0,
  Terrain = 1 << 0,
  Things = 1 << 1,
  Biomes = 1 << 2,
  Rivers = 1 << 3,
  Hilliness = 1 << 4,
  Roads = 1 << 5
}