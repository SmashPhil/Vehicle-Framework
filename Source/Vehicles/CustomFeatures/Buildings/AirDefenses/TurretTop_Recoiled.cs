using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class TurretTop_Recoiled : TurretTop_Artillery
	{
		public float curRecoil;
		protected float targetRecoil;
		protected float recoilStep;
		protected bool recoilingBack;

		public TurretTop_Recoiled(Building_RecoiledTurret parentTurret) : base(parentTurret)
		{
		}

		protected virtual Building_RecoiledTurret ParentTurret => parentTurret as Building_RecoiledTurret;

		public VerbProperties_Recoil VerbProps
		{
			get
			{
				if (parentTurret.AttackVerb.verbProps is VerbProperties_Recoil verbProps)
				{
					return verbProps;
				}
				SmashLog.Error($"Unable to retrieve <type>VerbProperties_Recoil</type> for recoiled turret. <property>AttackVerb</property> must have a VerbProperty of type <type>VerbProperties_Recoil</type>");
				return null;
			}
		}

		public override void DrawTurret()
		{
			Vector3 offset = new Vector3(parentTurret.def.building.turretTopOffset.x, 0f, parentTurret.def.building.turretTopOffset.y).RotatedBy(CurRotation);
			Vector3 vectorRecoiled = Ext_Math.PointFromAngle(offset, -curRecoil, CurRotation);
			float drawSize = parentTurret.def.building.turretTopDrawSize;
			Matrix4x4 matrix = default;
			matrix.SetTRS(parentTurret.DrawPos + altitudeDrawLayer + Altitudes.AltIncVect + vectorRecoiled, (CurRotation + ArtworkRotation).ToQuat(), new Vector3(drawSize, 1f, drawSize));
			Graphics.DrawMesh(MeshPool.plane10, matrix, TurretGraphic?.MatAt(parentTurret.Rotation) ?? parentTurret.def.building.turretTopMat, 0);
			if (DrawLayer != null)
			{
				DrawLayer.DrawExtra(parentTurret.DrawPos + altitudeDrawLayer + Altitudes.AltIncVect + offset, CurRotation);
			}
		}

		public virtual void RecoilTick()
		{
			if (targetRecoil > 0f)
			{
				if (recoilingBack)
				{
					curRecoil += recoilStep;
					if (curRecoil >= targetRecoil)
					{
						curRecoil = targetRecoil;
					}
				}
				else
				{
					curRecoil -= recoilStep * VerbProps.recoil.speedMultiplierPostRecoil;
					if (curRecoil <= 0)
					{
						ResetRecoilVars();
					}
				}

				if (curRecoil >= targetRecoil)
				{
					recoilingBack = false;
				}
			}
		}

		public void Notify_TurretRecoil()
		{
			targetRecoil = VerbProps.recoil.distanceTotal;
			recoilStep = VerbProps.recoil.distancePerTick;
			curRecoil = 0;
			recoilingBack = true;
		}

		private void ResetRecoilVars()
		{
			curRecoil = 0;
			targetRecoil = 0;
			recoilStep = 0;
		}
	}
}
