using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vehicles
{
	public static class VehicleEventMethods
	{
		public static void SetComponentHealth(VehiclePawn vehicle, string key, float health)
		{
			vehicle.statHandler.SetComponentHealth(key, health);
		}
	}
}
