using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using SmashTools;
using SmashTools.Animations;
using SmashTools.Performance;
using SmashTools.Rendering;
using UnityEngine;
using UnityEngine.Assertions;
using Vehicles.Rendering;
using Verse;
using Verse.Sound;
using Transform = SmashTools.Rendering.Transform;

namespace Vehicles;

[StaticConstructorOnStartup]
public partial class VehicleTurret
{
	/* --- Parsed --- */

	[TweakField]
	public VehicleTurretRender renderProperties = new();

	[TweakField(SettingsType = UISettingsType.FloatBox)]
	public Vector2 aimPieOffset = Vector2.zero;

	[TweakField(SettingsType = UISettingsType.IntegerBox)]
	public int drawLayer = 1;

	public string gizmoLabel;

	/* ----------------- */

	[TweakField]
	[AnimationProperty(Name = "Transform")]
	private Transform transform = new();

	[Unsaved]
	private PreRenderResults results;

	[Unsaved]
	private List<PreRenderResults> subGraphicResults = [];

	// Cache all root draw pos on spawn
	[Unsaved]
	private Vector3 rootDrawPosNorth;

	[Unsaved]
	private Vector3 rootDrawPosEast;

	[Unsaved]
	private Vector3 rootDrawPosSouth;

	[Unsaved]
	private Vector3 rootDrawPosWest;

	[Unsaved]
	private Vector3 rootDrawPosNorthEast;

	[Unsaved]
	private Vector3 rootDrawPosSouthEast;

	[Unsaved]
	private Vector3 rootDrawPosSouthWest;

	[Unsaved]
	private Vector3 rootDrawPosNorthWest;

	[Unsaved]
	public Texture2D currentFireIcon;

	[Unsaved]
	private Texture2D gizmoIcon;

	[Unsaved]
	private Texture2D mainMaskTex;

	[Unsaved]
	private Texture2D cachedTexture;

	[Unsaved]
	private Material cachedMaterial;

	[Unsaved]
	private Graphic_Turret cachedGraphic;

	[Unsaved]
	private GraphicDataRGB cachedGraphicData;

	[Unsaved]
	private List<TurretDrawData> turretGraphics;

	[Unsaved]
	private RotatingList<Texture2D> overheatIcons;

	[Unsaved]
	private bool selfDirty = true;

	bool IParallelRenderer.IsDirty
	{
		get { return selfDirty; }
		set
		{
			selfDirty = value;
			// Propagate dirty flag upward so top parent can initiate an EnsuredInitialized DrawPhase
			// that gets called back down to the child turret that needs dirtying.
			attachedTo?.SetDirty();
		}
	}

	public bool GizmoHighlighted { get; set; }

	public MaterialPropertyBlock PropertyBlock { get; private set; }

	// Need recursive parent check for nested turret attachment
	public bool ShouldDraw => (component is null || component.MeetsRequirements) &&
		(attachedTo is null || attachedTo.ShouldDraw);

	public int MaterialCount => 1;

	public string Name => $"{def}_{key}_{vehicle?.ThingID ?? "Def"}";

	public bool NoGraphic => def.graphicData is null;

	public float DrawLayerOffset => drawLayer * (Altitudes.AltInc / GraphicDataLayered.SubLayerCount);

	public Transform Transform => transform;

	public PatternDef PatternDef
	{
		get
		{
			if (NoGraphic)
			{
				return PatternDefOf.Default;
			}

			if (vehicle == null)
			{
				return VehicleMod.settings.vehicles.defaultGraphics
					 .TryGetValue(vehicleDef.defName, vehicleDef.graphicData)?.patternDef ??
					PatternDefOf.Default;
			}

			if (def.matchParentColor)
			{
				return vehicle?.PatternDef ?? PatternDefOf.Default;
			}

			return GraphicData.pattern;
		}
	}

	public Texture2D FireIcon
	{
		get
		{
			if (Find.TickManager.TicksGame % TicksPerOverheatingFrame == 0)
			{
				currentFireIcon = OverheatIcons.Next;
			}
			return currentFireIcon;
		}
	}

	protected RotatingList<Texture2D> OverheatIcons
	{
		get
		{
			if (overheatIcons.NullOrEmpty())
			{
				overheatIcons = TexData.FireIcons.ToRotatingList();
			}
			return overheatIcons;
		}
	}

	public virtual Material Material
	{
		get
		{
			if (!cachedMaterial)
				ResolveGraphics(vehicle);
			return cachedMaterial;
		}
	}

	public virtual Texture2D Texture
	{
		get
		{
			if (GraphicData.texPath.NullOrEmpty())
				return null;
			cachedTexture ??= ContentFinder<Texture2D>.Get(GraphicData.texPath);
			return cachedTexture;
		}
	}

	public virtual Texture2D MainMaskTexture
	{
		get
		{
			if (GraphicData.texPath.NullOrEmpty())
				return null;

			mainMaskTex ??=
				ContentFinder<Texture2D>.Get(GraphicData.texPath + Graphic_Turret.TurretMaskSuffix);
			return mainMaskTex;
		}
	}

