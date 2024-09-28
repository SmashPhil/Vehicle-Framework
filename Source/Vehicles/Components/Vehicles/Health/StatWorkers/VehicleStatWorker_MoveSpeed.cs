using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vehicles
{
	public class VehicleStatWorker_MoveSpeed : VehicleStatWorker
	{
		public override bool ShouldShowFor(VehicleDef vehicleDef)
		{
			if (vehicleDef.vehicleMovementPermissions == VehiclePermissions.NotAllowed)
			{
				return false;
			}
			return base.ShouldShowFor(vehicleDef);
		}
	}
}
