using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	internal class Extra : IPatchCategory
	{
		public const float IconBarDim = 30;

		public void PatchMethods()
		{
			VehicleHarmony.Patch(original: AccessTools.Property(typeof(MapPawns), nameof(MapPawns.FreeColonistsSpawnedOrInPlayerEjectablePodsCount)).GetGetMethod(), prefix: null,
				postfix: new HarmonyMethod(typeof(Extra),
				nameof(FreeColonistsInVehiclesTransport)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(MapPawns), nameof(MapPawns.FreeHumanlikesSpawnedOfFaction)), prefix: null,
				postfix: new HarmonyMethod(typeof(Extra),
				nameof(FreeHumanlikesSpawnedInVehicles)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(MapPawns), nameof(MapPawns.FreeHumanlikesOfFaction)), prefix: null,
				postfix: new HarmonyMethod(typeof(Extra),
				nameof(FreeHumanlikesInVehicles)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Selector), "HandleMapClicks"),
				prefix: new HarmonyMethod(typeof(Extra),
				nameof(MultiSelectFloatMenu)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(MentalState_Manhunter), nameof(MentalState_Manhunter.ForceHostileTo), new Type[] { typeof(Thing) }), prefix: null,
				postfix: new HarmonyMethod(typeof(Extra),
				nameof(ManhunterDontAttackVehicles)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Projectile_Explosive), "Impact"),
				prefix: new HarmonyMethod(typeof(Extra),
				nameof(ShellsImpactWater)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(WindowStack), nameof(WindowStack.Notify_ClickedInsideWindow)),
				prefix: new HarmonyMethod(typeof(Extra),
				nameof(HandleSingleWindowDialogs)));
			VehicleHarmony.Patch(original: AccessTools.PropertyGetter(typeof(TickManager), nameof(TickManager.Paused)),
				postfix: new HarmonyMethod(typeof(Extra),
				nameof(PausedFromVehicles)));
			VehicleHarmony.Patch(original: AccessTools.PropertyGetter(typeof(TickManager), nameof(TickManager.CurTimeSpeed)),
				postfix: new HarmonyMethod(typeof(Extra),
				nameof(ForcePauseFromVehicles)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Dialog_ManageAreas), "DoAreaRow"),
				transpiler: new HarmonyMethod(typeof(Extra),
				nameof(VehicleAreaRowTranspiler)));

			VehicleHarmony.Patch(original: AccessTools.Method(typeof(PawnCapacitiesHandler), nameof(PawnCapacitiesHandler.Notify_CapacityLevelsDirty)),
				prefix: new HarmonyMethod(typeof(Extra),
				nameof(RecheckVehicleHandlerCapacities)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pawn), nameof(Pawn.Kill)),
                prefix: new HarmonyMethod(typeof(Extra), 
				nameof(MoveOnDeath)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(PawnUtility), nameof(PawnUtility.ShouldSendNotificationAbout)),
                postfix: new HarmonyMethod(typeof(Extra), 
				nameof(SendNotificationsVehicle)));

		}

		public static void FreeColonistsInVehiclesTransport(ref int __result, List<Pawn> ___pawnsSpawned)
		{
			List<VehiclePawn> vehicles = ___pawnsSpawned.Where(x => x is VehiclePawn vehicle && x.Faction == Faction.OfPlayer).Cast<VehiclePawn>().ToList();
			
			foreach(VehiclePawn vehicle in vehicles)
			{
				if(vehicle.AllPawnsAboard.NotNullAndAny(x => !x.Dead))
					__result += vehicle.AllPawnsAboard.Count;
			}
		}

		public static void FreeHumanlikesSpawnedInVehicles(Faction faction, ref List<Pawn> __result, MapPawns __instance)
		{
			List<Pawn> innerPawns = __instance.SpawnedPawnsInFaction(faction).Where(p => p is VehiclePawn).SelectMany(v => (v as VehiclePawn).AllPawnsAboard).ToList();
			__result.AddRange(innerPawns);
		}

		public static void FreeHumanlikesInVehicles(Faction faction, ref List<Pawn> __result, MapPawns __instance)
		{
			List<Pawn> innerPawns = __instance.AllPawns.Where(p => p.Faction == faction && p is VehiclePawn).SelectMany(v => (v as VehiclePawn).AllPawnsAboard).ToList();
			__result.AddRange(innerPawns);
		}

		public static bool MultiSelectFloatMenu(List<object> ___selected)
		{
			if(Event.current.type == EventType.MouseDown)
			{
				if(Event.current.button == 1 && ___selected.Count > 0)
				{
					if(___selected.Count > 1)
					{
						return !SelectionHelper.MultiSelectClicker(___selected);
					}
				}
			}
			return true;
		}

		public static void ManhunterDontAttackVehicles(Thing t, ref bool __result)
		{
			if(__result is true && t is VehiclePawn vehicle && !SettingsCache.TryGetValue(vehicle.VehicleDef, typeof(VehicleProperties), "manhunterTargetsVehicle", vehicle.VehicleDef.properties.manhunterTargetsVehicle))
			{
				__result = false;
			}
		}

		//REDO
		/// <summary>
		/// Shells impacting water now have reduced radius of effect and different sound
		/// </summary>
		/// <param name="hitThing"></param>
		/// <param name="__instance"></param>
		public static bool ShellsImpactWater(Thing hitThing, ref Projectile __instance)
		{
			Map map = __instance.Map;
			TerrainDef terrainImpact = map.terrainGrid.TerrainAt(__instance.Position);
			if(__instance.def.projectile.explosionDelay == 0 && terrainImpact.IsWater && !__instance.Position.GetThingList(__instance.Map).NotNullAndAny(x => x is VehiclePawn vehicle))
			{
				DamageHelper.Explode(__instance);
				return false;
			}
			return true;
		}

		public static void HandleSingleWindowDialogs(Window window, WindowStack __instance)
		{
			if (Event.current.type == EventType.MouseDown)
			{
				if (window is null || (!window.GetType().IsAssignableFrom(typeof(SingleWindow)) && (__instance.GetWindowAt(Verse.UI.GUIToScreenPoint(Event.current.mousePosition)) != SingleWindow.CurrentlyOpenedWindow)))
				{
					if (SingleWindow.CurrentlyOpenedWindow != null && SingleWindow.CurrentlyOpenedWindow.closeOnAnyClickOutside)
					{
						Find.WindowStack.TryRemove(SingleWindow.CurrentlyOpenedWindow);
					}
				}
			}
		}

		public static void PausedFromVehicles(ref bool __result)
		{
			if (LandingTargeter.Instance.ForcedTargeting || StrafeTargeter.Instance.ForcedTargeting)
			{
				__result = true;
			}
		}

		public static void ForcePauseFromVehicles(ref TimeSpeed __result)
		{
			if (LandingTargeter.Instance.ForcedTargeting || StrafeTargeter.Instance.ForcedTargeting)
			{
				__result = TimeSpeed.Paused;
			}
		}

		public static IEnumerable<CodeInstruction> VehicleAreaRowTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if (instruction.Calls(AccessTools.Method(typeof(WidgetRow), nameof(WidgetRow.Icon))))
				{
					yield return instruction; //WidgetRow.Icon
					i += 2; //Skip Pop
					instruction = instructionList[i];
					yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(Extra), nameof(ChangeAreaColor)));
				}
				/*
				if (instruction.opcode == OpCodes.Stloc_1)
				{
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.PropertyGetter(typeof(Extra), nameof(VehiclesButtonWidth)));
					yield return new CodeInstruction(opcode: OpCodes.Add);
				}
				if (instruction.Calls(AccessTools.Method(typeof(WidgetRow), nameof(WidgetRow.Label))))
				{
					yield return instruction; //Call WidgetRow.Label
					instruction = instructionList[++i];
					yield return instruction; //Pop
					instruction = instructionList[++i];

					yield return new CodeInstruction(opcode: OpCodes.Ldloc_0);
					yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(Extra), nameof(ConfigureVehicleArea)));
				}
				*/
				yield return instruction;
			}
		}

		private static float VehiclesButtonWidth => -(Text.CalcSize("VF_Vehicles".Translate()).x + 20);

		private static void ChangeAreaColor(Rect rect, Area area)
		{
			if (area is Area_Allowed && Widgets.ButtonInvisible(rect))
			{
				Find.WindowStack.Add(new Dialog_ColorWheel(area.Color, delegate (Color color)
				{
					AccessTools.Field(typeof(Area_Allowed), "colorInt").SetValue(area, color);
					AccessTools.Field(typeof(Area), "colorTextureInt").SetValue(area, null);
					AccessTools.Field(typeof(Area), "drawer").SetValue(area, null);
				}));
			}
		}

		private static void ConfigureVehicleArea(WidgetRow widgetRow, Area area)
		{
			if (widgetRow.ButtonText("VF_Vehicles".Translate(), "VehiclesConfigurationAreaTooltip".Translate()))
			{
				if (Find.CurrentMap is null)
				{
					Messages.Message("Map must be loaded in order to configure areas for vehicles.", MessageTypeDefOf.RejectInput);
					return;
				}
				Find.WindowStack.Add(new Dialog_ConfigureVehicleAreas(Find.CurrentMap, area));
			}
		}

		public static void RecheckVehicleHandlerCapacities(Pawn ___pawn)
		{
			if (___pawn.GetVehicle() is VehiclePawn vehicle)
			{
				//Null check for initial pawn capacities dirty caching when VehiclePawn has not yet called SpawnSetup
				vehicle.EventRegistry?[VehicleEventDefOf.PawnCapacitiesDirty].ExecuteEvents();
			}
		}

		public static void MoveOnDeath(Pawn __instance)
        {
            if (__instance.IsInVehicle())
            {
                VehiclePawn vehicle = __instance.GetVehicle();
				vehicle.EventRegistry[VehicleEventDefOf.PawnKilled].ExecuteEvents();
                vehicle.AddOrTransfer(__instance);
				if (Find.World.worldPawns.Contains(__instance))
				{
					Find.WorldPawns.RemovePawn(__instance);
				}
            }
        }

        public static void SendNotificationsVehicle(Pawn p, ref bool __result)
        {
            if (!__result && p.Faction is {IsPlayer: true} && (p.ParentHolder is VehicleHandler || p.ParentHolder is Pawn_InventoryTracker {pawn: VehiclePawn { }}))
            {
                __result = true;
            }
        }

		[DebugAction(VehicleHarmony.VehiclesLabel, "Kill Someone", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void KillPawn()
        {
			Find.CurrentMap.mapPawns.FreeColonists.RandomElement().Kill(null);
        }
	}
}
