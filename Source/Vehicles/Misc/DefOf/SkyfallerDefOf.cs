using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace Vehicles
{
	[DefOf]
	public static class SkyfallerDefOf
	{
		static SkyfallerDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(SkyfallerDefOf));
		}

		public static ThingDef ProjectileSkyfaller;

		public static AirdropDef AirdropPackage;

		public static AirdropDef AirdropParatrooper;
	}
}
