using System.Collections.Generic;
using System.Linq;
using DevTools.Testing;
using Verse;

namespace Vehicles.Testing;

[TestFixture(TestType.Playing)]
internal sealed class Test_HotReload
{
	private const string HotReloadSuffix = "_HotReloadedThrowaway";

	private List<Def> mcpDefs;
	private List<IMaterialCacheTarget> cacheTargets;
	private List<VehicleDef> allVehicleDefs;

	[OneTimeSetUp]
	private void CacheCounts()
	{
		mcpDefs = [.. VehicleMod.content.AllDefs];
		cacheTargets = [.. RGBMaterialPool.AllCacheTargets];
		allVehicleDefs = [.. DefDatabase<VehicleDef>.AllDefsListForReading];
		PlayDataLoader.HotReloadDefs();
	}

	[OneTimeTearDown]
	private void ClearAll()
	{
		mcpDefs = null;
		cacheTargets = null;
	}

	[Test]
	private void ModContentPack()
	{
		// Verify the transient defs don't linger behind in the def lists
		foreach (Def def in VehicleMod.content.AllDefs)
			Expect.IsFalse(def.defName.Contains(HotReloadSuffix));
		foreach (VehicleDef def in DefDatabase<VehicleDef>.AllDefsListForReading)
			Expect.IsFalse(def.defName.Contains(HotReloadSuffix));

		Expect.AreEqual(mcpDefs.Count, VehicleMod.content.AllDefs.Count(),
			"ModContentPack Def Count");
		Expect.AreEqual(allVehicleDefs.Count,
			DefDatabase<VehicleDef>.AllDefsListForReading.Count,
			"DefDatabase Def Count");
	}

	[Test]
	private void MaterialPool()
	{
		Expect.AreEqual(CacheTargetCountPreHotReload(cacheTargets),
			CacheTargetCountPreHotReload(RGBMaterialPool.AllCacheTargets), "MaterialPool Targets");
		Expect.AreEqual(MaterialCountPreHotReload(cacheTargets),
			MaterialCountPreHotReload(RGBMaterialPool.AllCacheTargets),
			"Total Material Count");

		foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
		{
			if (vehicleDef.graphicData.shaderType.Shader.SupportsRGBMaskTex())
				Expect.IsTrue(cacheTargets.Contains(vehicleDef));
			if (vehicleDef.GetCompProperties<CompProperties_VehicleTurrets>() is { } props &&
				!props.turrets.NullOrEmpty())
			{
				foreach (VehicleTurret turret in props.turrets)
				{
					if (turret.NoGraphic)
						continue;

					if (turret.GraphicData.shaderType.Shader.SupportsRGBMaskTex())
						Expect.IsTrue(cacheTargets.Contains(turret));
					if (!turret.TurretGraphics.NullOrEmpty())
					{
						foreach (VehicleTurret.TurretDrawData drawData in turret.TurretGraphics)
						{
							if (drawData.graphicData.shaderType.Shader.SupportsRGBMaskTex())
								Expect.IsTrue(cacheTargets.Contains(drawData));
						}
					}
				}
			}
		}
		foreach (PatternDef patternDef in DefDatabase<PatternDef>.AllDefsListForReading)
		{
			Expect.IsTrue(cacheTargets.Contains(patternDef));
		}
		return;

		static int CacheTargetCountPreHotReload(List<IMaterialCacheTarget> cacheTargets)
		{
			int count = 0;
			foreach (IMaterialCacheTarget cacheTarget in cacheTargets)
			{
				if (cacheTarget is Def def && def.defName.EndsWith(HotReloadSuffix))
					continue;
				count++;
			}
			return count;
		}

		static int MaterialCountPreHotReload(List<IMaterialCacheTarget> cacheTargets)
		{
			int count = 0;
			foreach (IMaterialCacheTarget cacheTarget in cacheTargets)
			{
				if (cacheTarget is Def def && def.defName.EndsWith(HotReloadSuffix))
					continue;
				count += cacheTarget.MaterialCount;
			}
			return count;
		}
	}
}