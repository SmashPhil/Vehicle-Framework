using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HarmonyLib;
using Verse;
using Verse.Sound;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	internal class Gizmos : IPatchCategory
	{
		public void PatchMethods()
		{
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Settlement), nameof(Settlement.GetCaravanGizmos)), prefix: null,
				postfix: new HarmonyMethod(typeof(Gizmos),
				nameof(NoAttackSettlementWhenDocked)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Settlement), nameof(Settlement.GetGizmos)), prefix: null,
				postfix: new HarmonyMethod(typeof(Gizmos),
				nameof(AddVehicleCaravanGizmoPassthrough)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(FormCaravanComp), nameof(FormCaravanComp.GetGizmos)), prefix: null,
				postfix: new HarmonyMethod(typeof(Gizmos),
				nameof(AddVehicleGizmosPassthrough)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.GetGizmos)), prefix: null,
				postfix: new HarmonyMethod(typeof(Gizmos),
				nameof(GizmosForVehicleCaravans)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Designator_Build), nameof(Designator_Build.GizmoOnGUI)),
				prefix: new HarmonyMethod(typeof(Gizmos),
				nameof(VehicleMaterialOnBuildGizmo)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(BuildCopyCommandUtility), nameof(BuildCopyCommandUtility.BuildCopyCommand)),
				prefix: new HarmonyMethod(typeof(Gizmos),
				nameof(VehicleMaterialOnCopyBuildGizmo)));

			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Dialog_InfoCard), nameof(Dialog_InfoCard.DoWindowContents)),
				prefix: new HarmonyMethod(typeof(Gizmos),
				nameof(VehicleInfoCardOverride)));
		}

		/// <summary>
		/// Disable the ability to attack a settlement when docked there. Breaks immersion and can cause an entry cell error. (Boats Only)
		/// </summary>
		/// <param name="caravan"></param>
		/// <param name="__result"></param>
		/// <param name="__instance"></param>
		public static void NoAttackSettlementWhenDocked(Caravan caravan, ref IEnumerable<Gizmo> __result, Settlement __instance)
		{
			if (caravan is VehicleCaravan vehicleCaravan && vehicleCaravan.HasBoat() && !vehicleCaravan.vPather.Moving)
			{
				List<Gizmo> gizmos = __result.ToList();
				if (caravan.PawnsListForReading.NotNullAndAny(p => !p.IsBoat()))
				{
					int index = gizmos.FindIndex(x => (x as Command_Action).icon == Settlement.AttackCommand);
					if (index >= 0 && index < gizmos.Count)
					{
						gizmos[index].Disable("VF_CommandAttackDockDisable".Translate(__instance.LabelShort));
					}
				}
				else
				{
					int index2 = gizmos.FindIndex(x => (x as Command_Action).icon == ContentFinder<Texture2D>.Get("UI/Commands/Trade", false));
					if (index2 >= 0 && index2 < gizmos.Count)
					{
						gizmos[index2].Disable("VF_CommandTradeDockDisable".Translate(__instance.LabelShort));
					}
					int index3 = gizmos.FindIndex(x => (x as Command_Action).icon == ContentFinder<Texture2D>.Get("UI/Commands/OfferGifts", false));
					if (index3 >= 0 && index3 < gizmos.Count)
					{
						gizmos[index3].Disable("VF_CommandTradeDockDisable".Translate(__instance.LabelShort));
					}
				}
				__result = gizmos;
			}
		}

		//REDO
		/// <summary>
		/// Adds FormVehicleCaravan gizmo to settlements, allowing custom dialog menu, seating arrangements, custom RoutePlanner
		/// </summary>
		/// <param name="__result"></param>
		/// <param name="__instance"></param>
		/// <returns></returns>
		public static IEnumerable<Gizmo> AddVehicleCaravanGizmoPassthrough(IEnumerable<Gizmo> __result, Settlement __instance)
		{
			IEnumerator<Gizmo> enumerator = __result.GetEnumerator();
			if(__instance.Faction == Faction.OfPlayer)
			{
			  //  yield return new Command_Action()
			  //  {
			  //      defaultLabel = "CommandFormVehicleCaravan".Translate(),
					//defaultDesc = "CommandFormVehicleCaravanDesc".Translate(),
					//icon = Settlement.FormCaravanCommand,
			  //      action = delegate ()
			  //      {
			  //          Find.Tutor.learningReadout.TryActivateConcept(ConceptDefOf.FormCaravan);
			  //          Find.WindowStack.Add(new Dialog_FormVehicleCaravan(__instance.Map));
			  //      }
			  //  };
			}
			while(enumerator.MoveNext())
			{
				var element = enumerator.Current;
				yield return element;
			}
		}

		/// <summary>
		/// Adds FormVehicleCaravan gizmo to FormCaravanComp, allowing 
		/// </summary>
		/// <param name="__result"></param>
		/// <param name="__instance"></param>
		public static IEnumerable<Gizmo> AddVehicleGizmosPassthrough(IEnumerable<Gizmo> __result, FormCaravanComp __instance)
		{
			IEnumerator<Gizmo> enumerator = __result.GetEnumerator();
			if (__instance.parent is MapParent mapParent && __instance.ParentHasMap)
			{
				if (!__instance.Reform)
				{
					yield return new Command_Action()
					{
						defaultLabel = "VF_CommandFormVehicleCaravan".Translate(),
						defaultDesc = "VF_CommandFormVehicleCaravanDesc".Translate(),
						icon = VehicleTex.FormCaravanVehicle,
						action = delegate ()
						{
							Find.WindowStack.Add(new Dialog_FormVehicleCaravan(mapParent.Map));
						}
					};
				}
				else if (mapParent.Map.mapPawns.AllPawnsSpawned.HasVehicle())
				{
					Command_Action command_Action = new Command_Action
					{
						defaultLabel = "VF_CommandReformVehicleCaravan".Translate(),
						defaultDesc = "VF_CommandReformVehicleCaravanDesc".Translate(),
						icon = VehicleTex.FormCaravanVehicle,
						hotKey = KeyBindingDefOf.Misc2,
						action = delegate ()
						{
							Find.WindowStack.Add(new Dialog_FormVehicleCaravan(mapParent.Map, true));
						}
					};
					if (GenHostility.AnyHostileActiveThreatToPlayer(mapParent.Map))
					{
						command_Action.Disable("CommandReformCaravanFailHostilePawns".Translate());
					}
					yield return command_Action;
				}
			}
			while(enumerator.MoveNext())
			{
				var element = enumerator.Current;
				yield return element;
			}
		}

		/// <summary>
		/// Insert Gizmos from Vehicle caravans which are still forming. Allows for pawns to join the caravan if the Lord Toil has not yet reached LeaveShip
		/// </summary>
		/// <param name="__result"></param>
		/// <param name="pawn"></param>
		/// <param name="___AddToCaravanCommand"></param>
		public static void GizmosForVehicleCaravans(ref IEnumerable<Gizmo> __result, Pawn pawn, Texture2D ___AddToCaravanCommand)
		{
			if(pawn.Spawned)
			{
				bool anyCaravanToJoin = false;
				foreach (Lord lord in pawn.Map.lordManager.lords)
				{
					if(lord.faction == Faction.OfPlayer && lord.LordJob is LordJob_FormAndSendVehicles && !(lord.CurLordToil is LordToil_PrepareCaravan_LeaveWithVehicles) && !(lord.CurLordToil is LordToil_PrepareCaravan_BoardVehicles))
					{
						anyCaravanToJoin = true;
						break;
					}
				}
				if(anyCaravanToJoin && Dialog_FormCaravan.AllSendablePawns(pawn.Map, false).Contains(pawn))
				{
					Command_Action joinCaravan = new Command_Action();
					joinCaravan = new Command_Action
					{
						defaultLabel = "CommandAddToCaravan".Translate(),
						defaultDesc = "CommandAddToCaravanDesc".Translate(),
						icon = ___AddToCaravanCommand,
						action = delegate()
						{
							List<Lord> list = new List<Lord>();
							foreach(Lord lord in pawn.Map.lordManager.lords)
							{
								if(lord.faction == Faction.OfPlayer && lord.LordJob is LordJob_FormAndSendVehicles)
								{
									list.Add(lord);
								}
							}
							if (list.Count <= 0)
								return;
							if(list.Count == 1)
							{
								AccessTools.Method(typeof(CaravanFormingUtility), "LateJoinFormingCaravan").Invoke(null, new object[] { pawn, list[0] });
								SoundDefOf.Click.PlayOneShotOnCamera(null);
							}
							else
							{
								List<FloatMenuOption> list2 = new List<FloatMenuOption>();
								for(int i = 0; i < list.Count; i++)
								{
									Lord caravanLocal = list[i];
									string label = "Caravan".Translate() + " " + (i + 1);
									list2.Add(new FloatMenuOption(label, delegate ()
									{
										if (pawn.Spawned && pawn.Map.lordManager.lords.Contains(caravanLocal) && Dialog_FormCaravan.AllSendablePawns(pawn.Map, false).Contains(pawn))
										{
											AccessTools.Method(typeof(CaravanFormingUtility), "LateJoinFormingCaravan").Invoke(null, new object[] { pawn, caravanLocal });
										} 
									}, MenuOptionPriority.Default, null, null, 0f, null, null));
								}
								Find.WindowStack.Add(new FloatMenu(list2));
							}
						},
						hotKey = KeyBindingDefOf.Misc7
					};
					List<Gizmo> gizmos = __result.ToList();
					gizmos.Add(joinCaravan);
					__result = gizmos;
				}
			}
		}

		public static bool VehicleMaterialOnBuildGizmo(Vector2 topLeft, float maxWidth, BuildableDef ___entDef, ref GizmoResult __result, Designator_Build __instance)
		{
			if (___entDef is VehicleBuildDef def)
			{
				float width = __instance.GetWidth(maxWidth);
				__result = VehicleGUI.GizmoOnGUIWithMaterial(__instance, new Rect(topLeft.x, topLeft.y, width, width), def);
				if (def.MadeFromStuff)
				{
					Designator_Dropdown.DrawExtraOptionsIcon(topLeft, __instance.GetWidth(maxWidth));
				}
				return false;
			}
			return true;
		}

		public static bool VehicleMaterialOnCopyBuildGizmo(BuildableDef buildable, ThingDef stuff, ref Command __result)
		{
			if (buildable is VehicleBuildDef buildDef)
			{
				Designator_Build des = BuildCopyCommandUtility.FindAllowedDesignator(buildable, true);
				if (des == null)
				{
					__result = null;
				}
				if (buildable.MadeFromStuff && stuff == null)
				{
					__result = des;
				}
				Command_ActionVehicleDrawn command_Action = new Command_ActionVehicleDrawn();
				command_Action.action = delegate ()
				{
					SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
					Find.DesignatorManager.Select(des);
					des.SetStuffDef(stuff);
				};
				command_Action.defaultLabel = "CommandBuildCopy".Translate();
				command_Action.defaultDesc = "CommandBuildCopyDesc".Translate();
				ThingDef stuffDefRaw = des.StuffDefRaw;
				des.SetStuffDef(stuff);
				command_Action.icon = des.ResolvedIcon();
				command_Action.iconProportions = des.iconProportions;
				command_Action.iconDrawScale = des.iconDrawScale;
				command_Action.iconTexCoords = des.iconTexCoords;
				command_Action.iconAngle = des.iconAngle;
				command_Action.iconOffset = des.iconOffset;
				command_Action.Order = 10f;
				command_Action.buildDef = buildDef;
				command_Action.SetColorOverride(des.IconDrawColor);
				des.SetStuffDef(stuffDefRaw);
				if (stuff != null)
				{
					command_Action.defaultIconColor = buildable.GetColorForStuff(stuff);
				}
				else
				{
					command_Action.defaultIconColor = buildable.uiIconColor;
				}
				command_Action.hotKey = KeyBindingDefOf.Misc11;

				__result = command_Action;
				return false;
			}
			return true;
		}

		public static bool VehicleInfoCardOverride(Rect inRect, Thing ___thing, ThingDef ___def)
		{
			if (___def is VehicleBuildDef buildDef)
			{
				VehicleInfoCard.DrawFor(inRect, buildDef.thingToSpawn);
				return false;
			}
			else if (___thing is VehicleBuilding building)
			{
				VehicleInfoCard.DrawFor(inRect, building.VehicleDef);
				return false;
			}
			else if (___thing is VehiclePawn vehicle)
			{
				VehicleInfoCard.DrawFor(inRect, vehicle);
				return false;
			}
			return true;
		}
	}
}
