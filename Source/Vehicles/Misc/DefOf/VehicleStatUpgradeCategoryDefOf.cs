using RimWorld;

namespace Vehicles;

// ReSharper disable InconsistentNaming
[DefOf]
public class VehicleStatUpgradeCategoryDefOf
{
  // Fuel
  public static StatUpgradeCategoryDef FuelCapacity;
  public static StatUpgradeCategoryDef FuelConsumptionRate;
  public static StatUpgradeCategoryDef ChargeRate;
  public static StatUpgradeCategoryDef DischargeRate;

  // World Map
  public static StatUpgradeCategoryDef WorldSpeedMultiplier;
  public static StatUpgradeCategoryDef OffRoadMultiplier;
  public static StatUpgradeCategoryDef WinterCostMultiplier;

  // Combat
  public static StatUpgradeCategoryDef PawnCollisionMultiplier;
  public static StatUpgradeCategoryDef PawnCollisionRecoilMultiplier;
  public static StatUpgradeCategoryDef BuildingCollisionMultiplier;
  public static StatUpgradeCategoryDef BuildingCollisionRecoilMultiplier;

  static VehicleStatUpgradeCategoryDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(VehicleStatUpgradeCategoryDefOf));
  }
}