using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class Dialog_StatSettings : Window
	{
		private VehicleDef vehicleDef;
		private Listing_SplitColumns listing;

		private static Vector2 scrollPos;

		public Dialog_StatSettings(VehicleDef vehicleDef)
		{
			this.vehicleDef = vehicleDef;
			this.doCloseX = true;
			this.resizeable = true;
			listing = new Listing_SplitColumns();
			scrollPos = Vector2.zero;
			RecacheHeight();
		}

		public override Vector2 InitialSize => new Vector2(400, 400);

		public float CachedHeight { get; private set; }

		public override void DoWindowContents(Rect inRect)
		{
			Rect rect = inRect.ContractedBy(10);
			Rect viewRect = new Rect(rect.x, rect.y, rect.width, CachedHeight);
			listing.BeginScrollView(rect, ref scrollPos, ref viewRect, 1);
			if (!vehicleDef.statBases.NullOrEmpty())
			{
				//TODO
			}
			if (!vehicleDef.vehicleStats.NullOrEmpty())
			{
				Dictionary<string, float> stats = VehicleMod.settings.vehicles.vehicleStats.TryGetValue(vehicleDef.defName, null);
				foreach (VehicleStatModifier statModifier in VehicleMod.selectedDef.vehicleStats)
				{
					UISettingsType settingsType = SettingsType(statModifier.statDef.toStringStyle);
					float value = stats?.TryGetValue(statModifier.statDef.defName, statModifier.value) ?? statModifier.value;
					string label = statModifier.statDef.LabelCap;
					if (stats?.ContainsKey(statModifier.statDef.defName) ?? false)
					{
						label = $"<color={UIElements.ToHex(Listing_Settings.modifiedColor)}>{label}</color>"; //Rich Text to avoid having to add optional parameter to all lister methods
					}
					switch (settingsType)
					{
						case UISettingsType.IntegerBox:
							{
								float newValue = value;
								listing.FloatBox(label, ref newValue, string.Empty, string.Empty, min: statModifier.statDef.minValue, max: statModifier.statDef.maxValue, lineHeight: 24, labelProportion: 0.5f);
								if (value != newValue)
								{
									if (!VehicleMod.settings.vehicles.vehicleStats.ContainsKey(vehicleDef.defName))
									{
										VehicleMod.settings.vehicles.vehicleStats[vehicleDef.defName] = new Dictionary<string, float>();
									}
									VehicleMod.settings.vehicles.vehicleStats[vehicleDef.defName][statModifier.statDef.defName] = Mathf.RoundToInt(newValue);
								}
							}
							break;
						case UISettingsType.FloatBox:
							{
								float newValue = value;
								listing.FloatBox(label, ref newValue, string.Empty, string.Empty, min: statModifier.statDef.minValue, max: statModifier.statDef.maxValue, lineHeight: 24, labelProportion: 0.5f);
								if (value != newValue)
								{
									if (!VehicleMod.settings.vehicles.vehicleStats.ContainsKey(vehicleDef.defName))
									{
										VehicleMod.settings.vehicles.vehicleStats[vehicleDef.defName] = new Dictionary<string, float>();
									}
									VehicleMod.settings.vehicles.vehicleStats[vehicleDef.defName][statModifier.statDef.defName] = newValue;
								}
							}
							break;
						case UISettingsType.SliderFloat:
							{
								float newValue = value;
								listing.SliderLabeled(label, ref newValue, string.Empty, string.Empty, string.Empty,
									min: statModifier.statDef.minValue, max: statModifier.statDef.maxValue, decimalPlaces: GetRoundingPlaces(statModifier.statDef.toStringStyle));
								if (value != newValue)
								{
									if (!VehicleMod.settings.vehicles.vehicleStats.ContainsKey(vehicleDef.defName))
									{
										VehicleMod.settings.vehicles.vehicleStats[vehicleDef.defName] = new Dictionary<string, float>();
									}
									VehicleMod.settings.vehicles.vehicleStats[vehicleDef.defName][statModifier.statDef.defName] = newValue;
								}
							}
							break;
					}

					Rect currentRect = listing.GetCurrentRect(24);
					if (Mouse.IsOver(currentRect))
					{
						Widgets.DrawHighlight(currentRect);
						if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
						{
							Event.current.Use();
							List<FloatMenuOption> options = new List<FloatMenuOption>();
							options.Add(new FloatMenuOption("ResetButton".Translate(), delegate ()
							{
								if (VehicleMod.settings.vehicles.vehicleStats.ContainsKey(vehicleDef.defName))
								{
									VehicleMod.settings.vehicles.vehicleStats[vehicleDef.defName].Remove(statModifier.statDef.defName);
								}
							}));
							FloatMenu floatMenu = new FloatMenu(options)
							{
								vanishIfMouseDistant = true
							};
							Find.WindowStack.Add(floatMenu);
						}
					}
				}
			}
			listing.EndScrollView(ref viewRect);
		}

		private UISettingsType SettingsType(ToStringStyle stringStyle)
		{
			return stringStyle switch
			{
				ToStringStyle.Integer => UISettingsType.IntegerBox,
				ToStringStyle.FloatOne => UISettingsType.FloatBox,
				ToStringStyle.FloatTwo => UISettingsType.FloatBox,
				ToStringStyle.FloatThree => UISettingsType.FloatBox,
				ToStringStyle.FloatMaxOne => UISettingsType.FloatBox,
				ToStringStyle.FloatMaxTwo => UISettingsType.FloatBox,
				ToStringStyle.FloatMaxThree => UISettingsType.FloatBox,
				ToStringStyle.FloatTwoOrThree => UISettingsType.FloatBox,
				ToStringStyle.PercentZero => UISettingsType.SliderFloat,
				ToStringStyle.PercentOne => UISettingsType.SliderFloat,
				ToStringStyle.PercentTwo => UISettingsType.SliderFloat,
				ToStringStyle.Temperature => UISettingsType.IntegerBox,
				ToStringStyle.TemperatureOffset => UISettingsType.IntegerBox,
				ToStringStyle.WorkAmount => UISettingsType.IntegerBox,
				ToStringStyle.Money => UISettingsType.IntegerBox,
				_ => UISettingsType.None
			};
		}

		private int GetRoundingPlaces(ToStringStyle stringStyle)
		{
			return stringStyle switch
			{
				ToStringStyle.Integer => 0,
				ToStringStyle.FloatOne => 1,
				ToStringStyle.FloatTwo => 2,
				ToStringStyle.FloatThree => 3,
				ToStringStyle.FloatMaxOne => 1,
				ToStringStyle.FloatMaxTwo => 2,
				ToStringStyle.FloatMaxThree => 3,
				ToStringStyle.FloatTwoOrThree => 3,
				ToStringStyle.PercentZero => 2,
				ToStringStyle.PercentOne => 3,
				ToStringStyle.PercentTwo => 4,
				ToStringStyle.Temperature => 0,
				ToStringStyle.TemperatureOffset => 0,
				ToStringStyle.WorkAmount => 0,
				ToStringStyle.Money => 0,
				_ => 0
			};
		}

		private void RecacheHeight()
		{
			CachedHeight = 0;
			using TextBlock textFont = new(GameFont.Small);
			//if (!vehicleDef.statBases.NullOrEmpty())
			//{
			//	//foreach (StatModifier _ in vehicleDef.statBases)
			//	//{
			//	//	CachedHeight += Text.LineHeight;
			//	//}
			//}
			if (!vehicleDef.vehicleStats.NullOrEmpty())
			{
				foreach (VehicleStatModifier _ in vehicleDef.vehicleStats)
				{
					CachedHeight += Text.LineHeight;
				}
			}
		}
	}
}
