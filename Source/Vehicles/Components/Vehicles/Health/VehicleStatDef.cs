using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class VehicleStatDef : Def, IDefIndex<VehicleStatDef>
	{
		public float defaultBaseValue;
		public float minValue = float.MinValue;
		public float maxValue = float.MaxValue;

		public float hideAtValue = float.NaN;
		public bool alwaysHide;
		public bool showIfUndefined;
		public bool neverDisabled;
		public bool showZeroBaseValue;
		public bool applyFactorsIfNegative = true;

		public List<VehicleStatDef> statFactors;
		public List<VehicleStatPart> parts;
		public SettingsValueInfo modSettingsInfo;
		public StatCategoryDef category;
		public List<string> showIfModsLoaded;
		public List<VehicleType> showOnVehicleTypes;

		public string formatString;
		public ToStringStyle toStringStyle = ToStringStyle.Integer;
		public ToStringNumberSense toStringNumberSense = ToStringNumberSense.Absolute;
		public EfficiencyOperationType operationType = EfficiencyOperationType.None;

		public SimpleCurve postProcessCurve;
		public List<VehicleStatDef> postProcessStatFactors;

		public Type workerClass = typeof(VehicleStatWorker);
		public int displayPriorityInCategory = 1;

		[MustTranslate]
		public string formatStringUnfinalized;
		[Unsaved]
		private VehicleStatWorker statWorker;
		[Unsaved]
		private ToStringStyle? toStringStyleUnfinalized;

		public int DefIndex { get; set; }

		public VehicleStatWorker Worker
		{
			get
			{
				if (statWorker == null)
				{
					if (!parts.NullOrEmpty())
					{
						foreach (VehicleStatPart statPart in parts)
						{
							statPart.statDef = this;
						}
					}
					statWorker = (VehicleStatWorker)Activator.CreateInstance(workerClass);
					statWorker.InitStatWorker(this);
				}
				return statWorker;
			}
		}

		public ToStringStyle ToStringStyleUnfinalized
		{
			get
			{
				if (toStringStyleUnfinalized == null)
				{
					return toStringStyle;
				}
				return toStringStyleUnfinalized.Value;
			}
		}

		public override void PostLoad()
		{
			modSettingsInfo.minValue = minValue;
			modSettingsInfo.maxValue = maxValue;
		}

		public string ValueToString(float val, bool finalized = true, ToStringNumberSense numberSense = ToStringNumberSense.Absolute)
		{
			return Worker.ValueToString(val, finalized, numberSense);
		}

		public bool CanShowWithLoadedMods()
		{
			if (!showIfModsLoaded.NullOrEmpty())
			{
				for (int i = 0; i < showIfModsLoaded.Count; i++)
				{
					if (!ModsConfig.IsActive(showIfModsLoaded[i]))
					{
						return false;
					}
				}
			}
			return true;
		}

		public bool CanShowWithVehicle(VehicleDef vehicleDef)
		{
			if (!showOnVehicleTypes.NullOrEmpty())
			{
				return showOnVehicleTypes.Contains(vehicleDef.vehicleType);
			}
			return true;
		}
	}
}
