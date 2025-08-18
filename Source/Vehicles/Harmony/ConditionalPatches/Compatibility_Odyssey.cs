using RimWorld;
using SmashTools.Patching;
using Verse;

namespace Vehicles.Compatibility;

internal class Compatibility_Odyssey : ConditionalVehiclePatch
{
	public override string PackageId => ModPackageIds.Odyssey;

	public override PatchSequence PatchAt =>
#if FISHING
		PatchSequence.PostDefDatabase;
#else
		PatchSequence.Disabled;
#endif

	public override void PatchAll(ModMetaData mod)
	{
		// Odyssey fishing has hardcoded weights, and all common / uncommon fish are treated equally
		const float WeightCommon = 1;
		const float WeightUncommon = 0.05f;

		foreach (BiomeDef biomeDef in DefDatabase<BiomeDef>.AllDefsListForReading)
		{
			if (biomeDef.fishTypes == null)
				continue;

			foreach (FishChance fishChance in biomeDef.fishTypes.freshwater_Common)
			{
				//Assert.IsTrue(fishChance.fishDef.thingCategories.NotNullAndContains(ThingCategoryDefOf.Fish));
				FishingCompatibility.AddFishDef(biomeDef, WaterBodyType.Freshwater, fishChance.fishDef, WeightCommon);
			}
			foreach (FishChance fishChance in biomeDef.fishTypes.saltwater_Common)
			{
				FishingCompatibility.AddFishDef(biomeDef, WaterBodyType.Saltwater, fishChance.fishDef, WeightCommon);
			}
			foreach (FishChance fishChance in biomeDef.fishTypes.freshwater_Uncommon)
			{
				FishingCompatibility.AddFishDef(biomeDef, WaterBodyType.Freshwater, fishChance.fishDef, WeightUncommon);
			}
			foreach (FishChance fishChance in biomeDef.fishTypes.saltwater_Uncommon)
			{
				FishingCompatibility.AddFishDef(biomeDef, WaterBodyType.Saltwater, fishChance.fishDef, WeightUncommon);
			}
		}
	}
}