using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public static class AerialAnimationEvents
	{
		public static void ShakeCamera(LaunchProtocol launchProtocol, float magnitude)
		{
			Find.CameraDriver.shaker.DoShake(magnitude);
		}
	}
}
