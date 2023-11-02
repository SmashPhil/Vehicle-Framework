using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public struct Hitbox : IEnumerable<IntVec2>, IEnumerable
	{
		public VehicleComponentPosition side;
		public List<IntVec2> cells;

		public List<IntVec2> Cells { get; set; }

		public void Initialize(VehicleDef def)
		{
			if (!cells.NullOrEmpty())
			{
				Cells = cells;
			}
			else
			{
				CellRect rect = def.VehicleRect(new IntVec3(0, 0, 0), Rot4.North);
				List<IntVec3> cells;
				if (side == VehicleComponentPosition.Body)
				{
					cells = rect.Cells.ToList();
				}
				else if (side != VehicleComponentPosition.Empty)
				{
					cells = rect.GetEdgeCells(ComponentHitbox.RotationFromSide(side)).ToList();
				}
				else
				{
					cells = new List<IntVec3>(); //If no hitbox provided, default to no hitbox.
				}
				List<IntVec2> intVec2s = new List<IntVec2>();
				foreach (IntVec3 cell in cells)
				{
					intVec2s.Add(new IntVec2(cell.x, cell.z));
				}
				Cells = intVec2s;
			}
		}

		public IEnumerator<IntVec2> GetEnumerator()
		{
			foreach (IntVec2 cell in Cells)
			{
				yield return cell;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
