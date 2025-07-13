using System;

namespace Vehicles;

// TODO - 'Always' could be better set as an 'All' condition. No name change is necessary but there could be
// more conditions available under which a vehicle consumes fuel.  Or at the very least, some control over when
// it consumes fuel on the world map + Drafted / Undrafted / Moving.
[Flags]
public enum FuelConsumptionCondition
{
  Drafted = 1 << 0,
  Moving = 1 << 1,
  Flying = 1 << 2,
  Always = 1 << 3, // TODO - Remove
  All = Drafted | Moving | Flying
};