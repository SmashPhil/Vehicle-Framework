using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using static Vehicles.VehicleUpgrade;

namespace Vehicles
{
	public class VehicleDef : ThingDef, IDefIndex<VehicleDef>, IMaterialCacheTarget, ITweakFields
	{
		[PostToSettings]
		public VehicleEnabledFor enabled = VehicleEnabledFor.Everyone;

		[PostToSettings(Label = "VF_Nameable", Translate = true, Tooltip = "VF_NameableTooltip", UISettingsType = UISettingsType.Checkbox)]
		public bool nameable = true;

		[DisableSetting]
		[PostToSettings(Label = "VF_CombatPower", Translate = true, Tooltip = "VF_CombatPowerTooltip", UISettingsType = UISettingsType.FloatBox)]
		[NumericBoxValues(MinValue = 0, MaxValue = float.MaxValue)]
		public float combatPower = 100;

		//Editing in ModSettings is handled manually as StatModifier list won't serialize well to the config file in the existing setup.
		public List<VehicleStatModifier> vehicleStats;

		[PostToSettings(Label = "VF_MovementPermissions", Translate = true, UISettingsType = UISettingsType.SliderEnum)]
		[ActionOnSettingsInput(typeof(VehicleHarmony), nameof(VehicleHarmony.RecacheMoveableVehicleDefs))]
		public VehiclePermissions vehicleMovementPermissions = VehiclePermissions.DriverNeeded;

		[PostToSettings(Label = "VF_CanCaravan", Translate = true, Tooltip = "VF_CanCaravanTooltip", UISettingsType = UISettingsType.Checkbox)]
		public bool canCaravan = true;

		public VehicleCategory vehicleCategory;
		public VehicleType vehicleType = VehicleType.Land;

		[PostToSettings(Label = "VF_NavigationType", Translate = true, Tooltip = "VF_NavigationTypeTooltip", UISettingsType = UISettingsType.SliderEnum)]
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

		//Event : SoundDef
		public List<VehicleSoundEventEntry<VehicleEventDef>> soundOneShotsOnEvent = new List<VehicleSoundEventEntry<VehicleEventDef>>();
		//<Start Event, Stop Event> : SoundDef
		public List<VehicleSustainerEventEntry<VehicleEventDef>> soundSustainersOnEvent = new List<VehicleSustainerEventEntry<VehicleEventDef>>();

		public Dictionary<VehicleEventDef, List<ResolvedMethod<VehiclePawn>>> events = new Dictionary<VehicleEventDef, List<ResolvedMethod<VehiclePawn>>>();

		public List<Type> designatorTypes = new List<Type>();

		[NoTranslate] //Should be translated in xml and parsed in appropriately
		public string draftLabel = null;
		public SoundDef soundBuilt;

		/// <summary>
		/// Auto-generated <c>PawnKindDef</c> that has been assigned for this VehicleDef.
		/// </summary>
		/// <remarks>If kindDef is set in VehicleDef, it will be overridden and the implied PawnKindDef will not be assigned.</remarks>
		public PawnKindDef kindDef;

		public List<VehicleComponentProperties> components;

		[Unsaved]
		private readonly SelfOrderingList<CompProperties> cachedComps = new SelfOrderingList<CompProperties>();
		[Unsaved]
		private Texture2D resolvedLoadCargoTexture;
		[Unsaved]
		private Texture2D resolvedCancelCargoTexture;

		public int DefIndex { get; set; }

		public int SizePadding { get; private set; }

		public VehicleFleshTypeDef BodyType => kindDef.RaceProps.FleshType as VehicleFleshTypeDef;

		/// <summary>
		/// Icon used for LoadCargo gizmo
		/// </summary>
		public Texture2D LoadCargoIcon
		{
			get
			{
				if (resolvedLoadCargoTexture is null)
				{
					resolvedLoadCargoTexture = ContentFinder<Texture2D>.Get(drawProperties.loadCargoTexPath, false) ?? VehicleTex.PackCargoIcon[(uint)vehicleType];
				}
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
				if (resolvedCancelCargoTexture is null)
				{
					resolvedCancelCargoTexture = ContentFinder<Texture2D>.Get(drawProperties.cancelCargoTexPath, false) ?? VehicleTex.CancelPackCargoIcon[(uint)vehicleType];
				}
				return resolvedCancelCargoTexture;
			}
		}

		public int MaterialCount => 4;

		public PatternDef PatternDef => PatternDefOf.Default;

		public string Name => $"{modContentPack.Name}_{defName}";

		string ITweakFields.Label => defName;

		string ITweakFields.Category => defName;

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
			if (GetCompProperties<CompProperties_UpgradeTree>() != null)
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

			int padding = Mathf.CeilToInt(Mathf.Min(size.x, size.z) / 2f);
			int result = Mathf.Clamp(padding - 1, 0, 100);
			SizePadding = result;

			if (VehicleMod.settings.vehicles.defaultGraphics.NullOrEmpty())
			{
				VehicleMod.settings.vehicles.defaultGraphics = new Dictionary<string, PatternData>();
			}

