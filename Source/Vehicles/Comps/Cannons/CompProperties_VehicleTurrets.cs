using System.Linq;
using System.Collections.Generic;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class CompProperties_VehicleTurrets : VehicleCompProperties
	{
		//deploy time in seconds
		[PostToSettings(Label = "VF_DeployTime", Translate = true, UISettingsType = UISettingsType.FloatBox)]
		[NumericBoxValues(MinValue = 0)]
		[ActionOnSettingsInput(typeof(CompProperties_VehicleTurrets), nameof(CompProperties_VehicleTurrets.RecacheAllTurrets))]
		public float deployTime = 5;

		public List<VehicleTurret> turrets = new List<VehicleTurret>();

		public SoundDef deployingSustainer;
		public SoundDef deploySound;
		public SoundDef undeploySound;

		public CompProperties_VehicleTurrets()
		{
			compClass = typeof(CompVehicleTurrets);
		}

		public override void ResolveReferences(ThingDef parentDef)
		{
			base.ResolveReferences(parentDef);
			ResolveTurrets(parentDef as VehicleDef);
		}

		private void ResolveTurrets(VehicleDef vehicleDef)
		{
			if (!turrets.NullOrEmpty())
			{
				foreach (VehicleTurret turret in turrets)
				{
					turret.ResetAngle();
					turret.vehicleDef = vehicleDef;
					ResolveChildTurrets(turret);
					turret.turretDef.ammunition?.ResolveReferences();
				}
			}
		}

		public override void PostDefDatabase()
		{
			base.PostDefDatabase();

			//Cache turret graphics for UI
			foreach (VehicleTurret turret in turrets)
			{
				turret.ResolveCannonGraphics(turret.vehicleDef, forceRegen: true);
			}
		}

		/// <summary>
		/// Attach VehicleTurrets to parents.
		/// </summary>
		/// <remarks>Even though these VehicleTurret instances are for xml data only, the parent reference is necessary for UI rendering</remarks>
		/// <param name="turret"></param>
		private void ResolveChildTurrets(VehicleTurret turret)
		{
			turret.childTurrets = new List<VehicleTurret>();
			if (!string.IsNullOrEmpty(turret.parentKey))
			{
				foreach (VehicleTurret parentTurret in turrets.Where(c => c.key == turret.parentKey))
				{
					turret.attachedTo = parentTurret;
					if (parentTurret.attachedTo == turret || turret == parentTurret)
					{
						Log.Error($"Recursive turret attachments detected, this is not allowed. Disconnecting turret from parent.");
						turret.attachedTo = null;
					}
					else
					{
						parentTurret.childTurrets.Add(turret);
					}
				}
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

		private static void RecacheAllTurrets()
		{
			if (!Find.Maps.NullOrEmpty())
			{
				foreach (Map map in Find.Maps)
				{
					foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
					{
						if (pawn is VehiclePawn vehicle)
						{
							vehicle.CompVehicleTurrets?.RecacheDeployment();
						}
					}
				}
			}
		}
	}
}
