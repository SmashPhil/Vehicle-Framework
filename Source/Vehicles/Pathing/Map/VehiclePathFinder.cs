using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using SmashTools;

namespace Vehicles.AI
{
	public class VehiclePathFinder
	{
		public const int Cost_OutsideAllowedArea = 600;
		private const int Cost_PawnCollision = 200; //175
		private const int NodesToOpenBeforeRegionbasedPathing_NonShip = 2000; //Needed?
		private const int NodesToOpenBeforeRegionBasedPathing_Ship = 100000; //Needed
		public const int DefaultMoveTicksCardinal = 13;
		private const int DefaultMoveTicksDiagonal = 18;
		private const int SearchLimit = 160000;

		internal Dictionary<IntVec3, int> postCalculatedCells = new Dictionary<IntVec3, int>();
		private Map map;
		internal bool report;
		private FastPriorityQueue<CostNode> openList;
		private VehiclePathFinderNodeFast[] calcGrid;

		private ushort statusOpenValue = 1;
		private ushort statusClosedValue = 2;

		private VehicleRegionCostCalculatorWrapper regionCostCalculatorSea;
		private RegionCostCalculatorWrapper regionCostCalculatorLand; //REDO - Remove 

		private int mapSizeX;
		private int mapSizeZ;

		private VehiclePathGrid VehiclePathGrid;
		private PathGrid pathGrid;

		private Building[] edificeGrid;
		private List<Blueprint>[] blueprintGrid;

		private CellIndices cellIndices;
		private List<int> disallowedCornerIndices = new List<int>(4);

		private static readonly int[] Directions = new int[]
		{
			0,
			1,
			0,
			-1,
			1,
			1,
			-1,
			-1,
			-1,
			0,
			1,
			0,
			-1,
			1,
			1,
			-1
		};

		private static readonly SimpleCurve NonRegionBasedHeuristicStrengthHuman_DistanceCurve = new SimpleCurve
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

		private static readonly SimpleCurve RegionheuristicWeighByNodesOpened = new SimpleCurve
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

		public VehiclePathFinder(Map map, bool report = true)
		{
			this.map = map;
			mapSizeX = map.Size.x;
			mapSizeZ = map.Size.z;
			calcGrid = new VehiclePathFinderNodeFast[mapSizeX * mapSizeZ];
			openList = new FastPriorityQueue<CostNode>(new CostNodeComparer());
			regionCostCalculatorSea = new VehicleRegionCostCalculatorWrapper(map);
			regionCostCalculatorLand = new RegionCostCalculatorWrapper(map);
			postCalculatedCells = new Dictionary<IntVec3, int>();
			this.report = report;
		}

		public (PawnPath path, bool found) FindVehiclePath(IntVec3 start, LocalTargetInfo dest, VehiclePawn pawn, CancellationToken token, PathEndMode peMode = PathEndMode.OnCell)
		{
			if(pawn.LocationRestrictedBySize(dest.Cell))
			{
				Messages.Message("VehicleCannotFit".Translate(), MessageTypeDefOf.RejectInput);
				return (PawnPath.NotFound, false);
			}
			Danger maxDanger = Danger.Deadly;
			return FindVehiclePath(start, dest, TraverseParms.For(pawn, maxDanger, TraverseMode.ByPawn, false), token, peMode, pawn.IsBoat());
		}

