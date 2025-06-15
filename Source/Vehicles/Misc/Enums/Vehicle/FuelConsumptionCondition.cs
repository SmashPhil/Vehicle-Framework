using System;

namespace Vehicles;

[Flags]
public enum FuelConsumptionCondition
{
  Drafted = 1 << 0,
  Moving = 1 << 1,
  Flying = 1 << 2,
  Always = 1 << 3
};