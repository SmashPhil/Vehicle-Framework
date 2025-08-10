using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using UnityEngine;
using Verse;

namespace Vehicles;

[PublicAPI]
public class VehicleComponent : IExposable, ITweakFields
{
  [Unsaved]
  public readonly VehiclePawn vehicle;

  [TweakField]
  public VehicleComponentProperties props;

  private float health;

  public IndicatorDef indicator;
  public Color highlightColor = Color.white;

  public VehiclePartDepth? depthOverride;

  public VehicleComponent(VehiclePawn vehicle)
  {
    this.vehicle = vehicle;
  }

  public float Health => health;

  public float HealthPercent => (health / MaxHealth).RoundTo(0.01f);

  public float Efficiency =>
    props.efficiency.Evaluate(HealthPercent); //Allow evaluating beyond 100% via stat parts

  public Dictionary<string, List<StatModifier>> SetArmorModifiers { get; private set; } = [];

  public Dictionary<string, List<StatModifier>> AddArmorModifiers { get; private set; } = [];

  public float SetHealthModifier { get; set; } = -1;

  public Dictionary<string, float> AddHealthModifiers { get; private set; } = [];

  public VehiclePartDepth Depth => depthOverride ?? props.depth;

  string ITweakFields.Category => props.key;

  string ITweakFields.Label => props.key;

  public float MaxHealth
  {
    get
    {
      if (SetHealthModifier > 0)
      {
        return SetHealthModifier;
      }
      float value = props.health;
      if (!AddHealthModifiers.NullOrEmpty())
      {
        foreach (float healthModifier in AddHealthModifiers.Values)
        {
          value += healthModifier;
        }
      }
      return value;
    }
  }

  public virtual void DrawIcon(Rect rect)
  {
    if (indicator != null)
    {
      Widgets.DrawTextureFitted(rect, indicator.Icon, 1);
      TooltipHandler.TipRegion(rect, indicator.label);
    }
  }

  public void SetHealth(float health)
  {
    this.health = Mathf.Clamp(health, 0, MaxHealth);
    vehicle?.EventRegistry[VehicleEventDefOf.HealthChanged].ExecuteEvents();
  }

  // TODO 1.7 - Remove vehicle param
  public void TakeDamage(VehiclePawn _, DamageInfo dinfo, bool ignoreArmor = false)
  {
    TakeDamage(null, ref dinfo, ignoreArmor: ignoreArmor);
  }

  // TODO 1.7 - Remove vehicle param
  public virtual Penetration TakeDamage(VehiclePawn _, ref DamageInfo dinfo,
    bool ignoreArmor = false)
  {
    Penetration penetration = Penetration.NonPenetrated;
    if (!ignoreArmor)
    {
      ReduceDamageFromArmor(ref dinfo, out penetration);
    }

    // EMP Damage always penetrates
    if (dinfo.Def == DamageDefOf.EMP)
    {
      penetration = Penetration.Electrified;
    }

    health -= dinfo.Amount;
    float remainingDamage = Mathf.Clamp(-health, 0, float.MaxValue);
    health = Mathf.Clamp(health, 0, MaxHealth);

    if (dinfo.Amount > 0)
    {
      if (!props.reactors.NullOrEmpty())
      {
        foreach (Reactor reactor in props.reactors)
        {
          reactor.Hit(vehicle, this, ref dinfo, penetration);
        }
      }

      vehicle.EventRegistry[VehicleEventDefOf.DamageTaken].ExecuteEvents();
      vehicle.EventRegistry[VehicleEventDefOf.HealthChanged].ExecuteEvents();

      if (vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed) <= 0.1f)
        vehicle.ignition.Drafted = false;
      // TODO - should destroy the vehicle even unspawned, and eject to caravan or 'lose' pawns if in aerial vehicle
      if (vehicle.Spawned && vehicle.GetStatValue(VehicleStatDefOf.BodyIntegrity) < 0.01f)
        vehicle.Kill(dinfo);
    }

