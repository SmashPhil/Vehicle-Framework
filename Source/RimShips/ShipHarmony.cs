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

            //Map Gen
            harmony.Patch(original: AccessTools.Method(typeof(BeachMaker), name: nameof(BeachMaker.Init)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(BeachMakerTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(TileFinder), name: nameof(TileFinder.RandomSettlementTileFor)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(PushSettlementToCoastTranspiler)));

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
            harmony.Patch(original: AccessTools.Method(typeof(PawnRenderer), parameters: new Type[] { typeof(Vector3), typeof(float), typeof(bool), typeof(Rot4), typeof(Rot4),
                    typeof(RotDrawMode), typeof(bool), typeof(bool)}, name: "RenderPawnInternal"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                name: nameof(RenderPawnRotationTranspiler)));
            harmony.Patch(original: AccessTools.Method(type: typeof(ColonistBar), name: "CheckRecacheEntries"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(CheckRecacheEntriesTranspiler)));
            harmony.Patch(original: AccessTools.Method(type: typeof(ColonistBarColonistDrawer), "DrawIcons"), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                name: nameof(DrawIconsShips)));

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
                name: nameof(StartPathShip)));
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
            harmony.Patch(original: AccessTools.Method(typeof(Map), name: nameof(Map.ExposeData)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(ExposeDataMapExtensions)));
            harmony.Patch(original: AccessTools.Method(type: typeof(FloatMenuMakerMap), name: "GotoLocationOption"),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(GotoLocationShips)));
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
                name: nameof(CanStackShipTranspiler)));
            harmony.Patch(original: AccessTools.Property(type: typeof(JobDriver_PrepareCaravan_GatherItems), name: "Transferables").GetGetMethod(nonPublic: true),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(TransferablesShip))); //DOUBLE CHECK
            harmony.Patch(original: AccessTools.Method(type: typeof(FloatMenuMakerMap), name: "AddHumanlikeOrders"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(AddHumanLikeOrdersTranspiler)));
            harmony.Patch(original: AccessTools.Method(type: typeof(LordToil_PrepareCaravan_GatherAnimals), name: nameof(LordToil_PrepareCaravan_GatherAnimals.UpdateAllDuties)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(UpdateDutyOfShip)));
            harmony.Patch(original: AccessTools.Method(type: typeof(Dialog_FormCaravan), name: "TryFormAndSendCaravan"), 
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(TryFormAndSendCaravanShips)));

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

            //Debug
            if (debug)
            {
                harmony.Patch(original: AccessTools.Method(typeof(WorldRoutePlanner), name: nameof(WorldRoutePlanner.WorldRoutePlannerUpdate)), prefix: null,
                    postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                    name: nameof(DebugSettlementPaths)));
                harmony.Patch(original: AccessTools.Method(typeof(WorldObjectsHolder), name: nameof(WorldObjectsHolder.Add)),
                    prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                    name: nameof(DebugWorldObjects)));
            }
            #endregion Functions
        }

        public static void DebugWorldObjects(WorldObject o)
        {
            if(o is Settlement)
            {
                tiles.Add(new Pair<int, int>(o.Tile, 0));
            }
        }

        #region MapGen
        public static IEnumerable<CodeInstruction> BeachMakerTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            MethodInfo randomRange = AccessTools.Property(type: typeof(FloatRange), name: nameof(FloatRange.RandomInRange)).GetGetMethod();
            MethodInfo floatMultiplier = AccessTools.Method(type: typeof(ShipHarmony), name: nameof(ShipHarmony.CustomFloatBeach));
            for(int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if(instruction.opcode == OpCodes.Call && instruction.operand == randomRange)
                {
                    i++;
                    instruction = instructionList[i];
                    //yield return new CodeInstruction(opcode: OpCodes.Ldc_R8, operand: 60);
                    yield return new CodeInstruction(opcode: OpCodes.Pop);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: floatMultiplier);
                }
                yield return instruction;
            }
        }

        public static IEnumerable<CodeInstruction> PushSettlementToCoastTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for(int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if(instruction.opcode == OpCodes.Ldnull && instructionList[i-1].opcode == OpCodes.Ldloc_1)
                {
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(ShipHarmony), name: nameof(ShipHarmony.PushSettlementToCoast)));
                    yield return new CodeInstruction(opcode: OpCodes.Stloc_1);
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_1);
                }
                yield return instruction;
            }
        }
        #endregion MapGen

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

        public static bool ShipShouldBeDowned(Pawn_HealthTracker __instance, ref bool __result, ref Pawn ___pawn)
        {
            if (___pawn != null && IsShip(___pawn))
            {
                __result = ___pawn.GetComp<CompShips>().Props.downable;
                return false;
            }
            return true;
        }

        public static bool ShipShouldWiggle(PawnDownedWiggler __instance, ref Pawn ___pawn)
        {
            if (___pawn != null && IsShip(___pawn) && !___pawn.GetComp<CompShips>().Props.movesWhenDowned)
            {
                return false;
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

        public static IEnumerable<CodeInstruction> RenderPawnRotationTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            Label label = ilg.DefineLabel();

            yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
            yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(PawnRenderer), name: "pawn"));
            yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(ShipHarmony), name: nameof(IsShip)));
            yield return new CodeInstruction(opcode: OpCodes.Brfalse, operand: label);

            yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
            yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(PawnRenderer), name: "pawn"));
            yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(ShipHarmony), name: nameof(ShipHarmony.ShipAngle)));
            yield return new CodeInstruction(opcode: OpCodes.Starg_S, operand: 2);

            yield return new CodeInstruction(opcode: OpCodes.Nop) { labels = new List<Label> { label } };
            foreach (CodeInstruction instruction in instructionList)
            {
                yield return instruction;
            }
        }

        public static IEnumerable<CodeInstruction> CheckRecacheEntriesTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            bool flag = false;
            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if(!flag && instruction.opcode == OpCodes.Stloc_S && ((LocalBuilder)instruction.operand).LocalIndex == 5)
                {
                    yield return instruction;
                    instruction = instructionList[++i];
                    flag = true;

                    yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(type: typeof(ColonistBar), name: "tmpPawns"));
                    yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(type: typeof(ColonistBar), name: "tmpMaps"));
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_1);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(ShipHarmony), name: nameof(GetShipsForColonistBar)));
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(List<Pawn>), name: nameof(List<Pawn>.AddRange)));
                }

                yield return instruction;
            }
        }

        private static void DrawIconsShips(Rect rect, Pawn colonist, ColonistBarColonistDrawer __instance)
        {
            if (colonist.Dead)
            {
                return;
            }
            float num = 20f * Find.ColonistBar.Scale;
            Vector2 vector = new Vector2(rect.x + 1f, rect.yMax - num - 1f);
            bool flag = false;
            if (colonist.CurJob != null)
            {
                JobDef def = colonist.CurJob.def;
                if (def == JobDefOf.AttackMelee || def == JobDefOf.AttackStatic)
                {
                    flag = true;
                }
                else if (def == JobDefOf.Wait_Combat)
                {
                    Stance_Busy stance_Busy = colonist.stances.curStance as Stance_Busy;
                    if (stance_Busy != null && stance_Busy.focusTarg.IsValid)
                    {
                        flag = true;
                    }
                }
            }
            
            List<Pawn> ships = Find.CurrentMap.mapPawns.AllPawnsSpawned.FindAll(x => !(x.GetComp<CompShips>() is null));
            if(ships.Any(x => x.GetComp<CompShips>().AllPawnsAboard.Contains(colonist)))
            {
                Rect rect2 = new Rect(vector.x, vector.y, num, num);
                GUI.DrawTexture(rect2, TexCommandShips.OnboardIcon);
                TooltipHandler.TipRegion(rect2, "ActivityIconOnBoardShip".Translate());
                vector.x += num;
            }
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
        public static bool StartPathShip(LocalTargetInfo dest, PathEndMode peMode, Pawn_PathFollower __instance, ref Pawn ___pawn, ref PathEndMode ___peMode,
            ref LocalTargetInfo ___destination, ref bool ___moving)
        {
            if (disabled) return true;
            if (IsShip(___pawn))
            {
                dest = (LocalTargetInfo)GenPathShip.ResolvePathMode(___pawn, dest.ToTargetInfo(___pawn.Map), ref peMode, MapExtensionUtility.GetExtensionToMap(___pawn.Map));
                if (dest.HasThing && dest.ThingDestroyed)
                {
                    Log.Error(___pawn + " pathing to destroyed thing " + dest.Thing, false);
                    PatherFailedHelper(ref __instance, ___pawn);
                    return false;
                }
                //Add Building and Position Recoverable extras
                if (!GenGridShips.Walkable(___pawn.Position, MapExtensionUtility.GetExtensionToMap(___pawn.Map)))
                {
                    return false;
                }
                if (__instance.Moving && __instance.curPath != null && ___destination == dest && ___peMode == peMode)
                {
                    return false;
                }
                if (!MapExtensionUtility.GetExtensionToMap(___pawn.Map).getShipReachability.CanReachShip(___pawn.Position, dest, peMode, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
                {
                    PatherFailedHelper(ref __instance, ___pawn);
                    return false;
                }
                ___peMode = peMode;
                ___destination = dest;
                if ((GenGridShips.Walkable(__instance.nextCell, MapExtensionUtility.GetExtensionToMap(___pawn.Map)) || __instance.WillCollideWithPawnOnNextPathCell()) || __instance.nextCellCostLeft
                    == __instance.nextCellCostTotal)
                {
                    __instance.ResetToCurrentPosition();
                }
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

        public static IEnumerable<CodeInstruction> CanStackShipTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
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

        //COME BACK TO THIS -> For Exit Point
        private static bool TryFormAndSendCaravanShips(Dialog_FormCaravan __instance, ref bool __result, ref int ___startingTile)
        {
            return true;
            List<Pawn> pawnsTransferables = TransferableUtility.GetPawnsFromTransferables(__instance.transferables);
            
            if(pawnsTransferables.Any(x => IsShip(x)))
            {
                MethodInfo checkMethod = typeof(Dialog_FormCaravan).GetMethod("CheckForErrors", BindingFlags.NonPublic | BindingFlags.Instance);
                bool flag = Convert.ToBoolean(checkMethod.Invoke(__instance, new System.Object[] { pawnsTransferables })); 
                if(!flag)
                {
                    __result = false;
                    return false;
                }
                //Direction8Way direction8WayFromToj = FindWorldGrid.GetDirection8WayFromTo(__instance.CurrentTile, ___startingTile);

                return false;
            }

            return true;
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
                target = (LocalTargetInfo)GenPathShip.ResolvePathMode(pawn, target.ToTargetInfo(map), ref peMode, MapExtensionUtility.GetExtensionToMap(map));
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

        private static bool GenerateNewShipPath(ref PawnPath __result, ref Pawn_PathFollower __instance, ref Pawn ___pawn, ref PathEndMode ___peMode)
        {
            if (disabled) return true;
            
            if(IsShip(___pawn))
            {
                __instance.lastPathedTargetPosition = __instance.Destination.Cell;
                __result = MapExtensionUtility.GetExtensionToMap(___pawn.Map).getShipPathFinder.FindShipPath(___pawn.Position, __instance.Destination, ___pawn, ___peMode);
                if (!__result.Found) Log.Warning("Path Not Found");
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
                if(___pawn.GetComp<CompShips>().beached || !___pawn.Position.GetTerrain(___pawn.Map).IsWater)
                {
                    ___pawn.GetComp<CompShips>().BeachShip();
                    __instance.StopDead();
                    ___pawn.jobs.curDriver.Notify_PatherFailed();
                }
                //Buildings?
                ___lastCell = ___pawn.Position;
                ___pawn.Position = __instance.nextCell;
                //Clamor?
                //More Buildings?
                
                if(NeedNewPath(___destination, __instance.curPath, ___pawn, ___peMode, __instance.lastPathedTargetPosition) && !TrySetNewPath(ref __instance, __instance.lastPathedTargetPosition, 
                    ___destination, ___pawn, ___pawn.Map, ref ___peMode))
                {
                    return false;
                }
                if(___pawn.CanReachImmediateShip(___destination, ___peMode))
                {
                    PatherArrivedHelper(__instance, ___pawn);
                }
                else
                {
                    SetupMoveIntoNextCell(ref __instance, ___pawn, ___destination);
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
            MapExtension mapE = new MapExtension(__result);
            mapE.ConstructComponents();
        }

        public static void ExposeDataMapExtensions()
        {
            MapExtensionUtility.ClearMapExtensions();
        }

        private static bool GotoLocationShips(IntVec3 clickCell, Pawn pawn, ref FloatMenuOption __result)
        {
            if (disabled) return true;
            if(IsShip(pawn))
            {
                int num = GenRadial.NumCellsInRadius(2.9f);
                int i = 0;
                IntVec3 curLoc;
                while(i < num)
                {
                    curLoc = GenRadial.RadialPattern[i] + clickCell;
                    if (GenGridShips.Standable(curLoc, pawn.Map, MapExtensionUtility.GetExtensionToMap(pawn.Map)))
                    {
                        Log.Message("Standable!");
                        if (curLoc == pawn.Position)
                        {
                            __result = null;
                            return false;
                        }
                        if(!ShipReachabilityUtility.CanReachShip(pawn, curLoc, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                        {
                            Log.Message("CANT REACH ");
                            __result = new FloatMenuOption("CannotSailToCell".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null);
                            return false;
                        }
                        Log.Message("Reachable?");
                        Action action = delegate ()
                        {
                            Job job = new Job(JobDefOf.Goto, curLoc);
                            if(pawn.Map.exitMapGrid.IsExitCell(Verse.UI.MouseCell()))
                            {
                                job.exitMapOnArrival = true;
                            }
                            else if(!pawn.Map.IsPlayerHome && !pawn.Map.exitMapGrid.MapUsesExitGrid && CellRect.WholeMap(pawn.Map).IsOnEdge(Verse.UI.MouseCell(), 3) &&
                                pawn.Map.Parent.GetComponent<FormCaravanComp>() != null && MessagesRepeatAvoider.MessageShowAllowed("MessagePlayerTriedToLeaveMapViaExitGrid-" +
                                pawn.Map.uniqueID, 60f))
                            {
                                FormCaravanComp component = pawn.Map.Parent.GetComponent<FormCaravanComp>();
                                if(component.CanFormOrReformCaravanNow)
                                {
                                    Messages.Message("MessagePlayerTriedToLeaveMapViaExitGrid_CanReform".Translate(), pawn.Map.Parent, MessageTypeDefOf.RejectInput, false);
                                }
                                else
                                {
                                    Messages.Message("MessagePlayerTriedToLeaveMapViaExitGrid_CantReform".Translate(), pawn.Map.Parent, MessageTypeDefOf.RejectInput, false);
                                }
                            }
                            if(pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc))
                            {
                                MoteMaker.MakeStaticMote(curLoc, pawn.Map, ThingDefOf.Mote_FeedbackGoto, 1f);
                            }
                        };
                        __result = new FloatMenuOption("GoHere".Translate(), action, MenuOptionPriority.GoHere, null, null, 0f, null, null)
                        {
                            autoTakeable = true,
                            autoTakeablePriority = 10f
                        };
                        return false;
                    }
                    else
                    {
                        i++;
                    }
                }
                __result = null;
                return false;
            }
            return true;
        }

        #region HelperFunctions

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

        private static void PatherFailedHelper(ref Pawn_PathFollower instance, Pawn pawn)
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
            num += MapExtensionUtility.GetExtensionToMap(pawn.Map).getShipPathGrid.CalculatedCostAt(c, false, pawn.Position);
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

        private static bool TryFindExitSpotHelper(List<Pawn> pawns, List<Pawn> ships, bool reachableForEveryShip, out IntVec3 spot, ref int startingTile, Map map)
        {
            Rot4 rotFromTo = Find.WorldGrid.GetRotFromTo(map.Tile, startingTile);
            return TryFindExitSpotHelper(pawns, ships, reachableForEveryShip, rotFromTo, out spot, ref startingTile, map) ||
                TryFindExitSpotHelper(pawns, ships, reachableForEveryShip, rotFromTo.Rotated(RotationDirection.Clockwise), out spot, ref startingTile, map) ||
                TryFindExitSpotHelper(pawns, ships, reachableForEveryShip, rotFromTo.Rotated(RotationDirection.Counterclockwise), out spot, ref startingTile, map);
        }

        private static bool TryFindExitSpotHelper(List<Pawn> pawns, List<Pawn> ships, bool reachableForEveryShip, Rot4 exitDirection, out IntVec3 spot, ref int startingTile, Map map)
        {
            if(startingTile < 0)
            {
                Log.Error("Can't find exit spot because startingTile is not set.", false);
                spot = IntVec3.Invalid;
                return false;
            }
            Predicate<IntVec3> validator = (IntVec3 x) => !x.Fogged(map); //Add Water based Check
            if(reachableForEveryShip)
            {
                return CellFinder.TryFindRandomEdgeCellWith(delegate (IntVec3 x)
                {
                    if (!validator(x))
                    {
                        return false;
                    }
                    foreach(Pawn ship in ships)
                    {
                        foreach (Pawn pawn in pawns)
                        {
                            if (pawn.IsColonist && !pawn.Downed && !pawn.CanReachShip(ship.Position, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
                            {
                                return false;
                            }
                        }
                    }
                    return true;
                }, map, exitDirection, CellFinder.EdgeRoadChance_Always, out spot);
            }
            IntVec3 intVec = IntVec3.Invalid;
            int num = -1;
            foreach(IntVec3 intVec2 in CellRect.WholeMap(map).GetEdgeCells(exitDirection).InRandomOrder(null))
            {
                if(validator(intVec2))
                {
                    int num2 = 0;
                    foreach(Pawn pawn in pawns)
                    {
                        if(pawn.IsColonist && !pawn.Downed && pawn.CanReachShip(intVec2, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
                        {
                            num2++;
                        }
                    }
                    if(num2 > num)
                    {
                        num = num2;
                        intVec = intVec2;
                    }
                }
            }
            spot = intVec;
            return intVec.IsValid;
        }

        public static float ShipAngle(Pawn pawn)
        {
            float angle = 0f;
            if (pawn.pather.Moving)
            {
                IntVec3 c = pawn.pather.nextCell - pawn.Position;
                if (c.x > 0 && c.z > 0)
                {
                    angle = -45f;
                }
                else if (c.x > 0 && c.z < 0)
                {
                    angle = 45;
                }
                else if (c.x < 0 && c.z < 0)
                {
                    angle = -45;
                }
                else if (c.x < 0 && c.z > 0)
                {
                    angle = 45f;
                }
            }
            return angle;
        }

        public static bool NeedNewPath(LocalTargetInfo destination, PawnPath curPath, Pawn pawn, PathEndMode peMode, IntVec3 lastPathedTargetPosition)
        {
            if(!destination.IsValid || curPath is null || !curPath.Found || curPath.NodesLeftCount == 0)
                return true;

            if (destination.HasThing && destination.Thing.Map != pawn.Map)
                return true;

            if((pawn.Position.InHorDistOf(curPath.LastNode, 15f) || pawn.Position.InHorDistOf(destination.Cell, 15f)) && !ShipReachabilityImmediate.CanReachImmediateShip(
                curPath.LastNode, destination, pawn.Map, peMode, pawn))
                return true;

            if(curPath.UsedRegionHeuristics && curPath.NodesConsumedCount >= 75)
                return true;

            if (lastPathedTargetPosition != destination.Cell)
            {
                float num = (float)(pawn.Position - destination.Cell).LengthHorizontalSquared;
                float num2;
                if (num > 900f) num2 = 10f;
                else if (num > 289f) num2 = 5f;
                else if (num > 100f) num2 = 3f;
                else if (num > 49f) num2 = 2f;
                else num2 = 0.5f;

                if ((float)(lastPathedTargetPosition - destination.Cell).LengthHorizontalSquared > num2 * num2)
                    return true;
            }
            bool flag = curPath.NodesLeftCount < 30;
            IntVec3 other = IntVec3.Invalid;
            IntVec3 intVec = IntVec3.Invalid;
            int num3 = 0;
            while(num3 < 20 && num3 < curPath.NodesLeftCount)
            {
                intVec = curPath.Peek(num3);
                if (!GenGridShips.Walkable(intVec, MapExtensionUtility.GetExtensionToMap(pawn.Map)))
                    return true;
                //Handle Doors?

                if (num3 != 0 && intVec.AdjacentToDiagonal(other) && (ShipPathFinder.BlocksDiagonalMovement(pawn.Map.cellIndices.CellToIndex(intVec.x, other.z), pawn.Map,
                    MapExtensionUtility.GetExtensionToMap(pawn.Map)) || ShipPathFinder.BlocksDiagonalMovement(pawn.Map.cellIndices.CellToIndex(other.x, intVec.z), pawn.Map,
                    MapExtensionUtility.GetExtensionToMap(pawn.Map))) )
                    return true;
                other = intVec;
                num3++;
            }
            return false;
        }

        private static bool TrySetNewPath(ref Pawn_PathFollower instance, IntVec3 lastPathedTargetPosition, LocalTargetInfo destination, Pawn pawn, Map map, ref PathEndMode peMode)
        {
            PawnPath pawnPath = ShipHarmony.GenerateNewPath(ref lastPathedTargetPosition, destination, ref pawn, map, peMode);
            if(!pawnPath.Found)
            {
                PatherFailedHelper(ref instance, pawn);
            }
            if(!(instance.curPath is null))
            {
                instance.curPath.ReleaseToPool();
            }
            instance.curPath = pawnPath;
            int num = 0;
            int foundPathWhichCollidesWithPawns = Traverse.Create(instance).Field("foundPathWhichCollidesWithPawns").GetValue<int>();
            int foundPathWithDanger = Traverse.Create(instance).Field("foundPathWithDanger").GetValue<int>();
            while(num < 20 && num < instance.curPath.NodesLeftCount)
            {
                IntVec3 c = instance.curPath.Peek(num);
                if(PawnUtility.ShouldCollideWithPawns(pawn) && PawnUtility.AnyPawnBlockingPathAt(c, pawn, false, false, false))
                {
                    foundPathWhichCollidesWithPawns = Find.TickManager.TicksGame;
                }
                if(PawnUtility.KnownDangerAt(c, pawn.Map, pawn))
                {
                    foundPathWithDanger = Find.TickManager.TicksGame;
                }
                if(foundPathWhichCollidesWithPawns == Find.TickManager.TicksGame && foundPathWithDanger == Find.TickManager.TicksGame)
                {
                    break;
                }
                num++;
            }
            return true;
        }

        private static PawnPath GenerateNewPath(ref IntVec3 lastPathedTargetPosition, LocalTargetInfo destination, ref Pawn pawn, Map map, PathEndMode peMode)
        {
            lastPathedTargetPosition = destination.Cell;
            return MapExtensionUtility.GetExtensionToMap(map).getShipPathFinder.FindShipPath(pawn.Position, destination, pawn, peMode);
        }

        private static void SetupMoveIntoNextCell(ref Pawn_PathFollower instance, Pawn pawn, LocalTargetInfo destination)
        {
            if(instance.curPath.NodesLeftCount <= 1)
            {
                Log.Error(string.Concat(new object[]
                {
                    pawn,
                    " at ",
                    pawn.Position,
                    " ran out of path nodes while pathing to ",
                    destination, "."
                }), false);
                PatherFailedHelper(ref instance, pawn);
                return;
            }
            instance.nextCell = instance.curPath.ConsumeNextNode();
            if(!GenGridShips.Walkable(instance.nextCell, MapExtensionUtility.GetExtensionToMap(pawn.Map)))
            {
                Log.Error(string.Concat(new object[]
                {
                pawn,
                " entering ",
                instance.nextCell,
                " which is unwalkable."
                }), false);
            }
            int num = CostToMoveIntoCellShips(pawn, instance.nextCell);
            instance.nextCellCostTotal = (float)num;
            instance.nextCellCostLeft = (float)num;
            //Doors?
        }

        public static float CustomFloatBeach()
        {
            return (float)40 * (RimShipMod.mod.settings.beachMultiplier / 20f);
        }

        private static int PushSettlementToCoast(int tileID, Faction faction)
        {
            List<int> neighbors = new List<int>();
            Stack<int> stack = new Stack<int>();
            stack.Push(tileID);
            Stack<int> stackFull = stack;
            List<int> newTilesSearch = new List<int>();
            List<int> allSearchedTiles = new List<int>() { tileID };
            int searchTile;
            int searchedRadius = 0;

            if(Find.World.CoastDirectionAt(tileID).IsValid)
            {
                if(Find.WorldGrid[tileID].biome.canBuildBase && !(faction is null))
                    tiles.Add(new Pair<int, int>(tileID, 0));
                return tileID;
            }

            while (searchedRadius < RimShipMod.mod.settings.coastRadius)
            {
                for (int j = 0; j < stackFull.Count; j++)
                {
                    searchTile = stack.Pop();
                    GetList<int>(Find.WorldGrid.tileIDToNeighbors_offsets, Find.WorldGrid.tileIDToNeighbors_values, searchTile, neighbors);
                    int count = neighbors.Count;
                    for (int i = 0; i < count; i++)
                    {
                        if (allSearchedTiles.Any(x => x == neighbors[i]))
                            continue;
                        newTilesSearch.Add(neighbors[i]);
                        allSearchedTiles.Add(neighbors[i]);
                        if (Find.World.CoastDirectionAt(neighbors[i]).IsValid)
                        {
                            if(Find.WorldGrid[neighbors[i]].biome.canBuildBase && Find.WorldGrid[neighbors[i]].biome.implemented && Find.WorldGrid[neighbors[i]].hilliness != Hilliness.Impassable)
                            {
                                if (debug && !(faction is null)) DebugDrawSettlement(tileID, neighbors[i]);
                                if(!(faction is null))
                                    tiles.Add(new Pair<int, int>(neighbors[i], searchedRadius));
                                return neighbors[i];
                            }
                        }
                    }
                }
                stack.Clear();
                stack = new Stack<int>(newTilesSearch);
                stackFull = stack;
                newTilesSearch.Clear();
                searchedRadius++;
            }
            return tileID;
        }

        private static void GetList<T>(List<int> offsets, List<T> values, int index, List<T> outList)
        {
            outList.Clear();
            int num = offsets[index];
            int num2 = values.Count;
            if (index + 1 < offsets.Count)
            {
                num2 = offsets[index + 1];
            }
            
            for (int i = num; i < num2; i++)
            {
                outList.Add(values[i]);
            }
        }

        public static void DebugSettlementPaths()
        {
            if (drawPaths && (debugLines is null || !debugLines.Any())) return;
            if (!drawPaths) goto DrawRings;
            foreach (WorldPath wp in debugLines)
            {
                wp.DrawPath(null);
            }
            DrawRings:
                foreach(Pair<int, int> t in tiles)
                {
                    GenDraw.DrawWorldRadiusRing(t.First, t.Second);
                }
        }

        private static void DebugDrawSettlement(int from, int to)
        {
            PeaceTalks o = (PeaceTalks)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfShips.DebugSettlement);
            o.Tile = from;
            o.SetFaction(Faction.OfMechanoids);
            Find.WorldObjects.Add(o);
            if(drawPaths)
                debugLines.Add(Find.WorldPathFinder.FindPath(from, to, null, null));
        }

        private static List<Pawn> GetShipsForColonistBar(List<Map> maps, int i)
        {
            Map map = maps[i];
            List<Pawn> ships = new List<Pawn>();
            foreach(Pawn ship in map.mapPawns.AllPawnsSpawned)
            {
                if(IsShip(ship))
                {
                    //ships.Add(ship); /*  Uncomment to add Ships to colonist bar  */
                    foreach(Pawn p in ship.GetComp<CompShips>().AllPawnsAboard)
                    {
                        ships.Add(p);
                    }
                }
            }
            return ships;
        }

        #endregion HelperFunctions

        private static readonly bool disabled = true;
        public static readonly bool debug = true;
        public static readonly bool drawPaths = false;
        private static List<WorldPath> debugLines = new List<WorldPath>();
        private static List<Pair<int, int>> tiles = new List<Pair<int,int>>(); // Pair -> TileID : Cycle
    }
}