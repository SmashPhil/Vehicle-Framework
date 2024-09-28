using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using SmashTools;
using SmashTools.Performance;

namespace Vehicles
{
	internal class VehiclePathing : IPatchCategory
	{
		private static readonly HashSet<IntVec3> hitboxUpdateCells = new HashSet<IntVec3>();
		
		public void PatchMethods()
		{
			//TODO - Implement in 1.5 with more testing and drag-to-rotate
			//	   - Needs another patch on RCellFinder.BestOrderedGotoDestNear so it recomputes best position for vehicle
			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(FloatMenuMakerMap), "CanTakeOrder"),
			//	postfix: new HarmonyMethod(typeof(VehiclePathing),
			//	nameof(VehiclesCanTakeOrders)));

			VehicleHarmony.Patch(original: AccessTools.Method(typeof(FloatMenuMakerMap), "GotoLocationOption"),
				prefix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(GotoLocationVehicles)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(JobDriver_Goto), "MakeNewToils"),
				postfix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(GotoToilsPassthrough)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.IsCurrentJobPlayerInterruptible)),
				prefix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(JobInterruptibleForVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pawn_PathFollower), "NeedNewPath"),
				postfix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(IsVehicleInNextCell)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.StartPath)),
				prefix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(StartVehiclePath)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(GenAdj), nameof(GenAdj.AdjacentTo8WayOrInside), parameters: new Type[] { typeof(IntVec3), typeof(Thing) }),
				prefix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(AdjacentTo8WayOrInsideVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(GenAdj), nameof(GenAdj.OccupiedRect), parameters: new Type[] { typeof(Thing) }),
				prefix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(OccupiedRectVehicles)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pathing), nameof(Pathing.RecalculateAllPerceivedPathCosts)),
				postfix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(RecalculateAllPerceivedPathCostForVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pathing), nameof(Pathing.RecalculatePerceivedPathCostAt)),
				postfix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(RecalculatePerceivedPathCostForVehicle)));

			VehicleHarmony.Patch(original: AccessTools.Method(typeof(TerrainGrid), "DoTerrainChangedEffects"),
				prefix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(SetTerrainAndUpdateVehiclePathCosts)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Thing), nameof(Thing.DeSpawn)),
				transpiler: new HarmonyMethod(typeof(VehiclePathing),
				nameof(DeSpawnAndUpdateVehicleRegionsTranspiler)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Thing), nameof(Thing.SpawnSetup)),
				transpiler: new HarmonyMethod(typeof(VehiclePathing),
				nameof(SpawnAndUpdateVehicleRegionsTranspiler)));
			VehicleHarmony.Patch(original: AccessTools.PropertySetter(typeof(Thing), nameof(Thing.Position)),
				postfix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(SetPositionAndUpdateVehicleRegions)));
			VehicleHarmony.Patch(original: AccessTools.PropertySetter(typeof(Thing), nameof(Thing.Rotation)),
				prefix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(SetRotationAndUpdateVehicleRegionsClipping)),
				postfix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(SetRotationAndUpdateVehicleRegions)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(ThingGrid), nameof(ThingGrid.Register)),
				prefix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(MonitorThingGridRegisterStart)),
				finalizer: new HarmonyMethod(typeof(VehiclePathing),
				nameof(MonitorThingGridRegisterEnd)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(ThingGrid), nameof(ThingGrid.Deregister)),
				prefix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(MonitorThingGridDeregisterStart)),
				finalizer: new HarmonyMethod(typeof(VehiclePathing),
				nameof(MonitorThingGridDeregisterEnd)));

			VehicleHarmony.Patch(original: AccessTools.Method(typeof(GenStep_RocksFromGrid), nameof(GenStep_RocksFromGrid.Generate)),
				prefix: new HarmonyMethod(typeof(VehiclePathing),
				nameof(DisableRegionUpdatingRockGen)));
		}

		private static void VehiclesCanTakeOrders(Pawn pawn, ref bool __result)
		{
			if (!__result && pawn is VehiclePawn)
			{
				__result = true;
			}
		}

		/// <summary>
		/// Intercepts FloatMenuMakerMap call to restrict by size and call through to custom water based pathing requirements
		/// </summary>
		/// <param name="clickCell"></param>
		/// <param name="pawn"></param>
		/// <param name="__result"></param>
		private static bool GotoLocationVehicles(IntVec3 clickCell, Pawn pawn, ref FloatMenuOption __result, bool suppressAutoTakeableGoto)
		{
			if (pawn is VehiclePawn vehicle)
			{
				__result = null;
				if (suppressAutoTakeableGoto)
				{
					return false;
				}
				if (vehicle.Faction != Faction.OfPlayer || !vehicle.CanMoveFinal)
				{
					return false;
				}
				if (vehicle.Deploying || (vehicle.CompVehicleTurrets != null && vehicle.CompVehicleTurrets.Deployed))
				{
					Messages.Message("VF_VehicleImmobileDeployed".Translate(vehicle), MessageTypeDefOf.RejectInput);
					return false;
				}
				
				if (vehicle.CompFueledTravel != null && vehicle.CompFueledTravel.EmptyTank)
				{
					Messages.Message("VF_OutOfFuel".Translate(vehicle), MessageTypeDefOf.RejectInput);
					return false;
				}

				VehicleMapping mapping = MapComponentCache<VehicleMapping>.GetComponent(vehicle.Map);
				
				if (PathingHelper.TryFindNearestStandableCell(vehicle, clickCell, out IntVec3 result))
				{
					__result = new FloatMenuOption("GoHere".Translate(), delegate ()
					{
						VehicleOrientationController.StartOrienting(vehicle, result, clickCell);
					}, MenuOptionPriority.GoHere, null, null, 0f, null, null)
					{
						autoTakeable = true,
						autoTakeablePriority = 10f
					};
				}
				if (!VehicleReachabilityUtility.CanReachVehicle(vehicle, clickCell, PathEndMode.OnCell, Danger.Deadly, TraverseMode.ByPawn))
				{
					__result = new FloatMenuOption("VF_CannotMoveToCell".Translate(vehicle.LabelCap), null);
				}
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

		private static IEnumerable<Toil> GotoToilsPassthrough(IEnumerable<Toil> __result, Job ___job, Pawn ___pawn)
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
		/// Bypass vanilla check for now, since it forces on-fire pawns to not be able to interrupt jobs which obviously shouldn't apply to vehicles.
		/// </summary>
		private static bool JobInterruptibleForVehicle(Pawn_JobTracker __instance, Pawn ___pawn, ref bool __result)
		{
			if (___pawn is VehiclePawn)
			{
				__result = true;
				if (__instance.curJob != null)
				{
					if (!__instance.curJob.def.playerInterruptible)
					{
						__result = false;
					}
					else if (__instance.curDriver != null && !__instance.curDriver.PlayerInterruptable)
					{
						__result = false;
					}
				}
				return false;
			}
			return true;
		}

		/// <summary>
		/// Determine if next cell is walkable with final determination if vehicle is in cell or not
		/// </summary>
		/// <param name="__result"></param>
		/// <param name="___pawn"></param>
		/// <param name="nextCell"></param>
		private static void IsVehicleInNextCell(ref bool __result, Pawn ___pawn, Pawn_PathFollower __instance)
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
		private static bool StartVehiclePath(LocalTargetInfo dest, PathEndMode peMode, Pawn ___pawn)
		{
			if (___pawn is VehiclePawn vehicle)
			{
				vehicle.vehiclePather.StartPath(dest, peMode);
				return false;
			}
			return true;
		}

		private static bool AdjacentTo8WayOrInsideVehicle(IntVec3 root, Thing t, ref bool __result)
		{
			if (t is VehiclePawn vehicle)
			{
				IntVec2 size = vehicle.def.size;
				Rot4 rot = vehicle.Rotation;
				Ext_Vehicles.AdjustForVehicleOccupiedRect(ref size, ref rot);
				__result = root.AdjacentTo8WayOrInside(vehicle.Position, rot, size);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Set cells in which vehicles reside as impassable to other Pawns
		/// </summary>
		/// <param name="instructions"></param>
		/// <param name="ilg"></param>
		private static IEnumerable<CodeInstruction> PathAroundVehicles(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
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
		private static bool CanReachVehiclePosition(IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParams, ref bool __result)
		{
			if (peMode == PathEndMode.OnCell && !(traverseParams.pawn is VehiclePawn) && traverseParams.pawn?.Map.GetCachedMapComponent<VehiclePositionManager>().ClaimedBy(dest.Cell) is VehiclePawn vehicle &&
				vehicle.VehicleDef.passability != Traversability.Standable)
			{
				__result = false;
				return false;
			}
			return true;
		}

		private static void ImpassableThroughVehicle(IntVec3 c, Map map, ref bool __result)
		{
			if (!__result && !PathingHelper.RegionWorking(map))
			{
				__result = PathingHelper.VehicleImpassableInCell(map, c);
			}
		}

		private static void WalkableThroughVehicle(IntVec3 loc, ref bool __result, Map ___map)
		{
			if (__result && !PathingHelper.RegionWorking(___map))
			{
				__result = !PathingHelper.VehicleImpassableInCell(___map, loc);
			}
		}

		private static void WalkableFastThroughVehicleIntVec3(IntVec3 loc, ref bool __result, Map ___map)
		{
			if (__result && !PathingHelper.RegionWorking(___map))
			{
				__result = !PathingHelper.VehicleImpassableInCell(___map, loc);
			}
		}

		private static void WalkableFastThroughVehicleInt2(int x, int z, ref bool __result, Map ___map)
		{
			if (__result && !PathingHelper.RegionWorking(___map))
			{
				__result = !PathingHelper.VehicleImpassableInCell(___map, new IntVec3(x, 0, z));
			}
		}

		private static void WalkableFastThroughVehicleInt(int index, ref bool __result, Map ___map)
		{
			if (__result && !PathingHelper.RegionWorking(___map))
			{
				__result = !PathingHelper.VehicleImpassableInCell(___map, ___map.cellIndices.IndexToCell(index));
			}
		}

		private static bool OccupiedRectVehicles(Thing t, ref CellRect __result)
		{
			if (t is VehiclePawn vehicle)
			{
				__result = vehicle.VehicleRect();
				return false;
			}
			return true;
		}

		private static void RecalculateAllPerceivedPathCostForVehicle(PathingContext ___normal)
		{
			PathingHelper.RecalculateAllPerceivedPathCosts(___normal.map);
		}

		private static void RecalculatePerceivedPathCostForVehicle(IntVec3 c, PathingContext ___normal)
		{
			PathingHelper.RecalculatePerceivedPathCostAt(c, ___normal.map);
		}

		/// <summary>
		/// Pass <paramref name="c"/> by reference to allow Harmony to skip prefix method when MapPreview skips it during preview generation
		/// </summary>
		/// <param name="c"></param>
		/// <param name="___map"></param>
		private static void SetTerrainAndUpdateVehiclePathCosts(ref IntVec3 c, Map ___map)
		{
			if (Current.ProgramState == ProgramState.Playing)
			{
				PathingHelper.RecalculatePerceivedPathCostAt(c, ___map);
			}
		}

		private static IEnumerable<CodeInstruction> DeSpawnAndUpdateVehicleRegionsTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			MethodInfo coverGridDeregisterMethod = AccessTools.Method(typeof(TickManager), nameof(TickManager.DeRegisterAllTickabilityFor));
			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if (instruction.Calls(coverGridDeregisterMethod))
				{
					yield return instruction;
					instruction = instructionList[++i];

					yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
					yield return new CodeInstruction(opcode: OpCodes.Ldloc_0);
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(VehiclePathing), nameof(VehiclePathing.DeSpawnAndNotifyVehicleRegions)));
				}

				yield return instruction;
			}
		}

		private static IEnumerable<CodeInstruction> SpawnAndUpdateVehicleRegionsTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			MethodInfo coverGridDeregisterMethod = AccessTools.Method(typeof(CoverGrid), nameof(CoverGrid.Register));
			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if (instruction.Calls(coverGridDeregisterMethod))
				{
					yield return instruction;
					instruction = instructionList[++i];

					yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
					yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(VehiclePathing), nameof(VehiclePathing.SpawnAndNotifyVehicleRegions)));
				}

				yield return instruction;
			}
		}

		private static void SetPositionAndUpdateVehicleRegions(Thing __instance)
		{
			if (__instance.Spawned)
			{
				if (__instance is VehiclePawn vehicle)
				{
					vehicle.Map.GetCachedMapComponent<VehiclePositionManager>().ClaimPosition(vehicle);
				}
				PathingHelper.ThingAffectingRegionsOrientationChanged(__instance, __instance.Map);
			}
		}

		private static bool SetRotationAndUpdateVehicleRegionsClipping(Thing __instance, Rot4 value)
		{
			if (__instance is VehiclePawn vehicle && vehicle.Spawned)
			{
				if (!vehicle.OccupiedRectShifted(IntVec2.Zero, value).InBounds(vehicle.Map))
				{
					return false;
				}
				hitboxUpdateCells.Clear();
				hitboxUpdateCells.AddRange(vehicle.OccupiedRectShifted(IntVec2.Zero, Rot4.East));
				hitboxUpdateCells.AddRange(vehicle.OccupiedRectShifted(IntVec2.Zero, Rot4.North));
				
				foreach (IntVec3 cell in hitboxUpdateCells)
				{
					vehicle.Map.pathing.RecalculatePerceivedPathCostAt(cell);
				}
				
				hitboxUpdateCells.Clear();

				vehicle.Map.coverGrid.DeRegister(vehicle);
			}
			return true;
		}

		private static void SetRotationAndUpdateVehicleRegions(Thing __instance)
		{
			if (__instance is VehiclePawn vehicle && vehicle.Spawned)
			{
				vehicle.Map.coverGrid.Register(vehicle);
				vehicle.Map.GetCachedMapComponent<VehiclePositionManager>().ClaimPosition(vehicle);
			}
			if (__instance.Spawned && (__instance.def.size.x != 1 || __instance.def.size.z != 1))
			{
				PathingHelper.ThingAffectingRegionsOrientationChanged(__instance, __instance.Map);
			}
		}

		private static void MonitorThingGridRegisterStart(ThingGrid __instance)
		{
			Monitor.Enter(__instance);
		}

		private static void MonitorThingGridRegisterEnd(ThingGrid __instance)
		{
			Monitor.Exit(__instance);
		}

		private static void MonitorThingGridDeregisterStart(ThingGrid __instance)
		{
			Monitor.Enter(__instance);
		}

		private static void MonitorThingGridDeregisterEnd(ThingGrid __instance)
		{
			Monitor.Exit(__instance);
		}

		private static void DisableRegionUpdatingRockGen(Map map)
		{
			if (!map.TileInfo.WaterCovered)
			{
				PathingHelper.DisableAllRegionUpdaters(map);
			}
		}

		/* ---- Helper Methods related to patches ---- */

		private static void SpawnAndNotifyVehicleRegions(Thing thing, Map map)
		{
			PathingHelper.ThingAffectingRegionsStateChange(thing, map, true);
		}

		private static void DeSpawnAndNotifyVehicleRegions(Thing thing, Map map)
		{
			PathingHelper.ThingAffectingRegionsStateChange(thing, map, false);
		}
	}
}
