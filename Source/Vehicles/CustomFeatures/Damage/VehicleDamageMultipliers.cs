using System;
using System.Globalization;
using SmashTools;

namespace Vehicles
{
	[HeaderTitle(Label = "VehicleDamageMultipliers", Translate = true)]
	public struct VehicleDamageMultipliers
	{
		[PostToSettings(Label = "MeleeDamageMultiplier", Translate = true, UISettingsType = UISettingsType.SliderPercent)]
		[SliderValues(MinValue = 0, MaxValue = 2.5f, MinValueDisplay = "VehicleDamageInvulnerable", EndSymbol = "%")]
		public float meleeDamageMultiplier;
		[PostToSettings(Label = "RangedDamageMultiplier", Translate = true, UISettingsType = UISettingsType.SliderPercent)]
		[SliderValues(MinValue = 0, MaxValue = 2.5f, MinValueDisplay = "VehicleDamageInvulnerable", EndSymbol = "%")]
		public float rangedDamageMultiplier;
		[PostToSettings(Label = "ExplosiveDamageMultiplier", Translate = true, UISettingsType = UISettingsType.SliderPercent)]
		[SliderValues(MinValue = 0, MaxValue = 2.5f, MinValueDisplay = "VehicleDamageInvulnerable", EndSymbol = "%")]
		public float explosiveDamageMultiplier;

		public VehicleDamageMultipliers(float meleeDamageMultiplier, float rangedDamageMultiplier, float explosiveDamageMultiplier)
		{
			this.meleeDamageMultiplier = meleeDamageMultiplier;
			this.rangedDamageMultiplier = rangedDamageMultiplier;
			this.explosiveDamageMultiplier = explosiveDamageMultiplier;
		}

		public static VehicleDamageMultipliers Default => new VehicleDamageMultipliers(0.01f, 0.1f, 10f);

		public static VehicleDamageMultipliers FromString(string entry)
		{ 
			entry = entry.TrimStart(new char[] { '(' }).TrimEnd(new char[] { ')' });
			string[] data = entry.Split(new char[] { ',' });

			try
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				float meleeDamageMultiplier = Convert.ToSingle(data[0], invariantCulture);
				float rangedDamageMultiplier = Convert.ToSingle(data[1], invariantCulture);
				float explosiveDamageMultiplier = Convert.ToSingle(data[2], invariantCulture);
				return new VehicleDamageMultipliers(meleeDamageMultiplier, rangedDamageMultiplier, explosiveDamageMultiplier);
			}
			catch(Exception ex)
			{
				SmashLog.Error($"{entry} is not a valid <struct>VehicleDamageMultipliers</struct> format. Exception: {ex}");
				return Default;
			}
		}
		public override string ToString()
		{
			return $"({meleeDamageMultiplier},{rangedDamageMultiplier},{explosiveDamageMultiplier})";
		}
	}
}
