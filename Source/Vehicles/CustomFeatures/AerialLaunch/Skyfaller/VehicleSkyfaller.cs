using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;
using SmashTools;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public abstract class VehicleSkyfaller : Thing, IActiveDropPod, IThingHolder
	{
		protected static MaterialPropertyBlock shadowPropertyBlock = new MaterialPropertyBlock();

		public float angle;

		protected Material cachedShadowMaterial;

		protected bool anticipationSoundPlayed;

		public VehiclePawn vehicle;

		protected ActiveDropPodInfo contents;

		public override Vector3 DrawPos => vehicle.TrueCenter(Position, base.DrawPos.y);

		public ThingWithComps Thing => vehicle;

		private Material ShadowMaterial
		{
			get
			{
				if (cachedShadowMaterial is null && !def.skyfaller.shadow.NullOrEmpty())
				{
					cachedShadowMaterial = MaterialPool.MatFrom(def.skyfaller.shadow, ShaderDatabase.Transparent);
				}
				return cachedShadowMaterial;
			}
		}

		public virtual ActiveDropPodInfo Contents
		{
			get
			{
				if (contents is null)
				{
					contents = new ActiveDropPodInfo();
					foreach (Pawn pawn in vehicle.AllPawnsAboard)
					{
						contents.innerContainer.TryAdd(pawn, false);
					}
				}
				return contents;
			}
		}

		public void GetChildHolders(List<IThingHolder> outChildren)
		{
			outChildren = vehicle.AllPawnsAboard.Cast<IThingHolder>().ToList();
		}

		public ThingOwner GetDirectlyHeldThings()
		{
			return vehicle.inventory.innerContainer;
		}

		public override void Tick()
		{
			vehicle.CompVehicleLauncher.launchProtocol.Tick();
		}

		protected virtual void LeaveMap()
		{
			Destroy();
		}

		protected virtual void DrawDropSpotShadow()
		{
			Material shadowMaterial = ShadowMaterial;
			if (shadowMaterial is null)
			{
				return;
			}
			//TODO - draw shadow at DrawPos but z-axis is left on ground and size decreases through curve
			DrawDropSpotShadow(DrawPos, Rotation, shadowMaterial, def.skyfaller.shadowSize, vehicle.CompVehicleLauncher.launchProtocol.TicksPassed);
		}

		public static void DrawDropSpotShadow(Vector3 center, Rot4 rot, Material material, Vector2 shadowSize, int ticksToLand)
		{
			if (rot.IsHorizontal)
			{
				Gen.Swap(ref shadowSize.x, ref shadowSize.y);
			}
			ticksToLand = Mathf.Max(ticksToLand, 0);
			Vector3 pos = center;
			pos.y = AltitudeLayer.Shadows.AltitudeFor();
			float num = 1f + ticksToLand / 100f;
			Vector3 s = new Vector3(num * shadowSize.x, 1f, num * shadowSize.y);
			Color white = Color.white;
			if (ticksToLand > 150)
			{
				white.a = Mathf.InverseLerp(200f, 150f, ticksToLand);
			}
			shadowPropertyBlock.SetColor(ShaderPropertyIDs.Color, white);
			Matrix4x4 matrix = default;
			matrix.SetTRS(pos, rot.AsQuat, s);
			Graphics.DrawMesh(MeshPool.plane10Back, matrix, material, 0, null, 0, shadowPropertyBlock);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			
			Scribe_Values.Look(ref angle, "angle", 0f, false);
			Scribe_References.Look(ref vehicle, "vehicle", true);
		}
	}
}
