using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;
using SmashTools.Animations;
using System.Linq;

namespace Vehicles
{
	public class GraphicOverlay : IAnimationObject, IMaterialCacheTarget
	{
		[TweakField]
		public GraphicDataOverlay data;

		private readonly VehiclePawn vehicle;
    // Specific to vehicle instance
    private readonly GraphicOverlayRenderer renderer;

    // Vehicle may be null, but overlays should always originate from a VehicleDef
    private readonly VehicleDef vehicleDef;
		
		private Graphic graphic;
		private Graphic_DynamicShadow graphicShadow;

		[AnimationProperty(Name = "Position")]
		private Vector3 position;
		[AnimationProperty(Name = "Rotation")]
		private float rotation;
    [AnimationProperty(Name = "Propeller Acceleration")]
    private float acceleration;

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
			this.renderer = vehicle.overlayRenderer;

			this.vehicle.AddEvent(VehicleEventDefOf.Destroyed, OnDestroy);

			if (data.dynamicShadows)
			{
				ShadowData shadowData = new ShadowData()
				{
					volume = new Vector3(data.graphicData.drawSize.x, 0, data.graphicData.drawSize.y),
					offset = new Vector3(data.graphicData.drawOffset.x, 0, data.graphicData.drawOffset.z + 5),
				};
				graphicShadow = new Graphic_DynamicShadow(data.graphicData.Graphic.TexAt(Rot8.North), shadowData);
			}
		}

		public int MaterialCount => vehicle?.MaterialCount ?? vehicleDef.MaterialCount;

		public PatternDef PatternDef => PatternDefOf.Default;

		public string Name => $"{vehicleDef.Name}_{data.graphicData.texPath}";

		string IAnimationObject.ObjectId => data.identifier ?? nameof(GraphicOverlay);

		public Graphic_DynamicShadow ShadowGraphic => graphicShadow;

