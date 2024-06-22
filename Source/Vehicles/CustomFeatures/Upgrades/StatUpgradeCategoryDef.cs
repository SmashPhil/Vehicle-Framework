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
		public ToStringStyle toStringStyle = ToStringStyle.FloatTwo;
		public ToStringNumberSense toStringNumberSense = ToStringNumberSense.Absolute;
		[MustTranslate]
		public string formatString;

		public UpgradeEffectType upgradeEffectType = UpgradeEffectType.Positive;
	}
}
