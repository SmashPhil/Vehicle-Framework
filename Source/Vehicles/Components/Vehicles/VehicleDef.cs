using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using Vehicles.Defs;

namespace Vehicles
{
	public class VehicleDef : ThingDef
	{
		[PostToSettings]
		internal bool enabled = true;

		[PostToSettings(Label = "VehicleNameable", Translate = true, Tooltip = "VehicleNameableTooltip", UISettingsType = UISettingsType.Checkbox)]
		public bool nameable = false;
		[DisableSetting]
		[PostToSettings(Label = "VehicleRaidsEnabled", Translate = true, Tooltip = "VehicleRaidsEnabledTooltip", UISettingsType = UISettingsType.Checkbox)]
		public bool raidersCanUse = true;

		public float armor;
		[PostToSettings(Label = "VehicleBaseSpeed", Translate = true, Tooltip ="VehicleBaseSpeedTooltip", UISettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 8, RoundDecimalPlaces = 2, Increment = 0.25f)]
		public float speed;

		[PostToSettings(Label = "VehicleBaseCargo", Translate = true, Tooltip ="VehicleBaseCargoTooltip", UISettingsType = UISettingsType.IntegerBox)]
		public float cargoCapacity;

		[PostToSettings(Label = "VehicleTicksBetweenRepair", Translate = true, Tooltip = "VehicleTicksBetweenRepairTooltip", UISettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 1, MaxValue = 20f)]
		public float repairRate = 1;

		[PostToSettings(Label = "VehicleMovementPermissions", Translate = true, UISettingsType = UISettingsType.SliderEnum)]
		public VehiclePermissions vehicleMovementPermissions = VehiclePermissions.DriverNeeded;

		public VehicleCategory vehicleCategory = VehicleCategory.Misc;
		public TechLevel vehicleTech = TechLevel.Industrial;
		public VehicleType vehicleType = VehicleType.Land;

		[PostToSettings(Label = "VehicleNavigationType", Translate = true, UISettingsType = UISettingsType.SliderEnum)]
		public NavigationCategory defaultNavigation = NavigationCategory.Opportunistic;

		public VehicleBuildDef buildDef;

		[PostToSettings(Label = "VehicleProperties", Translate = true, ParentHolder = true)]
		public VehicleProperties properties;
		
		public VehicleDrawProperties drawProperties;

		public List<VehicleComponentProperties> components;

		private readonly SelfOrderingList<CompProperties> cachedComps = new SelfOrderingList<CompProperties>();

		public override void ResolveReferences()
		{
			base.ResolveReferences();
			if (!components.NullOrEmpty())
			{
				components.OrderBy(c => c.hitbox.side == VehicleComponentPosition.BodyNoOverlap).ForEach(c => c.ResolveReferences(this));
				properties.roles.OrderBy(c => c.hitbox.side == VehicleComponentPosition.BodyNoOverlap).ForEach(c => c.hitbox.Initialize(this));
			}
			if (properties.overweightSpeedCurve is null)
			{
				properties.overweightSpeedCurve = new SimpleCurve()
				{
					new CurvePoint(0, 1),
					new CurvePoint(0.65f, 1),
					new CurvePoint(0.85f, 0.9f),
					new CurvePoint(1.05f, 0.35f),
					new CurvePoint(1.25f, 0)
				};
			}
			if (VehicleMod.settings.vehicles.defaultMasks.EnumerableNullOrEmpty())
			{
				VehicleMod.settings.vehicles.defaultMasks = new Dictionary<string, string>();
			}
			if (!VehicleMod.settings.vehicles.defaultMasks.ContainsKey(defName))
			{
				VehicleMod.settings.vehicles.defaultMasks.Add(defName, "Default");
			}
			if (properties.customBiomeCosts is null)
			{
				properties.customBiomeCosts = new Dictionary<BiomeDef, float>();
			}
			if (properties.customHillinessCosts is null)
			{
				properties.customHillinessCosts = new Dictionary<Hilliness, float>();
			}
			if (properties.customRoadCosts is null)
			{
				properties.customRoadCosts = new Dictionary<RoadDef, float>();
			}
			if (properties.customTerrainCosts is null)
			{
				properties.customTerrainCosts = new Dictionary<TerrainDef, int>();
			}
			if (properties.customThingCosts is null)
			{
				properties.customThingCosts = new Dictionary<ThingDef, int>();
			}

			if (vehicleType == VehicleType.Sea)
			{
				if (!properties.customBiomeCosts.ContainsKey(BiomeDefOf.Ocean))
				{
					properties.customBiomeCosts.Add(BiomeDefOf.Ocean, 1);
				}
				if (!properties.customBiomeCosts.ContainsKey(BiomeDefOf.Lake))
				{
					properties.customBiomeCosts.Add(BiomeDefOf.Lake, 1);
				}
			}

			if (!comps.NullOrEmpty())
			{
				cachedComps.AddRange(comps);
			}
		}

		public override IEnumerable<string> ConfigErrors()
		{
			foreach (string error in base.ConfigErrors())
			{
				yield return error;
			}

			if (vehicleType == VehicleType.Undefined)
			{
				yield return "Cannot assign <field>Undefined</field> vehicle type to VehicleDef".ConvertRichText();
			}
			if (properties.vehicleJobLimitations.NullOrEmpty())
			{
				yield return "<field>vehicleJobLimitations</field> list must be populated".ConvertRichText();
			}
			else
			{
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
		}

		public Vector2 ScaleDrawRatio(Vector2 size)
		{
			float sizeX = size.x;
			float sizeY = size.y;
			Vector2 drawSize = race.AnyPawnKind.lifeStages.LastOrDefault().bodyGraphicData.drawSize;
			if (sizeX < sizeY)
			{
				sizeY = sizeX * (drawSize.y / drawSize.x);
			}
			else
			{
				sizeX = sizeY * (drawSize.x / drawSize.y);
			}
			return new Vector2(sizeX, sizeY);
		}

		public IEnumerable<VehicleStatCategoryDef> StatCategoryDefs()
		{
			if (speed > 0)
			{
				yield return VehicleStatCategoryDefOf.StatCategoryMovement;
			}
			yield return VehicleStatCategoryDefOf.StatCategoryArmor;

			foreach (VehicleCompProperties props in comps.Where(c => c is VehicleCompProperties))
			{
				foreach (VehicleStatCategoryDef statCategoryDef in props.StatCategoryDefs())
				{
					yield return statCategoryDef;
				}
			}
		}

		protected override void ResolveIcon()
		{
			if (graphic != null && graphic != BaseContent.BadGraphic)
			{
				Material material = graphic.ExtractInnerGraphicFor(null).MatAt(defaultPlacingRot, null);
				uiIcon = (Texture2D)material.mainTexture;
				uiIconColor = material.color;

				//PawnKindDef anyPawnKind = race.AnyPawnKind;
				//if (anyPawnKind != null)
				//{
				//	Material material2 = anyPawnKind.lifeStages.Last().bodyGraphicData.Graphic.MatAt(Rot4.East, null);
				//	uiIcon = (Texture2D)material2.mainTexture;
				//	uiIconColor = material2.color;
				//	return;
				//}
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
