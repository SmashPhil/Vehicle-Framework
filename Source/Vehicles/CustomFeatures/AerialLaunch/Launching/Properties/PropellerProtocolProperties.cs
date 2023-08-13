using System;
using System.Collections.Generic;
using System.Linq;
using SmashTools;
using Verse;

namespace Vehicles
{
	public class PropellerProtocolProperties : VerticalProtocolProperties
	{
		public int maxTicksPropeller;

		[GraphEditable]
		public LinearCurve angularVelocityPropeller;
		[GraphEditable(FunctionOfT = true)]
		public LinearCurve offsetPropellerCurve;
		[GraphEditable]
		public LinearCurve xPositionPropellerCurve;
		[GraphEditable]
		public LinearCurve zPositionPropellerCurve;
		[GraphEditable]
		public LinearCurve rotationPropellerCurve;

		[GraphEditable(Prefix = "FleckPropeller")]
		public FleckData fleckDataPropeller;
	}
}