		public (PawnPath path, bool found) FindVehiclePath(IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms,  CancellationToken token, PathEndMode peMode = PathEndMode.OnCell, bool waterPathing = false)
		{
			if (report)
			{
				Debug.Message($"{VehicleHarmony.LogLabel} MainPath for {traverseParms.pawn.LabelShort} - ThreadId: [{Thread.CurrentThread.ManagedThreadId}] TaskId: [{Task.CurrentId}]");
			}

			postCalculatedCells.Clear();
			VehicleMapping VehicleMapping = map.GetCachedMapComponent<VehicleMapping>();
			if(DebugSettings.pathThroughWalls)
			{
				traverseParms.mode = TraverseMode.PassAllDestroyableThings;
			}
			VehiclePawn pawn = traverseParms.pawn as VehiclePawn;
			if(!pawn.IsBoat() && waterPathing)
			{
				Log.Error($"Set to waterPathing but {pawn.LabelShort} is not registered as a Boat. Self Correcting...");
				waterPathing = false;
			}
			if (!(pawn is null) && pawn.Map != map)
			{
				Log.Error(string.Concat(new object[]
				{
					"Tried to FindVehiclePath for pawn which is spawned in another map. Their map PathFinder should  have been used, not this one. " +
					"pawn=", pawn,
					" pawn.Map=", pawn.Map,
					" map=", map
				}));
				return (PawnPath.NotFound, false);
			}
			if(!start.IsValid)
			{
				Log.Error(string.Concat(new object[]
				{
					"Tried to FindShipPath with invalid start ",
					start,
					", pawn=", pawn
				}));
				return (PawnPath.NotFound, false);
			}
			if (!dest.IsValid)
			{
				Log.Error(string.Concat(new object[]
				{
					"Tried to FindPath with invalid dest ",
					dest,
					", pawn= ",
					pawn
				}));
				return (PawnPath.NotFound, false);
			}
			if(traverseParms.mode == TraverseMode.ByPawn)
			{
				if(waterPathing)
				{
					if(!ShipReachabilityUtility.CanReachShip(pawn, dest, peMode, Danger.Deadly, false, traverseParms.mode))
					{
						return (PawnPath.NotFound, false);
					}
				}
				else
				{
					if(!ReachabilityUtility.CanReach(pawn, dest, peMode, Danger.Deadly, false, traverseParms.mode))
					{
						return (PawnPath.NotFound, false);
					}
				}
				
			}
			else
			{
				if (waterPathing)
				{
					if(!VehicleMapping.VehicleReachability.CanReachShip(start, dest, peMode, traverseParms))
					{
						return (PawnPath.NotFound, false);
					}
				}
				else
				{
					if(!map.reachability.CanReach(start, dest, peMode, traverseParms))
					{
						return (PawnPath.NotFound, false);
					}
				}
			}
			cellIndices = map.cellIndices;

			VehiclePathGrid = VehicleMapping.VehiclePathGrid;
			pathGrid = map.pathGrid;
			this.edificeGrid = map.edificeGrid.InnerArray;
			blueprintGrid = map.blueprintGrid.InnerArray;
			int x = dest.Cell.x;
			int z = dest.Cell.z;
			int num = cellIndices.CellToIndex(start);
			int num2 = cellIndices.CellToIndex(dest.Cell);
			ByteGrid byteGrid = (pawn is null) ? null : pawn.GetAvoidGrid(true);
			bool flag = traverseParms.mode == TraverseMode.PassAllDestroyableThings || traverseParms.mode == TraverseMode.PassAllDestroyableThingsNotWater;
			bool flag2 = traverseParms.mode != TraverseMode.NoPassClosedDoorsOrWater && traverseParms.mode != TraverseMode.PassAllDestroyableThingsNotWater;
			bool flag3 = !flag;
			CellRect cellRect = CalculateDestinationRect(dest, peMode);
			bool flag4 = cellRect.Width == 1 && cellRect.Height == 1;
			int[] boatsArray = VehiclePathGrid.pathGrid;
			int[] vehicleArray = pathGrid.pathGrid;
			TerrainDef[] topGrid = map.terrainGrid.topGrid;
			EdificeGrid edificeGrid = map.edificeGrid;
			int num3 = 0;
			int num4 = 0;
			Area allowedArea = GetAllowedArea(pawn);
			bool flag5 = !(pawn is null) && PawnUtility.ShouldCollideWithPawns(pawn);
			bool flag6 = true && DebugViewSettings.drawPaths;
			bool flag7 = !flag && !(VehicleGridsUtility.GetRegion(start, map, RegionType.Set_Passable) is null) && flag2;
			bool flag8 = !flag || !flag3;
			bool flag9 = false;
			bool flag10 = !(pawn is null) && pawn.Drafted;
			bool flag11 = !(pawn is null) && !(pawn is null);

			int num5 = (!flag11) ? NodesToOpenBeforeRegionbasedPathing_NonShip : NodesToOpenBeforeRegionBasedPathing_Ship;
			int num6 = 0;
			int num7 = 0;
			float num8 = DetermineHeuristicStrength(pawn, start, dest);
			int num9 = !(pawn is null) ? pawn.TicksPerMoveCardinal : DefaultMoveTicksCardinal;
			int num10 = !(pawn is null) ? pawn.TicksPerMoveDiagonal : DefaultMoveTicksDiagonal;

			CalculateAndAddDisallowedCorners(traverseParms, peMode, cellRect);
			InitStatusesAndPushStartNode(ref num, start);
			Rot8 rot = pawn.FullRotation;
			int iterations = 0;
			for(;;)
			{
				if (token.IsCancellationRequested)
				{
					return (PawnPath.NotFound, false);
				}

				iterations++;
				if(openList.Count <= 0)
				{
					break;
				}
				num6 += openList.Count;
				num7++;
				CostNode costNode = openList.Pop();
				num = costNode.index;
				if(costNode.cost == calcGrid[num].costNodeCost && calcGrid[num].status != statusClosedValue)
				{
					IntVec3 c = cellIndices.IndexToCell(num);
					IntVec3 prevCell = c;
					int x2 = c.x;
					int z2 = c.z;
					if(flag6)
					{
						DebugFlash(c, calcGrid[num].knownCost / 1500f, calcGrid[num].knownCost.ToString());
					}
					if(flag4)
					{
						if(num == num2)
						{
							goto Block_32;
						}
					}
					else if(cellRect.Contains(c) && !disallowedCornerIndices.Contains(num))
					{
						goto Block_32;
					}
					if(num3 > SearchLimit)
					{
						goto Block_33;
					}

					List<IntVec3> fullRectCells = CellRect.CenteredOn(c, pawn.def.size.x, pawn.def.size.z).Where(cl2 => cl2 != c).ToList();

					for(int i = 0; i < 8; i++)
					{
						uint num11 = (uint)(x2 + Directions[i]);   //x
						uint num12 = (uint)(z2 + Directions[i + 8]); //y

						if(num11 < ((ulong)mapSizeX) && num12 < (ulong)(mapSizeZ))
						{
							int num13 = (int)num11;
							int num14 = (int)num12;
							int num15 = cellIndices.CellToIndex(num13, num14);
							
							IntVec3 cellToCheck = cellIndices.IndexToCell(num15);
							if (VehicleMod.settings.main.fullVehiclePathing && pawn.LocationRestrictedBySize(cellToCheck))
							{
								goto EndPathing;
							}

							if(calcGrid[num15].status != statusClosedValue || flag9)
							{
								int num16 = 0;
								bool flag12 = false; //Extra cost for traversing water

								if(flag2 || !new IntVec3(num13, 0 ,num14).GetTerrain(map).HasTag("Water"))
								{
									if(waterPathing)
									{
										if(!pawn.DrivableFast(num15))
										{
											if(!flag)
											{
												if(flag6)
												{
													DebugFlash(new IntVec3(num13, 0, num14), 0.22f, "walk");
												}
												goto EndPathing;
											}
											
											num16 += 70;
											Building building = edificeGrid[num15];
											if (building is null)
											{
												goto EndPathing;
											}
											if(!IsDestroyable(building))
											{
												goto EndPathing;
											}
											num16 += (int)(building.HitPoints * 0.2f);
										}
									}
									else
									{
										if(!pawn.DrivableFast(num15))
										{
											if(!flag)
											{
												if(flag6)
												{
													DebugFlash(new IntVec3(num13, 0, num14), 0.22f, "walk");
												}
												goto EndPathing;
											}
											flag12 = true;
											num16 += 70;
											Building building = edificeGrid[num15];
											if (building is null)
											{
												goto EndPathing;
											}
											if(!IsDestroyable(building))
											{
												goto EndPathing;
											}
											num16 += (int)(building.HitPoints * 0.2f);
										}
									}
									
									if(i > 3)
									{
										switch(i)
										{
											case 4:
												if(BlocksDiagonalMovement(pawn, num - mapSizeX))
												{
													if(flag8)
													{
														if(flag6)
														{
															DebugFlash(new IntVec3(x2, 0, z2 - 1), 0.9f, "ships");
														}
														goto EndPathing;
													}
													num16 += 70;
												}
												if(BlocksDiagonalMovement(pawn, num + 1))
												{
													if(flag8)
													{
														if(flag6)
														{
															DebugFlash(new IntVec3(x2 + 1, 0, z2), 0.9f, "ships");
														}
														goto EndPathing;
													}
													num16 += 70;
												}
												break;
											case 5:
												if(BlocksDiagonalMovement(pawn, num + mapSizeX))
												{
													if(flag8)
													{
														if(flag6)
														{
															DebugFlash(new IntVec3(x2, 0, z2 + 1), 0.9f, "ships");
														}
														goto EndPathing;
													}
													num16 += 70;
												}
												if(BlocksDiagonalMovement(pawn, num + 1))
												{
													if(flag8)
													{
														if(flag6)
														{
															DebugFlash(new IntVec3(x2 + 1, 0, z2), 0.9f, "ships");
														}
														goto EndPathing;
													}
													num16 += 70;
												}
												break;
											case 6:
												if(BlocksDiagonalMovement(pawn, num + mapSizeX))
												{
													if(flag8)
													{
														if(flag6)
														{
															DebugFlash(new IntVec3(x2, 0, z2 + 1), 0.9f, "ships");
														}
														goto EndPathing;
													}
													num16 += 70;
												}
												if(BlocksDiagonalMovement(pawn, num - 1))
												{
													if(flag8)
													{
														if(flag6)
														{
															DebugFlash(new IntVec3(x2 - 1, 0, z2), 0.9f, "ships");
														}
														goto EndPathing;
													}
													num16 += 70;
												}
												break;
											case 7:
												if(BlocksDiagonalMovement(pawn, num - mapSizeX))
												{
													if(flag8)
													{
														if(flag6)
														{
															DebugFlash(new IntVec3(x2, 0, z2 - 1), 0.9f, "ships");
														}
														goto EndPathing;
													}
													num16 += 70;
												}
												if(BlocksDiagonalMovement(pawn, num - 1))
												{
													if(flag8)
													{
														if(flag6)
														{
															DebugFlash(new IntVec3(x2 - 1, 0, z2), 0.9f, "ships");
														}
														goto EndPathing;
													}
													num16 += 70;
												}
												break;
										}
									}
									int num17 = (i <= 3) ? num9 : num10;
									num17 += num16;
									//if (Rot8.DirectionFromCells(prevCell, cellToCheck) != rot)
									//{
									//    Log.Message("Additional Cost");
									//    num17 += ChangeDirectionAdditionalCost;
									//}
									if (!flag12 && !waterPathing)
									{
										//Extra Terrain costs
										if (pawn.VehicleDef.properties.customTerrainCosts?.NotNullAndAny() ?? false)
										{
											TerrainDef currentTerrain = map.terrainGrid.TerrainAt(num15);
											if (pawn.VehicleDef.properties.customTerrainCosts.ContainsKey(currentTerrain))
											{
												int customCost = pawn.VehicleDef.properties.customTerrainCosts[currentTerrain];
												if(customCost < 0)
												{
													goto EndPathing;
												}
												num17 += customCost;
											}
											else
											{
												num17 += vehicleArray[num15];
											}
										}
										else
										{
											num17 += vehicleArray[num15];
										}
										num17 += flag10 ? topGrid[num15].extraDraftedPerceivedPathCost : topGrid[num15].extraNonDraftedPerceivedPathCost;
									}
									if (byteGrid != null)
									{
										num17 += (byteGrid[num15] * 8);
									}
									//Allowed area cost?
									if(flag5 && MultithreadHelper.AnyVehicleBlockingPathAt(new IntVec3(num13, 0, num14), pawn, false, false, true) != null)
									{
										num17 += Cost_PawnCollision;
									}
									Building building2 = edificeGrid[num15];
									if(!(building2 is null))
									{
										//Building Costs Here
									}
									if(blueprintGrid[num15] != null)
									{
										List<Blueprint> list = new List<Blueprint>(blueprintGrid[num15]);
										if(!list.NullOrEmpty())
										{
											int num18 = 0;
											foreach(Blueprint bp in list)
											{
												num18 = Mathf.Max(num18, GetBlueprintCost(bp, pawn));
											}
											if(num18 == int.MaxValue)
											{
												goto EndPathing;
											}
											num17 += num18;
										}
									}
									
									int num19 = num17 + calcGrid[num].knownCost;
									ushort status = calcGrid[num15].status;
									
									//if(pawn.Props.useFullHitboxPathing)
									//{
									//    foreach(IntVec3 fullRect in fullRectCells)
									//    {
									//        if(fullRect != cellToCheck)
									//        {
									//            num19 += calcGrid[cellIndices.CellToIndex(fullRect)].knownCost;
									//            Log.Message($"Cell: {fullRect} Cost: {num19}");
									//            if(postCalculatedCells.ContainsKey(fullRect))
									//            {
									//                postCalculatedCells[fullRect] = num19;
									//            }
									//            else
									//            {
									//                postCalculatedCells.Add(fullRect, num19);
									//            }
									//        }
									//    }
									//}

									//Only generate path costs for linear non-reverse pathing check
									if(report)
									{
										if(postCalculatedCells.ContainsKey(cellToCheck))
										{
											postCalculatedCells[cellToCheck] = num19;
										}
										else
										{
											postCalculatedCells.Add(cellToCheck, num19);
										}
									}

									if (waterPathing && !map.terrainGrid.TerrainAt(num15).IsWater)
										num19 += 10000;
									if (status == statusClosedValue || status == statusOpenValue)
									{
										int num20 = 0;
										if(status == statusClosedValue)
										{
											num20 = num9;
										}
										if(calcGrid[num15].knownCost <= num19 + num20)
										{
											goto EndPathing;
										}
									}
									if (flag9)
									{
										calcGrid[num15].heuristicCost = waterPathing ? Mathf.RoundToInt(regionCostCalculatorSea.GetPathCostFromDestToRegion(num15) *
											RegionheuristicWeighByNodesOpened.Evaluate(num4)) : Mathf.RoundToInt(regionCostCalculatorLand.GetPathCostFromDestToRegion(num15) *
											RegionheuristicWeighByNodesOpened.Evaluate(num4));
										if(calcGrid[num15].heuristicCost < 0)
										{
											Log.ErrorOnce(string.Concat(new object[]
											{
												"Heuristic cost overflow for vehicle ", pawn.ToStringSafe<Pawn>(),
												" pathing from ", start,
												" to ", dest, "."
											}), pawn.GetHashCode() ^ 193840009);
											calcGrid[num15].heuristicCost = 0;
										}
									}
									else if(status != statusClosedValue && status != statusOpenValue)
									{
										int dx = Math.Abs(num13 - x);
										int dz = Math.Abs(num14 - z);
										int num21 = GenMath.OctileDistance(dx, dz, num9, num10);
										calcGrid[num15].heuristicCost = Mathf.RoundToInt((float)num21 * num8);
									}
									int num22 = num19 + calcGrid[num15].heuristicCost;
									if(num22 < 0)
									{
										Log.ErrorOnce(string.Concat(new object[]
										{
											"Node cost overflow for ship ", pawn.ToStringSafe<Pawn>(),
											" pathing from ", start,
											" to ", dest, "."
										}), pawn.GetHashCode() ^ 87865822);
										num22 = 0;
									}
									calcGrid[num15].parentIndex = num;
									calcGrid[num15].knownCost = num19;
									calcGrid[num15].status = statusOpenValue;
									calcGrid[num15].costNodeCost = num22;
									num4++;
									rot = Rot8.DirectionFromCells(prevCell, cellToCheck);
									openList.Push(new CostNode(num15, num22));
								}
							}
						}
						EndPathing:;
					}
					num3++;
					calcGrid[num].status = statusClosedValue;
					if(num4 >= num5 && flag7 && !flag9)
					{
						flag9 = true;
						if (waterPathing)
							regionCostCalculatorSea.Init(cellRect, traverseParms, num9, num10, byteGrid, allowedArea, flag10, disallowedCornerIndices);
						else
							regionCostCalculatorLand.Init(cellRect, traverseParms, num9, num10, byteGrid, allowedArea, flag10, disallowedCornerIndices);

						InitStatusesAndPushStartNode(ref num, start);
						num4 = 0;
						num3 = 0;
					}
				}
			}
			string text = ((pawn is null) || pawn.CurJob is null) ? "null" : pawn.CurJob.ToString();
			string text2 = ((pawn is null) || pawn.Faction is null) ? "null" : pawn.Faction.ToString();
			if(report)
			{
				Log.Warning(string.Concat(new object[]
				{
					"ship pawn: ", pawn, " pathing from ", start,
					" to ", dest, " ran out of cells to process.\nJob:", text,
					"\nFaction: ", text2,
					"\niterations: ", iterations
				}));
			}
			DebugDrawRichData();
			return (PawnPath.NotFound, false);
			Block_32:
			PawnPath result = PawnPath.NotFound;
			if (report)
			{
				result = FinalizedPath(num, flag9);
			}
			DebugDrawPathCost();
			return (result, true);
			Block_33:
			Log.Warning(string.Concat(new object[]
			{
				"Ship ", pawn, " pathing from ", start,
				" to ", dest, " hit search limit of ", SearchLimit, " cells."
			}));
			DebugDrawRichData();
			return (PawnPath.NotFound, false);
		}

