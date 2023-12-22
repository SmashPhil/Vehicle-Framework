using System.Collections.Generic;
using Verse;
using SmashTools;

namespace Vehicles
{
	[HeaderTitle(Label = "VF_LauncherProperties", Translate = true)]
	public class CompProperties_VehicleLauncher : VehicleCompProperties
	{
		[PostToSettings(Label = "VF_FuelConsumptionWorld", Translate = true, Tooltip = "VF_FuelConsumptionWorldTooltip", UISettingsType = UISettingsType.FloatBox, VehicleType = VehicleType.Air)]
		[NumericBoxValues(MinValue = 0)]
		public float fuelConsumptionWorldMultiplier = 10;

		//[PostToSettings(Label = "VF_RateOfClimb", Translate = true, Tooltip = "VF_RateOfClimbTooltip", UISettingsType = UISettingsType.SliderFloat, VehicleType = VehicleType.Air)]
		[SliderValues(MinValue = 1, MaxValue = 200, EndValue = 999999f, RoundDecimalPlaces = 0, Increment = 1, MaxValueDisplay = "VF_Instant")]
		public float rateOfClimb = 10f;

		//[PostToSettings(Label = "VF_MaxFallRate", Translate = true, Tooltip = "VF_MaxFallRateTooltip", UISettingsType = UISettingsType.SliderFloat, VehicleType = VehicleType.Air)]
		[SliderValues(MinValue = 1, MaxValue = 101, EndValue = 100000, RoundDecimalPlaces = 1, Increment = 1, MaxValueDisplay = "VF_Instant")]
		public float maxFallRate = 20;

		//[PostToSettings(Label = "VF_MaxAltitude", Translate = true, Tooltip = "VF_MaxAltitudeTooltip", UISettingsType = UISettingsType.IntegerBox, VehicleType = VehicleType.Air)]
		[NumericBoxValues(MinValue = AltitudeMeter.MinimumAltitude, MaxValue = AltitudeMeter.MaximumAltitude)]
		public int maxAltitude = 10000;

		//[PostToSettings(Label = "VehicleLandingAltitude", Translate = true, Tooltip = "VehicleLandingAltitudeTooltip", UISettingsType = UISettingsType.IntegerBox, VehicleType = VehicleType.Air)]
		[NumericBoxValues(MinValue = AltitudeMeter.MinimumAltitude, MaxValue = AltitudeMeter.MaximumAltitude)]
		public int landingAltitude = 1000;

		[PostToSettings(Label = "VF_ReconDistance", Translate = true, Tooltip = "VF_ReconDistanceTooltip", UISettingsType = UISettingsType.SliderInt, VehicleType = VehicleType.Air)]
		[SliderValues(MinValue = 1, MaxValue = 8)]
		[DisableSettingConditional(MemberType = typeof(CompProperties_VehicleLauncher), Field = nameof(controlInFlight), DisableIfEqualTo = false)]
		public int reconDistance = 1;

		[PostToSettings(Label = "VF_LaunchFixedMaxDistance", Translate = true, Tooltip = "VF_LaunchFixedMaxDistanceTooltip", UISettingsType = UISettingsType.SliderInt, VehicleType = VehicleType.Air)]
		[SliderValues(MinValue = 0, MaxValue = 100, MinValueDisplay = "VF_LaunchFixedMaxDistanceDisabled")]
		public int fixedLaunchDistanceMax = 0;

		[PostToSettings(Label = "VF_ControlInFlight", Translate = true, Tooltip = "VF_ControlInFlightTooltip", UISettingsType = UISettingsType.Checkbox, VehicleType = VehicleType.Air)]
		public bool controlInFlight = true;

		[PostToSettings(Label = "VF_SpaceFlight", Translate = true, Tooltip = "VF_SpaceFlightTooltip", UISettingsType = UISettingsType.Checkbox, VehicleType = VehicleType.Air)]
		[DisableSettingConditional(MayRequireAny = new string[] { CompatibilityPackageIds.SOS2, CompatibilityPackageIds.RimNauts, CompatibilityPackageIds.Universum })]
		public bool spaceFlight = false;

		public bool faceDirectionOfTravel = true;
		public bool circleToLand = true;
		public int deployTicks = 0;

		public string shadow = "Things/Skyfaller/SkyfallerShadowCircle";

		public BomberProperties bombing;
		public StrafingProperties strafing;

		public LaunchProtocol launchProtocol;

		public ThingDef skyfallerLeaving;
		public ThingDef skyfallerIncoming;
		public ThingDef skyfallerCrashing;
		public ThingDef skyfallerStrafing;
		public ThingDef skyfallerBombing;

		public CompProperties_VehicleLauncher()
		{
			compClass = typeof(CompVehicleLauncher);
		}

		public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
		{
			foreach (string error in base.ConfigErrors(parentDef))
			{
				yield return error;
			}
		}

		public override IEnumerable<VehicleStatDef> StatCategoryDefs()
		{
			yield return VehicleStatDefOf.FlightControl;
			yield return VehicleStatDefOf.FlightSpeed;
		}
	}
}
