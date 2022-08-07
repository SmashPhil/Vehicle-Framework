using System;
using System.Collections.Generic;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleStatDef : Def
	{
		public float defaultBaseValue;
		public float minValue = float.MinValue;
		public float maxValue = float.MaxValue;
		public List<VehicleStatPart> parts;
		public SettingsValueInfo modSettingsInfo;

		public string formatString;
		public ToStringStyle toStringStyle = ToStringStyle.Integer;
		public EfficiencyOperationType operationType;

		public Type workerClass = typeof(VehicleStatWorker);
		public int priority = 999;

		private VehicleStatWorker statWorker;

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

		public override void PostLoad()
		{
			modSettingsInfo.minValue = minValue;
			modSettingsInfo.maxValue = maxValue;
		}
	}
}
