using System;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;
using SmashTools;

namespace Vehicles
{
	public static class ThreadHelper
	{
		/// <summary>
		/// Any other vehicle is blocking the path of <paramref name="vehicle"/> at <paramref name="cell"/>
		/// </summary>
		/// <remarks>Exceptions thrown during Task will be handled at the TaskFactory level</remarks>
		/// <param name="c"></param>
		/// <param name="vehicle"></param>
		public static VehiclePawn AnyVehicleBlockingPathAt(IntVec3 cell, VehiclePawn vehicle)
		{
			List<Thing> thingList;
			if (UnityData.IsInMainThread)
			{
				thingList = cell.GetThingList(vehicle.Map);
			}
			else
			{
				//TODO - still not completely thread safe
				thingList  = new List<Thing>(cell.GetThingList(vehicle.Map));
			}
			if (thingList.Count == 0)
			{
				return null;
			}
			float euclideanDistance = Ext_Map.Distance(vehicle.Position, cell);
			for (int i = 0; i < thingList.Count; i++)
			{
				if (thingList[i] is VehiclePawn otherVehicle && otherVehicle != vehicle)
				{
					if (euclideanDistance < 20 || !otherVehicle.vehiclePather.Moving)
					{
						return otherVehicle;
					}
				}
			}
			return null;
		}
	}
}
