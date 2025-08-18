using JetBrains.Annotations;
using UnityEngine;
using Vehicles.Rendering;
using Verse;

namespace Vehicles;

[PublicAPI]
public static class MoteGenerator
{
	public static void ThrowMote(IntVec3 loc, Map map, MoteThrown mote,
		SaturationPriority priority = SaturationPriority.Low, RenderConditions renderConditions = RenderConditions.Vanilla)
	{
		const float SaturationThresholdHigh = 1.2f;

		switch (priority)
		{
			case SaturationPriority.Low:
				if (map.moteCounter.SaturatedLowPriority)
					return;
			break;
			case SaturationPriority.Normal:
				if (map.moteCounter.Saturated)
					return;
			break;
			case SaturationPriority.High:
				if (map.moteCounter.Saturation > SaturationThresholdHigh)
					return;
			break;
		}

		if (!RenderHelper.ShouldShow(map, loc, renderConditions))
			return;

		GenSpawn.Spawn(mote, loc, map);
	}


	public enum SaturationPriority
	{
		Low,
		Normal,
		High,
		AlwaysShow
	}
}