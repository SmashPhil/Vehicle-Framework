using System;

namespace Vehicles;

/// <summary>
/// Subject for removal, merely dictates permission checks related to drafting and takeoff. VehicleDef
/// now automatically sets this based on other configurations.
/// </summary>
[Flags]
public enum VehiclePermissions
{
  Immobile = 1 << 0,
  DriverNeeded = 1 << 1,
  Autonomous = 1 << 2
}