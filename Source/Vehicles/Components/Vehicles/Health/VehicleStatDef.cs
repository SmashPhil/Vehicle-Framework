using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles;

[PublicAPI]
public class VehicleStatDef : Def, IDefIndex<VehicleStatDef>
{
  public float defaultBaseValue;
  public float minValue = float.MinValue;
  public float maxValue = float.MaxValue;

  public float hideAtValue = float.NaN;
  public bool alwaysHide;
  public bool showIfUndefined;
  public bool neverDisabled;
  public bool showZeroBaseValue;
  public bool applyFactorsIfNegative = true;

  public List<VehicleStatDef> statFactors;
  public List<VehicleStatPart> parts;
  public SettingsValueInfo modSettingsInfo;
  public StatCategoryDef category;
  public List<string> showIfModsLoaded;
  public List<VehicleType> showOnVehicleTypes;

  public string formatString;
  public ToStringStyle toStringStyle = ToStringStyle.Integer;
  public ToStringStyle? toStringStyleUnfinalized;
  public ToStringNumberSense toStringNumberSense = ToStringNumberSense.Absolute;
  public EfficiencyOperationType operationType = EfficiencyOperationType.None;
  public UpgradeEffectType upgradeEffectType = UpgradeEffectType.Positive;

  public SimpleCurve postProcessCurve;
  public List<VehicleStatDef> postProcessStatFactors;

  public Type workerClass = typeof(VehicleStatWorker);
  public int displayPriorityInCategory = 1;

  [MustTranslate]
  public string formatStringUnfinalized;

  [Unsaved]
  private VehicleStatWorker statWorker;

  public int DefIndex { get; set; }

  public VehicleStatWorker Worker
  {
    get
    {
      if (statWorker == null)
      {
        if (!parts.NullOrEmpty())
        {
          foreach (VehicleStatPart statPart in parts)
          {
            statPart.statDef = this;
          }
        }
        statWorker = (VehicleStatWorker)Activator.CreateInstance(workerClass);
        statWorker.InitStatWorker(this);
      }
      return statWorker;
    }
  }

  public ToStringStyle ToStringStyleUnfinalized
  {
    get { return toStringStyleUnfinalized ?? toStringStyle; }
  }

  public override void PostLoad()
  {
    modSettingsInfo.minValue = minValue;
    modSettingsInfo.maxValue = maxValue;
  }

  public string ValueToString(float val, bool finalized = true,
    ToStringNumberSense numberSense = ToStringNumberSense.Absolute)
  {
    return Worker.ValueToString(val, finalized, numberSense);
  }

  public bool CanShowWithLoadedMods()
  {
    if (!showIfModsLoaded.NullOrEmpty())
    {
      foreach (string packageId in showIfModsLoaded)
      {
        if (!Ext_Mods.HasActiveMod(packageId))
        {
          return false;
        }
      }
    }
    return true;
  }

  public bool CanShowWithVehicle(VehicleDef vehicleDef)
  {
    if (!showOnVehicleTypes.NullOrEmpty())
    {
      return showOnVehicleTypes.Contains(vehicleDef.type);
    }
    return true;
  }
}