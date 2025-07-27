using System.Reflection;
using DevTools;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine.Assertions;
using Verse;
using static Vehicles.UnitTesting.VehicleGroup;

namespace Vehicles;

internal static class TestDefGenerator
{
  private static readonly FieldInfo FleshField;

  static TestDefGenerator()
  {
    FleshField = AccessTools.Field(typeof(RaceProperties), "fleshType");
  }

  public static VehicleDef CreateTransientVehicleDef(string defName,
    [CanBeNull] MockSettings settings)
  {
    Assert.IsNotNull(FleshField);
    DevLog.WriteVerbose($"Creating transient def {defName}");

    VehicleBuildDef buildDef = new()
    {
      defName = $"{defName}_Blueprint",
      label = $"{settings?.debugLabel ?? defName} Blueprint",
      modContentPack = VehicleMod.content,
      thingClass = typeof(VehicleBuilding),
      terrainAffordanceNeeded = TerrainAffordanceDefOf.Heavy,
      clearBuildingArea = true,
      category = ThingCategory.Building,
      rotatable = true,
      blockWind = true,
      useHitPoints = true,
      graphicData = new GraphicDataRGB
      {
        graphicClass = typeof(Graphic_Single),
        texPath = "Ignore/Vehicles/Land/Tier3_ModernArmor/Tier3_ModernArmor",
      },
      building = new BuildingProperties
      {
        canPlaceOverImpassablePlant = false,
        paintable = false
      }
    };
    VehicleDef def = new()
    {
      defName = defName,
      label = settings?.debugLabel ?? $"{defName}_LABEL",
      modContentPack = VehicleMod.content,
      thingClass = typeof(VehiclePawn),
      category = ThingCategory.Pawn,
      tickerType = TickerType.Normal,
      selectable = true,
      useHitPoints = false,
      properties = settings?.properties ?? new VehicleProperties(),
      drawProperties = settings?.drawProperties ?? new VehicleDrawProperties(),
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
    FleshField.SetValue(def.race, DefDatabase<FleshTypeDef>.GetNamed("MetalVehicle"));
    def.buildDef = buildDef;
    buildDef.thingToSpawn = def;

    if (settings != null)
    {
      def.type = settings.type;
      def.components = settings.components;
    }

    if (settings != null && !settings.comps.NullOrEmpty())
    {
      foreach (CompProperties compProps in settings.comps)
      {
        compProps.ResolveReferences(def);
        def.comps.Add(compProps);
      }
    }

    Assert.IsTrue(VehicleMod.GenerateImpliedDefs(def, false));
    def.PostLoad();
    def.ResolveReferences();
    def.PostDefDatabase();
    return def;
  }
}