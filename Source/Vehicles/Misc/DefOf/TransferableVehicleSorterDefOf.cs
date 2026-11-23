using JetBrains.Annotations;
using RimWorld;

namespace Vehicles;

// ReSharper disable InconsistentNaming
[DefOf, PublicAPI]
public static class TransferableVehicleSorterDefOf
{
	// Prefixed to avoid colliding with vanilla TransferableSorterDefs (eg. Mass), since
	// TransferableVehicleSorterDef is also registered in DefDatabase<TransferableSorterDef>.
	[DefAlias("VF_Type")]
	public static TransferableVehicleSorterDef Type;

	[DefAlias("VF_MoveSpeed")]
	public static TransferableVehicleSorterDef MoveSpeed;

	[DefAlias("VF_CargoCapacity")]
	public static TransferableVehicleSorterDef CargoCapacity;

	[DefAlias("VF_Mass")]
	public static TransferableVehicleSorterDef Mass;

	static TransferableVehicleSorterDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(TransferableVehicleSorterDefOf));
	}
}