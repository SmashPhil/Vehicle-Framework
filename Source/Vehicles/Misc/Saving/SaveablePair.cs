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
	public class SaveablePair<K, V> : SaveableField
	{
		public K key;
		public V value;

		public string uniqueKey;

		public LookMode keyLookMode;
		public LookMode valueLookMode;

		protected Type keyType;
		protected Type valueType;

		public SaveablePair()
		{
		}

		public SaveablePair(VehicleDef def, FieldInfo field, string uniqueKey, KeyValuePair<K, V> keyValuePair, LookMode keyLookMode, LookMode valueLookMode) : base(def, field)
		{
			if (keyLookMode == LookMode.Def || valueLookMode == LookMode.Def)
			{
				Log.Warning("Cannot use LookMode.Def for SaveablePair. Would require resolving of Def post-loading. Consider using SaveableDefPair");
				return;
			}
			key = keyValuePair.Key;
			value = keyValuePair.Value;
			this.uniqueKey = uniqueKey;
			this.keyLookMode = keyLookMode;
			this.valueLookMode = valueLookMode;
			keyType = typeof(K);
			valueType = typeof(V);
		}

		public override int GetHashCode()
		{
			return Gen.HashCombine(base.GetHashCode(), Gen.HashCombine(0, uniqueKey));
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref uniqueKey, "uniqueKey");
			Scribe_Values.Look(ref keyLookMode, "keyLookMode");
			Scribe_Values.Look(ref valueLookMode, "valueLookMode");
			if (keyLookMode == LookMode.Def || valueLookMode == LookMode.Def)
			{
				Log.Error("Cannot save LookMode.Def for SaveablePair. Would require resolving of Def post-loading. Consider using SaveableDefPair");
				return;
			}
			Scribe_Universal.Look(ref key, "key", keyLookMode, ref keyType);
			Scribe_Universal.Look(ref value, "value", valueLookMode, ref valueType);
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				keyType = typeof(K);
				valueType = typeof(V);
			}
		}
	}
}
