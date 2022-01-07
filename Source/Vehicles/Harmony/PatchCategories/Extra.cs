using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using SmashTools;
using Vehicles.UI;

namespace Vehicles
{
	internal class Extra : IPatchCategory
	{
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
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pawn), nameof(Pawn.Kill)), transpiler: new HarmonyMethod(typeof(Extra), nameof(CorpseInVehicle)));

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

        public static IEnumerable<CodeInstruction> CorpseInVehicle(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var list = instructions.ToList();
            var idx1 = list.FindIndex(ins => ins.opcode == OpCodes.Ldloc_S && ins.operand is LocalBuilder {LocalIndex: 12});
            var idx2 = list.FindIndex(idx1 + 1, ins => ins.opcode == OpCodes.Ldloc_S && ins.operand is LocalBuilder {LocalIndex: 12});
            var falseLabel = generator.DefineLabel();
            var trueLabel = (Label) list[idx2 - 1].operand;
            var vehicleHandler = generator.DeclareLocal(typeof(VehicleHandler));
			list[idx2].labels.Add(falseLabel);
			list.InsertRange(idx2, new []
            {
				new CodeInstruction(OpCodes.Ldloc, 11),
                new CodeInstruction(OpCodes.Brfalse, falseLabel),
				new CodeInstruction(OpCodes.Ldloc, 11),
				CodeInstruction.Call(typeof(ThingOwner), "get_Owner"),
                new CodeInstruction(OpCodes.Isinst, typeof(VehicleHandler)),
				new CodeInstruction(OpCodes.Stloc, vehicleHandler),
				new CodeInstruction(OpCodes.Ldloc, vehicleHandler),
				new CodeInstruction(OpCodes.Brfalse, falseLabel),
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldloc, 8),
				new CodeInstruction(OpCodes.Ldloc, 9),
				new CodeInstruction(OpCodes.Ldloc, 10),
				CodeInstruction.Call(typeof(Pawn), nameof(Pawn.MakeCorpse)),
				new CodeInstruction(OpCodes.Stloc, 21),
                new CodeInstruction(OpCodes.Ldloc, vehicleHandler),
				CodeInstruction.LoadField(typeof(VehicleHandler), nameof(VehicleHandler.vehiclePawn)), 
                CodeInstruction.LoadField(typeof(Pawn), nameof(Pawn.inventory)),
                CodeInstruction.LoadField(typeof(Pawn_InventoryTracker), nameof(Pawn_InventoryTracker.innerContainer)),
                new CodeInstruction(OpCodes.Ldloc, 21),
				new CodeInstruction(OpCodes.Ldc_I4_1),
				new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ThingOwner), nameof(ThingOwner.TryAdd), new []{typeof(Thing), typeof(bool)})),
				new CodeInstruction(OpCodes.Brtrue, trueLabel),
				new CodeInstruction(OpCodes.Ldloc, 21),
				new CodeInstruction(OpCodes.Ldc_I4_0),
				new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Thing), nameof(Thing.Destroy))),
				new CodeInstruction(OpCodes.Ldnull),
				new CodeInstruction(OpCodes.Stloc, 21),
				new CodeInstruction(OpCodes.Br, trueLabel)
            });
            return list;
        }
	}
}
