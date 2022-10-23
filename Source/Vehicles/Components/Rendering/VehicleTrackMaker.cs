using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public class VehicleTrackMaker
	{
		private readonly VehiclePawn vehicle;

		private Vector3 lastTrackPlacePos;
		
		public VehicleTrackMaker(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
		}

		public void ProcessPostTickVisuals(int ticksPassed)
		{
			if (vehicle.VehicleDef.properties.track != null)
			{
				TerrainDef terrain = vehicle.Position.GetTerrain(vehicle.Map);
				if (terrain == null)
				{
					return;
				}
				vehicle.VehicleDef.properties.track.TryPlaceTrack(vehicle, ref lastTrackPlacePos);
			}
		}
	}
}