		public static int GetBuildingCost(Building b, TraverseParms traverseParms, Pawn pawn)
		{
			Building_Door building_Door = b as Building_Door;
			if (building_Door != null)
			{
				switch (traverseParms.mode)
				{
					case TraverseMode.ByPawn:
						if (!traverseParms.canBash && building_Door.IsForbiddenToPass(pawn))
						{
							if (DebugViewSettings.drawPaths)
							{
								DebugFlash(b.Position, b.Map, 0.77f, "forbid");
							}
							return int.MaxValue;
						}
						if (building_Door.PawnCanOpen(pawn) && !building_Door.FreePassage)
						{
							return building_Door.TicksToOpenNow;
						}
						if (building_Door.CanPhysicallyPass(pawn))
						{
							return 0;
						}
						if (traverseParms.canBash)
						{
							return 300;
						}
						if (DebugViewSettings.drawPaths)
						{
							DebugFlash(b.Position, b.Map, 0.34f, "cant pass");
						}
						return int.MaxValue;
					case TraverseMode.PassDoors:
						if (pawn != null && building_Door.PawnCanOpen(pawn) && !building_Door.IsForbiddenToPass(pawn) && !building_Door.FreePassage)
						{
							return building_Door.TicksToOpenNow;
						}
						if ((pawn != null && building_Door.CanPhysicallyPass(pawn)) || building_Door.FreePassage)
						{
							return 0;
						}
						return 150;
					case TraverseMode.NoPassClosedDoors:
					case TraverseMode.NoPassClosedDoorsOrWater:
						if (building_Door.FreePassage)
						{
							return 0;
						}
						return int.MaxValue;
					case TraverseMode.PassAllDestroyableThings:
					case TraverseMode.PassAllDestroyableThingsNotWater:
						if (pawn != null && building_Door.PawnCanOpen(pawn) && !building_Door.IsForbiddenToPass(pawn) && !building_Door.FreePassage)
						{
							return building_Door.TicksToOpenNow;
						}
						if ((pawn != null && building_Door.CanPhysicallyPass(pawn)) || building_Door.FreePassage)
						{
							return 0;
						}
						return 50 + (int)((float)building_Door.HitPoints * 0.2f);
				}
			}
			else if (pawn != null)
			{
				return (int)b.PathFindCostFor(pawn);
			}
			return 0;
		}

