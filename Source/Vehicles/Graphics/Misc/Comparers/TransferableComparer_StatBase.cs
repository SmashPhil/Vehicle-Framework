using UnityEngine;

namespace Vehicles;

public abstract class TransferableComparer_StatBase : TransferableComparerBase
{
	protected abstract VehicleStatDef StatDef { get; }

	protected virtual CompareType Type => CompareType.Higher;

	protected override int Compare(VehiclePawn vehicle, VehiclePawn otherVehicle)
	{
		float value = vehicle.GetStatValue(StatDef);
		float otherValue = otherVehicle.GetStatValue(StatDef);
		return CompareStatValues(value, otherValue);
	}

	protected override int Compare(VehicleDef vehicleDef, VehicleDef otherVehicleDef)
	{
		float value = vehicleDef.GetStatValueAbstract(StatDef);
		float otherValue = otherVehicleDef.GetStatValueAbstract(StatDef);
		return CompareStatValues(value, otherValue);
	}

	private int CompareStatValues(float value, float otherValue)
	{
		if (Mathf.Approximately(value, otherValue))
			return 0;

		return Type == CompareType.Lower ?
			(value < otherValue ? -1 : 1) :
			(value > otherValue ? -1 : 1);
	}

	protected enum CompareType
	{
		Lower,
		Higher
	}
}