			if (draftLabel.NullOrEmpty())
			{
				draftLabel = "VF_draftLabel".Translate(); //Default translation for draft label
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
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				graphicData.shaderType ??= ShaderTypeDefOf.Cutout;
				if (!VehicleMod.settings.main.useCustomShaders)
				{
					graphicData.shaderType = graphicData.shaderType.Shader.SupportsRGBMaskTex(ignoreSettings: true) ? ShaderTypeDefOf.CutoutComplex : graphicData.shaderType;
				}
				if (graphicData.shaderType.Shader.SupportsRGBMaskTex())
				{
					RGBMaterialPool.CacheMaterialsFor(this);
					graphicData.Init(this);
					PatternData patternData = VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(defName, new PatternData(graphicData));
					patternData.ExposeDataPostDefDatabase();
					RGBMaterialPool.SetProperties(this, patternData, graphicData.Graphic.TexAt, graphicData.Graphic.MaskAt);
				}
				else
				{
					_ = graphicData.Graphic;
				}
			});
			
			base.PostLoad();
		}

		/// <summary>
		/// Initialize fields or properties that are reliant on the entire <see cref="DefDatabase{T}"/> being populated
		/// </summary>
		public void PostDefDatabase()
		{
			properties.PostDefDatabase(this);
			drawProperties.PostDefDatabase(this);
			graphicData.pattern ??= PatternDefOf.Default;
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
				Material material;
				if (graphicData.Graphic.Shader.SupportsRGBMaskTex())
				{
					material = RGBMaterialPool.Get(this, defaultPlacingRot);
				}
				else
				{
					material = graphic.MatAt(defaultPlacingRot);
				}
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
				yield return error;
			}
			foreach (string error in properties.ConfigErrors(this))
			{
				yield return error;
			}
			if (graphicData is null)
			{
				yield return "<field>graphicData</field> must be specified in order to properly render the vehicle.".ConvertRichText();
			}
			if (components.NullOrEmpty())
			{
				yield return "<field>components</field> must include at least 1 VehicleComponent".ConvertRichText();
			}
			if (!components.NullOrEmpty())
			{
				if (components.Select(c => c.key).GroupBy(s => s).Where(g => g.Count() > 1).Any())
				{
					yield return "<field>components</field> must not contain duplicate keys".ConvertRichText();
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

		public Vector2 ScaleDrawRatio(GraphicData graphicData, Rot4 rot, Vector2 size, float iconScale = 1)
		{
			Vector2 drawSize = graphicData.drawSize;
			if (rot.IsHorizontal)
			{
				float x = drawSize.x;
				drawSize.x = drawSize.y;
				drawSize.y = x;
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

		public VehicleRole CreateRole(string roleKey)
		{
			if (!properties.roles.NullOrEmpty())
			{
				foreach (VehicleRole vehicleRole in properties.roles)
				{
					if (vehicleRole.key == roleKey)
					{
						return new VehicleRole(vehicleRole);
					}
				}
			}
			if (GetCompProperties<CompProperties_UpgradeTree>() is CompProperties_UpgradeTree compPropertiesUpgradeTree)
			{
				foreach (UpgradeNode node in compPropertiesUpgradeTree.def.nodes)
				{
					if (!node.upgrades.NullOrEmpty())
					{
						foreach (Upgrade upgrade in node.upgrades)
						{
							if (upgrade is VehicleUpgrade vehicleUpgrade)
							{
								if (!vehicleUpgrade.roles.NullOrEmpty())
								{
									foreach (RoleUpgrade roleUpgrade in vehicleUpgrade.roles)
									{
										if (roleUpgrade.key == roleKey && roleUpgrade.editKey.NullOrEmpty())
										{
											return RoleUpgrade.RoleFromUpgrade(roleUpgrade);
										}
									}
								}
							}
						}
					}
				}
				Log.Error($"Unable to create role {roleKey}. Matching VehicleRole not found in VehicleDef ({defName}) or UpgradeTreeDef ({compPropertiesUpgradeTree.def.defName})");
				return null;
			}
			Log.Error($"Unable to create role {roleKey}. Matching VehicleRole not found in VehicleDef ({defName}).");
			return null;
		}

		/// <summary>
		/// Retrieve all <see cref="VehicleStatCategoryDef"/>'s for this VehicleDef
		/// </summary>
		/// <returns></returns>
		public IEnumerable<VehicleStatDef> StatCategoryDefs()
		{
			yield return VehicleStatDefOf.BodyIntegrity;

			foreach (VehicleStatModifier statModifier in vehicleStats)
			{
				if (statModifier.statDef == VehicleStatDefOf.MoveSpeed && vehicleMovementPermissions == VehiclePermissions.NotAllowed)
				{
					continue;
				}
				yield return statModifier.statDef;
			}

			foreach (VehicleCompProperties props in comps.Where(c => c is VehicleCompProperties))
			{
				foreach (VehicleStatDef statCategoryDef in props.StatCategoryDefs())
				{
					yield return statCategoryDef;
				}
			}
		}

		/// <summary>
		/// Better performant CompProperties retrieval for VehicleDefs
		/// </summary>
		/// <remarks>Can only be called after all references have been resolved. If a CompProperties is needed beforehand, use <see cref="GetCompProperties{T}"/></remarks>
		/// <typeparam name="T"></typeparam>
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
			return default;
		}
	}
}
