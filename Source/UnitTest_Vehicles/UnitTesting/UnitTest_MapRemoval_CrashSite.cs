using DevTools.Testing;
using HarmonyLib;
using RimWorld;
using UnityEngine.Assertions;
using Vehicles.World;
using Priority = DevTools.Testing.Priority;

namespace Vehicles.UnitTesting;

[TestDescription("Maps account for vehicles when checking removal conditions.")]
internal sealed class UnitTest_MapRemoval_CrashSite : UnitTest_MapRemoval<CrashSite>
{
	private readonly AccessTools.FieldRef<CrashSite, int> ticksSinceCrashRef =
		AccessTools.FieldRefAccess<CrashSite, int>(AccessTools.Field(typeof(CrashSite), "ticksSinceCrash"));

	protected override WorldObjectDef WorldObjectDef => WorldObjectDefOfVehicles.CrashedShipSite;

	protected override void PostGenerateMap()
	{
		ticksSinceCrashRef.Invoke(mapParent) = CrashSite.TicksTillRemovalAfterCrash;
	}

	[Test, ExecutionPriority(Priority.Last)]
	private void ObservationTimeout()
	{
		Assert.IsTrue(mapParent.ShouldRemoveMapNow(out _));
		ticksSinceCrashRef.Invoke(mapParent) = 0;
		Expect.IsFalse(mapParent.ShouldRemoveMapNow(out _));
		ticksSinceCrashRef.Invoke(mapParent) = CrashSite.TicksTillRemovalAfterCrash;
		Expect.IsTrue(mapParent.ShouldRemoveMapNow(out _));
	}
}