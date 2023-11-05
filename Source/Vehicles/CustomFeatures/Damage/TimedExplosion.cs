using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public class TimedExplosion : IExposable
	{
		private static readonly Material WickMaterialA = MaterialPool.MatFrom("Things/Special/BurningWickA", ShaderDatabase.MetaOverlay);
		private static readonly Material WickMaterialB = MaterialPool.MatFrom("Things/Special/BurningWickB", ShaderDatabase.MetaOverlay);
		private static readonly float WickAltitude = AltitudeLayer.MetaOverlays.AltitudeFor();

		private int ticksLeft = -1;
		
		private int radius = 1;
		private DamageDef damageDef;
		private int damageAmount = int.MaxValue;
		private float armorPenetration;

		private VehiclePawn vehicle;
		private IntVec2 cell;
		private DrawOffsets drawOffsets;

		private Sustainer wickSoundSustainer;

		public bool Active { get; private set; }

		public TimedExplosion(VehiclePawn vehicle, IntVec2 cell, int wickTicks, int radius, DamageDef damageDef, int damageAmount, float armorPenetration = -1, DrawOffsets drawOffsets = null)
		{
			this.vehicle = vehicle;
			this.cell = cell;
			this.drawOffsets = drawOffsets;
			ticksLeft = wickTicks;
			this.radius = radius;
			this.damageDef = damageDef;
			this.damageAmount = damageAmount;
			this.armorPenetration = armorPenetration;
			if (this.armorPenetration < 0)
			{
				this.armorPenetration = damageDef.defaultArmorPenetration;
			}

			Start();
		}

		public IntVec3 AdjustedCell
		{
			get
			{
				IntVec2 adjustedLoc = cell.RotatedBy(vehicle.Rotation, vehicle.VehicleDef.Size);
				IntVec3 adjustedCell = new IntVec3(adjustedLoc.x + vehicle.Position.x, 0, adjustedLoc.z + vehicle.Position.z);
				return adjustedCell;
			}
		}

		public void DrawAt(Vector3 drawLoc, Rot8 rot)
		{
			if (Active && vehicle.Spawned)
			{
				Material material;
				if ((vehicle.thingIDNumber + Find.TickManager.TicksGame) % 6 < 3)
				{
					material = WickMaterialA;
				}
				else
				{
					material = WickMaterialB;
				}
				drawLoc.y = WickAltitude;
				if (drawOffsets != null)
				{
					Vector3 offset = drawOffsets.OffsetFor(rot);
					drawLoc += offset;
				}
				else
				{
					IntVec2 rotatedHitCell = cell.RotatedBy(rot, vehicle.VehicleDef.Size);
					IntVec3 mapCell = rotatedHitCell.ToIntVec3 + vehicle.Position;
					Vector3 position = mapCell.ToVector3Shifted();
					drawLoc = new Vector3(position.x, drawLoc.y, position.z);
				}
				Matrix4x4 matrix = Matrix4x4.TRS(drawLoc, Quaternion.identity, Vector3.one);
				Graphics.DrawMesh(MeshPool.plane20, matrix, material, 0);
			}
		}

		public void Start()
		{
			if (!Active)
			{
				Active = true;
				StartWickSustainer();
				GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(vehicle, damageDef, instigator: vehicle);

				if (ticksLeft <= 0)
				{
					Explode();
				}
			}
		}

		public void End()
		{
			Active = false;
			wickSoundSustainer?.End();
		}

		public bool Tick()
		{
			if (!Active || !vehicle.Spawned)
			{
				return false;
			}
			UpdateWick();
			if (ticksLeft <= 0)
			{
				Explode();
			}
			ticksLeft--;
			return true;
		}

		private void UpdateWick()
		{
			if (wickSoundSustainer == null)
			{
				StartWickSustainer();
			}
			else
			{
				wickSoundSustainer.Maintain();
			}
		}

		private void StartWickSustainer()
		{
			SoundInfo info = SoundInfo.InMap(vehicle, MaintenanceType.PerTick);
			wickSoundSustainer = SoundDefOf.HissSmall.TrySpawnSustainer(info);
		}

		private void Explode()
		{
			End();
			GenExplosion.DoExplosion(AdjustedCell, vehicle.Map, radius, damageDef, vehicle, damageAmount, armorPenetration);
		}

		public void ExposeData()
		{
			Scribe_References.Look(ref vehicle, nameof(vehicle));
			Scribe_Values.Look(ref cell, nameof(cell));
			Scribe_Values.Look(ref ticksLeft, nameof(ticksLeft));
			Scribe_Values.Look(ref radius, nameof(radius));
			Scribe_Defs.Look(ref damageDef, nameof(damageDef));
			Scribe_Values.Look(ref damageAmount, nameof(damageAmount));
			Scribe_Values.Look(ref armorPenetration, nameof(armorPenetration));
		}
	}
}
