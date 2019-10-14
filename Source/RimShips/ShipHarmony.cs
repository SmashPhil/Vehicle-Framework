using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using RimShips.AI;
using RimShips.Defs;
using RimShips.Build;
using RimShips.Jobs;
using RimShips.Lords;
using RimShips.UI;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace RimShips
{
    [StaticConstructorOnStartup]
    internal static class ShipHarmony
    {
        static ShipHarmony()
        {
            var harmony = HarmonyInstance.Create("rimworld.rimships.comps.ship");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            //HarmonyInstance.DEBUG = true;

            #region Functions

            //Health 
            harmony.Patch(original: AccessTools.Method(typeof(HealthUtility), name: nameof(HealthUtility.GetGeneralConditionLabel)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(ReplaceConditionLabel)));
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_HealthTracker), name: "ShouldBeDowned"),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(ShipShouldBeDowned)));
            harmony.Patch(original: AccessTools.Method(typeof(PawnDownedWiggler), name: nameof(PawnDownedWiggler.WigglerTick)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(ShipShouldWiggle)));

            //Rendering
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_RotationTracker), name: nameof(Pawn_RotationTracker.UpdateRotation)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(UpdateShipRotation)));

            //Gizmos
            harmony.Patch(original: AccessTools.Method(typeof(JobDriver_Wait), name: "CheckForAutoAttack"),
                prefix: new HarmonyMethod(type: typeof(JobDriver_Wait),
                name: nameof(CheckForShipAttack)));
            harmony.Patch(original: AccessTools.Method(typeof(PawnAttackGizmoUtility), name: "ShouldUseMeleeAttackGizmo"),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(ShipMeleeAttacksNullified)));

            //Pathing
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_PathFollower), name: nameof(Pawn_PathFollower.StartPath)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(CanShipMove)));

            //?
            harmony.Patch(original: AccessTools.Method(type: typeof(Pawn_PathFollower), name: "TryEnterNextPathCell"), 
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(TryEnterNextCellShip)));

            harmony.Patch(original: AccessTools.Method(type: typeof(MapGenerator), name: nameof(MapGenerator.GenerateMap)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(GenerateMapExtension)));
            harmony.Patch(original: AccessTools.Method(typeof(ReachabilityImmediate), parameters: new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(Map),
                    typeof(PathEndMode), typeof(Pawn) }, name: nameof(ReachabilityImmediate.CanReachImmediate)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(CanReachShipImmediate)));
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_PathFollower), name: "GenerateNewPath"),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(GenerateNewShipPath)));
            harmony.Patch(original: AccessTools.Method(type: typeof(PathFinder), parameters: new Type[] { typeof(IntVec3), typeof(LocalTargetInfo)
                    , typeof(TraverseParms), typeof(PathEndMode)}, name: nameof(PathFinder.FindPath)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(FindPathShips)));

            //Jobs
            harmony.Patch(original: AccessTools.Method(typeof(JobUtility), name: nameof(JobUtility.TryStartErrorRecoverJob)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(ShipErrorRecoverJob)));
            harmony.Patch(original: AccessTools.Method(typeof(JobGiver_Wander), name: "TryGiveJob"),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(ShipsDontWander)));
            
            //Caravan
            harmony.Patch(original: AccessTools.Method(typeof(CaravanUIUtility), name: nameof(CaravanUIUtility.AddPawnsSections)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(AddShipsSections)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanFormingUtility), name: nameof(CaravanFormingUtility.StartFormingCaravan)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(StartFormingCaravanForShips)));
            harmony.Patch(original: AccessTools.Method(typeof(Dialog_FormCaravan), name: "DoBottomButtons"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(DoBottomButtonsTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(MassUtility), name: nameof(MassUtility.CanEverCarryAnything)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(CanCarryIfShip)));
            harmony.Patch(original: AccessTools.Method(type: typeof(CaravanFormingUtility), name: nameof(CaravanFormingUtility.RemovePawnFromCaravan)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(RemovePawnAddShip)));
            harmony.Patch(original: AccessTools.Method(type: typeof(CaravanFormingUtility), name: nameof(CaravanFormingUtility.GetFormAndSendCaravanLord)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(GetShipAndSendCaravanLord)));
            harmony.Patch(original: AccessTools.Method(type: typeof(CaravanFormingUtility), name: nameof(CaravanFormingUtility.IsFormingCaravan)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(IsFormingCaravanShip)));
            harmony.Patch(original: AccessTools.Method(type: typeof(TransferableUtility), name: nameof(TransferableUtility.CanStack)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(CanStackShip)));
            harmony.Patch(original: AccessTools.Property(type: typeof(JobDriver_PrepareCaravan_GatherItems), name: "Transferables").GetGetMethod(nonPublic: true),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(TransferablesShip))); //DOUBLE CHECK
            harmony.Patch(original: AccessTools.Method(type: typeof(FloatMenuMakerMap), name: "AddHumanlikeOrders"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(AddHumanLikeOrdersTranspiler)));
            harmony.Patch(original: AccessTools.Method(type: typeof(LordToil_PrepareCaravan_GatherAnimals), name: nameof(LordToil_PrepareCaravan_GatherAnimals.UpdateAllDuties)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(UpdateDutyOfShip)));

            //TO DO
            harmony.Patch(original: AccessTools.Method(type: typeof(PathGrid), parameters: new Type[] { typeof(IntVec3)} , 
                        name: nameof(PathGrid.Walkable)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(Walkable_RestrictWater)));

            //Draftable
            harmony.Patch(original: AccessTools.Property(typeof(Pawn_DraftController), name: nameof(Pawn_DraftController.Drafted)).GetSetMethod(),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(DraftedShipsCanMove)));
            harmony.Patch(original: AccessTools.Property(typeof(Pawn), name: nameof(Pawn.IsColonistPlayerControlled)).GetGetMethod(),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(IsShipDraftable)));
            //  Possibly change draft gizmo  

            //Construction
            harmony.Patch(original: AccessTools.Method(type: typeof(Frame), name: nameof(Frame.CompleteConstruction)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(CompleteConstructionShip)));

            //Transpilers
            /*harmony.Patch(original: AccessTools.Method(type: typeof(PathFinder), parameters: new Type[] { typeof(IntVec3), typeof(LocalTargetInfo)
                    , typeof(TraverseParms), typeof(PathEndMode)}, name: "FindPath"), prefix: null, postfix: null, 
                transpiler: new HarmonyMethod(type: typeof(ShipHarmony), 
                name: nameof(FindPathShips)));*/

            #endregion Functions
        }

        #region HealthStats

        public static bool ReplaceConditionLabel(ref string __result, Pawn pawn, bool shortVersion = false)
        {
            if (pawn != null)
            {
                CompShips ship = pawn.GetComp<CompShips>();
                if (IsShip(pawn))
                {
                    if (ship.movementStatus == ShipMovementStatus.Offline && !pawn.Dead)
                    {
                        if (ship.beached)
                        {
                            __result = ship.Props.healthLabel_Beached;
                        }
                        else
                        {
                            __result = ship.Props.healthLabel_Immobile;
                        }

                        return false;
                    }
                    if (pawn.Dead)
                    {
                        __result = ship.Props.healthLabel_Dead;
                        return false;
                    }
                    if (pawn.health.summaryHealth.SummaryHealthPercent < 0.95)
                    {
                        __result = ship.Props.healthLabel_Injured;
                        return false;
                    }
                    __result = ship.Props.healthLabel_Healthy;
                    return false;
                }
            }
            return true;
        }

        public static bool ShipShouldBeDowned(Pawn_HealthTracker __instance, ref bool __result)
        {
            Pawn pawn = (Pawn)AccessTools.Field(typeof(Pawn_HealthTracker), "pawn").GetValue(__instance);
            if (pawn != null)
            {
                if (IsShip(pawn))
                {
                    if (!pawn.GetComp<CompShips>().Props.downable)
                    {
                        __result = false;
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool ShipShouldWiggle(PawnDownedWiggler __instance)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn != null)
            {
                if (IsShip(pawn))
                {
                    if (!pawn.GetComp<CompShips>().Props.movesWhenDowned) return false;
                }
            }
            return true;
        }

        #endregion HealthStats

        #region Rendering
        public static bool UpdateShipRotation(Pawn_RotationTracker __instance)
        {
            if (Traverse.Create(__instance).Field("pawn").GetValue<Pawn>() is Pawn shipPawn &&
                IsShip(shipPawn))
            {
                if (shipPawn.Destroyed || shipPawn.jobs.HandlingFacing)
                {
                    return false;
                }
                if (shipPawn.pather.Moving)
                {
                    if (shipPawn.pather.curPath == null || shipPawn.pather.curPath.NodesLeftCount < 1)
                    {
                        return false;
                    }
                    FaceShipAdjacentCell(shipPawn.pather.nextCell, shipPawn);
                }
                else
                {
                    //Stance busy code here

                    if (shipPawn.jobs.curJob != null)
                    {
                        //LocalTargetInfo target = shipPawn.CurJob.GetTarget(shipPawn.jobs.curDriver.rotateToFace);
                        //Face Target here
                    }
                    if (shipPawn.Drafted)
                    {
                        //Ship Pawn Rotation stays the same
                    }

                }
                return false;
            }
            return true;
        }
        #endregion Rendering

        #region Drafting
        public static bool DraftedShipsCanMove(Pawn_DraftController __instance, bool value)
        {
            if (IsShip(__instance?.pawn))
            {
                if (value && !__instance.Drafted)
                {
                    if (!__instance.pawn.GetComp<CompShips>().CanMove)
                    {
                        Messages.Message("CompShips_CannotMove".Translate(), MessageTypeDefOf.RejectInput);
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool IsShipDraftable(Pawn __instance, ref bool __result)
        {
            if (__instance.Spawned && IsShip(__instance))
            {
                if (__instance.drafter is null && __instance.GetComp<CompShips>().movementStatus != ShipMovementStatus.Offline)
                {
                    __instance.drafter = new Pawn_DraftController(__instance);
                }
                __result = true;
                return false;
            }
            return true;
        }
        #endregion Drafting

        #region Gizmos
        public static bool CheckForShipAttack(JobDriver_Wait __instance)
        {
            if (__instance.pawn?.GetComp<CompShips>().weaponStatus == ShipWeaponStatus.Offline)
            {
                return false;
            }
            return true;
        }
        public static bool ShipMeleeAttacksNullified(Pawn pawn, bool __result)
        {
            if (IsShip(pawn) && pawn.Drafted)
            {
                __result = false;
                return false;
            }
            return true;
        }
        #endregion Gizmos

        #region Pathing
        public static bool CanShipMove(LocalTargetInfo dest, PathEndMode peMode, Pawn_PathFollower __instance, ref Pawn ___pawn, ref PathEndMode ___peMode,
            LocalTargetInfo ___destination, ref bool ___moving)
        {
            if (disabled) return true;
            if (IsShip(___pawn))
            {
                Log.Message("Starting path for pawn: " + ___pawn.LabelShort);
                dest = (LocalTargetInfo)GenPathShip.ResolvePathMode(___pawn, dest.ToTargetInfo(___pawn.Map), ref peMode, mapE);
                if (dest.HasThing && dest.ThingDestroyed)
                {
                    Log.Error(___pawn + " pathing to destroyed thing " + dest.Thing, false);
                    PatherFailedHelper(__instance, ___pawn);
                    return false;
                }
                Log.Message("cp1"); // RESUME WORK HERE
                //Add Building and Position Recoverable extras
                if (!GenGridShips.Walkable(___pawn.Position, mapE))
                {
                    Log.Message("not walkable");
                    return false;
                }
                Log.Message("cp2");
                if (__instance.Moving && __instance.curPath != null && __instance.Destination == dest && ___peMode == peMode)
                {
                    Log.Message("already moving");
                    return false;
                }
                Log.Message("cp3");
                //Log.Message("Check: " + (mapE.getShipReachability));
                if (!mapE.getShipReachability.CanReachShip(___pawn.Position, dest, peMode, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
                {
                    Log.Message("not reachable");
                    PatherFailedHelper(__instance, ___pawn);
                    return false;
                }
                Log.Message("cp4");
                ___peMode = peMode;
                ___destination = dest;
                if ((GenGridShips.Walkable(__instance.nextCell, mapE) || __instance.WillCollideWithPawnOnNextPathCell()) || __instance.nextCellCostLeft
                    == __instance.nextCellCostTotal)
                {
                    __instance.ResetToCurrentPosition();
                }
                Log.Message("cp5");
                PawnDestinationReservationManager.PawnDestinationReservation pawnDestinationReservation = ___pawn.Map.pawnDestinationReservationManager.
                    MostRecentReservationFor(___pawn);
                if (!(pawnDestinationReservation is null) && ((__instance.Destination.HasThing && pawnDestinationReservation.target != __instance.Destination.Cell)
                    || (pawnDestinationReservation.job != ___pawn.CurJob && pawnDestinationReservation.target != __instance.Destination.Cell)))
                {
                    ___pawn.Map.pawnDestinationReservationManager.ObsoleteAllClaimedBy(___pawn);
                }
                if (___pawn.CanReachImmediate(dest, peMode))
                {
                    PatherArrivedHelper(__instance, ___pawn);
                    return false;
                }
                if (___pawn.Downed)
                {
                    Log.Error("Ships should not be downable. Contact Mod Author.");
                }
                if (!(__instance.curPath is null))
                {
                    __instance.curPath.ReleaseToPool();
                }
                __instance.curPath = null;
                ___moving = true;
                ___pawn.jobs.posture = PawnPosture.Standing;
                Log.Message("Ended Path");
                return false;
            }
            return true;
        }
        #endregion Pathing

        #region Jobs
        public static bool ShipErrorRecoverJob(Pawn pawn, string message,
            Exception exception = null, JobDriver concreteDriver = null)
        {
            if (IsShip(pawn))
            {
                if (!(pawn.jobs is null))
                {
                    if (!(pawn.jobs.curJob is null))
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Errored, false);
                    }
                    try
                    {
                        pawn.jobs.StartJob(new Job(JobDefOf_Ships.IdleShip, 150, false), JobCondition.None, null, false, true, null, null, false);
                    }
                    catch
                    {
                        Log.Error("An error occurred when trying to recover the job for ship " + pawn.def + ". Please contact Mod Author.");
                    }
                }
                return false;
            }
            return true;
        }

        private static bool ShipsDontWander(Pawn pawn, ref Job __result, JobGiver_Wander __instance)
        {
            if(IsShip(pawn))
            {
                __result = new Job(JobDefOf_Ships.IdleShip);
                return false;
            }
            return true;
        }
        #endregion Jobs

        #region Caravan
        public static void AddShipsSections(TransferableOneWayWidget widget, List<TransferableOneWay> transferables)
        {
            IEnumerable<TransferableOneWay> source = from x in transferables
                                                     where x.ThingDef.category == ThingCategory.Pawn
                                                     select x;
            widget.AddSection("ShipSection".Translate(), from x in source
                                                         where !(((Pawn)x.AnyThing).GetComp<CompShips>() is null)
                                                         select x);
        }

        public static bool StartFormingCaravanForShips(List<Pawn> pawns, List<Pawn> downedPawns, Faction faction, List<TransferableOneWay> transferables,
            IntVec3 meetingPoint, IntVec3 exitSpot, int startingTile, int destinationTile)
        {
            if (pawns.Any((Pawn x) => !(x.GetComp<CompShips>() is null) && (x.GetComp<CompShips>().movementStatus is ShipMovementStatus.Online)))
            {
                if (startingTile < 0)
                {
                    Log.Error("Can't start forming caravan because startingTile is invalid.", false);
                    return false;
                }
                if (!pawns.Any<Pawn>())
                {
                    Log.Error("Can't start forming caravan with 0 pawns.", false);
                    return false;
                }
                if (pawns.Any((Pawn x) => x.Downed))
                {
                    Log.Warning("Forming a caravan with a downed pawn. This shouldn't happen because we have to create a Lord.", false);
                }

                List<TransferableOneWay> list = transferables.ToList<TransferableOneWay>();
                list.RemoveAll((TransferableOneWay x) => x.CountToTransfer <= 0 || !x.HasAnyThing || x.AnyThing is Pawn);

                foreach (Pawn p in pawns)
                {
                    Lord lord = p.GetLord();
                    if (!(lord is null))
                    {
                        lord.Notify_PawnLost(p, PawnLostCondition.ForcedToJoinOtherLord, null);
                    }
                }
                List<Pawn> ships = pawns.Where(x => !(x.GetComp<CompShips>() is null)).ToList();
                int seats = 0;
                foreach (Pawn ship in ships)
                {
                    seats += ship.GetComp<CompShips>().SeatsAvailable;
                }
                if ((pawns.Count + downedPawns.Count) > seats)
                {
                    Log.Error("Can't start forming caravan with ship(s) selected and not enough seats to house all pawns.", false);
                    return false;
                }

                LordJob_FormAndSendCaravanShip lordJob = new LordJob_FormAndSendCaravanShip(list, ships, downedPawns, meetingPoint, exitSpot, startingTile,
                    destinationTile);
                LordMaker.MakeNewLord(Faction.OfPlayer, lordJob, pawns[0].MapHeld, pawns);

                foreach (Pawn p in pawns)
                {
                    if (p.Spawned)
                    {
                        p.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                    }
                }
                return false;
            }
            return true;
        }

        private static IEnumerable<CodeInstruction> DoBottomButtonsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            MethodInfo methodSetSail = AccessTools.Method(type: typeof(ShipHarmony), name: nameof(CanSetSail));
            Label breakLabel = ilg.DefineLabel();

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Ldloc_S && ((LocalBuilder)instruction.operand).LocalIndex == 19)
                {
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, operand: 19);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: methodSetSail);
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, operand: breakLabel);
                }
                else if (instruction.opcode == OpCodes.Ldfld && instruction.operand == AccessTools.Field(type: typeof(Dialog_FormCaravan), name: "destinationTile"))
                {
                    instructionList[i - 1].labels.Add(breakLabel);
                }
                yield return instruction;
            }
        }

        public static bool CanCarryIfShip(Pawn p, ref bool __result)
        {
            return p.GetComp<CompShips>() is null ? true : !(__result = true);
        }

        public static bool RemovePawnAddShip(Pawn pawn, Lord lord, bool removeFromDowned = true)
        {
            if (lord.ownedPawns.Any(x => !(x.GetComp<CompShips>() is null)))
            {
                bool flag = false;
                bool flag2 = false;
                string text = "";
                string textShip = "";
                List<Pawn> ownedShips = lord.ownedPawns.FindAll(x => !(x.GetComp<CompShips>() is null));
                foreach (Pawn ship in ownedShips)
                {
                    if (ship.GetComp<CompShips>().AllPawnsAboard.Contains(pawn))
                    {
                        textShip = "MessagePawnBoardedFormingCaravan".Translate(pawn, ship.LabelShort).CapitalizeFirst();
                        flag2 = true;
                        break;
                    }
                }
                if (!flag2)
                {
                    foreach (Pawn p in lord.ownedPawns)
                    {
                        if (p != pawn && CaravanUtility.IsOwner(p, Faction.OfPlayer))
                        {
                            flag = true;
                            break;
                        }
                    }
                }
                text += flag ? "MessagePawnLostWhileFormingCaravan".Translate(pawn).CapitalizeFirst() : flag2 ? textShip :
                    "MessagePawnLostWhileFormingCaravan".Translate(pawn).CapitalizeFirst() + "MessagePawnLostWhileFormingCaravan_AllLost".Translate();
                //Message for boarding ship?
                bool flag3 = true;
                if (!flag2 && !flag) CaravanFormingUtility.StopFormingCaravan(lord);
                if (flag)
                {
                    pawn.inventory.UnloadEverything = true;
                    if (lord.ownedPawns.Contains(pawn))
                    {
                        lord.Notify_PawnLost(pawn, PawnLostCondition.ForcedByPlayerAction, null);
                        flag3 = false;
                    }
                    LordJob_FormAndSendCaravanShip lordJob_FormAndSendCaravanShip = lord.LordJob as LordJob_FormAndSendCaravanShip;
                    if (!(lordJob_FormAndSendCaravanShip is null) && lordJob_FormAndSendCaravanShip.downedPawns.Contains(pawn))
                    {
                        if (!removeFromDowned)
                        {
                            flag3 = false;
                        }
                        else
                        {
                            lordJob_FormAndSendCaravanShip.downedPawns.Remove(pawn);
                        }
                    }
                }
                if (flag3)
                {
                    MessageTypeDef msg = flag2 ? MessageTypeDefOf.SilentInput : MessageTypeDefOf.NegativeEvent;
                    Messages.Message(text, pawn, msg, true);
                }

                return false;
            }
            return true;
        }

        public static void GetShipAndSendCaravanLord(Pawn p, ref Lord __result)
        {
            if (__result is null)
            {
                if (IsFormingCaravanShipHelper(p))
                {
                    __result = p.GetLord();
                    return;
                }
                if (p.Spawned)
                {
                    List<Lord> lords = p.Map.lordManager.lords;
                    foreach (Lord lord in lords)
                    {
                        LordJob_FormAndSendCaravanShip lordJob_FormAndSendCaravanShip = lord.LordJob as LordJob_FormAndSendCaravanShip;
                        if (!(lordJob_FormAndSendCaravanShip is null) && lordJob_FormAndSendCaravanShip.downedPawns.Contains(p))
                        {
                            __result = lord;
                            return;
                        }
                    }
                }
            }
        }

        public static bool IsFormingCaravanShip(Pawn p, ref bool __result)
        {
            Lord lord = p.GetLord();
            if (!(lord is null) && (lord.LordJob is LordJob_FormAndSendCaravanShip))
            {
                __result = true;
                return false;
            }
            return true;
        }

        public static IEnumerable<CodeInstruction> CanStackShip(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            MethodInfo shipMethod = AccessTools.Method(type: typeof(ShipHarmony), name: nameof(IsShip));

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_0)
                {
                    Label label = ilg.DefineLabel();

                    i++;
                    yield return instruction;
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_0);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: shipMethod);
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ret);
                    instruction = instructionList[i];
                    instruction.labels.Add(label);
                }
                yield return instruction;
            }
        }

        private static bool TransferablesShip(JobDriver_PrepareCaravan_GatherItems __instance, ref List<TransferableOneWay> __result)
        {
            if (__instance.job.lord.LordJob is LordJob_FormAndSendCaravanShip)
            {
                __result = ((LordJob_FormAndSendCaravanShip)__instance.job.lord.LordJob).transferables;
                return false;
            }
            return true;
        }

        private static IEnumerable<CodeInstruction> AddHumanLikeOrdersTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && ((LocalBuilder)instruction.operand).LocalIndex == 50)
                {
                    i++;
                    Label label = ilg.DefineLabel();
                    yield return instruction;
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, operand: 50);
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse_S, operand: label);
                    instruction = instructionList[i];
                    instruction.labels.Add(label);
                }
                yield return instruction;
            }
        }

        public static void UpdateDutyOfShip(LordToil_PrepareCaravan_GatherAnimals __instance)
        {
            if(__instance.lord.LordJob is LordJob_FormAndSendCaravanShip)
            {
                List<Pawn> ships = __instance.lord.ownedPawns.Where(x => !(x.GetComp<CompShips>() is null)).ToList();
                foreach(Pawn ship in ships)
                {
                    ship.mindState.duty = new PawnDuty(DutyDefOf_Ships.PrepareCaravan_WaitShip);
                }
            }
        }

        #endregion Caravan

        #region Construction
        public static bool CompleteConstructionShip(Pawn worker, Frame __instance)
        {
            if (__instance.def.entityDefToBuild.designationCategory == DesignationCategoryDefOf_Ships.RimShips)
            {
                Pawn ship = PawnGenerator.GeneratePawn(__instance.def.entityDefToBuild.GetModExtension<Build.SpawnThingBuilt>().thingToSpawn);
                __instance.resourceContainer.ClearAndDestroyContents(DestroyMode.Vanish);
                Map map = __instance.Map;
                __instance.Destroy(DestroyMode.Vanish);

                if (!(__instance.def.entityDefToBuild.GetModExtension<SpawnThingBuilt>().soundFinished is null))
                {
                    __instance.def.entityDefToBuild.GetModExtension<SpawnThingBuilt>().soundFinished.PlayOneShot(new TargetInfo(__instance.Position, map, false));
                }
                ship.SetFaction(worker.Faction);
                GenSpawn.Spawn(ship, __instance.Position, map, __instance.Rotation, WipeMode.FullRefund, false);
                worker.records.Increment(RecordDefOf.ThingsConstructed);

                ship.GetComp<CompShips>().Rename();
                //Quality?
                //Art?
                //Tale RecordTale LongConstructionProject?
                return false;
            }
            return true;
        }
        #endregion Construction

        public static bool CanReachShipImmediate(IntVec3 start, LocalTargetInfo target, Map map, PathEndMode peMode, Pawn pawn, ref bool __result)
        {
            if (disabled) return true;
            if (IsShip(pawn))
            {
                if(!target.IsValid)
                {
                    __result = false;
                    return false;
                }
                target = (LocalTargetInfo)GenPathShip.ResolvePathMode(pawn, target.ToTargetInfo(map), ref peMode, mapE);
                if (target.HasThing)
                {
                    Thing thing = target.Thing;
                    if (!thing.Spawned)
                    {
                        if (pawn != null)
                        {
                            if (pawn.carryTracker.innerContainer.Contains(thing))
                            {
                                __result = true;
                                return false;
                            }
                            if (pawn.inventory.innerContainer.Contains(thing))
                            {
                                __result = true;
                                return false;
                            }
                            if (pawn.apparel != null && pawn.apparel.Contains(thing))
                            {
                                __result = true;
                                return false;
                            }
                            if (pawn.equipment != null && pawn.equipment.Contains(thing))
                            {
                                __result = true;
                                return false;
                            }
                        }
                        __result = false;
                        return false;
                    }
                    if (thing.Map != map)
                    {
                        __result = false;
                        return false;
                    }
                }
                if (!target.HasThing || (target.Thing.def.size.x == 1 && target.Thing.def.size.z == 1))
                {
                    if (start == target.Cell)
                    {
                        __result = true;
                        return false;
                    }
                }
                else if (start.IsInside(target.Thing))
                {
                    __result = true;
                    return false;
                }
                __result = peMode == PathEndMode.Touch && TouchPathEndModeUtility.IsAdjacentOrInsideAndAllowedToTouch(start, target, map);
                return false;
            }
            return true;
        }

        private static bool GenerateNewShipPath(ref PawnPath __result, Pawn_PathFollower __instance, ref Pawn ___pawn, ref PathEndMode ___peMode)
        {
            if (disabled) return true;
            __instance.lastPathedTargetPosition = __instance.Destination.Cell;
            if(IsShip(___pawn))
            {
                __result = mapE.getShipPathFinder.FindShipPath(___pawn.Position, __instance.Destination, ___pawn, ___peMode);
                return false;
            }
            return true;
        }

        private static bool TryEnterNextCellShip(Pawn_PathFollower __instance, ref Pawn ___pawn, ref IntVec3 ___lastCell, ref LocalTargetInfo ___destination,
            ref PathEndMode ___peMode)
        {
            if (disabled) return true;
            if (IsShip(___pawn))
            {
                if(___pawn.GetComp<CompShips>().beached)
                {
                    __instance.StopDead();
                    ___pawn.jobs.curDriver.Notify_PatherFailed();
                }
                //Buildings?
                ___lastCell = ___pawn.Position;
                ___pawn.Position = __instance.nextCell;
                //Clamor?
                //More Buildings?
                if(___pawn.CanReachImmediate(___destination, ___peMode))
                {
                    PatherArrivedHelper(__instance, ___pawn);
                }
                else
                {
                    if(__instance.curPath.NodesLeftCount <= 1)
                    {
                        Log.Error(string.Concat(new object[]
                        {
                            ___pawn,
                            " at ",
                            ___pawn.Position,
                            " ran out of path nodes while pathing to ",
                            ___destination,
                            "."
                        }), false);
                        PatherFailedHelper(__instance, ___pawn);
                        return false;
                    }
                    __instance.nextCell = __instance.curPath.ConsumeNextNode();
                    if(!GenGridShips.Walkable(__instance.nextCell,mapE))
                    {
                        Log.Error(string.Concat(new object[]
                        {
                            ___pawn,
                            " entering ",
                            __instance.nextCell,
                            " which is unwalkable."
                        }), false);
                    }
                    int num = CostToMoveIntoCellShips(___pawn, __instance.nextCell);
                    __instance.nextCellCostTotal = (float)num;
                    __instance.nextCellCostLeft = (float)num;
                    //notify door of pawn?
                }
                return false;
            }
            return true;
        }

        //[HarmonyPatch(typeof(PawnPath), "DrawPath")]

        public static bool Walkable_RestrictWater(IntVec3 loc, PathGrid __instance, ref bool __result)
        {
            
            return true;
        }

        public static void GenerateMapExtension(IntVec3 mapSize, MapParent parent, MapGeneratorDef mapGenerator, ref Map __result,
            IEnumerable<GenStepWithParams> extraGenStepDefs = null, Action<Map> extraInitBeforeContentGen = null)
        {
            mapE = new MapExtension(__result);
            mapE.ConstructComponents();
            Log.Message("Map Extension Initialized!");
        }

        public static bool FindPathShips(IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, ref PawnPath __result,
            PathEndMode peMode = PathEndMode.OnCell)
        {
            if (disabled) return true;

            if(!(traverseParms.pawn is null))
            {
                if (IsShip(traverseParms.pawn))
                {
                    __result = mapE.getShipPathFinder.FindShipPath(start, dest, traverseParms, peMode);
                    return false;
                }
            }
            return true;
        }

        #region TranspilersWIP

        /*public static IEnumerable<CodeInstruction> FindPathShips(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            MethodInfo methodIsShip = AccessTools.Method(type: typeof(ShipHarmony), name: nameof(IsShip));
            MethodInfo test = AccessTools.Method(type: typeof(Log), name: nameof(Log.Message));
            foreach(CodeInstruction instruction in instructionList)
            {

                if(instruction.opcode == OpCodes.Stloc_S && ((LocalBuilder)instruction.operand).LocalIndex == 55)
                {
                    Label skip = ilg.DefineLabel();

                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_0);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: methodIsShip);
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, operand: skip);

                    *//*yield return new CodeInstruction(opcode: OpCodes.Ldstr, operand: "test");
                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_0, operand: test);*/

        /*yield return new CodeInstruction(opcode: OpCodes.Ldloc_0);
        yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, operand: 5);
        yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(ShipHarmony), name: nameof(ShipPawnCellCost)));*//*

        instruction.labels.Add(item: skip);
    }
    yield return instruction;
}
}*/
        #endregion TranspilersWIP

        #region HelperFunctions

        private static MapExtension mapE;

        private static bool CanSetSail(List<Pawn> caravan)
        {
            int seats = 0;
            int pawns = 0;
            int prereq = 0;
            bool flag = caravan.Any(x => !(x.GetComp<CompShips>() is null)); //Ships or No Ships
            if(flag)
            {
                foreach (Pawn p in caravan)
                {
                    if (!(p.GetComp<CompShips>() is null))
                    {
                        seats += p.GetComp<CompShips>().SeatsAvailable;
                        prereq += p.GetComp<CompShips>().PawnCountToOperate;
                    }
                    else
                    {
                        pawns++;
                    }
                }
            }
            bool flag2 = flag ? pawns > seats : false; //Not Enough Room
            bool flag3 = flag ? pawns < prereq : false; //Not Enough Pawns to Sail
            if(flag2)
                Messages.Message("CaravanMustHaveEnoughSpaceOnShip".Translate(), MessageTypeDefOf.RejectInput, false);
            if(!caravan.Any(x => CaravanUtility.IsOwner(x, Faction.OfPlayer) && !x.Downed))
                Messages.Message("CaravanMustHaveAtLeastOneColonist".Translate(), MessageTypeDefOf.RejectInput, false);
            if (flag3)
                Messages.Message("CaravanMustHaveEnoughPawnsToOperate".Translate(prereq), MessageTypeDefOf.RejectInput, false);
            return !flag2 && !flag3;
        }

        private static bool IsShip(Pawn p)
        {
            return !(p?.TryGetComp<CompShips>() is null) ? true : false;
        }

        private static void PatherFailedHelper(Pawn_PathFollower instance, Pawn pawn)
        {
            instance.StopDead();
            pawn.jobs.curDriver.Notify_PatherFailed();
        }

        private static void PatherArrivedHelper(Pawn_PathFollower instance, Pawn pawn)
        {
            instance.StopDead();
            if(!(pawn.jobs.curJob is null))
            {
                pawn.jobs.curDriver.Notify_PatherArrived();
            }
        }

        //Needs case for prisoner ships
        public static bool WillAutoJoinIfCaptured(Pawn ship)
        {
            return ship.GetComp<CompShips>().movementStatus != ShipMovementStatus.Offline && !ship.GetComp<CompShips>().beached;
        }

        private static void FaceShipAdjacentCell(IntVec3 c, Pawn pawn)
        {
            if (c == pawn.Position)
            {
                return;
            }
            IntVec3 intVec = c - pawn.Position;
            if (intVec.x > 0)
            {
                pawn.Rotation = Rot4.East;
            }
            else if (intVec.x < 0)
            {
                pawn.Rotation = Rot4.West;
            }
            else if (intVec.z > 0)
            {
                pawn.Rotation = Rot4.North;
            }
            else
            {
                pawn.Rotation = Rot4.South;
            }
        }

        private static int CostToMoveIntoCellShips(Pawn pawn, IntVec3 c)
        {
            int num = (c.x == pawn.Position.x || c.z == pawn.Position.z) ? pawn.TicksPerMoveCardinal : pawn.TicksPerMoveDiagonal;
            num += mapE.getShipPathGrid.CalculatedCostAt(c, false, pawn.Position);
            if (pawn.CurJob != null)
            {
                Pawn locomotionUrgencySameAs = pawn.jobs.curDriver.locomotionUrgencySameAs;
                if (locomotionUrgencySameAs != null && locomotionUrgencySameAs != pawn && locomotionUrgencySameAs.Spawned)
                {
                    int num2 = CostToMoveIntoCellShips(locomotionUrgencySameAs, c);
                    if (num < num2)
                    {
                        num = num2;
                    }
                }
                else
                {
                    switch (pawn.jobs.curJob.locomotionUrgency)
                    {
                        case LocomotionUrgency.Amble:
                            num *= 3;
                            if (num < 60)
                            {
                                num = 60;
                            }
                            break;
                        case LocomotionUrgency.Walk:
                            num *= 2;
                            if (num < 50)
                            {
                                num = 50;
                            }
                            break;
                        case LocomotionUrgency.Jog:
                            break;
                        case LocomotionUrgency.Sprint:
                            num = Mathf.RoundToInt((float)num * 0.75f);
                            break;
                    }
                }
            }
            return Mathf.Max(num, 1);
        }

        public static bool IsFormingCaravanShipHelper(Pawn p)
        {
            Lord lord = p.GetLord();
            return !(lord is null) && lord.LordJob is LordJob_FormAndSendCaravanShip;
        }

        public static float CapacityLeft(LordJob_FormAndSendCaravanShip lordJob, ref List<ThingCount> tmpCaravanPawns)
        {
            float num = CollectionsMassCalculator.MassUsageTransferables(lordJob.transferables, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, false, false);
            tmpCaravanPawns.Clear();
            for (int i = 0; i < lordJob.lord.ownedPawns.Count; i++)
            {
                Pawn pawn = lordJob.lord.ownedPawns[i];
                tmpCaravanPawns.Add(new ThingCount(pawn, pawn.stackCount));
            }
            num += CollectionsMassCalculator.MassUsage(tmpCaravanPawns, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, false, false);
            float num2 = CollectionsMassCalculator.Capacity(tmpCaravanPawns, null);
            tmpCaravanPawns.Clear();
            return num2 - num;
        }

        #endregion HelperFunctions

        private static bool disabled = true;
    }
}