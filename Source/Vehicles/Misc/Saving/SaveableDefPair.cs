using System;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using HarmonyLib;

namespace Vehicles
{
	public class SaveableDefPair<K> : SaveableField where K : Def
	{
		[Unsaved]
		public K key;

		public string defName;

		public string uniqueKey;

		public LookMode valueLookMode;

		protected Type keyType;
		protected Type valueType;

		public SaveableDefPair()
		{
		}

		public SaveableDefPair(VehicleDef def, FieldInfo field, string uniqueKey, K key, LookMode valueLookMode) : base(def, field)
		{
			this.key = key;
			defName = key.defName;
			this.uniqueKey = uniqueKey;
			this.valueLookMode = valueLookMode;
			keyType = typeof(K);
		}

		public override void ResolveReferences()
		{
			key = DefDatabase<K>.GetNamed(defName);
		}

		public override int GetHashCode()
		{
			return Gen.HashCombine(base.GetHashCode(), Gen.HashCombine(0, uniqueKey));
		}
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref uniqueKey, "uniqueKey");
			Scribe_Values.Look(ref valueLookMode, "valueLookMode");
			Scribe_Values.Look(ref defName, "defName");
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				keyType = typeof(K);
			}
		}
	}
}
