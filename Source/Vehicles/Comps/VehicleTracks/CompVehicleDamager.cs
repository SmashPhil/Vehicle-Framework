using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace Vehicles
{
	[Obsolete("Currently disabled, do not use.")]
	public class CompVehicleDamager : VehicleAIComp
	{
		public CompProperties_VehicleDamager Props => (CompProperties_VehicleDamager)props;
		public VehiclePawn Vehicle => (VehiclePawn)parent;

		public void TakeStep()
		{
			throw new NotImplementedException();
		}

		public override void CompTick()
		{
			base.CompTick();

			if (Vehicle.Spawned && !Props.hitboxes.NullOrEmpty())
			{
				if (VehicleMod.settings.debug.debugDrawVehicleTracks)
				{
					foreach (Hitbox hitbox in Props.hitboxes)
					{
						DebugVehicleTracksDrawer(hitbox);
					}
				}
			}
		}

		private void DebugVehicleTracksDrawer(Hitbox hitbox)
		{
			foreach (IntVec2 cell in hitbox.Cells)
			{
				List<IntVec3> hitboxCells = new List<IntVec3>();
				int x = Vehicle.Position.x;
				int z = Vehicle.Position.z;
				switch (Vehicle.FullRotation.AsInt)
				{
					case 0:
						x += cell.x;
						z += cell.z;
						break;
					case 1:
						x += cell.z;
						z += -cell.x;
						break;
					case 2:
						x += -cell.x;
						z += -cell.z;
						break;
					case 3:
						x += -cell.z;
						z += cell.x;
						break;
					case 4:
						x += cell.z;
						z += -cell.x;
						break;
					case 5:
						x += cell.z;
						z += -cell.x;
						break;
					case 6:
						x += -cell.z;
						z += cell.x;
						break;
					case 7:
						x += -cell.z;
						z += cell.x;
						break;
				}
				hitboxCells.Add(new IntVec3(x, 0, z));
				GenDraw.DrawFieldEdges(hitboxCells, Color.white, AltitudeLayer.MetaOverlays.AltitudeFor());
			}

			foreach (IntVec3 cell in WarningRect().Cells)
			{
				GenDraw.DrawFieldEdges(new List<IntVec3>() { cell }, Color.red);
			}
		}

		private CellRect WarningRect()
		{
			int opDirection = 1;
			if (Vehicle.Rotation == Rot4.South)
			{
				opDirection = -1;
			}
			else if (Vehicle.Rotation == Rot4.West)
			{
				opDirection = -1;
			}

			int x = Vehicle.Position.x;
			int z = Vehicle.Position.z;

			if (Vehicle.Rotation.IsHorizontal)
			{
				x += Vehicle.def.Size.z * opDirection;
			}
			else
			{
				z += Vehicle.def.Size.z * opDirection;
			}

			int sizeX = Vehicle.def.Size.x;
			int sizeZ = Vehicle.def.Size.z;
			if (Vehicle.Rotation.IsHorizontal)
			{
				int tmp = sizeX;
				sizeX = sizeZ;
				sizeZ = tmp;
			}
			return CellRect.CenteredOn(new IntVec3(x, 0, z), sizeX, sizeZ);
		}
	}
}
