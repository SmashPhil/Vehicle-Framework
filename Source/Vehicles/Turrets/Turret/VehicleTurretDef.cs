using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using UnityEngine;
using Vehicles.Compatibility;
using Verse;
using Verse.AI;

namespace Vehicles;

[PublicAPI]
public class VehicleTurretDef : Def, ITweakFields
{
  /// <summary>
  /// Type of turret mechanics.
  /// <para>
  /// <see cref="TurretType.Static"/>: Locked in position at the default angle provided.
  /// </para>
  /// <para>
  /// <see cref="TurretType.Rotatable"/>: Can rotate about its center to aim at targets within its firing cone.
  /// </para>
  /// </summary>
  public TurretType turretType = TurretType.Rotatable;

  /// <summary>
  /// List of motes to spawn after the turret fires
  /// </summary>
  public List<AnimationProperties> motes;

  /// <summary>
  /// Ammunition filter for which types of ammo this turret can use.
  /// </summary>
  public ThingFilter ammunition;

  /// <summary>
  /// Number of shots this turret can make before needing to reload.
  /// </summary>
  [TweakField(SettingsType = UISettingsType.IntegerBox)]
  public int magazineCapacity = 1;

  /// <summary>
  /// Amount of ammo needed to reload 1 count in the magazine.
  /// </summary>
  [TweakField(SettingsType = UISettingsType.FloatBox)]
  public float chargePerAmmoCount = 1;

  /// <summary>
  /// This turret uses a non-projectile based ammo such as Steel or Chemfuel.
  /// </summary>
  public bool genericAmmo;

  /// <summary>
  /// Optional properties limiting the number of shots the turret can make before needing to cooldown.
  /// </summary>
  public TurretCooldownProperties cooldown;

  /// <summary>
  /// Optional recoil properties animating the position of the turret after each shot.
  /// </summary>
  [TweakField(SubCategory = "Turret Recoil")]
  public RecoilProperties recoil;

  /// <summary>
  /// Optional recoil properties animating the position of the vehicle after each shot.
  /// </summary>
  [TweakField(SubCategory = "Vehicle Recoil")]
  public RecoilProperties vehicleRecoil;

  /// <summary>
  /// Data for drawing the turret.
  /// </summary>
  [TweakField]
  public GraphicDataRGB graphicData;

  /// <summary>
  /// Additional draw data for layered graphics relative to the base graphicData
  /// </summary>
  [TweakField(SubCategory = "Layered Graphics")]
  public List<VehicleTurretRenderData> graphics;

  /// <summary>
  /// Description of gizmo shown on hover as a tooltip.
  /// </summary>
  public string gizmoDescription;

  /// <summary>
  /// Scale applied to icon drawn in the gizmo.
  /// </summary>
  [TweakField(SettingsType = UISettingsType.FloatBox)]
  public float gizmoIconScale = 1f;

  /// <summary>
  /// Override for turret icon in the gizmo bar. If left empty, the turret will be rendered as the icon instead.
  /// </summary>
  public string gizmoIconTexPath;

  /// <summary>
  /// Apply the same color pattern as the parent vehicle to the turret.
  /// </summary>
  public bool matchParentColor = true;

  /// <summary>
  /// Configuration settings for how the turret cycles shots.
  /// </summary>
  [TweakField(SubCategory = "Fire Modes")]
  public List<FireMode> fireModes = [];

  /// <summary>
  /// Rotatable turret immediately snaps to target while aiming instead of rotating at a fixed speed.
  /// </summary>
  [TweakField(SettingsType = UISettingsType.Checkbox)]
  public bool autoSnapTargeting;

  /// <summary>
  /// Speed (degrees per second) at which the turret rotates to track targets.
  /// </summary>
  [TweakField(SettingsType = UISettingsType.FloatBox)]
  public float rotationSpeed = 1;

  /// <summary>
  /// Seconds for turret rotation to ramp up / slow down to target speed.
  /// </summary>
  [TweakField(SettingsType = UISettingsType.FloatBox)]
  public float rotationDelta;

  /// <summary>
  /// Max range this turret can target to.
  /// </summary>
  [TweakField(SettingsType = UISettingsType.FloatBox)]
  [NumericBoxValues(MinValue = 0, MaxValue = 9999)]
  public float maxRange;

  /// <summary>
  /// Min range this turret can target to.
  /// </summary>
  [TweakField(SettingsType = UISettingsType.FloatBox)]
  [NumericBoxValues(MinValue = 0, MaxValue = 9999)]
  public float minRange;

