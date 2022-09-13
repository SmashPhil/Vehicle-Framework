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

namespace Vehicles
{
	public class VehicleDef : ThingDef
	{
		[PostToSettings]
		public VehicleEnabledFor enabled = VehicleEnabledFor.Everyone;

		[PostToSettings(Label = "VehicleNameable", Translate = true, Tooltip = "VehicleNameableTooltip", UISettingsType = UISettingsType.Checkbox)]
		public bool nameable = false;

		[PostToSettings(Label = "VehicleCombatPower", Translate = true, Tooltip = "VehicleCombatPowerTooltip", UISettingsType = UISettingsType.FloatBox)]
		[NumericBoxValues(MinValue = 0, MaxValue = float.MaxValue)]
		public float combatPower = 0;

		//[PostToSettings(Label = "VF_VehicleStats", Translate = true, Tooltip = "VF_VehicleStatsTooltip")]
		public List<VehicleStatModifier> vehicleStats;

		[PostToSettings(Label = "VehicleMovementPermissions", Translate = true, UISettingsType = UISettingsType.SliderEnum)]
		[ActionOnSettingsInput(typeof(VehicleHarmony), nameof(VehicleHarmony.RecacheMoveableVehicleDefs))]
		public VehiclePermissions vehicleMovementPermissions = VehiclePermissions.DriverNeeded;

		[PostToSettings(Label = "VehicleCanCaravan", Translate = true, Tooltip = "VehicleCanCaravanTooltip", UISettingsType = UISettingsType.Checkbox)]
		public bool canCaravan = true;

		public VehicleCategory vehicleCategory = VehicleCategory.Misc;
		public VehicleType vehicleType = VehicleType.Land;

		[PostToSettings(Label = "VehicleNavigationType", Translate = true, UISettingsType = UISettingsType.SliderEnum)]
		public NavigationCategory defaultNavigation = NavigationCategory.Opportunistic;

		public VehicleBuildDef buildDef;
		public new GraphicDataRGB graphicData;

		[PostToSettings(Label = "VehicleProperties", Translate = true, ParentHolder = true)]
		public VehicleProperties properties;
		
		public VehicleDrawProperties drawProperties;

		//Event : SoundDef
		public Dictionary<VehicleEventDef, SoundDef> soundOneShotsOnEvent = new Dictionary<VehicleEventDef, SoundDef>();
		//<Start Event, Stop Event> : SoundDef
		public Dictionary<Pair<VehicleEventDef, VehicleEventDef>, SoundDef> soundSustainersOnEvent = new Dictionary<Pair<VehicleEventDef, VehicleEventDef>, SoundDef>();

		public List<Type> designatorTypes = new List<Type>();

		public string draftLabel = "[MISSING]";
		public SoundDef soundBuilt;

		/// <summary>
		/// Auto-generated <c>PawnKindDef</c> that has been assigned for this VehicleDef.
		/// </summary>
		/// <remarks>If kindDef is set in VehicleDef, it will be overridden and the implied PawnKindDef will not be assigned.</remarks>
		public PawnKindDef kindDef;

		public List<VehicleComponentProperties> components;

		private readonly SelfOrderingList<CompProperties> cachedComps = new SelfOrderingList<CompProperties>();
		private Texture2D resolvedLoadCargoTexture;
		private Texture2D resolvedCancelCargoTexture;

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

		/// <summary>
		/// Resolve all references related to this VehicleDef
		/// </summary>
		/// <remarks>
		/// Ensures no lists are left null in order to avoid null-reference exceptions
		/// </remarks>
		public override void ResolveReferences()
		{
			base.ResolveReferences();
			if (!components.NullOrEmpty())
			{
				components.OrderBy(c => c.hitbox.side == VehicleComponentPosition.BodyNoOverlap).ForEach(c => c.ResolveReferences(this));
			}
			designatorTypes ??= new List<Type>();
			drawProperties ??= new VehicleDrawProperties();
			properties ??= new VehicleProperties();
			properties.ResolveReferences(this);
			
			if (VehicleMod.settings.vehicles.defaultGraphics.NullOrEmpty())
			{
				VehicleMod.settings.vehicles.defaultGraphics = new Dictionary<string, PatternData>();
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
			base.PostLoad();
			properties.PostVehicleDefLoad(this);
		}

		/// <summary>
		/// Initialize fields or properties that are reliant on the entire <see cref="DefDatabase{T}"/> being populated
		/// </summary>
		public void PostDefDatabase()
		{
			drawProperties.PostDefDatabase();
			graphicData.pattern ??= PatternDefOf.Default;
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
			foreach (string error in properties.ConfigErrors())
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

		/// <summary>
		/// Retrieve all <see cref="VehicleStatCategoryDef"/>'s for this VehicleDef
		/// </summary>
		/// <returns></returns>
		public IEnumerable<VehicleStatDef> StatCategoryDefs()
		{
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
		/// Resolve icon for VehicleDef
		/// </summary>
		/// <remarks>
		/// Removed icon based on lifeStages, vehicles don't have lifeStages
		/// </remarks>
		protected override void ResolveIcon()
		{
			if (graphic != null && graphic != BaseContent.BadGraphic)
			{
				Material material = graphic.ExtractInnerGraphicFor(null).MatAt(defaultPlacingRot, null);
				uiIcon = (Texture2D)material.mainTexture;
				uiIconColor = material.color;
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
