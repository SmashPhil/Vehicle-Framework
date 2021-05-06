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
	/// <summary>
	/// Saveable List item for use with dropdowns
	/// </summary>
	public class SaveableListEntry : SaveableField
	{
		public int index;

		public SaveableListEntry()
		{
		}

		public SaveableListEntry(VehicleDef def, FieldInfo field, int index) : base(def, field)
		{
			this.index = index;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref index, "index");
		}
	}
}
