using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmashTools
{
	public static class Ext_LinearCurve
	{
		public static bool NullOrEmpty(this LinearCurve linearCurve)
		{
			return linearCurve is null || linearCurve.points.NullOrEmpty();
		}
	}
}
