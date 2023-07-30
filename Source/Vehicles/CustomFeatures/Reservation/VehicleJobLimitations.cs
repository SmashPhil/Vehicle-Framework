using System;
using System.Globalization;
using Verse;
using SmashTools;

namespace Vehicles
{
	[HeaderTitle(Label = "VF_JobLimitations", Translate = true)]
	public struct VehicleJobLimitations
	{
		public string defName;
		[PostToSettings(Label = "VF_MaxWorkers", Translate = true)]
		public int maxWorkers;

		public VehicleJobLimitations(string defName, int maxWorkers)
		{
			this.defName = defName;
			this.maxWorkers = maxWorkers;
		}

		public bool IsValid => !defName.NullOrEmpty();

		public static VehicleJobLimitations Invalid => new VehicleJobLimitations(string.Empty, 0);

		public static VehicleJobLimitations FromString(string entry)
		{
			entry = entry.TrimStart(new char[] { '(' }).TrimEnd(new char[] { ')' });
			string[] data = entry.Split(new char[] { ',' });

			try
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				string defName = Convert.ToString(data[0], invariantCulture);
				int workers = Convert.ToInt32(data[1], invariantCulture);
				return new VehicleJobLimitations(defName, workers);
			}
			catch(Exception ex)
			{
				SmashLog.Error($"{entry} is not a valid <struct>VehicleJobLimitations</struct> format. Exception: {ex}");
				return Invalid;
			}
		}

		public override string ToString()
		{
			return $"({defName},{maxWorkers})";
		}
	}
}
