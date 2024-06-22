using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vehicles
{
	public class UpgradeState
	{
		public string key;
		public List<Setting> settings;

		public abstract class Setting
		{
			public abstract void Unlocked(VehiclePawn vehicle, bool unlockingPostLoad);

			public abstract void Refunded(VehiclePawn vehicle);
		}
	}
}
