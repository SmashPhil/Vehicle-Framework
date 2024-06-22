using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Vehicles
{
	public struct UpgradeTextEntry
	{
		public string label;
		public string description;
		public UpgradeEffectType effectType;

		public UpgradeTextEntry(string label, string description, UpgradeEffectType effectType = UpgradeEffectType.None)
		{
			this.label = label;
			this.description = description;
			this.effectType = effectType;
		}

		public UpgradeTextEntry(string label, string description, float value, UpgradeEffectType effectType)
		{
			this.label = label;
			this.description = description;
			
			switch (effectType)
			{
				case UpgradeEffectType.Positive:
					this.effectType = EffectTypeFromValue(value);
					break;
				case UpgradeEffectType.Negative:
					this.effectType = EffectTypeFromValue(-value); //Inverts value for opposite effect types as Positive
					break;
				default:
					this.effectType = effectType;
					break;
			}
		}

		private static UpgradeEffectType EffectTypeFromValue(float value)
		{
			if (value > 0)
			{
				return UpgradeEffectType.Positive;
			}
			else if (value < 0)
			{
				return UpgradeEffectType.Negative;
			}
			return UpgradeEffectType.None;
		}

		public static string FormatValue(float value, UpgradeType upgradeType, ToStringStyle toStringStyle, ToStringNumberSense toStringNumberSense = ToStringNumberSense.Absolute, string formatString = null)
		{
			string text = value.ToStringByStyle(toStringStyle, numberSense: toStringNumberSense);
			if (toStringNumberSense != ToStringNumberSense.Factor && !formatString.NullOrEmpty())
			{
				text = string.Format(formatString, text);
			}
			if (upgradeType == UpgradeType.Add && value > 0)
			{
				text = "+" + text;
			}
			return text;
		}
	}
}
