using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;
using Vehicles.AI;
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

				vehicle.Rename();
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
				if(building.vehicleReference != null)
				{
					vehicle = building.vehicleReference;
					vehicle.health.Reset();
				}
				else
				{
					vehicle = PawnGenerator.GeneratePawn(vehicleDef.thingToSpawn.VehicleKindDef);
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
		/// <returns></returns>
		public static bool RegisterThingSpawned(Thing newThing, ref IntVec3 loc, Map map, Rot4 rot, ref Thing __result, WipeMode wipeMode, bool respawningAfterLoad)
		{
			if (newThing.def is VehicleBuildDef def)
			{
				if (!VehicleMod.settings.debug.debugSpawnVehicleBuildingGodMode && newThing.HitPoints == newThing.MaxHitPoints)
				{
					VehiclePawn vehiclePawn = VehicleSpawner.GenerateVehicle(def.thingToSpawn, newThing.Faction);// (VehiclePawn)PawnGenerator.GeneratePawn(def.thingToSpawn);
					
					if (def.soundBuilt != null)
					{
						def.soundBuilt.PlayOneShot(new TargetInfo(loc, map, false));
					}
					VehiclePawn vehicleSpawned = (VehiclePawn)GenSpawn.Spawn(vehiclePawn, loc, map, rot, WipeMode.FullRefund, false);
					vehicleSpawned.Rename();
					__result = vehicleSpawned;
					AchievementsHelper.TriggerVehicleConstructionEvent(vehicleSpawned);
					return false;
				}
			}
			else if (newThing is VehiclePawn vehicle)
			{
				bool standable = true;
				foreach (IntVec3 c in vehicle.PawnOccupiedCells(loc, rot))
				{
					if (!c.InBounds(map) || GenGridVehicles.Impassable(c, map, vehicle.VehicleDef))
					{
						standable = false;
						break;
					}
				}
				bool validator(IntVec3 c)
				{
					foreach (IntVec3 c2 in vehicle.PawnOccupiedCells(c, rot))
					{
						if (GenGridVehicles.Impassable(c, map, vehicle.VehicleDef))
						{
							return false;
						}
					}
					return true;
				}
				if (standable) return true;
				if (!CellFinder.TryFindRandomCellNear(loc, map, 20, validator, out IntVec3 newLoc, 100))
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
					var positionManager = map.GetCachedMapComponent<VehiclePositionManager>();
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
					Log.Error($"Pawn {newThing.Label} could not be readjusted for spawn location. Exception={ex.Message}");
				}
			}
			else
			{
				try
				{
					var positionManager = map.GetCachedMapComponent<VehiclePositionManager>();
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
					Log.Error($"Thing {newThing.Label} could not be readjusted for spawn location. Exception={ex.Message}");
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
						bool enabled = vehicleDef.TryGetValue(typeof(VehicleDef), nameof(VehicleDef.enabled), vehicleDef.enabled);
						if (!enabled)
						{
							if (VehicleMod.settings.main.showDisabledVehicles)
							{
								designator.Disable("VehicleGizmoDisabledTooltip".Translate());
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
	}
}
