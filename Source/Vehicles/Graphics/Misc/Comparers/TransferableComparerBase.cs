using RimWorld;

namespace Vehicles;

public abstract class TransferableComparerBase : TransferableComparer
{
	public override int Compare(Transferable lhs, Transferable rhs)
	{
		// Vehicle sorters are registered in DefDatabase<TransferableSorterDef>, so anything listing all
		// sorters (eg. other mods' transfer windows) can hand non-vehicle entries to this comparer. Keep
		// vehicles first and leave the relative order of everything else untouched instead of
		// asserting, which terminates the process through FailFast on the 1.7 .NET runtime.
		VehicleDef lhDef = lhs?.ThingDef as VehicleDef;
		VehicleDef rhDef = rhs?.ThingDef as VehicleDef;
		if (lhDef is null || rhDef is null)
		{
			if (lhDef is null && rhDef is null)
				return 0;

			return lhDef is null ? 1 : -1;
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