using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using SmashTools;
using UnityEngine;

namespace Vehicles
{
	public class VerticalProtocolProperties : LaunchProtocolProperties
	{
		public int maxTicksVertical;

		/* ----- Shadows ----- */
		[GraphEditable]
		public LinearCurve shadowSizeXVerticalCurve;
		[GraphEditable]
		public LinearCurve shadowSizeZVerticalCurve;
		[GraphEditable]
		public LinearCurve shadowAlphaVerticalCurve;
		/* --------------------*/

		/* ----- Graphics ----- */
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
		/* ---------------------*/
	}
}
