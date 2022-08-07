using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	[LoadedEarly]
	[StaticConstructorOnStartup]
	public class VehicleMod : Mod
	{
		public const int MaxCoastalSettlementPush = 21;
		public const float ResetImageSize = 25f;

		public static VehiclesModSettings settings;
		public static VehicleMod mod;
		public static VehicleDef selectedDef;

		public static Color SemiLightGrey = new Color(0.1f, 0.1f, 0.1f);

		public static Vector2 saveableFieldsScrollPosition;
		public static Vector2 vehicleDefsScrollPosition;
		public static float scrollableViewHeight;

		public string currentKey;
		public static UpgradeNode selectedNode;
		public static List<PatternDef> selectedPatterns = new List<PatternDef>();
		public static CompProperties_UpgradeTree selectedDefUpgradeComp;

		private static List<TabRecord> tabs = new List<TabRecord>();
		private static List<VehicleDef> vehicleDefs;

		private static QuickSearchFilter vehicleFilter = new QuickSearchFilter();

		public static List<FieldInfo> vehicleDefFields = new List<FieldInfo>();
		private static Dictionary<Type, List<FieldInfo>> vehicleCompFields = new Dictionary<Type, List<FieldInfo>>();
		public static readonly Dictionary<Type, List<FieldInfo>> cachedFields = new Dictionary<Type, List<FieldInfo>>();

		public static readonly HashSet<string> settingsDisabledFor = new HashSet<string>();

		public VehicleMod(ModContentPack content) : base(content)
		{
			mod = this;
			settings = GetSettings<VehiclesModSettings>();
			InitializeSections();

			settings.colorStorage ??= new ColorStorage();
			selectedPatterns ??= new List<PatternDef>();

			CurrentSection = settings.main;
		}

		public static SettingsSection CurrentSection { get; private set; }

		public static bool ModifiableSettings => settings.main.modifiableSettings;
		public static int CoastRadius => settings.main.forceFactionCoastRadius >= MaxCoastalSettlementPush ? 9999 : settings.main.forceFactionCoastRadius;
		public static float FishingSkillValue => settings.main.fishingSkillIncrease / 100;

		public static Dictionary<Type, List<FieldInfo>> VehicleCompFields
		{
			get
			{
				if (vehicleCompFields.EnumerableNullOrEmpty())
				{
					ResetSelectedCachedTypes();
					vehicleDefFields = vehicleCompFields.TryGetValue(typeof(VehicleDef), new List<FieldInfo>());
					vehicleCompFields.Remove(typeof(VehicleDef));
					vehicleCompFields.RemoveAll(d => d.Value.NullOrEmpty() || d.Value.All(f => f.TryGetAttribute<PostToSettingsAttribute>(out var settings) && settings.UISettingsType == UISettingsType.None));
					vehicleCompFields = vehicleCompFields.OrderByDescending(d => d.Key == typeof(List<VehicleStatModifier>))
														 .ThenByDescending(d => d.Key.SameOrSubclass(typeof(VehicleProperties)))
														 .ThenByDescending(d => d.Key.SameOrSubclass(typeof(VehicleJobLimitations)))
														 .ThenByDescending(d => d.Key.IsAssignableFrom(typeof(CompProperties)))
														 .ThenByDescending(d => d.Key.IsClass)
														 .ThenByDescending(d => d.Key.IsValueType && !d.Key.IsPrimitive && !d.Key.IsEnum)
														 .ToDictionary(d => d.Key, d => d.Value);
				}
				return vehicleCompFields;
			}
		}
		
		private static List<VehicleDef> VehicleDefs
		{
			get
			{
				if (vehicleDefs.NullOrEmpty())
				{
					vehicleDefs = DefDatabase<VehicleDef>.AllDefs.OrderBy(d => d.modContentPack.PackageId.Contains(VehicleHarmony.VehiclesUniqueId)).ThenBy(d2 => d2.modContentPack.PackageId).ToList();
				}
				return vehicleDefs;
			}
		}

		private static void SelectVehicle(VehicleDef vehicleDef)
		{
			selectedDef = vehicleDef;
			ClearSelectedDefCache();
			selectedPatterns = DefDatabase<PatternDef>.AllDefs.Where(d => d.ValidFor(selectedDef)).ToList();
			selectedDefUpgradeComp = vehicleDef.GetSortedCompProperties<CompProperties_UpgradeTree>();
			CurrentSection.VehicleSelected();
			RecalculateHeight(selectedDef);
			SetVehicleTex(selectedDef);
		}

		private static void DeselectVehicle()
		{
			selectedDef = null;
			selectedPatterns.Clear();
			selectedDefUpgradeComp = null;
			selectedNode = null;
			SetVehicleTex(null);
		}
		
		public static void SetVehicleTex(VehicleDef selectedDef)
		{
			if (selectedDef is null)
			{
				return;
			}
			var bodyGraphicData = selectedDef.graphicData;
			var graphicData = new GraphicDataRGB();
			graphicData.CopyFrom(bodyGraphicData);
		}

		private static void InitializeSections()
		{
			settings.main ??= new Section_Main();
			settings.main.Initialize();

			settings.vehicles ??= new Section_Vehicles();
			settings.vehicles.Initialize();

			settings.upgrades ??= new Section_Upgrade();
			settings.upgrades.Initialize();

			settings.debug ??= new Section_Debug();
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
			if (field.TryGetAttribute(out PostToSettingsAttribute settings))
			{
				if (settings.ParentHolder)
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
						vehicleCompFields.Add(containingType, new List<FieldInfo>());
					}
					vehicleCompFields[containingType].Add(field);
				}
			}
		}

		internal static void PopulateCachedFields()
		{
			foreach (Type type in GenTypes.AllTypes)
			{
				CacheForType(type);
			}
		}

		private static void CacheForType(Type type)
		{
			var fields = type.GetPostSettingsFields();
			if (!fields.NullOrEmpty())
			{
				cachedFields.Add(type, fields);
			}
		}

		public void InitializeTabs()
		{
			tabs = new List<TabRecord>();
			tabs.Add(new TabRecord("VF_MainSettings".Translate(), delegate ()
			{
				CurrentSection = settings.main;
			}, () => CurrentSection == settings.main));
			if (ModifiableSettings)
			{
				tabs.Add(new TabRecord("VF_Vehicles".Translate(), delegate()
				{
					CurrentSection = settings.vehicles;
				}, () => CurrentSection == settings.vehicles));
				/*
				tabs.Add(new TabRecord("VF_Upgrades".Translate(), delegate()
				{
					CurrentSection = settings.upgrades;
				}, () => CurrentSection == settings.upgrades));
				*/
			}
			tabs.Add(new TabRecord("DevModeVehicles".Translate(), delegate()
			{
				CurrentSection = settings.debug;
			}, () => CurrentSection == settings.debug));
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			base.DoSettingsWindowContents(inRect);

			Rect menuRect = inRect.ContractedBy(10f);
			menuRect.y += 20f;
			menuRect.height -= 20f;

			Widgets.DrawMenuSection(menuRect);
			TabDrawer.DrawTabs(menuRect, tabs, 200f);

			CurrentSection.DrawSection(menuRect);

			/* Reset Buttons */
			float padding = ResetImageSize + 5;
			Rect resetAllButton = new Rect(menuRect.width - padding, menuRect.y + 15, ResetImageSize, ResetImageSize);
			Rect settingsButton = new Rect(resetAllButton.x - padding, resetAllButton.y, ResetImageSize, ResetImageSize);
			
			if (Widgets.ButtonImage(CurrentSection.ButtonRect(resetAllButton), VehicleTex.ResetPage))
			{
				List<FloatMenuOption> options = CurrentSection.ResetOptions.ToList();
				FloatMenu floatMenu = new FloatMenu(options)
				{
					vanishIfMouseDistant = true
				};
				//floatMenu.onCloseCallback...
				Find.WindowStack.Add(floatMenu);
			}
			if (Widgets.ButtonImage(CurrentSection.ButtonRect(settingsButton), VehicleTex.ExportSettings))
			{

			}
		}

		public override string SettingsCategory()
		{
			return "VF_Vehicles".Translate();
		}

		public static void ResetAllSettings()
		{
			Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("DevModeResetAllConfirmation".Translate(), delegate()
			{
				ResetAllSettingsConfirmed();
			}, false, null));
		}

		internal static void ResetAllSettingsConfirmed()
		{
			SoundDefOf.Click.PlayOneShotOnCamera(null);
			cachedFields.Clear();
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

		public static Rect DrawVehicleList(Rect rect, Func<bool, string> tooltipGetter = null, Predicate<VehicleDef> validator = null)
		{
			var font = Text.Font;
			Rect scrollContainer = rect.ContractedBy(10);
			scrollContainer.width /= 4;
			Widgets.DrawBoxSolid(scrollContainer, Color.grey);
			Rect innerContainer = scrollContainer.ContractedBy(1);
			Widgets.DrawBoxSolid(innerContainer, ListingExtension.MenuSectionBGFillColor);

			Rect searchBoxRect = new Rect(innerContainer)
			{
				height = 22f
			};
			Text.Font = GameFont.Small;
			Widgets.Label(searchBoxRect, "VehicleListSearchText".Translate());
			searchBoxRect.y += searchBoxRect.height;
			vehicleFilter.Text = Widgets.TextField(searchBoxRect, vehicleFilter.Text);
			Text.Font = font;

			Rect scrollList = new Rect(innerContainer.ContractedBy(1))
			{
				y = innerContainer.y + searchBoxRect.height * 2.15f
			};
			Rect scrollView = scrollList;
			scrollView.height = VehicleDefs.Count * 22f;

			List<VehicleDef> vehicleDefsFiltered = VehicleDefs.Where(v => vehicleFilter.Text.NullOrEmpty() || vehicleFilter.Matches(v.defName) || vehicleFilter.Matches(v.label) || vehicleFilter.Matches(v.modContentPack.Name)).ToList();
			if (selectedDef != null)
			{
				if (KeyBindingDefOf.MapDolly_Up.KeyDownEvent)
				{
					int index = vehicleDefsFiltered.IndexOf(selectedDef) - 1;
					if (index < 0)
					{
						index = vehicleDefsFiltered.Count - 1;
					}
					SelectVehicle(vehicleDefsFiltered[index]);
				}
				if (KeyBindingDefOf.MapDolly_Down.KeyDownEvent)
				{
					int index = vehicleDefsFiltered.IndexOf(selectedDef) + 1;
					if (index >= vehicleDefsFiltered.Count)
					{
						index = 0;
					}
					SelectVehicle(vehicleDefsFiltered[index]);
				}
			}
			
			Widgets.BeginScrollView(scrollList, ref vehicleDefsScrollPosition, scrollView);
			Listing_Standard listingStandard = new Listing_Standard();
			listingStandard.Begin(scrollList);
			string currentModTitle = string.Empty;
			foreach (VehicleDef vehicle in vehicleDefsFiltered)
			{
				try
				{
					if (currentModTitle != vehicle.modContentPack.Name)
					{
						currentModTitle = vehicle.modContentPack.Name;
						listingStandard.Header(currentModTitle, ListingExtension.BannerColor, GameFont.Medium, TextAnchor.MiddleCenter);
					}
					bool validated = validator is null || validator(vehicle);
					string tooltip = tooltipGetter != null ? tooltipGetter(validated) : string.Empty;
					if (listingStandard.ListItemSelectable(vehicle.defName, Color.yellow, selectedDef == vehicle, validated, tooltip))
					{
						if (selectedDef == vehicle)
						{
							DeselectVehicle();
						}
						else
						{
							SelectVehicle(vehicle);
						}
					}
				}
				catch (Exception ex)
				{
					Log.Error($"Exception thrown while trying to select {vehicle.defName}. Disabling vehicle to preserve mod settings.\nException={ex.Message}");
					selectedDef = null;
					selectedPatterns.Clear();
					selectedDefUpgradeComp = null;
					selectedNode = null;
					SetVehicleTex(null);
					settingsDisabledFor.Add(vehicle.defName);
				}
			}
			listingStandard.End();
			Widgets.EndScrollView();
			Text.Font = font;
			return scrollContainer;
		}

		public override void WriteSettings()
		{
			base.WriteSettings();
			selectedNode = null;
			Find.WindowStack.Windows.FirstOrDefault(w => w is Dialog_NodeSettings)?.Close();
		}

		private static void RecalculateHeight(VehicleDef def, int columns = 3)
		{
			float propertySectionHeight = 5; //Buffer for bottom scrollable
			foreach (var saveableObject in VehicleCompFields)
			{
				if (saveableObject.Value.NullOrEmpty() || saveableObject.Value.All(f => f.TryGetAttribute<PostToSettingsAttribute>(out var settings) && settings.VehicleType != VehicleType.Universal && settings.VehicleType != def.vehicleType))
				{
					continue;
				}
				int rows = Mathf.CeilToInt(saveableObject.Value.Count / columns);
				propertySectionHeight += 50 + rows * 16; //72
			}
			scrollableViewHeight = propertySectionHeight;
		}
	}
}
