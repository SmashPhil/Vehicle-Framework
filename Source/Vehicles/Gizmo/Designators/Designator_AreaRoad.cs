using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public abstract class Designator_AreaRoad : Designator_Cells
	{
		private readonly DesignateMode mode;

		public override bool DragDrawMeasurements => true;

		public override int DraggableDimensions => 2;

		public Designator_AreaRoad(DesignateMode mode)
		{
			this.mode = mode;
			useMouseIcon = true;
		}

		public override void DesignateSingleCell(IntVec3 cell)
		{
			if (mode == DesignateMode.Add)
			{
				Map.areaManager.Get<Area_Road>()[cell] = true;
				return;
			}
			Map.areaManager.Get<Area_Road>()[cell] = false;
		}

		public override AcceptanceReport CanDesignateCell(IntVec3 loc)
		{
			if (!loc.InBounds(Map))
			{
				return false;
			}
			bool enabled = Map.areaManager.Get<Area_Road>()[loc];
			if (mode == DesignateMode.Add)
			{
				return !enabled;
			}
			return enabled;
		}

		public override void SelectedUpdate()
		{
			GenUI.RenderMouseoverBracket();
			Map.areaManager.Get<Area_Road>().MarkForDraw();
		}
	}
}
