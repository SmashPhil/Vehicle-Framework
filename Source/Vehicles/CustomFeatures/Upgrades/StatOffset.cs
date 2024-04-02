using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vehicles
{
	public class StatOffset
	{
		public readonly VehiclePawn vehicle;
		public readonly VehicleStatDef statDef;
		public readonly StatUpgradeCategoryDef upgradeCategoryDef;

		private float offset;
		
		public StatOffset(VehiclePawn vehicle, StatUpgradeCategoryDef upgradeCategoryDef)
		{
			this.vehicle = vehicle;
			this.upgradeCategoryDef = upgradeCategoryDef;
		}

		public StatOffset(VehiclePawn vehicle, VehicleStatDef statDef)
		{
			this.vehicle = vehicle;
			this.statDef = statDef;
		}

		public float Offset
		{
			get
			{
				return offset;
			}
			set
			{
				if (offset != value)
				{
					offset = value;
					vehicle.statHandler.MarkAllDirty();
				}
			}
		}
	}
}
