using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Vehicles
{
	public class LaunchProtocolProperties
	{
		public float speed = 1f;

		public Rot4? forcedRotation;

		public int maxTicks = 250;

		public int delayByTicks;

		public bool reversed;

		public List<GraphicDataLayered> additionalLaunchTextures;

		public List<GraphicDataLayered> additionalLandingTextures;

		public SimpleCurve xPositionCurve;

		public SimpleCurve zPositionCurve;

		public SimpleCurve angleCurve;

		public SimpleCurve rotationCurve;

		public SimpleCurve speedCurve;
	}
}
