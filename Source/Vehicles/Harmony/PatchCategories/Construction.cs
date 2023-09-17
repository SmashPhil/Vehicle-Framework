using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;
using Vehicles.Achievements;

namespace Vehicles
{
	internal class Construction : IPatchCategory
	{
		public void PatchMethods()
		{
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Frame), nameof(Frame.CompleteConstruction)),
				prefix: new HarmonyMethod(typeof(Construction),
				nameof(CompleteConstructionVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(ListerBuildingsRepairable), nameof(ListerBuildingsRepairable.Notify_BuildingRepaired)),
				prefix: new HarmonyMethod(typeof(Construction),
				nameof(Notify_RepairedVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(GenSpawn), name: nameof(GenSpawn.Spawn), new Type[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool) }),
				prefix: new HarmonyMethod(typeof(Construction),
				nameof(RegisterThingSpawned)));
			VehicleHarmony.Patch(original: AccessTools.PropertyGetter(typeof(DesignationCategoryDef), nameof(DesignationCategoryDef.ResolvedAllowedDesignators)),
				postfix: new HarmonyMethod(typeof(Construction),
				nameof(RemoveDisabledVehicles)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Designator_Deconstruct), nameof(Designator.CanDesignateThing)),
				postfix: new HarmonyMethod(typeof(Construction),
				nameof(AllowDeconstructVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(GenLeaving), nameof(GenLeaving.DoLeavingsFor), new Type[] { typeof(Thing), typeof(Map), typeof(DestroyMode), typeof(List<Thing>) }),
				prefix: new HarmonyMethod(typeof(Construction),
				nameof(DoUnsupportedVehicleRefunds)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pawn), nameof(Pawn.Destroy)),
				transpiler: new HarmonyMethod(typeof(Construction),
				nameof(ValidDestroyModeForVehicles)));
		}

		public static bool CompleteConstructionVehicle(Pawn worker, Frame __instance)
		{
			if (__instance.def.entityDefToBuild is VehicleBuildDef def && def.thingToSpawn != null)
			{
				VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(def.thingToSpawn, worker.Faction);
				__instance.resourceContainer.ClearAndDestroyContents(DestroyMode.Vanish);
				Map map = __instance.Map;
				__instance.Destroy(DestroyMode.Vanish);

				if (def.soundBuilt != null)
				{
					def.soundBuilt.PlayOneShot(new TargetInfo(__instance.Position, map, false));
				}
				vehicle.SetFaction(worker.Faction);
				GenSpawn.Spawn(vehicle, __instance.Position, map, __instance.Rotation, WipeMode.FullRefund, false);
				worker.records.Increment(RecordDefOf.ThingsConstructed);
				
				if (!DebugSettings.godMode) //quick spawning for development
				{
					vehicle.Rename();
				}
				else
				{
					foreach (VehicleComp vehicleComp in vehicle.AllComps.Where(comp => comp is VehicleComp))
					{
						vehicleComp.SpawnedInGodMode();
					}
				}

				//Quality?
				//Art?
				//Tale RecordTale LongConstructionProject?
				AchievementsHelper.TriggerVehicleConstructionEvent(vehicle);
				return false;
			}
			return true;
		}

		public static bool Notify_RepairedVehicle(Building b, ListerBuildingsRepairable __instance)
		{
			if (b is VehicleBuilding building && b.def is VehicleBuildDef vehicleDef && vehicleDef.thingToSpawn != null)
			{
				if (b.HitPoints < b.MaxHitPoints)
					return true;

				Pawn vehicle;
				if(building.vehicle != null)
				{
					vehicle = building.vehicle;
					vehicle.health.Reset();
				}
				else
				{
					vehicle = PawnGenerator.GeneratePawn(vehicleDef.thingToSpawn.kindDef);
				}
				
				Map map = b.Map;
				IntVec3 position = b.Position;
				Rot4 rotation = b.Rotation;

				AccessTools.Method(typeof(ListerBuildingsRepairable), "UpdateBuilding").Invoke(__instance, new object[] { b });
				if (vehicleDef.soundBuilt != null)
				{
					vehicleDef.soundBuilt.PlayOneShot(new TargetInfo(position, map, false));
				}
				if(vehicle.Faction != Faction.OfPlayer)
				{
					vehicle.SetFaction(Faction.OfPlayer);
				}
				b.Destroy(DestroyMode.Vanish);
				vehicle.ForceSetStateToUnspawned();
				GenSpawn.Spawn(vehicle, position, map, rotation, WipeMode.FullRefund, false);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Catch All for vehicle related Things spawned in. Handles GodMode placing of vehicle buildings, corrects immovable spawn locations, and registers air defenses
		/// </summary>
		/// <param name="newThing"></param>
		/// <param name="loc"></param>
		/// <param name="map"></param>
		/// <param name="rot"></param>
		/// <param name="__result"></param>
		/// <param name="wipeMode"></param>
		/// <param name="respawningAfterLoad"></param>
		public static bool RegisterThingSpawned(Thing newThing, ref IntVec3 loc, Map map, Rot4 rot, ref Thing __result, WipeMode wipeMode, bool respawningAfterLoad)
		{
			if (newThing.def is VehicleBuildDef def)
			{
				if (!VehicleMod.settings.debug.debugSpawnVehicleBuildingGodMode && newThing.HitPoints == newThing.MaxHitPoints)
				{
					VehiclePawn vehiclePawn = VehicleSpawner.GenerateVehicle(def.thingToSpawn, newThing.Faction);
					
					if (def.soundBuilt != null)
					{
						def.soundBuilt.PlayOneShot(new TargetInfo(loc, map, false));
					}
					VehiclePawn vehicleSpawned = (VehiclePawn)GenSpawn.Spawn(vehiclePawn, loc, map, rot, WipeMode.FullRefund, false);

					if (!DebugSettings.godMode) //quick spawning for development
					{
						vehicleSpawned.Rename();
					}
					else
					{
						foreach (VehicleComp vehicleComp in vehicleSpawned.AllComps.Where(comp => comp is VehicleComp))
						{
							vehicleComp.SpawnedInGodMode();
						}
					}
					
					__result = vehicleSpawned;
					AchievementsHelper.TriggerVehicleConstructionEvent(vehicleSpawned);
					return false;
				}
			}
			else if (newThing is VehiclePawn vehicle)
			{
				bool standable = true;
				foreach (IntVec3 cell in vehicle.PawnOccupiedCells(loc, rot))
				{
					if (!cell.InBounds(map) || !GenGridVehicles.Walkable(cell, vehicle.VehicleDef, map))
					{
						standable = false;
						break;
					}
				}
				if (standable) return true;
				if (!CellFinder.TryFindRandomCellNear(loc, map, 20, (IntVec3 cell) =>
				{
					foreach (IntVec3 cell2 in vehicle.PawnOccupiedCells(cell, rot))
					{
						if (!GenGridVehicles.Walkable(cell2, vehicle.VehicleDef, map))
						{
							return false;
						}
					}
					return true;
				}, out IntVec3 newLoc, 100))
				{
					Log.Error($"Unable to find location to spawn {newThing.LabelShort} after 100 attempts. Aborting spawn.");
					return false;
				}
				loc = newLoc;
			}
			else if (newThing is Pawn pawn && !pawn.Dead)
			{
				try
				{
					VehiclePositionManager positionManager = map.GetCachedMapComponent<VehiclePositionManager>();
					if (positionManager.PositionClaimed(loc))
					{
						VehiclePawn inPlaceVehicle = positionManager.ClaimedBy(loc);
						CellRect occupiedRect = inPlaceVehicle.OccupiedRect().ExpandedBy(1);
						Rand.PushState();
						for (int i = 0; i < 3; i++)
						{
							IntVec3 newLoc = occupiedRect.EdgeCells.Where(c => GenGrid.InBounds(c, map) && GenGrid.Standable(c, map)).RandomElementWithFallback(inPlaceVehicle.Position);
							if (occupiedRect.EdgeCells.Contains(newLoc))
							{
								loc = newLoc;
								break;
							}
							occupiedRect = occupiedRect.ExpandedBy(1);
						}
						Rand.PopState();
					}
				}
				catch (Exception ex)
				{
					Log.Error($"Pawn {newThing.Label} could not be readjusted for spawn location. Exception={ex}");
				}
			}
			else
			{
				try
				{
					VehiclePositionManager positionManager = map.GetCachedMapComponent<VehiclePositionManager>();
					if (positionManager.PositionClaimed(loc))
					{
						VehiclePawn inPlaceVehicle = positionManager.ClaimedBy(loc);
						CellRect occupiedRect = inPlaceVehicle.OccupiedRect().ExpandedBy(1);
						for (int i = 0; i < 3; i++)
						{
							IntVec3 newLoc = IntVec3.Invalid;
							//foreach (IntVec3 cell in occupiedRect.EdgeCells.Where(c => GenGrid.InBounds(c, map)))
							//{
							//	Thing thing2 = thing;
							//	bool flag = false;
							//	if (thing.stackCount > thing.def.stackLimit)
							//	{
							//		thing = thing.SplitOff(thing.def.stackLimit);
							//		flag = true;
							//	}
							//	if (thing.def.stackLimit > 1)
							//	{
							//		List<Thing> thingList = loc.GetThingList(map);
							//		int i = 0;
							//		while (i < thingList.Count)
							//		{
							//			Thing thing3 = thingList[i];
							//			if (thing3.CanStackWith(thing))
							//			{
							//				int stackCount = thing.stackCount;
							//				if (thing3.TryAbsorbStack(thing, true))
							//				{
							//					resultingThing = thing3;
							//					if (placedAction != null)
							//					{
							//						placedAction(thing3, stackCount);
							//					}
							//					return !flag;
							//				}
							//				resultingThing = null;
							//				if (placedAction != null && stackCount != thing.stackCount)
							//				{
							//					placedAction(thing3, stackCount - thing.stackCount);
							//				}
							//				if (thing2 != thing)
							//				{
							//					thing2.TryAbsorbStack(thing, false);
							//				}
							//				return false;
							//			}
							//			else
							//			{
							//				i++;
							//			}
							//		}
							//	}
							//}
							//if (newLoc.IsValid)
							//{
							//	loc = newLoc;
							//	break;
							//}
							occupiedRect = occupiedRect.ExpandedBy(1);
						}
					}
				}
				catch (Exception ex)
				{
					Log.Error($"Thing {newThing.Label} could not be readjusted for spawn location. Exception={ex}");
				}
			}
			return true;
		}

		public static IEnumerable<Designator> RemoveDisabledVehicles(IEnumerable<Designator> __result)
		{
			foreach (Designator designator in __result)
			{
				if (designator is Designator_Build buildDesignator)
				{
					if (AccessTools.Field(typeof(Designator_Build), "entDef").GetValue(buildDesignator) is VehicleBuildDef buildDef && buildDef.thingToSpawn is VehicleDef vehicleDef)
					{
						VehicleEnabledFor enabled = SettingsCache.TryGetValue(vehicleDef, typeof(VehicleDef), nameof(VehicleDef.enabled), vehicleDef.enabled);
						if (enabled == VehicleEnabledFor.None || enabled == VehicleEnabledFor.Raiders)
						{
							if (!VehicleMod.settings.main.hideDisabledVehicles)
							{
								designator.Disable("VF_GizmoDisabledTooltip".Translate());
							}
							else
							{
								continue;
							}
						}
					}
				}
				yield return designator;
			}
		}

		public static void AllowDeconstructVehicle(Designator_Deconstruct __instance, Thing t, ref AcceptanceReport __result)
		{
			if (t is VehiclePawn vehicle && vehicle.DeconstructibleBy(Faction.OfPlayer))
			{
				if (__instance.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
				{
					__result = false;
				}
				else if (__instance.Map.designationManager.DesignationOn(t, DesignationDefOf.Uninstall) != null)
				{
					__result = false;
				}
				else
				{
					__result = true;
				}
			}
		}

		public static bool DoUnsupportedVehicleRefunds(Thing diedThing, Map map, DestroyMode mode, List<Thing> listOfLeavingsOut = null)
		{
			if (diedThing is VehiclePawn vehicle)
			{
				vehicle.RefundMaterials(map, mode, listOfLeavingsOut);
				return false;
			}
			return true;
		}

		public static IEnumerable<CodeInstruction> ValidDestroyModeForVehicles(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if ((instruction.opcode == OpCodes.Brfalse || instruction.opcode == OpCodes.Brfalse_S) && !instructionList.OutOfBounds(i - 1) && instructionList[i - 1].opcode == OpCodes.Ldarg_1)
				{
					List<Label> labels = instruction.labels;
					yield return instruction;
					instruction = instructionList[++i];

					yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
					yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(Construction), nameof(VehicleValidDestroyMode)));
					yield return new CodeInstruction(opcode: OpCodes.Brtrue, operand: labels.FirstOrDefault());
				}

				yield return instruction;
			}
		}

		public static bool VehicleValidDestroyMode(Pawn pawn, DestroyMode destroyMode)
		{
			return pawn is VehiclePawn && destroyMode != DestroyMode.QuestLogic && destroyMode != DestroyMode.FailConstruction && destroyMode != DestroyMode.WillReplace;
		}
	}
}
