using System;
using System.Collections.Generic;
using RimWorld;
using SmashTools;
using SmashTools.Animations;
using SmashTools.Rendering;
using UnityEngine;
using Vehicles.Rendering;
using Verse;
using Transform = SmashTools.Rendering.Transform;

namespace Vehicles;

public class GraphicOverlay : IAnimationObject, IMaterialCacheTarget,
                              IParallelRenderer, IBlitTarget, ITransformable,
                              ITweakFields
{
	public GraphicDataOverlay data;

	private readonly VehiclePawn vehicle;

	// Vehicle may be null, but overlays should always originate from a VehicleDef
	private readonly VehicleDef vehicleDef;

	private PreRenderResults results;

	[TweakField]
	private Graphic graphic;

	// ReSharper disable once FieldCanBeMadeReadOnly.Local
	private Graphic_DynamicShadow graphicShadow;

	[TweakField]
	[AnimationProperty(Name = "Transform")]
	private readonly Transform transform = new();

	[TweakField(SettingsType = UISettingsType.FloatBox)]
	[AnimationProperty(Name = "Propeller Acceleration")]
	internal float acceleration;

	private GraphicOverlay(GraphicDataOverlay graphicDataOverlay, VehicleDef vehicleDef)
	{
		data = graphicDataOverlay;
		this.vehicleDef = vehicleDef;
	}

	private GraphicOverlay(GraphicDataOverlay graphicDataOverlay, VehiclePawn vehicle)
	{
		data = graphicDataOverlay;
		this.vehicle = vehicle;
		this.vehicleDef = vehicle.VehicleDef;

		this.vehicle.AddEvent(VehicleEventDefOf.Destroyed, Destroy);

		if (data.dynamicShadows)
		{
			ShadowData shadowData = new()
			{
				volume = new Vector3(data.graphicData.drawSize.x, 0, data.graphicData.drawSize.y),
				offset = new Vector3(data.graphicData.drawOffset.x, 0, data.graphicData.drawOffset.z + 5),
			};
			graphicShadow =
				new Graphic_DynamicShadow(data.graphicData.Graphic.TexAt(Rot8.North), shadowData);
		}
	}

	public int MaterialCount => vehicle?.MaterialCount ?? vehicleDef.MaterialCount;

	public PatternDef PatternDef => PatternDefOf.Default;

	public string Name => $"{vehicleDef.Name}_{data.graphicData.texPath}";

	public MaterialPropertyBlock PropertyBlock { get; private set; }

	string IAnimationObject.ObjectId => data.identifier ?? nameof(GraphicOverlay);

	public Graphic_DynamicShadow ShadowGraphic => graphicShadow;

	public Transform Transform => transform;

	string ITweakFields.Category => "Graphic Overlay";

	string ITweakFields.Label => data.identifier ?? data.graphicData.texPath;

	bool IParallelRenderer.IsDirty { get; set; }

	public Graphic Graphic
	{
		get
		{
			if (graphic is null)
			{
				PropertyBlock ??= new MaterialPropertyBlock();
				if (vehicle is { Destroyed: true } && !RGBMaterialPool.GetAll(this).NullOrEmpty())
				{
					Log.Error(
						$"Reinitializing RGB Materials but {this} has already been destroyed and the cache " +
						$"was not cleared for this entry. This may result in a memory leak.");
					RGBMaterialPool.Release(this);
				}

				PatternData patternData = vehicle?.patternData ?? VehicleMod.settings.vehicles
				 .defaultGraphics
				 .TryGetValue(vehicleDef.defName, new PatternData(vehicleDef.graphicData));
				GraphicDataRGB graphicData = new();
				graphicData.CopyFrom(data.graphicData);

				if (graphicData.graphicClass.SameOrSubclass(typeof(Graphic_Rgb)) && graphicData.shaderType
				 .Shader
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
					Graphic_Rgb graphicRGB = (Graphic_Rgb)graphic;
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

	public void DynamicDrawPhaseAt(DrawPhase phase, in TransformData transformData,
		bool forceDraw = false)
	{
		switch (phase)
		{
			case DrawPhase.EnsureInitialized:
				// Ensure meshes are cached beforehand
				for (int i = 0; i < 4; i++)
					_ = Graphic.MeshAt(new Rot4(i));
			break;
			case DrawPhase.ParallelPreDraw:
				results = ParallelGetPreRenderResults(in transformData, forceDraw: forceDraw);
			break;
			case DrawPhase.Draw:
				// Out of phase drawing must immediately generate pre-render results for valid data.
				if (!results.valid)
					results = ParallelGetPreRenderResults(in transformData, forceDraw: forceDraw);
				Draw(in transformData);
				results = default;
			break;
			default:
				throw new NotImplementedException();
		}
	}

	private PreRenderResults ParallelGetPreRenderResults(ref readonly TransformData transformData,
		bool forceDraw = false)
	{
		if (data.component is { MeetsRequirements: false })
		{
			// Skip rendering if health percent is below set amount for rendering
			return new PreRenderResults { valid = true, draw = false };
		}

		if (Graphic is Graphic_Rgb graphicRgb)
		{
			float extraRotation = transform.rotation + data.rotation;
			PreRenderResults render =
				graphicRgb.ParallelGetPreRenderResults(in transformData, forceDraw: forceDraw,
					thing: vehicle, extraRotation: extraRotation);
			render.position += transform.position;
			return render;
		}
		return new PreRenderResults { valid = true, draw = true };
	}

	private void Draw(ref readonly TransformData transformData)
	{
		if (!results.draw)
			return;

		if (Graphic is Graphic_Rgb)
		{
			Graphics.DrawMesh(results.mesh, results.position, results.quaternion, results.material, 0);
		}
		else
		{
			Graphic.DrawWorker(transformData.position, transformData.orientation, null, null,
				transformData.rotation);
		}
	}

	private void Notify_ColorChanged()
	{
		if (data.graphicData.shaderType.Shader.SupportsRGBMaskTex())
		{
			PatternData patternData = vehicle?.patternData ?? VehicleMod.settings.vehicles
			 .defaultGraphics
			 .TryGetValue(vehicleDef.defName, new PatternData(vehicleDef.graphicData));
			RGBMaterialPool.SetProperties(this, patternData);
		}
	}

	public void Destroy()
	{
		vehicle.RemoveEvent(VehicleEventDefOf.ColorChanged, Notify_ColorChanged);
		RGBMaterialPool.Release(this);
	}

	public static GraphicOverlay Create(GraphicDataOverlay graphicDataOverlay, VehiclePawn vehicle)
	{
		if (!UnityData.IsInMainThread)
		{
			Log.Error("Trying to create GraphicOverlay outside of the main thread.");
			return null;
		}

		GraphicOverlay graphicOverlay = new(graphicDataOverlay, vehicle);
		graphicDataOverlay.graphicData.shaderType ??= ShaderTypeDefOf.Cutout;
		if (!VehicleMod.settings.main.useCustomShaders)
		{
			graphicDataOverlay.graphicData.shaderType = graphicDataOverlay.graphicData.shaderType.Shader
			 .SupportsRGBMaskTex(ignoreSettings: true) ?
				ShaderTypeDefOf.CutoutComplex :
				graphicDataOverlay.graphicData.shaderType;
		}

		if (graphicDataOverlay.graphicData.shaderType.Shader.SupportsRGBMaskTex())
		{
			RGBMaterialPool.CacheMaterialsFor(graphicOverlay);
			graphicDataOverlay.graphicData.Init(graphicOverlay);
			PatternData patternData = vehicle.patternData;
			Graphic_Rgb graphic = (Graphic_Rgb)graphicOverlay.Graphic;
			RGBMaterialPool.SetProperties(graphicOverlay, patternData, graphic.TexAt, graphic.MaskAt);
		}
		else
		{
			_ = graphicDataOverlay.graphicData.Graphic;
		}

		vehicle.AddEvent(VehicleEventDefOf.ColorChanged, graphicOverlay.Notify_ColorChanged);
		return graphicOverlay;
	}

	public static GraphicOverlay Create(GraphicDataOverlay graphicDataOverlay,
		VehicleDef vehicleDef)
	{
		GraphicOverlay graphicOverlay = new(graphicDataOverlay, vehicleDef);
		graphicDataOverlay.graphicData.shaderType ??= ShaderTypeDefOf.Cutout;
		if (!VehicleMod.settings.main.useCustomShaders)
		{
			graphicDataOverlay.graphicData.shaderType = graphicDataOverlay.graphicData.shaderType.Shader
			 .SupportsRGBMaskTex(ignoreSettings: true) ?
				ShaderTypeDefOf.CutoutComplex :
				graphicDataOverlay.graphicData.shaderType;
		}

		if (graphicDataOverlay.graphicData.shaderType.Shader.SupportsRGBMaskTex())
		{
			RGBMaterialPool.CacheMaterialsFor(graphicOverlay);
			graphicDataOverlay.graphicData.Init(graphicOverlay);
			PatternData patternData = VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(
				vehicleDef.defName,
				new PatternData(vehicleDef.graphicData));

			Graphic_Rgb graphic = (Graphic_Rgb)graphicOverlay.Graphic;
			RGBMaterialPool.SetProperties(graphicOverlay, patternData, graphic.TexAt, graphic.MaskAt);
		}
		else
		{
			_ = graphicDataOverlay.graphicData.Graphic;
		}
		return graphicOverlay;
	}

	(int width, int height) IBlitTarget.TextureSize(in BlitRequest request)
	{
		Texture texture = Graphic.MatAt(request.rot)?.mainTexture;
		return texture != null ? (texture.width, texture.height) : (0, 0);
	}

	IEnumerable<RenderData> IBlitTarget.GetRenderData(Rect rect, BlitRequest request)
	{
		Rect overlayRect = VehicleGraphics.OverlayRect(rect, vehicleDef, this, request.rot);
		bool canMask = Graphic.Shader.SupportsMaskTex() || Graphic.Shader.SupportsRGBMaskTex();

		Material material = null;
		Texture2D texture = Graphic.MatAt(request.rot).mainTexture as Texture2D;
		if (canMask)
		{
			material = Graphic.MatAt(request.rot);
			if (Graphic is Graphic_Rgb graphicRgb)
			{
				if (Graphic.Shader.SupportsRGBMaskTex())
					material = RGBMaterialPool.GetUi(this, request.rot);
				RGBMaterialPool.SetProperties(this, request.patternData, graphicRgb.TexAt, graphicRgb.MaskAt);
			}
			else
			{
				RGBMaterialPool.SetProperties(this, request.patternData,
					forRot => Graphic.MatAt(forRot).mainTexture as Texture2D,
					forRot => Graphic.MatAt(forRot).GetMaskTexture());
			}
		}
		// TODO - vehicleDef.PropertyBlock here would be incorrect for VehiclePawn instance rendering. Will
		// need a refactor later if and when I get to drawing all of this via material property blocks.
		RenderData overlayRenderData = new(overlayRect, texture, material, vehicleDef.PropertyBlock,
			data.graphicData.DrawOffsetFull(request.rot).y, data.rotation);
		yield return overlayRenderData;
	}

	void ITweakFields.OnFieldChanged()
	{
		this.SetDirty();
	}
}