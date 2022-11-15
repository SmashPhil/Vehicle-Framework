using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VerticalProtocolProperties : LaunchProtocolProperties
	{
		public int maxTicksVertical;

		[GraphEditable(FunctionOfT = true)]
		public LinearCurve offsetVerticalCurve;
		[GraphEditable]
		public LinearCurve xPositionVerticalCurve;
		[GraphEditable]
		public LinearCurve zPositionVerticalCurve;
		[GraphEditable]
		public LinearCurve rotationVerticalCurve;

		[GraphEditable(Prefix = "FleckVTOL")]
		public FleckData fleckDataVertical;
	}
}
