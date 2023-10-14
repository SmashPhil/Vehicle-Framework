using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace Vehicles
{
	public static class ThingDefGenerator_Skyfallers
	{
		public static bool GenerateImpliedSkyfallerDef(VehicleDef vehicleDef, out ThingDef skyfallerLeavingImpliedDef, out ThingDef skyfallerIncomingImpliedDef, out ThingDef skyfallerCrashingImpliedDef)
		{
			skyfallerLeavingImpliedDef = null;
			skyfallerIncomingImpliedDef = null;
			skyfallerCrashingImpliedDef = null;
			if (vehicleDef.GetCompProperties<CompProperties_VehicleLauncher>() is CompProperties_VehicleLauncher comp)
			{
				if (comp.skyfallerLeaving == null)
				{
					skyfallerLeavingImpliedDef = new ThingDef()
					{
						defName = $"{vehicleDef.defName}Leaving",
						label = $"{vehicleDef.defName}Leaving",
						thingClass = typeof(VehicleSkyfaller_Leaving),
						category = ThingCategory.Ethereal,
						useHitPoints = false,
						drawOffscreen = true,
						tickerType = TickerType.Normal,
						altitudeLayer = AltitudeLayer.Skyfaller,
						drawerType = DrawerType.RealtimeOnly,
						skyfaller = new SkyfallerProperties()
						{
							shadow = "Things/Skyfaller/SkyfallerShadowDropPod",
							shadowSize = vehicleDef.Size.ToVector2(),
						}
					};
					comp.skyfallerLeaving = skyfallerLeavingImpliedDef;
				}
				if (comp.skyfallerIncoming == null)
				{
					skyfallerIncomingImpliedDef = new ThingDef()
					{
						defName = $"{vehicleDef.defName}Incoming",
						label = $"{vehicleDef.defName}Incoming",
						thingClass = typeof(VehicleSkyfaller_Arriving),
						category = ThingCategory.Ethereal,
						useHitPoints = false,
						drawOffscreen = true,
						tickerType = TickerType.Normal,
						altitudeLayer = AltitudeLayer.Skyfaller,
						drawerType = DrawerType.RealtimeOnly,
						skyfaller = new SkyfallerProperties()
						{
							shadow = "Things/Skyfaller/SkyfallerShadowDropPod",
							shadowSize = vehicleDef.Size.ToVector2()
						}
					};
					comp.skyfallerIncoming = skyfallerIncomingImpliedDef;
				}
				if (comp.skyfallerCrashing == null)
				{
					skyfallerCrashingImpliedDef = new ThingDef()
					{
						defName = $"{vehicleDef.defName}Crashing",
						label = $"{vehicleDef.defName}Crashing",
						thingClass = typeof(VehicleSkyfaller_Crashing),
						category = ThingCategory.Ethereal,
						useHitPoints = false,
						drawOffscreen = true,
						tickerType = TickerType.Normal,
						altitudeLayer = AltitudeLayer.Skyfaller,
						drawerType = DrawerType.RealtimeOnly,
						skyfaller = new SkyfallerProperties()
						{
							shadow = "Things/Skyfaller/SkyfallerShadowDropPod",
							shadowSize = vehicleDef.Size.ToVector2(),
							movementType = SkyfallerMovementType.ConstantSpeed,
							explosionRadius = Mathf.Max(vehicleDef.Size.x, vehicleDef.Size.z) * 1.5f,
							explosionDamage = DamageDefOf.Bomb,
							rotateGraphicTowardsDirection = vehicleDef.rotatable,
							speed = 2,
							ticksToImpactRange = new IntRange(300, 350)
						}
					};
					comp.skyfallerCrashing = skyfallerCrashingImpliedDef;
				}
				return true;
			}
			return false;
		}
	}
}
