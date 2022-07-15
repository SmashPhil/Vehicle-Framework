using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace Vehicles
{
	public class SpaceObjectDefModExtension : DefModExtension
	{
		//Land -> Space
		public float fixedPercentCostL2S = -1;
		//Space -> Land
		public float fixedPercentCostS2L = -1;
		//Space -> Space
		public float fixedPercentCostS2S = -1;

		//Land -> Space
		public float multiplierCostL2S = -1;
		//Space -> Land
		public float multiplierCostS2L = -1;
		//Space -> Space
		public float multiplierCostS2S = -1;
	}
}
