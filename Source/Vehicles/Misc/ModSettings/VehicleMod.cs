//#define UPGRADES_TAB

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using SmashTools.Performance;
using UnityEngine;
using Vehicles.Config;
using Verse;
using Verse.Sound;

namespace Vehicles;

[PublicAPI]
[StaticConstructorOnStartup]
public class VehicleMod : Mod
{
	public const float ResetImageSize = 22;

	internal static readonly ConcurrentDictionary<Type, List<FieldInfo>> CachedFields = [];
	internal static readonly HashSet<string> SettingsDisabledFor = [];

	public static VehiclesModSettings settings;
	public static VehicleMod mod;
	public static ModMetaData metaData;
	public static ModContentPack content;

	internal static VehicleDef selectedDef;

	internal string currentKey;
	internal static UpgradeNode selectedNode;
	internal static List<PatternDef> selectedPatterns = [];
	internal static CompProperties_UpgradeTree selectedDefUpgradeComp;

	private static List<TabRecord> tabs = [];

	internal static List<FieldInfo> vehicleDefFields = [];
	private static Dictionary<Type, List<FieldInfo>> vehicleCompFields = [];

	internal readonly FeatureFlags features;

	public VehicleMod(ModContentPack content) : base(content)
	{
		mod = this;
		settings = GetSettings<VehiclesModSettings>();
		InitializeSections();

		settings.colorStorage ??= new ColorStorage();
		selectedPatterns ??= [];

		CurrentSection = settings.main;

		VehicleMod.content = mod.Content;
		metaData = content.ModMetaData;

		features = FeatureFlags.InitDefault();

		GameEvent.OnNewGame += GizmoHelper.ResetDesignatorStatuses;
		GameEvent.OnLoadGame += GizmoHelper.ResetDesignatorStatuses;
		GameEvent.OnGenerateImpliedDefs += ImpliedDefGeneratorVehicles;

    DllInjector.LoadAllNativeAssemblies(content);
  }

	public static bool ModifiableSettings => settings.main.modifiableSettings;

	public static float FishingSkillValue => settings.main.fishingSkillIncrease / 100f;

	public static SettingsSection CurrentSection
  {
    get;
		set
		{
			if (field == value)
				return;

			field?.OnClose();
			field = value;
			field?.OnOpen();
		}
	}

	public static Dictionary<Type, List<FieldInfo>> VehicleCompFields
	{
		get
		{
			if (vehicleCompFields.NullOrEmpty())
			{
				ResetSelectedCachedTypes();
				vehicleDefFields =
					vehicleCompFields.TryGetValue(typeof(VehicleDef), []);
				vehicleCompFields.Remove(typeof(VehicleDef));
				vehicleCompFields.RemoveAll(d => d.Value.NullOrEmpty() || d.Value.All(f =>
					f.TryGetAttribute(out PostToSettingsAttribute postToSettings) &&
					postToSettings.UISettingsType == UISettingsType.None));
				vehicleCompFields = vehicleCompFields
				 .OrderByDescending(d => d.Key == typeof(List<VehicleStatModifier>))
				 .ThenByDescending(d => d.Key.SameOrSubclassOf(typeof(VehicleProperties)))
				 .ThenByDescending(d => d.Key.SameOrSubclassOf(typeof(VehicleJobLimitations)))
				 .ThenByDescending(d => d.Key.IsAssignableFrom(typeof(CompProperties)))
				 .ThenByDescending(d => d.Key.IsClass)
				 .ThenByDescending(d => d.Key.IsValueType && !d.Key.IsPrimitive && !d.Key.IsEnum)
				 .ToDictionary(d => d.Key, d => d.Value);
			}
			return vehicleCompFields;
		}
	}

	public static void SelectVehicle(VehicleDef vehicleDef)
	{
		selectedDef = vehicleDef;
		ClearSelectedDefCache();
		selectedPatterns = DefDatabase<PatternDef>.AllDefsListForReading
		 .Where(d => d.ValidFor(selectedDef)).ToList();
		selectedDefUpgradeComp = vehicleDef.GetSortedCompProperties<CompProperties_UpgradeTree>();
		CurrentSection.VehicleSelected();
	}

