using System;
using System.Collections.Generic;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;

namespace Vehicles
{
	public class VehicleTurretRecoiled : VehicleTurret
	{
		protected int currentFrame = 0;
		protected int ticksPerFrame = 1;
		protected int ticks;
		protected int cyclesLeft = 0;
		protected bool reverseAnimate;
		protected AnimationWrapperType wrapperType;

		protected Graphic_Turret[] cannonGraphics;
		protected Material[] cannonMaterials;
		protected Texture2D[] cannonTextures;

		public VehicleTurretRecoiled(VehiclePawn vehicle) : base(vehicle)
		{
		}

		public VehicleTurretRecoiled(VehiclePawn vehicle, VehicleTurretRecoiled reference) : base(vehicle, reference)
		{
			DisableAnimation();
		}

		public virtual int AnimationCount
		{
			get
			{
				return (cannonGraphic as Graphic_TurretAnimate).AnimationFrameCount;
			}
		}

		public override Texture2D CannonTexture
		{
			get
			{
				if (CannonGraphicData.texPath.NullOrEmpty())
				{
					return null;
				}
				if (cannonTex is null)
				{
					ResolveCannonGraphics(vehicle);
				}
				if (cannonTextures is null || cannonTextures.Length != AnimationCount)
				{
					ResolveCache();
				}
				return cannonTextures[currentFrame];
			}
		}

		public override Material CannonMaterial
		{
			get
			{
				if (cannonMaterialCache is null)
				{
					ResolveCannonGraphics(vehicle);
				}
				if (cannonMaterials is null || cannonMaterials.Length != AnimationCount)
				{
					ResolveCache();
				}
				return cannonMaterials[currentFrame];
			}
		}

		public override Graphic_Turret CannonGraphic
		{
			get
			{
				if (cannonGraphic is null)
				{
					ResolveCannonGraphics(vehicle);
				}
				if (cannonGraphics is null || cannonGraphics.Length != AnimationCount)
				{
					ResolveCache();
				}
				return cannonGraphics[currentFrame];
			}
		}

		public override void Draw()
		{
			base.Draw();
			if(ticks >= 0)
			{
				ticks++;
				if (ticks > ticksPerFrame)
				{
					if (reverseAnimate)
					{
						currentFrame--;
					}
					else
					{
						currentFrame++;
					}

					ticks = 0;
					
					if (currentFrame >= AnimationCount || currentFrame < 0)
					{
						cyclesLeft--;

						if (wrapperType == AnimationWrapperType.Oscillate)
						{
							reverseAnimate = !reverseAnimate;
						}

						currentFrame = reverseAnimate ? AnimationCount - 1 : 0;
						if(cyclesLeft <= 0)
						{
							DisableAnimation();
						}
					}
				}
			}
		}

		public override void PostTurretFire()
		{
			base.PostTurretFire();
			StartAnimation(2, 1, AnimationWrapperType.Oscillate);
		}

		public virtual void StartAnimation(int ticksPerFrame, int cyclesLeft, AnimationWrapperType wrapperType)
		{
			if (AnimationCount == 1)
			{
				return;
			}
			this.ticksPerFrame = ticksPerFrame;
			this.cyclesLeft = cyclesLeft;
			this.wrapperType = wrapperType;
			ticks = 0;
			currentFrame = 0;
			reverseAnimate = false;
		}

		public virtual void DisableAnimation()
		{
			currentFrame = 0;
			cyclesLeft = 0;
			ticksPerFrame = 1;
			ticks = -1;
			wrapperType = AnimationWrapperType.Off;
		}

		public override void ResolveCannonGraphics(VehiclePawn vehicle, bool forceRegen = false)
		{
			base.ResolveCannonGraphics(vehicle, forceRegen);
			if (forceRegen)
			{
				ResolveCache(forceRegen);
			}
		}

		public override void ResolveCannonGraphics(PatternData patternData, bool forceRegen = false)
		{
			base.ResolveCannonGraphics(patternData, forceRegen);
			if (forceRegen)
			{
				ResolveCache(forceRegen);
			}
		}

		public virtual void ResolveCache(bool forceRegen = false)
		{
			if (cannonGraphics.EnumerableNullOrEmpty() || cannonGraphics.Length != AnimationCount || forceRegen)
			{
				cannonGraphics = new Graphic_Turret[AnimationCount];
				for (int i = 0; i < AnimationCount; i++)
				{
					cannonGraphics[i] = (cannonGraphic as Graphic_TurretAnimate).SubGraphicCycle(i, cannonGraphic.Shader, CannonGraphicData.color, CannonGraphicData.colorTwo, CannonGraphicData.colorThree, CannonGraphicData.tiles) as Graphic_Turret;
				}
			}
			if (cannonMaterials.EnumerableNullOrEmpty() || cannonMaterials.Length != AnimationCount || forceRegen)
			{
				cannonMaterials = new Material[AnimationCount];
				for (int i = 0; i < AnimationCount; i++)
				{
					cannonMaterials[i] = (cannonGraphic as Graphic_TurretAnimate).SubMaterialCycle(vehicle?.patternData?.patternDef ?? PatternDefOf.Default, i);
				}
			}
			if (cannonTextures.EnumerableNullOrEmpty() || cannonTextures.Length != AnimationCount || forceRegen)
			{
				cannonTextures = (cannonGraphic as Graphic_TurretAnimate).SubTextures;
			}
		}
	}
}
