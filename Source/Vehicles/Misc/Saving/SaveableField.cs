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
	public class SaveableField : IExposable
	{
		public string name;
		public Type classType;

		private int defHashCode;

		public SaveableField()
		{
		}

		public SaveableField(Def def, FieldInfo field)
		{
			name = field.Name;
			classType = field.DeclaringType;
			defHashCode = def.GetHashCode();
		}

		public FieldInfo FieldInfo => SettingsCache.GetCachedField(classType, name);

		public virtual void ResolveReferences()
		{
		}

		public static SaveableField SaveableFieldFor(Def def, FieldInfo field)
		{
			return new SaveableField(def, field);
		}

		public static implicit operator FieldInfo(SaveableField field)
		{
			return field.FieldInfo;
		}

		public override bool Equals(object obj)
		{
			return obj is SaveableField field && Equals(this, field);
		}

		public static bool Equals(SaveableField source, SaveableField target)
		{
			return source.GetHashCode() == target.GetHashCode();
		}

		public override int GetHashCode()
		{
			return Gen.HashCombine(defHashCode, Gen.HashCombine(Gen.HashCombine(0, classType.FullName), name));
		}

		public virtual void ExposeData()
		{
			Scribe_Values.Look(ref name, "name");
			Scribe_Values.Look(ref classType, "classType");
			Scribe_Values.Look(ref defHashCode, "defHashCode", -1);
		}
	}
}
