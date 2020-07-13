using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using Vehicles.AI;
using Vehicles.Defs;
using Vehicles.Lords;
using Vehicles.UI;
using SPExtended;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace Vehicles
{
    [StaticConstructorOnStartup]
    internal static class ShipHarmony
    {
        static ShipHarmony()
        {
            var harmony = new Harmony("rimworld.boats.smashphil");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            //Harmony.DEBUG = true;
            
            #region Functions

            /* Map Gen */
            harmony.Patch(original: AccessTools.Method(typeof(BeachMaker), nameof(BeachMaker.Init)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(BeachMakerTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(TileFinder), nameof(TileFinder.RandomSettlementTileFor)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(PushSettlementToCoastTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(PathGrid), nameof(PathGrid.RecalculatePerceivedPathCostUnderThing)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(RecalculateShipPathCostUnderThing)));

            /* Health */
            harmony.Patch(original: AccessTools.Method(typeof(HealthUtility), nameof(HealthUtility.GetGeneralConditionLabel)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(ReplaceConditionLabel)));
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_HealthTracker), "ShouldBeDowned"),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehicleShouldBeDowned)));
            harmony.Patch(original: AccessTools.Method(typeof(PawnDownedWiggler), nameof(PawnDownedWiggler.WigglerTick)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehicleShouldWiggle)));
            harmony.Patch(original: AccessTools.Method(typeof(HediffUtility), nameof(HediffUtility.CanHealNaturally)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehiclesDontHeal)));
            harmony.Patch(original: AccessTools.Method(typeof(HediffUtility), nameof(HediffUtility.CanHealFromTending)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehiclesDontHealTended)));
            harmony.Patch(original: AccessTools.Method(typeof(Widgets), nameof(Widgets.InfoCardButton), new Type[] { typeof(float), typeof(float), typeof(Thing) }), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(InfoCardVehiclesTranspiler))); //REDO - change to remove info card button rather than patching the widget

            /* Rendering */
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_RotationTracker), nameof(Pawn_RotationTracker.UpdateRotation)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(UpdateVehicleRotation)));
            harmony.Patch(original: AccessTools.Method(typeof(PawnRenderer), "RenderPawnInternal", new Type[] { typeof(Vector3), typeof(float), typeof(bool), typeof(Rot4), typeof(Rot4),
                    typeof(RotDrawMode), typeof(bool), typeof(bool), typeof(bool)}), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(RenderPawnRotationTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(PawnGraphicSet), nameof(PawnGraphicSet.MatsBodyBaseAt)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(MatsBodyOfVehicles)));
            harmony.Patch(original: AccessTools.Method(typeof(ColonistBar), "CheckRecacheEntries"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(CheckRecacheEntriesTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(ColonistBarColonistDrawer), "DrawIcons"), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(DrawIconsVehicles)));
            harmony.Patch(original: AccessTools.Method(typeof(SelectionDrawer), "DrawSelectionBracketFor"),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(DrawSelectionBracketsVehicles)));
            harmony.Patch(original: AccessTools.Method(typeof(PawnFootprintMaker), nameof(PawnFootprintMaker.FootprintMakerTick)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(BoatWakesTicker)));

            //Change targeter to transpiler on UIRoot_Play
            harmony.Patch(original: AccessTools.Method(typeof(Targeter), nameof(Targeter.TargeterOnGUI)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(DrawCannonTargeter)));
            harmony.Patch(original: AccessTools.Method(typeof(Targeter), nameof(Targeter.ProcessInputEvents)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(ProcessCannonInputEvents)));
            harmony.Patch(original: AccessTools.Method(typeof(Targeter), nameof(Targeter.TargeterUpdate)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(CannonTargeterUpdate)));
            harmony.Patch(original: AccessTools.Method(typeof(PawnGraphicSet), nameof(PawnGraphicSet.MatsBodyBaseAt)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(RegisterDiagonalMovement)));
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_DrawTracker), nameof(Pawn_DrawTracker.Notify_DamageApplied)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehiclesDamageTakenWiggler)));
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_DrawTracker), nameof(Pawn_DrawTracker.Notify_DamageDeflected)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehiclesDamageDeflectedWiggler)));
            
            /* Gizmos */
            harmony.Patch(original: AccessTools.Method(typeof(Settlement), nameof(Settlement.GetCaravanGizmos)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(NoAttackSettlementWhenDocked)));
            harmony.Patch(original: AccessTools.Method(typeof(Settlement), nameof(Settlement.GetGizmos)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(AddVehicleCaravanGizmoPassthrough)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.GetGizmos)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(GizmosForVehicleCaravans)));

            /* Pathing */
            harmony.Patch(original: AccessTools.Method(typeof(FloatMenuMakerMap), "GotoLocationOption"),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(GotoLocationShips)));
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.StartPath)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(StartVehiclePath)));
            harmony.Patch(original: AccessTools.Method(typeof(PawnTweener), "TweenedPosRoot"), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehicleTweenedPosRoot)));
            harmony.Patch(original: AccessTools.Method(typeof(PawnDestinationReservationManager), nameof(PawnDestinationReservationManager.Reserve)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(ReserveEntireVehicle)));

            /* Upgrade Stats */
            harmony.Patch(original: AccessTools.Method(typeof(Pawn), "TicksPerMove"),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehicleMoveSpeedUpgradeModifier)));
            harmony.Patch(original: AccessTools.Method(typeof(MassUtility), nameof(MassUtility.Capacity)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehicleCargoCapacity)));

            /* World Pathing */
            harmony.Patch(original: AccessTools.Method(typeof(WorldSelector), "AutoOrderToTileNow"),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(AutoOrderVehicleCaravanPathing)));
            harmony.Patch(original: AccessTools.Method(typeof(TilesPerDayCalculator), nameof(TilesPerDayCalculator.ApproxTilesPerDay), new Type[] { typeof(Caravan), typeof(StringBuilder) }),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(ApproxTilesForShips)));
            harmony.Patch(original: AccessTools.Method(typeof(WorldRoutePlanner), nameof(WorldRoutePlanner.WorldRoutePlannerUpdate)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehicleRoutePlannerUpdateHook)));
            harmony.Patch(original: AccessTools.Method(typeof(WorldRoutePlanner), nameof(WorldRoutePlanner.WorldRoutePlannerOnGUI)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehicleRoutePlannerOnGUIHook)));
            harmony.Patch(original: AccessTools.Method(typeof(WorldRoutePlanner), nameof(WorldRoutePlanner.DoRoutePlannerButton)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehicleRoutePlannerButton)));

            /* World Handling */
            harmony.Patch(original: AccessTools.Method(typeof(WorldPawns), nameof(WorldPawns.GetSituation)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(SituationBoardedVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(WorldPawns), nameof(WorldPawns.RemoveAndDiscardPawnViaGC)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(DoNotRemoveDockedBoats)));

            /* Jobs */
            harmony.Patch(original: AccessTools.Method(typeof(JobUtility), nameof(JobUtility.TryStartErrorRecoverJob)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehicleErrorRecoverJob)));
            harmony.Patch(original: AccessTools.Method(typeof(JobGiver_Wander), "TryGiveJob"),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehiclesDontWander)));
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.CheckForJobOverride)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(NoOverrideDamageTakenTranspiler)));
            harmony.Patch(original: AccessTools.Property(typeof(JobDriver_PrepareCaravan_GatherItems), "Transferables").GetGetMethod(nonPublic: true),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(TransferablesVehicle)));

            /* Lords */
            harmony.Patch(original: AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherAnimals), nameof(LordToil_PrepareCaravan_GatherAnimals.UpdateAllDuties)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(UpdateDutyOfVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(GatheringsUtility), nameof(GatheringsUtility.ShouldGuestKeepAttendingGathering)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehiclesDontParty)));

            /* Forming Caravan */
            //CaravanFormingUtility.GetFormAndSendCaravanLord
            //CaravanFormingUtility.FormAndCreateCaravan
            //CaravanFormingUtility.RemovePawnFromCaravan
            harmony.Patch(original: AccessTools.Method(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.IsFormingCaravan)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(IsFormingCaravanVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(TransferableUtility), nameof(TransferableUtility.CanStack)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(CanStackVehicleTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(GiveToPackAnimalUtility), nameof(GiveToPackAnimalUtility.UsablePackAnimalWithTheMostFreeSpace)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(UsableVehicleWithMostFreeSpace)));
            harmony.Patch(original: AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(AddHumanLikeOrdersLoadVehiclesTranspiler)));

            /* Caravan */
            harmony.Patch(original: AccessTools.Method(typeof(CollectionsMassCalculator), nameof(CollectionsMassCalculator.Capacity), new Type[] { typeof(List<ThingCount>), typeof(StringBuilder) }),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(CapacityWithVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(MassUtility), nameof(MassUtility.CanEverCarryAnything)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(CanCarryIfVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "FillTab"),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(FillTabVehicleCaravan)));
            harmony.Patch(original: AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "DoPeopleAndAnimals"), 
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(DoPeopleAnimalsAndVehicle)));
            //harmony.Patch(original: AccessTools.Method(typeof(CaravanArrivalAction_Enter), nameof(CaravanArrivalAction_Enter.Arrived)), prefix: null, postfix: null,
            //    transpiler: new HarmonyMethod(typeof(ShipHarmony),
            //    nameof(VehiclesArrivedTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanArrivalAction_VisitEscapeShip), "DoArrivalAction"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(ShipsVisitEscapeShipTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(SettlementUtility), "AttackNow"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(AttackNowWithShipsTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(SettleInEmptyTileUtility), nameof(SettleInEmptyTileUtility.Settle)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(SettleFromSeaTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanArrivalAction_VisitSite), "DoEnter"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(DoEnterWithShipsTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanEnterMapUtility), nameof(CaravanEnterMapUtility.Enter), new Type[]{typeof(Caravan), typeof(Map), typeof(CaravanEnterMode), typeof(CaravanDropInventoryMode), typeof(bool), typeof(Predicate<IntVec3>) }),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(EnterMapShipsCatchAll1)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanEnterMapUtility), nameof(CaravanEnterMapUtility.Enter), new Type[] { typeof(Caravan), typeof(Map), typeof(Func<Pawn, IntVec3>), typeof(CaravanDropInventoryMode), typeof(bool) }),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(EnterMapShipsCatchAll2)));
            harmony.Patch(original: AccessTools.Property(typeof(Caravan), nameof(Caravan.AllOwnersDowned)).GetGetMethod(),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(AllOwnersDownedVehicle)));
            harmony.Patch(original: AccessTools.Property(typeof(Caravan), nameof(Caravan.AllOwnersHaveMentalBreak)).GetGetMethod(),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(AllOwnersMentalBreakVehicle)));
            harmony.Patch(original: AccessTools.Property(typeof(Caravan), nameof(Caravan.NightResting)).GetGetMethod(),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(NoRestForVehicles)));
            harmony.Patch(original: AccessTools.Method(typeof(Caravan), nameof(Caravan.ContainsPawn)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(ContainsPawnInVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(Caravan), nameof(Caravan.IsOwner)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(IsOwnerOfVehicle)));
            harmony.Patch(original: AccessTools.Property(typeof(MapPawns), nameof(MapPawns.AnyPawnBlockingMapRemoval)).GetGetMethod(), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(AnyVehicleBlockingMapRemoval)));
            harmony.Patch(original: AccessTools.Method(typeof(SettlementDefeatUtility), nameof(SettlementDefeatUtility.CheckDefeated)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(CheckDefeatedWithVehiclesTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(Tale_DoublePawn), nameof(Tale_DoublePawn.Concerns)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(ConcernNullThing)));
            harmony.Patch(original: AccessTools.Method(typeof(WITab_Caravan_Needs), "FillTab"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehicleNeedsFillTabTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(WITab_Caravan_Needs), "UpdateSize"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehicleNeedsUpdateSizeTranspiler)));
            harmony.Patch(original: AccessTools.Property(typeof(WITab_Caravan_Gear), "Pawns").GetGetMethod(nonPublic: true), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehicleGearTabPawns)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanInventoryUtility), nameof(CaravanInventoryUtility.AllInventoryItems)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehicleAllInventoryItems)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanInventoryUtility), nameof(CaravanInventoryUtility.GiveThing)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehicleGiveThingInventoryTranspiler)));
            harmony.Patch(original: AccessTools.Property(typeof(WITab_Caravan_Health), "Pawns").GetGetMethod(nonPublic: true), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehicleHealthTabPawns)));
            harmony.Patch(original: AccessTools.Property(typeof(WITab_Caravan_Social), "Pawns").GetGetMethod(nonPublic: true), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehicleSocialTabPawns)));
            harmony.Patch(original: AccessTools.Method(typeof(BestCaravanPawnUtility), nameof(BestCaravanPawnUtility.FindPawnWithBestStat)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(FindVehicleWithBestStat)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanArrivalAction_OfferGifts), nameof(CaravanArrivalAction_OfferGifts.Arrived)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(UnloadVehicleOfferGifts)));
            harmony.Patch(original: AccessTools.Method(typeof(Settlement_TraderTracker), nameof(Settlement_TraderTracker.GiveSoldThingToPlayer)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(GiveSoldThingToVehicleTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(Caravan_NeedsTracker), "TrySatisfyPawnNeeds"),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(TrySatisfyVehiclePawnsNeeds)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanUtility), nameof(CaravanUtility.IsCaravanMember)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(IsParentCaravanMember)));

            /* Draftable */
            harmony.Patch(original: AccessTools.Property(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted)).GetSetMethod(),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(DraftedVehiclesCanMove)));
            harmony.Patch(original: AccessTools.Method(typeof(FloatMenuMakerMap), "CanTakeOrder"), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(CanVehicleTakeOrder)));
            harmony.Patch(original: AccessTools.Method(typeof(FloatMenuUtility), nameof(FloatMenuUtility.GetMeleeAttackAction)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(NoMeleeForVehicles))); //Change..?
            harmony.Patch(original: AccessTools.Method(typeof(PawnComponentsUtility), nameof(PawnComponentsUtility.AddComponentsForSpawn)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(AddComponentsForVehicleSpawn)));
            harmony.Patch(original: AccessTools.Method(typeof(PawnComponentsUtility), nameof(PawnComponentsUtility.AddAndRemoveDynamicComponents)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(AddAndRemoveVehicleComponents)));

            /* Construction */
            harmony.Patch(original: AccessTools.Method(typeof(Frame), nameof(Frame.CompleteConstruction)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(CompleteConstructionVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(ListerBuildingsRepairable), nameof(ListerBuildingsRepairable.Notify_BuildingRepaired)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(Notify_RepairedVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(GenSpawn), name: nameof(GenSpawn.Spawn), new Type[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool) }),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(SpawnVehicleGodMode)));

            /* Extra */
            harmony.Patch(original: AccessTools.Property(typeof(MapPawns), nameof(MapPawns.FreeColonistsSpawnedOrInPlayerEjectablePodsCount)).GetGetMethod(), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(FreeColonistsInVehiclesTransport)));
            harmony.Patch(original: AccessTools.Method(typeof(Selector), "HandleMapClicks"),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(MultiSelectFloatMenu)));
            harmony.Patch(original: AccessTools.Method(typeof(MentalState_Manhunter), nameof(MentalState_Manhunter.ForceHostileTo), new Type[] { typeof(Thing) }), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(ManhunterDontAttackVehicles)));
            harmony.Patch(original: AccessTools.Method(typeof(Projectile_Explosive), "Impact"),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(ShellsImpactWater)));

            /* Debug */
            if(debug)
            {
                harmony.Patch(original: AccessTools.Method(typeof(WorldRoutePlanner), nameof(WorldRoutePlanner.WorldRoutePlannerUpdate)), prefix: null,
                    postfix: new HarmonyMethod(typeof(ShipHarmony),
                    nameof(DebugSettlementPaths)));
                harmony.Patch(original: AccessTools.Method(typeof(WorldObjectsHolder), nameof(WorldObjectsHolder.Add)),
                    prefix: new HarmonyMethod(typeof(ShipHarmony),
                    nameof(DebugWorldObjects)));
                harmony.Patch(original: AccessTools.Method(typeof(RegionGrid), nameof(RegionGrid.DebugDraw)), prefix: null,
                    postfix: new HarmonyMethod(typeof(ShipHarmony),
                    nameof(DebugDrawWaterRegion)));
            }

            harmony.Patch(original: AccessTools.Method(typeof(Need_Food), nameof(Need_Food.NeedInterval)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(TestDebug)));

            HelperMethods.CannonTargeter = new CannonTargeter();

            HelperMethods.missingIcon = ContentFinder<Texture2D>.Get("Upgrades/missingIcon", true);
            HelperMethods.assignedSeats = new Dictionary<Pawn, Pair<VehiclePawn, VehicleHandler>>();

            HelperMethods.ValidateAllVehicleDefs();
            #endregion Functions
        }

        public static void TestDebug(Pawn ___pawn, Need_Food __instance)
        {
            if(___pawn.ParentHolder != null && ___pawn.ParentHolder is VehicleHandler)
            {
                //Log.Message($"Frozen: {___pawn.LabelShort} - { !(___pawn.SpawnedOrAnyParentSpawned || ___pawn.IsCaravanMember() || PawnUtility.IsTravelingInTransportPodWorldObject(___pawn))}");
            }
        }

        #region Debug

        /// <summary>
        /// Show original settlement positions before being moved to the coast
        /// </summary>
        /// <param name="o"></param>
        public static void DebugWorldObjects(WorldObject o)
        {
            if(o is Settlement)
            {
                tiles.Add(new Pair<int, int>(o.Tile, 0));
            }
        }

        /// <summary>
        /// Draw water regions to show if they are valid and initialized
        /// </summary>
        /// <param name="___map"></param>
        public static void DebugDrawWaterRegion(Map ___map)
        {
            WaterMapUtility.GetExtensionToMap(___map)?.getWaterRegionGrid?.DebugDraw();
        }

        /// <summary>
        /// Draw paths from original settlement position to new position when moving settlement to coastline
        /// </summary>
        public static void DebugSettlementPaths()
        {
            if (ShipHarmony.drawPaths && (ShipHarmony.debugLines is null || !ShipHarmony.debugLines.Any())) return;
            if (!ShipHarmony.drawPaths) goto DrawRings;
            foreach (WorldPath wp in ShipHarmony.debugLines)
            {
                wp.DrawPath(null);
            }
        DrawRings:
            foreach (Pair<int, int> t in ShipHarmony.tiles)
            {
                GenDraw.DrawWorldRadiusRing(t.First, t.Second);
            }
        }

        #endregion Debug

        #region MapGen

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
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.CustomFloatBeach)));
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
                    ///Call method, grab new location and store
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.PushSettlementToCoast)));
                    yield return new CodeInstruction(opcode: OpCodes.Stloc_1);
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_1);
                }
                yield return instruction;
            }
        }

        /// <summary>
        /// Hook on recalculating pathgrid that recalculates the path cost under Things (with additional path costs) for water based pathgrid
        /// </summary>
        /// <param name="t"></param>
        /// <param name="___map"></param>
        public static void RecalculateShipPathCostUnderThing(Thing t, Map ___map)
        {
            if (t is null) return;
            ___map.GetComponent<WaterMap>()?.getShipPathGrid?.RecalculatePerceivedPathCostUnderThing(t);
        }

        #endregion MapGen

        #region HealthStats

        /// <summary>
        /// Replace vanilla labels on Boats to instead show custom ones which are modifiable in the XML defs
        /// </summary>
        /// <param name="__result"></param>
        /// <param name="pawn"></param>
        /// <param name="shortVersion"></param>
        /// <returns></returns>
        public static bool ReplaceConditionLabel(ref string __result, Pawn pawn, bool shortVersion = false)
        {
            if (pawn != null)
            {
                CompVehicle vehicle = pawn.GetComp<CompVehicle>();
                if(HelperMethods.IsVehicle(pawn))
                {
                    if (vehicle.movementStatus == VehicleMovementStatus.Offline && !pawn.Dead)
                    {
                        if (HelperMethods.IsBoat(pawn) && vehicle.beached)
                        {
                            __result = vehicle.Props.healthLabel_Beached;
                        }
                        else
                        {
                            __result = vehicle.Props.healthLabel_Immobile;
                        }

                        return false;
                    }
                    if (pawn.Dead)
                    {
                        __result = vehicle.Props.healthLabel_Dead;
                        return false;
                    }
                    if (pawn.health.summaryHealth.SummaryHealthPercent < 0.95)
                    {
                        __result = vehicle.Props.healthLabel_Injured;
                        return false;
                    }
                    __result = vehicle.Props.healthLabel_Healthy;
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Only allow the Boat to be downed if specified within XML def
        /// </summary>
        /// <param name="__result"></param>
        /// <param name="___pawn"></param>
        /// <returns></returns>
        public static bool VehicleShouldBeDowned(ref bool __result, ref Pawn ___pawn)
        {
            if (___pawn != null && HelperMethods.IsVehicle(___pawn))
            {
                __result = ___pawn.GetComp<CompVehicle>().Props.downable;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Only allow the Boat to wiggle if specified within the XML def
        /// </summary>
        /// <param name="___pawn"></param>
        /// <returns></returns>
        public static bool VehicleShouldWiggle(ref Pawn ___pawn)
        {
            if (___pawn != null && HelperMethods.IsVehicle(___pawn) && !___pawn.GetComp<CompVehicle>().Props.movesWhenDowned)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Vehicles do not heal over time, and must be repaired instead
        /// </summary>
        /// <param name="hd"></param>
        /// <param name="__result"></param>
        /// <returns></returns>
        public static bool VehiclesDontHeal(Hediff_Injury hd, ref bool __result)
        {
            Pawn pawn = Traverse.Create(hd).Field("pawn").GetValue<Pawn>();
            if(HelperMethods.IsVehicle(pawn))
            {
                __result = false;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Boats can not be tended, and thus don't heal. They must be repaired instead
        /// </summary>
        /// <param name="hd"></param>
        /// <param name="__result"></param>
        /// <returns></returns>
        public static bool VehiclesDontHealTended(Hediff_Injury hd, ref bool __result)
        {
            Pawn pawn = Traverse.Create(hd).Field("pawn").GetValue<Pawn>();
            
            if(HelperMethods.IsVehicle(pawn))
            {
                __result = false;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Remove and replace Vehicle's info cards. Info Card is currently Work In Progress
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="ilg"></param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> InfoCardVehiclesTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for(int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.Calls(AccessTools.Property(typeof(Find), nameof(Find.WindowStack)).GetGetMethod()))
                {
                    Label label = ilg.DefineLabel();
                    ///Check if pawn in question is a Boat
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_2);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.IsVehicle), new Type[] { typeof(Thing) }));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

                    ///Load a new object of type Dialog_InfoCard_Ship and load onto the WindowStack. Return after
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Property(typeof(Find), nameof(Find.WindowStack)).GetGetMethod());
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_2);
                    yield return new CodeInstruction(opcode: OpCodes.Newobj, operand: AccessTools.Constructor(typeof(Dialog_InfoCard_Vehicle), new Type[] { typeof(Thing) }));
                    yield return new CodeInstruction(opcode: OpCodes.Callvirt, operand: AccessTools.Method(typeof(WindowStack), nameof(WindowStack.Add)));
                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(opcode: OpCodes.Ret);

                    instruction.labels.Add(label);
                }

                yield return instruction;
            }
        }

        #endregion HealthStats

        #region Rendering
        /// <summary>
        /// Use own Boat rotation to disallow moving rotation for various tasks such as Drafted
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        public static bool UpdateVehicleRotation(Pawn_RotationTracker __instance, Pawn ___pawn)
        {
            if (___pawn is VehiclePawn pawn && HelperMethods.IsVehicle(pawn))
            {
                if (pawn.Destroyed || pawn.jobs.HandlingFacing)
                {
                    return false;
                }
                if (pawn.vPather.Moving)
                {
                    if (pawn.vPather.curPath == null || pawn.vPather.curPath.NodesLeftCount < 1)
                    {
                        return false;
                    }
                    HelperMethods.FaceShipAdjacentCell(pawn.vPather.nextCell, pawn);
                }
                else
                {
                    //Stance busy code here

                    if (pawn.jobs.curJob != null)
                    {
                        //LocalTargetInfo target = shipPawn.CurJob.GetTarget(shipPawn.jobs.curDriver.rotateToFace);
                        //Face Target here
                    }
                    if (pawn.Drafted)
                    {
                        //Ship Pawn Rotation stays the same
                    }

                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Render Pawn's iconic diagonal rotation if allowed in the def's XML.
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="ilg"></param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> RenderPawnRotationTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            Label label = ilg.DefineLabel();

            ///Check if the pawn being rendered is a vehicle
            yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
            yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(typeof(PawnRenderer), "pawn"));
            yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.IsVehicle), new Type[] { typeof(Pawn) }));
            yield return new CodeInstruction(opcode: OpCodes.Brfalse, operand: label);

            ///Get resulting angle value determined by diagonal movement, store inside angle vector
            yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
            yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(typeof(PawnRenderer), "pawn"));
            yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.ShipAngle)));
            yield return new CodeInstruction(opcode: OpCodes.Starg_S, operand: 2);

            yield return new CodeInstruction(opcode: OpCodes.Nop) { labels = new List<Label> { label } };
            foreach (CodeInstruction instruction in instructionList)
            {
                yield return instruction;
            }
        }

        public static IEnumerable<CodeInstruction> MatsBodyOfVehicles(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for(int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if(instruction.Calls(AccessTools.Method(typeof(Graphic), nameof(Graphic.MatAt))))
                {
                    Label brlabel = ilg.DefineLabel();
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(typeof(PawnGraphicSet), "pawn"));
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.IsVehicle), new Type[] { typeof(Pawn) }));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, brlabel);

                    yield return new CodeInstruction(opcode: OpCodes.Pop);
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(typeof(PawnGraphicSet), "pawn"));

                    instruction.labels.Add(brlabel);
                }
                yield return instruction;
            }
        }

        /// <summary>
        /// Draw pawns onboard vehicles and in vehicle caravans onto the colonist bar
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> CheckRecacheEntriesTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            bool flag = false;
            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Stloc_2)
                {
                    yield return instruction; //stloc.2
                    instruction = instructionList[++i];

                    ///get pawns onboard vehicle and store inside list to render on colonist bar
                    yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(typeof(ColonistBar), "tmpPawns"));
                    yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(typeof(ColonistBar), "tmpMaps"));
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_1);
                    yield return new CodeInstruction(opcode: OpCodes.Callvirt, operand: AccessTools.Property(typeof(List<Map>), "Item").GetGetMethod());
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.GetVehiclesForColonistBar)));
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(List<Pawn>), nameof(List<Pawn>.AddRange)));
                }
                if(instruction.opcode == OpCodes.Stloc_S && ((LocalBuilder)instruction.operand).LocalIndex == 9)
                {
                    flag = true;
                }    
                if (flag && instruction.Calls(AccessTools.Method(typeof(PlayerPawnsDisplayOrderUtility), nameof(PlayerPawnsDisplayOrderUtility.Sort))))
                {
                    ///grab pawns from vehicle caravan and store inside list to be rendered on colonist bar
                    yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(typeof(ColonistBar), "tmpCaravans"));
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, 9);
                    yield return new CodeInstruction(opcode: OpCodes.Callvirt, operand: AccessTools.Property(typeof(List<Caravan>), "Item").GetGetMethod());
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.ExtractPawnsFromCaravan)));

                    yield return new CodeInstruction(opcode: OpCodes.Callvirt, operand: AccessTools.Method(typeof(List<Pawn>), nameof(List<Pawn>.AddRange)));
                    yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(typeof(ColonistBar), "tmpPawns"));
                }

                yield return instruction;
            }
        }

        /// <summary>
        /// Render small boat icon on colonist bar picture rect if they are currently onboard a boat
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="colonist"></param>
        public static void DrawIconsVehicles(Rect rect, Pawn colonist)
        {
            if(colonist.Dead || !(colonist.ParentHolder is VehicleHandler handler))
            {
                return;
            }
            float num = 20f * Find.ColonistBar.Scale;
            Vector2 vector = new Vector2(rect.x + 1f, rect.yMax - num - 1f);

            Rect rect2 = new Rect(vector.x, vector.y, num, num);
            GUI.DrawTexture(rect2, TexCommandVehicles.CachedTextureIcons[handler.vehiclePawn.def]);
            TooltipHandler.TipRegion(rect2, "ActivityIconOnBoardShip".Translate(handler.vehiclePawn.Label)); 
            vector.x += num;
        }

        /// <summary>
        /// Draw diagonal and shifted brackets for Boats
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool DrawSelectionBracketsVehicles(object obj)
        {
            if(HelperMethods.IsVehicle(obj as VehiclePawn))
            {
                Thing thing = obj as Thing;
                Vector3[] brackets = new Vector3[4];
                float angle = (thing as VehiclePawn).GetComp<CompVehicle>().Angle; //(thing as VehiclePawn).GetComp<CompVehicle>().BearingAngle;

                Vector3 newDrawPos = (thing as VehiclePawn).DrawPosTransformed((thing as VehiclePawn).GetComp<CompVehicle>().Props.hitboxOffsetX, (thing as VehiclePawn).GetComp<CompVehicle>().Props.hitboxOffsetZ, angle);

                FieldInfo info = AccessTools.Field(typeof(SelectionDrawer), "selectTimes");
                object o = info.GetValue(null);
                SPMultiCell.CalculateSelectionBracketPositionsWorldForMultiCellPawns<object>(brackets, thing, newDrawPos, thing.RotatedSize.ToVector2(), (Dictionary<object, float>)o, Vector2.one, angle, 1f);

                int num = (angle != 0) ? (int)angle : 0;
                for (int i = 0; i < 4; i++)
                {
                    Quaternion rotation = Quaternion.AngleAxis((float)num, Vector3.up);
                    Graphics.DrawMesh(MeshPool.plane10, brackets[i], rotation, MaterialDefOf.SelectionBracketMat, 0);
                    num -= 90;
                }
                return false;
            }
            //Add for building too?
            return true;
        }

        /// <summary>
        /// Create custom water footprints, resembling a wake behind the boat
        /// </summary>
        /// <param name="___pawn"></param>
        /// <param name="___lastFootprintPlacePos"></param>
        /// <returns></returns>
        public static bool BoatWakesTicker(Pawn ___pawn, ref Vector3 ___lastFootprintPlacePos)
        {
            if(HelperMethods.IsBoat(___pawn))
            {
                if ((___pawn.Drawer.DrawPos - ___lastFootprintPlacePos).MagnitudeHorizontalSquared() > 0.1)
                {
                    Vector3 drawPos = ___pawn.Drawer.DrawPos;
                    if (drawPos.ToIntVec3().InBounds(___pawn.Map) && !___pawn.GetComp<CompVehicle>().beached)
                    {
                        MoteMaker.MakeWaterSplash(drawPos, ___pawn.Map, 7 * ___pawn.GetComp<CompVehicle>().Props.wakeMultiplier, ___pawn.GetComp<CompVehicle>().Props.wakeSpeed);
                        ___lastFootprintPlacePos = drawPos;
                    }
                }
                else if(VehicleMod.mod.settings.passiveWaterWaves)
                {
                    if(Find.TickManager.TicksGame % 360 == 0)
                    {
                        float offset = Mathf.PingPong(Find.TickManager.TicksGame / 10, ___pawn.kindDef.lifeStages.Find(x => x.bodyGraphicData != null).bodyGraphicData.drawSize.y / 4);
                        MoteMaker.MakeWaterSplash(___pawn.Drawer.DrawPos - new Vector3(0,0, offset), ___pawn.Map, ___pawn.GetComp<CompVehicle>().Props.wakeMultiplier, ___pawn.GetComp<CompVehicle>().Props.wakeSpeed);
                    }
                }
                return false;
            }
            return true;
        }

        public static void DrawCannonTargeter()
        {
            HelperMethods.CannonTargeter.TargeterOnGUI();
        }

        public static void ProcessCannonInputEvents()
        {
            HelperMethods.CannonTargeter.ProcessInputEvents();
        }

        public static void CannonTargeterUpdate()
        {
            HelperMethods.CannonTargeter.TargeterUpdate();
        }

        public static void RegisterDiagonalMovement(Rot4 facing, PawnGraphicSet __instance, ref List<Material> ___cachedMatsBodyBase, ref int ___cachedMatsBodyBaseHash, RotDrawMode bodyCondition = RotDrawMode.Fresh)
        {
            if(__instance.pawn is VehiclePawn vehicle && HelperMethods.IsVehicle(vehicle))
            {
                if(facing.IsHorizontal && vehicle.GetComp<CompVehicle>().Angle != vehicle.GetComp<CompVehicle>().CachedAngle)
                {
                    ___cachedMatsBodyBase.Clear();
                    ___cachedMatsBodyBaseHash = -1;
                    vehicle.GetComp<CompVehicle>().CachedAngle = vehicle.GetComp<CompVehicle>().Angle;
                }
            }
        }

        public static bool VehiclesDamageTakenWiggler(DamageInfo dinfo, Pawn ___pawn, Pawn_DrawTracker __instance)
        {
            if(HelperMethods.IsVehicle(___pawn) && !___pawn.GetComp<CompVehicle>().Props.movesWhenDowned)
            {
                __instance.renderer.Notify_DamageApplied(dinfo);
                return false;
            }
            return true;
        }

        public static bool VehiclesDamageDeflectedWiggler(DamageInfo dinfo, Pawn ___pawn, Pawn_DrawTracker __instance)
        {
            if(HelperMethods.IsVehicle(___pawn) && !___pawn.GetComp<CompVehicle>().Props.movesWhenDowned)
            {
                return false;
            }
            return true;
        }

        #endregion Rendering

        #region Drafting

        /// <summary>
        /// Allow vehicles to be drafted under the right conditions
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool DraftedVehiclesCanMove(Pawn_DraftController __instance, bool value)
        {
            if(__instance.pawn.IsVehicle())
            {
                VehiclePawn vehicle = __instance.pawn as VehiclePawn;

                if (VehicleMod.mod.settings.debugDisableWaterPathing && __instance.pawn.GetComp<CompVehicle>().beached)
                    vehicle.GetComp<CompVehicle>().RemoveBeachedStatus();
                if (value && !__instance.Drafted)
                {
                    if(!VehicleMod.mod.settings.debugDraftAnyShip && vehicle.TryGetComp<CompFueledTravel>() != null && vehicle.GetComp<CompFueledTravel>().EmptyTank)
                    {
                        Messages.Message("CompShips_OutOfFuel".Translate(), MessageTypeDefOf.RejectInput);
                        return false;
                    }
                    if(!VehicleMod.mod.settings.debugDraftAnyShip && !vehicle.GetComp<CompVehicle>().ResolveSeating())
                    {
                        Messages.Message("CompShips_CannotMove".Translate(), MessageTypeDefOf.RejectInput);
                        return false;
                    }
                }
                else if(!value && vehicle.vPather.curPath != null)
                {
                    vehicle.vPather.PatherFailed();
                }
                if(!VehicleMod.mod.settings.fishingPersists) __instance.pawn.GetComp<CompVehicle>().currentlyFishing = false;
            }
            return true;
        }

        /// <summary>
        /// Allow vehicles to take orders despite them not being categorized as humanlike
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="__result"></param>
        public static void CanVehicleTakeOrder(Pawn pawn, ref bool __result)
        {
            if(__result is false)
            {
                __result = HelperMethods.IsVehicle(pawn);
            }
        }

        /// <summary>
        /// Disable melee attacks for vehicles, which don't work anyways due to not having Manipulation capacity and only cause errors
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="target"></param>
        /// <param name="failStr"></param>
        /// <returns></returns>
        public static bool NoMeleeForVehicles(Pawn pawn, LocalTargetInfo target, out string failStr)
        {
            if(HelperMethods.IsVehicle(pawn))
            {
                failStr = "IsIncapableOfRamming".Translate(target.Thing.LabelShort);
                //Add more to string or Action if ramming is implemented
                return false;
            }
            failStr = string.Empty;
            return true;
        }

        /// <summary>
        /// Shells impacting water now have reduced radius of effect and different sound
        /// </summary>
        /// <param name="hitThing"></param>
        /// <param name="__instance"></param>
        /// <returns></returns>
        public static bool ShellsImpactWater(Thing hitThing, ref Projectile __instance)
        {
            Map map = __instance.Map;
            TerrainDef terrainImpact = map.terrainGrid.TerrainAt(__instance.Position);
            if(__instance.def.projectile.explosionDelay == 0 && terrainImpact.IsWater && !__instance.Position.GetThingList(__instance.Map).Any(x => HelperMethods.IsVehicle(x as Pawn)))
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

                int waterDepth = map.terrainGrid.TerrainAt(__instance.Position).IsWater ? map.terrainGrid.TerrainAt(__instance.Position) == TerrainDefOf.WaterOceanShallow ||
                    map.terrainGrid.TerrainAt(__instance.Position) == TerrainDefOf.WaterShallow || map.terrainGrid.TerrainAt(__instance.Position) == TerrainDefOf.WaterMovingShallow ? 1 : 2 : 0;
                if (waterDepth == 0) Log.Error("Impact Water Depth is 0, but terrain is water.");
                float explosionRadius = (__instance.def.projectile.explosionRadius / (2f * waterDepth));
                if (explosionRadius < 1) explosionRadius = 1f;
                DamageDef damageDef = __instance.def.projectile.damageDef;
                Thing launcher = null;
                int damageAmount = __instance.DamageAmount;
                float armorPenetration = __instance.ArmorPenetration;
                SoundDef soundExplode;
                soundExplode = SoundDefOf_Ships.Explode_BombWater; //Changed for current issues
                SoundStarter.PlayOneShot(soundExplode, new TargetInfo(__instance.Position, map, false));
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

        public static void AddComponentsForVehicleSpawn(Pawn pawn)
        {
            if(HelperMethods.IsVehicle(pawn))
            {
                if ((pawn as VehiclePawn).vPather == null)
			    {
				    (pawn as VehiclePawn).vPather = new Vehicle_PathFollower(pawn as VehiclePawn);
			    }
            }
        }

        /// <summary>
        /// Ensure that vehicles are given the right components when terminating from the main method
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="actAsIfSpawned"></param>
        public static void AddAndRemoveVehicleComponents(Pawn pawn, bool actAsIfSpawned = false)
        {
            if(HelperMethods.IsVehicle(pawn) && (pawn.Spawned || actAsIfSpawned) && pawn.drafter is null)
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

        public static IEnumerable<Gizmo> AddVehicleCaravanGizmoPassthrough(IEnumerable<Gizmo> __result, Settlement __instance)
        {
            IEnumerator<Gizmo> enumerator = __result.GetEnumerator();
            if(__instance.Faction == Faction.OfPlayer)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "CommandFormVehicleCaravan".Translate(),
		            defaultDesc = "CommandFormVehicleCaravanDesc".Translate(),
		            icon = Settlement.FormCaravanCommand,
                    action = delegate ()
                    {
                        Find.Tutor.learningReadout.TryActivateConcept(ConceptDefOf.FormCaravan);
                        Find.WindowStack.Add(new Dialog_FormVehicleCaravan(__instance.Map));
                    }
                };
            }
            while(enumerator.MoveNext())
            {
                var element = enumerator.Current;
                yield return element;
            }
        }

        /// <summary>
        /// Disable the ability to attack a settlement when docked there. Breaks immersion and can cause an entry cell error
        /// </summary>
        /// <param name="caravan"></param>
        /// <param name="__result"></param>
        /// <param name="__instance"></param>
        public static void NoAttackSettlementWhenDocked(Caravan caravan, ref IEnumerable<Gizmo> __result, Settlement __instance)
        {
            if(HelperMethods.HasBoat(caravan) && !caravan.pather.Moving)
            {
                List<Gizmo> gizmos = __result.ToList();
                if (caravan.PawnsListForReading.Any(x => !HelperMethods.IsBoat(x)))
                {
                    int index = gizmos.FindIndex(x => (x as Command_Action).icon == Settlement.AttackCommand);
                    if (index >= 0 && index < gizmos.Count)
                        gizmos[index].Disable("CommandAttackDockDisable".Translate(__instance.LabelShort));
                }
                else
                {
                    int index2 = gizmos.FindIndex(x => (x as Command_Action).icon == ContentFinder<Texture2D>.Get("UI/Commands/Trade", false));
                    if (index2 >= 0 && index2 < gizmos.Count)
                        gizmos[index2].Disable("CommandTradeDockDisable".Translate(__instance.LabelShort));
                    int index3 = gizmos.FindIndex(x => (x as Command_Action).icon == ContentFinder<Texture2D>.Get("UI/Commands/OfferGifts", false));
                    if (index3 >= 0 && index3 < gizmos.Count)
                        gizmos[index3].Disable("CommandTradeDockDisable".Translate(__instance.LabelShort));
                }
                __result = gizmos;
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

        #endregion Gizmos

        #region Pathing

        public static bool GotoLocationShips(IntVec3 clickCell, Pawn pawn, ref FloatMenuOption __result)
        {
            if (HelperMethods.IsVehicle(pawn))
            {
                if( (pawn as VehiclePawn).LocationRestrictedBySize(clickCell))
                {
                    Messages.Message("VehicleCannotFit".Translate(), MessageTypeDefOf.RejectInput);
                    return false;
                }

                if (HelperMethods.IsBoat(pawn))
                {
                    if (DebugSettings.godMode)
                    {
                        Log.Message("-> " + clickCell + " | " + pawn.Map.terrainGrid.TerrainAt(clickCell).LabelCap + " | " + WaterMapUtility.GetExtensionToMap(pawn.Map).getShipPathGrid.CalculatedCostAt(clickCell) +
                            " - " + WaterMapUtility.GetExtensionToMap(pawn.Map).getShipPathGrid.pathGrid[pawn.Map.cellIndices.CellToIndex(clickCell)]);
                    }


                    //if (!VehicleMod.mod.settings.debugDisableSmoothPathing && pawn.GetComp<CompVehicle>().Props.diagonalRotation)
                    //{
                    //    if (!(pawn as VehiclePawn).InitiateSmoothPath(clickCell))
                    //    {
                    //        Log.Error($"Failed Smooth Pathing. Cell: {clickCell} Pawn: {pawn.LabelShort}");
                    //    }
                    //    return false;
                    //}
                    if (VehicleMod.mod.settings.debugDisableWaterPathing)
                        return true;

                    int num = GenRadial.NumCellsInRadius(2.9f);
                    int i = 0;
                    IntVec3 curLoc;
                    while (i < num)
                    {
                        curLoc = GenRadial.RadialPattern[i] + clickCell;
                        if (GenGridShips.Standable(curLoc, pawn.Map))
                        {
                            if (curLoc == pawn.Position || pawn.GetComp<CompVehicle>().beached)
                            {
                                __result = null;
                                return false;
                            }
                            if (!ShipReachabilityUtility.CanReachShip(pawn, curLoc, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                            {
                                if (debug) Log.Message("CANT REACH ");
                                __result = new FloatMenuOption("CannotSailToCell".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null);
                                return false;
                            }
                            Action action = delegate ()
                            {
                                Job job = new Job(JobDefOf.Goto, curLoc);
                                if (pawn.Map.exitMapGrid.IsExitCell(Verse.UI.MouseCell()))
                                {
                                    job.exitMapOnArrival = true;
                                }
                                else if (!pawn.Map.IsPlayerHome && !pawn.Map.exitMapGrid.MapUsesExitGrid && CellRect.WholeMap(pawn.Map).IsOnEdge(Verse.UI.MouseCell(), 3) &&
                                    pawn.Map.Parent.GetComponent<FormCaravanComp>() != null && MessagesRepeatAvoider.MessageShowAllowed("MessagePlayerTriedToLeaveMapViaExitGrid-" +
                                    pawn.Map.uniqueID, 60f))
                                {
                                    FormCaravanComp component = pawn.Map.Parent.GetComponent<FormCaravanComp>();
                                    if (component.CanFormOrReformCaravanNow)
                                    {
                                        Messages.Message("MessagePlayerTriedToLeaveMapViaExitGrid_CanReform".Translate(), pawn.Map.Parent, MessageTypeDefOf.RejectInput, false);
                                    }
                                    else
                                    {
                                        Messages.Message("MessagePlayerTriedToLeaveMapViaExitGrid_CantReform".Translate(), pawn.Map.Parent, MessageTypeDefOf.RejectInput, false);
                                    }
                                }
                                if (pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc))
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
            }
            return true;
        }

        public static bool StartVehiclePath(LocalTargetInfo dest, PathEndMode peMode, Pawn ___pawn)
        {
            if(HelperMethods.IsVehicle(___pawn))
            {
                (___pawn as VehiclePawn).vPather.StartPath(dest, peMode);
                return false;
            }
            return true;
        }

        public static void VehicleTweenedPosRoot(ref Vector3 __result, Pawn ___pawn)
		{
            if(HelperMethods.IsVehicle(___pawn))
            {
                if (!___pawn.Spawned)
			    {
				    __result = ___pawn.Position.ToVector3Shifted();
                    return;
			    }
                float num = HelperMethods.MovedPercent(___pawn as VehiclePawn);
			    __result = (___pawn as VehiclePawn).vPather.nextCell.ToVector3Shifted() * num + ___pawn.Position.ToVector3Shifted() * (1f - num) + PawnCollisionTweenerUtility.PawnCollisionPosOffsetFor(___pawn);
            }
		}

        public static bool ReserveEntireVehicle(Pawn p, Job job, IntVec3 loc, PawnDestinationReservationManager __instance)
        {
            //REDO
            //if(HelperMethods.IsVehicle(p) && p.GetComp<CompVehicle>().Props.reserveFullHitbox)
            //{
            //    foreach(IntVec3 c in CellRect.CenteredOn(loc, p.def.size.x, p.def.size.z))
            //    {
            //        if (p.Faction == null)
	           //     {
		          //      return false;
	           //     }
	           //     Pawn pawn;
	           //     if (p.Drafted && p.Faction == Faction.OfPlayer && __instance.IsReserved(c, out pawn) && pawn != p && !pawn.HostileTo(p) && pawn.Faction != p.Faction && (pawn.mindState == null || pawn.mindState.mentalStateHandler == null || !pawn.mindState.mentalStateHandler.InMentalState || (pawn.mindState.mentalStateHandler.CurStateDef.category != MentalStateCategory.Aggro && pawn.mindState.mentalStateHandler.CurStateDef.category != MentalStateCategory.Malicious)))
	           //     {
		          //      pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true, true);
	           //     }
	           //     __instance.ObsoleteAllClaimedBy(p);
	           //     __instance.GetPawnDestinationSetFor(p.Faction).list.Add(new PawnDestinationReservationManager.PawnDestinationReservation
	           //     {
		          //      target = c,
		          //      claimant = p,
		          //      job = job
	           //     });
            //    }
            //    return false;
            //}
            return true;
        }

        public static bool AutoOrderVehicleCaravanPathing(Caravan c, int tile)
        {
            if(c is VehicleCaravan && c.HasVehicle())
            {
                if (tile < 0 || (tile == c.Tile && !(c as VehicleCaravan).vPather.Moving))
			    {
				    return false;
			    }
                int num = c.BestGotoDestForVehicle(tile);
			    if (num >= 0)
			    {
				    (c as VehicleCaravan).vPather.StartPath(num, null, true, true);
				    c.gotoMote.OrderedToTile(num);
				    SoundDefOf.ColonistOrdered.PlayOneShotOnCamera(null);
			    }
                return false;
            }
            return true;
        }

        public static void VehicleRoutePlannerUpdateHook()
        {
            Find.World.GetComponent<VehicleRoutePlanner>().WorldRoutePlannerUpdate();
        }

        public static void VehicleRoutePlannerOnGUIHook()
        {
            Find.World.GetComponent<VehicleRoutePlanner>().WorldRoutePlannerOnGUI();
        }

        public static void VehicleRoutePlannerButton(ref float curBaseY)
        {
            Find.World.GetComponent<VehicleRoutePlanner>().DoRoutePlannerButton(ref curBaseY);
        }

        #endregion Pathing

        #region UpgradeStatModifiers

        public static bool VehicleMoveSpeedUpgradeModifier(bool diagonal, Pawn __instance, ref int __result)
        {
            if(HelperMethods.IsVehicle(__instance))
            {
                float num = __instance.GetComp<CompVehicle>().ActualMoveSpeed / 60f;
                float num2 = 0f;
                if(num == 0f)
                {
                    num2 = 450f;
                }
                else
                {
                    num2 = 1 / num;
                    if(diagonal)
                    {
                        num2 *= 1.41421f;
                    }
                }
                __result = Mathf.Clamp(Mathf.RoundToInt(num2), 1, 450);
                return false;
            }
            return true;
        }

        public static void VehicleCargoCapacity(Pawn p, ref float __result)
        {
            if(HelperMethods.IsVehicle(p))
            {
                __result = HelperMethods.ExtractUpgradeValue(p, StatUpgrade.CargoCapacity);
            }
        }

        #endregion

        #region Jobs
        public static bool VehicleErrorRecoverJob(Pawn pawn, string message, Exception exception = null, JobDriver concreteDriver = null)
        {
            if(HelperMethods.IsVehicle(pawn))
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

        public static bool VehiclesDontWander(Pawn pawn, ref Job __result)
        {
            if(HelperMethods.IsVehicle(pawn))
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
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Property(typeof(ThinkResult), nameof(ThinkResult.IsValid)).GetGetMethod());
                    yield return new CodeInstruction(opcode: OpCodes.Brtrue, label);

                    yield return new CodeInstruction(opcode: OpCodes.Ret) { labels = new List<Label> { retlabel } };

                    instruction.labels.Add(label);
                }

                yield return instruction;
            }
        }

        #endregion Jobs

        #region Caravan

        public static bool CapacityWithVehicle(List<ThingCount> thingCounts, ref float __result, StringBuilder explanation = null)
        {
            if(thingCounts.Any(x => HelperMethods.IsVehicle(x.Thing as Pawn)))
            {
                float num = 0f;
                foreach(ThingCount tc in thingCounts)
                {
                    if(tc.Count > 0)
                    {
                        if (tc.Thing is Pawn && HelperMethods.IsVehicle(tc.Thing as Pawn))
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
        public static bool CanCarryIfVehicle(Pawn p, ref bool __result)
        {
            __result = false;
            if(HelperMethods.IsVehicle(p))
                __result = true;
            return !__result;
        }

        public static bool IsFormingCaravanVehicle(Pawn p, ref bool __result)
        {
            Lord lord = p.GetLord();
            if (lord != null && (lord.LordJob is LordJob_FormAndSendVehicles))
            {
                __result = true;
                return false;
            }
            return true;
        }

        public static IEnumerable<CodeInstruction> CanStackVehicleTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
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
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.IsVehicle), new Type[] { typeof(Pawn) }));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ret);
                    instruction = instructionList[i];
                    instruction.labels.Add(label);
                }
                yield return instruction;
            }
        }

        public static bool UsableVehicleWithMostFreeSpace(Pawn pawn, ref Pawn __result)
        {
            if(HelperMethods.IsFormingCaravanShipHelper(pawn) || HelperMethods.HasVehicle(pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction)))
            {
                __result = HelperMethods.UsableVehicleWithTheMostFreeSpace(pawn);
                return false;
            }
            return true;
        }

        public static bool TransferablesVehicle(JobDriver_PrepareCaravan_GatherItems __instance, ref List<TransferableOneWay> __result)
        {
            if (__instance.job.lord.LordJob is LordJob_FormAndSendVehicles)
            {
                __result = ((LordJob_FormAndSendVehicles)__instance.job.lord.LordJob).transferables;
                return false;
            }
            return true;
        }

        public static IEnumerable<CodeInstruction> AddHumanLikeOrdersLoadVehiclesTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];
                if(instruction.LoadsField(AccessTools.Field(typeof(JobDefOf), nameof(JobDefOf.GiveToPackAnimal))))
                {
                    yield return instruction; //Ldsfld : JobDefOf::GiveToPackAnimal
                    instruction = instructionList[++i];
                    Label jobLabel = ilg.DefineLabel();

                    yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.makingFor)));
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.HasVehicleInCaravan)));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, jobLabel);

                    yield return new CodeInstruction(opcode: OpCodes.Pop);
                    yield return new CodeInstruction(opcode: OpCodes.Ldsfld, AccessTools.Field(typeof(JobDefOf_Ships), nameof(JobDefOf_Ships.CarryItemToShip)));
                    int j = i;
                    while(j < instructionList.Count)
                    {
                        j++;
                        if(instructionList[j].opcode == OpCodes.Stfld)
                        {
                            instructionList[j].labels.Add(jobLabel);
                            break;
                        }
                    }
                }
                if(instruction.Calls(AccessTools.Property(typeof(Lord), nameof(Lord.LordJob)).GetGetMethod()))
                {
                    yield return instruction;
                    instruction = instructionList[++i];
                    Label label = ilg.DefineLabel();
                    Label label2 = ilg.DefineLabel();

                    yield return new CodeInstruction(opcode: OpCodes.Dup);
                    yield return new CodeInstruction(opcode: OpCodes.Isinst, operand: typeof(LordJob_FormAndSendVehicles));
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
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(LordUtility), "GetLord", new Type[] { typeof(Pawn) }));
                    yield return new CodeInstruction(opcode: OpCodes.Callvirt, operand: AccessTools.Property(typeof(Lord), nameof(Lord.LordJob)).GetGetMethod());
                    yield return new CodeInstruction(opcode: OpCodes.Castclass, operand: typeof(LordJob_FormAndSendVehicles));
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.CapacityLeft)));

                    instruction.labels.Add(label2);
                    yield return instruction; //STFLD : capacityLeft
                    instruction = instructionList[++i];
                }
                yield return instruction;
            }
        }

        public static void UpdateDutyOfVehicle(LordToil_PrepareCaravan_GatherAnimals __instance)
        {
            if(__instance.lord.LordJob is LordJob_FormAndSendVehicles)
            {
                List<Pawn> ships = __instance.lord.ownedPawns.Where(x => !(x.GetComp<CompVehicle>() is null)).ToList();
                foreach(Pawn ship in ships)
                {
                    ship.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.PrepareCaravan_WaitShip);
                }
            }
        }

        public static bool FillTabVehicleCaravan(ITab_Pawn_FormingCaravan __instance, ref List<Thing> ___thingsToSelect, Vector2 ___size, 
            ref float ___lastDrawnHeight, ref Vector2 ___scrollPosition, ref List<Thing> ___tmpSingleThing)
        {
            if((Find.Selector.SingleSelectedThing as Pawn).GetLord().LordJob is LordJob_FormAndSendVehicles)
            {
                ___thingsToSelect.Clear();
                Rect outRect = new Rect(default(Vector2), ___size).ContractedBy(10f);
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
                HelperMethods.DoItemsListForVehicle(rect, ref num, ref ___tmpSingleThing, __instance);
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
                    if(HelperMethods.IsVehicle(pawn))
                    {
                        if(pawn.GetComp<CompVehicle>().AllPawnsAboard.Any())
                        {
                            num += pawn.GetComp<CompVehicle>().AllPawnsAboard.FindAll(x => x.IsFreeColonist).Count;
                            num2 += pawn.GetComp<CompVehicle>().AllPawnsAboard.FindAll(x => x.IsFreeColonist && x.InMentalState).Count;
                            num3 += pawn.GetComp<CompVehicle>().AllPawnsAboard.FindAll(x => x.IsPrisoner).Count;
                            num4 += pawn.GetComp<CompVehicle>().AllPawnsAboard.FindAll(x => x.IsPrisoner && x.InMentalState).Count;
                            num5 += pawn.GetComp<CompVehicle>().AllPawnsAboard.FindAll(x => x.RaceProps.Animal).Count;
                            num6 += pawn.GetComp<CompVehicle>().AllPawnsAboard.FindAll(x => x.RaceProps.Animal && x.InMentalState).Count;
                            num7 += pawn.GetComp<CompVehicle>().AllPawnsAboard.FindAll(x => x.RaceProps.Animal && x.RaceProps.packAnimal).Count;
                        }
                        if(!pawn.GetComp<CompVehicle>().beached)
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
                        if(HelperMethods.IsVehicle(p))
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
                        if(HelperMethods.IsVehicle(p))
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
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.HasBoat), new Type[] { typeof(Caravan) }));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(EnterMapUtilityVehicles), nameof(EnterMapUtilityVehicles.Enter)));
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
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.HasBoat), new Type[] { typeof(Caravan) }));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(EnterMapUtilityVehicles), nameof(EnterMapUtilityVehicles.Enter)));
                    yield return new CodeInstruction(opcode: OpCodes.Br, brlabel);

                    instruction.labels.Add(label);
                    yield return instruction; //CALL : CaravanEnterMapUtility::Enter
                    instruction = instructionList[++i];

                    instruction.labels.Add(brlabel);
                }
                yield return instruction;
            }
        }

        public static IEnumerable<CodeInstruction> SettleFromSeaTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
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
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.HasBoat), new Type[] { typeof(Caravan) }));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(EnterMapUtilityVehicles), nameof(EnterMapUtilityVehicles.Enter)));
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
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.HasBoat), new Type[] { typeof(Caravan) }));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(EnterMapUtilityVehicles), nameof(EnterMapUtilityVehicles.Enter)));
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
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.HasBoat), new Type[] { typeof(Caravan) }));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(EnterMapUtilityVehicles), nameof(EnterMapUtilityVehicles.Enter)));
                    yield return new CodeInstruction(opcode: OpCodes.Br, brlabel);

                    instruction.labels.Add(label);
                    yield return instruction; //CALL : CaravanEnterMapUtility::Enter
                    instruction = instructionList[++i];

                    instruction.labels.Add(brlabel);
                }
                yield return instruction;
            }
        }

        public static bool EnterMapShipsCatchAll1(Caravan caravan, Map map, CaravanEnterMode enterMode, CaravanDropInventoryMode dropInventoryMode = CaravanDropInventoryMode.DoNotDrop, 
            bool draftColonists = false, Predicate<IntVec3> extraCellValidator = null)
        {
            if(caravan.HasVehicle())
            {
                if(caravan.HasBoat())
                {
                    EnterMapUtilityVehicles.Enter(caravan, map, enterMode, dropInventoryMode, draftColonists, extraCellValidator);
                }
                else
                {
                    if (enterMode == CaravanEnterMode.None)
	                {
		                Log.Error(string.Concat(new object[]
		                {
			                "Caravan ",
			                caravan,
			                " tried to enter map ",
			                map,
			                " with enter mode ",
			                enterMode
		                }), false);
		                enterMode = CaravanEnterMode.Edge;
	                }
	                IntVec3 enterCell = EnterMapUtilityVehicles.GetEnterCellVehicle(caravan, map, enterMode, extraCellValidator);
	                Func<Pawn, IntVec3> spawnCellGetter = (Pawn p) => CellFinder.RandomSpawnCellForPawnNear(enterCell, map, 4);
	                CaravanEnterMapUtility.Enter(caravan, map, spawnCellGetter, dropInventoryMode, draftColonists);
                }
                return false;
            }
            return true;
        }

        public static bool EnterMapShipsCatchAll2(Caravan caravan, Map map, Func<Pawn, IntVec3> spawnCellGetter, CaravanDropInventoryMode dropInventoryMode = CaravanDropInventoryMode.DoNotDrop, bool draftColonists = false)
        {
            if(HelperMethods.HasBoat(caravan))
            {
                EnterMapUtilityVehicles.EnterSpawn(caravan, map, spawnCellGetter, dropInventoryMode, draftColonists);
                return false;
            }
            return true;
        }

        //REDO
        public static bool AllOwnersDownedVehicle(Caravan __instance, ref bool __result)
        {
            if(__instance.PawnsListForReading.Any(x => HelperMethods.IsVehicle(x)))
            {
                foreach (Pawn ship in __instance.pawns)
                {
                    if(HelperMethods.IsVehicle(ship) && (ship?.GetComp<CompVehicle>()?.AllPawnsAboard.All(x => x.Downed) ?? false))
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

        //REDO
        public static bool AllOwnersMentalBreakVehicle(Caravan __instance, ref bool __result)
        {
            if(__instance.PawnsListForReading.Any(x => HelperMethods.IsVehicle(x)))
            {
                foreach(Pawn ship in __instance.pawns)
                {
                    if(HelperMethods.IsVehicle(ship) && (ship?.GetComp<CompVehicle>()?.AllPawnsAboard.All(x => x.InMentalState) ?? false))
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

        

        public static void AnyVehicleBlockingMapRemoval(MapPawns __instance, ref bool __result)
        {
            if(__result is false)
            {
                foreach(Pawn p in __instance.AllPawnsSpawned)
                {
                    if(HelperMethods.IsVehicle(p) && p.GetComp<CompVehicle>().AllPawnsAboard.Any())
                    {
                        foreach (Pawn sailor in p.GetComp<CompVehicle>().AllPawnsAboard)
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

        public static bool NoRestForVehicles(Caravan __instance, ref bool __result)
        {
            if(HelperMethods.HasVehicle(__instance) && !__instance.PawnsListForReading.Any(x => !HelperMethods.IsVehicle(x)))
            {
                __result = false;
                if(__instance.PawnsListForReading.Any(x => x.GetComp<CompVehicle>().navigationCategory == NavigationCategory.Manual))
                {
                    __result = __instance.Spawned && (!__instance.pather.Moving || __instance.pather.nextTile != __instance.pather.Destination || !Caravan_PathFollower.IsValidFinalPushDestination(__instance.pather.Destination) ||
                        Mathf.CeilToInt(__instance.pather.nextTileCostLeft / 1f) > 10000) && CaravanNightRestUtility.RestingNowAt(__instance.Tile);
                }
                else if(__instance.PawnsListForReading.Any(x => x.GetComp<CompVehicle>().navigationCategory == NavigationCategory.Opportunistic))
                {
                    __result = __instance.Spawned && (!__instance.pather.Moving || __instance.pather.nextTile != __instance.pather.Destination || !Caravan_PathFollower.IsValidFinalPushDestination(__instance.pather.Destination) ||
                        Mathf.CeilToInt(__instance.pather.nextTileCostLeft / 1f) > 10000) && CaravanNightRestUtility.RestingNowAt(__instance.Tile);
                }
                return false;
            }
            return true;
        }

        public static IEnumerable<CodeInstruction> CheckDefeatedWithVehiclesTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for(int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.Calls(AccessTools.Property(typeof(MapPawns), nameof(MapPawns.FreeColonists)).GetGetMethod()))
                {
                    yield return new CodeInstruction(opcode: OpCodes.Callvirt, operand: AccessTools.Property(typeof(MapPawns), nameof(MapPawns.AllPawnsSpawned)).GetGetMethod());
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.GrabPawnsFromMapPawnsInVehicle)));
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

                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.GrabPawnsIfVehicles)));
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

                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.GrabPawnsIfVehicles)));
                }

                yield return instruction;
            }
        }
        
        public static void VehicleGearTabPawns(ref List<Pawn> __result)
        {
            if(HelperMethods.HasVehicle(__result) && __result.All(x => HelperMethods.IsVehicle(x)))
            {
                List<Pawn> sailors = new List<Pawn>();
                foreach(Pawn p in __result)
                {
                    sailors.AddRange(p.GetComp<CompVehicle>().AllPawnsAboard);
                }
                __result = sailors;
            }
        }

        public static bool VehicleAllInventoryItems(Caravan caravan, ref List<Thing> __result)
        {
            if(HelperMethods.HasVehicle(caravan) && caravan.PawnsListForReading.All(x => HelperMethods.IsVehicle(x)))
            {
                List<Thing> inventoryItems = new List<Thing>();
                foreach(Pawn p in caravan.PawnsListForReading)
                {
                    foreach(Thing t in p.inventory.innerContainer)
                    {
                        inventoryItems.Add(t);
                    }
                    foreach(Pawn sailor in p.GetComp<CompVehicle>().AllPawnsAboard)
                    {
                        foreach(Thing t in sailor.inventory.innerContainer)
                        {
                            inventoryItems.Add(t);
                        }
                    }
                }
                __result = inventoryItems;
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
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.GrabPawnsFromVehicleCaravanSilentFail)));
                }

                yield return instruction;
            }
        }

        public static void VehicleHealthTabPawns(ref List<Pawn> __result)
        {
            if(HelperMethods.HasVehicle(__result) && __result.All(x => HelperMethods.IsVehicle(x)))
            {
                List<Pawn> sailors = new List<Pawn>();
                foreach (Pawn p in __result)
                {
                    sailors.AddRange(p.GetComp<CompVehicle>().AllPawnsAboard);
                }
                __result = sailors;
            }
        }

        public static void VehicleSocialTabPawns(ref List<Pawn> __result)
        {
            if(HelperMethods.HasVehicle(__result) && __result.Any(x => HelperMethods.IsVehicle(x)))
            {
                List<Pawn> sailors = new List<Pawn>();
                foreach(Pawn p in __result.Where(x => HelperMethods.IsVehicle(x)))
                {
                    sailors.AddRange(p.GetComp<CompVehicle>().AllPawnsAboard.Where(x => x.RaceProps.Humanlike));
                }
                sailors.AddRange(__result.Where(x => x.RaceProps.Humanlike));
                __result = sailors;
            }
        }

        public static bool FindVehicleWithBestStat(Caravan caravan, StatDef stat, ref Pawn __result)
        {
            if(HelperMethods.HasVehicle(caravan) && caravan.PawnsListForReading.All(x => HelperMethods.IsVehicle(x)))
            {
                List<Pawn> pawns = caravan.PawnsListForReading;
                Pawn pawn = null;
                float num = -1f;
                foreach(Pawn s in pawns)
                {
                    foreach(Pawn p in s.GetComp<CompVehicle>().AllPawnsAboard.Where(x => !x.Dead && !x.Downed && !x.InMentalState && caravan.IsOwner(x)))
                    {
                        if(!stat.Worker.IsDisabledFor(p))
                        {
                            float statValue = p.GetStatValue(stat, true);
                            if(pawn is null || statValue > num)
                            {
                                pawn = p;
                                num = statValue;
                            }
                        }
                    }
                }
                __result = pawn;
                return false;
            }
            return true;
        }

        public static void ContainsPawnInVehicle(Pawn p, Caravan __instance, ref bool __result)
        {
            if(__result is false && HelperMethods.HasVehicle(__instance))
            {
                bool flag = false;
                List<Pawn> ships = __instance.PawnsListForReading.Where(x => HelperMethods.IsVehicle(x)).ToList();
                foreach (Pawn ship in ships)
                {
                    if(ship.GetComp<CompVehicle>().AllPawnsAboard.Contains(p))
                    {
                        flag = true;
                        break;
                    }
                }
                __result = flag;
            }
        }

        public static void IsOwnerOfVehicle(Pawn p, Caravan __instance, ref bool __result)
        {
            if(!__result && HelperMethods.HasVehicle(__instance))
            {
                foreach(Pawn s in __instance.PawnsListForReading.Where(x => HelperMethods.IsVehicle(x)))
                {
                    if(s.GetComp<CompVehicle>().AllPawnsAboard.Contains(p) && CaravanUtility.IsOwner(p, __instance.Faction))
                    {
                        __result = true;
                        return;
                    }
                }
            }
        }

        public static void UnloadVehicleOfferGifts(Caravan caravan)
        {
            if(HelperMethods.HasVehicle(caravan))
            {
                HelperMethods.ToggleDocking(caravan, true);
            }
        }

        public static IEnumerable<CodeInstruction> GiveSoldThingToVehicleTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for(int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if(instruction.opcode == OpCodes.Ldnull && instructionList[i+1].opcode == OpCodes.Ldnull)
                {
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_3);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(CaravanUtility), nameof(CaravanUtility.GetCaravan)));
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.GrabPawnsFromVehicleCaravanSilentFail)));
                    instruction = instructionList[++i];
                }

                yield return instruction;
            }
        }

        public static void IsParentCaravanMember(Pawn pawn, ref bool __result)
        {
            if(pawn.ParentHolder is VehicleHandler handler && handler.vehiclePawn != null)
            {
                __result = handler.vehiclePawn.IsCaravanMember();
            }
        }

        public static bool TrySatisfyVehiclePawnsNeeds(Pawn pawn, Caravan_NeedsTracker __instance)
        {
            if(HelperMethods.IsVehicle(pawn))
            {
                if (pawn.needs?.AllNeeds.NullOrEmpty() ?? true)
                    return false;
            }
            return true;
        }

        public static bool ApproxTilesForShips(Caravan caravan, StringBuilder explanation = null)
        {
            //Continue here
            return true;
        }

        #endregion Caravan

        #region Construction
        public static bool CompleteConstructionVehicle(Pawn worker, Frame __instance)
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

                ship.GetComp<CompVehicle>().Rename();
                //Quality?
                //Art?
                //Tale RecordTale LongConstructionProject?
                return false;
            }
            return true;
        }

        public static bool Notify_RepairedVehicle(Building b, ListerBuildingsRepairable __instance)
        {
            if (b.def.HasModExtension<SpawnThingBuilt>() && b.def.GetModExtension<SpawnThingBuilt>()?.thingToSpawn != null)
            {
                if (b.HitPoints < b.MaxHitPoints)
                    return true;

                Pawn ship;
                if(b.TryGetComp<CompSavePawnReference>()?.pawnReference != null)
                {
                    ship = b.GetComp<CompSavePawnReference>().pawnReference;
                    ship.health.Reset();
                }
                else
                {
                    ship = PawnGenerator.GeneratePawn(b.def.GetModExtension<SpawnThingBuilt>().thingToSpawn);
                }
                
                Map map = b.Map;
                IntVec3 position = b.Position;
                Rot4 rotation = b.Rotation;

                AccessTools.Method(typeof(ListerBuildingsRepairable), "UpdateBuilding").Invoke(__instance, new object[] { b });
                if (!(b.def.GetModExtension<SpawnThingBuilt>().soundFinished is null))
                {
                    b.def.GetModExtension<SpawnThingBuilt>().soundFinished.PlayOneShot(new TargetInfo(position, map, false));
                }
                if(ship.Faction != Faction.OfPlayer)
                {
                    ship.SetFaction(Faction.OfPlayer);
                }
                b.Destroy(DestroyMode.Vanish);
                ship.ForceSetStateToUnspawned();
                GenSpawn.Spawn(ship, position, map, rotation, WipeMode.FullRefund, false);
                return false;
            }
            return true;
        }

        public static bool SpawnVehicleGodMode(Thing newThing, IntVec3 loc, Map map, Rot4 rot, Thing __result, WipeMode wipeMode = WipeMode.Vanish, bool respawningAfterLoad = false)
        {
            if(newThing is VehiclePawn vehicle)
            {
                loc = vehicle.ClampToMap(loc, map);
            }
            if(!VehicleMod.mod.settings.debugSpawnBoatBuildingGodMode && Prefs.DevMode && DebugSettings.godMode && newThing.def.HasModExtension<SpawnThingBuilt>())
            {
                Pawn ship = PawnGenerator.GeneratePawn(newThing.def.GetModExtension<SpawnThingBuilt>().thingToSpawn);
                if (!(newThing.def.GetModExtension<SpawnThingBuilt>().soundFinished is null))
                {
                    newThing.def.GetModExtension<SpawnThingBuilt>().soundFinished.PlayOneShot(new TargetInfo(loc, map, false));
                }
                ship.SetFaction(Faction.OfPlayer);
                GenSpawn.Spawn(ship, loc, map, rot, WipeMode.FullRefund, false);
                ship.GetComp<CompVehicle>().Rename();
                __result = ship;
                return false;
            }
            return true;
        }

        #endregion Construction

        #region Extra

        public static void FreeColonistsInVehiclesTransport(ref int __result, List<Pawn> ___pawnsSpawned)
        {
            List<Pawn> ships = ___pawnsSpawned.Where(x => HelperMethods.IsVehicle(x) && x.Faction == Faction.OfPlayer).ToList();
            
            foreach(Pawn ship in ships)
            {
                if(ship.GetComp<CompVehicle>().AllPawnsAboard.Any(x => !x.Dead))
                    __result += ship.GetComp<CompVehicle>().AllPawnsAboard.Count;
            }
        }

        public static bool MultiSelectFloatMenu(List<object> ___selected)
        {
            if(Event.current.type == EventType.MouseDown)
            {
                if(Event.current.button == 1 && ___selected.Count > 0)
                {
                    if(___selected.Count > 1)
                    {
                        return !HelperMethods.MultiSelectClicker(___selected);
                    }
                }
            }
            return true;
        }

        public static void SituationBoardedVehicle(Pawn p, ref WorldPawnSituation __result)
        {
            if(__result == WorldPawnSituation.Free && p.Faction != null && p.Faction == Faction.OfPlayerSilentFail)
            {
                foreach(Map map in Find.Maps)
                {
                    foreach(Pawn ship in map.mapPawns.AllPawnsSpawned.Where(x => HelperMethods.IsVehicle(x) && x.Faction == Faction.OfPlayer))
                    {
                        if(ship.GetComp<CompVehicle>().AllPawnsAboard.Contains(p))
                        {
                            __result = WorldPawnSituation.CaravanMember;
                            return;
                        }
                    }
                }
                foreach(Caravan c in Find.WorldObjects.Caravans)
                {
                    foreach(Pawn ship in c.PawnsListForReading.Where(x => HelperMethods.IsVehicle(x)))
                    {
                        if(ship.GetComp<CompVehicle>().AllPawnsAboard.Contains(p))
                        {
                            __result = WorldPawnSituation.CaravanMember;
                            return;
                        }
                    }
                }
            }
        }

        public static bool VehiclesDontParty(Pawn p, ref bool __result)
        {
            if (HelperMethods.IsVehicle(p))
            {
                __result = false;
                return false;
            }
            return true;
        }

        public static bool DoNotRemoveDockedBoats(Pawn p)
        {
            foreach (DockedBoat obj in Find.WorldObjects.AllWorldObjects.Where(x => x is DockedBoat))
            {
                if (obj.dockedBoats.Contains(p))
                    return false;
            }
            foreach(Caravan c in Find.WorldObjects.AllWorldObjects.Where(x => x is Caravan))
            {
                foreach(Pawn innerPawn in c.PawnsListForReading)
                {
                    if(HelperMethods.IsVehicle(innerPawn) && innerPawn.GetComp<CompVehicle>().AllPawnsAboard.Contains(p))
                    {
                        
                        return false;
                    }
                }
            }
            return true;
        }

        public static void ManhunterDontAttackVehicles(Thing t, ref bool __result)
        {
            if(__result is true && HelperMethods.IsVehicle(t) && t.TryGetComp<CompVehicle>().Props.manhunterTargetsVehicle)
            {
                __result = false;
            }
        }

        #endregion Extra
        
        /// <summary>
        /// Local Variables for tracking and initializing certain mechanics
        /// </summary>
        internal static Dialog_FormVehicleCaravan currentFormingCaravan;
        internal static Dictionary<Map, List<WaterRegion>> terrainChangedCount = new Dictionary<Map, List<WaterRegion>>();

        //REDO (remove)
        internal static bool routePlannerActive;

        /// <summary>
        /// Debugging
        /// </summary>
        internal static List<WorldPath> debugLines = new List<WorldPath>();
        internal static List<Pair<int, int>> tiles = new List<Pair<int,int>>(); // Pair -> TileID : Cycle
        internal static readonly bool debug = false;
        internal static readonly bool drawPaths = false;
    }
}