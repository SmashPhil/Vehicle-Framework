using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public static class MultithreadHelper
	{
		public static async void RunAsync(Action action, bool reportFailure = true)
		{
			try
			{
				await Task.Run(action);
			}
			catch (Exception ex)
			{
				if (reportFailure)
				{
					string error = $"AsyncTask {action.Method.Name} threw exception while running. Exception = {ex}\n{StackTraceUtility.ExtractStringFromException(ex)}";
					Log.Error(error);
				}
			}
		}

		public static Task<(bool result, string reason)> CreateTask(Action action, CancellationToken? cancellationToken = null)
		{
			CancellationToken token = cancellationToken ?? CancellationToken.None;
			Task<(bool result, string reason)> recalculationTask = new Task<(bool result, string reason)>(delegate ()
			{
				try
				{
					action();
				}
				catch (Exception ex)
				{
					string reason = $"AsyncTask {action.Method.Name} threw exception while running. Exception = {ex}\n{StackTraceUtility.ExtractStringFromException(ex)}";
					return (false, reason);
				}
				return (true, string.Empty);
			}, token);
			return recalculationTask;
		}

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
					if (list[i].def.passability == Traversability.Impassable)
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
		/// <param name="actAsIfHadCollideWithPawnsJob"></param>
		/// <param name="collideOnlyWithStandingPawns"></param>
		/// <param name="forPathFinder"></param>
		public static Pawn AnyVehicleBlockingPathAt(IntVec3 cell, VehiclePawn vehicle, bool actAsIfHadCollideWithPawnsJob = false, bool collideOnlyWithStandingPawns = false, bool forPathFinder = false)
		{
			List<Thing> thingList = new List<Thing>(cell.GetThingList(vehicle.Map)); //Create new list so ref type list is not overriden mid-task
			if (thingList.Count == 0)
			{
				return null;
			}
			bool flag = false;
			if (actAsIfHadCollideWithPawnsJob)
			{
				flag = true;
			}
			else
			{
				Job curJob = vehicle.CurJob;
				if (curJob != null && (curJob.collideWithPawns || curJob.def.collideWithPawns || vehicle.jobs.curDriver.collideWithPawns))
				{
					flag = true;
				}
				else if (vehicle.Drafted)
				{
					bool moving = vehicle.vPather.Moving;
				}
			}
			for (int i = 0; i < thingList.Count; i++)
			{
				if (thingList[i] is Pawn pawn && pawn != vehicle && !pawn.Downed && (!collideOnlyWithStandingPawns || (!pawn.pather.MovingNow && (!pawn.pather.Moving || !pawn.pather.MovedRecently(60)))))
				{
					if (pawn.HostileTo(vehicle))
					{
						return pawn;
					}
					if (flag && (forPathFinder || !vehicle.Drafted || !pawn.RaceProps.Animal))
					{
						Job curJob2 = pawn.CurJob;
						if (curJob2 != null && (curJob2.collideWithPawns || curJob2.def.collideWithPawns || pawn.jobs.curDriver.collideWithPawns))
						{
							return pawn;
						}
					}
				}
			}
			return null;
		}
	}
}
