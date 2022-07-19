using System.Linq;
using System.Collections.Generic;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class CompProperties_VehicleTurrets : CompProperties
	{
		public List<VehicleTurret> turrets = new List<VehicleTurret>();

		public CompProperties_VehicleTurrets()
		{
			compClass = typeof(CompVehicleTurrets);
		}

		public override void ResolveReferences(ThingDef parentDef)
		{
			base.ResolveReferences(parentDef);
			if (turrets.NotNullAndAny())
			{
				turrets.ForEach(c => c.turretDef.ammunition?.ResolveReferences());
			}
		}

		public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
		{
			foreach (string error in base.ConfigErrors(parentDef))
			{
				yield return error;
			}
			if (parentDef is VehicleDef vehicleDef)
			{
				foreach (VehicleTurret turret in turrets)
				{
					foreach (string error in turret.ConfigErrors(vehicleDef))
					{
						yield return error;
					}
				}
			}
			else
			{
				yield return "<field>parentDef</field> must be a <type>VehicleDef</type> in order to implement <type>CompCannons</type>.".ConvertRichText();
			}
		}
	}
}
