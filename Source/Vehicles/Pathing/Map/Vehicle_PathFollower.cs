using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using SmashTools;
using SmashTools.Performance;

namespace Vehicles
{
	public class Vehicle_PathFollower : IExposable
	{
		private const int MaxMoveTicks = 450;
		private const int MaxCheckAheadNodes = 20;
		private const float SnowReductionFromWalking = 0.001f;
		private const int ClamorCellsInterval = 12;
		private const int MinCostWalk = 50;
		private const int MinCostAmble = 60;
		private const int CheckForMovingCollidingPawnsIfCloserToTargetThanX = 30;
		private const int AttackBlockingHostilePawnAfterTicks = 180;

		protected VehiclePawn vehicle;

		private List<IntVec3> bumperCells;

		private bool moving;

		public IntVec3 nextCell;

		private IntVec3 lastCell;

		public float nextCellCostLeft;

		public float nextCellCostTotal = 1f;

		private int cellsUntilClamor;

		private int lastMovedTick = -999999;

		private LocalTargetInfo destination;

		private PathEndMode peMode;

		public PawnPath curPath;

		public IntVec3 lastPathedTargetPosition;

		private int foundPathWhichCollidesWithPawns = -999999;

		private int foundPathWithDanger = -999999;

		private int failedToFindCloseUnoccupiedCellTicks = -999999;

		private List<CancellationTokenSource> tokenSources = new List<CancellationTokenSource>();

		public Vehicle_PathFollower(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
			bumperCells = new List<IntVec3>();
		}

		public bool Recalculating { get; private set; }

		public LocalTargetInfo Destination => destination;

		public bool Moving => moving;

		public IntVec3 LastPassableCellInPath
		{
			get
			{
				if (!Moving || curPath == null)
				{
					return IntVec3.Invalid;
				}
				if (!Destination.Cell.Impassable(vehicle.Map))
				{
					return Destination.Cell;
				}
				List<IntVec3> nodesReversed = curPath.NodesReversed;
				for (int i = 0; i < nodesReversed.Count; i++)
				{
					if (!nodesReversed[i].Impassable(vehicle.Map))
					{
						return nodesReversed[i];
					}
				}
				if (!vehicle.Position.Impassable(vehicle.Map))
				{
					return vehicle.Position;
				}
				return IntVec3.Invalid;
			}
		}

		public void RecalculatePermissions()
		{
			if (Moving && (!vehicle.CanMoveFinal || !vehicle.Drafted))
			{
				PatherFailed();
			}
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref moving, nameof(moving));
			Scribe_Values.Look(ref nextCell, nameof(nextCell));
			Scribe_Values.Look(ref nextCellCostLeft, nameof(nextCellCostLeft));
			Scribe_Values.Look(ref nextCellCostTotal, nameof(nextCellCostTotal));
			Scribe_Values.Look(ref peMode, nameof(peMode));
			Scribe_Values.Look(ref cellsUntilClamor, nameof(cellsUntilClamor));
			Scribe_Values.Look(ref lastMovedTick, nameof(lastMovedTick), -999999);
			if (moving)
			{
				Scribe_TargetInfo.Look(ref destination, "destination");
			}
		}

