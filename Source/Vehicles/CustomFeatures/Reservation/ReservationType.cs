using System;
using System.Collections.Generic;
using System.Linq;

namespace Vehicles
{
	/// <summary>
	/// Only for consistency within string key for reservation listers.
	/// </summary>
	/// <remarks>Use any generic string as key for reservation listers.</remarks>
	public static class ReservationType
	{
		public const string LoadVehicle = "LoadVehicle";
		public const string Refuel = "Refuel";
		public const string Repair = "Repair";
		public const string Upgrade = "Upgrade";
		public const string LoadUpgradeMaterials = "LoadUpgradeMaterials";
	}
}
