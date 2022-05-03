using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using RimWorld.Planet;
using OpCodes = System.Reflection.Emit.OpCodes;
using SmashTools;

namespace Vehicles
{
	internal class MapHandling : IPatchCategory
	{
		public void PatchMethods()
		{
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(BeachMaker), nameof(BeachMaker.Init)),
				transpiler: new HarmonyMethod(typeof(MapHandling),
				nameof(BeachMakerTranspiler)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(TileFinder), nameof(TileFinder.RandomSettlementTileFor)),
				transpiler: new HarmonyMethod(typeof(MapHandling),
				nameof(PushSettlementToCoastTranspiler)));
			VehicleHarmony.Patch(original: AccessTools.Property(typeof(MapPawns), nameof(MapPawns.AnyPawnBlockingMapRemoval)).GetGetMethod(),
				postfix: new HarmonyMethod(typeof(MapHandling),
				nameof(AnyVehicleBlockingMapRemoval)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(MapDeiniter), "NotifyEverythingWhichUsesMapReference"),
				postfix: new HarmonyMethod(typeof(MapHandling),
				nameof(NotifyEverythingWhichUsesMapReferencePost)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(MapInterface), nameof(MapInterface.MapInterfaceUpdate)),
				postfix: new HarmonyMethod(typeof(MapHandling),
				nameof(DebugUpdateVehicleRegions)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(MapInterface), nameof(MapInterface.MapInterfaceOnGUI_AfterMainTabs)),
				postfix: new HarmonyMethod(typeof(MapHandling),
				nameof(DebugOnGUIVehicleRegions)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Map), nameof(Map.FinalizeInit)),
				prefix: new HarmonyMethod(typeof(MapHandling),
				nameof(PreFinalizeInitRegionBuilding)));
		}

		/// <summary>
		/// Modify the pseudorandom beach value used and insert custom value within the mod settings.
		/// </summary>
		/// <param name="instructions"></param>
		/// <returns></returns>
		public static IEnumerable<CodeInstruction> BeachMakerTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			for(int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if(instruction.Calls(AccessTools.Property(typeof(FloatRange), nameof(FloatRange.RandomInRange)).GetGetMethod()))
				{
					i++;
					instruction = instructionList[i];
					yield return new CodeInstruction(opcode: OpCodes.Pop);
					yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(ModSettingsHelper), nameof(ModSettingsHelper.CustomFloatBeach)));
				}
				yield return instruction;
			}
		}

		/// <summary>
		/// Move settlement's spawning location towards the coastline with radius r specified in the mod settings
		/// </summary>
		/// <param name="instructions"></param>
		/// <returns></returns>
		public static IEnumerable<CodeInstruction> PushSettlementToCoastTranspiler(IEnumerable<CodeInstruction> instructions)
		{

			List<CodeInstruction> instructionList = instructions.ToList();

			for(int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if(instruction.opcode == OpCodes.Ldnull && instructionList[i-1].opcode == OpCodes.Ldloc_1)
				{
					//Call method, grab new location and store
					yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(WorldHelper), nameof(WorldHelper.PushSettlementToCoast)));
					yield return new CodeInstruction(opcode: OpCodes.Stloc_1);
					yield return new CodeInstruction(opcode: OpCodes.Ldloc_1);
				}
				yield return instruction;
			}
		}

		/// <summary>
		/// Ensure map is not removed with vehicles that contain pawns or maps currenty being targeted for landing.
		/// </summary>
		/// <param name="__instance"></param>
		/// <param name="__result"></param>
		public static void AnyVehicleBlockingMapRemoval(MapPawns __instance, ref bool __result, Map ___map)
		{
			if(__result is false)
			{
				if (LandingTargeter.Instance.IsTargeting && Current.Game.CurrentMap == ___map)
				{
					__result = true;
					return;
				}
				if (MapHelper.AnyVehicleSkyfallersBlockingMap(___map))
				{
					__result = true;
					return;
				}
				if (MapHelper.AnyAerialVehiclesInRecon(___map))
				{
					__result = true;
					return;
				}
				foreach(Pawn pawn in __instance.AllPawnsSpawned)
				{
					if(pawn is VehiclePawn vehicle && vehicle.AllPawnsAboard.NotNullAndAny())
					{
						foreach (Pawn sailor in vehicle.AllPawnsAboard)
						{
							if (!sailor.Downed && sailor.IsColonist)
							{
								__result = true;
								return;
							}
							if (sailor.relations != null && sailor.relations.relativeInvolvedInRescueQuest != null)
							{
								__result = true;
								return;
							}
							if (sailor.Faction == Faction.OfPlayer || sailor.HostFaction == Faction.OfPlayer)
							{
								if (sailor.CurJob != null && sailor.CurJob.exitMapOnArrival)
								{
									__result = true;
									return;
								}
							}
							//Caravan to join for?
						}
					}
				}
			}
		}

		public static void NotifyEverythingWhichUsesMapReferencePost(Map map)
		{
			List<Map> maps = Find.Maps;
			int mapIndex = maps.IndexOf(map);
			for (int i = mapIndex; i < maps.Count; i++)
			{
				Map searchMap = maps[i];
				foreach (VehicleMapping.VehiclePathData pathData in searchMap.GetCachedMapComponent<VehicleMapping>().AllPathData)
				{
					foreach (VehicleRegion region in pathData.VehicleRegionGrid.AllRegions_NoRebuild_InvalidAllowed)
					{
						if (i == mapIndex)
						{
							region.Notify_MyMapRemoved();
						}
						else
						{
							region.DecrementMapIndex();
						}
					}
				}
			}
		}

		public static void DebugUpdateVehicleRegions()
		{
			if (Find.CurrentMap != null && !WorldRendererUtility.WorldRenderedNow)
			{
				DebugHelper.DebugDrawVehicleRegion(Find.CurrentMap);
			}
		}

		public static void DebugOnGUIVehicleRegions()
		{
			if (Find.CurrentMap != null && !WorldRendererUtility.WorldRenderedNow)
			{
				DebugHelper.DebugDrawVehiclePathCostsOverlay(Find.CurrentMap);
			}
		}

		private static void PreFinalizeInitRegionBuilding(Map __instance)
		{
			__instance.GetCachedMapComponent<VehicleMapping>().RebuildVehiclePathData();
		}
	}
}