	public virtual GraphicDataRGB GraphicData
	{
		get
		{
			if (cachedGraphicData is null)
				ResolveGraphics(vehicle);
			return cachedGraphicData;
		}
	}

	public virtual Graphic_Turret Graphic
	{
		get
		{
			if (cachedGraphic is null)
				ResolveGraphics(vehicle);
			return cachedGraphic;
		}
	}

	public virtual List<TurretDrawData> TurretGraphics
	{
		get { return turretGraphics; }
	}

	public virtual Texture2D GizmoIcon
	{
		get
		{
			if (gizmoIcon is null)
			{
				if (!string.IsNullOrEmpty(def.gizmoIconTexPath))
				{
					gizmoIcon = ContentFinder<Texture2D>.Get(def.gizmoIconTexPath);
				}
				else if (NoGraphic)
				{
					gizmoIcon = BaseContent.BadTex;
				}
				else
				{
					gizmoIcon = Texture ?? BaseContent.BadTex;
				}
			}

			return gizmoIcon;
		}
	}

	/// <summary>
	/// Smaller actions inside VehicleTurret gizmo
	/// </summary>
	public virtual IEnumerable<SubGizmo> SubGizmos
	{
		get
		{
			if (def.magazineCapacity > 0)
			{
				if (def.ammunition != null)
					yield return SubGizmo_RemoveAmmo(this);

				yield return SubGizmo_ReloadFromInventory(this);
			}

			yield return SubGizmo_FireMode(this);

			if (autoTargeting)
				yield return SubGizmo_AutoTarget(this);
		}
	}

	public virtual void DynamicDrawPhaseAt(DrawPhase phase, in TransformData transformData,
		bool forceDraw = false)
	{
		if (NoGraphic)
			return;

		switch (phase)
		{
			case DrawPhase.EnsureInitialized:
				// Ensure meshes are created and cached
				for (int i = 0; i < 4; i++)
					_ = Graphic.MeshAt(new Rot4(i));
				if (!TurretGraphics.NullOrEmpty())
				{
					foreach (TurretDrawData drawData in TurretGraphics)
					{
						for (int i = 0; i < 4; i++)
							drawData.graphic.MeshAt(new Rot4(i));
					}
				}

				if (!childTurrets.NullOrEmpty())
				{
					foreach (VehicleTurret turret in childTurrets)
						turret.DynamicDrawPhaseAt(phase, in transformData, forceDraw: forceDraw);
				}
			break;
			case DrawPhase.ParallelPreDraw:
				ParallelPreRenderResultsRecursive(in transformData, TurretRotation, 0,
					forceDraw: forceDraw);
			break;
			case DrawPhase.Draw:
				if (!results.valid)
				{
					Assert.IsTrue(subGraphicResults.Count == 0);
					float fixedRotation = defaultAngleRotated + transformData.orientation.AsAngle;
					ParallelPreRenderResultsRecursive(in transformData, fixedRotation, 0,
						forceDraw: forceDraw);
				}
				Draw();
				results = default;
				subGraphicResults.Clear();
			break;
			default:
				throw new NotImplementedException(nameof(DrawPhase));
		}
	}

	private void ParallelPreRenderResultsRecursive(ref readonly TransformData transformData,
		float rotation, float parentRotation, bool forceDraw = false)
	{
		results = ParallelPreRenderResults(in transformData, rotation, parentRotation,
			forceDraw: forceDraw);
		AddSubGraphicParallelPreRenderResults(in transformData, subGraphicResults, rotation,
			parentRotation);

		// Recursively draws child turrets with parent render results
		if (!childTurrets.NullOrEmpty())
		{
			foreach (VehicleTurret turret in childTurrets)
			{
				turret.ParallelPreRenderResultsRecursive(in transformData,
					turret.TurretRotation,
					rotation,
					forceDraw: forceDraw);
			}
		}
	}

	protected virtual PreRenderResults ParallelPreRenderResults(
		ref readonly TransformData transformData, float rotation, float parentRotation,
		bool forceDraw = false)
	{
		if (NoGraphic || !ShouldDraw && !forceDraw)
		{
			// Skip rendering if health percent is below set amount for rendering
			return new PreRenderResults { valid = true, draw = false };
		}

		PreRenderResults render = new()
		{
			valid = true,
			draw = true,
		};

		// This is more or less the same implementation as Graphic_Rgb::ParallelGetPreRenderResults
		// The fixed North orientation, turret rotation, and additional offsetting makes it more
		// trouble than its worth to try and fetch then modify. May want to refactor in the future.
		float turretRotation = transformData.rotation + rotation;
		float pivotRotation = transformData.rotation + parentRotation;
		//Vector3 rootPos = transformData.position +
		//  TurretDrawLocFor(transformData.orientation, pivotRotation);
		Vector3 rootPos =
			transformData.position + DrawPosition(transformData.orientation, pivotRotation);
		if (vehicle is { Spawned: true } && recoilTracker is { Recoil: > 0f })
		{
			rootPos += Vector3.zero.PointFromAngle(recoilTracker.Recoil, recoilTracker.Angle);
		}
		render.position = rootPos;
		if (vehicle is { Spawned: true } && def.graphicData.altLayerSpawned is { } altLayerSpawned)
		{
			render.position.y = altLayerSpawned.AltitudeFor();
			render.position.y += Graphic.DrawOffset(Rot4.North).y;
		}
		render.quaternion = turretRotation.ToQuat();
		render.mesh = Graphic.MeshAt(transformData.orientation);
		render.material = Material;
		return render;
	}

