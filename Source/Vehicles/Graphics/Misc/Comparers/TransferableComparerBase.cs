using System.Diagnostics;
using RimWorld;

namespace Vehicles;

public abstract class TransferableComparerBase : TransferableComparer
{
	public override int Compare(Transferable lhs, Transferable rhs)
	{
		if (lhs?.ThingDef is not VehicleDef lhDef)
		{
			Trace.Fail($"Using vehicle comparer with non vehicle entity {lhs?.ThingDef}");
			return 0;
		}
		if (rhs?.ThingDef is not VehicleDef rhDef)
		{
			Trace.Fail($"Using vehicle comparer with non vehicle entity {rhs?.ThingDef}");
			return 0;
		}
		if (lhs.AnyThing is VehiclePawn vehicle && rhs.AnyThing is VehiclePawn otherVehicle)
		{
			return Compare(vehicle, otherVehicle);
		}
		return Compare(lhDef, rhDef);
	}

	protected virtual int Compare(VehiclePawn vehicle, VehiclePawn otherVehicle)
	{
		return Compare(vehicle.VehicleDef, otherVehicle.VehicleDef);
	}

	protected abstract int Compare(VehicleDef vehicleDef, VehicleDef otherVehicleDef);
}