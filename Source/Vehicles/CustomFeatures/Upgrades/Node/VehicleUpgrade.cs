using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace Vehicles;

[PublicAPI]
public class VehicleUpgrade : Upgrade
{
  public List<ArmorUpgrade> armor;

  public List<HealthUpgrade> health;

  public List<RoleUpgrade> roles;

  public RetextureDef retextureDef;

  public override bool UnlockOnLoad => true;

  public override IEnumerable<UpgradeTextEntry> UpgradeDescription(VehiclePawn vehicle)
  {
    if (!armor.NullOrEmpty())
    {
      foreach (ArmorUpgrade armorUpgrade in armor)
      {
        if (!armorUpgrade.key.NullOrEmpty() && !armorUpgrade.statModifiers.NullOrEmpty())
        {
          VehicleComponent component = vehicle.statHandler.GetComponent(armorUpgrade.key);
          switch (armorUpgrade.type)
          {
            case UpgradeType.Add:
              foreach (UpgradeTextEntry textEntry in armorUpgrade.UpgradeEntries(vehicle,
                component))
              {
                yield return textEntry;
              }
              break;
            case UpgradeType.Set:
              component.SetArmorModifiers[node.key] = armorUpgrade.statModifiers;
              break;
          }
        }
      }
    }
  }

  public override void Unlock(VehiclePawn vehicle, bool unlockingPostLoad)
  {
    if (!roles.NullOrEmpty()) //Roles are serialized as VehicleHandler, no need to re-upgrade
    {
      foreach (RoleUpgrade roleUpgrade in roles)
      {
        UpgradeRole(vehicle, roleUpgrade, false, unlockingPostLoad);
      }
    }
    if (retextureDef != null && !unlockingPostLoad)
    {
      vehicle.SetRetexture(retextureDef);
    }
    if (!armor.NullOrEmpty())
    {
      foreach (ArmorUpgrade armorUpgrade in armor)
      {
        if (!armorUpgrade.key.NullOrEmpty() && !armorUpgrade.statModifiers.NullOrEmpty())
        {
          VehicleComponent component = vehicle.statHandler.GetComponent(armorUpgrade.key);
          switch (armorUpgrade.type)
          {
            case UpgradeType.Add:
              component.AddArmorModifiers[node.key] = armorUpgrade.statModifiers;
              break;
            case UpgradeType.Set:
              component.SetArmorModifiers[node.key] = armorUpgrade.statModifiers;
              break;
          }
        }
      }
    }
    if (!health.NullOrEmpty())
    {
      foreach (HealthUpgrade healthUpgrade in health)
      {
        if (healthUpgrade.key.NullOrEmpty())
          continue;

        VehicleComponent component = vehicle.statHandler.GetComponent(healthUpgrade.key);
        if (healthUpgrade.value.HasValue)
        {
          switch (healthUpgrade.type)
          {
            case UpgradeType.Add:
              component.AddHealthModifiers[node.key] = healthUpgrade.value.Value;
              break;
            case UpgradeType.Set:
              component.SetHealthModifier = healthUpgrade.value.Value;
              break;
            default:
              throw new NotImplementedException(nameof(UpgradeType));
          }
          component.SetHealth(component.MaxHealth);
        }

        if (healthUpgrade.depth != null)
          component.depthOverride = healthUpgrade.depth;
      }
    }
  }

  public override void Refund(VehiclePawn vehicle)
  {
    if (!roles.NullOrEmpty())
    {
      foreach (RoleUpgrade roleUpgrade in roles)
      {
        UpgradeRole(vehicle, roleUpgrade, true, false);
      }
    }
    if (retextureDef != null)
    {
      vehicle.SetRetexture(null);
    }
    if (!armor.NullOrEmpty())
    {
      foreach (ArmorUpgrade armorUpgrade in armor)
      {
        if (!armorUpgrade.key.NullOrEmpty() && !armorUpgrade.statModifiers.NullOrEmpty())
        {
          VehicleComponent component = vehicle.statHandler.GetComponent(armorUpgrade.key);
          switch (armorUpgrade.type)
          {
            case UpgradeType.Add:
              component.AddArmorModifiers.Remove(node.key);
              break;
            case UpgradeType.Set:
              component.SetArmorModifiers.Remove(node.key);
              break;
          }
        }
      }
    }
    if (!health.NullOrEmpty())
    {
      foreach (HealthUpgrade healthUpgrade in health)
      {
        if (healthUpgrade.key.NullOrEmpty())
          continue;

        VehicleComponent component = vehicle.statHandler.GetComponent(healthUpgrade.key);
        if (healthUpgrade.value.HasValue)
        {
          switch (healthUpgrade.type)
          {
            case UpgradeType.Add:
              component.AddHealthModifiers.Remove(node.key);
              break;
            case UpgradeType.Set:
              component.SetHealthModifier = -1;
              break;
            default:
              throw new NotImplementedException(nameof(UpgradeType));
          }
          component.SetHealth(component.MaxHealth);
        }
        if (healthUpgrade.depth != null)
          component.depthOverride = null;
      }
    }
  }