		public static int GetBlueprintCost(Blueprint b, Pawn pawn)
		{
			if(pawn != null)
			{
				return (int)b.PathFindCostFor(pawn);
			}
			return 0;
		}

		public static bool IsDestroyable(Thing t)
		{
			return t.def.useHitPoints && t.def.destroyable;
		}

		public static bool BlocksDiagonalMovement(Map map, int x, int z)
		{
			return BlocksDiagonalMovement(map, map.cellIndices.CellToIndex(x, z));
		}

		public static bool BlocksDiagonalMovement(Map map, int index)
		{
			return map.GetCachedMapComponent<VehicleMapping>().VehiclePathGrid.WalkableFast(index) || map.edificeGrid[index] is Building_Door;
		}

		public static bool BlocksDiagonalMovement(VehiclePawn vehicle, int x, int z)
		{
			return BlocksDiagonalMovement(vehicle, vehicle.Map.cellIndices.CellToIndex(x, z));
		}

		public static bool BlocksDiagonalMovement(VehiclePawn vehicle, int index)
		{
			return !vehicle.DrivableFast(index) || vehicle.Map.edificeGrid[index] is Building_Door;
		}

		private void DebugFlash(IntVec3 c, float colorPct, string str)
		{
			DebugFlash(c, map, colorPct, str);
		}

