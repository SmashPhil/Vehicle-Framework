using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;
using Verse;
using SmashTools;
using Object = UnityEngine.Object;
using RimWorld;
using RimWorld.Planet;
using Verse.AI;
using Verse.Noise;

namespace Vehicles
{
	public static class RGBMaterialPool
	{
		private static readonly Dictionary<IMaterialCacheTarget, Material[]> cache = new Dictionary<IMaterialCacheTarget, Material[]>();

		public static Material[] GetAll(IMaterialCacheTarget target)
		{
			if (cache.TryGetValue(target, out Material[] materials))
			{
				return materials;
			}
			return null;
		}

		public static Material Get(IMaterialCacheTarget target, Rot8 rot)
		{
			if (cache.TryGetValue(target, out Material[] materials))
			{
				if (rot.AsInt >= materials.Length)
				{
					Log.Error($"Attempting to fetch material out of bounds. Target={target} Rot8={rot}.  Max count for {target} is {target.MaterialCount}");
					return null;
				}
				return materials[rot.AsInt];
			}
			return null;
		}

		public static void CacheMaterialsFor(IMaterialCacheTarget target, int renderQueue = 0, List<ShaderParameter> shaderParameters = null)
		{
			CacheMaterialsFor(target, target.PatternDef, renderQueue: renderQueue, shaderParameters: shaderParameters);
		}

		public static void CacheMaterialsFor(IMaterialCacheTarget target, PatternDef patternDef, int renderQueue = 0, List<ShaderParameter> shaderParameters = null)
		{
			if (cache.ContainsKey(target) || patternDef == null)
			{
				return;
			}

			Material[] materials = new Material[target.MaterialCount];
			for (int i = 0; i < materials.Length; i++)
			{
				Rot8 rot = new Rot8(i);
				Material material = new Material(patternDef.ShaderTypeDef.Shader)
				{
					name = target.Name + rot.ToStringNamed(),
					mainTexture = null,
					color = Color.clear,
				};

				if (renderQueue != 0)
				{
					material.renderQueue = renderQueue;
				}
				if (!shaderParameters.NullOrEmpty())
				{
					for (int p = 0; p < shaderParameters.Count; p++)
					{
						shaderParameters[p].Apply(material);
					}
				}

				materials[i] = material;
			}
			cache.Add(target, materials);
		}

		public static bool SetProperties(IMaterialCacheTarget target, PatternData patternData, Func<Rot8, Texture2D> mainTexGetter = null, Func<Rot8, Texture2D> maskTexGetter = null)
		{
			if (!cache.TryGetValue(target, out Material[] materials))
			{
				Log.Error($"Materials for {target} have not been created. Out of sequence material editing.");
				return false;
			}
			for (int i = 0; i < materials.Length; i++)
			{
				Material material = materials[i];

				material.SetColor(AdditionalShaderPropertyIDs.ColorOne, patternData.color);
				material.SetColor(ShaderPropertyIDs.ColorTwo, patternData.colorTwo);
				material.SetColor(AdditionalShaderPropertyIDs.ColorThree, patternData.colorThree);

				Rot8 rot = new Rot8(i);
				Texture2D mainTex = material.mainTexture as Texture2D;
				if (mainTexGetter != null)
				{
					mainTex = mainTexGetter(rot);
				}
				Texture2D maskTex = maskTexGetter?.Invoke(rot);
				if (patternData.patternDef != PatternDefOf.Default)
				{
					float tiles = patternData.tiles;
					if (patternData.patternDef.properties.tiles.TryGetValue("All", out float allTiles))
					{
						tiles *= allTiles;
					}
					if (tiles != 0)
					{
						material.SetFloat(AdditionalShaderPropertyIDs.TileNum, tiles);
					}
					//Add case for vehicle defName
					if (patternData.patternDef.properties.equalize)
					{
						float scaleX = 1;
						float scaleY = 1;
						if (mainTex.width > mainTex.height)
						{
							scaleY = (float)mainTex.height / mainTex.width;
						}
						else
						{
							scaleX = (float)mainTex.width / mainTex.height;
						}
						material.SetFloat(AdditionalShaderPropertyIDs.ScaleX, scaleX);
						material.SetFloat(AdditionalShaderPropertyIDs.ScaleY, scaleY);
					}
					if (patternData.patternDef.properties.dynamicTiling)
					{
						material.SetFloat(AdditionalShaderPropertyIDs.DisplacementX, patternData.displacement.x);
						material.SetFloat(AdditionalShaderPropertyIDs.DisplacementY, patternData.displacement.y);
					}
				}

				if (patternData.patternDef.ShaderTypeDef.Shader != material.shader)
				{
					material.shader = patternData.patternDef.ShaderTypeDef.Shader;
				}

				Texture2D patternTex = patternData.patternDef[rot];
				if (patternData.patternDef.ShaderTypeDef == RGBShaderTypeDefOf.CutoutComplexSkin)
				{
					//Null reverts to original tex. Default would calculate to red
					material.SetTexture(AdditionalShaderPropertyIDs.SkinTex, patternTex);
				}
				else if (patternData.patternDef.ShaderTypeDef == RGBShaderTypeDefOf.CutoutComplexPattern)
				{
					//Default to full red mask for full ColorOne pattern
					material.SetTexture(AdditionalShaderPropertyIDs.PatternTex, patternTex);
				}

				material.mainTexture = mainTex;
				if (maskTex != null)
				{
					material.SetTexture(ShaderPropertyIDs.MaskTex, maskTex);
				}

				material.SetColor(AdditionalShaderPropertyIDs.ColorOne, patternData.color);
				material.SetColor(ShaderPropertyIDs.ColorTwo, patternData.colorTwo);
				material.SetColor(AdditionalShaderPropertyIDs.ColorThree, patternData.colorThree);
			}
			return true;
		}

