using SmashTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public class Area_Road : Area
	{
		public Area_Road()
		{
		}

		public Area_Road(AreaManager areaManager) : base(areaManager)
		{
		}

		public override string Label => "VF_RoadZone".Translate();

		public override int ListPriority => 9000;

		public override Color Color
		{
			get
			{
				return new ColorInt(0, 0, 0).ToColor;
			}
		}

		public override string GetUniqueLoadID()
		{
			return $"Area_{ID}_VehicleRoad";
		}
	}
}
