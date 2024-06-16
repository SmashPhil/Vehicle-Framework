using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Vehicles
{
	public static class TranslationHelper
	{
		public static string Translate(this VehicleComponent.VehiclePartDepth depth)
		{
			return depth switch
			{
				VehicleComponent.VehiclePartDepth.Undefined => "VF_Depth_Undefined".Translate(),
				VehicleComponent.VehiclePartDepth.External => "VF_Depth_External".Translate(),
				VehicleComponent.VehiclePartDepth.Internal => "VF_Depth_Internal".Translate(),
				_ => throw new NotImplementedException(),
			};
		}
	}
}