		public static void Release(IMaterialCacheTarget target)
		{
			if (cache.TryGetValue(target, out Material[] materials))
			{
				for (int i = 0; i < materials.Length; i++)
				{
					Object.Destroy(materials[i]);
				}

				cache.Remove(target);
				GraphicDatabaseRGB.Remove(target);
				Debug.Message($"<success>{VehicleHarmony.LogLabel}</success> Removed {target} from RGBMaterialPool and cleared all entries.");
			}
		}

		internal static void LogAllMaterials()
		{
			StringBuilder report = new StringBuilder();
			report.AppendLine($"----- Outputting Cache (Targets={cache.Count} Total={cache.Values.Sum(arr => arr.Length)}) -----");
			report.AppendLine($"Vanilla Material Count: {((Dictionary<Material, MaterialRequest>)AccessTools.Field(typeof(MaterialPool), "matDictionaryReverse").GetValue(null)).Count}");
			foreach ((IMaterialCacheTarget target, Material[] materials) in cache)
			{
				report.AppendLine($"Target={target} Materials=\n{string.Join("\n", materials.Select(material => material.name))}");
			}
			report.AppendLine($"----- End of Cache Output -----");

			Log.Message(report.ToString());
		}

		[StartupAction(Category = "Performance", Name = "Material Memory Management", GameState = GameState.Playing)]
		private static void UnitTest_MaterialMemoryManagement()
		{
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				CoroutineManager.QueueInvoke(TestVehicleMaterialCaching);
			});
		}

		private static IEnumerator TestVehicleMaterialCaching()
		{
			Log.Message($"------ Running MaterialMemoryManagement Test ------ ");
			
			Map map = Find.CurrentMap ?? Find.Maps.FirstOrDefault(map => map.Parent is Settlement settlement && settlement.Faction == Faction.OfPlayerSilentFail);
			if (map == null)
			{
				Log.Error($"Unable to conduct material memory management unit test. Null map.");
				yield break;
			}

			MaterialTesting materialTest = new MaterialTesting();

			IntVec3 cell = map.Center;

			materialTest.Start("Defs");
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefs)
			{
				materialTest.Start(vehicleDef.defName);
				{
					ClearAreaSpawnTerrain(vehicleDef, map, cell);

					VehiclePawn generatedVehicle = VehicleSpawner.GenerateVehicle(vehicleDef, Faction.OfPlayer);
					VehiclePawn vehicle = (VehiclePawn)GenSpawn.Spawn(generatedVehicle, cell, map, Rot8.North, WipeMode.FullRefund, false);
					_ = vehicle.VehicleGraphic; //allow graphic to be cached before any upgrade calls
					
					if (vehicle.CompUpgradeTree != null)
					{
						foreach (UpgradeNode node in vehicle.CompUpgradeTree.Props.def.nodes)
						{
							materialTest.Start($"{vehicleDef.defName} {vehicle.CompUpgradeTree.Props.def}->{node.key}");
							{
								vehicle.CompUpgradeTree.FinishUnlock(node);
								yield return null; //1 frame for rendering
								vehicle.CompUpgradeTree.ResetUnlock(node);
							}
							materialTest.Stop();
						}
					}
					vehicle.Destroy();
				}
				materialTest.Stop();
				yield return null;
			}
			materialTest.Stop();
			
			Log.Message($"------ Report Complete ------ ");

			if (materialTest.OngoingTests)
			{
				Log.Error($"Finished MaterialCache Report but material testing is still ongoing.");
			}
			
		}

		private static void ClearAreaSpawnTerrain(VehicleDef vehicleDef, Map map, IntVec3 cell)
		{
			TerrainDef validTerrain = DefDatabase<TerrainDef>.AllDefsListForReading.Where(terrainDef => TerrainCostFor(vehicleDef, terrainDef) < VehiclePathGrid.ImpassableCost).RandomElement();

			try
			{
				IntVec2 sizeNeeded = vehicleDef.Size;
				for (int x = -sizeNeeded.x; x < sizeNeeded.x; x++)
				{
					for (int z = -sizeNeeded.z; z < sizeNeeded.z; z++)
					{
						IntVec3 rectCell = new IntVec3(cell.x + x, 0, cell.z + z);
						List<Thing> thingList = map.thingGrid.ThingsListAtFast(rectCell).ToList();
						foreach (Thing thing in thingList)
						{
							thing.Destroy();
						}
						map.terrainGrid.SetTerrain(rectCell, validTerrain);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Exception thrown clearing area for {vehicleDef}.\nException={ex}");
			}
		}

		private static int TerrainCostFor(VehicleDef vehicleDef, TerrainDef terrainDef)
		{
			if (vehicleDef.properties.customTerrainCosts.TryGetValue(terrainDef, out int pathCost))
			{
				return pathCost;
			}
			return terrainDef.pathCost;
		}

		private class MaterialTesting
		{
			private StringBuilder reportBuilder = new StringBuilder();
			private Stack<TestCase> ongoingTests = new Stack<TestCase>();

			public bool OngoingTests => ongoingTests.Count > 0;

			public void Start(string label)
			{
				int targets = cache.Count;
				int materials = cache.Values.Sum(arr => arr.Length);

				this.ongoingTests.Push(new TestCase(label, targets, materials));
			}

			public void Stop()
			{
				reportBuilder.Clear();
				int targetsAfter = cache.Count;
				int materialsAfter = cache.Values.Sum(arr => arr.Length);

				TestCase testCase = ongoingTests.Pop();

				bool targetResult = testCase.targets == targetsAfter;
				bool materialsResult = testCase.materials == materialsAfter;

				reportBuilder.AppendLine($"[<property>{testCase.label}</property>]: Targets={ResultString(targetResult)} Materials={ResultString(materialsResult)}");
				reportBuilder.AppendInNewLine($"Targets: {testCase.targets}->{targetsAfter} Materials: {testCase.materials}->{materialsAfter}");

				SmashLog.Message(reportBuilder.ToString());
				reportBuilder.Clear();
			}

			private string ResultString(bool value)
			{
				if (value)
				{
					return $"<success>True</success>";
				}
				return $"<error>False</error>";
			}

			private class TestCase
			{
				public string label;
				public int targets;
				public int materials;

				public TestCase(string label, int targets, int materials)
				{
					this.label = label;
					this.targets = targets;
					this.materials = materials;
				}
			}
		}
	}
}
