using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class GraphicOverlay : IMaterialCacheTarget
	{
		[TweakField]
		public GraphicDataOverlay data;

		private readonly VehicleDef vehicleDef;
		private readonly VehiclePawn vehicle;

		private Graphic graphicInt;

		public GraphicOverlay(GraphicDataOverlay graphicDataOverlay, VehicleDef vehicleDef)
		{
			data = graphicDataOverlay;
			this.vehicleDef = vehicleDef;
		}

		public GraphicOverlay(GraphicDataOverlay graphicDataOverlay, VehiclePawn vehicle)
		{
			data = graphicDataOverlay;
			this.vehicle = vehicle;
			this.vehicleDef = vehicle.VehicleDef;

			this.vehicle.AddEvent(VehicleEventDefOf.Destroyed, OnDestroy);
		}

		public int MaterialCount => vehicle?.MaterialCount ?? vehicleDef.MaterialCount;

		public PatternDef PatternDef => PatternDefOf.Default;

		public string Name => $"{vehicleDef.Name}_{data.graphicData.texPath}";

		public Graphic Graphic
		{
			get
			{
				if (graphicInt is null)
				{
					if (vehicle != null && vehicle.Destroyed && !RGBMaterialPool.GetAll(this).NullOrEmpty())
					{
						Log.Error($"Reinitializing RGB Materials but {this} has already been destroyed and the cache was not cleared for this entry. This may result in a memory leak.");
						RGBMaterialPool.Release(this);
					}
					PatternData patternData = vehicle?.patternData ?? VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName, new PatternData(vehicleDef.graphicData));
					GraphicDataRGB graphicData = new GraphicDataRGB();
					graphicData.CopyFrom(data.graphicData);
					
					if (graphicData.graphicClass.SameOrSubclass(typeof(Graphic_RGB)) && graphicData.shaderType.Shader.SupportsRGBMaskTex())
					{
						graphicData.color = patternData.color;
						graphicData.colorTwo = patternData.colorTwo;
						graphicData.colorThree = patternData.colorThree;
						graphicData.tiles = patternData.tiles;
						graphicData.displacement = patternData.displacement;
						graphicData.pattern = patternData.patternDef;

						RGBMaterialPool.CacheMaterialsFor(this);
						graphicData.Init(this);
						graphicInt = graphicData.Graphic;
						var graphicRGB = graphicInt as Graphic_RGB;
						RGBMaterialPool.SetProperties(this, patternData, graphicRGB.TexAt, graphicRGB.MaskAt);
					}
					else
					{
						graphicInt = ((GraphicData)graphicData).Graphic;
					}
				}
				return graphicInt;
			}
		}

		public void Notify_ColorChanged()
		{
			if (data.graphicData.shaderType.Shader.SupportsRGBMaskTex())
			{
				PatternData patternData = vehicle?.patternData ?? VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName, new PatternData(vehicleDef.graphicData));
				RGBMaterialPool.SetProperties(this, patternData);
				graphicInt = null;
			}
		}

		public void OnDestroy()
		{
			RGBMaterialPool.Release(this);
		}

		public static GraphicOverlay Create(GraphicDataOverlay graphicDataOverlay, VehiclePawn vehicle)
		{
			GraphicOverlay graphicOverlay = new GraphicOverlay(graphicDataOverlay, vehicle);
			graphicDataOverlay.graphicData.shaderType ??= ShaderTypeDefOf.Cutout;
			if (!VehicleMod.settings.main.useCustomShaders)
			{
				graphicDataOverlay.graphicData.shaderType = graphicDataOverlay.graphicData.shaderType.Shader.SupportsRGBMaskTex(ignoreSettings: true) ? ShaderTypeDefOf.CutoutComplex : graphicDataOverlay.graphicData.shaderType;
			}
			if (graphicDataOverlay.graphicData.shaderType.Shader.SupportsRGBMaskTex())
			{
				RGBMaterialPool.CacheMaterialsFor(graphicOverlay);
				graphicDataOverlay.graphicData.Init(graphicOverlay);
				PatternData patternData = vehicle.patternData;
				RGBMaterialPool.SetProperties(graphicOverlay, patternData, (graphicOverlay.Graphic as Graphic_RGB).TexAt, (graphicOverlay.Graphic as Graphic_RGB).MaskAt);
			}
			else
			{
				_ = graphicDataOverlay.graphicData.Graphic;
			}
			return graphicOverlay;
		}

		public static GraphicOverlay Create(GraphicDataOverlay graphicDataOverlay, VehicleDef vehicleDef)
		{
			GraphicOverlay graphicOverlay = new GraphicOverlay(graphicDataOverlay, vehicleDef);
			graphicDataOverlay.graphicData.shaderType ??= ShaderTypeDefOf.Cutout;
			if (!VehicleMod.settings.main.useCustomShaders)
			{
				graphicDataOverlay.graphicData.shaderType = graphicDataOverlay.graphicData.shaderType.Shader.SupportsRGBMaskTex(ignoreSettings: true) ? ShaderTypeDefOf.CutoutComplex : graphicDataOverlay.graphicData.shaderType;
			}
			if (graphicDataOverlay.graphicData.shaderType.Shader.SupportsRGBMaskTex())
			{
				RGBMaterialPool.CacheMaterialsFor(graphicOverlay);
				graphicDataOverlay.graphicData.Init(graphicOverlay);
				PatternData patternData = VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName, new PatternData(vehicleDef.graphicData));
				RGBMaterialPool.SetProperties(graphicOverlay, patternData, (graphicOverlay.Graphic as Graphic_RGB).TexAt, (graphicOverlay.Graphic as Graphic_RGB).MaskAt);
			}
			else
			{
				_ = graphicDataOverlay.graphicData.Graphic;
			}
			return graphicOverlay;
		}
	}
}
