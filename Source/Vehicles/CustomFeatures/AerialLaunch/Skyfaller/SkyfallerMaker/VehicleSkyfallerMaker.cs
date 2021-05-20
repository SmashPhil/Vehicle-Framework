using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public static class VehicleSkyfallerMaker
	{
		/// <summary>
		/// Generate VehicleSkyfaller with newly generated vehicle
		/// </summary>
		/// <param name="def">test</param>
		/// <param name="vehicleDef"></param>
		/// <param name="faction"></param>
		public static VehicleSkyfaller MakeSkyfaller(ThingDef def, VehicleDef vehicleDef, Faction faction, bool randomizeColors = false, bool randomizeMask = false, bool cleanSlate = true)
		{
			VehicleSkyfaller skyfaller = (VehicleSkyfaller)ThingMaker.MakeThing(def);
			skyfaller.vehicle = VehicleSpawner.GenerateVehicle(new VehicleGenerationRequest(vehicleDef, faction, randomizeColors, randomizeMask, cleanSlate));
			return skyfaller;
		}

		/// <summary>
		/// Generate VehicleSkyfaller with preassigned <paramref name="vehicle"/>
		/// </summary>
		/// <param name="def"></param>
		/// <param name="vehicle"></param>
		public static VehicleSkyfaller MakeSkyfaller(ThingDef def, VehiclePawn vehicle)
		{
			VehicleSkyfaller skyfaller = (VehicleSkyfaller)ThingMaker.MakeThing(def);
			skyfaller.vehicle = vehicle;
			return skyfaller;
		}

		/// <summary>
		/// Generate VehicleSkyfaller_FlyOver with preassigned <paramref name="vehicle"/> from <paramref name="start"/> to <paramref name="end"/>
		/// </summary>
		/// <param name="def"></param>
		/// <param name="vehicle"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <returns></returns>
		public static VehicleSkyfaller_FlyOver MakeSkyfallerFlyOver(ThingDef def, VehiclePawn vehicle, IntVec3 start, IntVec3 end)
		{
			try
			{
				VehicleSkyfaller_FlyOver skyfaller = (VehicleSkyfaller_FlyOver)MakeSkyfaller(def, vehicle);
				skyfaller.start = start;
				skyfaller.end = end;
				skyfaller.angle = start.AngleToPoint(end);
				return skyfaller;
			}
			catch (Exception ex)
			{
				Log.Error($"Unable to generate VehicleSkyfaller of type <type>{def.thingClass}</type>. Exception=\"{ex.Message}\"");
			}
			return null;
		}
	}
}
