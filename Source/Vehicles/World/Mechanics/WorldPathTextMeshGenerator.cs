using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using TMPro;

namespace Vehicles
{
	public static class WorldPathTextMeshGenerator
	{
		public static bool textObjectsGenerated = false;
		public static List<WorldFeatureTextMesh_TextMeshPro> worldPathTexts = new List<WorldFeatureTextMesh_TextMeshPro>();

		public static void GenerateTextMeshObjects()
		{
			DestroyTextMeshObjects();
			for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
			{
				WorldFeatureTextMesh_TextMeshPro feature = new WorldFeatureTextMesh_TextMeshPro();
				feature.Init();
				feature.Text = "0";
				feature.Size = Find.WorldGrid.averageTileSize;
				Vector3 normalized = Find.WorldGrid.GetTileCenter(i).normalized;
				Quaternion quaternion = Quaternion.LookRotation(Vector3.Cross(normalized, Vector3.up), normalized);
				quaternion *= Quaternion.Euler(Vector3.right * 90f);
				quaternion *= Quaternion.Euler(Vector3.forward * 90f);
				feature.Rotation = quaternion;
				feature.LocalPosition = Find.WorldGrid.GetTileCenter(i);
				feature.WrapAroundPlanetSurface();
				feature.SetActive(false);
				worldPathTexts.Add(feature);
			}
		}

		public static void UpdateVisibility()
		{
			if (WorldRendererUtility.WorldRenderedNow)
			{
				int tile = GenWorld.TileAt(Verse.UI.MousePositionOnUI);
				if (tile >= 0)
				{
					Log.Message($"TILE: {tile} COST: {worldPathTexts[tile].Text}");
				}
			}
			foreach (var textMesh in worldPathTexts)
			{
				bool visibleOnWorld = !WorldRendererUtility.HiddenBehindTerrainNow(textMesh.Position);
				if (visibleOnWorld != textMesh.Active)
				{
					textMesh.SetActive(visibleOnWorld);
					textMesh.WrapAroundPlanetSurface();
				}
			}
		}
		
		public static void UpdateTextMeshObjectsFor(List<VehiclePawn> vehiclePawns)
		{
			List<VehicleDef> vehicleDefs = vehiclePawns.Select(v => v.VehicleDef).Distinct().ToList();
			UpdateTextMeshObjectsFor(vehicleDefs);
		}

		public static void UpdateTextMeshObjectsFor(List<VehicleDef> vehicleDefs)
		{
			for (int i = 0; i < worldPathTexts.Count; i++)
			{
				var textMesh = worldPathTexts[i];
				Tile tile = Find.WorldGrid[i];
				BiomeDef biome = tile.biome;
				float biomeCost = vehicleDefs.Max(v => v.properties.customBiomeCosts.TryGetValue(biome, Find.WorldGrid[i].biome.movementDifficulty));
				float hillinessCost = vehicleDefs.Max(v => v.properties.customHillinessCosts.TryGetValue(tile.hilliness, WorldVehiclePathGrid.HillinessMovementDifficultyOffset(tile.hilliness)));
				float roadMultiplier = VehicleCaravan_PathFollower.GetRoadMovementDifficultyMultiplier(vehicleDefs, i, -1);
				textMesh.Text = $"{(biomeCost + hillinessCost) * roadMultiplier}";
				textMesh.SetActive(true);
			}
		}

		public static void ResetAllTextMeshObjects()
		{
			for (int i = 0; i < worldPathTexts.Count; i++)
			{
				var textMesh = worldPathTexts[i];
				textMesh.Text = "0";
				textMesh.SetActive(false);
			}
		}

		public static void DestroyTextMeshObjects()
		{
			worldPathTexts.ForEach(t => t.Destroy());
			worldPathTexts.Clear();
		}
	}
}
