using System.Globalization;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// FireMode selection option for VehicleTurret
	/// </summary>
	/// <remarks>XML Notation: (shotsPerBurst, ticksBetweenShots, ticksBetweenBursts, label, texPath)</remarks>
	public record class FireMode
	{
		public string label;
		public string texPath;

		[TweakField(SettingsType = UISettingsType.IntegerBox)]
		public IntRange shotsPerBurst;
		[TweakField(SettingsType = UISettingsType.IntegerBox)]
		public int ticksBetweenShots;
		[TweakField(SettingsType = UISettingsType.IntegerBox)]
		public IntRange ticksBetweenBursts;
		[TweakField(SettingsType = UISettingsType.IntegerBox)]
		public int burstsTillWarmup = 1;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		public float spreadRadius;

		private Texture2D icon;

		public Texture2D Icon
		{
			get
			{
				if (icon is null)
				{
					if (!string.IsNullOrEmpty(texPath))
					{
						icon = ContentFinder<Texture2D>.Get(texPath);
						if (icon is null)
						{
							icon = BaseContent.BadTex;
						}
					}
				}
				return icon;
			}
		}

		public int RoundsPerMinute
		{
			get
			{
				if (ticksBetweenBursts.TrueMin > ticksBetweenShots)
				{
					float roundsPerSecond = 60f / ticksBetweenShots;
					float secondsPerBurst = shotsPerBurst.Average / roundsPerSecond;
					float totalBurstCycle = secondsPerBurst + ticksBetweenBursts.TrueMin.TicksToSeconds();
					float burstsPerMinute = 60f / totalBurstCycle;
					return Mathf.RoundToInt(burstsPerMinute * shotsPerBurst.Average);
				}
				return Mathf.RoundToInt(3600f / ticksBetweenShots);
			}
		}

		public bool IsValid
		{
			get
			{
				return shotsPerBurst.TrueMin > 0;
			}
		}

		public override int GetHashCode()
		{
			return Gen.HashCombineInt(Gen.HashCombineInt(Gen.HashCombineInt(Gen.HashCombineInt(0, ticksBetweenBursts.GetHashCode()), 
				shotsPerBurst.GetHashCode()), burstsTillWarmup), ticksBetweenShots);
		}
	}
}
