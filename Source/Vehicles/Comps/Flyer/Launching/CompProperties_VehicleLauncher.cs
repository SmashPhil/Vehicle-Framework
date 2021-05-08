using System.Collections.Generic;
using Verse;
using SmashTools;
using Vehicles.Defs;

namespace Vehicles
{
	[HeaderTitle(Label = "VehicleLauncherProperties", Translate = true)]
	public class CompProperties_VehicleLauncher : VehicleCompProperties
	{
		[PostToSettings(Label = "VehicleFuelEfficiencyWorld", Translate = true, Tooltip = "VehicleFuelEfficiencyWorldTooltip", UISettingsType = UISettingsType.SliderPercent, VehicleType = VehicleType.Air)]
		[SliderValues(MinValue = 0.05f, MaxValue = 1f, Increment = 0.05f, RoundDecimalPlaces = 2, EndSymbol = "%")]
		public float fuelEfficiencyWorld = 0.1f;
		[PostToSettings(Label = "VehicleFlySpeed", Translate = true, Tooltip = "VehicleFlySpeedTooltip", UISettingsType = UISettingsType.SliderFloat, VehicleType = VehicleType.Air)]
		[SliderValues(MinValue = 0.5f, MaxValue = 20.5f, EndValue = 999999f, RoundDecimalPlaces = 1, Increment = 0.5f, MaxValueDisplay = "VehicleTeleportation")]
		public float flySpeed = 1.5f;

		[PostToSettings(Label = "VehicleRateOfClimb", Translate = true, Tooltip = "VehicleRateOfClimbTooltip", UISettingsType = UISettingsType.SliderFloat, VehicleType = VehicleType.Air)]
		[SliderValues(MinValue = 1, MaxValue = 200, EndValue = 999999f, RoundDecimalPlaces = 0, Increment = 1, MaxValueDisplay = "VehicleInstant")]
		public float rateOfClimb = 10f;
		[PostToSettings(Label = "VehicleMaxFallRate", Translate = true, Tooltip = "VehicleMaxFallRateTooltip", UISettingsType = UISettingsType.SliderFloat, VehicleType = VehicleType.Air)]
		[SliderValues(MinValue = 1, MaxValue = 101, EndValue = 100000, RoundDecimalPlaces = 1, Increment = 1, MaxValueDisplay = "VehicleInstant")]
		public float maxFallRate = 20;

		[PostToSettings(Label = "VehicleMaxAltitude", Translate = true, Tooltip = "VehicleMaxAltitudeTooltip", UISettingsType = UISettingsType.IntegerBox, VehicleType = VehicleType.Air)]
		[NumericBoxValues(MinValue = AltitudeMeter.MinimumAltitude, MaxValue = AltitudeMeter.MaximumAltitude)]
		public int maxAltitude = 10000;
		[PostToSettings(Label = "VehicleLandingAltitude", Translate = true, Tooltip = "VehicleLandingAltitudeTooltip", UISettingsType = UISettingsType.IntegerBox, VehicleType = VehicleType.Air)]
		[NumericBoxValues(MinValue = AltitudeMeter.MinimumAltitude, MaxValue = AltitudeMeter.MaximumAltitude)]
		public int landingAltitude = 200;

		[PostToSettings(Label = "VehicleReconDistance", Translate = true, Tooltip = "VehicleReconDistanceTooltip", UISettingsType = UISettingsType.SliderInt, VehicleType = VehicleType.Air)]
		[SliderValues(MinValue = 1, MaxValue = 8)]
		[DisableSetting]
		public int reconDistance = 1;
		[PostToSettings(Label = "VehicleLaunchFixedMaxDistance", Translate = true, Tooltip = "VehicleLaunchFixedMaxDistanceTooltip", UISettingsType = UISettingsType.SliderInt, VehicleType = VehicleType.Air)]
		[SliderValues(MinValue = 0, MaxValue = 100, MinValueDisplay = "VehicleLaunchFixedMaxDistanceDisabled")]
		public int fixedLaunchDistanceMax = 0;

		[PostToSettings(Label = "VehicleControlInFlight", Translate = true, Tooltip = "VehicleControlInFlightTooltip", UISettingsType = UISettingsType.Checkbox, VehicleType = VehicleType.Air)]
		public bool controlInFlight = true;

		public bool faceDirectionOfTravel = true;
		public bool circleToLand = true;

		public BomberProperties bomber;

		public List<LaunchProtocol> launchProtocols;

		public ThingDef skyfallerLeaving;
		public ThingDef skyfallerIncoming;
		public ThingDef skyfallerCrashing;

		public CompProperties_VehicleLauncher()
		{
			compClass = typeof(CompVehicleLauncher);
		}

		public override IEnumerable<VehicleStatCategoryDef> StatCategoryDefs()
		{
			yield return VehicleStatCategoryDefOf.StatCategoryFlightSpeed;
			yield return VehicleStatCategoryDefOf.StatCategoryFlightControl;
		}

		public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
		{
			foreach (string error in base.ConfigErrors(parentDef))
			{
				yield return error;
			}

			if (skyfallerLeaving is null)
			{
				yield return "Must populate <field>skyfallerLeaving</field>".ConvertRichText();
			}
			if (skyfallerIncoming is null)
			{
				yield return "Must populate <field>skyfallerIncoming</field>".ConvertRichText();
			}
			if (skyfallerCrashing is null)
			{
				yield return "Must populate <field>skyfallerCrashing</field>".ConvertRichText();
			}
		}
	}
}
