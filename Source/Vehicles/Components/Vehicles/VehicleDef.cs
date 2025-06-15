using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using SmashTools.Rendering;
using UnityEngine;
using UnityEngine.Assertions;
using Vehicles.Rendering;
using Verse;
using static Vehicles.VehicleUpgrade;

namespace Vehicles;

[PublicAPI, VehicleSettingsClass]
public class VehicleDef : ThingDef, IDefIndex<VehicleDef>, IMaterialCacheTarget, ITweakFields,
                          IBlitTarget
{
  [PostToSettings]
  public VehicleEnabled.For enabled = VehicleEnabled.For.Everyone;

  [PostToSettings(Label = "VF_Nameable", Translate = true, Tooltip = "VF_NameableTooltip",
    UISettingsType = UISettingsType.Checkbox)]
  public bool nameable = true;

  [DisableSetting]
  [PostToSettings(Label = "VF_CombatPower", Translate = true, Tooltip = "VF_CombatPowerTooltip",
    UISettingsType = UISettingsType.FloatBox)]
  [NumericBoxValues(MinValue = 0, MaxValue = float.MaxValue)]
  public float combatPower = 100;

  // Editing in ModSettings is handled manually as StatModifier list won't serialize well to the
  // config file in the existing setup.
  public List<VehicleStatModifier> vehicleStats = [];

  // TODO - remove in 1.6
  #pragma warning disable CS0414
  //[PostToSettings(Label = "VF_MovementPermissions", Translate = true,
  //  UISettingsType = UISettingsType.SliderEnum)]
  //[ActionOnSettingsInput(typeof(VehicleHarmony),
  //  nameof(GridOwners.RecacheMoveableVehicleDefs))]
  [Unsaved]
  [LoadAlias("vehicleMovementPermissions")]
  private VehiclePermissions movementPermissions = VehiclePermissions.DriverNeeded;
  #pragma warning restore CS0414

  [PostToSettings(Label = "VF_CanCaravan", Translate = true, Tooltip = "VF_CanCaravanTooltip",
    UISettingsType = UISettingsType.Checkbox)]
  public bool canCaravan = true;

  //[LoadAlias("vehicleCategory")]
  public VehicleCategory vehicleCategory;

  [LoadAlias("vehicleType")]
  public VehicleType type = VehicleType.Land;

  [PostToSettings(Label = "VF_NavigationType", Translate = true,
    Tooltip = "VF_NavigationTypeTooltip", UISettingsType = UISettingsType.SliderEnum)]
  public NavigationCategory navigationCategory = NavigationCategory.Opportunistic;

  public VehicleBuildDef buildDef;

  [TweakField(SubCategory = "GraphicData")]
  public new GraphicDataRGB graphicData;

  [TweakField]
  [PostToSettings(Label = "VF_Properties", Translate = true, ParentHolder = true)]
  public VehicleProperties properties;

  //[TweakField]
  //[PostToSettings(Label = "VF_NPCProperties", Translate = true, ParentHolder = true)]
  public VehicleNPCProperties npcProperties;

  [TweakField]
  public VehicleDrawProperties drawProperties;

  public List<StatCache.EventLister> statEvents;

  // Event : SoundDef
  public List<VehicleSoundEventEntry<VehicleEventDef>> soundOneShotsOnEvent = [];

  // <Start Event, Stop Event> : SoundDef
  public List<VehicleSustainerEventEntry<VehicleEventDef>> soundSustainersOnEvent;

  public SimpleDictionary<VehicleEventDef, List<DynamicDelegate<VehiclePawn>>> events;

  public List<Type> designatorTypes = [];

  // Should be translated in xml and parsed in appropriately
  [NoTranslate]
  public string draftLabel;

  public SoundDef soundBuilt;

  /// <summary>
  /// Auto-generated <c>PawnKindDef</c> that has been assigned for this VehicleDef.
  /// </summary>
  /// <remarks>If kindDef is set in VehicleDef, it will be overridden and the implied 
  /// PawnKindDef will not be assigned.</remarks>
  public PawnKindDef kindDef;

  public List<VehicleComponentProperties> components;

  [Unsaved]
  private readonly SelfOrderingList<CompProperties> cachedComps = [];

  [Unsaved]
  private Texture2D resolvedLoadCargoTexture;

  [Unsaved]
  private Texture2D resolvedCancelCargoTexture;

  /// <summary>
  /// Index based caching for various vehicle arrays. Provides faster lookup than Def dictionary lookup.
  /// </summary>
  /// <remarks>Assigned on startup, may not be the same across different mod lists.</remarks>
  public int DefIndex { get; set; }

  /// <summary>
  /// Padding adjustment for regions, defining reachability based on vehicle size and how close
  /// the vehicle can get to the impassable cell without overlapping its hitbox.
  /// </summary>
  public int SizePadding { get; private set; }

  public MaterialPropertyBlock PropertyBlock { get; private set; }

  public VehicleFleshTypeDef BodyType => kindDef.RaceProps.FleshType as VehicleFleshTypeDef;

  public CompProperties_FueledTravel CompPropsFueledTravel { get; private set; }

  public CompProperties_VehicleLauncher CompPropsVehicleLauncher { get; private set; }

  public CompProperties_VehicleTurrets CompPropsVehicleTurrets { get; private set; }

  public CompProperties_UpgradeTree CompPropsUpgradeTree { get; private set; }

  /// <summary>
  /// Icon used for LoadCargo gizmo
  /// </summary>
  public Texture2D LoadCargoIcon
  {
    get
    {
      resolvedLoadCargoTexture ??=
        ContentFinder<Texture2D>.Get(drawProperties.loadCargoTexPath, false)
        ?? VehicleTex.PackCargoIcon[(uint)type];
      return resolvedLoadCargoTexture;
    }
  }

  /// <summary>
  /// Icon used for CancelCargo gizmo
  /// </summary>
  public Texture2D CancelCargoIcon
  {
    get
    {
      resolvedCancelCargoTexture ??=
        ContentFinder<Texture2D>.Get(drawProperties.cancelCargoTexPath, false)
        ?? VehicleTex.CancelPackCargoIcon[(uint)type];
      return resolvedCancelCargoTexture;
    }
  }

  /// <summary>
  /// Size allocated in material pool
  /// </summary>
  public int MaterialCount => 4;

  public PatternDef PatternDef => PatternDefOf.Default;

  public string Name => defName;

  string ITweakFields.Label => defName;

  string ITweakFields.Category => defName;

  public bool CanDisableEMPSetting
  {
    get
    {
      if (!components.NullOrEmpty())
      {
        foreach (VehicleComponentProperties props in components)
        {
          if (props.empSeverity > VehicleEMPSeverity.None)
          {
            return false;
          }
        }
      }
      return true;
    }
  }

  public void OnFieldChanged()
  {
  }

  /// <summary>
  /// Resolve all references related to this VehicleDef
  /// </summary>
  /// <remarks>
  /// Ensures no lists are left null in order to avoid null-reference exceptions
  /// </remarks>
  public override void ResolveReferences()
  {
    CacheCompProperties();

    if (CompPropsUpgradeTree != null)
    {
      inspectorTabs.Add(typeof(ITab_Vehicle_Upgrades));
    }

    base.ResolveReferences();
    if (!components.NullOrEmpty())
    {
      foreach (VehicleComponentProperties component in components)
      {
        component.ResolveReferences(this);
      }
    }

    designatorTypes ??= new List<Type>();
    drawProperties ??= new VehicleDrawProperties();
    properties ??= new VehicleProperties();
    properties.ResolveReferences(this);

    npcProperties ??= new VehicleNPCProperties();

    int padding = Mathf.CeilToInt(Mathf.Min(size.x, size.z) / 2f);
    int result = Mathf.Clamp(padding - 1, 0, 100);
    SizePadding = result;

    if (VehicleMod.settings.vehicles.defaultGraphics.NullOrEmpty())
    {
      VehicleMod.settings.vehicles.defaultGraphics = new Dictionary<string, PatternData>();
    }

    if (draftLabel.NullOrEmpty())
    {
      // Use Default translation if non specified. This alleviates the issue of implementing it in xml
      // and vehicles that don't inherit the base vehicle pawn xml missing out on translations.
      draftLabel = "VF_draftLabel".Translate();
    }

    if (!comps.NullOrEmpty())
    {
      cachedComps.AddRange(comps);
    }
  }

  /// <summary>
  /// Vanilla-compatibility with graphicData
  /// </summary>
  public override void PostLoad()
  {
    base.graphicData = graphicData;

    LongEventHandler.ExecuteWhenFinished(VerifyGraphicData);

    base.PostLoad();
  }

  private void VerifyGraphicData()
  {
    PropertyBlock = new MaterialPropertyBlock();

    if (graphicData is null)
      return;

    graphicData.shaderType ??= ShaderTypeDefOf.Cutout;
    if (!VehicleMod.settings.main.useCustomShaders)
    {
      graphicData.shaderType =
        graphicData.shaderType.Shader.SupportsRGBMaskTex(ignoreSettings: true) ?
          ShaderTypeDefOf.CutoutComplex :
          graphicData.shaderType;
    }

    if (graphicData.shaderType.Shader.SupportsRGBMaskTex())
    {
      RGBMaterialPool.CacheMaterialsFor(this);
      graphicData.Init(this);
      PatternData patternData =
        VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(defName,
          new PatternData(graphicData));
      patternData.ExposeDataPostDefDatabase();
      RGBMaterialPool.SetProperties(this, patternData, graphicData.Graphic.TexAt,
        graphicData.Graphic.MaskAt);
    }
    else
    {
      _ = graphicData.Graphic;
    }
  }

  /// <summary>
  /// Initialize fields or properties that are reliant on the entire <see cref="DefDatabase{T}"/> being populated
  /// </summary>
  public void PostDefDatabase()
  {
    properties.PostDefDatabase(this);
    drawProperties.PostDefDatabase(this);
    if (graphicData != null)
      graphicData.pattern ??= PatternDefOf.Default;
  }

  private void CacheCompProperties()
  {
    CompPropsFueledTravel = GetCompProperties<CompProperties_FueledTravel>();
    CompPropsVehicleLauncher = GetCompProperties<CompProperties_VehicleLauncher>();
    CompPropsVehicleTurrets = GetCompProperties<CompProperties_VehicleTurrets>();
    CompPropsUpgradeTree = GetCompProperties<CompProperties_UpgradeTree>();
  }

  /// <summary>
  /// Resolve icon for VehicleDef
  /// </summary>
  /// <remarks>
  /// Removed icon based on lifeStages, vehicles don't have lifeStages
  /// </remarks>
  protected override void ResolveIcon()
  {
    if (graphic != null && graphic != BaseContent.BadGraphic)
    {
      Material material = graphicData.Graphic.Shader.SupportsRGBMaskTex() ?
        RGBMaterialPool.Get(this, defaultPlacingRot) :
        graphic.MatAt(defaultPlacingRot);

      uiIcon = (Texture2D)material.mainTexture;
      uiIconColor = material.color;
    }
  }

  /// <summary>
  /// Config errors to ensure all required data has been filled in
  /// </summary>
  public override IEnumerable<string> ConfigErrors()
  {
    foreach (string error in base.ConfigErrors())
    {
      if (Fillage == FillCategory.Full)
      {
        // Vehicles can provide full cover without affecting edifice grid
        switch (error)
        {
          case "fillPercent is 1.00 but is not edifice":
          case "gives full cover but is not a building.":
            continue;
        }
      }

      yield return error;
    }

    if (drawerType != DrawerType.RealtimeOnly)
      yield return
        $"{drawerType} is not valid for vehicle rendering. <field>drawerType</type> must be DrawerType.RealtimeOnly";

    foreach (string error in properties.ConfigErrors(this))
    {
      yield return error;
    }

    if (!statBases.NullOrEmpty())
    {
      foreach (StatModifier statModifier in statBases)
      {
        if (statModifier.stat == StatDefOf.Mass)
          Log.Error("Vehicles must define Mass in vehicleStats and not statBases.");
        if (statModifier.stat == StatDefOf.MoveSpeed)
          Log.Error("Vehicles must define MoveSpeed in vehicleStats and not statBases.");
      }
    }

    if (graphicData is null)
    {
      yield return
        "<field>graphicData</field> must be specified in order to properly render the vehicle."
         .ConvertRichText();
    }

    if (components.NullOrEmpty())
    {
      yield return "<field>components</field> must include at least 1 VehicleComponent"
       .ConvertRichText();
    }

    if (!components.NullOrEmpty())
    {
      if (components.GroupBy(component => component.key).Any(group => group.Count() > 1))
      {
        yield return "<field>components</field> must not contain duplicate keys"
         .ConvertRichText();
      }

      foreach (VehicleComponentProperties props in components)
      {
        foreach (string error in props.ConfigErrors())
        {
          yield return error;
        }
      }
    }
  }

  /// <summary>
  /// Scale the drawSize appropriately for this VehicleDef
  /// </summary>
  /// <param name="size"></param>
  public Vector2 ScaleDrawRatio(Vector2 size)
  {
    float width = size.x * uiIconScale;
    float height = size.y * uiIconScale;
    Vector2 drawSize = graphicData.drawSize;
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

  public Vector2 ScaleDrawRatio(GraphicData graphicData, Rot4 rot, Vector2 size,
    float iconScale = 1)
  {
    Vector2 drawSize = graphicData.drawSize;
    if (rot.IsHorizontal)
    {
      (drawSize.x, drawSize.y) = (drawSize.y, drawSize.x);
    }

    Vector2 scalar = drawSize / this.graphicData.drawSize;

    float width = size.x * uiIconScale * scalar.x * iconScale;
    float height = size.y * uiIconScale * scalar.y * iconScale;

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

  public VehicleRole GetRole(string key)
  {
    if (!properties.roles.NullOrEmpty())
    {
      foreach (VehicleRole vehicleRole in properties.roles)
      {
        if (vehicleRole.key == key)
        {
          return vehicleRole;
        }
      }
    }
    return null;
  }

  public VehicleRole CreateRole(string key)
  {
    VehicleRole roleReference = GetRole(key);
    if (roleReference != null)
    {
      return new VehicleRole(roleReference);
    }

    if (GetCompProperties<CompProperties_UpgradeTree>() is { } compPropertiesUpgradeTree)
    {
      foreach (UpgradeNode node in compPropertiesUpgradeTree.def.nodes)
      {
        if (node.upgrades.NullOrEmpty()) continue;

        foreach (Upgrade upgrade in node.upgrades)
        {
          if (upgrade is not VehicleUpgrade vehicleUpgrade) continue;
          if (vehicleUpgrade.roles.NullOrEmpty()) continue;
          foreach (RoleUpgrade roleUpgrade in vehicleUpgrade.roles)
          {
            if (roleUpgrade.key == key && roleUpgrade.editKey.NullOrEmpty())
            {
              return RoleUpgrade.RoleFromUpgrade(roleUpgrade);
            }
          }
        }
      }

      Log.Error(
        $"Unable to create role {key}. Matching VehicleRole not found in VehicleDef ({defName}) or UpgradeTreeDef ({compPropertiesUpgradeTree.def.defName})");
      return null;
    }

    Log.Error(
      $"Unable to create role {key}. Matching VehicleRole not found in VehicleDef ({defName}).");
    return null;
  }

  public virtual IEnumerable<VehicleStatDrawEntry> SpecialDisplayStats(VehiclePawn vehicle = null)
  {
    foreach (VehicleStatDrawEntry buildableStatEntry in buildDef.SpecialDisplayStats())
    {
      yield return buildableStatEntry;
    }

    (int current, int max, string explanation) health =
      vehicle?.GetTotalHealth() ?? this.GetTotalHealth();
    yield return new VehicleStatDrawEntry(VehicleStatCategoryDefOf.VehicleBasicsImportant,
      "HitPointsBasic".Translate().CapitalizeFirst(),
      $"{health.current} / {health.max}",
      $"{"Stat_HitPoints_Desc".Translate()}{Environment.NewLine}{Environment.NewLine}{health.explanation}",
      99998);

    StringBuilder crewExplanation = new();
    int crewCount = 0;
    List<VehicleRole> roles = vehicle?.handlers.Select(h => h.role).ToList() ?? properties.roles;
    if (!properties.roles.NullOrEmpty())
    {
      crewExplanation.AppendLine();
      foreach (VehicleRole role in roles)
      {
        crewCount += role.Slots;
        crewExplanation.AppendLine($"x{role.Slots}  {role.label}");
      }
    }

    yield return new VehicleStatDrawEntry(VehicleStatCategoryDefOf.VehicleBasicsImportant,
      "VF_CrewCount".Translate().CapitalizeFirst(),
      crewCount.ToString(),
      $"{"VF_CrewCountDesc".Translate()}{Environment.NewLine}{crewExplanation}", 99995);

    if (CompPropsFueledTravel != null)
    {
      ThingDef fuelType = CompPropsFueledTravel.fuelType;
      yield return new VehicleStatDrawEntry(VehicleStatCategoryDefOf.VehicleRefuelable,
        "VF_FuelType".Translate().CapitalizeFirst(),
        fuelType.LabelCap, "VF_FuelTypeDesc".Translate(), 4500);
      float fuelConsumptionRate = vehicle?.CompFueledTravel.FuelEfficiency ??
        CompPropsFueledTravel.fuelConsumptionRate;
      yield return new VehicleStatDrawEntry(VehicleStatCategoryDefOf.VehicleRefuelable,
        "VF_FuelConsumptionRate".Translate().CapitalizeFirst(),
        fuelConsumptionRate.ToString("0.##"), "VF_FuelConsumptionRateTooltip".Translate(), 4501);
      float fuelCapacity = vehicle?.CompFueledTravel.FuelCapacity ??
        CompPropsFueledTravel.fuelCapacity;
      yield return new VehicleStatDrawEntry(VehicleStatCategoryDefOf.VehicleRefuelable,
        "VF_FuelCapacity".Translate().CapitalizeFirst(),
        fuelCapacity.ToStringByStyle(ToStringStyle.Integer), "VF_FuelCapacityTooltip".Translate(),
        4502);
    }

    if (CompPropsVehicleTurrets != null)
    {
      List<VehicleTurret> turrets =
        (List<VehicleTurret>)vehicle?.CompVehicleTurrets.Turrets ?? CompPropsVehicleTurrets.turrets;
      if (!turrets.NullOrEmpty())
      {
        for (int i = 0; i < turrets.Count; i++)
        {
          VehicleTurret turret = turrets[i];
          foreach (VehicleStatDrawEntry statDrawEntry in turret.def.SpecialDisplayStats(
            VehicleStatCategoryDefOf.VehicleTurrets.displayOrder + i))
          {
            yield return statDrawEntry;
          }
        }
      }
    }

    if (CompPropsVehicleLauncher != null)
    {
      if (ModsConfig.IsActive(CompatibilityPackageIds.SOS2) ||
        ModsConfig.IsActive(CompatibilityPackageIds.RimNauts) ||
        ModsConfig.IsActive(CompatibilityPackageIds.Universum))
      {
        bool spaceFlight = vehicle?.CompVehicleLauncher.SpaceFlight ??
          CompPropsVehicleLauncher.spaceFlight;
        yield return new VehicleStatDrawEntry(VehicleStatCategoryDefOf.VehicleRefuelable,
          "VF_SpaceFlight".Translate().CapitalizeFirst(),
          spaceFlight.ToStringYesNo(), "VF_SpaceFlightTooltip".Translate(), 1000);
      }
    }

    if (fillPercent > 0f)
    {
      yield return new VehicleStatDrawEntry(StatCategoryDefOf.Building,
        "CoverEffectiveness".Translate(),
        this.BaseBlockChance().ToStringPercent(), "CoverEffectivenessExplanation".Translate(),
        2000);
    }

    yield return new VehicleStatDrawEntry(VehicleStatCategoryDefOf.VehicleBasics,
      "VF_Upgradeable".Translate(),
      (CompPropsUpgradeTree != null).ToStringYesNo(), "VF_UpgradeableDesc".Translate(), 6001);
    if (VehicleMod.settings.main.useCustomShaders)
    {
      yield return new VehicleStatDrawEntry(VehicleStatCategoryDefOf.VehicleBasics,
        "Stat_Building_Paintable".Translate(),
        graphicData.shaderType.Shader.SupportsRGBMaskTex().ToStringYesNo(),
        "VF_PaintableDesc".Translate(), 6000);
    }

    if (modContentPack is { IsCoreMod: false })
    {
      yield return new VehicleStatDrawEntry(StatCategoryDefOf.Source,
        "Stat_Source_Label".Translate(), modContentPack.Name,
        $"{(modContentPack.IsOfficialMod ? "Stat_Source_OfficialExpansionReport".Translate() :
          "Stat_Source_ModReport".Translate())}: {modContentPack.Name}", 90000);
    }
  }

  public IEnumerable<VehicleStatDef> StatCategoryDefs()
  {
    yield return VehicleStatDefOf.BodyIntegrity;

    foreach (VehicleStatModifier statModifier in vehicleStats)
    {
      if (statModifier.statDef == VehicleStatDefOf.MoveSpeed &&
        properties.roles.NotNullAndAny(role => role.HandlingTypes.HasFlag(HandlingType.Movement)))
      {
        continue;
      }
      yield return statModifier.statDef;
    }

    foreach (CompProperties props in comps)
    {
      if (props is not VehicleCompProperties vehicleCompProps)
        continue;

      foreach (VehicleStatDef statCategoryDef in vehicleCompProps.StatCategoryDefs())
      {
        yield return statCategoryDef;
      }
    }
  }

  /// <summary>
  /// Better performant CompProperties retrieval for VehicleDefs
  /// </summary>
  public T GetSortedCompProperties<T>() where T : CompProperties
  {
    for (int i = 0; i < cachedComps.Count; i++)
    {
      if (cachedComps[i] is T t)
      {
        cachedComps.CountIndex(i);
        return t;
      }
    }
    return null;
  }

  (int width, int height) IBlitTarget.TextureSize(in BlitRequest request)
  {
    Graphic_Vehicle graphicVehicle = graphicData.Graphic as Graphic_Vehicle;
    Assert.IsNotNull(graphicVehicle);
    Texture2D mainTex = graphicVehicle.TexAt(request.rot);
    return mainTex != null ? (mainTex.width, mainTex.height) : (0, 0);
  }

  IEnumerable<RenderData> IBlitTarget.GetRenderData(Rect rect, BlitRequest request)
  {
    Vector2 rectSize = ScaleDrawRatio(rect.size);
    bool elongated = request.rot.IsHorizontal || request.rot.IsDiagonal;
    Vector2 displayOffset = drawProperties.DisplayOffsetForRot(request.rot);
    float scaledWidth = rectSize.x;
    float scaledHeight = rectSize.y;
    if (elongated)
    {
      scaledWidth = rectSize.y;
      scaledHeight = rectSize.x;
    }
    float offsetX = (rect.width - scaledWidth) / 2 + (displayOffset.x * rect.width);
    float offsetY = (rect.height - scaledHeight) / 2 + (displayOffset.y * rect.height);

    Rect adjustedRect = new(rect.x + offsetX, rect.y + offsetY, scaledWidth, scaledHeight);

    Graphic_Vehicle graphicVehicle = graphicData.Graphic as Graphic_Vehicle;
    Assert.IsNotNull(graphicVehicle);

    Texture2D mainTex = graphicVehicle.TexAt(request.rot);
    //Texture2D maskTex = graphicVehicle.MaskAt(rot);

    Material material = null;
    if (graphicVehicle.Shader.SupportsRGBMaskTex())
    {
      material = RGBMaterialPool.Get(this, request.rot);
      RGBMaterialPool.SetProperties(this, request.patternData, graphicVehicle.TexAt,
        graphicVehicle.MaskAt);
    }
    yield return new RenderData(adjustedRect, mainTex, material, PropertyBlock, 0, 0);
  }
}