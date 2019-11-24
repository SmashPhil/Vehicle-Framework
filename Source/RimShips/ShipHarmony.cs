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
            var harmony = HarmonyInstance.Create("rimworld.boats.smashphil");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            //HarmonyInstance.DEBUG = true;

            #region Functions

            //Map Gen
            harmony.Patch(original: AccessTools.Method(type: typeof(BeachMaker), name: nameof(BeachMaker.Init)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(BeachMakerTranspiler)));
            harmony.Patch(original: AccessTools.Method(type: typeof(TileFinder), name: nameof(TileFinder.RandomSettlementTileFor)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(PushSettlementToCoastTranspiler)));
            harmony.Patch(original: AccessTools.Method(type: typeof(MapGenerator), name: nameof(MapGenerator.GenerateMap)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(GenerateMapExtension)));
            harmony.Patch(original: AccessTools.Method(typeof(Map), name: nameof(Map.ExposeData)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(ExposeDataMapExtensions)));
            harmony.Patch(original: AccessTools.Method(type: typeof(Map), name: nameof(Map.FinalizeInit)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(RecalculateShipPathGrid)));
            harmony.Patch(original: AccessTools.Method(type: typeof(PathGrid), name: nameof(PathGrid.RecalculatePerceivedPathCostUnderThing)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(RecalculateShipPathCostUnderThing))); //DOUBLE CHECK
            harmony.Patch(original: AccessTools.Method(type: typeof(TerrainGrid), name: "DoTerrainChangedEffects"), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(RecalculateShipPathCostTerrainChange)));

            //Health 
            harmony.Patch(original: AccessTools.Method(type: typeof(HealthUtility), name: nameof(HealthUtility.GetGeneralConditionLabel)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(ReplaceConditionLabel)));
            harmony.Patch(original: AccessTools.Method(type: typeof(Pawn_HealthTracker), name: "ShouldBeDowned"),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(ShipShouldBeDowned)));
            harmony.Patch(original: AccessTools.Method(type: typeof(PawnDownedWiggler), name: nameof(PawnDownedWiggler.WigglerTick)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(ShipShouldWiggle)));
            harmony.Patch(original: AccessTools.Method(type: typeof(Pawn), name: nameof(Pawn.Kill)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(KillAndDespawnShip)));
            harmony.Patch(original: AccessTools.Method(type: typeof(HediffUtility), name: nameof(HediffUtility.CanHealNaturally)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(ShipsDontHeal)));
            harmony.Patch(original: AccessTools.Method(type: typeof(HediffUtility), name: nameof(HediffUtility.CanHealFromTending)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(ShipsDontHealTended)));

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
            harmony.Patch(original: AccessTools.Method(type: typeof(SettlementBase), name: nameof(SettlementBase.GetCaravanGizmos)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(NoAttackSettlementWhenDocked)));
            harmony.Patch(original: AccessTools.Method(type: typeof(Caravan), name: nameof(Caravan.GetGizmos)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(AddAnchorGizmo)));
            harmony.Patch(original: AccessTools.Method(type: typeof(Pawn), name: nameof(Pawn.GetGizmos)), prefix: null, 
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(GetGizmosForShip)));
            harmony.Patch(original: AccessTools.Method(type: typeof(CaravanFormingUtility), name: nameof(CaravanFormingUtility.GetGizmos)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(GizmosForShipCaravans)));

            //Pathing
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_PathFollower), name: nameof(Pawn_PathFollower.StartPath)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(StartPathShip)));
            harmony.Patch(original: AccessTools.Method(type: typeof(Pawn_PathFollower), name: "TryEnterNextPathCell"), 
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(TryEnterNextCellShip)));
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_PathFollower), name: "GenerateNewPath"),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(GenerateNewShipPath)));
            harmony.Patch(original: AccessTools.Method(type: typeof(FloatMenuMakerMap), name: "GotoLocationOption"),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(GotoLocationShips)));
            harmony.Patch(original: AccessTools.Method(type: typeof(RegionLink), name: nameof(RegionLink.Deregister)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(DeregisterWaterRegion))); //MORE WORK

            //World Pathing
            harmony.Patch(original: AccessTools.Method(type: typeof(WorldPathFinder), name: nameof(WorldPathFinder.FindPath)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(FindPathWithShipTranspiler)));
            harmony.Patch(original: AccessTools.Method(type: typeof(WorldRoutePlanner), name: "TryAddWaypoint"), 
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(TryAddWayPointWater)));
            harmony.Patch(original: AccessTools.Method(type: typeof(Dialog_FormCaravan), name: nameof(Dialog_FormCaravan.DoWindowContents)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(Dialog_SetInstance)));
            harmony.Patch(original: AccessTools.Method(type: typeof(WorldReachability), parameters: new Type[] { typeof(Caravan), typeof(int) }, name: nameof(WorldReachability.CanReach)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(CanReachWithShip)));
            harmony.Patch(original: AccessTools.Method(type: typeof(CaravanTicksPerMoveUtility), parameters: new Type[] { typeof(List<Pawn>), typeof(float), typeof(float), typeof(StringBuilder)}, name: nameof(CaravanTicksPerMoveUtility.GetTicksPerMove)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(GetTicksPerMoveShips)));

            //Jobs
            harmony.Patch(original: AccessTools.Method(type: typeof(JobUtility), name: nameof(JobUtility.TryStartErrorRecoverJob)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(ShipErrorRecoverJob)));
            harmony.Patch(original: AccessTools.Method(type: typeof(JobGiver_Wander), name: "TryGiveJob"),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(ShipsDontWander)));
            harmony.Patch(original: AccessTools.Method(type: typeof(Pawn_JobTracker), name: nameof(Pawn_JobTracker.CheckForJobOverride)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(NoOverrideDamageTakenTranspiler)));

            //Caravan
            harmony.Patch(original: AccessTools.Method(type: typeof(CaravanUIUtility), name: nameof(CaravanUIUtility.AddPawnsSections)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(AddShipsSections)));
            harmony.Patch(original: AccessTools.Method(type: typeof(CaravanFormingUtility), name: nameof(CaravanFormingUtility.StartFormingCaravan)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(StartFormingCaravanForShips)));
            harmony.Patch(original: AccessTools.Method(type: typeof(Dialog_FormCaravan), name: "DoBottomButtons"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(DoBottomButtonsTranspiler)));
            harmony.Patch(original: AccessTools.Method(type: typeof(CollectionsMassCalculator), parameters: new Type[] { typeof(List<ThingCount>), typeof(StringBuilder) }, name: nameof(CollectionsMassCalculator.Capacity)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(CapacityWithShip)));
            harmony.Patch(original: AccessTools.Method(type: typeof(MassUtility), name: nameof(MassUtility.CanEverCarryAnything)),
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
                name: nameof(TransferablesShip)));
            harmony.Patch(original: AccessTools.Method(type: typeof(FloatMenuMakerMap), name: "AddHumanlikeOrders"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(AddHumanLikeOrdersTranspiler)));
            harmony.Patch(original: AccessTools.Method(type: typeof(LordToil_PrepareCaravan_GatherAnimals), name: nameof(LordToil_PrepareCaravan_GatherAnimals.UpdateAllDuties)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(UpdateDutyOfShip)));
            harmony.Patch(original: AccessTools.Method(type: typeof(Dialog_FormCaravan), parameters: new Type[] {typeof(List<Pawn>), typeof(bool), typeof(IntVec3).MakeByRefType()}, name: "TryFindExitSpot"), 
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(TryFindExitSpotShips)));
            harmony.Patch(original: AccessTools.Method(type: typeof(Dialog_FormCaravan), name: "TryFindRandomPackingSpot"),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(TryFindPackingSpotShips)));
            harmony.Patch(original: AccessTools.Method(type: typeof(ITab_Pawn_FormingCaravan), name: "FillTab"),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(FillTabShipCaravan)));
            harmony.Patch(original: AccessTools.Method(type: typeof(ITab_Pawn_FormingCaravan), name: "DoPeopleAndAnimals"), 
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(DoPeopleAnimalsAndShip)));
            harmony.Patch(original: AccessTools.Method(type: typeof(CaravanEnterMapUtility), name: "GetEnterCell"), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(GetEnterCellOffset)));
            harmony.Patch(original: AccessTools.Property(type: typeof(Caravan), name: nameof(Caravan.AllOwnersDowned)).GetGetMethod(),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(AllOwnersDownedShip)));
            harmony.Patch(original: AccessTools.Property(type: typeof(Caravan), name: nameof(Caravan.AllOwnersHaveMentalBreak)).GetGetMethod(),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(AllOwnersMentalBreakShip)));
            harmony.Patch(original: AccessTools.Method(type: typeof(Caravan), name: nameof(Caravan.GetInspectString)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(GetInspectStringShip)));
            harmony.Patch(original: AccessTools.Method(type: typeof(CaravanEnterMapUtility), name: "GetEnterCell"),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(GetEnterCellWater)));
            harmony.Patch(original: AccessTools.Method(type: typeof(Caravan), name: nameof(Caravan.Notify_MemberDied)), 
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(CaravanLostAllShips)));
            harmony.Patch(original: AccessTools.Property(type: typeof(MapPawns), name: nameof(MapPawns.AnyPawnBlockingMapRemoval)).GetGetMethod(), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(AnyShipBlockingMapRemoval)));
            harmony.Patch(original: AccessTools.Property(type: typeof(Caravan), name: nameof(Caravan.NightResting)).GetGetMethod(),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(NoRestForBoats)));

            //Draftable
            harmony.Patch(original: AccessTools.Property(type: typeof(Pawn_DraftController), name: nameof(Pawn_DraftController.Drafted)).GetSetMethod(),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(DraftedShipsCanMove)));
            harmony.Patch(original: AccessTools.Method(type: typeof(FloatMenuMakerMap), name: "CanTakeOrder"), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(CanShipTakeOrder)));
            harmony.Patch(original: AccessTools.Method(type: typeof(FloatMenuUtility), name: nameof(FloatMenuUtility.GetMeleeAttackAction)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(NoMeleeForShips))); //Change..?
            harmony.Patch(original: AccessTools.Method(type: typeof(Projectile_Explosive), name: "Impact"),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(ShellsImpactWater)));
            harmony.Patch(original: AccessTools.Method(type: typeof(Pawn), name: nameof(Pawn.DrawExtraSelectionOverlays)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(DrawESOShipsTranspiler)));
            harmony.Patch(original: AccessTools.Method(type: typeof(PawnComponentsUtility), name: nameof(PawnComponentsUtility.AddAndRemoveDynamicComponents)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(AddAndRemoveShipComponents)));

            //Construction
            harmony.Patch(original: AccessTools.Method(type: typeof(Frame), name: nameof(Frame.CompleteConstruction)),
                prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(CompleteConstructionShip)));

            //Extra
            harmony.Patch(original: AccessTools.Property(type: typeof(MapPawns), name: nameof(MapPawns.FreeColonistsSpawnedOrInPlayerEjectablePodsCount)).GetGetMethod(), prefix: null,
                postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                name: nameof(FreeColonistsInShips)));

            //Debug
            if(debug)
            {
                harmony.Patch(original: AccessTools.Method(type: typeof(WorldRoutePlanner), name: nameof(WorldRoutePlanner.WorldRoutePlannerUpdate)), prefix: null,
                    postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                    name: nameof(DebugSettlementPaths)));
                harmony.Patch(original: AccessTools.Method(type: typeof(WorldObjectsHolder), name: nameof(WorldObjectsHolder.Add)),
                    prefix: new HarmonyMethod(type: typeof(ShipHarmony),
                    name: nameof(DebugWorldObjects)));   
            }
            harmony.Patch(original: AccessTools.Method(type: typeof(RegionGrid), name: nameof(RegionGrid.DebugDraw)), prefix: null,
                    postfix: new HarmonyMethod(type: typeof(ShipHarmony),
                    name: nameof(DebugDrawWaterRegion)));
            #endregion Functions
        }

        #region Debug
        public static void DebugWorldObjects(WorldObject o)
        {
            if(o is Settlement)
            {
                tiles.Add(new Pair<int, int>(o.Tile, 0));
            }
        }

        public static void DebugDrawWaterRegion(Map ___map)
        {
            MapExtensionUtility.GetExtensionToMap(___map)?.getWaterRegionGrid?.DebugDraw();
        }

        #endregion Debug


        #region MapGen
        public static IEnumerable<CodeInstruction> BeachMakerTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for(int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if(instruction.opcode == OpCodes.Call && instruction.operand == AccessTools.Property(type: typeof(FloatRange), name: nameof(FloatRange.RandomInRange)).GetGetMethod())
                {
                    i++;
                    instruction = instructionList[i];
                    yield return new CodeInstruction(opcode: OpCodes.Pop);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(ShipHarmony), name: nameof(ShipHarmony.CustomFloatBeach)));
                }
                yield return instruction;
            }
        }

        public static IEnumerable<CodeInstruction> PushSettlementToCoastTranspiler(IEnumerable<CodeInstruction> instructions)
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

        public static void RecalculateShipPathGrid(Map __instance)
        {
            MapExtension mapE = MapExtensionUtility.GetExtensionToMap(__instance);
            mapE?.getShipPathGrid?.RecalculateAllPerceivedPathCosts();

            if (mapE != null && mapE.getWaterRegionAndRoomUpdater != null)
            {
                mapE.getWaterRegionAndRoomUpdater.Enabled = true;
            }
            mapE?.getWaterRegionAndRoomUpdater?.RebuildAllWaterRegions();
            Log.Message("[Boats]: Water Regions built");
        }

        public static void RecalculateShipPathCostUnderThing(Thing t, Map ___map)
        {
            if (t is null) return;
            MapExtensionUtility.GetExtensionToMap(___map)?.getShipPathGrid?.RecalculatePerceivedPathCostUnderThing(t);
        }

        private static void RecalculateShipPathCostTerrainChange(IntVec3 c, Map ___map)
        {
            MapExtensionUtility.GetExtensionToMap(___map)?.getShipPathGrid?.RecalculatePerceivedPathCostAt(c);
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

        public static bool ShipShouldBeDowned(ref bool __result, ref Pawn ___pawn)
        {
            if (___pawn != null && IsShip(___pawn))
            {
                __result = ___pawn.GetComp<CompShips>().Props.downable;
                return false;
            }
            return true;
        }

        public static bool ShipShouldWiggle(ref Pawn ___pawn)
        {
            if (___pawn != null && IsShip(___pawn) && !___pawn.GetComp<CompShips>().Props.movesWhenDowned)
            {
                return false;
            }
            return true;
        }

        public static bool KillAndDespawnShip(DamageInfo? dinfo, Pawn __instance)
        {
            if(IsShip(__instance))
            {
                IntVec3 position = __instance.PositionHeld;
                float angle = __instance.GetComp<CompShips>().Angle;
                Rot4 rotation = __instance.Rotation;

                Map map = __instance.Map;
                Map mapHeld = __instance.MapHeld;
                bool flag = __instance.Spawned;
                bool worldPawn = __instance.IsWorldPawn();
                Caravan caravan = __instance.GetCaravan();
                ThingDef shipDef = __instance.GetComp<CompShips>().Props.buildDef;

                Thing thing = ThingMaker.MakeThing(shipDef);
                thing.SetFactionDirect(__instance.Faction);
                thing.HitPoints = __instance.MaxHitPoints / 3;

                if (Current.ProgramState == ProgramState.Playing)
                {
                    Find.Storyteller.Notify_PawnEvent(__instance, AdaptationEvent.Died, null);
                }
                if(flag && dinfo != null && dinfo.Value.Def.ExternalViolenceFor(__instance))
                {
                    LifeStageUtility.PlayNearestLifestageSound(__instance, (LifeStageAge ls) => ls.soundDeath, 1f);
                }
                if(dinfo != null && dinfo.Value.Instigator != null)
                {
                    Pawn pawn = dinfo.Value.Instigator as Pawn;
                    if(pawn != null)
                    {
                        RecordsUtility.Notify_PawnKilled(__instance, pawn);
                    }
                }

                if(__instance.GetLord() != null)
                {
                    __instance.GetLord().Notify_PawnLost(__instance, PawnLostCondition.IncappedOrKilled, dinfo);
                }
                if(flag)
                {
                    __instance.DropAndForbidEverything(false);
                }

                __instance.meleeVerbs.Notify_PawnKilled();
                if(flag)
                {
                    if (map.terrainGrid.TerrainAt(position) == TerrainDefOf.WaterOceanDeep || map.terrainGrid.TerrainAt(position) == TerrainDefOf.WaterDeep)
                    {
                        __instance.GetComp<CompShips>().KillAll();
                        IntVec3 lookCell = __instance.Position;
                        string textPawnList = "";
                        foreach (Pawn p in __instance?.GetComp<CompShips>()?.AllPawnsAboard)
                        {
                            textPawnList += p.LabelShort + ". ";
                        }
                        Find.LetterStack.ReceiveLetter("ShipSunkLabel".Translate(), "ShipSunkDeep".Translate(__instance.LabelShort, textPawnList), LetterDefOf.NegativeEvent, new TargetInfo(lookCell, map, false), null, null);
                        __instance.Destroy();
                        return false;
                    }
                    else
                    {
                        Find.LetterStack.ReceiveLetter("ShipSunkLabel".Translate(), "ShipSunkShallow".Translate(__instance.LabelShort), LetterDefOf.NegativeEvent, new TargetInfo(position, map, false), null, null);
                        __instance.GetComp<CompShips>().DisembarkAll();
                        __instance.Destroy();
                    }
                }
                GenSpawn.Spawn(thing, position, map, rotation, WipeMode.Vanish, false);

                return false;
            }
            return true;
        }

        public static bool ShipsDontHeal(Hediff_Injury hd, ref bool __result)
        {
            Pawn pawn = Traverse.Create(hd).Field("pawn").GetValue<Pawn>();
            if(IsShip(pawn))
            {
                __result = false;
                return false;
            }
            return true;
        }

        public static bool ShipsDontHealTended(Hediff_Injury hd, ref bool __result)
        {
            Pawn pawn = Traverse.Create(hd).Field("pawn").GetValue<Pawn>();
            
            if (IsShip(pawn))
            {
                __result = false;
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

        public static IEnumerable<CodeInstruction> CheckRecacheEntriesTranspiler(IEnumerable<CodeInstruction> instructions)
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
                if ((instruction.opcode == OpCodes.Callvirt && instruction.operand == AccessTools.Method(type: typeof(List<Pawn>), name: nameof(List<Pawn>.AddRange))) &&
                    (instructionList[i-1].opcode == OpCodes.Callvirt && instructionList[i-1].operand == AccessTools.Property(type: typeof(Caravan), name: nameof(Caravan.PawnsListForReading)).GetGetMethod()))
                {
                    yield return instruction; //CALLVIRT : AddRange
                    instruction = instructionList[++i];

                    yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(type: typeof(ColonistBar), "tmpPawns"));
                    yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(type: typeof(ColonistBar), "tmpCaravans"));
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, 9);
                    yield return new CodeInstruction(opcode: OpCodes.Callvirt, operand: AccessTools.Property(type: typeof(List<Caravan>), name: "Item").GetGetMethod());
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(ShipHarmony), name: nameof(ShipHarmony.ExtractPawnsFromCaravan)));

                    yield return new CodeInstruction(opcode: OpCodes.Callvirt, operand: AccessTools.Method(type: typeof(List<Pawn>), name: nameof(List<Pawn>.AddRange)));
                }

                yield return instruction;
            }
        }

        private static void DrawIconsShips(Rect rect, Pawn colonist)
        {
            if (colonist.Dead)
            {
                return;
            }
            float num = 20f * Find.ColonistBar.Scale;
            Vector2 vector = new Vector2(rect.x + 1f, rect.yMax - num - 1f);
            
            List<Pawn> ships = Find.CurrentMap.mapPawns.AllPawnsSpawned.FindAll(x => !(x.GetComp<CompShips>() is null));
            Pawn p = ships.Find(x => x.GetComp<CompShips>().AllPawnsAboard.Contains(colonist));
            if(!(p is null))
            {
                Rect rect2 = new Rect(vector.x, vector.y, num, num);
                GUI.DrawTexture(rect2, TexCommandShips.OnboardIcon);
                TooltipHandler.TipRegion(rect2, "ActivityIconOnBoardShip".Translate(p.Label));
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

        private static void CanShipTakeOrder(Pawn pawn, ref bool __result)
        {
            if(__result is false)
            {
                __result = IsShip(pawn);
            }
        }

        public static bool NoMeleeForShips(Pawn pawn, LocalTargetInfo target, out string failStr)
        {
            if(IsShip(pawn))
            {
                failStr = "IsIncapableOfRamming".Translate(target.Thing.LabelShort);
                //Add more to string or Action if ramming is implemented
                return false;
            }
            failStr = string.Empty;
            return true;
        }

        private static bool ShellsImpactWater(Thing hitThing, ref Projectile __instance)
        {
            Map map = __instance.Map;
            TerrainDef terrainImpact = map.terrainGrid.TerrainAt(__instance.Position);
            if(__instance.def.projectile.explosionDelay == 0 && terrainImpact.IsWater && !__instance.Position.GetThingList(__instance.Map).Any(x => IsShip(x as Pawn)))
            {
                __instance.Destroy(DestroyMode.Vanish);
                if (__instance.def.projectile.explosionEffect != null)
                {
                    Effecter effecter = __instance.def.projectile.explosionEffect.Spawn();
                    effecter.Trigger(new TargetInfo(__instance.Position, map, false), new TargetInfo(__instance.Position, map, false));
                    effecter.Cleanup();
                }
                IntVec3 position = __instance.Position;
                Map map2 = map;

                int waterDepth = map.terrainGrid.TerrainAt(__instance.Position).IsWater ? 2 : 0;
                if (waterDepth == 0) Log.Error("Impact Water Depth is 0, but terrain is water.");
                float explosionRadius = (__instance.def.projectile.explosionRadius / (2f * waterDepth));
                if (explosionRadius < 1) explosionRadius = 1f;
                DamageDef damageDef = __instance.def.projectile.damageDef;
                Thing launcher = null;
                int damageAmount = __instance.DamageAmount;
                float armorPenetration = __instance.ArmorPenetration;
                SoundDef soundExplode;
                if (__instance.def.HasModExtension<Projectile_Water>()) { soundExplode = __instance.def.GetModExtension<Projectile_Water>().soundExplodeWater; }
                else { soundExplode = __instance.def.projectile.soundHitThickRoof; Log.Warning("Missing Water Explosion sound from " + __instance); }
                SoundStarter.PlayOneShot(__instance.def.projectile.soundExplode, new TargetInfo(__instance.Position, map, false));
                ThingDef equipmentDef = null;
                ThingDef def = __instance.def;
                Thing thing = null;
                ThingDef postExplosionSpawnThingDef = __instance.def.projectile.postExplosionSpawnThingDef;
                float postExplosionSpawnChance = 0.0f;
                float chanceToStartFire = __instance.def.projectile.explosionChanceToStartFire * 0.0f;
                int postExplosionSpawnThingCount = __instance.def.projectile.postExplosionSpawnThingCount;
                ThingDef preExplosionSpawnThingDef = __instance.def.projectile.preExplosionSpawnThingDef;
                GenExplosion.DoExplosion(position, map2, explosionRadius, damageDef, launcher, damageAmount, armorPenetration, soundExplode,
                    equipmentDef, def, thing, postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount,
                    __instance.def.projectile.applyDamageToExplosionCellsNeighbors, preExplosionSpawnThingDef, __instance.def.projectile.preExplosionSpawnChance,
                    __instance.def.projectile.preExplosionSpawnThingCount, chanceToStartFire, __instance.def.projectile.explosionDamageFalloff);
                return false;
            }
            return true;
        }

        public static IEnumerable<CodeInstruction> DrawESOShipsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Call && instruction.operand == AccessTools.Property(type: typeof(Pawn), name: nameof(Pawn.IsColonistPlayerControlled)).GetGetMethod())
                {
                    yield return instruction; //CALL : IsColonistPlayerControlled
                    instruction = instructionList[++i];

                    Label label = ilg.DefineLabel();
                    Label label2 = ilg.DefineLabel();

                    yield return new CodeInstruction(opcode: OpCodes.Brtrue, label);
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(ShipHarmony), name: nameof(ShipHarmony.IsShip)));
                    yield return new CodeInstruction(opcode: OpCodes.Br, label2);

                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_1) { labels = new List<Label> { label } };

                    instruction.labels.Add(label2);
                }

                yield return instruction;
            }
        }

        public static void AddAndRemoveShipComponents(Pawn pawn, bool actAsIfSpawned = false)
        {
            if(IsShip(pawn) && (pawn.Spawned || actAsIfSpawned) && pawn.drafter is null)
            {
                pawn.drafter = new Pawn_DraftController(pawn);
                pawn.trader = new Pawn_TraderTracker(pawn);
                pawn.training = new Pawn_TrainingTracker(pawn);
                pawn.story = new Pawn_StoryTracker(pawn);
                pawn.playerSettings = new Pawn_PlayerSettings(pawn);
            }
        }
        #endregion Drafting

        #region Gizmos
        public static bool CheckForShipAttack(JobDriver_Wait __instance)
        {
            if (__instance.pawn?.GetComp<CompShips>()?.weaponStatus == ShipWeaponStatus.Offline)
            {
                return false;
            }
            return true;
        }

        public static void NoAttackSettlementWhenDocked(Caravan caravan, ref IEnumerable<Gizmo> __result, SettlementBase __instance)
        {
            if (HasShip(caravan))
            {
                if (!caravan.pather.Moving && CaravanVisitUtility.SettlementVisitedNow(caravan) != null && caravan.PawnsListForReading.Any(x => !IsShip(x)))
                {
                    List<Gizmo> gizmos = __result.ToList();
                    int index = gizmos.FindIndex(x => (x as Command_Action).icon == SettlementBase.AttackCommand);
                    gizmos[index].Disable("CommandAttackDockDisable".Translate(__instance.LabelShort));
                    __result = gizmos;
                }
            }
        }

        public static void AddAnchorGizmo(ref IEnumerable<Gizmo> __result, Caravan __instance)
        {
            if(HasShip(__instance) && Find.World.CoastDirectionAt(__instance.Tile).IsValid)
            {
                if(!__instance.pather.Moving && !__instance.PawnsListForReading.Any(x => !IsShip(x)))
                {
                    Command_Action gizmo = new Command_Action();
                    gizmo.icon = TexCommandShips.Anchor;
                    gizmo.defaultLabel = "CommandDockShip".Translate();
                    gizmo.defaultDesc = "CommandDockShipDesc".Translate(__instance.Label);
                    gizmo.action = delegate ()
                    {
                        ShipHarmony.ToggleDocking(__instance, true);
                    };

                    List<Gizmo> gizmos = __result.ToList();
                    gizmos.Add(gizmo);
                    __result = gizmos;
                }
                else if (!__instance.pather.Moving && __instance.PawnsListForReading.Any(x => !IsShip(x)))
                {
                    Command_Action gizmo = new Command_Action();
                    gizmo.icon = TexCommandShips.UnloadAll;
                    gizmo.defaultLabel = "CommandUndockShip".Translate();
                    gizmo.defaultDesc = "CommandUndockShipDesc".Translate(__instance.Label);
                    gizmo.action = delegate ()
                    {
                        ShipHarmony.ToggleDocking(__instance, false);
                    };

                    List<Gizmo> gizmos = __result.ToList();
                    gizmos.Add(gizmo);
                    __result = gizmos;
                }
            }
        }

        public static void GetGizmosForShip(ref IEnumerable<Gizmo> __result, Pawn __instance)
        {
            if(IsShip(__instance))
            {
                List<Gizmo> gizmos = __result.ToList();

                if(__instance.drafter != null)
                {
                    IEnumerable<Gizmo> draftGizmos = (IEnumerable<Gizmo>)AccessTools.Method(type: typeof(Pawn_DraftController), name: "GetGizmos").Invoke(__instance.drafter, null);
                    foreach(Gizmo c in draftGizmos)
                    {
                        gizmos.Add(c);
                    }

                    foreach(Gizmo c2 in __instance.GetComp<CompShips>().CompGetGizmosExtra())
                    {
                        gizmos.Add(c2);
                    }
                }
                __result = gizmos;
            }
        }

        public static void GizmosForShipCaravans(ref IEnumerable<Gizmo> __result, Pawn pawn, Texture2D ___AddToCaravanCommand)
        {
            Command_Action joinCaravan = new Command_Action();
            if(pawn.Spawned)
            {
                bool anyCaravanToJoin = false;
                foreach (Lord lord in pawn.Map.lordManager.lords)
                {
                    if(lord.faction == Faction.OfPlayer && lord.LordJob is LordJob_FormAndSendCaravanShip && !(lord.CurLordToil is LordToil_PrepareCaravan_LeaveShip) && !(lord.CurLordToil is LordToil_PrepareCaravan_BoardShip))
                    {
                        anyCaravanToJoin = true;
                        break;
                    }
                }
                if(anyCaravanToJoin && Dialog_FormCaravan.AllSendablePawns(pawn.Map, false).Contains(pawn))
                {
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
                                if(lord.faction == Faction.OfPlayer && lord.LordJob is LordJob_FormAndSendCaravanShip)
                                {
                                    list.Add(lord);
                                }
                            }
                            if (list.Count <= 0)
                                return;
                            if(list.Count == 1)
                            {
                                AccessTools.Method(type: typeof(CaravanFormingUtility), name: "LateJoinFormingCaravan").Invoke(null, new object[] { pawn, list[0] });
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
                                            AccessTools.Method(type: typeof(CaravanFormingUtility), name: "LateJoinFormingCaravan").Invoke(null, new object[] { pawn, caravanLocal });
                                        } 
                                    }, MenuOptionPriority.Default, null, null, 0f, null, null));
                                }
                                Find.WindowStack.Add(new FloatMenu(list2));
                            }
                        },
                        hotKey = KeyBindingDefOf.Misc7
                    };
                }
            }
            List<Gizmo> gizmos = __result.ToList();
            gizmos.Add(joinCaravan);
            __result = gizmos;
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
                if (!MapExtensionUtility.GetExtensionToMap(___pawn.Map)?.getShipReachability?.CanReachShip(___pawn.Position, dest, peMode, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)) ?? false)
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
                if (ShipReachabilityImmediate.CanReachImmediateShip(___pawn, dest, peMode))
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
        public static bool ShipErrorRecoverJob(Pawn pawn, string message, Exception exception = null, JobDriver concreteDriver = null)
        {
            if(IsShip(pawn))
            {
                if (!(pawn.jobs is null))
                {
                    if (!(pawn.jobs.curJob is null))
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Errored, false);
                    }
                    try
                    {
                        if (pawn.jobs.jobQueue.Count > 0)
                        {
                            Job job = pawn.jobs.jobQueue.Dequeue().job;
                            pawn.jobs.StartJob(job, JobCondition.Succeeded);
                        }
                        else
                        {
                            pawn.jobs.StartJob(new Job(JobDefOf_Ships.IdleShip, 150, false), JobCondition.None, null, false, true, null, null, false);
                        }  
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

        private static bool ShipsDontWander(Pawn pawn, ref Job __result)
        {
            if(IsShip(pawn))
            {
                __result = new Job(JobDefOf_Ships.IdleShip);
                return false;
            }
            return true;
        }

        public static IEnumerable<CodeInstruction> NoOverrideDamageTakenTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if(instruction.opcode == OpCodes.Stloc_1)
                {
                    yield return instruction; //STLOC.1
                    instruction = instructionList[++i];
                    Label label = ilg.DefineLabel();
                    Label retlabel = ilg.DefineLabel();

                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_0);
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, retlabel);

                    yield return new CodeInstruction(opcode: OpCodes.Ldloca_S, operand: 1);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Property(type: typeof(ThinkResult), name: nameof(ThinkResult.IsValid)).GetGetMethod());
                    yield return new CodeInstruction(opcode: OpCodes.Brtrue, label);

                    yield return new CodeInstruction(opcode: OpCodes.Ret) { labels = new List<Label> { retlabel } };

                    instruction.labels.Add(label);
                }

                yield return instruction;
            }
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
                List<Pawn> ships = pawns.Where(x => IsShip(x)).ToList();
                List<Pawn> capablePawns = pawns.Where(x => !IsShip(x) && x.IsColonist && !x.Downed && !x.Dead).ToList();
                List<Pawn> prisoners = pawns.Where(x => !IsShip(x) && !x.IsColonist && !x.RaceProps.Animal).ToList();
                int seats = 0;
                foreach (Pawn ship in ships)
                {
                    seats += ship.GetComp<CompShips>().SeatsAvailable;
                }
                if ((pawns.Where(x => x.GetComp<CompShips>() is null).ToList().Count + downedPawns.Count) > seats)
                {
                    Log.Error("Can't start forming caravan with ship(s) selected and not enough seats to house all pawns. Seats: " + seats + " Pawns boarding: " +
                        (pawns.Where(x => x.GetComp<CompShips>() is null).ToList().Count + downedPawns.Count), false);
                    return false;
                }

                LordJob_FormAndSendCaravanShip lordJob = new LordJob_FormAndSendCaravanShip(list, ships, capablePawns, downedPawns, prisoners, meetingPoint, exitSpot, startingTile,
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
            Label breakLabel = ilg.DefineLabel();

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Ldloc_S && ((LocalBuilder)instruction.operand).LocalIndex == 19)
                {
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, operand: 19);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(ShipHarmony), name: nameof(CanSetSail)));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, operand: breakLabel);
                }
                else if (instruction.opcode == OpCodes.Ldfld && instruction.operand == AccessTools.Field(type: typeof(Dialog_FormCaravan), name: "destinationTile"))
                {
                    instructionList[i - 1].labels.Add(breakLabel);
                }
                yield return instruction;
            }
        }

        public static bool CapacityWithShip(List<ThingCount> thingCounts, ref float __result, StringBuilder explanation = null)
        {
            if(thingCounts.Any(x => IsShip(x.Thing as Pawn)))
            {
                float num = 0f;
                foreach(ThingCount tc in thingCounts)
                {
                    if(tc.Count > 0)
                    {
                        if (tc.Thing is Pawn && IsShip(tc.Thing as Pawn))
                        {
                            num += MassUtility.Capacity(tc.Thing as Pawn, explanation) * (float)tc.Count;
                        }
                    }
                }
                __result = Mathf.Max(num, 0f);
                return false;
            }
            return true;
        }
        public static bool CanCarryIfShip(Pawn p, ref bool __result)
        {
            return p?.GetComp<CompShips>() is null ? true : !(__result = true);
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

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_0)
                {
                    Label label = ilg.DefineLabel();

                    i++;
                    yield return instruction;
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_0);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(ShipHarmony), name: nameof(IsShip)));
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
                if(instruction.opcode == OpCodes.Callvirt && instruction.operand == AccessTools.Property(type: typeof(Lord), name: nameof(Lord.LordJob)).GetGetMethod())
                {
                    yield return instruction;
                    instruction = instructionList[++i];
                    Label label = ilg.DefineLabel();
                    Label label2 = ilg.DefineLabel();

                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(CaravanUtility), name: nameof(CaravanUtility.GetCaravan)));
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(ShipHarmony), parameters: new Type[] { typeof(Caravan) }, name: nameof(ShipHarmony.HasShip)));
                    yield return new CodeInstruction(opcode: OpCodes.Brtrue, label);

                    yield return instruction; //CASTCLASS : LordJob_FormAndSendCaravan
                    instruction = instructionList[++i]; 
                    yield return instruction; //STLOC_S : 50
                    instruction = instructionList[++i];
                    yield return instruction; //LDLOC_S : 49
                    instruction = instructionList[++i];
                    yield return instruction; //LDLOC_S : 50
                    instruction = instructionList[++i];
                    yield return instruction; //CALL : CapacityLeft
                    instruction = instructionList[++i];
                    yield return new CodeInstruction(opcode: OpCodes.Br, label2);

                    yield return new CodeInstruction(opcode: OpCodes.Pop) { labels = new List<Label> { label } };
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, operand: 49);
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(LordUtility), name: "GetLord"));
                    yield return new CodeInstruction(opcode: OpCodes.Callvirt, operand: AccessTools.Property(type: typeof(Lord), name: nameof(Lord.LordJob)).GetGetMethod());
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(ShipHarmony), name: nameof(ShipHarmony.CapacityLeft)));

                    instruction.labels.Add(label2);
                    yield return instruction; //STFLD
                    instruction = instructionList[++i];
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

        private static bool TryFindExitSpotShips(List<Pawn> pawns, bool reachableForEveryColonist, out IntVec3 spot, Map ___map, int ___startingTile, ref bool __result)
        {
            if(pawns.Any(x => IsShip(x)))
            {
                //Rot4 rotFromTo = Find.WorldGrid.GetRotFromTo(__instance.CurrentTile, ___startingTile); WHEN WORLD GRID IS ESTABLISHED
                Rot4 rotFromTo = Find.World.CoastDirectionAt(___map.Tile);
                __result = TryFindExitSpotOnWater(pawns, reachableForEveryColonist, rotFromTo, out spot, ___startingTile, ___map) || TryFindExitSpotOnWater(pawns, reachableForEveryColonist, rotFromTo.Rotated(RotationDirection.Clockwise),
                    out spot, ___startingTile, ___map) || TryFindExitSpotOnWater(pawns, reachableForEveryColonist, rotFromTo.Rotated(RotationDirection.Counterclockwise), out spot, ___startingTile, ___map);
                
                float offset = pawns.FindAll(x => IsShip(x)).Max(x => x.kindDef.lifeStages.First().bodyGraphicData.drawSize.x > x.kindDef.lifeStages.First().bodyGraphicData.drawSize.y
                    ? x.kindDef.lifeStages.First().bodyGraphicData.drawSize.x : x.kindDef.lifeStages.First().bodyGraphicData.drawSize.y);
                PushCellByOffset(offset, ref spot, ___map);
                return false;
            }
            spot = IntVec3.Invalid;
            return true;
        }
        private static bool TryFindExitSpotOnWater(List<Pawn> pawns, bool reachableForEveryColonist, Rot4 exitDirection, out IntVec3 spot, int startingTile, Map map)
        {
            if(startingTile < 0)
            {
                Log.Error("Can't find exit spot because startingTile is not set.", false);
                spot = IntVec3.Invalid;
                return false;
            }

            bool validator(IntVec3 x) => !x.Fogged(map) && GenGridShips.Standable(x, map, MapExtensionUtility.GetExtensionToMap(map));

            IntVec3 intVec = IntVec3.Invalid;
            int num = -1;
            List<IntVec3> cells = CellRect.WholeMap(map).GetEdgeCells(exitDirection).ToList();
            Pawn leadShip = pawns.First(x => IsShip(x) && x.RaceProps.baseBodySize == pawns.Max(y => y.RaceProps.baseBodySize));
            Dictionary<IntVec3, float> cellDist = new Dictionary<IntVec3, float>();

            foreach(IntVec3 c in cells)
            {
                float dist = (float)(Math.Sqrt(Math.Pow((c.x - leadShip.Position.x), 2) + Math.Pow((c.z - leadShip.Position.z), 2)));
                cellDist.Add(c, dist);
            }
            cellDist = cellDist.OrderBy(x => x.Value).ToDictionary(z => z.Key, y => y.Value);

            for(int i = 0; i < cells.Count; i++)
            {
                IntVec3 iV2 = cellDist.Keys.ElementAt(i);
                if(validator(iV2))
                {
                    int num2 = 0;
                    foreach(Pawn p in pawns)
                    {
                        if(IsShip(p) && !p.Downed && !p.GetComp<CompShips>().beached && ShipReachabilityUtility.CanReachShip(p, iV2, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
                        {
                            num2++;
                        }
                    }
                    if(num2 > num)
                    {
                        num = num2;
                        intVec = iV2;
                    }
                }
            }
            spot = intVec;
            return intVec.IsValid;
        }

        private static bool TryFindPackingSpotShips(IntVec3 exitSpot, out IntVec3 packingSpot, ref bool __result, ref List<Thing> ___tmpPackingSpots, Dialog_FormCaravan __instance, Map ___map)
        {
            if(__instance.transferables.Any(x => x.ThingDef.category is ThingCategory.Pawn && IsShip(x.AnyThing as Pawn)))
            {
                ___tmpPackingSpots.Clear();
                List<Thing> list = ___map.listerThings.ThingsOfDef(ThingDefOf.CaravanPackingSpot);
                TraverseParms traverseParms = TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false);
                foreach(Thing t in list)
                {
                    if(MapExtensionUtility.GetExtensionToMap(___map)?.getShipReachability?.CanReachShip(exitSpot, t, PathEndMode.OnCell, traverseParms) ?? false)
                        ___tmpPackingSpots.Add(t);
                }
                if(___tmpPackingSpots.Any())
                {
                    Thing t = ___tmpPackingSpots.RandomElement<Thing>();
                    ___tmpPackingSpots.Clear();
                    packingSpot = t.Position;
                    __result = true;
                    return false;
                }
                List<TransferableOneWay> ships = __instance.transferables.Where(x => x.AnyThing is Pawn && IsShip(x.AnyThing as Pawn)).ToList();

                for(int i = 0; i < ships.Count; i++)
                {
                    Pawn s = ships[i].AnyThing as Pawn;
                    if(!(s is null) && ___map.terrainGrid.TerrainAt(s.Position).passability == Traversability.Impassable)
                    {
                        Messages.Message("ShipNotDockedCancelCaravan".Translate(s), MessageTypeDefOf.RejectInput, false);
                        packingSpot = IntVec3.Invalid;
                        return false;
                    }
                }
                Pawn first = ships.First().AnyThing as Pawn;
                __result = RCellFinder.TryFindRandomSpotJustOutsideColony(first.Position, ___map, out packingSpot);
                return false;
            }
            packingSpot = IntVec3.Invalid;
            return true;
        }

        private static bool FillTabShipCaravan(ITab_Pawn_FormingCaravan __instance, ref List<Thing> ___thingsToSelect, Vector2 ___size, 
            ref float ___lastDrawnHeight, Vector2 ___scrollPosition, ref List<Thing> ___tmpSingleThing)
        {
            if((Find.Selector.SingleSelectedThing as Pawn).GetLord().LordJob is LordJob_FormAndSendCaravanShip)
            {
                ___thingsToSelect.Clear();
                Rect outRect = new Rect(default(Vector2), ___size).ContractedBy(10f);
                outRect.yMin += 20f;
                Rect rect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(___lastDrawnHeight, outRect.height));
                Widgets.BeginScrollView(outRect, ref ___scrollPosition, rect, true);
                float num = 0f;
                string status = ((LordJob_FormAndSendCaravanShip)(Find.Selector.SingleSelectedThing as Pawn).GetLord().LordJob).Status;
                Widgets.Label(new Rect(0f, num, rect.width, 100f), status);
                num += 22f;
                num += 4f;
                object[] method1Args = new object[2] { rect, num };
                MethodInfo doPeopleAndAnimals = AccessTools.Method(type: typeof(ITab_Pawn_FormingCaravan), name: "DoPeopleAndAnimals");
                doPeopleAndAnimals.Invoke(__instance, method1Args);
                num = (float)method1Args[1];
                num += 4f;
                DoItemsListForShip(rect, ref num, ref ___tmpSingleThing, __instance);
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

        private static bool DoPeopleAnimalsAndShip(Rect inRect, ref float curY, ITab_Pawn_FormingCaravan __instance, ref List<Thing> ___tmpPawns)
        {
            if((Find.Selector.SingleSelectedThing as Pawn).GetLord().LordJob is LordJob_FormAndSendCaravanShip)
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
                    if(IsShip(pawn))
                    {
                        if(pawn.GetComp<CompShips>().AllPawnsAboard.Any())
                        {
                            num += pawn.GetComp<CompShips>().AllPawnsAboard.FindAll(x => x.IsFreeColonist).Count;
                            num2 += pawn.GetComp<CompShips>().AllPawnsAboard.FindAll(x => x.IsFreeColonist && x.InMentalState).Count;
                            num3 += pawn.GetComp<CompShips>().AllPawnsAboard.FindAll(x => x.IsPrisoner).Count;
                            num4 += pawn.GetComp<CompShips>().AllPawnsAboard.FindAll(x => x.IsPrisoner && x.InMentalState).Count;
                            num5 += pawn.GetComp<CompShips>().AllPawnsAboard.FindAll(x => x.RaceProps.Animal).Count;
                            num6 += pawn.GetComp<CompShips>().AllPawnsAboard.FindAll(x => x.RaceProps.Animal && x.InMentalState).Count;
                            num7 += pawn.GetComp<CompShips>().AllPawnsAboard.FindAll(x => x.RaceProps.Animal && x.RaceProps.packAnimal).Count;
                        }
                        if(!pawn.GetComp<CompShips>().beached)
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
                MethodInfo getPawnsCountLabel = AccessTools.Method(type: typeof(ITab_Pawn_FormingCaravan), name: "GetPawnsCountLabel");
                string pawnsCountLabel = (string)getPawnsCountLabel.Invoke(__instance, new object[] { num, num2, -1 });
                string pawnsCountLabel2 = (string)getPawnsCountLabel.Invoke(__instance, new object[] { num3, num4, -1 });
                string pawnsCountLabel3 = (string)getPawnsCountLabel.Invoke(__instance, new object[] { num5, num6, num7 });
                string pawnsCountLabelShip = (string)getPawnsCountLabel.Invoke(__instance, new object[] { numShip, -1, -1});

                MethodInfo doPeopleAndAnimalsEntry = AccessTools.Method(type: typeof(ITab_Pawn_FormingCaravan), name: "DoPeopleAndAnimalsEntry");

                float y = curY;
                float num8;
                object[] m1args = new object[] { inRect, Faction.OfPlayer.def.pawnsPlural.CapitalizeFirst(), pawnsCountLabel, curY, null };
                doPeopleAndAnimalsEntry.Invoke(__instance, m1args);
                curY = (float)m1args[3];
                num8 = (float)m1args[4];

                float yShip = curY;
                float numS;
                object[] mSargs = new object[] { inRect, "CaravanShips".Translate(), pawnsCountLabelShip, curY, null };
                doPeopleAndAnimalsEntry.Invoke(__instance, mSargs);
                curY = (float)mSargs[3];
                numS = (float)mSargs[4];

                float y2 = curY;
                float num9;
                object[] m2args = new object[] { inRect, "CaravanPrisoners".Translate(), pawnsCountLabel2, curY, null };
                doPeopleAndAnimalsEntry.Invoke(__instance, m2args);
                curY = (float)m2args[3];
                num9 = (float)m2args[4];

                float y3 = curY;
                float num10;
                object[] m3args = new object[] { inRect, "CaravanAnimals".Translate(), pawnsCountLabel3, curY, null };
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
                    AccessTools.Method(type: typeof(ITab_Pawn_FormingCaravan), name: "HighlightColonists").Invoke(__instance, null);
                }
                if (Widgets.ButtonInvisible(rect, false))
                {
                    AccessTools.Method(type: typeof(ITab_Pawn_FormingCaravan), name: "SelectColonistsLater").Invoke(__instance, null);
                }

                Rect rectS = new Rect(0f, yShip, width, 22f);
                if(Mouse.IsOver(rectS))
                {
                    Widgets.DrawHighlight(rectS);
                    foreach(Pawn p in lord.ownedPawns)
                    {
                        if(IsShip(p))
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
                        if(IsShip(p))
                        {
                            ___tmpPawns.Add(p);
                        }
                    }
                    AccessTools.Method(type: typeof(ITab_Pawn_FormingCaravan), name: "SelectLater").Invoke(__instance, new object[] { ___tmpPawns });
                    ___tmpPawns.Clear();
                }

                Rect rect2 = new Rect(0f, y2, width, 22f);
                if (Mouse.IsOver(rect2))
                {
                    Widgets.DrawHighlight(rect2);
                    AccessTools.Method(type: typeof(ITab_Pawn_FormingCaravan), name: "HighlightPrisoners").Invoke(__instance, null);
                }
                if (Widgets.ButtonInvisible(rect2, false))
                {
                    AccessTools.Method(type: typeof(ITab_Pawn_FormingCaravan), name: "SelectPrisonersLater").Invoke(__instance, null);
                }

                Rect rect3 = new Rect(0f, y3, width, 22f);
                if (Mouse.IsOver(rect3))
                {
                    Widgets.DrawHighlight(rect3);
                    AccessTools.Method(type: typeof(ITab_Pawn_FormingCaravan), name: "HighlightAnimals").Invoke(__instance, null);
                }
                if (Widgets.ButtonInvisible(rect3, false))
                {
                    AccessTools.Method(type: typeof(ITab_Pawn_FormingCaravan), name: "SelectAnimalsLater").Invoke(__instance, null);
                }
                return false;
            }
            return true;
        }

        private static void GetEnterCellOffset(Caravan caravan, Map map, CaravanEnterMode enterMode, Predicate<IntVec3> extraCellValidator, ref IntVec3 __result)
        {
            if(caravan.PawnsListForReading.Any(x => IsShip(x)))
            {
                float offset = caravan.PawnsListForReading.FindAll(x => IsShip(x)).Max(x => x.kindDef.lifeStages.First().bodyGraphicData.drawSize.x > x.kindDef.lifeStages.First().bodyGraphicData.drawSize.y
                    ? x.kindDef.lifeStages.First().bodyGraphicData.drawSize.x : x.kindDef.lifeStages.First().bodyGraphicData.drawSize.y);
                PushCellByOffset(offset, ref __result, map);
            }
        }

        public static bool AllOwnersDownedShip(Caravan __instance, ref bool __result)
        {
            if(__instance.PawnsListForReading.Any(x => IsShip(x)))
            {
                foreach (Pawn ship in __instance.pawns)
                {
                    if(IsShip(ship) && (ship?.GetComp<CompShips>()?.AllPawnsAboard.All(x => x.Downed) ?? false))
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

        public static bool AllOwnersMentalBreakShip(Caravan __instance, ref bool __result)
        {
            if(__instance.PawnsListForReading.Any(x => IsShip(x)))
            {
                foreach(Pawn ship in __instance.pawns)
                {
                    if(IsShip(ship) && (ship?.GetComp<CompShips>()?.AllPawnsAboard.All(x => x.InMentalState) ?? false))
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

        public static bool GetInspectStringShip(Caravan __instance, ref string __result)
        {
            if(__instance.PawnsListForReading.Any(x => IsShip(x)))
            {
                StringBuilder stringBuilder = new StringBuilder();
                
                // base inspect string? 

                if (stringBuilder.Length != 0)
                    stringBuilder.AppendLine();
                int num = 0;
                int num2 = 0;
                int num3 = 0;
                int num4 = 0;
                int num5 = 0;
                int numS = 0;
                foreach(Pawn ship in __instance.PawnsListForReading.Where(x => IsShip(x)))
                {
                    numS++;
                    foreach(Pawn p in ship.GetComp<CompShips>().AllPawnsAboard)
                    {
                        if(p.IsColonist)
                            num++;
                        if(p.RaceProps.Animal)
                            num2++;
                        if(p.IsPrisoner)
                            num3++;
                        if(p.Downed)
                            num4++;
                        if(p.InMentalState)
                            num5++;
                    }
                }
                if (numS == 1)
                    stringBuilder.Append("CaravanShipsSingle".Translate());
                else if (numS > 1)
                    stringBuilder.Append("CaravanShipsPlural".Translate(numS));
                stringBuilder.Append(", " + "CaravanColonistsCount".Translate(num, (num != 1) ? Faction.OfPlayer.def.pawnsPlural : Faction.OfPlayer.def.pawnSingular));
                if (num2 == 1)
                    stringBuilder.Append(", " + "CaravanAnimal".Translate());
                else if (num2 > 1)
                    stringBuilder.Append(", " + "CaravanAnimalsCount".Translate(num2));
                if (num3 == 1)
                    stringBuilder.Append(", " + "CaravanPrisoner".Translate());
                else if (num3 > 1)
                    stringBuilder.Append(", " + "CaravanPrisonersCount".Translate(num3));
                stringBuilder.AppendLine();
                if (num5 > 0)
                    stringBuilder.Append("CaravanPawnsInMentalState".Translate(num5));
                if (num4 > 0)
                {
                    if (num5 > 0)
                    {
                        stringBuilder.Append(", ");
                    }
                    stringBuilder.Append("CaravanPawnsDowned".Translate(num4));
                }
                if (num5 > 0 || num4 > 0)
                {
                    stringBuilder.AppendLine();
                }

                if(__instance.pather.Moving)
                {
                    if (!(__instance.pather.ArrivalAction is null))
                        stringBuilder.Append(__instance.pather.ArrivalAction.ReportString);
                    else
                        stringBuilder.Append("CaravanSailing".Translate());
                }
                else
                {
                    SettlementBase settlementBase = CaravanVisitUtility.SettlementVisitedNow(__instance);
                    if (!(settlementBase is null))
                        stringBuilder.Append("CaravanVisiting".Translate(settlementBase.Label));
                    else
                        stringBuilder.Append("CaravanWaiting".Translate());
                }
                if (__instance.pather.Moving)
                {
                    float num6 = (float)CaravanArrivalTimeEstimator.EstimatedTicksToArrive(__instance, true) / 60000f;
                    stringBuilder.AppendLine();
                    stringBuilder.Append("CaravanEstimatedTimeToDestination".Translate(num6.ToString("0.#")));
                }
                if (__instance.AllOwnersDowned)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append("AllCaravanMembersDowned".Translate());
                }
                else if (__instance.AllOwnersHaveMentalBreak)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append("AllCaravanMembersMentalBreak".Translate());
                }
                else if (__instance.ImmobilizedByMass)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append("CaravanImmobilizedByMass".Translate());
                }
                if (__instance.needs.AnyPawnOutOfFood(out string text))
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append("CaravanOutOfFood".Translate());
                    if (!text.NullOrEmpty())
                    {
                        stringBuilder.Append(" ");
                        stringBuilder.Append(text);
                        stringBuilder.Append(".");
                    }
                }
                if (!__instance.pather.MovingNow)
                {
                    int usedBedCount = __instance.beds.GetUsedBedCount();
                    stringBuilder.AppendLine();
                    stringBuilder.Append(CaravanBedUtility.AppendUsingBedsLabel("CaravanResting".Translate(), usedBedCount));
                }
                else
                {
                    string inspectStringLine = __instance.carryTracker.GetInspectStringLine();
                    if (!inspectStringLine.NullOrEmpty())
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.Append(inspectStringLine);
                    }
                    string inBedForMedicalReasonsInspectStringLine = __instance.beds.GetInBedForMedicalReasonsInspectStringLine();
                    if (!inBedForMedicalReasonsInspectStringLine.NullOrEmpty())
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.Append(inBedForMedicalReasonsInspectStringLine);
                    }
                }
                __result = stringBuilder.ToString();
                return false;
            }
            return true;
        }

        private static bool GetEnterCellWater(Caravan caravan, Map map, CaravanEnterMode enterMode, Predicate<IntVec3> extraCellValidator, ref IntVec3 __result)
        {
            if(HasShip(caravan))
            {
                switch(enterMode)
                {
                    case CaravanEnterMode.Edge:
                        __result = FindNearEdgeWaterCell(map, extraCellValidator);
                        break;
                    case CaravanEnterMode.Center:
                        throw new NotImplementedException("ShipCaravanEnterMode");
                }
                __result = FindNearEdgeWaterCell(map, extraCellValidator);
                return false;
            }
            return true;
        }

        public static bool CaravanLostAllShips(Pawn member, Caravan __instance)
        {
            if(HasShip(__instance) && !__instance.PawnsListForReading.Any(x => !IsShip(x)))
            {
                if(!__instance.Spawned)
                {
                    Log.Error("Caravan member died in an unspawned caravan. Unspawned caravans shouldn't be kept for more than a single frame.", false);
                }
                if(!__instance.PawnsListForReading.Any(x => IsShip(x) && !x.Dead && x.GetComp<CompShips>().AllPawnsAboard.Any((Pawn y) => y != member && __instance.IsOwner(y))))
                {
                    __instance.RemovePawn(member);
                    if (__instance.Faction == Faction.OfPlayer)
                    {
                        Find.LetterStack.ReceiveLetter("LetterLabelAllCaravanColonistsDied".Translate(), "LetterAllCaravanColonistsDied".Translate(__instance.Name).CapitalizeFirst(), LetterDefOf.NegativeEvent, new GlobalTargetInfo(__instance.Tile), null, null);
                    }
                    __instance.pawns.Clear();
                    Find.WorldObjects.Remove(__instance);
                }
                else
                {
                    member.Strip();
                    __instance.RemovePawn(member);
                }
                return false;
            }
            return true;
        }

        public static void AnyShipBlockingMapRemoval(MapPawns __instance, ref bool __result)
        {
            if(__result is false)
            {
                foreach(Pawn p in __instance.AllPawnsSpawned)
                {
                    if(IsShip(p) && p.GetComp<CompShips>().AllPawnsAboard.Any())
                    {
                        foreach (Pawn sailor in p.GetComp<CompShips>().AllPawnsAboard)
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

        public static bool NoRestForBoats(Caravan __instance, ref bool __result)
        {
            if(HasShip(__instance) && !__instance.PawnsListForReading.Any(x => !IsShip(x)))
            {
                __result = false;
                if(__instance.PawnsListForReading.Any(x => x.GetComp<CompShips>().Props.shipPowerType == ShipType.Paddles))
                {
                    __result = __instance.Spawned && (!__instance.pather.Moving || __instance.pather.nextTile != __instance.pather.Destination || !Caravan_PathFollower.IsValidFinalPushDestination(__instance.pather.Destination) ||
                        Mathf.CeilToInt(__instance.pather.nextTileCostLeft / 1f) > 10000) && CaravanNightRestUtility.RestingNowAt(__instance.Tile);
                }
                return false;
            }
            return true;
        }

        #endregion Caravan

        #region Construction
        public static bool CompleteConstructionShip(Pawn worker, Frame __instance)
        {
            if (__instance.def.entityDefToBuild?.GetModExtension<SpawnThingBuilt>()?.thingToSpawn != null)
            {
                Pawn ship = PawnGenerator.GeneratePawn(__instance.def.entityDefToBuild.GetModExtension<SpawnThingBuilt>().thingToSpawn);
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

        #region Extra
        
        public static void FreeColonistsInShips(ref int __result, List<Pawn> ___pawnsSpawned)
        {
            List<Pawn> ships = ___pawnsSpawned.Where(x => IsShip(x)).ToList();
            
            foreach(Pawn ship in ships)
            {
                if(ship.GetComp<CompShips>().AllPawnsAboard.Any(x => !x.Dead))
                    __result += ship.GetComp<CompShips>().AllPawnsAboard.Count;
            }
        }

        #endregion Extra
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
                if(!___pawn.Drafted)
                {
                    if(___pawn.CurJob is null)
                    {
                        ShipHarmony.ShipErrorRecoverJob(___pawn, "");
                    }
                    __instance?.StopDead();
                }
                else if(ShipHarmony.ShipEdgeOfMap(___pawn, __instance.nextCell, ___pawn.Map))
                {
                    ___pawn.jobs.curDriver.Notify_PatherFailed();
                    __instance.StopDead();
                }
                if (___pawn.GetComp<CompShips>().beached || !__instance.nextCell.GetTerrain(___pawn.Map).IsWater)
                {
                    ___pawn.GetComp<CompShips>().BeachShip();
                    ___pawn.Position = __instance.nextCell;
                    __instance.StopDead();
                    ___pawn.jobs.curDriver.Notify_PatherFailed();
                }

                //Buildings?
                ___lastCell = ___pawn.Position;
                ___pawn.Position = __instance.nextCell;
                //Clamor?
                //More Buildings?
                
                if(NeedNewPath(___destination, __instance.curPath, ___pawn, ___peMode, __instance.lastPathedTargetPosition) && !TrySetNewPath(ref __instance, ref __instance.lastPathedTargetPosition, 
                    ___destination, ___pawn, ___pawn.Map, ref ___peMode))
                {
                    return false;
                }
                if(ShipReachabilityImmediate.CanReachImmediateShip(___pawn, ___destination, ___peMode))
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

        public static bool GotoLocationShips(IntVec3 clickCell, Pawn pawn, ref FloatMenuOption __result)
        {
            if (disabled) return true;
            if(IsShip(pawn))
            {
                if (debug)
                {
                    Log.Message("-> " + clickCell + " | " + pawn.Map.terrainGrid.TerrainAt(clickCell).LabelCap + " | " + pawn.Map.terrainGrid.TerrainAt(clickCell).pathCost);
                }
                
                int num = GenRadial.NumCellsInRadius(2.9f);
                int i = 0;
                IntVec3 curLoc;
                while(i < num)
                {
                    curLoc = GenRadial.RadialPattern[i] + clickCell;
                    if (GenGridShips.Standable(curLoc, pawn.Map, MapExtensionUtility.GetExtensionToMap(pawn.Map)))
                    {
                        if (curLoc == pawn.Position || pawn.GetComp<CompShips>().beached)
                        {
                            __result = null;
                            return false;
                        }
                        if(!ShipReachabilityUtility.CanReachShip(pawn, curLoc, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                        {
                            if(debug) Log.Message("CANT REACH ");
                            __result = new FloatMenuOption("CannotSailToCell".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null);
                            return false;
                        }
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

        public static void DeregisterWaterRegion(Region reg)
        {
            //DO MORE HERE
        }

        public static IEnumerable<CodeInstruction> FindPathWithShipTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for(int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if(instruction.opcode == OpCodes.Callvirt && instruction.operand == AccessTools.Method(type: typeof(World), name: nameof(World.Impassable)))
                {
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_3);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(ShipHarmony), name: nameof(ShipHarmony.ImpassableModified)));
                    instruction = instructionList[++i];
                }
                if(instruction.opcode == OpCodes.Callvirt && instruction.operand == AccessTools.Method(type: typeof(WorldGrid), name: nameof(WorldGrid.GetRoadMovementDifficultyMultiplier)))
                {
                    Label label = ilg.DefineLabel();
                    yield return instruction;
                    instruction = instructionList[++i];
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_3);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(ShipHarmony), parameters: new Type[] { typeof(Caravan) }, name: nameof(ShipHarmony.HasShip)));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

                    yield return new CodeInstruction(opcode: OpCodes.Pop);
                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_3);

                    instruction.labels.Add(label);
                }
                if(instruction.opcode == OpCodes.Stloc_S && ((LocalBuilder)instruction.operand).LocalIndex == 20)
                {
                    yield return instruction;
                    instruction = instructionList[++i];
                    Label label2 = ilg.DefineLabel();

                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_3);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(ShipHarmony), parameters: new Type[] { typeof(Caravan) }, name: nameof(ShipHarmony.HasShip)));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, label2);

                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, operand: 14);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(ShipHarmony), name: nameof(ShipHarmony.IsWaterTile)));
                    yield return new CodeInstruction(opcode: OpCodes.Brtrue, label2);
                    
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, operand: 20);
                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_3);
                    yield return new CodeInstruction(opcode: OpCodes.Mul);
                    yield return new CodeInstruction(opcode: OpCodes.Stloc_S, operand: 20);

                    instruction.labels.Add(label2);
                }

                yield return instruction;
            }
        }

        public static bool TryAddWayPointWater(int tile, Dialog_FormCaravan ___currentFormCaravanDialog, bool playSound = true)
        {
            List<Pawn> pawnsOnCaravan = TransferableUtility.GetPawnsFromTransferables(___currentFormCaravanDialog.transferables);
            if(Find.WorldGrid[tile].biome == BiomeDefOf.Ocean || Find.WorldGrid[tile].biome == BiomeDefOf.Lake || (Find.World.CoastDirectionAt(tile).IsValid && HasShip(pawnsOnCaravan)))
            {
                if(!HasShip(pawnsOnCaravan))
                {
                    Messages.Message("MessageCantAddWaypointBecauseNoShip".Translate(Find.WorldGrid[tile].biome.defName), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
            }
            else
            {
                if (HasShip(pawnsOnCaravan))
                {
                    Messages.Message("MessageCantAddWaypointBecauseShip".Translate(), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
            }
            return true;
        }

        public static void Dialog_SetInstance(Rect inRect, Dialog_FormCaravan __instance)
        {
            currentFormingCaravan = __instance;
        }

        public static bool CanReachWithShip(Caravan c, int tile, int[] ___fields, int ___impassableFieldID, WorldReachability __instance, ref bool __result)
        {
            if(HasShip(c))
            {
                int start = c.Tile;
                if (start < 0 || start >= ___fields.Length || tile < 0 || tile >= ___fields.Length)
                {
                    __result = false;
                    return false;
                }
                if(!IsWaterTile(start) || !IsWaterTile(tile))
                {
                    __result = false;
                    return false;
                }
                if(___fields[start] == ___impassableFieldID || ___fields[tile] == ___impassableFieldID)
                {
                    __result = false;
                    return false;
                }
                MethodInfo validField = AccessTools.Method(type: typeof(WorldReachability), name: "IsValidField");
                if((bool)validField.Invoke(__instance, new object[] { start }) || (bool)validField.Invoke(__instance, new object[] { tile }))
                {
                    __result = ___fields[start] == ___fields[tile];
                    return false;
                }
                AccessTools.Method(type: typeof(WorldReachability), name: "FloodFillAt").Invoke(__instance, new object[] { start });
                __result = ___fields[start] != ___impassableFieldID && ___fields[start] != ___fields[tile];
                return false;
            }
            return true;
        }

        public static bool GetTicksPerMoveShips(List<Pawn> pawns, float massUsage, float massCapacity, ref int __result, StringBuilder explanation = null)
        {
            if(pawns.Any<Pawn>() && HasShip(pawns))
            {
                //Caravan Const Values
                const int MaxShipPawnTicksPerMove = 150;
                const float CellToTilesConversionRatio = 340f;
                const int DefaultTicksPerMove = 3300;
                const float MoveSpeedFactorAtLowMass = 2f;

                if(!(explanation is null))
                {
                    explanation.Append("CaravanMovementSpeedFull".Translate() + ":");
                    float num = 0f;
                    for(int i = 0; i  < pawns.Count; i++)
                    {
                        num = Mathf.Min((float)pawns[i].TicksPerMoveCardinal, MaxShipPawnTicksPerMove) * CellToTilesConversionRatio;
                        float num2 = 60000f / num;
                        if(!(explanation is null))
                        {
                            explanation.AppendLine();
                            explanation.Append(string.Concat(new string[]
                            {
                            "  - ",
                            pawns[i].LabelShortCap,
                            ": ",
                            num2.ToString("0.#"),
                            " ",
                            "TilesPerDay".Translate()
                            }));
                        }
                        int count = 0;
                        foreach(Pawn p in pawns)
                            count += p?.GetComp<CompShips>()?.AllPawnsAboard?.Count ?? 1;
                        num += num2 / (float)count;
                    }
                    float moveSpeedFactorFromMass = massCapacity <= 0f ? 1f : Mathf.Lerp(MoveSpeedFactorAtLowMass, 1f, massUsage / massCapacity);
                    if(!(explanation is null))
                    {
                        float num3 = 60000f / num;
                        explanation.AppendLine();
                        explanation.Append(string.Concat(new string[]
                        {
                            "  ",
                            "Average".Translate(),
                            ": ",
                            num3.ToString("0.#"),
                            " ",
                            "TilesPerDay".Translate()
                        }));
                        explanation.AppendLine();
                        explanation.Append("  " + "MultiplierForCarriedMass".Translate(moveSpeedFactorFromMass.ToStringPercent()));
                    }
                    int num4 = Mathf.Max(Mathf.RoundToInt(num / moveSpeedFactorFromMass), 1);
                    if(!(explanation is null))
                    {
                        float num5 = 60000f / (float)num4;
                        explanation.AppendLine();
                        explanation.Append(string.Concat(new string[]
                        {
                            "  ",
                            "FinalCaravanPawnsMovementSpeed".Translate(),
                            ": ",
                            num5.ToString("0.#"),
                            " ",
                            "TilesPerDay".Translate()
                        }));
                    }
                    __result = num4;
                    return false;
                }
                if(!(explanation is null))
                {
                    explanation.Append("CaravanMovementSpeedFull".Translate() + ":");
                    float num = 18.181818f;
                    explanation.AppendLine();
                    explanation.Append(string.Concat(new string[]
                    {
                        "  ",
                        "Default".Translate(),
                        ": ",
                        num.ToString("0.#"),
                        " ",
                        "TilesPerDay".Translate()
                    }));
                }
                __result = DefaultTicksPerMove;
                return false;
            }
            return true;
        }

        #region HelperFunctions

        private static bool ShipEdgeOfMap(Pawn p, IntVec3 nextCell, Map map)
        {
            int x = p.def.size.x % 2 == 0 ? p.def.size.x / 2 : (p.def.size.x + 1) / 2;
            int z = p.def.size.z % 2 == 0 ? p.def.size.z / 2 : (p.def.size.z + 1) / 2;

            int hitbox = x > z ? x : z;
            if(nextCell.x + hitbox >= map.Size.x || nextCell.z + hitbox >= map.Size.z)
            {
                return true;
            }
            if(nextCell.x - hitbox <= 0 || nextCell.z - hitbox <= 0)
            {
                return true;
            }
            return false;
        }

        private static bool IsWaterTile(int tile)
        {
            return Find.WorldGrid[tile].WaterCovered || Find.World.CoastDirectionAt(tile).IsValid;
        }

        private static IntVec3 FindNearEdgeWaterCell(Map map, Predicate<IntVec3> extraCellValidator)
        {
            MapExtension mapE = MapExtensionUtility.GetExtensionToMap(map);
            bool baseValidator(IntVec3 x) => GenGridShips.Standable(x, map, mapE) && !x.Fogged(map);
            Faction hostFaction = map.ParentFaction;
            IntVec3 root;
            if(CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => baseValidator(x) && (extraCellValidator is null || extraCellValidator(x)) && ((hostFaction != null && mapE.getShipReachability.CanReachFactionBase(x, hostFaction)) ||
            (hostFaction is null && mapE.getShipReachability.CanReachBiggestMapEdgeRoom(x))), map, CellFinder.EdgeRoadChance_Ignore, out root))
            {
                return root;
            }
            Log.Warning("Could not find any valid edge cell.  Smash Phil hasn't implemented all 3 checks yet.");
            return CellFinder.RandomCell(map);
        }

        private static bool CanSetSail(List<Pawn> caravan)
        {
            int seats = 0;
            int pawns = 0;
            int prereq = 0;
            bool flag = caravan.Any(x => !(x.GetComp<CompShips>() is null)); //Ships or No Ships
            if(flag)
            {
                foreach(Pawn p in caravan)
                {
                    if(IsShip(p))
                    {
                        seats += p.GetComp<CompShips>().SeatsAvailable;
                        prereq += p.GetComp<CompShips>().PawnCountToOperate;
                    }
                    else if(p.IsColonistPlayerControlled && !p.Downed && !p.Dead)
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

        private static void ToggleDocking(Caravan caravan, bool dock = false)
        {
            if(HasShip(caravan))
            {
                if(!dock)
                {
                    List<Pawn> pawns = caravan.PawnsListForReading.Where(x => !IsShip(x)).ToList();
                    List<Pawn> ships = caravan.PawnsListForReading.Where(x => IsShip(x)).ToList();
                    for(int i = 0; i < ships.Count; i++)
                    {
                        Pawn ship = ships[i];
                        for(int j = 0; j < ship?.GetComp<CompShips>()?.PawnCountToOperate; j++)
                        {
                            if(pawns.Count <= 0)
                            {
                                return;
                            }
                            foreach(ShipHandler handler in ship.GetComp<CompShips>().handlers)
                            {
                                if(handler.AreSlotsAvailable)
                                {
                                    ship.GetComp<CompShips>().Notify_BoardedCaravan(pawns.Pop(), handler.handlers);
                                    break;
                                }
                            }
                        }
                    }
                    if(pawns.Count > 0)
                    {
                        int x = 0;
                        while(pawns.Count > 0)
                        {
                            Pawn ship = ships[x];
                            foreach(ShipHandler handler in ship.GetComp<CompShips>().handlers)
                            {
                                if(handler.AreSlotsAvailable)
                                {
                                    ship.GetComp<CompShips>().Notify_BoardedCaravan(pawns.Pop(), handler.handlers);
                                    break;
                                }
                            }
                            x = (x + 2) > ships.Count ? 0 : ++x;
                        }
                    }
                }
                else
                {
                    List<Pawn> ships = caravan.PawnsListForReading.Where(x => IsShip(x)).ToList();
                    for(int i = 0; i < ships.Count; i++)
                    {
                        Pawn ship = ships[i];
                        ship?.GetComp<CompShips>()?.DisembarkAll();
                    }
                }
            }
        }

        public static bool IsShip(Pawn p)
        {
            return !(p?.TryGetComp<CompShips>() is null) ? true : false;
        }

        public static bool HasShip(List<Pawn> pawns)
        {
            return pawns?.Any(x => IsShip(x)) ?? false;
        }

        public static bool HasShip(Caravan c)
        {
            return (c is null) ? (currentFormingCaravan is null) ? false : HasShip(TransferableUtility.GetPawnsFromTransferables(currentFormingCaravan.transferables)) : HasShip(c?.PawnsListForReading);
        }

        public static bool ImpassableModified(World world, int tileID, Caravan caravan)
        {
            bool flag = Find.WorldGrid[tileID].biome == BiomeDefOf.Ocean || Find.WorldGrid[tileID].biome == BiomeDefOf.Lake;
            return HasShip(caravan) ? (!Find.WorldGrid[tileID].WaterCovered && !Find.World.CoastDirectionAt(tileID).IsValid) : (flag || world.Impassable(tileID));
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

        private static void PushCellByOffset(float offset, ref IntVec3 result, Map map)
        {
            if (result.x < offset)
            {
                result.x = (int)(offset/2);
            }
            else if (result.x >= (map.Size.x - (offset / 2)))
            {
                result.x = (int)(map.Size.x - (offset / 2));
            }
            if (result.z < offset)
            {
                result.z = (int)(offset / 2);
            }
            else if (result.z > (map.Size.z - (offset / 2)))
            {
                result.z = (int)(map.Size.z - (offset / 2));
            }
        }

        private static int CostToMoveIntoCellShips(Pawn pawn, IntVec3 c)
        {
            int num = (c.x == pawn.Position.x || c.z == pawn.Position.z) ? pawn.TicksPerMoveCardinal : pawn.TicksPerMoveDiagonal;
            num += MapExtensionUtility.GetExtensionToMap(pawn.Map)?.getShipPathGrid?.CalculatedCostAt(c, false, pawn.Position) ?? 200;
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

        public static List<Pawn> ExtractPawnsFromCaravan(Caravan caravan)
        {
            List<Pawn> sailors = new List<Pawn>();

            foreach(Pawn ship in caravan.PawnsListForReading)
            {
                if(IsShip(ship))
                {
                    sailors.AddRange(ship.GetComp<CompShips>().AllPawnsAboard);
                }
            }
            return sailors;
        }

        public static float CapacityLeft(LordJob_FormAndSendCaravanShip lordJob)
        {
            float num = CollectionsMassCalculator.MassUsageTransferables(lordJob.transferables, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, false, false);
            List<ThingCount> tmpCaravanPawns = new List<ThingCount>();
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

        public static float ShipAngle(Pawn pawn)
        {
            if (pawn is null) return 0f;
            if (pawn.pather.Moving)
            {
                IntVec3 c = pawn.pather.nextCell - pawn.Position;
                if (c.x > 0 && c.z > 0)
                {
                    pawn.GetComp<CompShips>().Angle = -45f;
                }
                else if (c.x > 0 && c.z < 0)
                {
                    pawn.GetComp<CompShips>().Angle = 45f;
                }
                else if (c.x < 0 && c.z < 0)
                {
                    pawn.GetComp<CompShips>().Angle = -45f;
                }
                else if (c.x < 0 && c.z > 0)
                {
                    pawn.GetComp<CompShips>().Angle = 45f;
                }
                else
                {
                    pawn.GetComp<CompShips>().Angle = 0f;
                }
            }
            return pawn.GetComp<CompShips>().Angle;
        }

        public static bool NeedNewPath(LocalTargetInfo destination, PawnPath curPath, Pawn pawn, PathEndMode peMode, IntVec3 lastPathedTargetPosition)
        {
            if(!destination.IsValid || curPath is null || !curPath.Found || curPath.NodesLeftCount == 0)
                return true;
            if (destination.HasThing && destination.Thing.Map != pawn.Map)
                return true;
            if ((pawn.Position.InHorDistOf(curPath.LastNode, 15f) || pawn.Position.InHorDistOf(destination.Cell, 15f)) && !ShipReachabilityImmediate.CanReachImmediateShip(
                curPath.LastNode, destination, pawn.Map, peMode, pawn))
                return true;
            if (curPath.UsedRegionHeuristics && curPath.NodesConsumedCount >= 75)
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

                if ((float)(lastPathedTargetPosition - destination.Cell).LengthHorizontalSquared > (num2 * num2))
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
                if (num3 != 0 && intVec.AdjacentToDiagonal(other) && (ShipPathFinder.BlocksDiagonalMovement(pawn.Map.cellIndices.CellToIndex(intVec.x, other.z), pawn.Map,
                    MapExtensionUtility.GetExtensionToMap(pawn.Map)) || ShipPathFinder.BlocksDiagonalMovement(pawn.Map.cellIndices.CellToIndex(other.x, intVec.z), pawn.Map,
                    MapExtensionUtility.GetExtensionToMap(pawn.Map))) )
                    return true;
                other = intVec;
                num3++;
            }
            return false;
        }

        private static bool TrySetNewPath(ref Pawn_PathFollower instance, ref IntVec3 lastPathedTargetPosition, LocalTargetInfo destination, Pawn pawn, Map map, ref PathEndMode peMode)
        {
            PawnPath pawnPath = ShipHarmony.GenerateNewPath(ref lastPathedTargetPosition, destination, ref pawn, map, peMode);
            if(!pawnPath.Found)
            {
                PatherFailedHelper(ref instance, pawn);
                return false;
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

                if(pawn.GetComp<CompShips>().beached) break;
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
            return MapExtensionUtility.GetExtensionToMap(map)?.getShipPathFinder?.FindShipPath(pawn.Position, destination, pawn, peMode) ?? PawnPath.NotFound;
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
            float beach = 60f; //Rand.Range(40f, 80f);
            return (float)(beach + (beach * (RimShipMod.mod.settings.beachMultiplier) / 100f));
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

        private static void DoItemsListForShip(Rect inRect, ref float curY, ref List<Thing> tmpSingleThing, ITab_Pawn_FormingCaravan instance)
        {
            LordJob_FormAndSendCaravanShip lordJob_FormAndSendCaravanShip = (LordJob_FormAndSendCaravanShip)(Find.Selector.SingleSelectedThing as Pawn).GetLord().LordJob;
            Rect position = new Rect(0f, curY, (inRect.width - 10f) / 2f, inRect.height);
            float a = 0f;
            GUI.BeginGroup(position);
            Widgets.ListSeparator(ref a, position.width, "ItemsToLoad".Translate());
            bool flag = false;
            foreach(TransferableOneWay transferableOneWay in lordJob_FormAndSendCaravanShip.transferables)
            {
                if(transferableOneWay.CountToTransfer > 0 && transferableOneWay.HasAnyThing)
                {
                    flag = true;
                    MethodInfo doThingRow = AccessTools.Method(type: typeof(ITab_Pawn_FormingCaravan), name: "DoThingRow");
                    object[] args = new object[] { transferableOneWay.ThingDef, transferableOneWay.CountToTransfer, transferableOneWay.things, position.width, a };
                    doThingRow.Invoke(instance, args);
                    a = (float)args[4];
                }
            }
            if(!flag)
            {
                Widgets.NoneLabel(ref a, position.width, null);
            }
            GUI.EndGroup();
            Rect position2 = new Rect((inRect.width + 10f) / 2f, curY, (inRect.width - 10f) / 2f, inRect.height);
            float b = 0f;
            GUI.BeginGroup(position2);
            Widgets.ListSeparator(ref b, position2.width, "LoadedItems".Translate());
            bool flag2 = false;
            foreach(Pawn pawn in lordJob_FormAndSendCaravanShip.lord.ownedPawns)
            {
                if(!pawn.inventory.UnloadEverything)
                {
                    foreach (Thing thing in pawn.inventory.innerContainer)
                    {
                        flag2 = true;
                        tmpSingleThing.Clear();
                        tmpSingleThing.Add(thing);
                        MethodInfo doThingRow = AccessTools.Method(type: typeof(ITab_Pawn_FormingCaravan), name: "DoThingRow");
                        object[] args = new object[] { thing.def, thing.stackCount, tmpSingleThing, position2.width, b };
                        doThingRow.Invoke(instance, args);
                        b = (float)args[4];
                    }
                }
            }
            if(!flag2)
            {
                Widgets.NoneLabel(ref b, position.width, null);
            }
            GUI.EndGroup();
            curY += Mathf.Max(a, b);
        }

        private static Dialog_FormCaravan currentFormingCaravan;

        #endregion HelperFunctions
        
        private static readonly bool disabled = false;
        public static readonly bool debug = false;
        public static readonly bool drawPaths = false;
        private static List<WorldPath> debugLines = new List<WorldPath>();
        private static List<Pair<int, int>> tiles = new List<Pair<int,int>>(); // Pair -> TileID : Cycle
    }
}