  public void UpgradeRole(VehiclePawn vehicle, RoleUpgrade roleUpgrade, bool isRefund,
    bool unlockingAfterLoad)
  {
    // XOR operation for inverse behavior of removal upgrades
    bool needsRemoval = roleUpgrade.remove ^ isRefund;
    if (needsRemoval)
    {
      VehicleRoleHandler handler = vehicle.GetHandler(roleUpgrade.key);
      if (!roleUpgrade.editKey.NullOrEmpty())
      {
        if (handler == null)
        {
          Log.Error($"Unable to edit {roleUpgrade.editKey}. Matching VehicleRole not found.");
          return;
        }
        handler.role.RemoveUpgrade(roleUpgrade);
      }
      else if (!unlockingAfterLoad)
      {
        if (handler == null)
        {
          Log.Error($"Unable to remove {roleUpgrade.key} from {vehicle.Name}. Role not found.");
          return;
        }
        vehicle.RemoveRole(roleUpgrade.key);
      }
    }
    else
    {
      VehicleRoleHandler handler = vehicle.GetHandler(roleUpgrade.key);
      if (!roleUpgrade.editKey.NullOrEmpty())
      {
        if (handler == null)
        {
          Log.Error($"Unable to edit {roleUpgrade.editKey}. Matching VehicleRole not found.");
          return;
        }
        handler.role.AddUpgrade(roleUpgrade);
      }
      else if (!unlockingAfterLoad)
      {
        if (handler != null)
        {
          Log.Error(
            "Attempting to create new role with existing key. If the upgrade is for modifying an existing role, an editKey must be specified.");
          return;
        }
        if (vehicle.VehicleDef.GetRole(roleUpgrade.key) is { } roleReference)
        {
          vehicle.AddRole(new VehicleRole(roleReference));
        }
        else
        {
          vehicle.AddRole(RoleUpgrade.RoleFromUpgrade(roleUpgrade));
        }
      }
    }
  }

  public struct ArmorUpgrade
  {
    public string key;
    public List<StatModifier> statModifiers;

    public UpgradeType type;

    public IEnumerable<UpgradeTextEntry> UpgradeEntries(VehiclePawn vehicle,
      VehicleComponent component)
    {
      if (!statModifiers.NullOrEmpty())
      {
        foreach (StatModifier statModifier in statModifiers)
        {
          string valueFormatted = UpgradeTextEntry.FormatValue(statModifier.value, type,
            statModifier.stat.toStringStyle, statModifier.stat.toStringNumberSense,
            statModifier.stat.formatString);
          yield return new UpgradeTextEntry(
            $"{statModifier.stat.LabelCap} ({component.props.label})", valueFormatted,
            UpgradeEffectType.Positive);
        }
      }
    }
  }

  public struct HealthUpgrade
  {
    public string key;
    public int? value;

    public VehicleComponent.VehiclePartDepth? depth;

    public UpgradeType type;

    public IEnumerable<UpgradeTextEntry> UpgradeEntries(VehiclePawn vehicle,
      VehicleComponent component)
    {
      if (value != null)
      {
        string valueFormatted =
          UpgradeTextEntry.FormatValue(value.Value, type, ToStringStyle.Integer);
        yield return new UpgradeTextEntry($"{component.props.label}", valueFormatted,
          UpgradeEffectType.Positive);
      }
      if (depth != null)
      {
        yield return new UpgradeTextEntry($"{component.props.label}",
          $"{"Depth".Translate()} = {depth.Value.Translate()}");
      }
    }
  }

  [PublicAPI]
  public class RoleUpgrade
  {
    public string key;
    public string label = "[MissingLabel]";
    public string editKey;

    public bool remove;

    //Operating
    public HandlingType? handlingTypes;
    public int? slots;
    public int? slotsToOperate;
    public float? comfort;
    public List<string> turretIds;

    //Damaging
    public ComponentHitbox hitbox;
    public bool? exposed;
    public float? chanceToHit;

    //Rendering
    public PawnOverlayRenderer pawnRenderer;

    public static VehicleRole RoleFromUpgrade(RoleUpgrade upgrade)
    {
      VehicleRole role = new()
      {
        key = upgrade.key,
        label = upgrade.label
      };
      role.CopyFrom(upgrade);
      return role;
    }
  }
}