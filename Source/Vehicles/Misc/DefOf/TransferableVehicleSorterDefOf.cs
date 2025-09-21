using JetBrains.Annotations;
using RimWorld;

namespace Vehicles;

// ReSharper disable InconsistentNaming
[DefOf, PublicAPI]
public static class TransferableVehicleSorterDefOf
{
	public static TransferableVehicleSorterDef Type;
	public static TransferableVehicleSorterDef MoveSpeed;
	public static TransferableVehicleSorterDef CargoCapacity;
	public static TransferableVehicleSorterDef Mass;

	static TransferableVehicleSorterDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(TransferableVehicleSorterDefOf));
	}
}