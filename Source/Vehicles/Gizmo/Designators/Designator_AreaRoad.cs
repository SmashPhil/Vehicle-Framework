using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using static Vehicles.Designator_AreaRoad;

namespace Vehicles
{
	public abstract class Designator_AreaRoad : Designator_Cells
	{
		private readonly DesignateMode mode;
		private static RoadType roadType = RoadType.Prioritize;

		public override bool DragDrawMeasurements => true;

		public override int DraggableDimensions => 2;

		public Designator_AreaRoad(DesignateMode mode)
		{
			this.mode = mode;
			useMouseIcon = true;
		}

		public override void ProcessInput(Event ev)
		{
			if (!CheckCanInteract())
			{
				return;
			}
			if (mode == DesignateMode.Add)
			{
				List<FloatMenuOption> options = new List<FloatMenuOption>
				{
					RoadTypeOption("VF_RoadType_Prioritize".Translate(), RoadType.Prioritize),
					RoadTypeOption("VF_RoadType_Avoid".Translate(), RoadType.Avoid)
				};
				Find.WindowStack.Add(new FloatMenu(options));
			}
			base.ProcessInput(ev);

			FloatMenuOption RoadTypeOption(string label, RoadType roadType)
			{
				return new FloatMenuOption(label, delegate ()
				{
					Designator_AreaRoad.roadType = roadType;
					base.ProcessInput(ev);
				}, priority: MenuOptionPriority.Low);
			}
		}

		public override void DesignateSingleCell(IntVec3 cell)
		{
			if (mode == DesignateMode.Add)
			{
				switch (roadType)
				{
					case RoadType.Prioritize:
						Map.areaManager.Get<Area_Road>()[cell] = true;
						Map.areaManager.Get<Area_RoadAvoidal>()[cell] = false;
						break;
					case RoadType.Avoid:
						Map.areaManager.Get<Area_Road>()[cell] = false;
						Map.areaManager.Get<Area_RoadAvoidal>()[cell] = true;
						break;
				}
				return;
			}
			Map.areaManager.Get<Area_Road>()[cell] = false;
			Map.areaManager.Get<Area_RoadAvoidal>()[cell] = false;
		}

		public override AcceptanceReport CanDesignateCell(IntVec3 cell)
		{
			if (!cell.InBounds(Map))
			{
				return false;
			}
			bool road = Map.areaManager.Get<Area_Road>()[cell];
			bool avoidal = Map.areaManager.Get<Area_RoadAvoidal>()[cell];
			if (mode == DesignateMode.Add)
			{
				return roadType switch
				{
					RoadType.Prioritize => !road,
					RoadType.Avoid => !avoidal,
					_ => true,
				};
			}
			return road || avoidal;
		}

		public override void SelectedUpdate()
		{
			GenUI.RenderMouseoverBracket();
			Map.areaManager.Get<Area_Road>().MarkForDraw();
			Map.areaManager.Get<Area_RoadAvoidal>().MarkForDraw();
		}

		public enum RoadType : byte
		{
			None,
			Prioritize,
			Avoid
		}
	}
}
