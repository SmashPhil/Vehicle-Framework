using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using SmashTools.Patching;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.Compatibility;

internal class Compatibility_VanillaExpandedFishing : ConditionalVehiclePatch
{
	public override string PackageId => ModPackageIds.VanillaExpandedFishing;

	public override PatchSequence PatchAt => PatchSequence.PostDefDatabase;

	public override void PatchAll(ModMetaData mod)
	{
		if (ModsConfig.OdysseyActive)
			return;

		Type fishDefType = GenTypes.GetTypeInAnyAssembly("VCE_Fishing.FishDef");
		Assert.IsNotNull(fishDefType);
		Type biomeTempDefType = GenTypes.GetTypeInAnyAssembly("VCE_Fishing.BiomeTempDef");
		Assert.IsNotNull(biomeTempDefType);
		FieldInfo biomeTempLabelField = AccessTools.Field(biomeTempDefType, "biomeTempLabel");
		FieldInfo biomesField = AccessTools.Field(biomeTempDefType, "biomes");

		FieldInfo thingDefField = AccessTools.Field(fishDefType, "thingDef");
		FieldInfo allowedBiomesField = AccessTools.Field(fishDefType, "allowedBiomes");
		FieldInfo canBeFreshWaterField = AccessTools.Field(fishDefType, "canBeFreshwater");
		FieldInfo canBeSaltWaterField = AccessTools.Field(fishDefType, "canBeSaltwater");
		FieldInfo commonalityField = AccessTools.Field(fishDefType, "commonality");
		FieldInfo baseFishingYieldField = AccessTools.Field(fishDefType, "baseFishingYield");

		Dictionary<string, Def> biomeTempDefs = GenDefDatabase.GetAllDefsInDatabaseForDef(biomeTempDefType)
		 .ToDictionary(def => (string)biomeTempLabelField.GetValue(def), def => def);
		foreach (Def def in GenDefDatabase.GetAllDefsInDatabaseForDef(fishDefType))
		{
			ThingDef fishDef = (ThingDef)thingDefField.GetValue(def);
			var allowedBiomes = (List<string>)allowedBiomesField.GetValue(def);
			bool canBeFreshWater = (bool)canBeFreshWaterField.GetValue(def);
			bool canBeSaltWater = (bool)canBeSaltWaterField.GetValue(def);
			float commonality = (float)commonalityField.GetValue(def);
			int baseFishingYield = (int)baseFishingYieldField.GetValue(def);

			foreach (string biomeTempDefLabel in allowedBiomes)
			{
				Def biomeTempDef = biomeTempDefs.TryGetValue(biomeTempDefLabel);
				var biomes = (List<string>)biomesField.GetValue(biomeTempDef);
				foreach (string biomeDefName in biomes)
				{
					BiomeDef biomeDef = DefDatabase<BiomeDef>.GetNamed(biomeDefName);
					if (biomeDef != null)
					{
						if (canBeFreshWater)
						{
							FishingCompatibility.AddFishDef(biomeDef, WaterBodyType.Freshwater, fishDef, commonality,
								fishYield: baseFishingYield / FishingCompatibility.FishList.DefaultYieldModifier);
						}
						if (canBeSaltWater)
						{
							FishingCompatibility.AddFishDef(biomeDef, WaterBodyType.Saltwater, fishDef, commonality,
								fishYield: baseFishingYield / FishingCompatibility.FishList.DefaultYieldModifier);
						}
					}
				}
			}
		}
	}
}