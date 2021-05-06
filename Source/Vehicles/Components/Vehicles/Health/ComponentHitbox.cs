using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace Vehicles
{
	public class ComponentHitbox
	{
		public VehicleComponentPosition side = VehicleComponentPosition.Empty;
		public bool fallthrough = true;
		public List<IntVec2> cells = new List<IntVec2>();

		public List<VehicleComponentPosition> noOverlapWith = new List<VehicleComponentPosition>();

		public List<IntVec2> Hitbox { get; set; }

		public bool Empty => Hitbox.NullOrEmpty();

		public bool Contains(IntVec2 cell) => Hitbox?.Contains(cell) ?? false;

		public bool Contains(IntVec3 cell) => Hitbox?.Contains(new IntVec2(cell.x, cell.z)) ?? false;

		public void Initialize(VehicleDef def)
		{
			if (!cells.NullOrEmpty())
			{
				Hitbox = cells;
			}
			else
			{
				CellRect rect = CellRect.CenteredOn(new IntVec3(0, 0, 0), def.Size.x, def.Size.z);
				List<IntVec3> cells = new List<IntVec3>();
				if (side == VehicleComponentPosition.Body)
				{
					cells = rect.Cells.ToList();
				}
				else if (side == VehicleComponentPosition.BodyNoOverlap)
				{
					foreach (var cell in rect.Cells.Where(c => !def.components.Where(cp => noOverlapWith.Contains(cp.hitbox.side)).Any(cp => cp.hitbox.Contains(c))))
					{
						cells.Add(new IntVec3(cell.x, 0, cell.z));
					}
				}
				else
				{
					cells = rect.GetEdgeCells(RotationFromSide(side)).ToList();
				}
				List<IntVec2> intVec2s = new List<IntVec2>();
				foreach (IntVec3 cell in cells)
				{
					intVec2s.Add(new IntVec2(cell.x, cell.z));
				}
				Hitbox = intVec2s;
			}
		}

		public static Rot4 RotationFromSide(VehicleComponentPosition pos)
		{
			return pos switch
			{
				VehicleComponentPosition.Front => Rot4.North,
				VehicleComponentPosition.Right => Rot4.East,
				VehicleComponentPosition.Back => Rot4.South,
				VehicleComponentPosition.Left => Rot4.West,
				_ => Rot4.Invalid
			};
		}
	}
}
