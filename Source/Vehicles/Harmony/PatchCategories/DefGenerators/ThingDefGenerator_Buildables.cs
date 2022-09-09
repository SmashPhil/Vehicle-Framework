using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public static class ThingDefGenerator_Buildables
	{
		public static bool GenerateImpliedBuildDef(VehicleDef vehicleDef, out VehicleBuildDef impliedBuildDef)
		{
			impliedBuildDef = null;
			if (vehicleDef.buildDef is null)
			{
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
					constructEffect = vehicleDef.constructEffect,
					leaveResourcesWhenKilled = vehicleDef.leaveResourcesWhenKilled,
					passability = vehicleDef.passability,
					fillPercent = vehicleDef.fillPercent,
					neverMultiSelect = true,
					designationCategory = vehicleDef.designationCategory,
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

					graphicData = new GraphicData()
				};
				impliedBuildDef.graphicData.CopyFrom(vehicleDef.graphicData);
				impliedBuildDef.graphicData.graphicClass = vehicleDef.graphicData.drawRotated ? typeof(Graphic_Single) : typeof(Graphic_Multi);
				return true;
			}
			return false;
		}
	}
}
