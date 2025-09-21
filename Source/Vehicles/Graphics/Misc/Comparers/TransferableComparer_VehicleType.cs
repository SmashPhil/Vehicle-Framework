using System;

namespace Vehicles;

public class TransferableComparer_VehicleType : TransferableComparerBase
{
	protected override int Compare(VehicleDef vehicleDef, VehicleDef otherVehicleDef)
	{
		return CompareTypePriority(vehicleDef.type).CompareTo(CompareTypePriority(otherVehicleDef.type));

		static int CompareTypePriority(VehicleType type)
		{
			// Ordered from highest to lowest priority
			return type switch
			{
				VehicleType.Universal => 0,
				VehicleType.Land      => 1,
				VehicleType.Sea       => 2,
				VehicleType.Air       => 3,
				_                     => throw new NotImplementedException(nameof(VehicleType))
			};
		}
	}
}