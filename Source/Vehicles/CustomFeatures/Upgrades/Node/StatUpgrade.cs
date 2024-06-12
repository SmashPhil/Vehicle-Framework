using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;
using static Vehicles.StatUpgrade;

namespace Vehicles
{
	public class StatUpgrade : Upgrade
	{
		public List<StatDefUpgrade> stats;

		public List<VehicleStatDefUpgrade> vehicleStats;

		public List<StatCategoryUpgrade> statCategories;

		public override bool UnlockOnLoad => true;

		public override IEnumerable<string> UpgradeDescription
		{
			get
			{
				if (!stats.NullOrEmpty())
				{
					foreach (StatDefUpgrade statDefUpgrade in stats)
					{
						switch (statDefUpgrade.type)
						{
							case UpgradeType.Add:
								yield return $"{statDefUpgrade.def.LabelCap} +{statDefUpgrade.value}";
								break;
							case UpgradeType.Set:
								yield return $"{statDefUpgrade.def.LabelCap} -> {statDefUpgrade.value}";
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
								yield return $"{vehicleStatDefUpgrade.def.LabelCap} +{vehicleStatDefUpgrade.value}";
								break;
							case UpgradeType.Set:
								yield return $"{vehicleStatDefUpgrade.def.LabelCap} -> {vehicleStatDefUpgrade.value}";
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
								yield return $"{statCategoryUpgrade.def.LabelCap} +{statCategoryUpgrade.value}";
								break;
							case UpgradeType.Set:
								yield return $"{statCategoryUpgrade.def.LabelCap} -> {statCategoryUpgrade.value}";
								break;
						}
					}
				}
			}
		}

		public override void Unlock(VehiclePawn vehicle, bool unlockingAfterLoad)
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
		}

		public class VehicleStatDefUpgrade
		{
			public VehicleStatDef def;
			public float value;

			public UpgradeType type;
		}

		public class StatCategoryUpgrade
		{
			public StatUpgradeCategoryDef def;
			public float value;

			public UpgradeType type;
		}
	}
}
