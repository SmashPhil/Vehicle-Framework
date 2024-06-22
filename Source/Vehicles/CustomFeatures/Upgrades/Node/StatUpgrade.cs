using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;
using static Vehicles.StatUpgrade;
using static System.Net.Mime.MediaTypeNames;

namespace Vehicles
{
	public class StatUpgrade : Upgrade
	{
		public List<StatDefUpgrade> stats;

		public List<VehicleStatDefUpgrade> vehicleStats;

		public List<StatCategoryUpgrade> statCategories;

		public override bool UnlockOnLoad => true;

		public override IEnumerable<UpgradeTextEntry> UpgradeDescription(VehiclePawn vehicle)
		{
			if (!stats.NullOrEmpty())
			{
				foreach (StatDefUpgrade statDefUpgrade in stats)
				{
					switch (statDefUpgrade.type)
					{
						case UpgradeType.Add:
							yield return new UpgradeTextEntry(statDefUpgrade.def.LabelCap, statDefUpgrade.ValueFormatted, statDefUpgrade.value, UpgradeEffectType.Positive);
							break;
						case UpgradeType.Set:
							yield return new UpgradeTextEntry(statDefUpgrade.def.LabelCap, statDefUpgrade.value.ToString());
							break;
					}
				}
			}
			if (!vehicleStats.NullOrEmpty())
			{
				foreach (VehicleStatDefUpgrade vehicleStatDefUpgrade in vehicleStats)
				{
					switch (vehicleStatDefUpgrade.type)
					{
						case UpgradeType.Add:
							yield return new UpgradeTextEntry(vehicleStatDefUpgrade.def.LabelCap, vehicleStatDefUpgrade.ValueFormatted, vehicleStatDefUpgrade.value, vehicleStatDefUpgrade.def.upgradeEffectType);
							break;
						case UpgradeType.Set:
							yield return new UpgradeTextEntry(vehicleStatDefUpgrade.def.LabelCap, vehicleStatDefUpgrade.value.ToString());
							break;
					}
				}
			}
			if (!statCategories.NullOrEmpty())
			{
				foreach (StatCategoryUpgrade statCategoryUpgrade in statCategories)
				{
					switch (statCategoryUpgrade.type)
					{
						case UpgradeType.Add:
							yield return new UpgradeTextEntry(statCategoryUpgrade.def.LabelCap, statCategoryUpgrade.ValueFormatted, statCategoryUpgrade.value, statCategoryUpgrade.def.upgradeEffectType);
							break;
						case UpgradeType.Set:
							yield return new UpgradeTextEntry(statCategoryUpgrade.def.LabelCap, statCategoryUpgrade.value.ToString());
							break;
					}
				}
			}
		}

		public override void Unlock(VehiclePawn vehicle, bool unlockingPostLoad)
		{
			if (!stats.NullOrEmpty())
			{
				foreach (StatDefUpgrade statDefUpgrade in stats)
				{
					switch (statDefUpgrade.type)
					{
						case UpgradeType.Add:
							vehicle.statHandler.AddUpgradeableStatValue(statDefUpgrade.def, statDefUpgrade.value);
							break;
						case UpgradeType.Set:
							vehicle.statHandler.SetUpgradeableStatValue(node.key, statDefUpgrade.def, statDefUpgrade.value);
							break;
					}
				}
			}
			if (!vehicleStats.NullOrEmpty())
			{
				foreach (VehicleStatDefUpgrade vehicleStatDefUpgrade in vehicleStats)
				{
					switch (vehicleStatDefUpgrade.type)
					{
						case UpgradeType.Add:
							vehicle.statHandler.AddStatOffset(vehicleStatDefUpgrade.def, vehicleStatDefUpgrade.value);
							break;
						case UpgradeType.Set:
							vehicle.statHandler.SetStatOffset(node.key, vehicleStatDefUpgrade.def, vehicleStatDefUpgrade.value);
							break;
					}
				}
			}
			if (!statCategories.NullOrEmpty())
			{
				foreach (StatCategoryUpgrade statCategoryUpgrade in statCategories)
				{
					switch (statCategoryUpgrade.type)
					{
						case UpgradeType.Add:
							vehicle.statHandler.AddStatOffset(statCategoryUpgrade.def, statCategoryUpgrade.value);
							break;
						case UpgradeType.Set:
							vehicle.statHandler.SetStatOffset(node.key, statCategoryUpgrade.def, statCategoryUpgrade.value);
							break;
					}
				}
			}
		}

		public override void Refund(VehiclePawn vehicle)
		{
			if (!stats.NullOrEmpty())
			{
				foreach (StatDefUpgrade statDefUpgrade in stats)
				{
					switch (statDefUpgrade.type)
					{
						case UpgradeType.Add:
							vehicle.statHandler.SubtractUpgradeableStatValue(statDefUpgrade.def, statDefUpgrade.value);
							break;
						case UpgradeType.Set:
							vehicle.statHandler.RemoveUpgradeableStatValue(node.key, statDefUpgrade.def);
							break;
					}
				}
			}
			if (!vehicleStats.NullOrEmpty())
			{
				foreach (VehicleStatDefUpgrade vehicleStatDefUpgrade in vehicleStats)
				{
					switch (vehicleStatDefUpgrade.type)
					{
						case UpgradeType.Add:
							vehicle.statHandler.SubtractStatOffset(vehicleStatDefUpgrade.def, vehicleStatDefUpgrade.value);
							break;
						case UpgradeType.Set:
							vehicle.statHandler.RemoveStatOffset(node.key, vehicleStatDefUpgrade.def);
							break;
					}
				}
			}
			if (!statCategories.NullOrEmpty())
			{
				foreach (StatCategoryUpgrade statCategoryUpgrade in statCategories)
				{
					switch (statCategoryUpgrade.type)
					{
						case UpgradeType.Add:
							vehicle.statHandler.SubtractStatOffset(statCategoryUpgrade.def, statCategoryUpgrade.value);
							break;
						case UpgradeType.Set:
							vehicle.statHandler.RemoveStatOffset(node.key, statCategoryUpgrade.def);
							break;
					}
				}
			}
		}

		/// <summary>
		/// Anything with Def fields must be a reference type for Cross Reference resolving during parsing
		/// </summary>

		public class StatDefUpgrade
		{
			public StatDef def;
			public float value;

			public UpgradeType type;

			public string ValueFormatted
			{
				get
				{
					string text = value.ToStringByStyle(def.toStringStyle, numberSense: def.toStringNumberSense);
					if (def.toStringNumberSense != ToStringNumberSense.Factor && !def.formatString.NullOrEmpty())
					{
						text = string.Format(def.formatString, text);
					}
					if (type == UpgradeType.Add && value > 0)
					{
						text = "+" + text;
					}
					return text;
				}
			}
		}

		public class VehicleStatDefUpgrade
		{
			public VehicleStatDef def;
			public float value;

			public UpgradeType type;

			public string ValueFormatted
			{
				get
				{
					return UpgradeTextEntry.FormatValue(value, type, def.toStringStyle, def.toStringNumberSense, def.formatString);
				}
			}
		}

		public class StatCategoryUpgrade
		{
			public StatUpgradeCategoryDef def;
			public float value;

			public UpgradeType type;

			public string ValueFormatted
			{
				get
				{
					return UpgradeTextEntry.FormatValue(value, type, def.toStringStyle, def.toStringNumberSense, def.formatString);
				}
			}
		}
	}
}
