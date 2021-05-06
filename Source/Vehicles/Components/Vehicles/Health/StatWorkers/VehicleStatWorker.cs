using Verse;
using SmashTools;

namespace Vehicles
{
	public abstract class VehicleStatWorker
	{
		public VehicleStatCategoryDef statDef;

		public VehicleStatWorker()
		{
		}

		public abstract object Stat(VehiclePawn vehicle);

		public abstract void DrawVehicleStat(Listing_SplitColumns lister, VehiclePawn vehicle);

		public virtual string StatValueFormatted(VehiclePawn vehicle)
		{
			string output = Stat(vehicle)?.ToStringSafe() ?? "NULL";
			if (!statDef.formatString.NullOrEmpty())
			{
				output = string.Format(statDef.formatString, output);
			}
			return output;
		}

		public virtual string StatBuilderExplanation(VehiclePawn vehicle)
		{
			return statDef.description;
		}
	}
}