	protected virtual void AddSubGraphicParallelPreRenderResults(
		ref readonly TransformData transformData, List<PreRenderResults> outList, float rotation,
		float parentRotation)
	{
		if (results is { valid: true, draw: false })
		{
			// Rendering will be skipped if base pre-render results has draw = false. No point in
			// evaluating further, the main turret body and all subgraphics will not be drawn.
			return;
		}

		if (TurretGraphics.NullOrEmpty())
			return;

		for (int i = 0; i < TurretGraphics.Count; i++)
		{
			PreRenderResults render = new()
			{
				valid = true,
				draw = true
			};

			TurretDrawData turretDrawData = TurretGraphics[i];
			Turret_RecoilTracker subRecoilTracker = recoilTrackers[i];
			float turretRotation = transformData.rotation + rotation;
			float pivotRotation = transformData.rotation + parentRotation;
			// This is more or less the same implementation as Graphic_Rgb::ParallelGetPreRenderResults
			// The fixed North orientation, turret rotation, and additional offsetting makes it more
			// trouble than its worth to try and fetch then modify.
			Vector3 rootPos = transformData.position +
				turretDrawData.DrawOffset(transformData.orientation, turretRotation, pivotRotation);
			Vector3 recoilOffset = Vector3.zero;
			Vector3 parentRecoilOffset = Vector3.zero;
			if (subRecoilTracker is { Recoil: > 0f })
			{
				recoilOffset = Vector3.zero.PointFromAngle(subRecoilTracker.Recoil,
					subRecoilTracker.Angle);
			}
			if (attachedTo?.recoilTracker is { Recoil: > 0f })
			{
				parentRecoilOffset = Vector3.zero.PointFromAngle(attachedTo.recoilTracker.Recoil,
					attachedTo.recoilTracker.Angle);
			}
			render.position = rootPos + recoilOffset + parentRecoilOffset;
			if (vehicle is { Spawned: true } && turretDrawData.graphicData.altLayerSpawned is
				{ } altLayerSpawned)
			{
				render.position.y = altLayerSpawned.AltitudeFor();
				render.position.y += Graphic.DrawOffset(Rot4.North).y;
			}
			if (vehicle.Transform is not null)
			{
				Transform vehicleTransform = vehicle.Transform;
				render.position += vehicleTransform.position;
				//rotation += vehicleTransform.rotation;
			}
			render.quaternion = turretRotation.ToQuat();
			render.mesh = turretDrawData.graphic.MeshAt(transformData.orientation);
			render.material = turretDrawData.graphic.MatAt(Rot4.North);

			outList.Add(render);
		}
	}

	protected virtual void Draw()
	{
		if (!results.draw)
			return;

		Graphics.DrawMesh(results.mesh, results.position, results.quaternion, results.material, 0);

		if (subGraphicResults != null)
		{
			foreach (PreRenderResults renderResults in subGraphicResults)
			{
				Graphics.DrawMesh(renderResults.mesh, renderResults.position, renderResults.quaternion,
					renderResults.material, 0);
			}
		}

		if (!childTurrets.NullOrEmpty())
		{
			foreach (VehicleTurret childTurret in childTurrets)
				childTurret.Draw();
		}

		if (vehicle.Spawned)
		{
			DrawTargeter();
			DrawAimPie();
		}
	}

	public Vector3 DrawPosition(Rot8 rot)
	{
		Rot8 offsetRot = rot;
		if (attachedTo != null)
			offsetRot = Rot8.North;
		Vector3 graphicOffset = Graphic?.DrawOffset(offsetRot) ?? Vector3.zero;
		Vector2 propsOffset = renderProperties.OffsetFor(offsetRot);
		Vector2 offset = new(graphicOffset.x + propsOffset.x, graphicOffset.z + propsOffset.y);

		float rotation = InheritedRotation(this);
		offset = offset.RotatePointClockwise(rotation);

		if (attachedTo != null)
		{
			Vector3 parentOffset = attachedTo.DrawPosition(rot);
			offset.x += parentOffset.x;
			offset.y += parentOffset.z;
		}
		return new Vector3(offset.x, graphicOffset.y + DrawLayerOffset, offset.y);

		static float InheritedRotation(VehicleTurret turret)
		{
			float rotation = turret.vehicle?.Transform.rotation ?? 0;
			VehicleTurret parent = turret.attachedTo;
			while (parent != null)
			{
				rotation += parent.transform.rotation;
				parent = parent.attachedTo;
			}
			return rotation;
		}
	}

