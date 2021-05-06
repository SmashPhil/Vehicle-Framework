using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;

namespace Vehicles
{
	public abstract class StatUpgradeCategoryDef : Def
	{
		public StatUpgradeCategoryDef()
		{
		}

		public FloatRange? settingListerRange;

		public virtual Texture2D StatFillableBar { get; }
		public virtual Texture2D StatFillableBarAdded { get; }

		public abstract bool AppliesToVehicle(VehicleDef def);

		public abstract void ApplyStatUpgrade(VehiclePawn vehicle, float value);

		public abstract void DrawStatLister(VehicleDef def, Listing_Settings lister, SaveableField field, float value);
	}
}
