using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;

namespace Vehicles
{
	public class StatUpgradeCategoryDef : Def
	{
		public StatUpgradeCategoryDef()
		{
		}

		public FloatRange? settingListerRange;

		public virtual void DrawStatLister(VehicleDef def, Listing_Settings lister, SaveableField field, float value)
		{
		}
	}
}