	private Vector3 DrawPosition(Rot8 rot, float rotation)
	{
		Rot8 offsetRot = rot;
		if (attachedTo != null)
			offsetRot = Rot8.North;
		Vector3 graphicOffset = Graphic?.DrawOffset(offsetRot) ?? Vector3.zero;
		Vector2 propsOffset = renderProperties.OffsetFor(offsetRot);
		Vector2 offset = new(graphicOffset.x + propsOffset.x, graphicOffset.z + propsOffset.y);

		offset = offset.RotatePointClockwise(rotation);

		if (attachedTo != null)
		{
			Vector3 parentOffset = attachedTo.DrawPosition(rot);
			offset.x += parentOffset.x;
			offset.y += parentOffset.z;
		}
		return new Vector3(offset.x, graphicOffset.y + DrawLayerOffset, offset.y);
	}

	public Rect ScaleUIRectFor(VehicleDef vehicleDef, Rect rect, Rot8 rot, float iconScale = 1)
	{
		GraphicDataRGB data = def.graphicData;
		Vector2 size = vehicleDef.ScaleDrawRatio(data, rot, rect.size, iconScale: iconScale);
		Vector2 original = new(data.drawSize.x, data.drawSize.y);
		Vector2 scaleFactors = new(size.x / original.x, size.y / original.y);
		Vector3 drawOffset = data.DrawOffsetForRot(rot);
		Vector2 baseOffset = new(drawOffset.x * scaleFactors.x, -drawOffset.z * scaleFactors.y);
		Vector2 propOffset = renderProperties.OffsetFor(rot);
		Vector2 offset = new(propOffset.x * scaleFactors.x, -propOffset.y * scaleFactors.y);
		Vector2 position = rect.center + baseOffset + offset;
		if (attachedTo != null)
		{
			Rect parentRect = attachedTo.ScaleUIRectFor(vehicleDef, rect, rot, iconScale);
			position += parentRect.center - rect.center;
		}
		return new Rect(position - size * 0.5f, size);
	}

	(int width, int height) IBlitTarget.TextureSize(in BlitRequest request)
	{
		return Texture ? (Texture.width, Texture.height) : (0, 0);
	}

	IEnumerable<RenderData> IBlitTarget.GetRenderData(Rect rect, BlitRequest request)
	{
		if (NoGraphic)
			yield break;

		VehicleDef graphicDef = vehicleDef ?? request.vehicleDef;
		Assert.IsNotNull(graphicDef);

		Rect turretRect = VehicleGraphics.TurretRect(rect, graphicDef, this, request.rot);
		yield return GetRenderDataFor(this, this, turretRect, request, Graphic);
		if (!TurretGraphics.NullOrEmpty())
		{
			foreach (TurretDrawData turretDrawData in TurretGraphics)
			{
				yield return GetRenderDataFor(this, turretDrawData, turretRect, request, turretDrawData.graphic);
			}
		}
		yield break;

		static RenderData GetRenderDataFor(VehicleTurret turret, IMaterialCacheTarget target, Rect turretRect,
			BlitRequest request, Graphic_Turret graphic)
		{
			bool canMask = graphic.Shader.SupportsRGBMaskTex() || graphic.Shader.SupportsMaskTex();
			Material material = canMask ? graphic.MatAtFull(Rot8.North) : null;
			if (graphic.Shader.SupportsRGBMaskTex())
				material = RGBMaterialPool.GetUi(target, request.rot);
			if (canMask && turret.def.matchParentColor)
			{
				RGBMaterialPool.SetProperties(target, request.patternData, graphic.TexAt, graphic.MaskAt);
			}
			RenderData turretRenderData = new(turretRect, graphic.TexAt(Rot8.North), material,
				target.PropertyBlock, graphic.DrawOffset(Rot4.North).y + turret.DrawLayerOffset,
				turret.defaultAngleRotated + request.rot.AsAngle);
			return turretRenderData;
		}
	}

