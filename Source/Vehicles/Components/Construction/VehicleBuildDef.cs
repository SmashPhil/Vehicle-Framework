using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public class VehicleBuildDef : ThingDef
	{
		public VehicleDef thingToSpawn;
		public SoundDef soundBuilt;

		public SimpleCurve shakeAmountPerAreaCurve = new SimpleCurve
		{
			{
				new CurvePoint(1f, 0.07f),
				true
			},
			{
				new CurvePoint(2f, 0.07f),
				true
			},
			{
				new CurvePoint(4f, 0.1f),
				true
			},
			{
				new CurvePoint(9f, 0.2f),
				true
			},
			{
				new CurvePoint(16f, 0.5f),
				true
			}
		};

		public virtual IEnumerable<VehicleStatDrawEntry> SpecialDisplayStats()
		{
			if (BuildableByPlayer)
			{
				List<TerrainAffordanceDef> terrainAffordances = [];
				if (PlaceWorkers != null)
				{
					terrainAffordances.AddRange(PlaceWorkers.SelectMany((PlaceWorker pw) => pw.DisplayAffordances()));
				}
				TerrainAffordanceDef terrainAffordanceNeed = this.GetTerrainAffordanceNeed();
				if (terrainAffordanceNeed != null)
				{
					terrainAffordances.Add(terrainAffordanceNeed);
				}
				if (terrainAffordances.Count > 0)
				{
					yield return new VehicleStatDrawEntry(StatCategoryDefOf.Building, "TerrainRequirement".Translate(),
						terrainAffordances.Distinct().OrderBy(ta => ta.order).Select(ta => ta.label).ToCommaList(false, false).CapitalizeFirst(),
						"Stat_Thing_TerrainRequirement_Desc".Translate(), 1101);
				}
				tmpCostList.Clear();
				tmpHyperlinks.Clear();
				if (MadeFromStuff && costStuffCount > 0)
				{
					tmpCostList.Add($"{costStuffCount}x {stuffCategories.Select(sc => sc.label).ToCommaListOr()}");
					foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs)
					{
						if (thingDef.IsStuff && !thingDef.stuffProps.categories.NullOrEmpty() &&
							thingDef.stuffProps.categories.Any(stuffCategories.Contains))
						{
							tmpHyperlinks.Add(new Dialog_InfoCard.Hyperlink(thingDef));
						}
					}
				}
				if (!CostList.NullOrEmpty())
				{
					foreach (ThingDefCountClass countClass in CostList)
					{
						tmpCostList.Add(countClass.Summary);
						if (!tmpHyperlinks.Any(hyperlink => hyperlink.def == countClass.thingDef))
						{
							tmpHyperlinks.Add(new Dialog_InfoCard.Hyperlink(countClass.thingDef));
						}
					}
				}
				if (tmpCostList.Any())
				{
					yield return new VehicleStatDrawEntry(StatCategoryDefOf.Building, "Stat_Building_ResourcesToMake".Translate(),
						tmpCostList.ToCommaList().CapitalizeFirst(), "Stat_Building_ResourcesToMakeDesc".Translate(),
						4405, null, tmpHyperlinks);
				}
				float workToBuild = this.GetStatValueAbstract(StatDefOf.WorkToBuild);
				yield return new VehicleStatDrawEntry(StatCategoryDefOf.Building, StatDefOf.WorkToBuild.LabelCap,
						workToBuild.ToString(), StatDefOf.WorkToBuild.description,
						4404, null);
			}
			if (constructionSkillPrerequisite > 0)
			{
				yield return new VehicleStatDrawEntry(StatCategoryDefOf.Building, "SkillRequiredToBuild".Translate(SkillDefOf.Construction.LabelCap),
					constructionSkillPrerequisite.ToString(), "SkillRequiredToBuildExplanation".Translate(SkillDefOf.Construction.LabelCap), 1100);
			}
			if (artisticSkillPrerequisite > 0)
			{
				yield return new VehicleStatDrawEntry(StatCategoryDefOf.Building, "SkillRequiredToBuild".Translate(SkillDefOf.Artistic.LabelCap),
					artisticSkillPrerequisite.ToString(), "SkillRequiredToBuildExplanation".Translate(SkillDefOf.Artistic.LabelCap), 1100);
			}
		}
	}
}
