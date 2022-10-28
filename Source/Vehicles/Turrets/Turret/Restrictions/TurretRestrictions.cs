using System;
using System.Collections.Generic;
using System.Linq;

namespace Vehicles
{
	public abstract class TurretRestrictions
	{
		protected VehiclePawn vehicle;
		protected VehicleTurret turret;

		public virtual void Init(VehiclePawn vehicle, VehicleTurret turret)
		{
			this.vehicle = vehicle;
			this.turret = turret;
		}

		public abstract bool Disabled { get; }

		public abstract string DisableReason { get; }
	}
}