    if (penetration == Penetration.Electrified)
    {
      // Damage is based on %, penetrated damage should be set to 0 for post-emp calculations
      dinfo.SetAmount(0);
    }
    else if (penetration < Penetration.Penetrated || health > (MaxHealth / 2f))
    {
      //Don't fallthrough until part is below 50% health or damage has penetrated
      dinfo.SetAmount(0);
    }
    else if (penetration == Penetration.Penetrated ||
      (remainingDamage > 0 && remainingDamage >= dinfo.Amount / 2))
    {
      //If penetrated or remaining damage is above half original damage amount
      dinfo.SetAmount(remainingDamage);
    }
    return penetration;
  }

  /// <returns>Amount to stun vehicle for</returns>
  public int ApplyEMPDamage(ref DamageInfo dinfo, ref bool adapted)
  {
    adapted = false;
    if (!vehicle.VehicleDef.properties.empStuns)
    {
      return 0;
    }

    VehicleEMPSeverity severity = props.empSeverity;
    if (severity == VehicleEMPSeverity.None)
    {
      return 0;
    }

    int stunTicks = 0;
    float chanceToStun = DamageHelper.EMPChanceToStun(severity);
    if (Rand.Chance(chanceToStun))
    {
      // Applied in VehicleStatHandler, will only apply the highest duration of all 'stunned' components in this in
      stunTicks = DamageHelper.EMPStunLength(severity, dinfo.Amount);
      float damage = DamageHelper.EMPStunDamage(severity);
      dinfo.SetAmount(damage * MaxHealth);
      TakeDamage(vehicle, dinfo, ignoreArmor: true);
    }
    return stunTicks;
  }

  public virtual void HealComponent(float amount)
  {
    float oldHealth = health;
    health = Mathf.Clamp(health + amount, 0, MaxHealth);
    if (!Mathf.Approximately(oldHealth, health))
    {
      vehicle.EventRegistry[VehicleEventDefOf.Repaired].ExecuteEvents();
      vehicle.EventRegistry[VehicleEventDefOf.HealthChanged].ExecuteEvents();
    }
  }

  public virtual void ReduceDamageFromArmor(ref DamageInfo dinfo, out Penetration result)
  {
    result = Penetration.NonPenetrated;
    if (dinfo.Def.armorCategory == null)
      return;

    DamageArmorCategoryDef armorCategoryDef = dinfo.Def.armorCategory;
    float armorRating = ArmorRating(armorCategoryDef, out _);
    float armorDiff = armorRating - dinfo.ArmorPenetrationInt;

    result = props.hitbox.fallthrough ? Penetration.Penetrated : Penetration.NonPenetrated;

    float chance = Rand.Value;
    if (chance < (armorDiff / 2))
    {
      dinfo.SetAmount(0);
      result = Penetration.Deflected;
    }
    else
    {
      if (chance < armorDiff)
      {
        float damage = GenMath.RoundRandom(dinfo.Amount / 2);
        dinfo.SetAmount(damage);
        result = Penetration.Diminished;
        if (dinfo.Def.armorCategory == DamageArmorCategoryDefOf.Sharp)
        {
          dinfo.Def = DamageDefOf.Blunt;
        }
      }
    }
  }

  /// <summary>
  /// Pulls armor rating of component for <paramref name="armorCategoryDef"/>.  If armor rating is not specified, defaults to vehicles overall armor rating for that armor category.
  /// </summary>
  /// <returns>armor rating %</returns>
  public float ArmorRating(DamageArmorCategoryDef armorCategoryDef, out float upgraded)
  {
    float baseValue = vehicle.GetStatValue(armorCategoryDef.armorRatingStat);

    upgraded = 0;
    if (!SetArmorModifiers.NullOrEmpty())
    {
      foreach (List<StatModifier> statModifiers in SetArmorModifiers.Values)
      {
        if (TryGetModifier(statModifiers, out float setValue))
        {
          upgraded = setValue - baseValue;
          return setValue;
        }
      }
    }

    float value = baseValue +
      vehicle.statHandler.GetUpgradeableStatValue(armorCategoryDef.armorRatingStat);

    StatModifier armorModifier =
      props.armor?.FirstOrDefault(rating => rating.stat == armorCategoryDef.armorRatingStat);
    if (armorModifier != null)
    {
      baseValue =
        armorModifier.value; //Part-specific armor does not apply vehicle-wide armor upgrades
      value = baseValue;
    }
    if (!AddArmorModifiers.NullOrEmpty())
    {
      foreach (List<StatModifier> statModifiers in AddArmorModifiers.Values)
      {
        if (TryGetModifier(statModifiers, out float addValue))
        {
          value += addValue;
        }
      }
    }
    upgraded = value - baseValue;
    return value;

    bool TryGetModifier(List<StatModifier> statModifiers, out float statModifierValue)
    {
      statModifierValue = 0;
      if (statModifiers.NullOrEmpty())
      {
        return false;
      }
      foreach (StatModifier statModifier in statModifiers)
      {
        if (statModifier.stat == armorCategoryDef.armorRatingStat)
        {
          statModifierValue = statModifier.value;
          return true;
        }
      }
      return false;
    }
  }

  public virtual void PostCreate()
  {
    health = MaxHealth;
  }

  public virtual void Initialize(VehicleComponentProperties props)
  {
    this.props = props;
    indicator = props.reactors?.FirstOrDefault(reactor => reactor.indicator != null)?.indicator;
    highlightColor = props.reactors?.FirstOrDefault()?.highlightColor ?? Color.white;
  }

  public virtual void ExposeData()
  {
    Scribe_Values.Look(ref health, nameof(health));
  }

  void ITweakFields.OnFieldChanged()
  {
  }

  // Similar functionality to BodyPartDepth
  public enum VehiclePartDepth
  {
    Undefined,
    External,
    Internal
  }

  public enum Penetration
  {
    NonPenetrated,
    Deflected,
    Diminished,
    Penetrated,
    Electrified,
  }

  public struct DamageResult
  {
    public Penetration penetration;

    public DamageInfo damageInfo;
    public IntVec2 cell;
  }
}