  /// <summary>
  /// Seconds required to reload this turret.
  /// </summary>
  [TweakField(SettingsType = UISettingsType.FloatBox)]
  public float reloadTimer = 5;

  public LinearCurve reloadTimerMultiplierPerCrewCount;

  /// <summary>
  /// Warmup time before firing the turret. This is after the target has been locked onto and the turret is aligned onto the target.
  /// </summary>
  [TweakField(SettingsType = UISettingsType.FloatBox)]
  public float warmUpTimer = 3;

  /// <summary>
  /// Ratio of ammo for colonists to automatically bring to the vehicle to store internally for ammo reserves.
  /// </summary>
  [TweakField(SettingsType = UISettingsType.FloatBox)]
  public float autoRefuelProportion = 2;

  /// <summary>
  /// EMP damage disables this turret temporarily.
  /// </summary>
  [TweakField(SettingsType = UISettingsType.Checkbox)]
  public bool empDisables;

  /// <summary>
  /// Sound oneshot after this turret shoots.
  /// </summary>
  public SoundDef shotSound;

  /// <summary>
  /// Sound oneshot after this turret begins reloading.
  /// </summary>
  public SoundDef reloadSound;

  /// <summary>
  /// Targeting parameters for configuring how this turret can acquire targets.
  /// </summary>
  public TargetScanFlags targetScanFlags = TargetScanFlags.None;

  /// <summary>
  /// Fixed projectile shot from this turret. If a projectile is supplied through the ammo def, it will use that instead.
  /// </summary>
  /// <remarks>Required field if <see cref="genericAmmo"/> = <see langword="true"/></remarks>
  public ThingDef projectile;

  /// <summary>
  /// Override projectile configuration for when it should impact an entity on its path to the target.
  /// </summary>
  public CustomHitFlags attachProjectileFlag;

  /// <summary>
  /// Forward offset of projectile when shot from the turret.
  /// </summary>
  [TweakField(SettingsType = UISettingsType.FloatBox)]
  public float projectileOffset;

  /// <summary>
  /// Override projectile speed when shot from this turret.
  /// </summary>
  [TweakField(SettingsType = UISettingsType.FloatBox)]
  public float projectileSpeed = -1;

  /// <summary>
  /// Cyclic horizontal offset of projectile when shot from the turret.
  /// </summary>
  public List<float> projectileShifting = [];

  /// <summary>
  /// Restriction worker class
  /// </summary>
  public Type restrictionType;

  string ITweakFields.Label => nameof(VehicleTurretDef);

  string ITweakFields.Category => string.Empty; //$"{defName} (Def)";