	protected virtual void DrawTargeter()
	{
		if (GizmoHighlighted || TurretTargeter.Turret == this)
		{
			switch (def.turretType)
			{
				case TurretType.Rotatable:
					// TODO - Bake into a mesh for better rendering performance
					if (Mathf.Approximately(restrictedTheta, 0))
					{
						if (MaxRange < DefaultMaxRange)
							GenDraw.DrawCircleOutline(results.position, MaxRange);
						if (MinRange > 0)
							GenDraw.DrawCircleOutline(results.position, MaxRange);
					}
					else
					{
						DrawAngleLines(results.position, angleRestricted, MinRange, MaxRange, restrictedTheta,
							attachedTo?.TurretRotation ?? vehicle.FullRotation.AsAngle);
					}
				break;
				case TurretType.Static:
					if (!groupKey.NullOrEmpty())
					{
						foreach (VehicleTurret turret in GroupTurrets)
						{
							Vector3 target =
								turret.TurretLocation.PointFromAngle(turret.MaxRange, turret.TurretRotation);
							float range = Vector3.Distance(turret.TurretLocation, target);
							GenDraw.DrawRadiusRing(target.ToIntVec3(),
								turret.CurrentFireMode.forcedMissRadius * (range / turret.def.maxRange));
						}
					}
					else
					{
						Vector3 target = TurretLocation.PointFromAngle(MaxRange, TurretRotation);
						float range = Vector3.Distance(TurretLocation, target);
						GenDraw.DrawRadiusRing(target.ToIntVec3(),
							CurrentFireMode.forcedMissRadius * (range / def.maxRange));
					}
				break;
				default:
					throw new NotImplementedException(nameof(def.turretType));
			}
		}
	}

	/// <summary>
	/// Render lines from <paramref name="position"/> given angle and ranges
	/// </summary>
	/// <param name="position"></param>
	/// <param name="restrictedAngle"></param>
	/// <param name="minRange"></param>
	/// <param name="maxRange"></param>
	/// <param name="theta"></param>
	/// <param name="rotation"></param>
	public static void DrawAngleLines(Vector3 position, Vector2 restrictedAngle, float minRange,
		float maxRange, float theta, float rotation = 0f)
	{
		Vector3 minTargetPos1 =
			position.PointFromAngle(minRange, restrictedAngle.x + rotation);
		Vector3 minTargetPos2 =
			position.PointFromAngle(minRange, restrictedAngle.y + rotation);

		Vector3 maxTargetPos1 =
			position.PointFromAngle(maxRange, restrictedAngle.x + rotation);
		Vector3 maxTargetPos2 =
			position.PointFromAngle(maxRange, restrictedAngle.y + rotation);

		GenDraw.DrawLineBetween(minTargetPos1, maxTargetPos1);
		GenDraw.DrawLineBetween(minTargetPos2, maxTargetPos2);
		if (minRange > 0)
		{
			GenDraw.DrawLineBetween(position, minTargetPos1, SimpleColor.Red);
			GenDraw.DrawLineBetween(position, minTargetPos2, SimpleColor.Red);
		}

		float angleStart = restrictedAngle.x;

		Vector3 lastPointMin = minTargetPos1;
		Vector3 lastPointMax = maxTargetPos1;

		for (int angle = 0; angle < theta + 1; angle++)
		{
			Vector3 targetPointMax =
				position.PointFromAngle(maxRange, angleStart + angle + rotation);
			GenDraw.DrawLineBetween(lastPointMax, targetPointMax);
			lastPointMax = targetPointMax;

			if (minRange > 0)
			{
				Vector3 targetPointMin =
					position.PointFromAngle(minRange, angleStart + angle + rotation);
				GenDraw.DrawLineBetween(lastPointMin, targetPointMin, SimpleColor.Red);
				lastPointMin = targetPointMin;
			}
		}
	}

	protected virtual void DrawAimPie()
	{
		if (TargetLocked && ReadyToFire && Find.Selector.SingleSelectedThing == vehicle)
		{
			float facing = targetInfo.Thing != null ?
				(targetInfo.Thing.DrawPos - TurretLocation).AngleFlat() :
				(targetInfo.Cell - TurretLocation.ToIntVec3()).AngleFlat;
			GenDraw.DrawAimPieRaw(
				TurretLocation +
				new Vector3(aimPieOffset.x, Altitudes.AltInc, aimPieOffset.y).RotatedBy(TurretRotation),
				facing, (int)(PrefireTickCount * 0.5f));
		}
	}

	public virtual void ResolveGraphics(VehiclePawn vehicle, bool forceRegen = false)
	{
		ResolveGraphics(vehicle.patternData, forceRegen: forceRegen);
	}