	public static void DeselectVehicle()
	{
		selectedDef = null;
		selectedPatterns.Clear();
		selectedDefUpgradeComp = null;
		selectedNode = null;
	}

	private static void InitializeSections()
	{
		settings.main ??= new SectionMain();
		settings.main.Initialize();

		settings.vehicles ??= new SectionVehicles();
		settings.vehicles.Initialize();

		settings.upgrades ??= new SectionUpgrades();
		settings.upgrades.Initialize();

		settings.debug ??= new SectionDebug();
		settings.debug.Initialize();
	}

	private static void ClearSelectedDefCache()
	{
		vehicleCompFields.Clear();
		vehicleDefFields.Clear();
	}

	private static void ResetSelectedCachedTypes()
	{
		if (selectedDef != null)
		{
			foreach (FieldInfo field in selectedDef.GetType().GetPostSettingsFields())
			{
				IterateTypeFields(typeof(VehicleDef), field);
			}
			foreach (CompProperties comp in selectedDef.comps)
			{
				foreach (FieldInfo field in comp.GetType().GetPostSettingsFields())
				{
					IterateTypeFields(comp.GetType(), field);
				}
			}
		}
	}

	private static void IterateTypeFields(Type containingType, FieldInfo field)
	{
		if (field.TryGetAttribute(out PostToSettingsAttribute postToSettingsAttr))
		{
			if (postToSettingsAttr.ParentHolder)
			{
				foreach (FieldInfo innerField in field.FieldType.GetPostSettingsFields())
				{
					IterateTypeFields(field.FieldType, innerField);
				}
			}
			else
			{
				if (!vehicleCompFields.ContainsKey(containingType))
				{
					vehicleCompFields.Add(containingType, []);
				}
				vehicleCompFields[containingType].Add(field);
			}
		}
	}

	internal static void PopulateCachedFields()
	{
		try
		{
			QuickIter.EnumerateAllModTypes(CacheForType);
		}
		catch (Exception ex)
		{
			Log.Error($"Exception thrown populating field cache for mod settings. Disable modifiable settings...\n{ex}");
			settings.main.modifiableSettings = false;
			CachedFields?.Clear();
		}
	}

	private static void CacheForType(Type type)
	{
		if (!type.HasAttribute<VehicleSettingsClassAttribute>())
			return;

		List<FieldInfo> fields = type.GetPostSettingsFields().ToList();
		if (!fields.NullOrEmpty())
		{
			CachedFields[type] = fields;
		}
	}

	public void InitializeTabs()
	{
		tabs =
		[
			new TabRecord("VF_MainSettings".Translate(),
				delegate { CurrentSection = settings.main; }, () => CurrentSection == settings.main),
		];
		if (ModifiableSettings)
		{
			tabs.Add(new TabRecord("VF_Vehicles".Translate(), delegate
			{
				CurrentSection = settings.vehicles;
				_ = SectionDrawer.VehicleDefs; // Trigger recache
			}, () => CurrentSection == settings.vehicles));
#if UPGRADES_TAB
				tabs.Add(new TabRecord("VF_Upgrades".Translate(), delegate()
				{
					CurrentSection = settings.upgrades;
				}, () => CurrentSection == settings.upgrades));
#endif
		}
		tabs.Add(new TabRecord("VF_DevMode".Translate(),
			delegate { CurrentSection = settings.debug; }, () => CurrentSection == settings.debug));
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		const float Padding = ResetImageSize + 5;

		base.DoSettingsWindowContents(inRect);

		Rect menuRect = inRect.ContractedBy(10f);
		menuRect.y += 20f;
		menuRect.height -= 20f;

		Widgets.DrawMenuSection(menuRect);
		TabDrawer.DrawTabs(menuRect, tabs);

		CurrentSection.OnGUI(menuRect);

		/* Reset Buttons */
		Rect resetAllButton = new(menuRect.width - Padding, menuRect.y + 15, ResetImageSize,
			ResetImageSize);

		if (Widgets.ButtonImage(CurrentSection.ButtonRect(resetAllButton), VehicleTex.ResetPage))
		{
			List<FloatMenuOption> options = CurrentSection.ResetOptions.ToList();
			FloatMenu floatMenu = new(options)
			{
				vanishIfMouseDistant = true
			};
			Find.WindowStack.Add(floatMenu);
		}
	}