		public void StartPath(LocalTargetInfo dest, PathEndMode peMode)
		{
			if (!vehicle.Drafted)
			{
				PatherFailed();
				return;
			}

			dest = (LocalTargetInfo)GenPathVehicles.ResolvePathMode(vehicle.VehicleDef, vehicle.Map, dest.ToTargetInfo(vehicle.Map), ref peMode);
			if (dest.HasThing && dest.ThingDestroyed)
			{
				Log.Error(vehicle + " pathing to destroyed thing " + dest.Thing);
				PatherFailed();
				return;
			}
			//Add Building and Position Recoverable extras
			if (!GenGridVehicles.Walkable(vehicle.Position, vehicle.VehicleDef, vehicle.Map))
			{
				PatherFailed();
				return;
			}
			if (Moving && curPath != null && destination == dest && this.peMode == peMode)
			{
				PatherFailed();
				return;
			}
			if (!vehicle.Map.GetCachedMapComponent<VehicleMapping>()[vehicle.VehicleDef].VehicleReachability.CanReachVehicle(vehicle.Position, dest, peMode, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
			{
				PatherFailed();
				return;
			}
			this.peMode = peMode;
			destination = dest;
			if (NextCellDoorToWaitForOrManuallyOpen() != null || nextCellCostLeft == nextCellCostTotal)
			{
				ResetToCurrentPosition();
			}
			PawnDestinationReservationManager.PawnDestinationReservation pawnDestinationReservation = vehicle.Map.pawnDestinationReservationManager.MostRecentReservationFor(vehicle);
			if (!(pawnDestinationReservation is null) && ((Destination.HasThing && pawnDestinationReservation.target != Destination.Cell)
				|| (pawnDestinationReservation.job != vehicle.CurJob && pawnDestinationReservation.target != Destination.Cell)))
			{
				vehicle.Map.pawnDestinationReservationManager.ObsoleteAllClaimedBy(vehicle);
			}
			if (AtDestinationPosition())
			{
				PatherArrived();
				return;
			}
			if (curPath != null)
			{
				curPath.ReleaseToPool();
			}
			curPath = null;
			moving = true;
			vehicle.EventRegistry[VehicleEventDefOf.MoveStart].ExecuteEvents();
		}

		public void StopDead()
		{
			if (!vehicle.Spawned)
			{
				return;
			}
			if (curPath != null)
			{
				vehicle.EventRegistry[VehicleEventDefOf.MoveStop].ExecuteEvents();
				curPath.ReleaseToPool();
			}
			curPath = null;
			moving = false;
			nextCell = vehicle.Position;
		}

		public void PatherTick()
		{
			if ((!vehicle.Drafted || !vehicle.CanMoveFinal) && curPath != null)
			{
				PatherFailed();
				return;
			}

			if (VehicleMod.settings.debug.debugDrawBumpers)
			{
				GenDraw.DrawFieldEdges(bumperCells);
			}

			//if (WillCollideWithPawnAt(vehicle.Position))
			//{
			//	if (!FailedToFindCloseUnoccupiedCellRecently())
			//	{
			//		if (CellFinder.TryFindBestPawnStandCell(vehicle, out IntVec3 intVec, true) && intVec != vehicle.Position)
			//		{
			//			vehicle.Position = intVec;
			//			ResetToCurrentPosition();

			//			if (moving && TrySetNewPath())
			//			{
			//				TryEnterNextPathCell();
			//				return;
			//			}
			//		}
			//		else
			//		{
			//			failedToFindCloseUnoccupiedCellTicks = Find.TickManager.TicksGame;
			//		}
			//	}
			//	return;
			//}
			lastMovedTick = Find.TickManager.TicksGame;
			if (nextCellCostLeft > 0f)
			{
				nextCellCostLeft -= CostToPayThisTick();
				return;
			}
			if (moving)
			{
				TryEnterNextPathCell();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void TryResumePathingAfterLoading()
		{
			if (moving)
			{
				StartPath(destination, peMode);
			}
		}

		public void Notify_Teleported()
		{
			StopDead();
			ResetToCurrentPosition();
		}

		public void ResetToCurrentPosition()
		{
			nextCell = vehicle.Position;
			nextCellCostLeft = 0f;
			nextCellCostTotal = 1f;
		}

		private bool PawnCanOccupy(IntVec3 c)
		{
			if (!vehicle.Drivable(c))
			{
				return false;
			}
			Building edifice = c.GetEdifice(vehicle.Map);
			if (edifice != null)
			{
				Building_Door building_Door = edifice as Building_Door;
				if (building_Door != null && !building_Door.PawnCanOpen(vehicle) && !building_Door.Open)
				{
					return false;
				}
			}
			return true;
		}

		public Building BuildingBlockingNextPathCell()
		{
			Building edifice = nextCell.GetEdifice(vehicle.Map);
			if (edifice != null && edifice.BlocksPawn(vehicle))
			{
				return edifice;
			}
			return null;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool IsNextCellWalkable()
		{
			return vehicle.Drivable(nextCell);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool AtDestinationPosition()
		{
			return VehicleReachabilityImmediate.CanReachImmediateVehicle(vehicle, destination, peMode);
		}

		public Building_Door NextCellDoorToWaitForOrManuallyOpen()
		{
			Building_Door building_Door = vehicle.Map.thingGrid.ThingAt<Building_Door>(nextCell);
			if (building_Door != null && building_Door.SlowsPawns && (!building_Door.Open || building_Door.TicksTillFullyOpened > 0) && building_Door.PawnCanOpen(vehicle))
			{
				return building_Door;
			}
			return null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PatherDraw()
		{
			if (DebugViewSettings.drawPaths && curPath != null && Find.Selector.IsSelected(vehicle))
			{
				curPath.DrawPath(vehicle);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool MovedRecently(int ticks)
		{
			return Find.TickManager.TicksGame - lastMovedTick <= ticks;
		}

		public bool TryRecoverFromUnwalkablePosition(bool error = true)
		{
			bool flag = false;
			int i = 0;
			while (i < GenRadial.RadialPattern.Length)
			{
				IntVec3 intVec = vehicle.Position + GenRadial.RadialPattern[i];
				if (PawnCanOccupy(intVec))
				{
					if (intVec == vehicle.Position)
					{
						return true;
					}
					if (error)
					{
						Log.Warning(string.Concat(new object[]
						{
							vehicle,
							" on unwalkable cell ",
							vehicle.Position,
							". Teleporting to ",
							intVec
						}));
					}
					vehicle.Position = intVec;
					vehicle.Notify_Teleported(true, false);
					flag = true;
					break;
				}
				else
				{
					i++;
				}
			}
			if (!flag)
			{
				vehicle.Destroy(DestroyMode.Vanish);
				Log.Error(string.Concat(new object[]
				{
					vehicle.ToStringSafe<Pawn>(),
					" on unwalkable cell ",
					vehicle.Position,
					". Could not find walkable position nearby. Destroyed."
				}));
			}
			return flag;
		}

		internal void PatherArrived()
		{
			StopDead();
			if (vehicle.jobs.curJob != null)
			{
				vehicle.jobs.curDriver.Notify_PatherArrived();
			}
		}

		internal void PatherFailed()
		{
			StopDead();
			vehicle.jobs?.curDriver?.Notify_PatherFailed();
		}

		private void SetBumperCells()
		{
			int dir = Ext_Map.DirectionToCell(vehicle.Position, nextCell);
			if (dir == -1)
			{
				dir = vehicle.Rotation.AsInt;
			}
			//TEMP
			if (dir == 4 || dir == 5)
			{
				dir = 1;
			}
			else if (dir == 6 || dir == 7)
			{
				dir = 3;
			}
			bumperCells = vehicle.OccupiedRectShifted(new IntVec2(0, 2), new Rot4(dir)).GetEdgeCells(new Rot4(dir)).ToList();
		}

		private void TryEnterNextPathCell()
		{
			//CompVehicleDamager vehicleDamager = vehicle.GetSortedComp<CompVehicleDamager>();
			//if (vehicleDamager != null)
			//{
			//	vehicleDamager.TakeStep();
			//}

			Building building = BuildingBlockingNextPathCell();
			if (building != null)
			{
				PatherFailed();
				return;
			}
			lastCell = vehicle.Position;
			vehicle.Position = nextCell;

			if (vehicle.beached)
			{
				vehicle.BeachShip();
				vehicle.Position = nextCell;
				StopDead();
				vehicle.jobs.curDriver.Notify_PatherFailed();
			}

			if (vehicle.RaceProps.Humanlike)
			{
				cellsUntilClamor--;
				if (cellsUntilClamor <= 0)
				{
					GenClamor.DoClamor(vehicle, 7f, ClamorDefOf.Movement);
					cellsUntilClamor = ClamorCellsInterval;
				}
			}
			//no filth for now
			if (vehicle.BodySize > 0.9f)
			{
				vehicle.Map.snowGrid.AddDepth(vehicle.Position, -SnowReductionFromWalking); //REDO
			}
			if (NeedNewPath() && (!TrySetNewPath() || curPath == null))
			{
				return;
			}
			if (AtDestinationPosition())
			{
				PatherArrived();
				return;
			}
			SetupMoveIntoNextCell();
			vehicle.Map.GetCachedMapComponent<VehiclePositionManager>().ClaimPosition(vehicle);
		}

		private void SetupMoveIntoNextCell()
		{
			if (curPath.NodesLeftCount <= 1)
			{
				Log.Error(string.Concat(new object[]
				{
					vehicle,
					" at ",
					vehicle.Position,
					" ran out of path nodes while pathing to ",
					destination,
					"."
				}));
				PatherFailed();
				return;
			}
			nextCell = curPath.ConsumeNextNode();
			if (!vehicle.DrivableFast(nextCell))
			{
				Log.Error(string.Concat(new object[]
				{
					vehicle,
					" entering ",
					nextCell,
					" which is unwalkable."
				}));
			}
			int num = CostToMoveIntoCell(vehicle, nextCell);
			nextCellCostTotal = num;
			nextCellCostLeft = num;
			Building_Door building_Door = vehicle.Map.thingGrid.ThingAt<Building_Door>(nextCell);
			if (building_Door != null)
			{
				building_Door.Notify_PawnApproaching(vehicle, num);
			}
			SetBumperCells();
			bool flag = bumperCells.Any(c => c.InBounds(vehicle.Map) && c.GetThingList(vehicle.Map).NotNullAndAny(t => t is VehiclePawn vehicle && vehicle != this.vehicle));

			if (vehicle.InsideMap(nextCell, vehicle.Map) || flag)
			{
				PatherFailed();
			}
		}

		public static int CostToMoveIntoCell(VehiclePawn vehicle, IntVec3 c)
		{
			int num;
			if (c.x == vehicle.Position.x || c.z == vehicle.Position.z)
			{
				num = vehicle.TicksPerMoveCardinal;
			}
			else
			{
				num = vehicle.TicksPerMoveDiagonal;
			}
			num += vehicle.Map.GetCachedMapComponent<VehicleMapping>()[vehicle.VehicleDef].VehiclePathGrid.CalculatedCostAt(c);
			Building edifice = c.GetEdifice(vehicle.Map);
			if (edifice != null)
			{
				num += edifice.PathWalkCostFor(vehicle);
			}
			if (num > MaxMoveTicks)
			{
				num = MaxMoveTicks;
			}
			if (vehicle.CurJob != null)
			{
				Pawn locomotionUrgencySameAs = vehicle.jobs.curDriver.locomotionUrgencySameAs;
				if (locomotionUrgencySameAs is VehiclePawn locomotionVehicle && locomotionUrgencySameAs != vehicle && locomotionUrgencySameAs.Spawned)
				{
					int num2 = CostToMoveIntoCell(locomotionVehicle, c);
					if (num < num2)
					{
						num = num2;
					}
				}
				else
				{
					switch (vehicle.jobs.curJob.locomotionUrgency)
					{
						case LocomotionUrgency.Amble:
							num *= 3;
							if (num < MinCostAmble)
							{
								num = MinCostAmble;
							}
							break;
						case LocomotionUrgency.Walk:
							num *= 2;
							if (num < MinCostWalk)
							{
								num = MinCostWalk;
							}
							break;
						case LocomotionUrgency.Jog:
							break;
						case LocomotionUrgency.Sprint:
							num = Mathf.RoundToInt(num * 0.75f);
							break;
					}
				}
			}
			return Mathf.Max(num, 1);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private float CostToPayThisTick()
		{
			return Mathf.Max(1, nextCellCostTotal / MaxMoveTicks);
		}

		private bool TrySetNewPath()
		{
			PawnPath pawnPath = GenerateNewPath_Concurrent();
			if (pawnPath is null || !pawnPath.Found)
			{
				PatherFailed();
				Messages.Message("NoPathForVehicle".Translate(), MessageTypeDefOf.RejectInput, false);
				return false;
			}
			if (curPath != null)
			{
				curPath.ReleaseToPool();
			}
			curPath = pawnPath;
			return true;
		}

		private bool TrySetNewPath_Threaded()
		{
			if (!Recalculating)
			{
				//Recalculating = true;
				//TrySetNewPath_Async();
				vehicle.Map.GetCachedMapComponent<VehicleMapping>().dedicatedThread.Queue(new AsyncAction(() => TrySetNewPath(), () => moving));
			}
			return Recalculating;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PawnPath GenerateNewPath_Concurrent()
		{
			return GenerateNewPath(CancellationToken.None);
		}

		public async void TrySetNewPath_Async()
		{
			PawnPath pawnPath = await GenerateNewPath_Async();
			curPath = pawnPath;
			Recalculating = false;
		}

		public async Task<PawnPath> GenerateNewPath_Async()
		{
			if (!tokenSources.NullOrEmpty())
			{
				foreach (CancellationTokenSource activeTokenSources in tokenSources)
				{
					activeTokenSources.Cancel();
				}
			}
			CancellationTokenSource tokenSource = new CancellationTokenSource();
			tokenSources.Add(tokenSource);
			try
			{
				PawnPath pawnPath = await TaskManager.RunAsync(GenerateNewPath, tokenSource.Token);

				if (pawnPath == null || !pawnPath.Found)
				{ 
					Debug.Message($"PawnPath not found. Ending and disposing remaining tasks...");
					pawnPath = PawnPath.NotFound;
				}
				return pawnPath;
			}
			catch (AggregateException ex)
			{
				SmashLog.WarningLabel(VehicleHarmony.LogLabel, $"Pathfinding thread encountered an error due to unsafe thread activity. The resulting task and token have been cancelled and disposed." +
					$"\nIf this occurrs often please report this behavior on the workshop page, it should be at worst an extreme edge case.");
				SmashLog.ErrorLabel(VehicleHarmony.LogLabel, "Logging Errors for Multithreaded pathing:");
				int exIndex = 1;
				foreach (Exception innerEx in ex.InnerExceptions)
				{
					Log.Error($"InnerException {exIndex}: {innerEx.Message} \nStackTrace: {innerEx.StackTrace} \nSource: {innerEx.Source}");
					exIndex++;
				}
			}
			finally
			{
				tokenSource.Dispose();
				tokenSources.Remove(tokenSource);
				tokenSource = null;
			}
			return PawnPath.NotFound;
		}

		private PawnPath GenerateNewPath(CancellationToken token)
		{
			lastPathedTargetPosition = destination.Cell;
			PawnPath pawnPath = vehicle.Map.GetCachedMapComponent<VehicleMapping>()[vehicle.VehicleDef].VehiclePathFinder.FindVehiclePath(vehicle.Position, destination, vehicle, token, peMode: peMode);
			return pawnPath;
		}
		
		private bool NeedNewPath()
		{
			if (!destination.IsValid || curPath is null || !curPath.Found || curPath.NodesLeftCount == 0)
			{
				return true;
			}
			if (destination.HasThing && destination.Thing.Map != vehicle.Map)
			{
				return true;
			}
			if ((vehicle.Position.InHorDistOf(curPath.LastNode, 15f) || vehicle.Position.InHorDistOf(destination.Cell, 15f)) && 
				!VehicleReachabilityImmediate.CanReachImmediateVehicle(curPath.LastNode, destination, vehicle.Map, vehicle.VehicleDef, peMode))
			{
				return true;
			}
			if (curPath.UsedRegionHeuristics && curPath.NodesConsumedCount >= 75)
			{
				return true;
			}
			if (lastPathedTargetPosition != destination.Cell)
			{
				float num = (vehicle.Position - destination.Cell).LengthHorizontalSquared;
				float num2;
				if (num > 900f)
				{
					num2 = 10f;
				}
				else if (num > 289f)
				{
					num2 = 5f;
				}
				else if (num > 100f)
				{
					num2 = 3f;
				}
				else if (num > 49f)
				{
					num2 = 2f;
				}
				else
				{
					num2 = 0.5f;
				}

				if ((lastPathedTargetPosition - destination.Cell).LengthHorizontalSquared > (num2 * num2))
				{
					return true;
				}
			}
			bool flag = curPath.NodesLeftCount < CheckForMovingCollidingPawnsIfCloserToTargetThanX;
			IntVec3 other = IntVec3.Invalid;
			IntVec3 intVec = IntVec3.Invalid;
			int num3 = 0;
			while (num3 < MaxCheckAheadNodes && num3 < curPath.NodesLeftCount)
			{
				intVec = curPath.Peek(num3);
				if (!GenGridVehicles.Walkable(intVec, vehicle.VehicleDef, vehicle.Map))
				{
					return true;
				}
				if (num3 != 0 && intVec.AdjacentToDiagonal(other) && (VehiclePathFinder.BlocksDiagonalMovement(vehicle, vehicle.Map.cellIndices.CellToIndex(intVec.x, other.z))
					|| VehiclePathFinder.BlocksDiagonalMovement(vehicle, vehicle.Map.cellIndices.CellToIndex(other.x, intVec.z))))
				{
					return true;
				}
				other = intVec;
				num3++;
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool BestPathHadPawnsInTheWayRecently()
		{
			return foundPathWhichCollidesWithPawns + 240 > Find.TickManager.TicksGame;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool BestPathHadDangerRecently()
		{
			return foundPathWithDanger + 240 > Find.TickManager.TicksGame;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool FailedToFindCloseUnoccupiedCellRecently()
		{
			return failedToFindCloseUnoccupiedCellTicks + 100 > Find.TickManager.TicksGame;
		}

		[DebugAction(VehicleHarmony.VehiclesLabel, "Test Pathfinder", allowedGameStates = AllowedGameStates.IsCurrentlyOnMap)]
		private static void TestPathfinder()
		{
			List<DebugMenuOption> options = new List<DebugMenuOption>();
			foreach (VehicleDef vehicleDef in VehicleHarmony.AllMoveableVehicleDefs)
			{
				options.Add(new DebugMenuOption(vehicleDef.defName, DebugMenuOptionMode.Action, delegate()
				{
					CoroutineManager.QueueInvoke(() => PathfinderRoutine(vehicleDef));
				}));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
		}
		
		private static IEnumerator PathfinderRoutine(VehicleDef vehicleDef)
		{
			IntVec3 start = IntVec3.Invalid;
			IntVec3 dest = IntVec3.Invalid;

			DebugTools.curTool = new DebugTool("Select Start", delegate ()
			{
				start = UI.MouseCell();
				if (start.IsValid)
				{
					DebugTools.curTool = new DebugTool("Select Destination", () => dest = UI.MouseCell(), onGUIAction: delegate ()
					{
						Find.CurrentMap.debugDrawer.FlashCell(start, colorPct: 0.5f, duration: 5);
						GenDraw.DrawLineBetween(start.ToVector3ShiftedWithAltitude(AltitudeLayer.Skyfaller), UI.MouseMapPosition(), SimpleColor.Green);
					});
				}
			});

			while (!start.IsValid && !dest.IsValid)
			{
				SmashLog.QuickMessage($"PathFinding: {start} to {dest}");
				yield return null;
			}


			DebugTools.curTool = null;
		}
	}
}