	public virtual void ResolveGraphics(VehicleDef vehicleDef, bool forceRegen = false)
	{
		PatternData patternData =
			VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName,
				vehicleDef.graphicData);
		ResolveGraphics(patternData, forceRegen: forceRegen);
	}

	public virtual void ResolveGraphics(PatternData patternData, bool forceRegen = false)
	{
		if (NoGraphic)
			return;

		if (cachedGraphicData is null || forceRegen)
		{
			cachedGraphic = GenerateGraphicData(this, this, def.graphicData, patternData,
				ref cachedGraphicData);
			cachedMaterial = null;
			if (!def.graphics.NullOrEmpty())
				SetLayerGraphics(patternData);
			this.SetDirty();
		}

		if (!cachedMaterial || forceRegen)
		{
			cachedMaterial = cachedGraphic?.MatAt(Rot8.North, vehicle);
		}
	}

	private void SetLayerGraphics(PatternData patternData)
	{
		if (turretGraphics.NullOrEmpty())
		{
			turretGraphics ??= [];
			foreach (VehicleTurretRenderData renderData in def.graphics)
			{
				turretGraphics.Add(new TurretDrawData(this, renderData));
			}
		}
		for (int i = 0; i < def.graphics.Count; i++)
		{
			VehicleTurretRenderData renderData = def.graphics[i];
			TurretDrawData drawData = TurretGraphics[i];
			drawData.Set(renderData.graphicData, patternData);
		}
	}

	private static Graphic_Turret GenerateGraphicData(IMaterialCacheTarget cacheTarget,
		VehicleTurret turret, GraphicDataRGB copyGraphicData, PatternData patternData,
		ref GraphicDataRGB cachedGraphicData)
	{
		cachedGraphicData = new GraphicDataRGB();
		cachedGraphicData.CopyFrom(copyGraphicData);
		Graphic_Turret graphic;
		if ((cachedGraphicData.shaderType.Shader.SupportsMaskTex() ||
			cachedGraphicData.shaderType.Shader.SupportsRGBMaskTex()))
		{
			if (turret.def.matchParentColor)
			{
				cachedGraphicData.CopyDrawData(patternData);
			}
			else
			{
				cachedGraphicData.CopyDrawData(copyGraphicData);
			}
		}

		if (cachedGraphicData.shaderType != null &&
			cachedGraphicData.shaderType.Shader.SupportsRGBMaskTex())
		{
			RGBMaterialPool.CacheMaterialsFor(cacheTarget, patternData.patternDef);
			cachedGraphicData.Init(cacheTarget);
			graphic = cachedGraphicData.Graphic as Graphic_Turret;
			Assert.IsNotNull(graphic);
			RGBMaterialPool.SetProperties(cacheTarget, cachedGraphicData, graphic.TexAt, graphic.MaskAt);
		}
		else
		{
			graphic = ((GraphicData)cachedGraphicData).Graphic as Graphic_Turret;
		}

		return graphic;
	}

	public virtual void InitTurretMotes(Vector3 loc)
	{
		if (def.motes.NullOrEmpty())
			return;

		foreach (AnimationProperties moteProps in def.motes)
		{
			Vector3 moteLoc = loc;
			if (!loc.ShouldSpawnMotesAt(vehicle.Map))
				continue;

			try
			{
				float altitudeLayer = moteProps.moteDef.altitudeLayer.AltitudeFor();
				Vector3 offset = moteProps.offset.RotatedBy(TurretRotation);
				moteLoc += new Vector3(offset.x, altitudeLayer + offset.y, offset.z);
				Mote mote = (Mote)ThingMaker.MakeThing(moteProps.moteDef);
				mote.instanceColor = moteProps.color;
				mote.rotationRate = moteProps.rotationRate;
				mote.Scale = moteProps.scale;
				mote.def = moteProps.moteDef;
				GenSpawn.Spawn(mote, moteLoc.ToIntVec3(), vehicle.Map);
				mote.exactPosition = moteLoc;
				mote.exactRotation = moteProps.exactRotation.RandomInRange;
				switch (mote)
				{
					case MoteThrown thrownMote:
						float thrownAngle = TurretRotation + moteProps.angleThrown.RandomInRange;
						if (thrownMote is MoteThrownExpand expandMote)
						{
							if (expandMote is MoteThrownSlowToSpeed accelMote)
							{
								accelMote.SetDecelerationRate(moteProps.deceleration.RandomInRange,
									moteProps.fixedAcceleration, thrownAngle);
							}
							expandMote.growthRate = moteProps.growthRate.RandomInRange;
						}
						thrownMote.SetVelocity(thrownAngle, moteProps.speedThrown.RandomInRange);
					break;
					case MoteCannonPlume plumeMote:
						plumeMote.cyclesLeft = moteProps.cycles;
						plumeMote.animationType = moteProps.animationType;
						plumeMote.exactRotation = TurretRotation;
					break;
				}
			}
			catch (Exception ex)
			{
				SmashLog.Error(
					$"Failed to spawn mote at {loc}. MoteDef = <field>{moteProps.moteDef?.defName ?? "Null"}</field> Exception = {ex}");
			}
		}
	}

	/// <summary>
	/// Caches VehicleTurret draw location based on <see cref="renderProperties"/>, <see cref="attachedTo"/>
	/// draw loc is not cached, as rotating can alter the final draw location.
	/// </summary>
	public void RecacheRootDrawPos()
	{
		if (GraphicData != null)
		{
			rootDrawPosNorth = RootOffset(this, Rot8.North);
			rootDrawPosEast = RootOffset(this, Rot8.East);
			rootDrawPosSouth = RootOffset(this, Rot8.South);
			rootDrawPosWest = RootOffset(this, Rot8.West);
			rootDrawPosNorthEast = RootOffset(this, Rot8.NorthEast);
			rootDrawPosSouthEast = RootOffset(this, Rot8.SouthEast);
			rootDrawPosSouthWest = RootOffset(this, Rot8.SouthWest);
			rootDrawPosNorthWest = RootOffset(this, Rot8.NorthWest);
		}
		return;

		static Vector3 RootOffset(VehicleTurret turret, Rot8 rot)
		{
			Vector2 turretLoc = turret.renderProperties.OffsetFor(rot);
			Vector3 graphicOffset = turret.Graphic?.DrawOffset(rot) ?? Vector3.zero;
			return new Vector3(graphicOffset.x + turretLoc.x, graphicOffset.y + turret.DrawLayerOffset,
				graphicOffset.z + turretLoc.y);
		}
	}

	public Vector3 TurretOffset(Rot8 rot)
	{
		return rot.AsInt switch
		{
			// North
			0 => rootDrawPosNorth,
			// East
			1 => rootDrawPosEast,
			// South
			2 => rootDrawPosSouth,
			// West
			3 => rootDrawPosWest,
			// NorthEast
			4 => rootDrawPosNorthEast,
			// SouthEast
			5 => rootDrawPosSouthEast,
			// SouthWest
			6 => rootDrawPosSouthWest,
			// NorthWest
			7 => rootDrawPosNorthWest,
			_ => throw new NotImplementedException("Invalid Rot8")
		};
	}

	public static SubGizmo SubGizmo_RemoveAmmo(VehicleTurret turret)
	{
		Assert.IsNotNull(turret.def.ammunition);
		return new SubGizmo(
			drawGizmo: delegate(Rect rect)
			{
				//Widgets.DrawTextureFitted(rect, BGTex, 1);
				if (turret.loadedAmmo != null)
				{
					//Only modify alpha
					using (new TextBlock(new Color(GUI.color.r, GUI.color.g, GUI.color.b,
						turret.IconAlphaTicked)))
					{
						Widgets.DrawTextureFitted(rect, turret.loadedAmmo.uiIcon, 1);
					}

					Rect ammoCountRect = new(rect);
					string ammoCount = turret.vehicle.inventory.innerContainer
					 .Where(td => td.def == turret.loadedAmmo).Select(t => t.stackCount).Sum().ToStringSafe();
					ammoCountRect.y += ammoCountRect.height / 2;
					ammoCountRect.x += ammoCountRect.width - Text.CalcSize(ammoCount).x;
					Widgets.Label(ammoCountRect, ammoCount);
				}
				else if (turret.def.genericAmmo && turret.def.ammunition.AllowedDefCount > 0)
				{
					ThingDef ammoDef = turret.def.ammunition.AllowedThingDefs.FirstOrDefault();
					Assert.IsNotNull(ammoDef);
					Widgets.DrawTextureFitted(rect, ammoDef.uiIcon, 1);

					Rect ammoCountRect = new(rect);
					string ammoCount = turret.vehicle.inventory.innerContainer
					 .Where(td => td.def == turret.def.ammunition.AllowedThingDefs.FirstOrDefault())
					 .Select(t => t.stackCount).Sum().ToStringSafe();
					ammoCountRect.y += ammoCountRect.height / 2;
					ammoCountRect.x += ammoCountRect.width - Text.CalcSize(ammoCount).x;
					Widgets.Label(ammoCountRect, ammoCount);
				}
			},
			canClick: () => turret.shellCount > 0,
			onClick: delegate
			{
				turret.TryClearChamber();
				SoundDefOf.Artillery_ShellLoaded.PlayOneShot(new TargetInfo(turret.vehicle.Position,
					turret.vehicle.Map));
			},
			tooltip: turret.loadedAmmo?.LabelCap
		);
	}

	public static SubGizmo SubGizmo_ReloadFromInventory(VehicleTurret turret)
	{
		return new SubGizmo(
			drawGizmo: delegate(Rect rect) { Widgets.DrawTextureFitted(rect, VehicleTex.ReloadIcon, 1); },
			canClick: () => true,
			onClick: delegate
			{
				if (turret.def.ammunition is null)
				{
					turret.Reload();
				}
				else if (turret.def.genericAmmo)
				{
					if (!turret.vehicle.inventory.innerContainer.Contains(turret.def.ammunition
					 .AllowedThingDefs.FirstOrDefault()))
					{
						Messages.Message("VF_NoAmmoAvailable".Translate(), MessageTypeDefOf.RejectInput);
					}
					else
					{
						turret.Reload(turret.def.ammunition.AllowedThingDefs.FirstOrDefault());
					}
				}
				else
				{
					List<FloatMenuOption> options = [];
					List<ThingDef> ammoAvailable = turret.vehicle.inventory.innerContainer
					 .Where(d => turret.ContainsAmmoDefOrShell(d.def)).Select(t => t.def).Distinct().ToList();
					for (int i = ammoAvailable.Count - 1; i >= 0; i--)
					{
						ThingDef ammo = ammoAvailable[i];
						options.Add(new FloatMenuOption(ammoAvailable[i].LabelCap,
							delegate { turret.Reload(ammo); }));
					}

					if (options.Count == 0)
					{
						FloatMenuOption noAmmoOption =
							new("VF_VehicleTurrets_NoAmmoToReload".Translate(), null)
							{
								Disabled = true
							};
						options.Add(noAmmoOption);
					}
					Find.WindowStack.Add(new FloatMenu(options));
				}
			},
			tooltip: "VF_ReloadVehicleTurret".Translate()
		);
	}

	public static SubGizmo SubGizmo_FireMode(VehicleTurret turret)
	{
		return new SubGizmo(
			drawGizmo: delegate(Rect rect) { Widgets.DrawTextureFitted(rect, turret.CurrentFireMode.Icon, 1); },
			canClick: () => turret.def.fireModes.Count > 1,
			onClick: turret.CycleFireMode,
			tooltip: turret.CurrentFireMode.label
		);
	}

	public static SubGizmo SubGizmo_AutoTarget(VehicleTurret turret)
	{
		return new SubGizmo(
			drawGizmo: delegate(Rect rect)
			{
				Widgets.DrawTextureFitted(rect, VehicleTex.AutoTargetIcon, 1);
				Rect checkboxRect = new(rect.x + rect.width / 2, rect.y + rect.height / 2,
					rect.width / 2, rect.height / 2);
				GUI.DrawTexture(checkboxRect,
					turret.AutoTarget ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex);
			},
			canClick: () => turret.CanAutoTarget,
			onClick: turret.SwitchAutoTarget,
			tooltip: AutoTargetingTooltip()
		);

		string AutoTargetingTooltip()
		{
			StringBuilder tooltip = UIHelper.tooltipBuilder;
			tooltip.Clear();
			tooltip.AppendLine("VF_ToggleAutoTargeting".Translate());
			tooltip.AppendLine();
			tooltip.AppendLine();
			tooltip.AppendLine("VF_ToggleAutoTargetingDesc"
			 .Translate((turret.AutoTarget ? "On".TranslateSimple() : "Off".TranslateSimple())
				 .UncapitalizeFirst().Named("ONOFF"))
			 .Resolve());
			string text = tooltip.ToString();
			tooltip.Clear();
			return text;
		}
	}

	[NoProfiling]
	public (Texture2D mainTex, Texture2D maskTex) GetTextures(Rot8 rot)
	{
		throw new NotImplementedException();
	}

	public record SubGizmo
	{
		public readonly Action<Rect> drawGizmo;
		public readonly Func<bool> canClick;
		public readonly Action onClick;
		public readonly string tooltip;

		public SubGizmo(Action<Rect> drawGizmo, Func<bool> canClick, Action onClick, string tooltip)
		{
			this.drawGizmo = drawGizmo;
			this.canClick = canClick;
			this.onClick = onClick;
			this.tooltip = tooltip;
		}

		public bool IsValid => onClick != null;
	}

	public class TurretDrawData : IMaterialCacheTarget
	{
		private readonly VehicleTurret turret;

		public Graphic_Turret graphic;
		public GraphicDataRGB graphicData;
		public VehicleTurretRenderData renderData;

		public TurretDrawData(VehicleTurret turret, VehicleTurretRenderData renderData)
		{
			this.turret = turret;
			this.renderData = renderData;
		}

		public int MaterialCount => 1;

		public PatternDef PatternDef => turret.PatternDef;

		public string Name => $"{turret.def}_{turret.key}_{turret.vehicle?.ThingID ?? "Def"}";

		// TurretDrawData is already created on the main thread
		public MaterialPropertyBlock PropertyBlock { get; } = new();

		public void Set(GraphicDataRGB copyFrom, PatternData patternData)
		{
			graphic = GenerateGraphicData(this, turret, copyFrom, patternData, ref graphicData);
		}

		public Vector3 DrawOffset(Rot8 rot, float parentRotation, float rotation)
		{
			Rot8 offsetRot = rot;
			if (turret.attachedTo != null)
				offsetRot = Rot8.North;
			Vector3 graphicOffset = graphic?.DrawOffset(offsetRot) ?? Vector3.zero;
			Vector2 graphicOffsetRotated = new Vector2(graphicOffset.x, graphicOffset.z).RotatePointClockwise(parentRotation);
			Vector2 propsOffset = turret.renderProperties.OffsetFor(offsetRot);
			Vector2 offset = new(graphicOffsetRotated.x + propsOffset.x, graphicOffsetRotated.y + propsOffset.y);

			offset = offset.RotatePointClockwise(rotation);

			if (turret.attachedTo != null)
			{
				Vector3 parentOffset = turret.attachedTo.DrawPosition(rot);
				offset.x += parentOffset.x;
				offset.y += parentOffset.z;
			}
			return new Vector3(offset.x, graphicOffset.y + turret.DrawLayerOffset, offset.y);
		}

		public override string ToString()
		{
			return $"TurretDrawData_{turret.key}_({graphicData.texPath})";
		}
	}
}