		private static void DebugFlash(IntVec3 c, Map map, float colorPct, string str)
		{
			map.debugDrawer.FlashCell(c, colorPct, str, 50);
		}

		private PawnPath FinalizedPath(int finalIndex, bool usedRegionHeuristics)
		{
			PawnPath emptyPawnPath = map.pawnPathPool.GetEmptyPawnPath();
			int num = finalIndex;
			for(; ;)
			{
				VehiclePathFinderNodeFast shipPathFinderNodeFast = calcGrid[num];
				int parentIndex = shipPathFinderNodeFast.parentIndex;
				IntVec3 cell = map.cellIndices.IndexToCell(num);
				emptyPawnPath.AddNode(cell);
				if(num == parentIndex)
				{
					break;
				}
				num = parentIndex;
			}
			emptyPawnPath.SetupFound(calcGrid[finalIndex].knownCost, usedRegionHeuristics);
			return emptyPawnPath;
		}

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
			openList.Push(new CostNode(curIndex, 0));
		}

		private void ResetStatuses()
		{
			for(int i = 0; i < calcGrid.Length; i++)
			{
				calcGrid[i].status = 0;
			}
			statusOpenValue = 1;
			statusClosedValue = 2;
		}

		internal void DebugDrawRichData()
		{
			if (VehicleMod.settings.debug.debugDrawVehiclePathCosts)
			{
				while (openList.Count > 0)
				{
					int index = openList.Pop().index;
					IntVec3 c = new IntVec3(index % mapSizeX, 0, index / mapSizeX);
					map.debugDrawer.FlashCell(c, 0f, "open", 50);
				}
			}
		}

