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

		[GraphEditable(Prefix = "Fleck")]
		public List<FleckData> fleckData;
	}
}