  /// <summary>
  /// Used in <see cref="VehicleDef.SpecialDisplayStats(VehiclePawn)"/> for info card.
  /// </summary>
  public virtual IEnumerable<VehicleStatDrawEntry> SpecialDisplayStats(int displayOrder)
  {
    // Description
    yield return new VehicleStatDrawEntry(LabelCap, displayOrder, "Description".Translate(),
      string.Empty, description, 99999,
      hyperlinks: Dialog_InfoCard.DefsToHyperlinks(descriptionHyperlinks));
    // Rotatable
    yield return new VehicleStatDrawEntry(LabelCap, displayOrder, "VF_Rotatable".Translate(),
      (turretType == TurretType.Rotatable).ToStringYesNo(), "VF_RotatableTooltip".Translate(),
      9000);
    // MagazineCapacity (infinity if <= 0)
    string magazineCapacityLabel = magazineCapacity <= 0 ? "\u221E" : magazineCapacity.ToString();
    yield return new VehicleStatDrawEntry(LabelCap, displayOrder,
      "VF_MagazineCapacity".Translate(),
      magazineCapacityLabel, "VF_MagazineCapacityTooltip".Translate(), 6000);
    // Min and Max range
    if (minRange > 0)
    {
      yield return new VehicleStatDrawEntry(LabelCap, displayOrder, "VF_MinRange".Translate(),
        minRange.ToString("F0"), "VF_MinRangeTooltip".Translate(), 5010);
    }
    float maxRangeActual = maxRange <= 0 ? VehicleTurret.DefaultMaxRange : maxRange;
    yield return new VehicleStatDrawEntry(LabelCap, displayOrder, "VF_MaxRange".Translate(),
      maxRangeActual.ToString("F0"), "VF_MaxRangeTooltip".Translate(), 5000);
    // Warmup time
    yield return new VehicleStatDrawEntry(LabelCap, displayOrder, "VF_WarmupTime".Translate(),
      "VF_WarmupTimeValue".Translate(warmUpTimer.ToStringByStyle(ToStringStyle.FloatOne)),
      "VF_WarmupTimeTooltip".Translate(), 4010);
    // Reload time
    yield return new VehicleStatDrawEntry(LabelCap, displayOrder, "VF_ReloadTime".Translate(),
      "VF_ReloadTimeValue".Translate(reloadTimer.ToStringByStyle(ToStringStyle.FloatOne)),
      "VF_ReloadTimeTooltip".Translate(), 4000);

    // RotationSpeed
    string rotationSpeedReadout = autoSnapTargeting ?
      "VF_Instant".Translate() :
      "VF_RotationSpeedValue".Translate(Mathf.RoundToInt(rotationSpeed * 60));
    // rotationSpeed in infoCard is deg/sec (x60 of rotationSpeed) so it's more human readable.
    yield return new VehicleStatDrawEntry(LabelCap, displayOrder, "VF_RotationSpeed".Translate(),
      rotationSpeedReadout, "VF_RotationSpeedTooltip".Translate(), 3000);

    StringBuilder fireModeExplanation = new();
    // FireModes
    foreach (FireMode fireMode in fireModes)
    {
      fireModeExplanation.AppendLine();
      fireModeExplanation.AppendLine(fireMode.label);

      int roundsPerMinute = fireMode.shotsPerBurst.TrueMax > 1 ?
        fireMode.RoundsPerMinute :
        Mathf.RoundToInt(60f / (warmUpTimer + reloadTimer));

      fireModeExplanation.AppendLine(
        $"    {"VF_RateOfFire".Translate()}: {"VF_RateOfFireValue".Translate(RoundsPerMinuteRounded(roundsPerMinute))}");
      if (fireMode.ticksBetweenBursts.TrueMax > fireMode.ticksBetweenShots)
      {
        string shotsPerBurst = fireMode.shotsPerBurst.min == fireMode.shotsPerBurst.max ?
          fireMode.shotsPerBurst.min.ToString() :
          fireMode.shotsPerBurst.ToString();
        fireModeExplanation.AppendLine($"    {"VF_ShotsPerBurst".Translate()}: {shotsPerBurst}");
      }
      fireModeExplanation.AppendLine(
        $"    {"VF_ShotGroup".Translate()}: {"VF_ShotGroupValue".Translate(fireMode.forcedMissRadius)}");
    }
    yield return new VehicleStatDrawEntry(LabelCap, displayOrder, "VF_FireModes".Translate(),
      string.Empty,
      $"{"VF_FireModesTooltip".Translate()}{Environment.NewLine}{fireModeExplanation}", 99998);
  }

  [UsedImplicitly]
  public static int RoundsPerMinuteRounded(int roundsPerMinute)
  {
    return roundsPerMinute switch
    {
      < 25   => roundsPerMinute,
      < 100  => roundsPerMinute.RoundTo(5),
      < 1000 => roundsPerMinute.RoundTo(10),
      _      => roundsPerMinute.RoundTo(50)
    };
  }

  public void OnFieldChanged()
  {
  }

  public override void ResolveReferences()
  {
    base.ResolveReferences();
    ammunition?.ResolveReferences();
    ValidateTargetScanFlags();
  }

  public virtual void PostDefDatabase()
  {
  }

  public override void PostLoad()
  {
    base.PostLoad();
    LongEventHandler.ExecuteWhenFinished(delegate
    {
      if (graphicData == null)
      {
        return;
      }
      FixInvalidGraphicDataFields(graphicData);
      if (!graphics.NullOrEmpty())
      {
        foreach (VehicleTurretRenderData renderData in graphics)
        {
          FixInvalidGraphicDataFields(renderData.graphicData);
        }
      }
    });
  }

  private void ValidateTargetScanFlags()
  {
    if (targetScanFlags == TargetScanFlags.None)
    {
      //targetScanFlags = TargetScanFlags.NeedActiveThreat | TargetScanFlags.NeedAutoTargetable;
      if (projectile?.projectile != null)
      {
        if (!projectile.projectile.flyOverhead)
        {
          targetScanFlags |= TargetScanFlags.NeedLOSToAll;
        }
        else
        {
          targetScanFlags |= TargetScanFlags.NeedNotUnderThickRoof;
        }
        if (projectile.projectile.ai_IsIncendiary)
        {
          targetScanFlags |= TargetScanFlags.NeedNonBurning;
        }
      }
    }
  }