	public override string SettingsCategory()
	{
		return "VehicleFramework".Translate();
	}

	public static void ResetAllSettings()
	{
		Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
			"VF_DevMode_ResetAllConfirmation".Translate(), ResetAllSettingsConfirmed));
	}

	private static void ResetAllSettingsConfirmed()
	{
		SoundDefOf.Click.PlayOneShotOnCamera();
		CachedFields.Clear();
		PopulateCachedFields();
		settings.main.ResetSettings();
		settings.vehicles.ResetSettings();
		settings.upgrades.ResetSettings();
		settings.debug.ResetSettings();

		if (Current.ProgramState == ProgramState.Playing)
		{
			foreach (Map map in Find.Maps)
			{
				map.GetCachedMapComponent<VehicleReservationManager>().ReleaseAllClaims();
			}
		}
	}

	public override void WriteSettings()
	{
		base.WriteSettings();
		selectedNode = null;
		Find.WindowStack.Windows.FirstOrDefault(w => w is Dialog_NodeSettings)?.Close();
	}

	[PublicAPI]
	public static void GenerateImpliedDefs<T, D>(bool hotReload)
		where T : IVehicleDefGenerator<D>, new()
		where D : Def, new()
	{
		T generator = new();
		foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
		{
			if (generator.TryGenerateImpliedDef(vehicleDef, out D impliedDef, hotReload))
				DefGenerator.AddImpliedDef(impliedDef, hotReload);
		}
	}

	public static bool GenerateImpliedDefs(VehicleDef vehicleDef, bool hotReload)
	{
		bool success = true;
		success &= TryGenerateImpliedDef<GeneratorVehiclePawnKindDef, PawnKindDef>(vehicleDef, false);
		// TODO - Expected to fail since build def generation is incomplete
		_ = TryGenerateImpliedDef<GeneratorVehicleBuildDef, VehicleBuildDef>(vehicleDef, false);

		if (vehicleDef.GetCompProperties<CompProperties_VehicleLauncher>() is not null)
		{
			success &= TryGenerateImpliedDef<GeneratorVehicleSkyfallerLeaving, ThingDef>(vehicleDef, false);
			success &= TryGenerateImpliedDef<GeneratorVehicleSkyfallerIncoming, ThingDef>(vehicleDef, false);
			success &= TryGenerateImpliedDef<GeneratorVehicleSkyfallerCrashing, ThingDef>(vehicleDef, false);
		}
		return success;
	}

	public static bool TryGenerateImpliedDef<T, D>(VehicleDef vehicleDef, bool hotReload)
		where T : IVehicleDefGenerator<D>, new()
		where D : Def, new()
	{
		T generator = new();
		return generator.TryGenerateImpliedDef(vehicleDef, out D impliedDef, hotReload);
	}

	/// <summary>
	/// Autogenerate implied PawnKindDefs for VehicleDefs
	/// </summary>
	private static void ImpliedDefGeneratorVehicles(bool hotReload)
	{
		GenerateImpliedDefs<GeneratorVehiclePawnKindDef, PawnKindDef>(hotReload);
		GenerateImpliedDefs<GeneratorVehicleBuildDef, VehicleBuildDef>(hotReload);
		GenerateImpliedDefs<GeneratorVehicleSkyfallerLeaving, ThingDef>(hotReload);
		GenerateImpliedDefs<GeneratorVehicleSkyfallerIncoming, ThingDef>(hotReload);
		GenerateImpliedDefs<GeneratorVehicleSkyfallerCrashing, ThingDef>(hotReload);
	}
}