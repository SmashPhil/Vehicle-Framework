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
	public class FireMode : IEquatable<FireMode>
	{
		public string label;
		public string texPath;

		[TweakField(SettingsType = UISettingsType.IntegerBox)]
		public int shotsPerBurst;
		[TweakField(SettingsType = UISettingsType.IntegerBox)]
		public int ticksBetweenShots;
		[TweakField(SettingsType = UISettingsType.IntegerBox)]
		public int ticksBetweenBursts;
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
						if(icon is null)
						{
							icon = BaseContent.BadTex;
						}
					}
				}
				return icon;
			}
		}

		public bool IsValid
		{
			get
			{
				return shotsPerBurst > 0;
			}
		}

		public (float timeBetweenBursts, float timeBetweenShots) GetRelativeTime()
		{
			return (ticksBetweenBursts / 60f, ticksBetweenShots / 60f);
		}

		public static bool operator ==(FireMode fm1, FireMode fm2) => fm1.Equals(fm2);
		public static bool operator !=(FireMode fm1, FireMode fm2) => !fm1.Equals(fm2);

		public override bool Equals(object obj)
		{
			return obj is FireMode fireMode && Equals(fireMode);
		}

		public bool Equals(FireMode fireMode2)
		{
			return fireMode2.ticksBetweenBursts == ticksBetweenBursts && fireMode2.shotsPerBurst == shotsPerBurst && fireMode2.burstsTillWarmup == burstsTillWarmup && fireMode2.ticksBetweenShots == ticksBetweenShots;
		}

		public override int GetHashCode()
		{
			return Gen.HashCombineInt(Gen.HashCombineInt(Gen.HashCombineInt(Gen.HashCombineInt(0, ticksBetweenBursts), shotsPerBurst), burstsTillWarmup), ticksBetweenShots);
		}
	}
}
