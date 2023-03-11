using System;
using System.Collections.Generic;
using System.Threading;
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
		/// <paramref name="cell"/> is impassable for <paramref name="vehicle"/>
		/// </summary>
		/// <remarks>Method is Thread-safe for multithreaded pathfinding</remarks>
		/// <param name="c"></param>
		/// <param name="map"></param>
		/// <param name="vehicle"></param>
		public static bool ImpassableReverseThreaded(IntVec3 cell, Map map, Pawn vehicle)
		{
			if (cell == vehicle.Position)
			{
				return false;
			}
			else if (!cell.InBounds(map))
			{
				return true;
			}
			try
			{
				//Create new list for thread safety
				List<Thing> list = new List<Thing>(map.thingGrid.ThingsListAtFast(cell));
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].ImpassableForVehicles())
					{
						return true;
					}
				}
			}
			catch(Exception ex)
			{
				Log.ErrorOnce($"Exception Thrown in ThreadId [{Thread.CurrentThread.ManagedThreadId}] Exception: {ex.StackTrace}", vehicle.thingIDNumber ^ Thread.CurrentThread.ManagedThreadId);
			}
			return false;
		}

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
				thingList  = new List<Thing>(cell.GetThingList(vehicle.Map)); //Create snapshot of current thing list to avoid race condition with read / write access
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
					if (euclideanDistance < 20 || !otherVehicle.vPather.Moving)
					{
						return otherVehicle;
					}
				}
			}
			return null;
		}
	}
}
