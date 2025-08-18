using HarmonyLib;
using SmashTools.Patching;
using Verse;

namespace Vehicles.Compatibility;

public abstract class ConditionalVehiclePatch : IConditionalPatch
{
	string IConditionalPatch.SourceId => VehicleHarmony.VehiclesUniqueId;

	public abstract string PackageId { get; }

	public abstract PatchSequence PatchAt { get; }

	public abstract void PatchAll(ModMetaData mod);
}