		public Graphic Graphic
		{
			get
			{
				if (graphic is null)
				{
					if (vehicle != null && vehicle.Destroyed && !RGBMaterialPool.GetAll(this).NullOrEmpty())
					{
						Log.Error($@"Reinitializing RGB Materials but {this} has already been destroyed and the cache was not cleared for this entry. 
This may result in a memory leak.");
						RGBMaterialPool.Release(this);
					}
					PatternData patternData = vehicle?.patternData ?? VehicleMod.settings.vehicles.defaultGraphics
						.TryGetValue(vehicleDef.defName, new PatternData(vehicleDef.graphicData));
					GraphicDataRGB graphicData = new GraphicDataRGB();
					graphicData.CopyFrom(data.graphicData);
					
					if (graphicData.graphicClass.SameOrSubclass(typeof(Graphic_RGB)) && graphicData.shaderType.Shader
						.SupportsRGBMaskTex())
					{
						graphicData.color = patternData.color;
						graphicData.colorTwo = patternData.colorTwo;
						graphicData.colorThree = patternData.colorThree;
						graphicData.tiles = patternData.tiles;
						graphicData.displacement = patternData.displacement;
						graphicData.pattern = patternData.patternDef;

						RGBMaterialPool.CacheMaterialsFor(this);
						graphicData.Init(this);
						graphic = graphicData.Graphic;
						var graphicRGB = graphic as Graphic_RGB;
						RGBMaterialPool.SetProperties(this, patternData, graphicRGB.TexAt, graphicRGB.MaskAt);
					}
					else
					{
						graphic = ((GraphicData)graphicData).Graphic;
					}
				}
				return graphic;
			}
		}

		public void Draw(ref readonly TransformData transform)
		{
			Vector3 position = this.position + transform.position;
			float rotation = this.rotation + transform.rotation + data.rotation;
			
			if (data.component != null)
			{
				// TODO - Move to queue based system where overlays get pulled from a render pool
				float healthPercent = vehicle.statHandler.GetComponentHealthPercent(data.component.key);
				if (!data.component.comparison.Compare(healthPercent, data.component.healthPercent))
				{
					return; //Skip rendering if health percent is below set amount for rendering
				}
			}
			if (!data.graphicData.AboveBody)
			{
				position.y -= (VehicleRenderer.YOffset_Body + VehicleRenderer.SubInterval);
			}
			if (renderer != null && Graphic is Graphic_Rotator rotator)
			{
				if (acceleration != 0)
				{
          renderer.rotationRegistry[rotator.RegistryKey] += acceleration;
        }
				rotation = renderer.rotationRegistry[rotator.RegistryKey].ClampAndWrap(0, 359);
      }
			if (Graphic is Graphic_RGB graphicRGB)
			{
				graphicRGB.DrawWorker(position, transform.orientation, null, null, rotation);
			}
			else
			{
				Graphic.DrawWorker(position, transform.orientation, null, null, rotation);
			}
			ShadowGraphic?.DrawWorker(position, transform.orientation, null, null, rotation);
		}

		public void Notify_ColorChanged()
		{
			if (data.graphicData.shaderType.Shader.SupportsRGBMaskTex())
			{
				PatternData patternData = vehicle?.patternData ?? VehicleMod.settings.vehicles.defaultGraphics
					.TryGetValue(vehicleDef.defName, new PatternData(vehicleDef.graphicData));
				RGBMaterialPool.SetProperties(this, patternData);
				graphic = null;
			}
		}

		public void OnDestroy()
		{
			RGBMaterialPool.Release(this);
		}

		public static GraphicOverlay Create(GraphicDataOverlay graphicDataOverlay, VehiclePawn vehicle)
		{
			if (!UnityData.IsInMainThread)
			{
				Log.Error($"Trying to create GraphicOverlay outside of the main thread.");
				return null;
			}
			GraphicOverlay graphicOverlay = new GraphicOverlay(graphicDataOverlay, vehicle);
			graphicDataOverlay.graphicData.shaderType ??= ShaderTypeDefOf.Cutout;
			if (!VehicleMod.settings.main.useCustomShaders)
			{
				graphicDataOverlay.graphicData.shaderType = graphicDataOverlay.graphicData.shaderType.Shader
					.SupportsRGBMaskTex(ignoreSettings: true) ? ShaderTypeDefOf.CutoutComplex : 
					graphicDataOverlay.graphicData.shaderType;
			}
			if (graphicDataOverlay.graphicData.shaderType.Shader.SupportsRGBMaskTex())
			{
				RGBMaterialPool.CacheMaterialsFor(graphicOverlay);
				graphicDataOverlay.graphicData.Init(graphicOverlay);
				PatternData patternData = vehicle.patternData;
				RGBMaterialPool.SetProperties(graphicOverlay, patternData, (graphicOverlay.Graphic as Graphic_RGB)
					.TexAt, (graphicOverlay.Graphic as Graphic_RGB).MaskAt);
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
				graphicDataOverlay.graphicData.shaderType = graphicDataOverlay.graphicData.shaderType.Shader
					.SupportsRGBMaskTex(ignoreSettings: true) ? ShaderTypeDefOf.CutoutComplex : graphicDataOverlay.graphicData.shaderType;
			}
			if (graphicDataOverlay.graphicData.shaderType.Shader.SupportsRGBMaskTex())
			{
				RGBMaterialPool.CacheMaterialsFor(graphicOverlay);
				graphicDataOverlay.graphicData.Init(graphicOverlay);
				PatternData patternData = VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName, 
					new PatternData(vehicleDef.graphicData));
				RGBMaterialPool.SetProperties(graphicOverlay, patternData, (graphicOverlay.Graphic as Graphic_RGB).TexAt, 
					(graphicOverlay.Graphic as Graphic_RGB).MaskAt);
			}
			else
			{
				_ = graphicDataOverlay.graphicData.Graphic;
			}
			return graphicOverlay;
		}
	}
}
