using System;

namespace Vehicles;

[Flags]
public enum HandlingType
{
  None = 0,
  Movement = 1 << 0,
  Turret = 1 << 1,

  Any = Movement | Turret
}