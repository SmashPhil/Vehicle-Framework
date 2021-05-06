using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles.AI
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

		protected VehiclePawn pawn;

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

		public Vehicle_PathFollower(VehiclePawn newPawn)
		{
			pawn = newPawn;
			bumperCells = new List<IntVec3>();
		}

		public LocalTargetInfo Destination
		{
			get
			{
				return destination;
			}
		}

		public bool Moving
		{
			get
			{
				return moving;
			}
		}

		public bool MovingNow
		{
			get
			{
				return Moving && !WillCollideWithPawnOnNextPathCell();
			}
		}

		public IntVec3 LastPassableCellInPath
		{
			get
			{
				if (!Moving || curPath == null)
				{
					return IntVec3.Invalid;
				}
				if (!Destination.Cell.Impassable(pawn.Map))
				{
					return Destination.Cell;
				}
				List<IntVec3> nodesReversed = curPath.NodesReversed;
				for (int i = 0; i < nodesReversed.Count; i++)
				{
					if (!nodesReversed[i].Impassable(pawn.Map))
					{
						return nodesReversed[i];
					}
				}
				if (!pawn.Position.Impassable(pawn.Map))
				{
					return pawn.Position;
				}
				return IntVec3.Invalid;
			}
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref moving, "moving", true, false);
			Scribe_Values.Look(ref nextCell, "nextCell", default(IntVec3), false);
			Scribe_Values.Look(ref nextCellCostLeft, "nextCellCostLeft", 0f, false);
			Scribe_Values.Look(ref nextCellCostTotal, "nextCellCostInitial", 0f, false);
			Scribe_Values.Look(ref peMode, "peMode", PathEndMode.None, false);
			Scribe_Values.Look(ref cellsUntilClamor, "cellsUntilClamor", 0, false);
			Scribe_Values.Look(ref lastMovedTick, "lastMovedTick", -999999, false);
			if (moving)
			{
				Scribe_TargetInfo.Look(ref destination, "destination");
			}
		}

		public void StartPath(LocalTargetInfo dest, PathEndMode peMode)
		{
			if (!pawn.Drafted)
			{
				PatherFailed();
				return;
			}

			if(pawn.IsBoat())
			{
				dest = (LocalTargetInfo)GenPathShip.ResolvePathMode(pawn, dest.ToTargetInfo(pawn.Map), ref peMode);
				if (dest.HasThing && dest.ThingDestroyed)
				{
					Log.Error(pawn + " pathing to destroyed thing " + dest.Thing);
					PatherFailed();
					return;
				}
				//Add Building and Position Recoverable extras
				if (!GenGridShips.Walkable(pawn.Position, pawn.Map.GetCachedMapComponent<WaterMap>()))
				{
					return;
				}
				if (Moving && curPath != null && destination == dest && this.peMode == peMode)
				{
					return;
				}
				if (!pawn.Map.GetCachedMapComponent<WaterMap>().ShipReachability?.CanReachShip(pawn.Position, dest, peMode, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)) ?? false)
				{
					PatherFailed();
					return;
				}
				this.peMode = peMode;
				destination = dest;
				if ((GenGridShips.Walkable(nextCell, pawn.Map.GetCachedMapComponent<WaterMap>()) || WillCollideWithPawnOnNextPathCell()) || nextCellCostLeft == nextCellCostTotal)
				{
					ResetToCurrentPosition();
				}
				PawnDestinationReservationManager.PawnDestinationReservation pawnDestinationReservation = pawn.Map.pawnDestinationReservationManager.
					MostRecentReservationFor(pawn);
				if (!(pawnDestinationReservation is null) && ((Destination.HasThing && pawnDestinationReservation.target != Destination.Cell)
					|| (pawnDestinationReservation.job != pawn.CurJob && pawnDestinationReservation.target != Destination.Cell)))
				{
					pawn.Map.pawnDestinationReservationManager.ObsoleteAllClaimedBy(pawn);
				}
				if (ShipReachabilityImmediate.CanReachImmediateShip(pawn, dest, peMode))
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
				pawn.jobs.posture = PawnPosture.Standing;

				return;
			}
			else
			{
				dest = (LocalTargetInfo)GenPath.ResolvePathMode(pawn, dest.ToTargetInfo(pawn.Map), ref peMode);
				if (dest.HasThing && dest.ThingDestroyed)
				{
					Log.Error(pawn + " pathing to destroyed thing " + dest.Thing);
					PatherFailed();
					return;
				}
				if (!PawnCanOccupy(pawn.Position) && !TryRecoverFromUnwalkablePosition(true))
				{
					return;
				}
				if (moving && curPath != null && destination == dest && this.peMode == peMode)
				{
					return;
				}
				if (!pawn.Map.reachability.CanReach(pawn.Position, dest, peMode, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
				{
					PatherFailed();
					return;
				}
				this.peMode = peMode;
				destination = dest;
				if (!IsNextCellWalkable() || NextCellDoorToWaitForOrManuallyOpen() != null || nextCellCostLeft == nextCellCostTotal)
				{
					ResetToCurrentPosition();
				}
				PawnDestinationReservationManager.PawnDestinationReservation pawnDestinationReservation = pawn.Map.pawnDestinationReservationManager.MostRecentReservationFor(pawn);
				if (pawnDestinationReservation != null && ((destination.HasThing && pawnDestinationReservation.target != destination.Cell) || (pawnDestinationReservation.job != pawn.CurJob && pawnDestinationReservation.target != destination.Cell)))
				{
					pawn.Map.pawnDestinationReservationManager.ObsoleteAllClaimedBy(pawn);
				}
				if (AtDestinationPosition())
				{
					PatherArrived();
					return;
				}
				if (pawn.Downed)
				{
					Log.Error(pawn.LabelCap + " tried to path while downed. This should never happen. curJob=" + pawn.CurJob.ToStringSafe());
					PatherFailed();
					return;
				}
				if (curPath != null)
				{
					curPath.ReleaseToPool();
				}
				curPath = null;
				moving = true;
				pawn.jobs.posture = PawnPosture.Standing;
			}
		}

		public void StopDead()
		{
			if (curPath != null)
			{
				curPath.ReleaseToPool();
			}
			curPath = null;
			moving = false;
			nextCell = pawn.Position;
		}

		public void PatherTick()
		{
			if(!pawn.Drafted)
			{
				return;
			}

			if (VehicleMod.settings.debug.debugDrawBumpers)
			{
				GenDraw.DrawFieldEdges(bumperCells);
			}

			if (WillCollideWithPawnAt(pawn.Position))
			{
				if (!FailedToFindCloseUnoccupiedCellRecently())
				{
					if (CellFinder.TryFindBestPawnStandCell(pawn, out IntVec3 intVec, true) && intVec != pawn.Position)
					{
						pawn.Position = intVec;
						ResetToCurrentPosition();
						
						if (moving && TrySetNewPath())
						{
							TryEnterNextPathCell();
							return;
						}
					}
					else
					{
						failedToFindCloseUnoccupiedCellTicks = Find.TickManager.TicksGame;
					}
				}
				return;
			}
			if (pawn.stances.FullBodyBusy)
			{
				return;
			}
			if (moving && WillCollideWithPawnOnNextPathCell())
			{
				nextCellCostLeft = nextCellCostTotal;
				if (((curPath != null && curPath.NodesLeftCount < CheckForMovingCollidingPawnsIfCloserToTargetThanX) || PawnUtility.AnyPawnBlockingPathAt(nextCell, pawn, false, true, false)) && !BestPathHadPawnsInTheWayRecently() && TrySetNewPath())
				{
					ResetToCurrentPosition();
					TryEnterNextPathCell();
					return;
				}
				if (Find.TickManager.TicksGame - lastMovedTick >= AttackBlockingHostilePawnAfterTicks)
				{
					Pawn pawn = PawnUtility.PawnBlockingPathAt(nextCell, this.pawn, false, false, false);
					if (pawn != null && this.pawn.HostileTo(pawn) && this.pawn.TryGetAttackVerb(pawn, false) != null)
					{
						Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, pawn);
						job.maxNumMeleeAttacks = 1;
						job.expiryInterval = 300;
						this.pawn.jobs.StartJob(job, JobCondition.Incompletable, null, false, true, null, null, false, false);
						return;
					}
				}
				return;
			}
			else
			{
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
				return;
			}
		}

		public void TryResumePathingAfterLoading()
		{
			if (moving)
			{
				StartPath(destination, peMode);
			}
		}

		public void Notify_Teleported_Int()
		{
			StopDead();
			ResetToCurrentPosition();
		}

		public void ResetToCurrentPosition()
		{
			nextCell = pawn.Position;
			nextCellCostLeft = 0f;
			nextCellCostTotal = 1f;
		}

		private bool PawnCanOccupy(IntVec3 c)
		{
			if (!pawn.Drivable(c))
			{
				return false;
			}
			Building edifice = c.GetEdifice(pawn.Map);
			if (edifice != null)
			{
				Building_Door building_Door = edifice as Building_Door;
				if (building_Door != null && !building_Door.PawnCanOpen(pawn) && !building_Door.Open)
				{
					return false;
				}
			}
			return true;
		}

		public Building BuildingBlockingNextPathCell()
		{
			Building edifice = nextCell.GetEdifice(pawn.Map);
			if (edifice != null && edifice.BlocksPawn(pawn))
			{
				return edifice;
			}
			return null;
		}

		public bool WillCollideWithPawnOnNextPathCell()
		{
			return WillCollideWithPawnAt(nextCell);
		}

		private bool IsNextCellWalkable()
		{
			return pawn.Drivable(nextCell) && !WillCollideWithPawnAt(nextCell);
		}

		private bool WillCollideWithPawnAt(IntVec3 c)
		{
			return PawnUtility.ShouldCollideWithPawns(pawn) && PawnUtility.AnyPawnBlockingPathAt(c, pawn, false, false, false);
		}


		public Building_Door NextCellDoorToWaitForOrManuallyOpen()
		{
			Building_Door building_Door = pawn.Map.thingGrid.ThingAt<Building_Door>(nextCell);
			if (building_Door != null && building_Door.SlowsPawns && (!building_Door.Open || building_Door.TicksTillFullyOpened > 0) && building_Door.PawnCanOpen(pawn))
			{
				return building_Door;
			}
			return null;
		}


		public void PatherDraw()
		{
			if (DebugViewSettings.drawPaths && curPath != null && Find.Selector.IsSelected(pawn))
			{
				curPath.DrawPath(pawn);
			}
		}

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
				IntVec3 intVec = pawn.Position + GenRadial.RadialPattern[i];
				if (PawnCanOccupy(intVec))
				{
					if (intVec == pawn.Position)
					{
						return true;
					}
					if (error)
					{
						Log.Warning(string.Concat(new object[]
						{
							pawn,
							" on unwalkable cell ",
							pawn.Position,
							". Teleporting to ",
							intVec
						}));
					}
					pawn.Position = intVec;
					pawn.Notify_Teleported(true, false);
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
				pawn.Destroy(DestroyMode.Vanish);
				Log.Error(string.Concat(new object[]
				{
					pawn.ToStringSafe<Pawn>(),
					" on unwalkable cell ",
					pawn.Position,
					". Could not find walkable position nearby. Destroyed."
				}));
			}
			return flag;
		}

		internal void PatherArrived()
		{
			StopDead();
			if (pawn.jobs.curJob != null)
			{
				pawn.jobs.curDriver.Notify_PatherArrived();
			}
		}

		internal void PatherFailed()
		{
			StopDead();
			pawn.jobs?.curDriver?.Notify_PatherFailed();
		}

		private void SetBumperCells()
		{
			int dir = Ext_Map.DirectionToCell(pawn.Position, nextCell);
			if (dir == -1)
			{
				dir = pawn.Rotation.AsInt;
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
			bumperCells = pawn.OccupiedRectShifted(new IntVec2(0, 2), new Rot4(dir)).GetEdgeCells(new Rot4(dir)).ToList();
		}

		private void TryEnterNextPathCell()
		{
			if (VehicleMod.settings.debug.debugDisableWaterPathing)
			{
				if (pawn.IsBoat() && pawn.beached)
				{
					pawn.RemoveBeachedStatus();
				}
				return;
			}

			var vehicleTrack = pawn.GetSortedComp<CompVehicleTracks>();
			if (vehicleTrack != null)
			{
				vehicleTrack.TakeStep();
			}

			if (pawn.IsBoat())
			{
				if(!pawn.Drafted)
				{
					if(pawn.CurJob is null)
					{
						JobUtility.TryStartErrorRecoverJob(pawn, string.Empty);
					}
					StopDead();
				}

				if (pawn.beached || !nextCell.GetTerrain(pawn.Map).IsWater)
				{
					pawn.BeachShip();
					pawn.Position = nextCell;
					StopDead();
					pawn.jobs.curDriver.Notify_PatherFailed();
				}

				lastCell = pawn.Position;
				pawn.Position = nextCell;
				if(NeedNewPath() && !TrySetNewPath())
				{
					return;
				}
				if(ShipReachabilityImmediate.CanReachImmediateShip(pawn, destination, peMode))
				{
					PatherArrived();
				}
				else
				{
					SetupMoveIntoNextCell();
				}
				return;
			}
			else
			{
				Building building = BuildingBlockingNextPathCell();
				if (building != null)
				{
					PatherFailed();
					return;
				}
				lastCell = pawn.Position;
				pawn.Position = nextCell;
				if (pawn.RaceProps.Humanlike)
				{
					cellsUntilClamor--;
					if (cellsUntilClamor <= 0)
					{
						GenClamor.DoClamor(pawn, 7f, ClamorDefOf.Movement);
						cellsUntilClamor = ClamorCellsInterval;
					}
				}
				//no filth for now
				if (pawn.BodySize > 0.9f)
				{
					pawn.Map.snowGrid.AddDepth(pawn.Position, -SnowReductionFromWalking); //REDO
				}
				if (NeedNewPath() && !TrySetNewPath())
				{
					return;
				}
				if (AtDestinationPosition())
				{
					PatherArrived();
					return;
				}
				SetupMoveIntoNextCell();
			}
			pawn.Map.GetCachedMapComponent<VehiclePositionManager>().ClaimPosition(pawn);
		}

		private void SetupMoveIntoNextCell()
		{
			if (curPath.NodesLeftCount <= 1)
			{
				Log.Error(string.Concat(new object[]
				{
					pawn,
					" at ",
					pawn.Position,
					" ran out of path nodes while pathing to ",
					destination,
					"."
				}));
				PatherFailed();
				return;
			}
			nextCell = curPath.ConsumeNextNode();
			if (!pawn.DrivableFast(nextCell))
			{
				Log.Error(string.Concat(new object[]
				{
					pawn,
					" entering ",
					nextCell,
					" which is unwalkable."
				}));
			}
			int num = CostToMoveIntoCell(pawn, nextCell);
			nextCellCostTotal = num;
			nextCellCostLeft = num;
			Building_Door building_Door = pawn.Map.thingGrid.ThingAt<Building_Door>(nextCell);
			if (building_Door != null)
			{
				building_Door.Notify_PawnApproaching(pawn, num);
			}
			SetBumperCells();
			bool flag = bumperCells.Any(c => c.InBounds(pawn.Map) && c.GetThingList(pawn.Map).NotNullAndAny(t => t is VehiclePawn vehicle && vehicle != pawn));

			if (pawn.ClampHitboxToMap(nextCell, pawn.Map) || flag)
			{
				pawn.jobs.curDriver.Notify_PatherFailed();
				StopDead();
				return;
			}
		}

		public static int CostToMoveIntoCell(Pawn pawn, IntVec3 c)
		{
			int num;
			if (c.x == pawn.Position.x || c.z == pawn.Position.z)
			{
				num = pawn.TicksPerMoveCardinal;
			}
			else
			{
				num = pawn.TicksPerMoveDiagonal;
			}
			num += pawn.IsBoat() ? pawn.Map.GetCachedMapComponent<WaterMap>().ShipPathGrid.CalculatedCostAt(c) : pawn.Map.pathGrid.CalculatedCostAt(c, false, pawn.Position);
			Building edifice = c.GetEdifice(pawn.Map);
			if (edifice != null)
			{
				num += edifice.PathWalkCostFor(pawn);
			}
			if (num > MaxMoveTicks)
			{
				num = MaxMoveTicks;
			}
			if (pawn.CurJob != null)
			{
				Pawn locomotionUrgencySameAs = pawn.jobs.curDriver.locomotionUrgencySameAs;
				if (locomotionUrgencySameAs != null && locomotionUrgencySameAs != pawn && locomotionUrgencySameAs.Spawned)
				{
					int num2 = CostToMoveIntoCell(locomotionUrgencySameAs, c);
					if (num < num2)
					{
						num = num2;
					}
				}
				else
				{
					switch (pawn.jobs.curJob.locomotionUrgency)
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

		private float CostToPayThisTick()
		{
			float num = 1f;
			if (num < nextCellCostTotal / MaxMoveTicks)
			{
				num = nextCellCostTotal / MaxMoveTicks;
			}
			return num;
		}

		private bool TrySetNewPath()
		{
			PawnPath pawnPath = GenerateNewPathThreaded(); // GenerateNewPath();
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
			int num = 0;
			while (num < MaxCheckAheadNodes && num < curPath.NodesLeftCount)
			{
				IntVec3 c = curPath.Peek(num);

				if (pawn.beached) break;
				if (PawnUtility.ShouldCollideWithPawns(pawn) && PawnUtility.AnyPawnBlockingPathAt(c, pawn, false, false, false))
				{
					foundPathWhichCollidesWithPawns = Find.TickManager.TicksGame;
				}
				if (PawnUtility.KnownDangerAt(c, pawn.Map, pawn))
				{
					foundPathWithDanger = Find.TickManager.TicksGame;
				}
				if (foundPathWhichCollidesWithPawns == Find.TickManager.TicksGame && foundPathWithDanger == Find.TickManager.TicksGame)
				{
					break;
				}
				num++;
			}
			return true;
		}

		private PawnPath GenerateNewPathThreaded()
		{
			var cts = new CancellationTokenSource();
			try
			{
				var tasks = new[]
				{
					Task<Tuple<PawnPath, bool>>.Factory.StartNew( () => GenerateReversePath(cts.Token), cts.Token),
					Task<Tuple<PawnPath, bool>>.Factory.StartNew( () => GenerateNewPath(cts.Token), cts.Token)
				};
				int taskIndex = Task.WaitAny(tasks, cts.Token);

				if (tasks[taskIndex].Result?.Item1 != null && !tasks[taskIndex].Result.Item1.Found && !tasks[taskIndex].Result.Item2)
				{ 
					try
					{
						cts.Cancel();
						cts.Dispose();
						Debug.Message($"Ending and disposing remaining tasks...");
						return PawnPath.NotFound;
					
					}
					catch(Exception ex)
					{
						Log.Error($"{VehicleHarmony.LogLabel} Unable to cancel and dispose remaining tasks. \nException: {ex.Message} \nStack: {ex.StackTrace}");
					}
				}
				return tasks[1].Result?.Item1;
			}
			catch(AggregateException ex)
			{
				Log.Warning($"{VehicleHarmony.LogLabel} Pathfinding thread encountered an error due to unsafe thread activity. The resulting task and token have been cancelled and disposed." +
					$"\nIf this occurrs often please report this behavior on the workshop page, it should be at worst an extreme edge case.");
				Log.Error($"{VehicleHarmony.LogLabel} Logging Errors for Multithreaded pathing:");
				int exIndex = 1;
				foreach(Exception innerEx in ex.InnerExceptions)
				{
					Log.Error($"InnerException {exIndex}: {innerEx.Message} \nStackTrace: {innerEx.StackTrace} \nSource: {innerEx.Source}");
					exIndex++;
				}
				cts.Cancel();
				cts.Dispose();
				return PawnPath.NotFound;
			}
		}

		internal Tuple<PawnPath, bool> GenerateNewPath(CancellationToken token)
		{
			lastPathedTargetPosition = destination.Cell;
			var pathResult = pawn.Map.GetCachedMapComponent<WaterMap>().ShipPathFinder.FindVehiclePath(pawn.Position, destination, pawn, token, peMode);
			if ( (!pathResult.path.Found && !pathResult.found) && Prefs.DevMode && VehicleMod.settings.debug.debugDrawVehiclePathCosts) 
				Log.Warning("Path Not Found");
			return new Tuple<PawnPath, bool>(pathResult.path, pathResult.found);
		}

		internal Tuple<PawnPath, bool> GenerateReversePath(CancellationToken token)
		{
			lastPathedTargetPosition = destination.Cell;
			var pathResult = pawn.Map.GetCachedMapComponent<WaterMap>().ThreadedPathFinderConstrained.FindVehiclePath(destination.Cell, new LocalTargetInfo(pawn.Position), pawn, token, peMode);
			return new Tuple<PawnPath, bool>(PawnPath.NotFound, pathResult.found);
		}

		private bool AtDestinationPosition()
		{
			return pawn.CanReachImmediate(destination, peMode);
		}

		private bool NeedNewPath()
		{
			if(pawn.IsBoat())
			{
				if (!destination.IsValid || curPath is null || !curPath.Found || curPath.NodesLeftCount == 0)
				{
					return true;
				}
				if (destination.HasThing && destination.Thing.Map != pawn.Map)
				{
					return true;
				}
				if ((pawn.Position.InHorDistOf(curPath.LastNode, 15f) || pawn.Position.InHorDistOf(destination.Cell, 15f)) && !ShipReachabilityImmediate.CanReachImmediateShip(
					curPath.LastNode, destination, pawn.Map, peMode, pawn))
				{
					return true;
				}
				if (curPath.UsedRegionHeuristics && curPath.NodesConsumedCount >= 75)
				{
					return true;
				}
				if (lastPathedTargetPosition != destination.Cell)
				{
					float num = (pawn.Position - destination.Cell).LengthHorizontalSquared;
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
					if (!GenGridShips.Walkable(intVec, pawn.Map.GetCachedMapComponent<WaterMap>()))
					{
						return true;
					}
					if (num3 != 0 && intVec.AdjacentToDiagonal(other) && (VehiclePathFinder.BlocksDiagonalMovement(pawn, pawn.Map.cellIndices.CellToIndex(intVec.x, other.z))
						|| VehiclePathFinder.BlocksDiagonalMovement(pawn, pawn.Map.cellIndices.CellToIndex(other.x, intVec.z))))
					{
						return true;
					}
					other = intVec;
					num3++;
				}
				return false;
			}
			else
			{
				if (!destination.IsValid || curPath is null || !curPath.Found || curPath.NodesLeftCount == 0)
				{
					return true;
				}
				if (destination.HasThing && destination.Thing.Map != pawn.Map)
				{
					return true;
				}
				if ((pawn.Position.InHorDistOf(curPath.LastNode, 15f) || pawn.Position.InHorDistOf(destination.Cell, 15f)) && !ReachabilityImmediate.CanReachImmediate(curPath.LastNode, destination, pawn.Map, peMode, pawn))
				{
					return true;
				}
				if (curPath.UsedRegionHeuristics && curPath.NodesConsumedCount >= 75)
				{
					return true;
				}
				if (lastPathedTargetPosition != destination.Cell)
				{
					float num = (pawn.Position - destination.Cell).LengthHorizontalSquared;
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
					if ((lastPathedTargetPosition - destination.Cell).LengthHorizontalSquared > num2 * num2)
					{
						return true;
					}
				}
				bool flag = PawnUtility.ShouldCollideWithPawns(pawn);
				bool flag2 = curPath.NodesLeftCount < CheckForMovingCollidingPawnsIfCloserToTargetThanX;
				IntVec3 intVec = IntVec3.Invalid;
				int num3 = 0;
				while (num3 < MaxCheckAheadNodes && num3 < curPath.NodesLeftCount)
				{
					IntVec3 intVec2 = curPath.Peek(num3);
					if (!pawn.Drivable(intVec2))
					{
						return true;
					}
					if (flag && !BestPathHadPawnsInTheWayRecently() && (PawnUtility.AnyPawnBlockingPathAt(intVec2, pawn, false, true, false) || (flag2 && PawnUtility.AnyPawnBlockingPathAt(intVec2, pawn, false, false, false))))
					{
						return true;
					}
					if (!BestPathHadDangerRecently() && PawnUtility.KnownDangerAt(intVec2, pawn.Map, pawn))
					{
						return true;
					}
					Building_Door building_Door = intVec2.GetEdifice(pawn.Map) as Building_Door;
					if (building_Door != null)
					{
						if (!building_Door.CanPhysicallyPass(pawn) && !pawn.HostileTo(building_Door))
						{
							return true;
						}
						if (building_Door.IsForbiddenToPass(pawn))
						{
							return true;
						}
					}
					if (num3 != 0 && intVec2.AdjacentToDiagonal(intVec) && (VehiclePathFinder.BlocksDiagonalMovement(pawn, intVec2.x, intVec.z) || VehiclePathFinder.BlocksDiagonalMovement(pawn, intVec.x, intVec2.z)))
					{
						return true;
					}
					intVec = intVec2;
					num3++;
				}
			}
			
			return false;
		}


		private bool BestPathHadPawnsInTheWayRecently()
		{
			return foundPathWhichCollidesWithPawns + 240 > Find.TickManager.TicksGame;
		}

		private bool BestPathHadDangerRecently()
		{
			return foundPathWithDanger + 240 > Find.TickManager.TicksGame;
		}

		private bool FailedToFindCloseUnoccupiedCellRecently()
		{
			return failedToFindCloseUnoccupiedCellTicks + 100 > Find.TickManager.TicksGame;
		}
	}
}
