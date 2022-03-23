using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using OpCodes = System.Reflection.Emit.OpCodes;
using UnityEngine;
using HarmonyLib;
using Verse;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using Vehicles.Lords;


namespace Vehicles
{
	internal class CaravanHandling : IPatchCategory
	{
		public void PatchMethods()
		{
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Dialog_FormCaravan), "TryReformCaravan"),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(ConfirmLeaveVehiclesOnReform)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(MassUtility), nameof(MassUtility.Capacity)),
				postfix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(CapacityOfVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(MassUtility), nameof(MassUtility.CanEverCarryAnything)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(CanCarryIfVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "FillTab"),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(FillTabVehicleCaravan)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "DoPeopleAndAnimals"), 
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(DoPeopleAnimalsAndVehicle)));
			VehicleHarmony.Patch(original: AccessTools.PropertyGetter(typeof(Alert_CaravanIdle), "IdleCaravans"),
				postfix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(IdleVehicleCaravans)));

			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanArrivalAction_Enter), nameof(CaravanArrivalAction_Enter.Arrived)), prefix: null, postfix: null,
			//	transpiler: new HarmonyMethod(typeof(CaravanHandling),
			//	nameof(VehiclesArrivedTranspiler)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanArrivalAction_VisitEscapeShip), "DoArrivalAction"), prefix: null, postfix: null,
				transpiler: new HarmonyMethod(typeof(CaravanHandling),
				nameof(ShipsVisitEscapeShipTranspiler)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(SettlementUtility), "AttackNow"), prefix: null, postfix: null,
				transpiler: new HarmonyMethod(typeof(CaravanHandling),
				nameof(AttackNowWithShipsTranspiler)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanArrivalAction_VisitSite), "DoEnter"), prefix: null, postfix: null,
				transpiler: new HarmonyMethod(typeof(CaravanHandling),
				nameof(DoEnterWithShipsTranspiler)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanEnterMapUtility), nameof(CaravanEnterMapUtility.Enter), new Type[]{typeof(Caravan), typeof(Map), typeof(CaravanEnterMode), typeof(CaravanDropInventoryMode), typeof(bool), typeof(Predicate<IntVec3>) }),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(EnterMapVehiclesCatchAll1)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanEnterMapUtility), nameof(CaravanEnterMapUtility.Enter), new Type[] { typeof(Caravan), typeof(Map), typeof(Func<Pawn, IntVec3>), typeof(CaravanDropInventoryMode), typeof(bool) }),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(EnterMapVehiclesCatchAll2)));

			VehicleHarmony.Patch(original: AccessTools.Property(typeof(Caravan), nameof(Caravan.AllOwnersDowned)).GetGetMethod(),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(AllOwnersDownedVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Property(typeof(Caravan), nameof(Caravan.AllOwnersHaveMentalBreak)).GetGetMethod(),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(AllOwnersMentalBreakVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Property(typeof(Caravan), nameof(Caravan.NightResting)).GetGetMethod(),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(NoRestForVehicles)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Caravan), nameof(Caravan.ContainsPawn)), prefix: null,
				postfix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(ContainsPawnInVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Caravan), nameof(Caravan.IsOwner)), prefix: null,
				postfix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(IsOwnerOfVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(SettlementDefeatUtility), nameof(SettlementDefeatUtility.CheckDefeated)), prefix: null, postfix: null,
				transpiler: new HarmonyMethod(typeof(CaravanHandling),
				nameof(CheckDefeatedWithVehiclesTranspiler)));

			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Tale_DoublePawn), nameof(Tale_DoublePawn.Concerns)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(ConcernNullThing)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(WITab_Caravan_Needs), "FillTab"), prefix: null, postfix: null,
				transpiler: new HarmonyMethod(typeof(CaravanHandling),
				nameof(VehicleNeedsFillTabTranspiler)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(WITab_Caravan_Needs), "UpdateSize"), prefix: null, postfix: null,
				transpiler: new HarmonyMethod(typeof(CaravanHandling),
				nameof(VehicleNeedsUpdateSizeTranspiler)));
			VehicleHarmony.Patch(original: AccessTools.Property(typeof(WITab_Caravan_Gear), "Pawns").GetGetMethod(nonPublic: true), prefix: null,
				postfix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(VehicleGearTabPawns)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanInventoryUtility), nameof(CaravanInventoryUtility.AllInventoryItems)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(VehicleAllInventoryItems)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Settlement_TraderTracker), nameof(Settlement_TraderTracker.ColonyThingsWillingToBuy)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(AerialVehicleInventoryItems)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanInventoryUtility), nameof(CaravanInventoryUtility.GiveThing)), prefix: null, postfix: null,
				transpiler: new HarmonyMethod(typeof(CaravanHandling),
				nameof(VehicleGiveThingInventoryTranspiler)));
			VehicleHarmony.Patch(original: AccessTools.Property(typeof(WITab_Caravan_Health), "Pawns").GetGetMethod(nonPublic: true),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(VehicleHealthTabPawns)));
			VehicleHarmony.Patch(original: AccessTools.Property(typeof(WITab_Caravan_Social), "Pawns").GetGetMethod(nonPublic: true),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(VehicleSocialTabPawns)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(BestCaravanPawnUtility), nameof(BestCaravanPawnUtility.FindPawnWithBestStat)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(FindPawnInVehicleWithBestStat)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanArrivalAction_OfferGifts), nameof(CaravanArrivalAction_OfferGifts.Arrived)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(UnloadVehicleOfferGifts)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Settlement_TraderTracker), nameof(Settlement_TraderTracker.GiveSoldThingToPlayer)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(GiveSoldThingToAerialVehicle)),
				transpiler: new HarmonyMethod(typeof(CaravanHandling),
				nameof(GiveSoldThingToVehicleTranspiler)));

			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Caravan_NeedsTracker), "TrySatisfyPawnNeeds"),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(TrySatisfyVehiclePawnsNeeds)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanUtility), nameof(CaravanUtility.IsCaravanMember)), prefix: null,
				postfix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(IsParentCaravanMember)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanUtility), nameof(CaravanUtility.RandomOwner)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(RandomVehicleOwner)));
		}

		/// <summary>
		/// Show DialogMenu for confirmation on leaving vehicles behind when forming caravan
		/// </summary>
		/// <param name="__instance"></param>
		/// <param name="___transferables"></param>
		/// <param name="___map"></param>
		/// <param name="___destinationTile"></param>
		/// <param name="__result"></param>
		public static bool ConfirmLeaveVehiclesOnReform(Dialog_FormCaravan __instance, ref List<TransferableOneWay> ___transferables, Map ___map, int ___destinationTile, ref bool __result)
		{
			if(___map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).HasVehicle())
			{
				List<Pawn> pawns = TransferableUtility.GetPawnsFromTransferables(___transferables);
				List<Pawn> correctedPawns = pawns.Where(p => !(p is VehiclePawn)).ToList();
				string vehicles = "";
				foreach(Pawn pawn in pawns.Where(p => p is VehiclePawn))
				{
					vehicles += pawn.LabelShort;
				}
				
				Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("LeaveVehicleBehindCaravan".Translate(vehicles), delegate ()
				{
					if (!(bool)AccessTools.Method(typeof(Dialog_FormCaravan), "CheckForErrors").Invoke(__instance, new object[] { correctedPawns }))
					{
						return;
					}
					AccessTools.Method(typeof(Dialog_FormCaravan), "AddItemsFromTransferablesToRandomInventories").Invoke(__instance, new object[] { correctedPawns });
					Caravan caravan = CaravanExitMapUtility.ExitMapAndCreateCaravan(correctedPawns, Faction.OfPlayer, __instance.CurrentTile, __instance.CurrentTile, ___destinationTile, false);
					___map.Parent.CheckRemoveMapNow();
					TaggedString taggedString = "MessageReformedCaravan".Translate();
					if (caravan.pather.Moving && caravan.pather.ArrivalAction != null)
					{
						taggedString += " " + "MessageFormedCaravan_Orders".Translate() + ": " + caravan.pather.ArrivalAction.Label + ".";
					}
					Messages.Message(taggedString, caravan, MessageTypeDefOf.TaskCompletion, false);

				}, false, null));
				__result = true;
				return false;
			}
			return true;
		}

		/// <summary>
		/// Carry capacity with Vehicles when using MassCalculator
		/// </summary>
		/// <param name="thingCounts"></param>
		/// <param name="__result"></param>
		/// <param name="explanation"></param>
		public static void CapacityOfVehicle(Pawn p, ref float __result, StringBuilder explanation = null)
		{
			if (p is VehiclePawn vehicle)
			{
				__result = Mathf.Max(vehicle.CargoCapacity, 0f);
			}
		}

		/// <summary>
		/// Allow vehicles to carry items without being a PackAnimal or ToolUser
		/// </summary>
		/// <param name="p"></param>
		/// <param name="__result"></param>
		/// <originalMethod>MassUtility.CanEverCarryAnything</originalMethod>
		public static bool CanCarryIfVehicle(Pawn p, ref bool __result)
		{
			__result = false;
			if(p is VehiclePawn)
				__result = true;
			return !__result;
		}

		

		public static bool FillTabVehicleCaravan(ITab_Pawn_FormingCaravan __instance, ref List<Thing> ___thingsToSelect, Vector2 ___size, 
			ref float ___lastDrawnHeight, ref Vector2 ___scrollPosition, ref List<Thing> ___tmpSingleThing)
		{
			if((Find.Selector.SingleSelectedThing as Pawn).GetLord().LordJob is LordJob_FormAndSendVehicles)
			{
				___thingsToSelect.Clear();
				Rect outRect = new Rect(default, ___size).ContractedBy(10f);
				outRect.yMin += 20f;
				Rect rect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(___lastDrawnHeight, outRect.height));
				Widgets.BeginScrollView(outRect, ref ___scrollPosition, rect, true);
				float num = 0f;
				string status = ((LordJob_FormAndSendVehicles)(Find.Selector.SingleSelectedThing as Pawn).GetLord().LordJob).Status;
				Widgets.Label(new Rect(0f, num, rect.width, 100f), status);
				num += 22f;
				num += 4f;
				object[] method1Args = new object[2] { rect, num };
				MethodInfo doPeopleAndAnimals = AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "DoPeopleAndAnimals");
				doPeopleAndAnimals.Invoke(__instance, method1Args);
				num = (float)method1Args[1];
				num += 4f;
				CaravanHelper.DoItemsListForVehicle(rect, ref num, ref ___tmpSingleThing, __instance);
				___lastDrawnHeight = num;
				Widgets.EndScrollView();
				if(___thingsToSelect.Any<Thing>())
				{
					ITab_Pawn_FormingCaravan.SelectNow(___thingsToSelect);
					___thingsToSelect.Clear();
				}
				return false;
			}
			return true;
		}

		public static bool DoPeopleAnimalsAndVehicle(Rect inRect, ref float curY, ITab_Pawn_FormingCaravan __instance, ref List<Thing> ___tmpPawns)
		{
			if((Find.Selector.SingleSelectedThing as Pawn).GetLord().LordJob is LordJob_FormAndSendVehicles)
			{
				Widgets.ListSeparator(ref curY, inRect.width, "CaravanMembers".Translate());
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				int num5 = 0;
				int num6 = 0;
				int num7 = 0;
				int numShip = 0;
				Lord lord = (Find.Selector.SingleSelectedThing as Pawn).GetLord();
				for (int i = 0; i < lord.ownedPawns.Count; i++)
				{
					Pawn pawn = lord.ownedPawns[i];
					if (pawn.IsFreeColonist)
					{
						num++;
						if (pawn.InMentalState)
						{
							num2++;
						}
					}
					if(pawn is VehiclePawn vehicle)
					{
						if(vehicle.AllPawnsAboard.NotNullAndAny())
						{
							num += vehicle.AllPawnsAboard.FindAll(x => x.IsFreeColonist).Count;
							num2 += vehicle.AllPawnsAboard.FindAll(x => x.IsFreeColonist && x.InMentalState).Count;
							num3 += vehicle.AllPawnsAboard.FindAll(x => x.IsPrisoner).Count;
							num4 += vehicle.AllPawnsAboard.FindAll(x => x.IsPrisoner && x.InMentalState).Count;
							num5 += vehicle.AllPawnsAboard.FindAll(x => x.RaceProps.Animal).Count;
							num6 += vehicle.AllPawnsAboard.FindAll(x => x.RaceProps.Animal && x.InMentalState).Count;
							num7 += vehicle.AllPawnsAboard.FindAll(x => x.RaceProps.Animal && x.RaceProps.packAnimal).Count;
						}
						if(!vehicle.beached)
						{
							numShip++;
						}
					}
					else if (pawn.IsPrisoner)
					{
						num3++;
						if (pawn.InMentalState)
						{
							num4++;
						}
					}
					else if (pawn.RaceProps.Animal)
					{
						num5++;
						if (pawn.InMentalState)
						{
							num6++;
						}
						if (pawn.RaceProps.packAnimal)
						{
							num7++;
						}
					}
				}
				MethodInfo getPawnsCountLabel = AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "GetPawnsCountLabel");
				string pawnsCountLabel = (string)getPawnsCountLabel.Invoke(__instance, new object[] { num, num2, -1 });
				string pawnsCountLabel2 = (string)getPawnsCountLabel.Invoke(__instance, new object[] { num3, num4, -1 });
				string pawnsCountLabel3 = (string)getPawnsCountLabel.Invoke(__instance, new object[] { num5, num6, num7 });
				string pawnsCountLabelShip = (string)getPawnsCountLabel.Invoke(__instance, new object[] { numShip, -1, -1});

				MethodInfo doPeopleAndAnimalsEntry = AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "DoPeopleAndAnimalsEntry");

				float y = curY;
				float num8;
				object[] m1args = new object[] { inRect, Faction.OfPlayer.def.pawnsPlural.CapitalizeFirst(), pawnsCountLabel, curY, null };
				doPeopleAndAnimalsEntry.Invoke(__instance, m1args);
				curY = (float)m1args[3];
				num8 = (float)m1args[4];

				float yShip = curY;
				float numS;
				object[] mSargs = new object[] { inRect, "CaravanShips".Translate().ToStringSafe(), pawnsCountLabelShip, curY, null };
				doPeopleAndAnimalsEntry.Invoke(__instance, mSargs);
				curY = (float)mSargs[3];
				numS = (float)mSargs[4];

				float y2 = curY;
				float num9;
				object[] m2args = new object[] { inRect, "CaravanPrisoners".Translate().ToStringSafe(), pawnsCountLabel2, curY, null };
				doPeopleAndAnimalsEntry.Invoke(__instance, m2args);
				curY = (float)m2args[3];
				num9 = (float)m2args[4];

				float y3 = curY;
				float num10;
				object[] m3args = new object[] { inRect, "CaravanAnimals".Translate().ToStringSafe(), pawnsCountLabel3, curY, null };
				doPeopleAndAnimalsEntry.Invoke(__instance, m3args);
				curY = (float)m3args[3];
				num10 = (float)m3args[4];

				float width = Mathf.Max(new float[]
				{
					num8,
					numS,
					num9,
					num10
				}) + 2f;

				Rect rect = new Rect(0f, y, width, 22f);
				if (Mouse.IsOver(rect))
				{
					Widgets.DrawHighlight(rect);
					AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "HighlightColonists").Invoke(__instance, null);
				}
				if (Widgets.ButtonInvisible(rect, false))
				{
					AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "SelectColonistsLater").Invoke(__instance, null);
				}

				Rect rectS = new Rect(0f, yShip, width, 22f);
				if(Mouse.IsOver(rectS))
				{
					Widgets.DrawHighlight(rectS);
					foreach(Pawn p in lord.ownedPawns)
					{
						if(p is VehiclePawn)
						{
							TargetHighlighter.Highlight(p, true, true, false);
						}
					}
				}
				if(Widgets.ButtonInvisible(rectS, false))
				{
					___tmpPawns.Clear();
					foreach(Pawn p in lord.ownedPawns)
					{
						if(p is VehiclePawn)
						{
							___tmpPawns.Add(p);
						}
					}
					AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "SelectLater").Invoke(__instance, new object[] { ___tmpPawns });
					___tmpPawns.Clear();
				}

				Rect rect2 = new Rect(0f, y2, width, 22f);
				if (Mouse.IsOver(rect2))
				{
					Widgets.DrawHighlight(rect2);
					AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "HighlightPrisoners").Invoke(__instance, null);
				}
				if (Widgets.ButtonInvisible(rect2, false))
				{
					AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "SelectPrisonersLater").Invoke(__instance, null);
				}

				Rect rect3 = new Rect(0f, y3, width, 22f);
				if (Mouse.IsOver(rect3))
				{
					Widgets.DrawHighlight(rect3);
					AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "HighlightAnimals").Invoke(__instance, null);
				}
				if (Widgets.ButtonInvisible(rect3, false))
				{
					AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "SelectAnimalsLater").Invoke(__instance, null);
				}
				return false;
			}
			return true;
		}

		public static void IdleVehicleCaravans(ref List<Caravan> __result)
		{
			if (!__result.NullOrEmpty())
			{
				__result.RemoveAll(c => c is VehicleCaravan vehicleCaravan && vehicleCaravan.vPather.MovingNow);
			}
		}

		public static IEnumerable<CodeInstruction> VehiclesArrivedTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if(instruction.Calls(AccessTools.Method(typeof(CaravanEnterMapUtility), nameof(CaravanEnterMapUtility.Enter), new Type[]{typeof(Caravan), typeof(Map),
					typeof(CaravanEnterMode), typeof(CaravanDropInventoryMode), typeof(bool), typeof(Predicate<IntVec3>) })))
				{
					Label label = ilg.DefineLabel();
					Label brlabel = ilg.DefineLabel();

					yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(Ext_Caravan), nameof(Ext_Caravan.HasVehicle), new Type[] { typeof(Caravan) }));
					yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(EnterMapUtilityVehicles), nameof(EnterMapUtilityVehicles.EnterAndSpawn)));
					yield return new CodeInstruction(opcode: OpCodes.Br, brlabel);

					instruction.labels.Add(label);
					yield return instruction; //CALL : CaravanEnterMapUtility::Enter
					instruction = instructionList[++i];

					instruction.labels.Add(brlabel);
				}
				yield return instruction;
			}
		}

		public static IEnumerable<CodeInstruction> ShipsVisitEscapeShipTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if (instruction.Calls(AccessTools.Method(typeof(CaravanEnterMapUtility), nameof(CaravanEnterMapUtility.Enter), new Type[]{typeof(Caravan), typeof(Map),
					typeof(CaravanEnterMode), typeof(CaravanDropInventoryMode), typeof(bool), typeof(Predicate<IntVec3>) })))
				{
					Label label = ilg.DefineLabel();
					Label brlabel = ilg.DefineLabel();

					yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(Ext_Caravan), nameof(Ext_Caravan.HasVehicle), new Type[] { typeof(Caravan) }));
					yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(EnterMapUtilityVehicles), nameof(EnterMapUtilityVehicles.EnterAndSpawn)));
					yield return new CodeInstruction(opcode: OpCodes.Br, brlabel);

					instruction.labels.Add(label);
					yield return instruction; //CALL : CaravanEnterMapUtility::Enter
					instruction = instructionList[++i];

					instruction.labels.Add(brlabel);
				}
				yield return instruction;
			}
		}

		public static IEnumerable<CodeInstruction> DoEnterWithShipsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if (instruction.Calls(AccessTools.Method(typeof(CaravanEnterMapUtility), nameof(CaravanEnterMapUtility.Enter), new Type[]{typeof(Caravan), typeof(Map),
					typeof(CaravanEnterMode), typeof(CaravanDropInventoryMode), typeof(bool), typeof(Predicate<IntVec3>) })))
				{
					Label label = ilg.DefineLabel();
					Label brlabel = ilg.DefineLabel();

					yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(Ext_Caravan), nameof(Ext_Caravan.HasVehicle), new Type[] { typeof(Caravan) }));
					yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(EnterMapUtilityVehicles), nameof(EnterMapUtilityVehicles.EnterAndSpawn)));
					yield return new CodeInstruction(opcode: OpCodes.Br, brlabel);

					instruction.labels.Add(label);
					yield return instruction; //CALL : CaravanEnterMapUtility::Enter
					instruction = instructionList[++i];

					instruction.labels.Add(brlabel);
				}
				yield return instruction;
			}
		}

		public static IEnumerable<CodeInstruction> AttackNowWithShipsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if (instruction.Calls(AccessTools.Method(typeof(CaravanEnterMapUtility), nameof(CaravanEnterMapUtility.Enter), new Type[]{typeof(Caravan), typeof(Map),
					typeof(CaravanEnterMode), typeof(CaravanDropInventoryMode), typeof(bool), typeof(Predicate<IntVec3>) })))
				{
					Label label = ilg.DefineLabel();
					Label brlabel = ilg.DefineLabel();

					yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(Ext_Caravan), nameof(Ext_Caravan.HasVehicle), new Type[] { typeof(Caravan) }));
					yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(EnterMapUtilityVehicles), nameof(EnterMapUtilityVehicles.EnterAndSpawn)));
					yield return new CodeInstruction(opcode: OpCodes.Br, brlabel);

					instruction.labels.Add(label);
					yield return instruction; //CALL : CaravanEnterMapUtility::Enter
					instruction = instructionList[++i];

					instruction.labels.Add(brlabel);
				}
				yield return instruction;
			}
		}

		public static bool EnterMapVehiclesCatchAll1(Caravan caravan, Map map, CaravanEnterMode enterMode, CaravanDropInventoryMode dropInventoryMode = CaravanDropInventoryMode.DoNotDrop, 
			bool draftColonists = false, Predicate<IntVec3> extraCellValidator = null)
		{
			if (caravan is VehicleCaravan vehicleCaravan)
			{
				EnterMapUtilityVehicles.EnterAndSpawn(vehicleCaravan, map, enterMode, dropInventoryMode, draftColonists, extraCellValidator);
				return false;
			}
			return true;
		}

		public static bool EnterMapVehiclesCatchAll2(Caravan caravan, Map map, Func<Pawn, IntVec3> spawnCellGetter, CaravanDropInventoryMode dropInventoryMode = CaravanDropInventoryMode.DoNotDrop, bool draftColonists = false)
		{
			if (caravan is VehicleCaravan vehicleCaravan)
			{
				EnterMapUtilityVehicles.EnterAndSpawn(vehicleCaravan, map, CaravanEnterMode.Edge, dropInventoryMode, draftColonists, null);
				return false;
			}
			return true;
		}

		public static bool AllOwnersDownedVehicle(Caravan __instance, ref bool __result)
		{
			if(__instance.PawnsListForReading.NotNullAndAny(x => x is VehiclePawn))
			{
				foreach (Pawn pawn in __instance.pawns)
				{
					if(pawn is VehiclePawn vehicle && vehicle.AllPawnsAboard.All(x => x.Downed))
					{
						__result = true;
						return false;
					}
				}
				__result = false;
				return false;
			}
			return true;
		}

		public static bool AllOwnersMentalBreakVehicle(Caravan __instance, ref bool __result)
		{
			if(__instance.PawnsListForReading.NotNullAndAny(x => x is VehiclePawn))
			{
				foreach(Pawn pawn in __instance.pawns)
				{
					if(pawn is VehiclePawn vehicle && vehicle.AllPawnsAboard.All(x => x.InMentalState))
					{
						__result = true;
						return false;
					}
				}
				__result = false;
				return false;
			}
			return true;
		}

		public static bool NoRestForVehicles(Caravan __instance, ref bool __result)
		{
			if(__instance.HasVehicle() && !__instance.PawnsListForReading.NotNullAndAny(x => !(x is VehiclePawn)))
			{
				__result = false;
				if(__instance.PawnsListForReading.NotNullAndAny(x => x is VehiclePawn vehicle && vehicle.navigationCategory == NavigationCategory.Manual))
				{
					__result = __instance.Spawned && (!__instance.pather.Moving || __instance.pather.nextTile != __instance.pather.Destination || !Caravan_PathFollower.IsValidFinalPushDestination(__instance.pather.Destination) ||
						Mathf.CeilToInt(__instance.pather.nextTileCostLeft / 1f) > 10000) && CaravanNightRestUtility.RestingNowAt(__instance.Tile);
				}
				else if(__instance.PawnsListForReading.NotNullAndAny(x => x is VehiclePawn vehicle && vehicle.navigationCategory == NavigationCategory.Opportunistic))
				{
					__result = __instance.Spawned && (!__instance.pather.Moving || __instance.pather.nextTile != __instance.pather.Destination || !Caravan_PathFollower.IsValidFinalPushDestination(__instance.pather.Destination) ||
						Mathf.CeilToInt(__instance.pather.nextTileCostLeft / 1f) > 10000) && CaravanNightRestUtility.RestingNowAt(__instance.Tile);
				}
				return false;
			}
			return true;
		}

		//REDO - Need better transpiler to retrieve all map pawns
		public static IEnumerable<CodeInstruction> CheckDefeatedWithVehiclesTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			for(int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if (instruction.Calls(AccessTools.Property(typeof(MapPawns), nameof(MapPawns.FreeColonists)).GetGetMethod()))
				{
					yield return new CodeInstruction(opcode: OpCodes.Callvirt, operand: AccessTools.Property(typeof(MapPawns), nameof(MapPawns.AllPawnsSpawned)).GetGetMethod());
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(CaravanHelper), nameof(CaravanHelper.GrabPawnsFromMapPawnsInVehicle)));
					instruction = instructionList[++i];
				}
				yield return instruction;
			}
		}

		//REDO
		public static bool ConcernNullThing(Thing th, Tale_DoublePawn __instance, ref bool __result)
		{
			if(th is null || __instance is null || __instance.secondPawnData is null || __instance.firstPawnData is null)
			{
				__result = false;
				return false;
			}
			return true;
		}

		public static IEnumerable<CodeInstruction> VehicleNeedsFillTabTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if (instruction.Calls(AccessTools.Property(typeof(Caravan), nameof(Caravan.PawnsListForReading)).GetGetMethod()))
				{
					yield return instruction;
					instruction = instructionList[++i];

					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(CaravanHelper), nameof(CaravanHelper.GrabPawnsIfVehicles)));
				}

				yield return instruction;
			}
		}

		public static IEnumerable<CodeInstruction> VehicleNeedsUpdateSizeTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			for(int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if(instruction.Calls(AccessTools.Property(typeof(Caravan), nameof(Caravan.PawnsListForReading)).GetGetMethod()))
				{
					yield return instruction;
					instruction = instructionList[++i];

					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(CaravanHelper), nameof(CaravanHelper.GrabPawnsIfVehicles)));
				}

				yield return instruction;
			}
		}
		
		public static void VehicleGearTabPawns(ref List<Pawn> __result)
		{
			if(__result.HasVehicle())
			{
				List<Pawn> pawns = new List<Pawn>();
				foreach(Pawn pawn in __result)
				{
					if(pawn is VehiclePawn vehicle)
					{
						pawns.AddRange(vehicle.AllPawnsAboard);
					}
					else
					{
						pawns.Add(pawn);
					}
				}
				__result = pawns;
			}
		}

		public static bool VehicleAllInventoryItems(Caravan caravan, ref List<Thing> __result)
		{
			if(caravan.HasVehicle())
			{
				List<Thing> inventoryItems = new List<Thing>();
				foreach (Pawn pawn in caravan.PawnsListForReading)
				{
					foreach (Thing t in pawn.inventory.innerContainer)
					{
						inventoryItems.Add(t);
					}
					if (pawn is VehiclePawn vehicle)
					{
						inventoryItems.AddRange(vehicle.AllPawnsAboard.SelectMany(p => p.inventory.innerContainer));
					}
					else
					{
						inventoryItems.AddRange(pawn.inventory.innerContainer);
					}
				}
				__result = inventoryItems;
				return false;
			}
			return true;
		}

		public static bool AerialVehicleInventoryItems(Pawn playerNegotiator, ref IEnumerable<Thing> __result)
		{
			AerialVehicleInFlight aerial = playerNegotiator.GetAerialVehicle();
			if (aerial != null)
			{
				List<Thing> inventoryThings = new List<Thing>();
				if (!__result.EnumerableNullOrEmpty())
				{
					inventoryThings.AddRange(__result);
				}
				foreach (Thing thing in aerial.vehicle.inventory.innerContainer)
				{
					inventoryThings.Add(thing);
				}
				List<Pawn> pawns = aerial.vehicle.AllPawnsAboard;
				for (int i = 0; i < pawns.Count; i++)
				{
					if (!CaravanUtility.IsOwner(pawns[i], aerial.Faction))
					{
						inventoryThings.Add(pawns[i]);
					}
				}
				__result = inventoryThings;
				return false;
			}
			return true;
		}

		public static IEnumerable<CodeInstruction> VehicleGiveThingInventoryTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if(instruction.Calls(AccessTools.Property(typeof(Caravan), nameof(Caravan.PawnsListForReading)).GetGetMethod()))
				{
					yield return instruction;
					i += 2;
					instruction = instructionList[i];

					yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(Ext_Caravan), nameof(Ext_Caravan.GrabPawnsFromVehicleCaravanSilentFail)));
				}

				yield return instruction;
			}
		}

		public static bool VehicleHealthTabPawns(ref List<Pawn> __result)
		{
			if (Find.WorldSelector.SingleSelectedObject is Caravan caravan && caravan.HasVehicle())
			{
				List<Pawn> pawns = new List<Pawn>();
				foreach (Pawn p in caravan.PawnsListForReading)
				{
					if (p is VehiclePawn vehicle)
					{
						pawns.AddRange(vehicle.AllPawnsAboard);
					}
					else
					{
						pawns.Add(p);
					}
				}
				__result = pawns;
				return false;
			}
			return true;
		}

		public static bool VehicleSocialTabPawns(ref List<Pawn> __result)
		{
			if(Find.WorldSelector.SingleSelectedObject is VehicleCaravan caravan && caravan.HasVehicle())
			{
				List<Pawn> pawns = new List<Pawn>();
				foreach(Pawn pawn in caravan.PawnsListForReading)
				{
					if(pawn is VehiclePawn vehicle)
					{
						pawns.AddRange(vehicle.AllPawnsAboard);
					}
					else
					{
						pawns.Add(pawn);
					}
				}
				__result = pawns;
				return false;
			}
			return true;
		}

		public static bool FindPawnInVehicleWithBestStat(Caravan caravan, StatDef stat, ref Pawn __result)
		{
			if (caravan.HasVehicle())
			{
				float num = -1f;
				foreach (Pawn pawn in caravan.PawnsListForReading)
				{
					if (pawn is VehiclePawn vehicle)
					{
						foreach (Pawn innerPawn in vehicle.AllPawnsAboard.Where(p => !p.Dead && !p.Downed && !p.InMentalState && !stat.Worker.IsDisabledFor(p)))
						{
							float statValue = innerPawn.GetStatValue(stat, true);
							if (__result is null || statValue > num)
							{
								__result = innerPawn;
								num = statValue;
							}
						}
					}
					else if (!stat.Worker.IsDisabledFor(pawn))
					{
						float statValue = pawn.GetStatValue(stat, true);
						if (__result is null || statValue > num)
						{
							__result = pawn;
							num = statValue;
						}
					}
					
				}
				return false;
			}
			return true;
		}

		public static void ContainsPawnInVehicle(Pawn p, Caravan __instance, ref bool __result)
		{
			if(!__result)
			{
				__result = __instance.PawnsListForReading.Any(v => v is VehiclePawn vehicle && vehicle.AllPawnsAboard.Contains(p));
			}
		}

		public static void IsOwnerOfVehicle(Pawn p, Caravan __instance, ref bool __result)
		{
			if(!__result)
			{
				__result = __instance.PawnsListForReading.Any(v => v is VehiclePawn vehicle && vehicle.AllPawnsAboard.Contains(p) && CaravanUtility.IsOwner(p, __instance.Faction));
			}
		}

		//REDO?
		public static void UnloadVehicleOfferGifts(VehicleCaravan caravan)
		{
			if(caravan.HasVehicle())
			{
				CaravanHelper.ToggleDocking(caravan, true);
			}
		}

		public static bool GiveSoldThingToAerialVehicle(Thing toGive, int countToGive, Pawn playerNegotiator, Settlement ___settlement)
		{
			AerialVehicleInFlight aerial = playerNegotiator.GetAerialVehicle();
			if (aerial != null)
			{
				Thing thing = toGive.SplitOff(countToGive);
				thing.PreTraded(TradeAction.PlayerBuys, playerNegotiator, ___settlement);
				if (thing is Pawn pawn && pawn.RaceProps.Humanlike)
				{
					VehicleHandler handler = aerial.vehicle.GetAllHandlersMatch(null).FirstOrDefault();
					aerial.vehicle.GiveLoadJob(pawn, handler);
					aerial.vehicle.Notify_Boarded(pawn);
					return false;
				}
				if (!aerial.vehicle.inventory.innerContainer.TryAdd(thing, true))
				{
					Log.Error("Could not add sold thing to inventory.");
					thing.Destroy(DestroyMode.Vanish);
				}
				return false;
			}
			return true;
		}

		public static IEnumerable<CodeInstruction> GiveSoldThingToVehicleTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			for(int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if (instruction.opcode == OpCodes.Ldnull && instructionList[i + 1].opcode == OpCodes.Ldnull)
				{
					yield return new CodeInstruction(opcode: OpCodes.Ldarg_3);
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(CaravanUtility), nameof(CaravanUtility.GetCaravan)));
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(Ext_Caravan), nameof(Ext_Caravan.GrabPawnsFromVehicleCaravanSilentFail)));
					instruction = instructionList[++i];
				}

				yield return instruction;
			}
		}

		public static void IsParentCaravanMember(Pawn pawn, ref bool __result)
		{
			if (pawn.ParentHolder is VehicleHandler handler && handler.vehicle != null)
			{
				__result = handler.vehicle.IsCaravanMember();
			}
		}

		public static bool RandomVehicleOwner(Caravan caravan, ref Pawn __result)
		{
			if(caravan.HasVehicle())
			{
				
				__result = (from p in caravan.GrabPawnsFromVehicleCaravanSilentFail()
							where caravan.IsOwner(p)
							select p).RandomElement();
				return false;
			}
			return true;
		}

		//REDO?
		public static bool TrySatisfyVehiclePawnsNeeds(Pawn pawn, Caravan_NeedsTracker __instance)
		{
			if(pawn is VehiclePawn)
			{
				if (pawn.needs?.AllNeeds.NullOrEmpty() ?? true)
					return false;
			}
			return true;
		}
	}
}
