using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

internal static class TestDefGenerator
{
  private static readonly FieldInfo fleshField;

  private static readonly GeneratorVehiclePawnKindDef kindDefGenerator = new();

  static TestDefGenerator()
  {
    fleshField = AccessTools.Field(typeof(RaceProperties), "fleshType");
  }

  public static VehicleDef CreateTransientVehicleDef(string defName)
  {
    Assert.IsNotNull(fleshField);
    VehicleBuildDef buildDef = new()
    {
      defName = $"{defName}_Blueprint",
      label = $"{defName}_Blueprint_LABEL",
      modContentPack = VehicleHarmony.VehicleMCP,
      thingClass = typeof(VehicleBuilding),
      terrainAffordanceNeeded = TerrainAffordanceDefOf.Heavy,
      clearBuildingArea = true,
      category = ThingCategory.Building,
      rotatable = true,
      blockWind = true,
      useHitPoints = true,
      building = new BuildingProperties
      {
        canPlaceOverImpassablePlant = false,
        paintable = false
      }
    };
    VehicleDef def = new()
    {
      defName = defName,
      modContentPack = VehicleHarmony.VehicleMCP,
      thingClass = typeof(VehiclePawn),
      category = ThingCategory.Pawn,
      tickerType = TickerType.Normal,
      selectable = true,
      useHitPoints = false,
      properties = new VehicleProperties(),

      graphicData = new GraphicDataRGB
      {
        graphicClass = typeof(Graphic_Vehicle),
        texPath = "Ignore/Vehicles/Land/Tier3_ModernArmor/Tier3_ModernArmor",
      },

      race = new RaceProperties
      {
        body = DefDatabase<BodyDef>.GetNamed("emptyBody"),
        trainability = DefDatabase<TrainabilityDef>.GetNamed("None"),
        thinkTreeMain = DefDatabase<ThinkTreeDef>.GetNamed("Vehicle"),
        thinkTreeConstant = DefDatabase<ThinkTreeDef>.GetNamed("Vehicle_Constant"),
        intelligence = Intelligence.ToolUser,
        needsRest = false,
        hasGenders = false,
        foodType = FoodTypeFlags.None,
        alwaysAwake = true,
        doesntMove = true,

        baseBodySize = 1,

        lifeStageAges =
        [
          new LifeStageAge
          {
            def = DefDatabase<LifeStageDef>.GetNamed("MechanoidFullyFormed"),
            minAge = 0,
          }
        ]
      }
    };
    fleshField.SetValue(def.race, DefDatabase<FleshTypeDef>.GetNamed("MetalVehicle"));

    def.buildDef = buildDef;
    buildDef.thingToSpawn = def;

    Assert.IsTrue(kindDefGenerator.TryGenerateImpliedDef(def, out _, false));

    def.PostLoad();
    def.ResolveReferences();
    def.PostDefDatabase();
    return def;
  }
}