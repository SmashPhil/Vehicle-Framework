using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using SmashTools;

namespace Vehicles
{
	public class ComponentHitbox
	{
		public VehicleComponentPosition side = VehicleComponentPosition.Empty;
		public bool fallthrough = true;
		public List<IntVec2> cells = new List<IntVec2>();

		public List<VehicleComponentPosition> noOverlapWith = new List<VehicleComponentPosition>();

		public List<IntVec2> Hitbox { get; set; }

		public bool Empty { get; private set; }

		public bool Contains(IntVec2 cell) => Hitbox?.Contains(cell) ?? false;

		public bool Contains(IntVec3 cell) => Hitbox?.Contains(new IntVec2(cell.x, cell.z)) ?? false;

		public IntVec2 NearestTo(IntVec2 cell)
		{
			if (Hitbox.Count == 1)
			{
				return Hitbox[0];
			}
			return Hitbox.MinBy(hb => (hb - cell).Magnitude);
		}

		public void Initialize(VehicleDef def)
		{
			if (!cells.NullOrEmpty())
			{
				Hitbox = cells;
				Empty = Hitbox.NullOrEmpty();
			}
			else
			{
				Empty = false;
				CellRect rect = def.VehicleRect(new IntVec3(0, 0, 0), Rot4.North);
				List<IntVec3> cells;
				if (side == VehicleComponentPosition.Body) //TODO - Remove BodyNoOverlap in 1.5
				{
					cells = rect.Cells.ToList();
				}
				else if (side == VehicleComponentPosition.BodyNoOverlap)
				{
					cells = rect.Cells.ToList();
					Log.Warning($"[{def}] BodyNoOverlap is obsolete, specify the cells directly or use Body. This option will be removed in 1.5");
				}
				else if (side != VehicleComponentPosition.Empty)
				{
					cells = rect.GetEdgeCells(RotationFromSide(side)).ToList();
				}
				else
				{
					Empty = true;
					cells = new List<IntVec3>() { IntVec3.Zero }; //If no hitbox provided, default to root position. (Only matters in the case of non-hitbox external components)
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
