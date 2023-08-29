using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SmashTools;
using Verse;
using AnimationEvent = SmashTools.AnimationEvent;

namespace Vehicles
{
	public class PropellerProtocolProperties : VerticalProtocolProperties
	{
		public int maxTicksPropeller;

		public List<GraphicDataLayered> additionalTexturesPropeller;
		public List<AnimationEvent> eventsPropeller;

		/* ----- Shadows ----- */
		[GraphEditable]
		public LinearCurve shadowSizeXPropellerCurve;
		[GraphEditable]
		public LinearCurve shadowSizeZPropellerCurve;
		[GraphEditable]
		public LinearCurve shadowAlphaPropellerCurve;
		/* --------------------*/

		/* ----- Graphics ----- */
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
		/* ---------------------*/
	}
}
