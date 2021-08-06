using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class TurretTop_Artillery : TurretTop
	{
		protected const float IdleTurnDegreesPerTick = 0.26f;
		protected const int IdleTurnDuration = 140;
		protected const int IdleTurnIntervalMin = 150;
		protected const int IdleTurnIntervalMax = 350;

		protected Building_Artillery parentTurret;
		protected float curRotationInt;
		protected int ticksUntilIdleTurn;
		protected int idleTurnTicksLeft;
		protected bool idleTurnClockwise;
		protected Vector3 altitudeDrawLayer;

		protected CompDrawLayerTurret drawLayer;
		protected bool drawLayersDisabled = false;

		protected Graphic turretGraphic;
		protected GraphicData turretGraphicData;

		public TurretTop_Artillery(Building_Artillery parentTurret) : base(parentTurret)
		{
			this.parentTurret = parentTurret;
		}

		public virtual new float CurRotation { get => curRotationInt; set => curRotationInt = value.ClampAndWrap(0, 360); }

		public CompDrawLayerTurret DrawLayer
		{
			get
			{
				if (drawLayer is null && !drawLayersDisabled)
				{
					drawLayer = parentTurret.GetComp<CompDrawLayerTurret>();
					if (drawLayer is null)
					{
						drawLayersDisabled = true;
					}
				}
				return drawLayer;
			}
		}

		public GraphicData TurretGraphicData
		{
			get
			{
				if (turretGraphicData is null)
				{
					turretGraphicData = parentTurret.def.building.turretGunDef?.graphicData;
				}
				return turretGraphicData;
			}
		}

		public Graphic TurretGraphic
		{
			get
			{
				if (turretGraphic is null)
				{
					turretGraphic = TurretGraphicData?.Graphic.GetColoredVersion(TurretGraphicData.shaderType.Shader, parentTurret.DrawColor, parentTurret.DrawColorTwo);
				}
				return turretGraphic;
			}
		}

		public new virtual void SetRotationFromOrientation()
		{
			CurRotation = parentTurret.Rotation.AsAngle;
		}

		public virtual void DrawTurret()
		{
			Vector3 offset = new Vector3(parentTurret.def.building.turretTopOffset.x, 0f, parentTurret.def.building.turretTopOffset.y).RotatedBy(CurRotation);
			float drawSize = parentTurret.def.building.turretTopDrawSize;
			Matrix4x4 matrix = default;
			matrix.SetTRS(parentTurret.DrawPos + altitudeDrawLayer + Altitudes.AltIncVect, (CurRotation + ArtworkRotation).ToQuat(), new Vector3(drawSize, 1f, drawSize));
			Graphics.DrawMesh(MeshPool.plane10, matrix, TurretGraphic?.MatAt(parentTurret.Rotation) ?? parentTurret.def.building.turretTopMat, 0);
			if (DrawLayer != null)
			{
				DrawLayer.DrawExtra(parentTurret.DrawPos + altitudeDrawLayer + Altitudes.AltIncVect + offset, CurRotation);
			}
		}

		public virtual void Tick()
		{
			LocalTargetInfo currentTarget = parentTurret.CurrentTarget;
			GlobalTargetInfo worldTarget = parentTurret.CurrentWorldTarget;
			if (currentTarget.IsValid)
			{
				float curRotation = (currentTarget.Cell.ToVector3Shifted() - parentTurret.DrawPos).AngleFlat();
				CurRotation = curRotation;
				ticksUntilIdleTurn = Rand.RangeInclusive(IdleTurnIntervalMin, IdleTurnIntervalMax);
				return;
			}
			if (worldTarget.IsValid)
			{
				Vector3 source = Find.WorldGrid.GetTileCenter(parentTurret.Map.Tile);
				Vector3 target = worldTarget.WorldObject is null ? Find.WorldGrid.GetTileCenter(worldTarget.Tile) : worldTarget.WorldObject.DrawPos;
				CurRotation = WorldHelper.TryFindHeading(source, target);
				ticksUntilIdleTurn = Rand.RangeInclusive(IdleTurnIntervalMin, IdleTurnIntervalMax);
				return;
			}
			if (ticksUntilIdleTurn > 0)
			{
				ticksUntilIdleTurn--;
				if (ticksUntilIdleTurn == 0)
				{
					if (Rand.Value < 0.5f)
					{
						idleTurnClockwise = true;
					}
					else
					{
						idleTurnClockwise = false;
					}
					idleTurnTicksLeft = IdleTurnDuration;
					return;
				}
			}
			else
			{
				if (idleTurnClockwise)
				{
					CurRotation += IdleTurnDegreesPerTick;
				}
				else
				{
					CurRotation -= IdleTurnDegreesPerTick;
				}
				idleTurnTicksLeft--;
				if (idleTurnTicksLeft <= 0)
				{
					ticksUntilIdleTurn = Rand.RangeInclusive(IdleTurnIntervalMin, IdleTurnIntervalMax);
				}
			}
		}

		public virtual void PostSpawnSetup()
		{
			float altitude = parentTurret.def.building.turretGunDef.Altitude;
			altitudeDrawLayer = new Vector3(0, altitude, 0);
		}
	}
}