		internal void DebugDrawPathCost(float colorPct = 0f, int duration = 50)
		{
			if (VehicleMod.settings.debug.debugDrawVehiclePathCosts)
			{
				foreach (KeyValuePair<IntVec3, int> pathCells in postCalculatedCells)
				{
					map.debugDrawer.FlashCell(pathCells.Key, colorPct, pathCells.Value.ToString(), duration);
				}
			}
		}

		private float DetermineHeuristicStrength(Pawn pawn, IntVec3 start, LocalTargetInfo dest)
		{
			if(!(pawn is VehiclePawn))
			{
				Log.Error("Called method for pawn " + pawn + " in ShipPathFinder which should never occur. Contact mod author.");
			}

			float lengthHorizontal = (start - dest.Cell).LengthHorizontal;
			return (float)Mathf.RoundToInt(NonRegionBasedHeuristicStrengthHuman_DistanceCurve.Evaluate(lengthHorizontal));
		}

		private Area GetAllowedArea(Pawn pawn)
		{
			if(pawn != null && pawn.playerSettings != null && !pawn.Drafted && ForbidUtility.CaresAboutForbidden(pawn, true))
			{
				Area area = pawn.playerSettings.EffectiveAreaRestrictionInPawnCurrentMap;
				if(!(area is null) && area.TrueCount <= 0)
				{
					area = null;
				}
				return area;
			}
			return null;
		}

