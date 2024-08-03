using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public class Area_RoadAvoidal : Area
	{
		public Area_RoadAvoidal()
		{
		}

		public Area_RoadAvoidal(AreaManager areaManager) : base(areaManager)
		{
		}

		public override string Label => "VF_RoadZone".Translate();

		public override int ListPriority => 9000;

		public override Color Color
		{
			get
			{
				return new ColorInt(125, 25, 25).ToColor;
			}
		}

		public override string GetUniqueLoadID()
		{
			return $"Area_{ID}_VehicleRoadAvoidal";
		}
	}
}
