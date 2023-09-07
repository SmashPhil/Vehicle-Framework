using SmashTools;

namespace Vehicles
{
	public class RecoilProperties
	{
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		public float distanceTotal = 0.75f;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		public float distancePerTick = 0.15f;
		[TweakField(SettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 2, Increment = 0.05f, RoundDecimalPlaces = 2)]
		public float speedMultiplierPostRecoil = 0.25f;
	}
}