		private CellRect CalculateDestinationRect(LocalTargetInfo dest, PathEndMode peMode)
		{
			CellRect result;
			result = (!dest.HasThing || peMode == PathEndMode.OnCell) ? CellRect.SingleCell(dest.Cell) : dest.Thing.OccupiedRect();
			result = (peMode == PathEndMode.Touch) ? result.ExpandedBy(1) : result;
			return result;
		}

		private void CalculateAndAddDisallowedCorners(TraverseParms traverseParms, PathEndMode peMode, CellRect destinationRect)
		{
			this.disallowedCornerIndices.Clear();
			if (peMode == PathEndMode.Touch)
			{
				int minX = destinationRect.minX;
				int minZ = destinationRect.minZ;
				int maxX = destinationRect.maxX;
				int maxZ = destinationRect.maxZ;
				if (!this.IsCornerTouchAllowed(minX + 1, minZ + 1, minX + 1, minZ, minX, minZ + 1))
				{
					this.disallowedCornerIndices.Add(this.map.cellIndices.CellToIndex(minX, minZ));
				}
				if (!this.IsCornerTouchAllowed(minX + 1, maxZ - 1, minX + 1, maxZ, minX, maxZ - 1))
				{
					this.disallowedCornerIndices.Add(this.map.cellIndices.CellToIndex(minX, maxZ));
				}
				if (!this.IsCornerTouchAllowed(maxX - 1, maxZ - 1, maxX - 1, maxZ, maxX, maxZ - 1))
				{
					this.disallowedCornerIndices.Add(this.map.cellIndices.CellToIndex(maxX, maxZ));
				}
				if (!this.IsCornerTouchAllowed(maxX - 1, minZ + 1, maxX - 1, minZ, maxX, minZ + 1))
				{
					this.disallowedCornerIndices.Add(this.map.cellIndices.CellToIndex(maxX, minZ));
				}
			}
		}

		private bool IsCornerTouchAllowed(int cornerX, int cornerZ, int adjCardinal1X, int adjCardinal1Z, int adjCardinal2X, int adjCardinal2Z)
		{
			return TouchPathEndModeUtility.IsCornerTouchAllowed(cornerX, cornerZ, adjCardinal1X, adjCardinal1Z, adjCardinal2X, adjCardinal2Z, map);
		}

		internal struct CostNode
		{
			public CostNode(int index, int cost)
			{
				this.index = index;
				this.cost = cost;
			}
			public int index;
			public int cost;
		}

		private struct VehiclePathFinderNodeFast
		{
			public int knownCost;
			public int heuristicCost;
			public int parentIndex;
			public int costNodeCost;
			public ushort status;
		}

		internal class CostNodeComparer : IComparer<CostNode>
		{
			public int Compare(CostNode a, CostNode b)
			{
				return a.cost.CompareTo(b.cost);
			}
		}
	}
}
