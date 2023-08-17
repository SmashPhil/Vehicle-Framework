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


namespace Vehicles
{
	internal class CaravanHandling : IPatchCategory
	{
		private static MethodInfo addAllTradeablesMethod;

		private static List<Pawn> tmpCaravanPawns = new List<Pawn>();

		public void PatchMethods()
		{
			addAllTradeablesMethod = AccessTools.Method(typeof(TradeDeal), "AddToTradeables");

			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Dialog_FormCaravan), "TryReformCaravan"),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(ConfirmLeaveVehiclesOnReform)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(MassUtility), nameof(MassUtility.Capacity)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(CapacityOfVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(MassUtility), nameof(MassUtility.CanEverCarryAnything)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(CanCarryIfVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CollectionsMassCalculator), nameof(CollectionsMassCalculator.Capacity), parameters: new Type[] { typeof(List<ThingCount>), typeof(StringBuilder) }),
				transpiler: new HarmonyMethod(typeof(CaravanHandling),
				nameof(PawnCapacityInVehicleTranspiler)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "FillTab"),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(FillTabVehicleCaravan)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "DoPeopleAndAnimals"), 
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(DoPeopleAnimalsAndVehicle)));
			VehicleHarmony.Patch(original: AccessTools.PropertyGetter(typeof(Alert_CaravanIdle), "IdleCaravans"),
				postfix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(IdleVehicleCaravans)));

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

			VehicleHarmony.Patch(original: AccessTools.PropertyGetter(typeof(Caravan), nameof(Caravan.AllOwnersDowned)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(AllOwnersDownedVehicle)));
			VehicleHarmony.Patch(original: AccessTools.PropertyGetter(typeof(Caravan), nameof(Caravan.AllOwnersHaveMentalBreak)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(AllOwnersMentalBreakVehicle)));
			VehicleHarmony.Patch(original: AccessTools.PropertyGetter(typeof(Caravan), nameof(Caravan.NightResting)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(NoRestForVehicles)));
			VehicleHarmony.Patch(original: AccessTools.PropertyGetter(typeof(Caravan), nameof(Caravan.PawnsListForReading)),
				postfix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(InternalPawnsIncludedInList)));
			VehicleHarmony.Patch(original: AccessTools.PropertyGetter(typeof(Caravan), nameof(Caravan.TicksPerMove)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(VehicleCaravanTicksPerMove)));
			VehicleHarmony.Patch(original: AccessTools.PropertyGetter(typeof(Caravan), nameof(Caravan.TicksPerMoveExplanation)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(VehicleCaravanTicksPerMoveExplanation)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(ForagedFoodPerDayCalculator), nameof(ForagedFoodPerDayCalculator.GetBaseForagedNutritionPerDay)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(GetBaseForagedNutritionPerDayInVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(TilesPerDayCalculator), nameof(TilesPerDayCalculator.ApproxTilesPerDay), new Type[] { typeof(Caravan), typeof(StringBuilder) }),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(ApproxTilesForVehicles)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Caravan), nameof(Caravan.ContainsPawn)),
				postfix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(ContainsPawnInVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Caravan), nameof(Caravan.AddPawn)),
				postfix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(AddPawnInVehicleCaravan)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Caravan), nameof(Caravan.RemovePawn)),
				postfix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(RemovePawnInVehicleCaravan)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Caravan), nameof(Caravan.IsOwner)),
				postfix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(IsOwnerOfVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(SettlementDefeatUtility), nameof(SettlementDefeatUtility.CheckDefeated)),
				transpiler: new HarmonyMethod(typeof(CaravanHandling),
				nameof(CheckDefeatedWithVehiclesTranspiler)));

			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Tale_DoublePawn), nameof(Tale_DoublePawn.Concerns)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(ConcernNullThing)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Settlement_TraderTracker), nameof(Settlement_TraderTracker.ColonyThingsWillingToBuy)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(AerialVehicleInventoryItems)));
			VehicleHarmony.Patch(original: AccessTools.PropertyGetter(typeof(Tradeable), nameof(Tradeable.Interactive)),
				postfix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(AerialVehicleSlaveTradeRoomCheck)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Dialog_Trade), "CountToTransferChanged"),
				postfix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(AerialVehicleCountPawnsToTransfer)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanInventoryUtility), nameof(CaravanInventoryUtility.FindPawnToMoveInventoryTo)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(FindVehicleToMoveInventoryTo)));
			VehicleHarmony.Patch(original: AccessTools.Property(typeof(WITab_Caravan_Health), "Pawns").GetGetMethod(nonPublic: true),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(VehicleHealthTabPawns)));
			VehicleHarmony.Patch(original: AccessTools.Property(typeof(WITab_Caravan_Social), "Pawns").GetGetMethod(nonPublic: true),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(VehicleSocialTabPawns)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanNeedsTabUtility), nameof(CaravanNeedsTabUtility.DoRows)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(NoVehiclesNeedNeeds)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanNeedsTabUtility), nameof(CaravanNeedsTabUtility.GetSize)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(NoVehiclesNeedNeeds)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(BestCaravanPawnUtility), nameof(BestCaravanPawnUtility.FindBestNegotiator)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(FindBestNegotiatorInVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanArrivalAction_OfferGifts), nameof(CaravanArrivalAction_OfferGifts.Arrived)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(UnloadVehicleOfferGifts)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Settlement_TraderTracker), nameof(Settlement_TraderTracker.GiveSoldThingToPlayer)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(GiveSoldThingToAerialVehicle)),
				transpiler: new HarmonyMethod(typeof(CaravanHandling),
				nameof(GiveSoldThingToVehicleTranspiler)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(TradeDeal), "AddAllTradeables"),
				postfix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(AddAllTradeablesFromAerialVehicle)));

			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Caravan_NeedsTracker), "TrySatisfyPawnNeeds"),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(TrySatisfyVehiclePawnsNeeds)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanTendUtility), nameof(CaravanTendUtility.CheckTend)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(CheckTendInVehicles)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanUtility), nameof(CaravanUtility.GetCaravan)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(GetParentCaravan)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanUtility), nameof(CaravanUtility.RandomOwner)),
				prefix: new HarmonyMethod(typeof(CaravanHandling),
				nameof(RandomVehicleOwner)));

			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanArrivalAction_Trade), nameof(CaravanArrivalAction_Trade.CanTradeWith)),
                postfix: new HarmonyMethod(typeof(CaravanHandling), 
				nameof(NoTradingUndocked)));
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
			if (___map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).HasVehicle())
			{
				List<Pawn> pawns = TransferableUtility.GetPawnsFromTransferables(___transferables);
				List<Pawn> correctedPawns = pawns.Where(p => !(p is VehiclePawn)).ToList();
				string vehicles = "";
				foreach(Pawn pawn in pawns.Where(p => p is VehiclePawn))
				{
					vehicles += pawn.LabelShort;
				}
				
				Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("VF_LeaveVehicleBehindCaravan".Translate(vehicles), delegate ()
				{
					if (!(bool)AccessTools.Method(typeof(Dialog_FormCaravan), "CheckForErrors").Invoke(__instance, new object[] { correctedPawns }))
					{
						return;
					}
					AccessTools.Method(typeof(Dialog_FormCaravan), "AddItemsFromTransferablesToRandomInventories").Invoke(__instance, new object[] { correctedPawns });
					VehicleCaravan caravan = CaravanHelper.ExitMapAndCreateVehicleCaravan(correctedPawns, Faction.OfPlayer, __instance.CurrentTile, __instance.CurrentTile, ___destinationTile, false);
					___map.Parent.CheckRemoveMapNow();
					TaggedString taggedString = "MessageReformedCaravan".Translate();
					if (caravan.vPather.Moving && caravan.vPather.ArrivalAction != null)
					{
						taggedString += " " + "MessageFormedCaravan_Orders".Translate() + ": " + caravan.vPather.ArrivalAction.Label + ".";
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
		public static bool CapacityOfVehicle(Pawn p, ref float __result, StringBuilder explanation = null)
		{
			if (p is VehiclePawn vehicle)
			{
				__result = vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity);
				if (explanation != null)
				{
					if (explanation.Length > 0)
					{
						explanation.AppendLine();
					}
					explanation.Append($"  - {vehicle.LabelShortCap}: {__result.ToStringMassOffset()}");
				}
				return false;
			}
			return true;
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
			if (p is VehiclePawn)
			{
				__result = true;
			}
			return !__result;
		}

		public static IEnumerable<CodeInstruction> PawnCapacityInVehicleTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
		{
			List<CodeInstruction> instructionList = instructions.ToList();
			MethodInfo capacityMethod = AccessTools.Method(typeof(MassUtility), nameof(MassUtility.Capacity));
			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if (instruction.Calls(capacityMethod))
				{
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(CaravanHandling), nameof(PawnCapacityInVehicle)));
					instruction = instructionList[++i]; //CALL : MassUtility.Capacity
				}

				yield return instruction;
			}
		}

		private static float PawnCapacityInVehicle(Pawn pawn, StringBuilder explanation)
		{
			if (pawn.IsInVehicle() || CaravanHelper.assignedSeats.ContainsKey(pawn))
			{
				return 0; //pawns in vehicles or assigned to vehicle don't contribute to capacity
			}
			return MassUtility.Capacity(pawn, explanation);
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
				object[] mSargs = new object[] { inRect, "VF_Vehicles".Translate().ToStringSafe(), pawnsCountLabelShip, curY, null };
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
			if (__instance is VehicleCaravan caravan)
			{
				foreach (Pawn pawn in caravan.pawns)
				{
					if (caravan.IsOwner(pawn) && !pawn.Downed)
					{
						__result = false;
						return false;
					}
					if (pawn is VehiclePawn vehicle)
					{
						foreach (Pawn innerPawn in vehicle.AllPawnsAboard)
						{
							if (__instance.IsOwner(innerPawn) && !innerPawn.Downed)
							{
								__result = false;
								return false;
							}
						}
					}
				}
				__result = true;
				return false;
			}
			return true;
		}

		public static bool AllOwnersMentalBreakVehicle(Caravan __instance, ref bool __result)
		{
			if (__instance is VehicleCaravan caravan)
			{
				foreach (Pawn pawn in caravan.pawns)
				{
					if (caravan.IsOwner(pawn) && !pawn.InMentalState)
					{
						__result = false;
						return false;
					}
					if (pawn is VehiclePawn vehicle)
					{
						foreach (Pawn innerPawn in vehicle.AllPawnsAboard)
						{
							if (__instance.IsOwner(innerPawn) && !innerPawn.InMentalState)
							{
								__result = false;
								return false;
							}
						}
					}
				}
				__result = true;
				return false;
			}
			return true;
		}

		public static bool NoRestForVehicles(Caravan __instance, ref bool __result)
		{
			if (__instance is VehicleCaravan caravan)
			{
				__result = VehicleCaravanPathingHelper.ShouldRestAt(caravan, caravan.Tile);
				return false;
			}
			return true;
		}

		public static List<Pawn> InternalPawnsIncludedInList(List<Pawn> __result, Caravan __instance)
		{
			if (__instance is VehicleCaravan vehicleCaravan)
			{
				tmpCaravanPawns.Clear();
				tmpCaravanPawns.AddRange(__result);
				foreach (VehiclePawn vehicle in vehicleCaravan.Vehicles)
				{
					tmpCaravanPawns.AddRange(vehicle.AllPawnsAboard);
				}
				return tmpCaravanPawns;
			}
			return __result;
		}

		public static bool VehicleCaravanTicksPerMove(ref int __result, Caravan __instance)
		{
			if (__instance is VehicleCaravan vehicleCaravan)
			{
				__result = vehicleCaravan.TicksPerMove;
				return false;
			}
			return true;
		}

		public static bool VehicleCaravanTicksPerMoveExplanation(ref string __result, Caravan __instance)
		{
			if (__instance is VehicleCaravan vehicleCaravan)
			{
				__result = vehicleCaravan.TicksPerMoveExplanation;
				return false;
			}
			return true;
		}

		public static bool GetBaseForagedNutritionPerDayInVehicle(Pawn p, out bool skip, ref float __result)
		{
			skip = false;
			if (p.IsInVehicle() || CaravanHelper.assignedSeats.ContainsKey(p))
			{
				skip = true;
				__result = 0;
				return false;
			}
			return true;
		}

		public static bool ApproxTilesForVehicles(Caravan caravan, ref float __result, StringBuilder explanation = null)
		{
			if (caravan is VehicleCaravan vehicleCaravan)
			{
				__result = VehicleCaravanTicksPerMoveUtility.ApproxTilesPerDay(vehicleCaravan, explanation);
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

		public static bool AerialVehicleInventoryItems(Pawn playerNegotiator, ref IEnumerable<Thing> __result)
		{
			AerialVehicleInFlight aerialVehicle = playerNegotiator.GetAerialVehicle();
			if (aerialVehicle != null)
			{
				List<Thing> inventoryThings = new List<Thing>();
				if (!__result.EnumerableNullOrEmpty())
				{
					inventoryThings.AddRange(__result);
				}
				foreach (Thing thing in aerialVehicle.vehicle.inventory.innerContainer)
				{
					inventoryThings.Add(thing);
				}
				List<Pawn> pawns = aerialVehicle.vehicle.AllPawnsAboard;
				for (int i = 0; i < pawns.Count; i++)
				{
					if (!CaravanUtility.IsOwner(pawns[i], aerialVehicle.Faction))
					{
						inventoryThings.Add(pawns[i]);
					}
				}
				__result = inventoryThings;
				return false;
			}
			return true;
		}

		public static void AerialVehicleSlaveTradeRoomCheck(ref bool __result, Tradeable __instance)
		{
			if (__instance.AnyThing is Pawn pawn && pawn.RaceProps.Humanlike && __instance.CountToTransfer == 0)
			{
				Pawn negotiator = TradeSession.playerNegotiator;
				if (negotiator.GetAerialVehicle() is AerialVehicleInFlight aerialVehicle)
				{
					__result &= CaravanHelper.CanFitInVehicle(aerialVehicle);
				}
			}
		}

		public static void AerialVehicleCountPawnsToTransfer(List<Tradeable> ___cachedTradeables)
		{
			CaravanHelper.CountPawnsBeingTraded(___cachedTradeables);
		}

		public static bool FindVehicleToMoveInventoryTo(ref Pawn __result, Thing item, List<Pawn> candidates, List<Pawn> ignoreCandidates, Pawn currentItemOwner = null)
		{
			if (candidates.HasVehicle())
			{
				if (candidates.Where(pawn => pawn is VehiclePawn && (ignoreCandidates == null || !ignoreCandidates.Contains(pawn))
					&& currentItemOwner != pawn && !MassUtility.IsOverEncumbered(pawn)).TryRandomElement(out __result))
				{
					return false;
				}
			}
			return true;
		}

		public static bool VehicleHealthTabPawns(ref List<Pawn> __result)
		{
			if (Find.WorldSelector.SingleSelectedObject is Caravan caravan && caravan.HasVehicle())
			{
				List<Pawn> pawns = new List<Pawn>();
				foreach (Pawn p in caravan.PawnsListForReading)
				{
					if (!(p is VehiclePawn))
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
				foreach (Pawn p in caravan.PawnsListForReading)
				{
					if (!(p is VehiclePawn))
					{
						pawns.Add(p);
					}
				}
				__result = pawns;
				return false;
			}
			return true;
		}

		public static void NoVehiclesNeedNeeds(ref List<Pawn> pawns)
		{
			pawns.RemoveAll(pawn => pawn is VehiclePawn);
		}

		public static bool FindBestNegotiatorInVehicle(Caravan caravan, ref Pawn __result, Faction negotiatingWith = null, TraderKindDef trader = null)
		{
			if (caravan is VehicleCaravan vehicleCaravan)
			{
				__result = WorldHelper.FindBestNegotiator(vehicleCaravan, faction: negotiatingWith, trader: trader);
				return false;
			}
			return true;
		}

		public static void ContainsPawnInVehicle(Pawn p, Caravan __instance, ref bool __result)
		{
			if (!__result)
			{
				__result = __instance.PawnsListForReading.Any(v => v is VehiclePawn vehicle && vehicle.AllPawnsAboard.Contains(p));
			}
		}

		public static void AddPawnInVehicleCaravan(Pawn p, Caravan __instance)
		{
			if (__instance is VehicleCaravan vehicleCaravan)
			{
				vehicleCaravan.RecacheVehicles();
			}
		}

		public static void RemovePawnInVehicleCaravan(Pawn p, Caravan __instance)
		{
			if (__instance is VehicleCaravan vehicleCaravan)
			{
				vehicleCaravan.RecacheVehicles();
			}
		}

		public static void IsOwnerOfVehicle(Pawn p, Caravan __instance, ref bool __result)
		{
			if (!__result)
			{
				__result = p.GetVehicle() is VehiclePawn vehicle && __instance.pawns.Contains(vehicle) && CaravanUtility.IsOwner(p, __instance.Faction);
			}
		}

		//REDO?
		public static void UnloadVehicleOfferGifts(VehicleCaravan caravan)
		{
			if (caravan.HasVehicle())
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
					VehicleHandler handler = aerial.vehicle.NextAvailableHandler(HandlingTypeFlags.None);
					if (handler == null)
					{
						Log.Error($"Unable to locate available handler for {toGive}. Squeezing into other role to avoid aborted trade.");
						handler = aerial.vehicle.NextAvailableHandler();
						handler ??= aerial.vehicle.handlers.RandomElementWithFallback(fallback: null);

						if (handler == null)
						{
							Log.Error($"Unable to find other role to squeeze {pawn} into. Tossing into inventory.");
							return true;
						}
					}

					if (pawn.Spawned)
					{
						aerial.vehicle.GiveLoadJob(pawn, handler);
						aerial.vehicle.Notify_Boarded(pawn);
					}
					else if (!pawn.IsInVehicle())
					{
						aerial.vehicle.Notify_BoardedCaravan(pawn, handler.handlers);
					}
					return false;
				}
				if (aerial.vehicle.AddOrTransfer(thing) <= 0)
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

		public static void AddAllTradeablesFromAerialVehicle(TradeDeal __instance)
		{
			if (TradeSession.playerNegotiator.GetAerialVehicle() is AerialVehicleInFlight)
			{
				foreach (Thing thing in TradeSession.trader.ColonyThingsWillingToBuy(TradeSession.playerNegotiator))
				{
					if (TradeUtility.PlayerSellableNow(thing, TradeSession.trader))
					{
						addAllTradeablesMethod.Invoke(__instance, new object[] { thing, Transactor.Colony });
					}
				}
			}
		}

		public static bool GetParentCaravan(Pawn pawn, ref Caravan __result)
		{
			if (pawn.ParentHolder is VehicleHandler handler && handler.vehicle.GetCaravan() is VehicleCaravan caravan)
			{
				__result = caravan;
                return false;
            }
            return true;
        }

		public static bool RandomVehicleOwner(Caravan caravan, ref Pawn __result)
		{
			if (caravan.HasVehicle())
			{
				__result = caravan.GrabPawnsFromVehicleCaravanSilentFail().Where(p => caravan.IsOwner(p)).RandomElement();
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

		public static bool CheckTendInVehicles(Caravan caravan)
		{
			if (caravan is VehicleCaravan vehicleCaravan)
			{
				var pawns = vehicleCaravan.PawnsListForReading;
				for (int i = 0; i < pawns.Count; i++)
				{
					Pawn pawn = pawns[i];
					if (IsValidDoctorFor(pawn, null, caravan) && pawn.IsHashIntervalTick(1250))
					{
						CaravanTendUtility.TryTendToAnyPawn(caravan);
					}
				}
				return false;
			}
			return true;
		}

		private static bool IsValidDoctorFor(Pawn doctor, Pawn patient, Caravan caravan)
		{
			return doctor.RaceProps.Humanlike && caravan.IsOwner(doctor) && (doctor != patient || (doctor.IsColonist && doctor.playerSettings.selfTend)) && 
				!doctor.Downed && !doctor.InMentalState && (doctor.story == null || !doctor.WorkTypeIsDisabled(WorkTypeDefOf.Doctor));
		}

		public static void NoTradingUndocked(Caravan caravan, Settlement settlement, ref FloatMenuAcceptanceReport __result)
        {
            if (__result.Accepted && caravan.HasBoat() && !caravan.PawnsListForReading.NotNullAndAny(p => !p.IsBoat()))
            {
                __result = false;
            }
        }
    }
}