  private static void FixInvalidGraphicDataFields(GraphicDataRGB graphicData)
  {
    if (graphicData == null)
    {
      return;
    }
    if (graphicData.shaderType == null)
    {
      graphicData.shaderType = ShaderTypeDefOf.Cutout;
    }
    else if (!VehicleMod.settings.main.useCustomShaders)
    {
      graphicData.shaderType =
        graphicData.shaderType.Shader.SupportsRGBMaskTex(ignoreSettings: true) ?
          ShaderTypeDefOf.CutoutComplex :
          graphicData.shaderType;
    }

    graphicData.RecacheLayerOffsets();
  }

  public override IEnumerable<string> ConfigErrors()
  {
    foreach (string error in base.ConfigErrors())
    {
      yield return error;
    }
    if (motes.NotNullAndAny(m =>
      m.moteDef is null || m.animationType == AnimationWrapperType.Off))
    {
      yield return
        "Invalid fields in <field>motes</field>. <field>moteDef</field> cannot be null and <field>animationType</field> cannot be \"Off\""
         .ConvertRichText();
    }
    if (graphicData == null && gizmoIconTexPath.NullOrEmpty())
    {
      yield return
        "Null graphicData and no gizmoIconTexPath, this turret has no way to be rendered in gizmos.";
    }
    if (fireModes.NullOrEmpty() || fireModes.Any(f => !f.IsValid))
    {
      yield return
        "Empty or Invalid <field>fireModes</field> list. Must include at least 1 entry with non-negative numbers."
         .ConvertRichText();
    }
    if (ammunition is null && projectile is null)
    {
      yield return
        "Must include either <field>ammunition</field> or a default <field>projectile</field>."
         .ConvertRichText();
    }
    if (ammunition is null)
    {
      if (genericAmmo)
      {
        yield return
          "Turret has no <field>ammunition</field> field, but has been flagged as using <field>genericAmmo</field>. This makes no sense.";
      }
      if (!Mathf.Approximately(chargePerAmmoCount, 1))
      {
        yield return
          "Turret has no <field>ammunition</field> field, but has been assigned <field>chargePerAmmoCount</field>. This makes no sense.";
      }
    }
    if (chargePerAmmoCount <= 0)
    {
      yield return "<field>chargePerAmmoCount</field> must be greater than 0.".ConvertRichText();
    }
    if (ammunition != null)
    {
      if (!Ext_Mods.HasActiveMod(CompatibilityPackageIds.CombatExtended) && !genericAmmo &&
        !ammunition.AllowedThingDefs.Any(c =>
          c.projectile != null || c.projectileWhenLoaded != null))
      {
        yield return
          "Non-generic ammo must be a <type>ThingDef</type> with projectile properties."
           .ConvertRichText();
      }
      if (ammunition.AllowedDefCount == 0)
      {
        yield return
          "<field>ammunition</field> is non-null but no defs are available to use as ammo. Either omit the field entirely or specify valid <type>ThingDefs</type> to use as ammo."
           .ConvertRichText();
      }
    }
    if (genericAmmo)
    {
      if (projectile is null)
      {
        yield return
          "Generic ammo must include a default projectile so the turret knows what to shoot."
           .ConvertRichText();
      }
      if (ammunition != null && ammunition.AllowedDefCount != 1)
      {
        yield return
          "Generic ammo turrets will only use the first <type>ThingDef</type> in <field>ammunition</field>. Consider removing all other entries but the first."
           .ConvertRichText();
      }
    }
    if (fireModes.Any(f => f.ticksBetweenShots > f.ticksBetweenBursts.TrueMin))
    {
      yield return
        "Setting <field>ticksBetweenBursts</field> with a lower tick count than <field>ticksBetweenShots</field> will produce odd shooting behavior. Please set to either the same amount (fully automatic) or greater than."
         .ConvertRichText();
    }
  }

  public Vector2 ScaleDrawRatio(VehicleDef vehicleDef, Vector2 size)
  {
    Vector2 drawSize = graphicData.drawSize;
    Vector2 scalar = drawSize / vehicleDef.graphicData.drawSize;

    float width = size.x * vehicleDef.uiIconScale * scalar.x;
    float height = size.y * vehicleDef.uiIconScale * scalar.y;

    if (width < height)
    {
      height = width * (drawSize.y / drawSize.x);
    }
    else
    {
      width = height * (drawSize.x / drawSize.y);
    }
    return new Vector2(width, height);
  }
}