using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class LaunchProtocolProperties
	{
		public int maxTicks = 250;
		public int delayByTicks;
		public Rot4? forcedRotation;

		public Rot4 flipHorizontal = Rot4.Invalid;
		public Rot4 flipVertical = Rot4.Invalid;
		public Rot4 flipRotation = Rot4.Invalid;

		public bool renderShadow = true;
		public bool lockShadowX = false;
		public bool lockShadowZ = false;

		public LaunchRestriction restriction;

		public List<GraphicDataLayered> additionalTextures;
		public List<AnimationEvent> events;

		[GraphEditable(FunctionOfT = true)]
		public LinearCurve offsetCurve;
		[GraphEditable]
		public LinearCurve xPositionCurve;
		[GraphEditable]
		public LinearCurve zPositionCurve;
		[GraphEditable]
		public LinearCurve rotationCurve;

		[GraphEditable]
		public LinearCurve shadowSizeXCurve;
		[GraphEditable]
		public LinearCurve shadowSizeZCurve;
		[GraphEditable]
		public LinearCurve shadowAlphaCurve;

		[GraphEditable(Prefix = "Fleck")]
		public List<FleckData> fleckData;
	}
}
