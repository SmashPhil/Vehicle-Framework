using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	internal class VehiclePathing : IPatchCategory
	{
		public void PatchMethods()
		{
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(FloatMenuMakerMap), "GotoLocationOption"),
				prefix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(GotoLocationVehicles)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(JobDriver_Goto), "MakeNewToils"),
				postfix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(GotoToilsPassthrough)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pawn_PathFollower), "NeedNewPath"),
				postfix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(IsVehicleInNextCell)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.StartPath)),
				prefix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(StartVehiclePath)));
			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(PathFinder), nameof(PathFinder.FindPath), new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode), typeof(PathFinderCostTuning) }),
			//	transpiler: new HarmonyMethod(typeof(VehiclePathing),
			//	nameof(PathAroundVehicles)));
			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(Reachability), nameof(Reachability.CanReach), new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms) }),
			//	prefix: new HarmonyMethod(typeof(VehiclePathing),
			//	nameof(CanReachVehiclePosition)));
			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(GenGrid), nameof(GenGrid.Impassable)),
			//	postfix: new HarmonyMethod(typeof(VehiclePathing),
			//	nameof(ImpassableThroughVehicle)));
			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(PathGrid), nameof(PathGrid.Walkable)),
			//	postfix: new HarmonyMethod(typeof(VehiclePathing),
			//	nameof(WalkableThroughVehicle)));
			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(PathGrid), nameof(PathGrid.WalkableFast), new Type[] { typeof(IntVec3) }),
			//	postfix: new HarmonyMethod(typeof(VehiclePathing),
			//	nameof(WalkableFastThroughVehicleIntVec3)));
			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(PathGrid), nameof(PathGrid.WalkableFast), new Type[] { typeof(int), typeof(int) }),
			//	postfix: new HarmonyMethod(typeof(VehiclePathing),
			//	nameof(WalkableFastThroughVehicleInt2)));
			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(PathGrid), nameof(PathGrid.WalkableFast), new Type[] { typeof(int) }),
			//	postfix: new HarmonyMethod(typeof(VehiclePathing),
			//	nameof(WalkableFastThroughVehicleInt)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(GenAdj), nameof(GenAdj.OccupiedRect), parameters: new Type[] { typeof(Thing) }),
				prefix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(OccupiedRectVehicles)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pathing), nameof(Pathing.RecalculateAllPerceivedPathCosts)),
				postfix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(RecalculateAllPerceivedPathCostForVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pathing), nameof(Pathing.RecalculatePerceivedPathCostAt)),
				postfix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(RecalculatePerceivedPathCostForVehicle)));

			VehicleHarmony.Patch(original: AccessTools.Method(typeof(RegionDirtyer), "Notify_ThingAffectingRegionsSpawned"),
				postfix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(Notify_ThingAffectingVehicleRegionsSpawned)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(RegionDirtyer), "Notify_ThingAffectingRegionsDespawned"),
				postfix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(Notify_ThingAffectingVehicleRegionsDespawned)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(RegionListersUpdater), nameof(RegionListersUpdater.RegisterInRegions)),
				postfix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(RegisterInVehicleRegions)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(RegionListersUpdater), nameof(RegionListersUpdater.DeregisterInRegions)),
				postfix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(DeregisterInVehicleRegions)));
		}

		/// <summary>
		/// Intercepts FloatMenuMakerMap call to restrict by size and call through to custom water based pathing requirements
		/// </summary>
		/// <param name="clickCell"></param>
		/// <param name="pawn"></param>
		/// <param name="__result"></param>
		/// <returns></returns>
		public static bool GotoLocationVehicles(IntVec3 clickCell, Pawn pawn, ref FloatMenuOption __result)
		{
			if (pawn is VehiclePawn vehicle)
			{
				if (vehicle.Faction != Faction.OfPlayer || !vehicle.CanMoveFinal)
				{
					return false;
				}
				if (VehicleMod.settings.main.fullVehiclePathing && !vehicle.FitsOnCell(clickCell))
				{
					Messages.Message("VehicleCannotFit".Translate(), MessageTypeDefOf.RejectInput);
					return false;
				}

				if (vehicle.CompFueledTravel != null && vehicle.CompFueledTravel.EmptyTank)
				{
					Messages.Message("VehicleOutOfFuel".Translate(), MessageTypeDefOf.RejectInput);
					return false;
				}

				VehicleMapping mapping = vehicle.Map.GetCachedMapComponent<VehicleMapping>();
				Debug.Message($"Click: {clickCell} Terrain={vehicle.Map.terrainGrid.TerrainAt(clickCell).LabelCap} CalculatedCost={mapping[vehicle.VehicleDef].VehiclePathGrid.CalculatedCostAt(clickCell)}" +
					$" GridCost={mapping[vehicle.VehicleDef].VehiclePathGrid.pathGrid[vehicle.Map.cellIndices.CellToIndex(clickCell)]} VanillaCost={vehicle.Map.terrainGrid.TerrainAt(clickCell).pathCost} VanillaCalcCost={vehicle.Map.pathing.Normal.pathGrid.CalculatedCostAt(clickCell, true, IntVec3.Invalid)}");
				
				int num = GenRadial.NumCellsInRadius(2.9f);
				IntVec3 curLoc;
				for (int i = 0; i < num; i++)
				{
					curLoc = GenRadial.RadialPattern[i] + clickCell;
					if (GenGridVehicles.Standable(curLoc, vehicle, vehicle.Map))
					{
						if (curLoc == vehicle.Position || vehicle.beached)
						{
							__result = null;
							return false;
						}
						if (!VehicleReachabilityUtility.CanReachVehicle(vehicle, curLoc, PathEndMode.OnCell, Danger.Deadly, TraverseMode.ByPawn))
						{
							Debug.Message($"Cant Reach {curLoc} with {vehicle.Label}");
							__result = new FloatMenuOption("VehicleCannotMoveToCell".Translate(vehicle.LabelCap), null, MenuOptionPriority.Default, null, null, 0f, null, null);
							return false;
						}
						__result = new FloatMenuOption("GoHere".Translate(), delegate ()
						{
							Job job = new Job(JobDefOf.Goto, curLoc);
							bool isOnEdge = CellRect.WholeMap(vehicle.Map).IsOnEdge(clickCell, 3);
							bool exitCell = vehicle.Map.exitMapGrid.IsExitCell(clickCell);
							bool vehicleCellsOverlapExit = vehicle.InhabitedCellsProjected(clickCell, Rot8.Invalid).NotNullAndAny(cell => pawn.Map.exitMapGrid.IsExitCell(cell));
							Debug.Message($"Exit Map? CellOnEdge={isOnEdge} ExitCell={exitCell} VehicleCellsOverlap={vehicleCellsOverlapExit}");
							if (exitCell || vehicleCellsOverlapExit)
							{
								job.exitMapOnArrival = true;
							}
							else if (!vehicle.Map.IsPlayerHome && !vehicle.Map.exitMapGrid.MapUsesExitGrid && isOnEdge && vehicle.Map.Parent.GetComponent<FormCaravanComp>() is FormCaravanComp formCaravanComp
								 && MessagesRepeatAvoider.MessageShowAllowed("MessagePlayerTriedToLeaveMapViaExitGrid-" +
								vehicle.Map.uniqueID, 60f))
							{
								if (formCaravanComp.CanFormOrReformCaravanNow)
								{
									Messages.Message("MessagePlayerTriedToLeaveMapViaExitGrid_CanReform".Translate(), vehicle.Map.Parent, MessageTypeDefOf.RejectInput, false);
								}
								else
								{
									Messages.Message("MessagePlayerTriedToLeaveMapViaExitGrid_CantReform".Translate(), vehicle.Map.Parent, MessageTypeDefOf.RejectInput, false);
								}
							}
							if (vehicle.jobs.TryTakeOrderedJob(job, JobTag.Misc))
							{
								FleckMaker.Static(curLoc, vehicle.Map, FleckDefOf.FeedbackGoto, 1f);
							}
						}, MenuOptionPriority.GoHere, null, null, 0f, null, null)
						{
							autoTakeable = true,
							autoTakeablePriority = 10f
						};
						return false;
					}
				}
				__result = null;
				return false;
			}
			else
			{
				if (PathingHelper.VehicleImpassableInCell(pawn.Map, clickCell))
				{
					__result = new FloatMenuOption("CannotGoNoPath".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null);
					return false;
				}
			}
			return true;
		}

		public static IEnumerable<Toil> GotoToilsPassthrough(IEnumerable<Toil> __result, Job ___job, Pawn ___pawn)
		{
			bool first = true;
			foreach (Toil toil in __result)
			{
				if (first)
				{
					first = false;
					toil.AddPreTickAction(delegate ()
					{
						if (___pawn is VehiclePawn vehicle && ___job.exitMapOnArrival && vehicle.InhabitedCells(1).NotNullAndAny(cell => ___pawn.Map.exitMapGrid.IsExitCell(cell)))
						{
							PathingHelper.ExitMapForVehicle(vehicle, ___job);
						}
					});
				}
				yield return toil;
			}
		}

		/// <summary>
		/// Determine if next cell is walkable with final determination if vehicle is in cell or not
		/// </summary>
		/// <param name="__result"></param>
		/// <param name="___pawn"></param>
		/// <param name="nextCell"></param>
		public static void IsVehicleInNextCell(ref bool __result, Pawn ___pawn, Pawn_PathFollower __instance)
		{
			if (!__result)
			{
				//Peek 2 nodes ahead to avoid collision last second
				__result = (__instance.curPath.NodesLeftCount > 1 && PathingHelper.VehicleImpassableInCell(___pawn.Map, __instance.curPath.Peek(1))) || 
					(__instance.curPath.NodesLeftCount > 2 && PathingHelper.VehicleImpassableInCell(___pawn.Map, __instance.curPath.Peek(2)));
			}
		}

		/// <summary>
		/// StartPath hook to divert to vehicle related pather
		/// </summary>
		/// <param name="dest"></param>
		/// <param name="peMode"></param>
		/// <param name="___pawn"></param>
		public static bool StartVehiclePath(LocalTargetInfo dest, PathEndMode peMode, Pawn ___pawn)
		{
			if (___pawn is VehiclePawn vehicle)
			{
				vehicle.vPather.StartPath(dest, peMode);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Set cells in which vehicles reside as impassable to other Pawns
		/// </summary>
		/// <param name="instructions"></param>
		/// <param name="ilg"></param>
		public static IEnumerable<CodeInstruction> PathAroundVehicles(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
		{
			List<CodeInstruction> instructionList = instructions.ToList();
			for(int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];
				if (instruction.Calls(AccessTools.Method(typeof(CellIndices), nameof(CellIndices.CellToIndex), new Type[] { typeof(int), typeof(int) })))
				{
					Label label = ilg.DefineLabel();
					Label vehicleLabel = ilg.DefineLabel();

					yield return instruction; //CALLVIRT CELLTOINDEX
					instruction = instructionList[++i];
					yield return instruction; //STLOC.S 43
					instruction = instructionList[++i];

					yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
					yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(typeof(PathFinder), "map"));
					yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, 41);
					yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, 42);
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(PathingHelper), nameof(PathingHelper.VehicleImpassableInCell), new Type[] { typeof(Map), typeof(int), typeof(int) }));

					yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);
					yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_0);
					yield return new CodeInstruction(opcode: OpCodes.Br, vehicleLabel);

					for (int j = i; j < instructionList.Count; j++)
					{
						CodeInstruction instruction2 = instructionList[j];
						if (instruction2.opcode == OpCodes.Brfalse || instruction2.opcode == OpCodes.Brfalse_S)
						{
							instruction2.labels.Add(vehicleLabel);
							break;
						}
					}

					instruction.labels.Add(label);

				}
				yield return instruction;
			}
		}
		
		/// <summary>
		/// Modify CanReach result if position is claimed by Vehicle in PositionManager
		/// </summary>
		/// <param name="start"></param>
		/// <param name="dest"></param>
		/// <param name="peMode"></param>
		/// <param name="traverseParams"></param>
		/// <param name="__result"></param>
		public static bool CanReachVehiclePosition(IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParams, ref bool __result)
		{
			if (peMode == PathEndMode.OnCell && !(traverseParams.pawn is VehiclePawn) && traverseParams.pawn?.Map.GetCachedMapComponent<VehiclePositionManager>().ClaimedBy(dest.Cell) is VehiclePawn vehicle &&
				vehicle.VehicleDef.passability != Traversability.Standable)
			{
				__result = false;
				return false;
			}
			return true;
		}

		public static void ImpassableThroughVehicle(IntVec3 c, Map map, ref bool __result)
		{
			if (!__result && !PathingHelper.RegionWorking(map))
			{
				__result = PathingHelper.VehicleImpassableInCell(map, c);
			}
		}

		public static void WalkableThroughVehicle(IntVec3 loc, ref bool __result, Map ___map)
		{
			if (__result && !PathingHelper.RegionWorking(___map))
			{
				__result = !PathingHelper.VehicleImpassableInCell(___map, loc);
			}
		}

		public static void WalkableFastThroughVehicleIntVec3(IntVec3 loc, ref bool __result, Map ___map)
		{
			if (__result && !PathingHelper.RegionWorking(___map))
			{
				__result = !PathingHelper.VehicleImpassableInCell(___map, loc);
			}
		}

		public static void WalkableFastThroughVehicleInt2(int x, int z, ref bool __result, Map ___map)
		{
			if (__result && !PathingHelper.RegionWorking(___map))
			{
				__result = !PathingHelper.VehicleImpassableInCell(___map, new IntVec3(x, 0, z));
			}
		}

		public static void WalkableFastThroughVehicleInt(int index, ref bool __result, Map ___map)
		{
			if (__result && !PathingHelper.RegionWorking(___map))
			{
				__result = !PathingHelper.VehicleImpassableInCell(___map, ___map.cellIndices.IndexToCell(index));
			}
		}
		
		public static bool OccupiedRectVehicles(Thing t, ref CellRect __result)
		{
			if (t is VehiclePawn vehicle)
			{
				__result = vehicle.VehicleRect();
				return false;
			}
			return true;
		}

		public static void RecalculateAllPerceivedPathCostForVehicle(PathingContext ___normal)
		{
			PathingHelper.RecalculateAllPerceivedPathCosts(___normal.map);
		}

		public static void RecalculatePerceivedPathCostForVehicle(IntVec3 c, PathingContext ___normal)
		{
			PathingHelper.RecalculatePerceivedPathCostAt(c, ___normal.map);
		}

		public static void Notify_ThingAffectingVehicleRegionsSpawned(Thing b)
		{
			if (b.Spawned) //For some reason other mods love to patch the SpawnSetup method and despawn the object. Extra check is necessary
			{
				PathingHelper.ThingAffectingRegionsSpawned(b, b.Map);
			}
		}

		public static void Notify_ThingAffectingVehicleRegionsDespawned(Thing b)
		{
			if (b.Spawned) //For some reason other mods love to patch the SpawnSetup method and despawn the object. Extra check is necessary
			{
				PathingHelper.ThingAffectingRegionsDeSpawned(b, b.Map);
			}
		}

		public static void RegisterInVehicleRegions(Thing thing, Map map)
		{
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefs)
			{
				VehicleRegionListersUpdater.RegisterInRegions(thing, map, vehicleDef);
			}
		}

		public static void DeregisterInVehicleRegions(Thing thing, Map map)
		{
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefs)
			{
				VehicleRegionListersUpdater.DeregisterInRegions(thing, map, vehicleDef);
			}
		}
	}
}
