using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using SmashTools;
using SmashTools.Animations;
using Verse;

namespace Vehicles
{
	// Short hashes can be used directly for getting / setting
	// parameter values in AnimationManager.
	public static class PropertyIds
	{
		public static readonly ushort Moving;
		public static readonly ushort Disabled;
		public static readonly ushort IgnitionOn;

		public static readonly ushort Takeoff;
		public static readonly ushort Landing;
		public static readonly ushort Loiter;

		static PropertyIds()
		{
			Moving = AnimationParameterDefOf.VF_VehicleIsMoving.shortHash;
			Disabled = AnimationParameterDefOf.VF_VehicleIsDisabled.shortHash;
			IgnitionOn = AnimationParameterDefOf.VF_VehicleIsIgnitionOn.shortHash;

			Takeoff = AnimationParameterDefOf.VF_VehicleIsTakingOff.shortHash;
			Landing = AnimationParameterDefOf.VF_VehicleIsLanding.shortHash;
			Loiter = AnimationParameterDefOf.VF_VehicleIsLoitering.shortHash;
		}
	}

	[DefOf]
	public static class AnimationParameterDefOf
	{
		// General
		public static AnimationParameterDef VF_VehicleIsMoving;
		public static AnimationParameterDef VF_VehicleIsDisabled;
		public static AnimationParameterDef VF_VehicleIsIgnitionOn;

		// Aerial
		public static AnimationParameterDef VF_VehicleIsTakingOff;
		public static AnimationParameterDef VF_VehicleIsLanding;
		public static AnimationParameterDef VF_VehicleIsLoitering;

		static AnimationParameterDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(AnimationParameterDefOf));
		}
	}
}
