using System;
using System.Collections.Generic;
using Verse;
using Verse.Sound;
using RimWorld;

namespace Vehicles
{
	public static class SoundHelper
	{
		public static void PlayImpactSound(this VehiclePawn vehicle, VehicleComponent.DamageResult damageResult)
		{
			if (!vehicle.Spawned)
			{
				return;
			}

			SoundDef soundDef = damageResult.penetration switch
			{
				VehicleComponent.Penetration.Deflected => vehicle.VehicleDef.BodyType.soundImpactDeflect,
				VehicleComponent.Penetration.Diminished => vehicle.VehicleDef.BodyType.soundImpactDiminished,
				VehicleComponent.Penetration.NonPenetrated => vehicle.VehicleDef.BodyType.soundImpactNonPenetrated,
				VehicleComponent.Penetration.Penetrated => vehicle.VehicleDef.BodyType.soundImpactPenetrated,
				_ => throw new NotImplementedException("Unhandled Penetration result.")
			};
			if (soundDef.NullOrUndefined())
			{
				soundDef = SoundDefOf.BulletImpact_Ground;
			}
			soundDef.PlayOneShot(new TargetInfo(vehicle.PositionHeld, vehicle.Map, false));
		}
	}
}
