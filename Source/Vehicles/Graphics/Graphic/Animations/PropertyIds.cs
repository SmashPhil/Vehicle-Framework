using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vehicles
{
	public static class PropertyIds
	{
		public static int Moving = "Moving".GetHashCode();
		public static int Dead = "Dead".GetHashCode();
		public static int IgnitionOn = "IgnitionOn".GetHashCode();

		public static int PropellerSpinUp = "PropellerSpinUp".GetHashCode();
		public static int Launch = "Launch".GetHashCode();

		public static int Explode = "Explode".GetHashCode();
	}
}
