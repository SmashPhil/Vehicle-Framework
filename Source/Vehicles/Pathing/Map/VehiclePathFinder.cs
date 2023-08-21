using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Kept similar to vanilla pathfinding for consistency
	/// </summary>
	public class VehiclePathFinder
	{
		public const int Cost_OutsideAllowedArea = 600;
		private const int Cost_VehicleCollision = 1000;
		private const int NodesToOpenBeforeRegionBasedPathing = 100000;
		public const int DefaultMoveTicksCardinal = 13;
		public const int DefaultMoveTicksDiagonal = 18;
		private const int SearchLimit = 160000;
		private const int TurnCostTicks = 2;
		private const float SecondsBetweenDraws = 0;

		public const float RootPosWeight = 0.75f;

		internal Dictionary<IntVec3, int> postCalculatedCells = new Dictionary<IntVec3, int>();

		private VehicleMapping mapping;
		private VehicleDef vehicleDef;
		private FastPriorityQueue<CostNode> openList;
		private VehiclePathFinderNodeFast[] calcGrid;

		private ushort statusOpenValue = 1;
		private ushort statusClosedValue = 2;

		private int mapSizeX;
		private int mapSizeZ;

		private VehiclePathGrid vehiclePathGrid;
		private VehicleRegionCostCalculatorWrapper regionCostCalculator;

		private Building[] edificeGrid; //REDO - allow vehicles to have custom edifice costs?
		private List<Blueprint>[] blueprintGrid;

		private CellIndices cellIndices;
		private List<int> disallowedCornerIndices = new List<int>(4);

		/// <summary>
		/// 8 directional x,y adjacent offsets
		/// </summary>
		private static readonly int[] directions = new int[]
		{
			//x coord
			0, //North
			1, //East
			0, //South
			-1, //West
			1, //NorthEast
			1, //SouthEast
			-1, //SouthWest
			-1, //NorthWest
			//y coord
			-1, //North
			0, //East
			1, //South
			0, //West
			-1, //NorthEast
			1, //SouthEast
			1, //SouthWest
			-1 //NorthWest
		};

		private static readonly SimpleCurve NonRegionBasedHeuristicStrength_DistanceCurve = new SimpleCurve
		{
			{
				new CurvePoint(40f, 1f),
				true
			},
			{
				new CurvePoint(120f, 2.8f),
				true
			}
		};

		private static readonly SimpleCurve RegionHeuristicWeightByNodesOpened = new SimpleCurve
		{
			{
				new CurvePoint(0f, 1f),
				true
			},
			{
				new CurvePoint(3500f, 1f),
				true
			},
			{
				new CurvePoint(4500f, 5f),
				true
			},
			{
				new CurvePoint(30000f, 50f),
				true
			},
			{
				new CurvePoint(100000f, 500f),
				true
			}
		};

		public VehiclePathFinder(VehicleMapping mapping, VehicleDef vehicleDef)
		{
			this.mapping = mapping;
			this.vehicleDef = vehicleDef;
			mapSizeX = mapping.map.Size.x;
			mapSizeZ = mapping.map.Size.z;
			calcGrid = new VehiclePathFinderNodeFast[mapSizeX * mapSizeZ];
			openList = new FastPriorityQueue<CostNode>(new CostNodeComparer());
			regionCostCalculator = new VehicleRegionCostCalculatorWrapper(mapping, vehicleDef);
			postCalculatedCells = new Dictionary<IntVec3, int>();
		}

		/// <summary>
		/// Find path from <paramref name="start"/> to <paramref name="start"/>
		/// </summary>
		/// <param name="start"></param>
		/// <param name="dest"></param>
		/// <param name="vehicle"></param>
		/// <param name="token"></param>
		/// <param name="peMode"></param>
		public PawnPath FindVehiclePath(IntVec3 start, LocalTargetInfo dest, VehiclePawn vehicle, CancellationToken token, PathEndMode peMode = PathEndMode.OnCell)
		{
			if (!vehicle.FitsOnCell(dest.Cell))
			{
				Messages.Message("VF_CannotFit".Translate(), MessageTypeDefOf.RejectInput);
				return PawnPath.NotFound;
			}
			Danger maxDanger = Danger.Deadly;
			return FindVehiclePath(start, dest, TraverseParms.For(vehicle, maxDanger, TraverseMode.ByPawn, false), token, peMode);
		}

		/// <summary>
		/// Find path from <paramref name="start"/> to <paramref name="start"/> internal algorithm call
		/// </summary>
		/// <param name="start"></param>
		/// <param name="dest"></param>
		/// <param name="traverseParms"></param>
		/// <param name="token"></param>
		/// <param name="peMode"></param>
		public PawnPath FindVehiclePath(IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, CancellationToken token, PathEndMode peMode = PathEndMode.OnCell)
		{
			postCalculatedCells.Clear();
			if (DebugSettings.pathThroughWalls)
			{
				traverseParms.mode = TraverseMode.PassAllDestroyableThings;
			}
			VehiclePawn vehicle = traverseParms.pawn as VehiclePawn;
			if (vehicle is null)
			{
				Log.Error($"Tried to FindVehiclePath for non-vehicle pawn {traverseParms.pawn}");
				return PawnPath.NotFound;
			}
			else if (vehicle.Map != mapping.map)
			{
				Log.Error($"Tried to FindVehiclePath for vehicle which is spawned in another map. Their map PathFinder should  have been used, not this one. vehicle={vehicle} vehicle's map={vehicle.Map} map={mapping.map}");
				return PawnPath.NotFound;
			}
			if (!start.IsValid)
			{
				Log.Error($"Tried to FindVehiclePath with invalid start {start}. vehicle={vehicle}");
				return PawnPath.NotFound;
			}
			if (!dest.IsValid)
			{
				Log.Error($"Tried to FindVehiclePath with invalid destination {dest}. vehicle={vehicle}");
				return PawnPath.NotFound;
			}
			//Will almost always be ByPawn
			if (traverseParms.mode == TraverseMode.ByPawn && !vehicle.CanReachVehicle(dest, peMode, Danger.Deadly, traverseParms.mode))
			{
				Log.Error($"Trying to path to region not reachable, this should be blocked by reachability checks.");
				return PawnPath.NotFound;
			}
			cellIndices = mapping.map.cellIndices;
			
			vehiclePathGrid = mapping[vehicleDef].VehiclePathGrid;
			this.edificeGrid = mapping.map.edificeGrid.InnerArray;
			blueprintGrid = mapping.map.blueprintGrid.InnerArray;
			int x = dest.Cell.x;
			int z = dest.Cell.z;
			int startIndex = cellIndices.CellToIndex(start);
			int destIndex = cellIndices.CellToIndex(dest.Cell);
			ByteGrid byteGrid = vehicle.GetAvoidGrid(true);
			bool passAllDestroyableThings = traverseParms.mode == TraverseMode.PassAllDestroyableThings || traverseParms.mode == TraverseMode.PassAllDestroyableThingsNotWater;
			bool freeTraversal = traverseParms.mode != TraverseMode.NoPassClosedDoorsOrWater && traverseParms.mode != TraverseMode.PassAllDestroyableThingsNotWater;
			CellRect cellRect = CalculateDestinationRect(dest, peMode);
			bool singleRect = cellRect.Width == 1 && cellRect.Height == 1;
			int[] vehicleArray = vehiclePathGrid.pathGrid;
			TerrainDef[] topGrid = mapping.map.terrainGrid.topGrid;
			EdificeGrid edificeGrid = mapping.map.edificeGrid;
			int searchCount = 0;
			int nodesOpened = 0;
			bool drawPaths = VehicleMod.settings.debug.debugDrawPathfinderSearch;
			bool allowedRegionTraversal = !passAllDestroyableThings && VehicleGridsUtility.GetRegion(start, mapping.map, vehicleDef, RegionType.Set_Passable) != null && freeTraversal;
			bool weightedHeuristics = false;
			bool drafted = vehicle.Drafted;
			
			float heuristicStrength = DetermineHeuristicStrength(vehicle, start, dest);
			int ticksCardinal = vehicle.TicksPerMoveCardinal;
			int ticksDiagonal = vehicle.TicksPerMoveDiagonal;

			int minSize = VehicleMod.settings.main.fullVehiclePathing ? Mathf.Min(vehicleDef.Size.x, vehicleDef.Size.z) : 1;

			ChunkSet chunks = null;
			if (VehicleMod.settings.main.hierarchalPathfinding)
			{
				try
				{
					chunks = mapping[vehicleDef].VehicleReachability.FindChunks(start, dest, PathEndMode.OnCell, traverseParms, debugDrawSearch: drawPaths, SecondsBetweenDraws);
				}
				catch (Exception ex)
				{
					Log.Error($"Exception thrown while attempting to fetch chunks for HPA* search. Exception = {ex}");
					return PawnPath.NotFound;
				}
			}
			bool useHPA = VehicleMod.settings.main.hierarchalPathfinding && chunks != null && !chunks.NullOrEmpty();

			CalculateAndAddDisallowedCorners(traverseParms, peMode, cellRect);
			InitStatusesAndPushStartNode(ref startIndex, start);
			int iterations = 0;
			while (true)
			{
				if (token.IsCancellationRequested)
				{
					Debug.Message($"Path request cancelled. Exiting...");
					return PawnPath.NotFound;
				}

				iterations++;
				if (openList.Count <= 0)
				{
					break; //Exit loop, ran out of Nodes to process
				}
				CostNode costNode = openList.Pop();
				startIndex = costNode.index;
				if (costNode.cost == calcGrid[startIndex].costNodeCost && calcGrid[startIndex].status != statusClosedValue)
				{
					IntVec3 prevCell = cellIndices.IndexToCell(startIndex);
					int x2 = prevCell.x;
					int z2 = prevCell.z;
					if (drawPaths)
					{
						DebugFlash(prevCell, calcGrid[startIndex].knownCost / 1500f, calcGrid[startIndex].knownCost.ToString());
					}
					if (singleRect)
					{
						if (startIndex == destIndex)
						{
							goto PathFound;
						}
					}
					else if (cellRect.Contains(prevCell) && !disallowedCornerIndices.Contains(startIndex))
					{
						goto PathFound;
					}
					if (searchCount > SearchLimit)
					{
						goto HitSearchLimit;
					}

					for (int i = 0; i < 8; i++)
					{
						uint cellX = (uint)(x2 + directions[i]);
						uint cellY = (uint)(z2 + directions[i + 8]);
						
						if (cellX < mapSizeX && cellY < mapSizeZ)
						{
							int cellIntX = (int)cellX;
							int cellIntY = (int)cellY;
							int cellIndex = cellIndices.CellToIndex(cellIntX, cellIntY);
							
							IntVec3 cellToCheck = cellIndices.IndexToCell(cellIndex);
							Rot8 pathDir = Rot8.DirectionFromCells(prevCell, cellToCheck);
							if (useHPA)
							{
								if (!chunks.Cells.Contains(cellToCheck))
								{
									goto NeighborSearchEnd; //Node not included in hierarchal path, ignore
								}
							}

							if (calcGrid[cellIndex].status != statusClosedValue || weightedHeuristics)
							{
								int initialCost = 0;
								if (!vehicle.DrivableFast(cellIndex))
								{
									if (!passAllDestroyableThings)
									{
										if (drawPaths)
										{
											DebugFlash(new IntVec3(cellIntX, 0, cellIntY), 0.22f, "impass");
										}
										goto NeighborSearchEnd;
									}

									initialCost += 70;
									Building building = edificeGrid[cellIndex];
									if (building is null)
									{
										goto NeighborSearchEnd;
									}
									if (!IsDestroyable(building))
									{
										goto NeighborSearchEnd;
									}
									initialCost += (int)(building.HitPoints * 0.2f);
								}

								int tickCost = ((i <= 3) ? ticksCardinal : ticksDiagonal) + initialCost;
								if (VehicleMod.settings.main.smoothVehiclePaths && (vehicle.VehicleDef.size.x != 1 || vehicle.VehicleDef.size.z != 1)) //Don't add turn cost for 1x1 vehicles
								{
									if (pathDir != costNode.direction)
									{
										int turnCost = costNode.direction.Difference(pathDir) * TurnCostTicks;
										tickCost += turnCost;
									}
								}
								float totalAreaCost = 0;
								float rootCost = 0;
								CellRect cellToCheckRect = vehicle.VehicleRect(cellToCheck, pathDir);// CellRect.CenteredOn(cellToCheck, Mathf.FloorToInt(minSize / 2f));
								foreach (IntVec3 cellInRect in cellToCheckRect)
								{
									if (!vehicle.Drivable(cellInRect))
									{
										goto NeighborSearchEnd; //hitbox has invalid node, ignore in neighbor search
									}
									int cellToCheckIndex = cellIndices.CellToIndex(cellInRect);
									if (cellInRect == cellToCheck)
									{
										rootCost = vehicleArray[cellToCheckIndex] * RootPosWeight;
									}
									else
									{
										totalAreaCost += vehicleArray[cellToCheckIndex] * (1 - RootPosWeight);
									}
								}
								tickCost += Mathf.RoundToInt(totalAreaCost / (minSize * 2 - 1)); //minSize^2 - 1 to account for average of all cells except root
								tickCost += Mathf.RoundToInt(rootCost);
								tickCost += drafted ? topGrid[cellIndex].extraDraftedPerceivedPathCost : topGrid[cellIndex].extraNonDraftedPerceivedPathCost;
								if (byteGrid != null)
								{
									tickCost += byteGrid[cellIndex] * 8;
								}
								if (ThreadHelper.AnyVehicleBlockingPathAt(cellToCheck, vehicle) != null)
								{
									tickCost += Cost_VehicleCollision;
								}
								if (blueprintGrid[cellIndex] != null)
								{
									List<Blueprint> list = new List<Blueprint>(blueprintGrid[cellIndex]);
									if (!list.NullOrEmpty())
									{
										int num18 = 0;
										foreach (Blueprint blueprint in list)
										{
											num18 = Mathf.Max(num18, GetBlueprintCost(blueprint, vehicle));
										}
										if (num18 == int.MaxValue)
										{
											goto NeighborSearchEnd;
										}
										tickCost += num18;
									}
								}

								int calculatedCost = tickCost + calcGrid[startIndex].knownCost;
								ushort status = calcGrid[cellIndex].status;

								//For debug path drawing
								postCalculatedCells[cellToCheck] = calculatedCost;

								if (status == statusClosedValue || status == statusOpenValue)
								{
									int closedValueCost = 0;
									if (status == statusClosedValue)
									{
										closedValueCost = ticksCardinal;
									}
									if (calcGrid[cellIndex].knownCost <= calculatedCost + closedValueCost)
									{
										goto NeighborSearchEnd;
									}
								}
								if (weightedHeuristics)
								{
									calcGrid[cellIndex].heuristicCost = Mathf.RoundToInt(regionCostCalculator.GetPathCostFromDestToRegion(cellIndex) * RegionHeuristicWeightByNodesOpened.Evaluate(nodesOpened));
									if (calcGrid[cellIndex].heuristicCost < 0)
									{
										Log.ErrorOnce($"Heuristic cost overflow for vehicle {vehicle} pathing from {start} to {dest}.", vehicle.GetHashCode() ^ "FVPHeuristicCostOverflow".GetHashCode());
										calcGrid[cellIndex].heuristicCost = 0;
									}
								}
								else if (status != statusClosedValue && status != statusOpenValue)
								{
									int dx = Math.Abs(cellIntX - x);
									int dz = Math.Abs(cellIntY - z);
									int num21 = GenMath.OctileDistance(dx, dz, ticksCardinal, ticksDiagonal);
									calcGrid[cellIndex].heuristicCost = Mathf.RoundToInt(num21 * heuristicStrength);
								}
								int costWithHeuristic = calculatedCost + calcGrid[cellIndex].heuristicCost;
								if (costWithHeuristic < 0)
								{
									Log.ErrorOnce($"Node cost overflow for vehicle {vehicle} pathing from {start} to {dest}.", vehicle.GetHashCode() ^ "FVPNodeCostOverflow".GetHashCode());
									costWithHeuristic = 0;
								}
								calcGrid[cellIndex].parentIndex = startIndex;
								calcGrid[cellIndex].knownCost = calculatedCost;
								calcGrid[cellIndex].status = statusOpenValue;
								calcGrid[cellIndex].costNodeCost = costWithHeuristic;
								nodesOpened++;
								openList.Push(new CostNode(cellIndex, costWithHeuristic, pathDir));
							}
						}
						NeighborSearchEnd:;
					}
					searchCount++;
					calcGrid[startIndex].status = statusClosedValue;
					if (nodesOpened >= NodesToOpenBeforeRegionBasedPathing && allowedRegionTraversal && !weightedHeuristics)
					{
						weightedHeuristics = true;
						regionCostCalculator.Init(cellRect, traverseParms, ticksCardinal, ticksDiagonal, byteGrid, drafted, disallowedCornerIndices);
						InitStatusesAndPushStartNode(ref startIndex, start);
						nodesOpened = 0;
						searchCount = 0;
					}
				}
				CurrentNodeEnd:;
			}
			string curJob = vehicle.CurJob?.ToString() ?? "null";
			string curFaction = vehicle.Faction?.ToString() ?? "null";
			Log.Warning($"Vehicle {vehicle} pathing from {start} to {dest} ran out of cells to process. Job={curJob} Faction={curFaction} iterations={iterations}");
			DebugDrawRichData();
			return PawnPath.NotFound;
			PathFound:
			PawnPath result = FinalizedPath(startIndex, weightedHeuristics);
			DebugDrawPathCost();
			return result;
			HitSearchLimit:
			Log.Warning($"Vehicle {vehicle} pathing from {start} to {dest} hit search limit of {SearchLimit}.");
			DebugDrawRichData();
			return PawnPath.NotFound;
		}

		/// <summary>
		/// Retrieve cost to path through <paramref name="building"/>
		/// </summary>
		/// <remarks>
		/// Currently unused as vehicles cannot open doors
		/// </remarks>
		/// <param name="building"></param>
		/// <param name="traverseParms"></param>
		/// <param name="pawn"></param>
		public static int GetBuildingCost(Building building, TraverseParms traverseParms, Pawn pawn)
		{
			if (building is Building_Door door)
			{
				switch (traverseParms.mode)
				{
					case TraverseMode.ByPawn:
						if (!traverseParms.canBashDoors && door.IsForbiddenToPass(pawn))
						{
							if (DebugViewSettings.drawPaths)
							{
								DebugFlash(building.Position, building.Map, 0.77f, "forbid");
							}
							return int.MaxValue;
						}
						if (door.PawnCanOpen(pawn) && !door.FreePassage)
						{
							return door.TicksToOpenNow;
						}
						if (door.CanPhysicallyPass(pawn))
						{
							return 0;
						}
						if (traverseParms.canBashDoors)
						{
							return 300;
						}
						if (DebugViewSettings.drawPaths)
						{
							DebugFlash(building.Position, building.Map, 0.34f, "cant pass");
						}
						return int.MaxValue;
					case TraverseMode.PassDoors:
						if (pawn != null && door.PawnCanOpen(pawn) && !door.IsForbiddenToPass(pawn) && !door.FreePassage)
						{
							return door.TicksToOpenNow;
						}
						if ((pawn != null && door.CanPhysicallyPass(pawn)) || door.FreePassage)
						{
							return 0;
						}
						return 150;
					case TraverseMode.NoPassClosedDoors:
					case TraverseMode.NoPassClosedDoorsOrWater:
						if (door.FreePassage)
						{
							return 0;
						}
						return int.MaxValue;
					case TraverseMode.PassAllDestroyableThings:
					case TraverseMode.PassAllDestroyableThingsNotWater:
						if (pawn != null && door.PawnCanOpen(pawn) && !door.IsForbiddenToPass(pawn) && !door.FreePassage)
						{
							return door.TicksToOpenNow;
						}
						if ((pawn != null && door.CanPhysicallyPass(pawn)) || door.FreePassage)
						{
							return 0;
						}
						return 50 + (int)(door.HitPoints * 0.2f);
				}
			}
			else if (pawn != null)
			{
				return building.PathFindCostFor(pawn);
			}
			return 0;
		}

		/// <summary>
		/// Cost to path over blueprint
		/// </summary>
		/// <param name="b"></param>
		/// <param name="pawn"></param>
		public static int GetBlueprintCost(Blueprint blueprint, VehiclePawn vehicle)
		{
			if (vehicle != null)
			{
				return VehiclePathGrid.ImpassableCost; //blueprint.PathFindCostFor(vehicle) (need implementation for vehicles that should path over blueprints)
			}
			return 0;
		}

		/// <summary>
		/// Can path through <paramref name="thing"/> by destroying
		/// </summary>
		/// <param name="thing"></param>
		/// <returns></returns>
		public static bool IsDestroyable(Thing thing)
		{
			return thing.def.useHitPoints && thing.def.destroyable;
		}

		/// <summary>
		/// Diagonal movement is blocked
		/// </summary>
		/// <param name="map"></param>
		/// <param name="vehicle"></param>
		/// <param name="x"></param>
		/// <param name="z"></param>
		public static bool BlocksDiagonalMovement(Map map, VehicleDef vehicleDef, int x, int z)
		{
			return BlocksDiagonalMovement(map, vehicleDef, map.cellIndices.CellToIndex(x, z));
		}

		/// <summary>
		/// Diagonal movement is blocked
		/// </summary>
		/// <param name="map"></param>
		/// <param name="vehicle"></param>
		/// <param name="index"></param>
		public static bool BlocksDiagonalMovement(Map map, VehicleDef vehicleDef, int index)
		{
			return map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehiclePathGrid.WalkableFast(index) || map.edificeGrid[index] is Building_Door;
		}

		/// <summary>
		/// Diagonal movement is blocked
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="x"></param>
		/// <param name="z"></param>
		public static bool BlocksDiagonalMovement(VehiclePawn vehicle, int x, int z)
		{
			return BlocksDiagonalMovement(vehicle, vehicle.Map.cellIndices.CellToIndex(x, z));
		}

		/// <summary>
		/// Diagonal movement is blocked
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="index"></param>
		public static bool BlocksDiagonalMovement(VehiclePawn vehicle, int index)
		{
			return !vehicle.DrivableFast(index) || vehicle.Map.edificeGrid[index] is Building_Door;
		}

		/// <summary>
		/// Flash <paramref name="label"/> for debugging
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="colorPct"></param>
		/// <param name="label"></param>
		private void DebugFlash(IntVec3 cell, float colorPct, string label)
		{
			DebugFlash(cell, mapping.map, colorPct, label);
		}

		/// <summary>
		/// Flash <paramref name="str"/> on <paramref name="map"/> for debugging
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="map"></param>
		/// <param name="colorPct"></param>
		/// <param name="label"></param>
		private static void DebugFlash(IntVec3 cell, Map map, float colorPct, string label, int duration = 50)
		{
			map.debugDrawer.FlashCell(cell, colorPct, text: label, duration: duration);
			//CoroutineManager.QueueOrInvoke(() => map.DrawCell_ThreadSafe(cell, colorPct, label, duration), SecondsBetweenDraws);
		}

		/// <summary>
		/// Finalize path results from internal algorithm call
		/// </summary>
		/// <param name="finalIndex"></param>
		/// <param name="usedRegionHeuristics"></param>
		private PawnPath FinalizedPath(int finalIndex, bool usedRegionHeuristics)
		{
			PawnPath newPath = mapping.map.pawnPathPool.GetEmptyPawnPath();
			int num = finalIndex;
			for (;;)
			{
				VehiclePathFinderNodeFast shipPathFinderNodeFast = calcGrid[num];
				int parentIndex = shipPathFinderNodeFast.parentIndex;
				IntVec3 cell = mapping.map.cellIndices.IndexToCell(num);
				newPath.AddNode(cell);
				if (num == parentIndex)
				{
					break;
				}
				num = parentIndex;
			}
			newPath.SetupFound(calcGrid[finalIndex].knownCost, usedRegionHeuristics);
			return newPath;
		}

		/// <summary>
		/// Push <paramref name="start"/> onto node list and reset associated <paramref name="curIndex"/> costs
		/// </summary>
		/// <param name="curIndex"></param>
		/// <param name="start"></param>
		private void InitStatusesAndPushStartNode(ref int curIndex, IntVec3 start)
		{
			statusOpenValue += 2;
			statusClosedValue += 2;
			if (statusClosedValue >= 65435)
			{
				ResetStatuses();
			}
			curIndex = cellIndices.CellToIndex(start);
			calcGrid[curIndex].knownCost = 0;
			calcGrid[curIndex].heuristicCost = 0;
			calcGrid[curIndex].costNodeCost = 0;
			calcGrid[curIndex].parentIndex = curIndex;
			calcGrid[curIndex].status = statusOpenValue;
			openList.Clear();
			openList.Push(new CostNode(curIndex, 0, Rot8.Invalid));
		}

		/// <summary>
		/// Reset all node statuses
		/// </summary>
		private void ResetStatuses()
		{
			for(int i = 0; i < calcGrid.Length; i++)
			{
				calcGrid[i].status = 0;
			}
			statusOpenValue = 1;
			statusClosedValue = 2;
		}

		/// <summary>
		/// Draw all open cells
		/// </summary>
		internal void DebugDrawRichData()
		{
			if (VehicleMod.settings.debug.debugDrawVehiclePathCosts)
			{
				while (openList.Count > 0)
				{
					int index = openList.Pop().index;
					IntVec3 cell = new IntVec3(index % mapSizeX, 0, index / mapSizeX);
					DebugFlash(cell, 0, "open");
				}
			}
		}

		/// <summary>
		/// Draw all calculated path costs
		/// </summary>
		/// <param name="colorPct"></param>
		/// <param name="duration"></param>
		internal void DebugDrawPathCost(float colorPct = 0f, int duration = 50)
		{
			if (VehicleMod.settings.debug.debugDrawVehiclePathCosts)
			{
				foreach ((IntVec3 cell, int cost) in postCalculatedCells)
				{
					DebugFlash(cell, mapping.map, colorPct, cost.ToString(), duration: duration);
				}
			}
		}

		//REDO - Allow player to modify weighted heuristic or spin into seperate thread for long distance traversal with accurate pathing
		/// <summary>
		/// Heuristic strength to use for A* algorithm
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="start"></param>
		/// <param name="dest"></param>
		private float DetermineHeuristicStrength(VehiclePawn vehicle, IntVec3 start, LocalTargetInfo dest)
		{
			float lengthHorizontal = (start - dest.Cell).LengthHorizontal;
			return Mathf.RoundToInt(NonRegionBasedHeuristicStrength_DistanceCurve.Evaluate(lengthHorizontal));
		}

		/// <summary>
		/// Calculate rect on <paramref name="dest"/> target
		/// </summary>
		/// <param name="dest"></param>
		/// <param name="peMode"></param>
		private CellRect CalculateDestinationRect(LocalTargetInfo dest, PathEndMode peMode)
		{
			CellRect result;
			result = (!dest.HasThing || peMode == PathEndMode.OnCell) ? CellRect.SingleCell(dest.Cell) : dest.Thing.OccupiedRect();
			result = (peMode == PathEndMode.Touch) ? result.ExpandedBy(1) : result;
			return result;
		}

		/// <summary>
		/// Calculate disallowed corners at <paramref name="destinationRect"/>
		/// </summary>
		/// <param name="traverseParms"></param>
		/// <param name="peMode"></param>
		/// <param name="destinationRect"></param>
		private void CalculateAndAddDisallowedCorners(TraverseParms traverseParms, PathEndMode peMode, CellRect destinationRect)
		{
			disallowedCornerIndices.Clear();
			if (peMode == PathEndMode.Touch)
			{
				int minX = destinationRect.minX;
				int minZ = destinationRect.minZ;
				int maxX = destinationRect.maxX;
				int maxZ = destinationRect.maxZ;
				if (!IsCornerTouchAllowed(minX + 1, minZ + 1, minX + 1, minZ, minX, minZ + 1))
				{
					disallowedCornerIndices.Add(mapping.map.cellIndices.CellToIndex(minX, minZ));
				}
				if (!IsCornerTouchAllowed(minX + 1, maxZ - 1, minX + 1, maxZ, minX, maxZ - 1))
				{
					disallowedCornerIndices.Add(mapping.map.cellIndices.CellToIndex(minX, maxZ));
				}
				if (!IsCornerTouchAllowed(maxX - 1, maxZ - 1, maxX - 1, maxZ, maxX, maxZ - 1))
				{
					disallowedCornerIndices.Add(mapping.map.cellIndices.CellToIndex(maxX, maxZ));
				}
				if (!IsCornerTouchAllowed(maxX - 1, minZ + 1, maxX - 1, minZ, maxX, minZ + 1))
				{
					disallowedCornerIndices.Add(mapping.map.cellIndices.CellToIndex(maxX, minZ));
				}
			}
		}

		/// <summary>
		/// Corner touching is allowed at relevant coordinates
		/// </summary>
		/// <param name="cornerX"></param>
		/// <param name="cornerZ"></param>
		/// <param name="adjCardinal1X"></param>
		/// <param name="adjCardinal1Z"></param>
		/// <param name="adjCardinal2X"></param>
		/// <param name="adjCardinal2Z"></param>
		private bool IsCornerTouchAllowed(int cornerX, int cornerZ, int adjCardinal1X, int adjCardinal1Z, int adjCardinal2X, int adjCardinal2Z)
		{
			return TouchPathEndModeUtility.IsCornerTouchAllowed(cornerX, cornerZ, adjCardinal1X, adjCardinal1Z, adjCardinal2X, adjCardinal2Z, null); //REDO - PathingContext
		}

		/// <summary>
		/// Node data
		/// </summary>
		internal struct CostNode
		{
			public CostNode(int index, int cost, Rot8 direction)
			{
				this.index = index;
				this.cost = cost;
				this.direction = direction;
			}
			public int index;
			public int cost;
			public Rot8 direction;
		}

		/// <summary>
		/// Node data pre-calculation
		/// </summary>
		private struct VehiclePathFinderNodeFast
		{
			public int knownCost;
			public int heuristicCost;
			public int parentIndex;
			public int costNodeCost;
			public ushort status;
		}

		/// <summary>
		/// Node cost comparer for path determination
		/// </summary>
		internal class CostNodeComparer : IComparer<CostNode>
		{
			public int Compare(CostNode a, CostNode b)
			{
				return a.cost.CompareTo(b.cost);
			}
		}
	}
}
