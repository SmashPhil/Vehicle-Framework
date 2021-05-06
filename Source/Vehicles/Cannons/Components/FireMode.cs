using System.Globalization;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using RimWorld;

namespace Vehicles
{
	/// <summary>
	/// FireMode selection option for VehicleTurret
	/// </summary>
	/// <remarks>XML Notation: (shotsPerBurst, ticksBetweenShots, ticksBetweenBursts, label, texPath)</remarks>
	public struct FireMode : IEquatable<FireMode>
	{
		public int shotsPerBurst;
		public int ticksBetweenShots;
		public int ticksBetweenBursts;
		public float spreadRadius;
		public string label;
		public string texPath;

		private Texture2D icon;

		public FireMode(int shotsPerBurst, int ticksBetweenShots, int ticksBetweenBursts, float spreadRadius, string label, string texPath)
		{
			this.shotsPerBurst = shotsPerBurst;
			this.ticksBetweenShots = ticksBetweenShots;
			this.ticksBetweenBursts = ticksBetweenBursts;
			this.spreadRadius = spreadRadius;
			this.label = label;
			this.texPath = texPath;
			icon = null;
		}

		public Texture2D Icon
		{
			get
			{
				if(icon is null)
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

		public static FireMode Invalid
		{
			get
			{
				return new FireMode(-1, -1, -1, -1, "Invalid", string.Empty);
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
			return fireMode2.ticksBetweenBursts == ticksBetweenBursts && fireMode2.shotsPerBurst == shotsPerBurst && fireMode2.ticksBetweenShots == ticksBetweenShots;
		}

		public override int GetHashCode()
		{
			return Gen.HashCombineInt(Gen.HashCombineInt(Gen.HashCombineInt(0, ticksBetweenBursts), shotsPerBurst), ticksBetweenShots);
		}
	}
}
