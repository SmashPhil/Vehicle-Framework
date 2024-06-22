using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public static class ThingDefGenerator_Buildables
	{
		//They removed the DefOf for this, and I can't be bothered to add it myself. Just doing a def lookup
		private const string DefaultDesignationCategoryDefName = "Structure";

		public static bool GenerateImpliedBuildDef(VehicleDef vehicleDef, out VehicleBuildDef impliedBuildDef)
		{
			impliedBuildDef = null;
			if (vehicleDef.buildDef is null)
			{
				Log.Warning($"[{vehicleDef}] Implied generation for vehicles is incomplete. Please define the VehicleBuildDef separately to avoid improper vehicle generation.");
				impliedBuildDef = new VehicleBuildDef
				{
					defName = $"{vehicleDef.defName}_Blueprint",
					label = vehicleDef.label,
					description = vehicleDef.description,

					thingClass = typeof(VehicleBuilding),
					thingToSpawn = vehicleDef,
					selectable = vehicleDef.selectable,
					altitudeLayer = vehicleDef.altitudeLayer,
					terrainAffordanceNeeded = vehicleDef.terrainAffordanceNeeded,
					constructEffect = vehicleDef.constructEffect ?? EffecterDefOf.ConstructMetal,
					leaveResourcesWhenKilled = vehicleDef.leaveResourcesWhenKilled,
					passability = vehicleDef.passability,
					fillPercent = vehicleDef.fillPercent,
					neverMultiSelect = true,
					designationCategory = vehicleDef.designationCategory ?? DefDatabase<DesignationCategoryDef>.GetNamed(DefaultDesignationCategoryDefName),
					clearBuildingArea = true,
					category = ThingCategory.Building,
					blockWind = vehicleDef.blockWind,
					useHitPoints = true,

					rotatable = vehicleDef.rotatable,
					statBases = vehicleDef.statBases,
					size = vehicleDef.size,
					researchPrerequisites = vehicleDef.researchPrerequisites,
					costList = vehicleDef.costList,

					soundImpactDefault = vehicleDef.soundImpactDefault,
					soundBuilt = vehicleDef.soundBuilt,

					graphicData = new GraphicData(),

					building = vehicleDef.building ?? new BuildingProperties()
					{
						canPlaceOverImpassablePlant = false,
						paintable = false
					}
				};
				vehicleDef.designationCategory = null; //Purge designation category from non-buildable VehiclePawn
				impliedBuildDef.graphicData.CopyFrom(vehicleDef.graphicData);
				Type graphicClass = vehicleDef.graphicData.drawRotated ? typeof(Graphic_Multi) : typeof(Graphic_Single);
				impliedBuildDef.graphicData.graphicClass = graphicClass;
				vehicleDef.buildDef = impliedBuildDef;
				return true;
			}
			return false;
		}
	}
}
