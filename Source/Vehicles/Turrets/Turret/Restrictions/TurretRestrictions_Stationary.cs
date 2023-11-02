using Verse;

namespace Vehicles
{
	public class TurretRestrictions_Stationary : TurretRestrictions
	{
		public override bool Disabled => vehicle.vehiclePather.Moving;

		public override string DisableReason => "VF_VehicleRestriction_Stationary".Translate();
	}
}
