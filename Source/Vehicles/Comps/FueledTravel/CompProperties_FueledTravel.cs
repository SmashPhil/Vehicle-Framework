using System.Collections.Generic;
using JetBrains.Annotations;
using SmashTools;
using UnityEngine;
using Verse;

namespace Vehicles;

[PublicAPI]
[HeaderTitle(Label = "VF_FueledTravelPropertes", Translate = true)]
public class CompProperties_FueledTravel : VehicleCompProperties
{
  public ThingDef fuelType;
  public ThingDef leakDef;

  public bool electricPowered;

  [PostToSettings(Label = "VF_DischargePerTick", Tooltip = "VF_DischargePerTickTooltip",
    Translate = true,
    UISettingsType = UISettingsType.FloatBox)]
  [NumericBoxValues(MinValue = 0)]
  [DisableSettingConditional(MemberType = typeof(CompProperties_FueledTravel),
    Property = nameof(ElectricPowered), DisableIfEqualTo = false,
    DisableReason = "VF_NotElectricPowered")]
  public float dischargeRate = 2;

  [PostToSettings(Label = "VF_TicksPerCharge", Tooltip = "VF_TicksPerCharge", Translate = true,
    UISettingsType = UISettingsType.FloatBox)]
  [NumericBoxValues(MinValue = 0)]
  [DisableSettingConditional(MemberType = typeof(CompProperties_FueledTravel),
    Property = nameof(ElectricPowered), DisableIfEqualTo = false,
    DisableReason = "VF_NotElectricPowered")]
  public float chargeRate = 1;

  [PostToSettings(Label = "VF_FuelConsumptionRate", Tooltip = "VF_FuelConsumptionRateTooltip",
    Translate = true, UISettingsType = UISettingsType.FloatBox)]
  public float fuelConsumptionRate;

  [PostToSettings(Label = "VF_FuelCapacity", Tooltip = "VF_FuelCapacityTooltip", Translate = true,
    UISettingsType = UISettingsType.IntegerBox)]
  public int fuelCapacity;

  [PostToSettings(Label = "VF_FuelConsumptionRateWorldMultiplier",
    Tooltip = "VF_FuelConsumptionRateWorldMultiplierTooltip", Translate = true,
    UISettingsType = UISettingsType.SliderFloat)]
  [SliderValues(MinValue = 0, MaxValue = 2, RoundDecimalPlaces = 1)]
  public float fuelConsumptionWorldMultiplier = 1;

  [PostToSettings(Label = "VF_AutoRefuelPercent", Tooltip = "VF_AutoRefuelPercentTooltip",
    Translate = true, UISettingsType = UISettingsType.SliderFloat)]
  [SliderValues(MinValue = 0, MaxValue = 1, RoundDecimalPlaces = 2)]
  public float autoRefuelPercent = 1;

  [PostToSettings(Label = "VF_TargetFuelConfigurable", Tooltip = "VF_TargetFuelConfigurableTooltip",
    Translate = true, UISettingsType = UISettingsType.Checkbox)]
  public bool targetFuelLevelConfigurable = true;

  [PostToSettings(Label = "VF_AmbientHeat", Tooltip = "VF_AmbientHeatTooltip", Translate = true,
    UISettingsType = UISettingsType.FloatBox)]
  public float ambientHeat;

  [MustTranslate]
  public string gizmoLabel;

  public FuelConsumptionCondition fuelConsumptionCondition = FuelConsumptionCondition.Drafted |
    FuelConsumptionCondition.Moving | FuelConsumptionCondition.Flying;

  public List<OffsetMote> motesGenerated;

  public ThingDef moteDisplayed;

  public int ticksToSpawnMote;

  public string fuelIconPath;

  private Texture2D fuelIcon;

  public CompProperties_FueledTravel()
  {
    compClass = typeof(CompFueledTravel);
  }

  public bool ElectricPowered => electricPowered;

  public string GizmoLabel
  {
    get
    {
      if (!gizmoLabel.NullOrEmpty())
        return gizmoLabel;
      return electricPowered ? "VF_Electricity".Translate() : "Fuel".Translate();
    }
  }

  public Texture2D FuelIcon
  {
    get
    {
      fuelIcon ??= !fuelIconPath.NullOrEmpty() ?
        ContentFinder<Texture2D>.Get(fuelIconPath) :
        fuelType.uiIcon;
      return fuelIcon;
    }
  }
}