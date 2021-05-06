using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Vehicles
{
	[DefOf]
	public static class RGBShaderTypeDefOf
	{
		static RGBShaderTypeDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(RGBShaderTypeDefOf));
		}

		public static RGBShaderTypeDef CutoutComplexRGB;

		public static RGBShaderTypeDef CutoutComplexPattern;
	}
}
