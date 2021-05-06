using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vehicles
{
	public static class VehicleIncidentSwapper
	{
		public static HashSet<Type> vehicleLordCategories = new HashSet<Type>();

		public static void RegisterLordType(Type type)
		{
			vehicleLordCategories.Add(type);
		}

		public static void RegisterIncident()
		{
			throw new NotImplementedException();
		}
	}
}
