using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Diagnostics;
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
using Vehicles.Build;
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
            harmony.Patch(original: AccessTools.Method(typeof(MapGenerator), nameof(MapGenerator.GenerateMap)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(GenerateMapExtension)));
            harmony.Patch(original: AccessTools.Method(typeof(Map), nameof(Map.ExposeData)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(ExposeDataMapExtensions)));
            harmony.Patch(original: AccessTools.Method(typeof(Map), nameof(Map.FinalizeInit)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(RecalculateShipPathGrid)));
            harmony.Patch(original: AccessTools.Method(typeof(PathGrid), nameof(PathGrid.RecalculatePerceivedPathCostUnderThing)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(RecalculateShipPathCostUnderThing)));
            harmony.Patch(original: AccessTools.Method(typeof(TerrainGrid), "DoTerrainChangedEffects"), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(RecalculateShipPathCostTerrainChange)));
            /*harmony.Patch(original: AccessTools.Method(typeof(Map), nameof(Map.MapUpdate)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(MapTickUpdateWaterTerrain)));*/

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
                nameof(InfoCardVehiclesTranspiler)));

            /* Rendering */
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_RotationTracker), nameof(Pawn_RotationTracker.UpdateRotation)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(UpdateVehicleRotation)));
            harmony.Patch(original: AccessTools.Method(typeof(PawnRenderer), "RenderPawnInternal", new Type[] { typeof(Vector3), typeof(float), typeof(bool), typeof(Rot4), typeof(Rot4),
                    typeof(RotDrawMode), typeof(bool), typeof(bool), typeof(bool)}), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(RenderPawnRotationTranspiler)));
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
            harmony.Patch(original: AccessTools.Method(typeof(Caravan), nameof(Caravan.GetGizmos)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(AddAnchorGizmo)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.GetGizmos)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(GizmosForVehicleCaravans)));

            /* Pathing */
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.StartPath)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(StartPathShip)));
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_PathFollower), "TryEnterNextPathCell"), 
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(TryEnterNextCellShip)));
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_PathFollower), "GenerateNewPath"),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(GenerateNewShipPath)));
            harmony.Patch(original: AccessTools.Method(typeof(FloatMenuMakerMap), "GotoLocationOption"),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(GotoLocationShips)));

            /* Upgrade Stats */
            harmony.Patch(original: AccessTools.Method(typeof(Pawn), "TicksPerMove"),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehicleMoveSpeedUpgradeModifier)));
            harmony.Patch(original: AccessTools.Method(typeof(MassUtility), nameof(MassUtility.Capacity)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(VehicleCargoCapacity)));

            /* World Pathing */
            harmony.Patch(original: AccessTools.Method(typeof(CaravanUtility), nameof(CaravanUtility.BestGotoDestNear)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(BestGotoDestNearOcean)));
            harmony.Patch(original: AccessTools.Method(typeof(WorldReachabilityUtility), name: nameof(WorldReachability.CanReach)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(CanReachBoats)));
            harmony.Patch(original: AccessTools.Method(typeof(Caravan_PathFollower), "IsPassable"), 
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(IsPassableForBoats)));
            harmony.Patch(original: AccessTools.Method(typeof(WorldPathFinder), name: nameof(WorldPathFinder.FindPath)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(FindOceanPath)));
            harmony.Patch(original: AccessTools.Method(typeof(Caravan_PathFollower), "NeedNewPath"),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(NeedNewPathForBoat)));
            harmony.Patch(original: AccessTools.Method(typeof(Caravan_PathFollower), "SetupMoveIntoNextTile"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(SetupMoveIntoNextOceanTileTranspiler)));
            //Continue here

            harmony.Patch(original: AccessTools.Method(typeof(WorldRoutePlanner), "TryAddWaypoint"), 
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(TryAddWayPointWater)));
            harmony.Patch(original: AccessTools.Method(typeof(Dialog_FormCaravan), nameof(Dialog_FormCaravan.PostOpen)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(PostOpenSetInstance)));
            harmony.Patch(original: AccessTools.Method(typeof(Dialog_FormCaravan), nameof(Dialog_FormCaravan.PostClose)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(PostCloseSetInstance)));

            
            harmony.Patch(original: AccessTools.Method(typeof(CaravanTicksPerMoveUtility), nameof(CaravanTicksPerMoveUtility.GetTicksPerMove), new Type[] { typeof(List<Pawn>), typeof(float), typeof(float), typeof(StringBuilder)}),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(GetTicksPerMoveVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(TilesPerDayCalculator), nameof(TilesPerDayCalculator.ApproxTilesPerDay), new Type[] { typeof(Caravan), typeof(StringBuilder) }),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(ApproxTilesForShips)));
            harmony.Patch(original: AccessTools.Method(typeof(WorldRoutePlanner), nameof(WorldRoutePlanner.WorldRoutePlannerUpdate)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(WorldRoutePlannerActive)));
            
            harmony.Patch(original: AccessTools.Method(typeof(CaravanExitMapUtility), nameof(CaravanExitMapUtility.ExitMapAndCreateCaravan), new Type[] { typeof(IEnumerable<Pawn>), typeof(Faction), typeof(int), typeof(int), typeof(int), typeof(bool) }),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(TryFindClosestSailableTile)));
            
            
            harmony.Patch(original: AccessTools.Method(typeof(WorldPathGrid), nameof(WorldPathGrid.PerceivedMovementDifficultyAt)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(PerceivedMovementDifficultyOnWater)));
            harmony.Patch(original: AccessTools.Method(typeof(WorldPathGrid), nameof(WorldPathGrid.CalculatedMovementDifficultyAt)), 
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(CalculatedMovementDifficultyAtOcean)));
            harmony.Patch(original: AccessTools.Method(typeof(Dialog_FormCaravan), nameof(Dialog_FormCaravan.Notify_ChoseRoute)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(IslandExitTile)));

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
            harmony.Patch(original: AccessTools.Method(typeof(Dialog_FormCaravan), "TryFindExitSpot", new Type[] { typeof(List<Pawn>), typeof(bool), typeof(IntVec3).MakeByRefType() }),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(TryFindExitSpotShips)));
            harmony.Patch(original: AccessTools.Method(typeof(Dialog_FormCaravan), "TryFindRandomPackingSpot"),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(TryFindPackingSpotShips)));
            harmony.Patch(original: AccessTools.Method(typeof(Dialog_FormCaravan), "TryReformCaravan"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(BoardVehiclesWhenReformingCaravanTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(Dialog_FormCaravan), "DoBottomButtons"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(DoBottomButtonsTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(Dialog_FormCaravan), "CheckForErrors"),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(CheckVehicleCrewRequirements)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanUIUtility), nameof(CaravanUIUtility.AddPawnsSections)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(AddVehiclesSection)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.StartFormingCaravan)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(StartFormingCaravanForVehicles)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.RemovePawnFromCaravan)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(RemovePawnAddVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.GetFormAndSendCaravanLord)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(GetVehicleAndSendCaravanLord)));
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
            harmony.Patch(original: AccessTools.Method(typeof(CaravanArrivalAction_Enter), nameof(CaravanArrivalAction_Enter.Arrived)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(ShipsArrivedTranspiler)));
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
            harmony.Patch(original: AccessTools.Method(typeof(Caravan), nameof(Caravan.GetInspectString)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(GetInspectStringVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(Caravan), nameof(Caravan.Notify_MemberDied)), 
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(CaravanLostAllVehicles)));
            harmony.Patch(original: AccessTools.Property(typeof(MapPawns), nameof(MapPawns.AnyPawnBlockingMapRemoval)).GetGetMethod(), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(AnyVehicleBlockingMapRemoval)));
            harmony.Patch(original: AccessTools.Property(typeof(Caravan), nameof(Caravan.NightResting)).GetGetMethod(),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(NoRestForVehicles)));
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
            harmony.Patch(original: AccessTools.Method(typeof(Caravan), nameof(Caravan.ContainsPawn)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(ContainsPawnInVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(Caravan), nameof(Caravan.IsOwner)), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(IsOwnerOfVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanArrivalAction_OfferGifts), nameof(CaravanArrivalAction_OfferGifts.Arrived)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(UnloadVehicleOfferGifts)));
            harmony.Patch(original: AccessTools.Method(typeof(Settlement_TraderTracker), nameof(Settlement_TraderTracker.GiveSoldThingToPlayer)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(ShipHarmony),
                nameof(GiveSoldThingToVehicleTranspiler)));

            /* Draftable */
            harmony.Patch(original: AccessTools.Property(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted)).GetSetMethod(),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(DraftedVehiclesCanMove)));
            harmony.Patch(original: AccessTools.Property(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted)).GetGetMethod(), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(UndraftedVehiclesStopPathing)));
            harmony.Patch(original: AccessTools.Method(typeof(FloatMenuMakerMap), "CanTakeOrder"), prefix: null,
                postfix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(CanVehicleTakeOrder)));
            harmony.Patch(original: AccessTools.Method(typeof(FloatMenuUtility), nameof(FloatMenuUtility.GetMeleeAttackAction)),
                prefix: new HarmonyMethod(typeof(ShipHarmony),
                nameof(NoMeleeForVehicles))); //Change..?
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

            HelperMethods.CannonTargeter = new CannonTargeter();
            HelperMethods.missingIcon = ContentFinder<Texture2D>.Get("Upgrades/missingIcon", true);

            #endregion Functions
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
            MapExtensionUtility.GetExtensionToMap(___map)?.getWaterRegionGrid?.DebugDraw();
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
        /// Recalculate the water based pathgrid upon map initialization
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        public static bool RecalculateShipPathGrid(Map __instance)
        {
            if(__instance is null)
            {
                Log.Error("Recalculating Water PathGrid on null map. This should never happen. - Smash Phil");
                return true;
            }

            MapExtension mapE = MapExtensionUtility.GetExtensionToMap(__instance);
            mapE?.getShipPathGrid?.RecalculateAllPerceivedPathCosts();

            if (mapE != null && mapE.getWaterRegionAndRoomUpdater != null)
            {
                mapE.getWaterRegionAndRoomUpdater.Enabled = true;
            }
            mapE?.getWaterRegionAndRoomUpdater?.RebuildAllWaterRegions();
            return true;
        }

        /// <summary>
        /// Hook on recalculating pathgrid that recalculates the path cost under Things (with additional path costs) for water based pathgrid
        /// </summary>
        /// <param name="t"></param>
        /// <param name="___map"></param>
        public static void RecalculateShipPathCostUnderThing(Thing t, Map ___map)
        {
            if (t is null) return;
            MapExtensionUtility.GetExtensionToMap(___map)?.getShipPathGrid?.RecalculatePerceivedPathCostUnderThing(t);
        }

        /// <summary>
        /// Hook on TerrainChangedEffects which recalculate the path cost when a terrain on a cell is changed.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="___map"></param>
        public static void RecalculateShipPathCostTerrainChange(IntVec3 c, Map ___map)
        {
            MapExtensionUtility.GetExtensionToMap(___map)?.getShipPathGrid?.RecalculatePerceivedPathCostAt(c);
            
        }

        public static void GenerateMapExtension(IntVec3 mapSize, MapParent parent, MapGeneratorDef mapGenerator, ref Map __result,
            IEnumerable<GenStepWithParams> extraGenStepDefs = null, Action<Map> extraInitBeforeContentGen = null)
        {
            MapExtensionUtility.GetExtensionToMap(__result).VerifyComponents();
        }

        public static void ExposeDataMapExtensions()
        {
            MapExtensionUtility.ClearMapExtensions();
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
                    yield return new CodeInstruction(opcode: OpCodes.Newobj, operand: AccessTools.Constructor(typeof(Dialog_InfoCard_Ship), new Type[] { typeof(Thing) }));
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
        public static bool UpdateVehicleRotation(Pawn_RotationTracker __instance)
        {
            if (Traverse.Create(__instance).Field("pawn").GetValue<Pawn>() is VehiclePawn pawn &&
                HelperMethods.IsVehicle(pawn))
            {
                if (pawn.Destroyed || pawn.jobs.HandlingFacing)
                {
                    return false;
                }
                if (pawn.pather.Moving)
                {
                    if (pawn.pather.curPath == null || pawn.pather.curPath.NodesLeftCount < 1)
                    {
                        return false;
                    }
                    HelperMethods.FaceShipAdjacentCell(pawn.pather.nextCell, pawn);
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

            ///Check if the pawn being rendered is a boat
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

                if (!flag && instruction.opcode == OpCodes.Stloc_S && ((LocalBuilder)instruction.operand).LocalIndex == 6)
                {
                    yield return instruction;
                    instruction = instructionList[++i];
                    flag = true;

                    ///Get pawns onboard boat and store inside list to render on colonist bar
                    yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(typeof(ColonistBar), "tmpPawns"));
                    yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(typeof(ColonistBar), "tmpMaps"));
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_1);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.GetVehiclesForColonistBar)));
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(List<Pawn>), nameof(List<Pawn>.AddRange)));
                }
                if ((instruction.Calls(AccessTools.Method(typeof(List<Pawn>), nameof(List<Pawn>.AddRange)))) &&
                    (instructionList[i - 1].Calls(AccessTools.Property(typeof(Caravan), nameof(Caravan.PawnsListForReading)).GetGetMethod())))
                {
                    yield return instruction; //CALLVIRT : AddRange
                    instruction = instructionList[++i];

                    ///Grab pawns from Boat caravan and store inside list to be rendered on colonist bar
                    yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(typeof(ColonistBar), "tmpPawns"));
                    yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(typeof(ColonistBar), "tmpCaravans"));
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, 10);
                    yield return new CodeInstruction(opcode: OpCodes.Callvirt, operand: AccessTools.Property(typeof(List<Caravan>), "Item").GetGetMethod());
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.ExtractPawnsFromCaravan)));

                    yield return new CodeInstruction(opcode: OpCodes.Callvirt, operand: AccessTools.Method(typeof(List<Pawn>), nameof(List<Pawn>.AddRange)));
                }

                yield return instruction;
            }
        }

        //WIP
        /// <summary>
        /// Render small boat icon on colonist bar picture rect if they are currently onboard a boat
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="colonist"></param>
        public static void DrawIconsVehicles(Rect rect, Pawn colonist)
        {
            if(colonist.Dead)
            {
                return;
            }
            float num = 20f * Find.ColonistBar.Scale;
            Vector2 vector = new Vector2(rect.x + 1f, rect.yMax - num - 1f);

            List<Pawn> ships = Find.CurrentMap?.mapPawns?.AllPawnsSpawned?.FindAll(x => HelperMethods.IsVehicle(x));
            Pawn p = ships?.Find(x => x.GetComp<CompVehicle>().AllPawnsAboard.Contains(colonist));
            if(p != null)
            {
                Rect rect2 = new Rect(vector.x, vector.y, num, num);
                GUI.DrawTexture(rect2, TexCommandVehicles.CachedTextureIcons[p.def]);
                TooltipHandler.TipRegion(rect2, "ActivityIconOnBoardShip".Translate(p.Label)); //REDO
                vector.x += num;
            }
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
                float angle = RimShipMod.mod.settings.debugDisableSmoothPathing ? (thing as VehiclePawn).GetComp<CompVehicle>().Angle : (thing as VehiclePawn).GetComp<CompVehicle>().BearingAngle;

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
                else if(RimShipMod.mod.settings.passiveWaterWaves)
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
            if(HelperMethods.IsVehicle(__instance?.pawn))
            {
                if(RimShipMod.mod.settings.debugDraftAnyShip)
                    return true;
                if (RimShipMod.mod.settings.debugDisableWaterPathing && __instance.pawn.GetComp<CompVehicle>().beached)
                    __instance.pawn.GetComp<CompVehicle>().RemoveBeachedStatus();
                if (value && !__instance.Drafted)
                {
                    if(__instance.pawn.TryGetComp<CompFueledTravel>() != null && __instance.pawn.GetComp<CompFueledTravel>().EmptyTank)
                    {
                        Messages.Message("CompShips_OutOfFuel".Translate(), MessageTypeDefOf.RejectInput);
                        return false;
                    }
                    if(!__instance.pawn.GetComp<CompVehicle>().ResolveSeating())
                    {
                        Messages.Message("CompShips_CannotMove".Translate(), MessageTypeDefOf.RejectInput);
                        return false;
                    }
                }
                if(!RimShipMod.mod.settings.fishingPersists) __instance.pawn.GetComp<CompVehicle>().currentlyFishing = false;
            }
            return true;
        }

        /// <summary>
        /// Terminate pathing if boat suddenly becomes undrafted
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="__result"></param>
        public static void UndraftedVehiclesStopPathing(Pawn_DraftController __instance, bool __result)
        {
            if(HelperMethods.IsVehicle(__instance?.pawn))
            {
                if(__result is false && __instance?.pawn?.pather?.curPath != null)
                {
                    if(debug) Log.Message("Pawn_PathFollower is null: " + (__instance.pawn?.pather is null) + " | PawnPath is null: " + (__instance.pawn?.pather?.curPath is null));
                    HelperMethods.PatherFailedHelper(ref __instance.pawn.pather, __instance.pawn);
                }
            }
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
        /// Add gizmo for docking Boat
        /// </summary>
        /// <note>Needs rework and a change to a Passthrough Postfix, rather than allocating new lists for each change.</note>
        /// <param name="__result"></param>
        /// <param name="__instance"></param>
        public static void AddAnchorGizmo(ref IEnumerable<Gizmo> __result, Caravan __instance)
        {
            if(HelperMethods.HasBoat(__instance) && (Find.World.CoastDirectionAt(__instance.Tile).IsValid || HelperMethods.RiverIsValid(__instance.Tile, __instance.PawnsListForReading.Where(x => HelperMethods.IsBoat(x)).ToList())))
            {
                if(!__instance.pather.Moving && !__instance.PawnsListForReading.Any(x => !HelperMethods.IsBoat(x)))
                {
                    Command_Action gizmo = new Command_Action();
                    gizmo.icon = TexCommandVehicles.Anchor;
                    gizmo.defaultLabel = Find.WorldObjects.AnySettlementBaseAt(__instance.Tile) ? "CommandDockShip".Translate() : "CommandDockShipDisembark".Translate();
                    gizmo.defaultDesc = Find.WorldObjects.AnySettlementBaseAt(__instance.Tile) ? "CommandDockShipDesc".Translate(Find.WorldObjects.SettlementBaseAt(__instance.Tile)) : "CommandDockShipObjectDesc".Translate();
                    gizmo.action = delegate ()
                    {
                        List<WorldObject> objects = Find.WorldObjects.ObjectsAt(__instance.Tile).ToList();
                        if(!objects.All(x => x is Caravan))
                            HelperMethods.ToggleDocking(__instance, true);
                        else
                            HelperMethods.SpawnDockedBoatObject(__instance);
                    };

                    List<Gizmo> gizmos = __result.ToList();
                    gizmos.Add(gizmo);
                    __result = gizmos;
                }
                else if (!__instance.pather.Moving && __instance.PawnsListForReading.Any(x => !HelperMethods.IsBoat(x)))
                {
                    Command_Action gizmo = new Command_Action();
                    gizmo.icon = TexCommandVehicles.UnloadAll;
                    gizmo.defaultLabel = "CommandUndockShip".Translate();
                    gizmo.defaultDesc = "CommandUndockShipDesc".Translate(__instance.Label);
                    gizmo.action = delegate ()
                    {
                        HelperMethods.ToggleDocking(__instance, false);
                    };

                    List<Gizmo> gizmos = __result.ToList();
                    gizmos.Add(gizmo);
                    __result = gizmos;
                }
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
                    if(lord.faction == Faction.OfPlayer && lord.LordJob is LordJob_FormAndSendCaravanShip && !(lord.CurLordToil is LordToil_PrepareCaravan_LeaveShip) && !(lord.CurLordToil is LordToil_PrepareCaravan_BoardShip))
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
                                if(lord.faction == Faction.OfPlayer && lord.LordJob is LordJob_FormAndSendCaravanShip)
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
        public static bool StartPathShip(LocalTargetInfo dest, PathEndMode peMode, Pawn_PathFollower __instance, ref Pawn ___pawn, ref PathEndMode ___peMode,
            ref LocalTargetInfo ___destination, ref bool ___moving)
        {
            if (RimShipMod.mod.settings.debugDisableWaterPathing)
                return true;
            if(HelperMethods.IsBoat(___pawn))
            {
                dest = (LocalTargetInfo)GenPathShip.ResolvePathMode(___pawn, dest.ToTargetInfo(___pawn.Map), ref peMode, MapExtensionUtility.GetExtensionToMap(___pawn.Map));
                if (dest.HasThing && dest.ThingDestroyed)
                {
                    Log.Error(___pawn + " pathing to destroyed thing " + dest.Thing, false);
                    HelperMethods.PatherFailedHelper(ref __instance, ___pawn);
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
                    HelperMethods.PatherFailedHelper(ref __instance, ___pawn);
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
                    HelperMethods.PatherArrivedHelper(__instance, ___pawn);
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

        public static bool GenerateNewShipPath(ref PawnPath __result, ref Pawn_PathFollower __instance, ref Pawn ___pawn, ref PathEndMode ___peMode)
        {
            if(HelperMethods.IsBoat(___pawn) && !RimShipMod.mod.settings.debugDisableWaterPathing)
            {
                __instance.lastPathedTargetPosition = __instance.Destination.Cell;
                __result = MapExtensionUtility.GetExtensionToMap(___pawn.Map).getShipPathFinder.FindShipPath(___pawn.Position, __instance.Destination, ___pawn, ___peMode);
                if (!__result.Found) Log.Warning("Path Not Found");
                return false;
            }
            return true;
        }

        public static bool TryEnterNextCellShip(Pawn_PathFollower __instance, ref Pawn ___pawn, ref IntVec3 ___lastCell, ref LocalTargetInfo ___destination,
            ref PathEndMode ___peMode)
        {
            if(HelperMethods.IsVehicle(___pawn) && SPMultiCell.ClampHitboxToMap(___pawn, __instance.nextCell, ___pawn.Map))
            {
                ___pawn.jobs.curDriver.Notify_PatherFailed();
                __instance.StopDead();
                return false;
            }

            if(RimShipMod.mod.settings.debugDisableWaterPathing)
            {
                if(HelperMethods.IsBoat(___pawn) && ___pawn.GetComp<CompVehicle>().beached)
                    ___pawn.GetComp<CompVehicle>().RemoveBeachedStatus();
                return true;
            }
            if (HelperMethods.IsBoat(___pawn))
            {
                if(!___pawn.Drafted)
                {
                    if(___pawn.CurJob is null)
                    {
                        JobUtility.TryStartErrorRecoverJob(___pawn, string.Empty);
                    }
                    __instance?.StopDead();
                }

                if (___pawn.GetComp<CompVehicle>().beached || !__instance.nextCell.GetTerrain(___pawn.Map).IsWater)
                {
                    ___pawn.GetComp<CompVehicle>().BeachShip();
                    ___pawn.Position = __instance.nextCell;
                    __instance.StopDead();
                    ___pawn.jobs.curDriver.Notify_PatherFailed();
                }

                //Buildings?
                ___lastCell = ___pawn.Position;
                ___pawn.Position = __instance.nextCell;
                //Clamor?
                //More Buildings?
                
                if(HelperMethods.NeedNewPath(___destination, __instance.curPath, ___pawn, ___peMode, __instance.lastPathedTargetPosition) && !HelperMethods.TrySetNewPath(ref __instance, ref __instance.lastPathedTargetPosition, 
                    ___destination, ___pawn, ___pawn.Map, ref ___peMode))
                {
                    return false;
                }
                if(ShipReachabilityImmediate.CanReachImmediateShip(___pawn, ___destination, ___peMode))
                {
                    HelperMethods.PatherArrivedHelper(__instance, ___pawn);
                }
                else
                {
                    HelperMethods.SetupMoveIntoNextCell(ref __instance, ___pawn, ___destination);
                }
                return false;
            }
            return true;
        }

        public static bool GotoLocationShips(IntVec3 clickCell, Pawn pawn, ref FloatMenuOption __result)
        {
            if(HelperMethods.IsBoat(pawn))
            {
                if (DebugSettings.godMode)
                {
                    Log.Message("-> " + clickCell + " | " + pawn.Map.terrainGrid.TerrainAt(clickCell).LabelCap + " | " + MapExtensionUtility.GetExtensionToMap(pawn.Map).getShipPathGrid.CalculatedCostAt(clickCell) +
                        " - " + MapExtensionUtility.GetExtensionToMap(pawn.Map).getShipPathGrid.pathGrid[pawn.Map.cellIndices.CellToIndex(clickCell)]);
                }


                if(!RimShipMod.mod.settings.debugDisableSmoothPathing && pawn.GetComp<CompVehicle>().Props.diagonalRotation)
                {
                    if(!(pawn as VehiclePawn).InitiateSmoothPath(clickCell))
                    {
                        Log.Error($"Failed Smooth Pathing. Cell: {clickCell} Pawn: {pawn.LabelShort}");
                    }
                    return false;
                }
                if(RimShipMod.mod.settings.debugDisableWaterPathing)
                    return true;
                
                int num = GenRadial.NumCellsInRadius(2.9f);
                int i = 0;
                IntVec3 curLoc;
                while(i < num)
                {
                    curLoc = GenRadial.RadialPattern[i] + clickCell;
                    if (GenGridShips.Standable(curLoc, pawn.Map, MapExtensionUtility.GetExtensionToMap(pawn.Map)))
                    {
                        if (curLoc == pawn.Position || pawn.GetComp<CompVehicle>().beached)
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

        public static bool TryAddWayPointWater(int tile, Dialog_FormCaravan ___currentFormCaravanDialog, WorldRoutePlanner __instance, bool playSound = true)
        {
            /*Log.Message("======================================");
            Log.Message("TryAddWaypoint");*/
            if(__instance.FormingCaravan)
            {
                List<Pawn> pawnsOnCaravan = TransferableUtility.GetPawnsFromTransferables(currentFormingCaravan.transferables);
                /*Log.Message("Forming Caravan w/ River Travel:" + (RiverIsValid(tile, pawnsOnCaravan)));*/
                if (Find.WorldGrid[tile].biome == BiomeDefOf.Ocean || Find.WorldGrid[tile].biome == BiomeDefOf.Lake || (Find.World.CoastDirectionAt(tile).IsValid && HelperMethods.HasBoat(pawnsOnCaravan)) ||
                    (HelperMethods.RiverIsValid(tile, pawnsOnCaravan) && !Find.World.Impassable(tile)))
                {
                    if(!HelperMethods.HasBoat(pawnsOnCaravan))
                    {
                        Messages.Message("MessageCantAddWaypointBecauseNoShip".Translate(Find.WorldGrid[tile].biome.defName), MessageTypeDefOf.RejectInput, false);
                        return false;
                    }
                    RoutePlannerWaypoint routePlannerWaypoint = (RoutePlannerWaypoint)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.RoutePlannerWaypoint);
                    routePlannerWaypoint.Tile = tile;
                    Find.WorldObjects.Add(routePlannerWaypoint);
                    __instance.waypoints.Add(routePlannerWaypoint);
                    AccessTools.Method(typeof(WorldRoutePlanner), "RecreatePaths").Invoke(__instance, null);
                    if(playSound)
                        SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    return false;
                }
                else
                {
                    if(HelperMethods.HasBoat(pawnsOnCaravan))
                    {
                        if(RimShipMod.mod.settings.riverTravel && (!Find.WorldGrid[tile]?.Rivers.NullOrEmpty() ?? false) )
                        {
                            Messages.Message("MessageCantAddWaypointBecauseRiverUnreachable".Translate(), MessageTypeDefOf.RejectInput, false);
                            return false;
                        }
                        else
                        {
                            Messages.Message("MessageCantAddWaypointBecauseShip".Translate(), MessageTypeDefOf.RejectInput, false);
                            return false;
                        }
                    }
                }
                if(__instance.waypoints.Any<RoutePlannerWaypoint>() && !Find.WorldReachability.CanReach(__instance.waypoints[__instance.waypoints.Count - 1].Tile, tile))
                {
                    Messages.Message("MessageCantAddWaypointBecauseUnreachable".Translate(), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
            }
            else if(routePlannerActive)
            {
                /*Log.Message("Route Planner");*/
                if(__instance.waypoints.Any<RoutePlannerWaypoint>() && !Find.WorldReachability.CanReach(__instance.waypoints[__instance.waypoints.Count - 1].Tile, tile))
                {
                    Messages.Message("MessageCantAddWaypointBecauseUnreachable".Translate(), MessageTypeDefOf.RejectInput, false);
                    return false;
                }

                RoutePlannerWaypoint routePlannerWaypoint = (RoutePlannerWaypoint)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.RoutePlannerWaypoint);
                routePlannerWaypoint.Tile = tile;
                Find.WorldObjects.Add(routePlannerWaypoint);
                __instance.waypoints.Add(routePlannerWaypoint);
                AccessTools.Method(typeof(WorldRoutePlanner), "RecreatePaths").Invoke(__instance, null);
                if (playSound)
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                return false;
            }
            return true;
        }

        public static bool CanReachBoats(Caravan c, int tile, ref bool __result)
        {
            if(HelperMethods.HasBoat(c))
            {
                __result = (Find.World.components.Find(x => x is WorldOceanReachability) as WorldOceanReachability).CanReach(c, tile);
                return false;
            }
            return true;
        }
        
        //REDO or double check
        public static bool GetTicksPerMoveVehicle(List<Pawn> pawns, float massUsage, float massCapacity, ref int __result, StringBuilder explanation = null)
        {
            if(pawns.Any() && HelperMethods.HasVehicle(pawns))
            {
                //Caravan Const Values
                const int MaxShipPawnTicksPerMove = 150;
                const float CellToTilesConversionRatio = 340f;
                const float MoveSpeedFactorAtLowMass = 2f;

                if (!(explanation is null))
                {
                    explanation.Append("CaravanMovementSpeedFull".Translate() + ":");
                    float num = 0f;
                    Pawn slowestVehicle = pawns.Where(x => HelperMethods.IsVehicle(x)).MinBy(x => x.def.statBases.Find(y => y.stat == StatDefOf.MoveSpeed).value);
                    num = Mathf.Min((float)slowestVehicle.TicksPerMoveCardinal, MaxShipPawnTicksPerMove) * CellToTilesConversionRatio;
                    float num2 = 60000f / num;
                    float moveSpeedMultiplier = 1f;
                    switch(slowestVehicle.GetComp<CompVehicle>().Props.vehiclePowerType)
                    {
                        case PowerType.Manual:
                            moveSpeedMultiplier = 0.8f;
                            break;
                        case PowerType.WindPowered:
                            moveSpeedMultiplier = 1.15f;
                            break;
                        case PowerType.Steam:
                            moveSpeedMultiplier = 1.1f;
                            break;
                        case PowerType.Fuel:
                            moveSpeedMultiplier = 1.25f;
                            break;
                        case PowerType.Nuclear:
                            moveSpeedMultiplier = 1.4f;
                            break;
                    }
                    
                    if(explanation != null)
                    {
                        explanation.AppendLine();
                        explanation.Append(string.Concat(new string[]
                        {
                        "Slowest Vehicle: ",
                        slowestVehicle.LabelShortCap,
                        "\nVehicle Type: ",
                        slowestVehicle.GetComp<CompVehicle>().Props.vehiclePowerType.ToString(),
                        " (",
                        moveSpeedMultiplier.ToString("0.#"),
                        "x Speed)\n", "BaseSpeed".Translate(), ": ",
                        num2.ToString("0.#"),
                        " ",
                        "TilesPerDay".Translate()
                        }));
                    }
                    num += num2;
                    float moveSpeedFactorFromMass = massCapacity <= 0f ? 1f : Mathf.Lerp(MoveSpeedFactorAtLowMass, 1f, massUsage / massCapacity);
                    
                    if(explanation != null)
                    {
                        explanation.AppendLine();
                        explanation.Append("MultiplierForCarriedMass".Translate(moveSpeedFactorFromMass.ToStringPercent()));
                    }
                    int num4 = Mathf.Max(Mathf.RoundToInt(num / (moveSpeedFactorFromMass * moveSpeedMultiplier)), 1);
                    if(explanation != null)
                    {
                        float num5 = 60000f / (float)num4;
                        explanation.AppendLine();
                        explanation.Append(string.Concat(new string[]
                        {
                            "FinalCaravanPawnsMovementSpeed".Translate(),
                            ": ",
                            num5.ToString("0.#"),
                            " ",
                            "TilesPerDay".Translate(),
                        }));
                    }
                    __result = num4;
                    return false;
                }
            }
            return true;
        }

        public static void WorldRoutePlannerActive(bool ___active)
        {
            if(routePlannerActive != ___active)
                routePlannerActive = ___active;
        }

        public static bool BestGotoDestNearOcean(int tile, Caravan c, ref int __result)
        {
            if(HelperMethods.HasBoat(c))
            {
                Predicate<int> predicate = (int t) => !HelperMethods.BoatCantTraverse(t) && (Find.World.components.Find(x => x is WorldOceanReachability) as WorldOceanReachability).CanReach(c, tile);
                if(predicate(tile))
                {
                    __result = tile;
                    return false;
                }
                GenWorldClosest.TryFindClosestTile(tile, predicate, out int result, 50, true);
                __result = result;
                return false;
            }
            return true;
        }

        public static bool TryFindClosestSailableTile(IEnumerable<Pawn> pawns, Faction faction, int exitFromTile, int directionTile, int destinationTile, ref Caravan __result, bool sendMessage = true)
        {
            if(HelperMethods.HasBoat(pawns))
            {
                //Log.Message("Has Ship");
            }
            return true;
        }

        public static IEnumerable<CodeInstruction> SetupMoveIntoNextOceanTileTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for(int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if(instruction.LoadsField(AccessTools.Field(typeof(Caravan_PathFollower), nameof(Caravan_PathFollower.previousTileForDrawingIfInDoubt))))
                {
                    yield return instruction;
                    instruction = instructionList[++i];

                    Label label = ilg.DefineLabel();
                    Label brlabel = ilg.DefineLabel();

                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(typeof(Caravan_PathFollower), "caravan"));
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.HasBoat), new Type[] { typeof(Caravan) }));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(typeof(Caravan_PathFollower), nameof(Caravan_PathFollower.nextTile)));
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(typeof(Caravan_PathFollower), "caravan"));
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Property(typeof(Caravan), nameof(Caravan.PawnsListForReading)).GetGetMethod());
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.IsNotWaterTile)));
                    yield return new CodeInstruction(opcode: OpCodes.Br, brlabel);

                    instruction.labels.Add(label);

                    int j = i;
                    CodeInstruction tmpInstruction;
                    while(j < instructionList.Count)
                    {
                        tmpInstruction = instructionList[j];
                        if(tmpInstruction.opcode == OpCodes.Brfalse)
                        {
                            tmpInstruction.labels.Add(brlabel);
                            break;
                        }
                        j++;
                    }
                }

                yield return instruction;
            }
        }

        public static bool IsPassableForBoats(int tile, Caravan ___caravan, ref bool __result)
        {
            if(HelperMethods.HasBoat(___caravan))
            {
                __result = ___caravan is null ? !HelperMethods.BoatCantTraverse(tile) : HelperMethods.IsWaterTile(tile, ___caravan.PawnsListForReading.Where(x => HelperMethods.IsBoat(x)).ToList());
                return false;
            }
            return true;
        }

        public static bool FindOceanPath(int startTile, int destTile, Caravan caravan, ref WorldPath __result, Func<float, bool> terminator = null)
        {
            if(caravan != null) //Add cached caravan information
            {
                __result = (Find.World.components.Find(x => x is WorldOceanPathFinder) as WorldOceanPathFinder).FindOceanPath(startTile, destTile, caravan, terminator);
                return false;
            }
            return true;
        }

        public static bool NeedNewPathForBoat(Caravan ___caravan, bool ___moving, ref bool __result, Caravan_PathFollower __instance)
        {
            if(HelperMethods.HasBoat(___caravan))
            {
                if(!___moving)
                {
                    __result = false;
                    return false;
                }
                if(__instance.curPath is null || !__instance.curPath.Found || __instance.curPath.NodesLeftCount == 0)
                {
                    __result = true;
                    return false;
                }
                for(int num = 0; num < 20 && num < __instance.curPath.NodesLeftCount; num++)
                {
                    int tile = __instance.curPath.Peek(num);
                    if(HelperMethods.BoatCantTraverse(tile))
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

        public static void PerceivedMovementDifficultyOnWater(int tile, ref float __result)
        {
            if(Find.WorldGrid[tile].biome == BiomeDefOf.Ocean || Find.WorldGrid[tile].biome == BiomeDefOf.Lake || HelperMethods.WaterCovered(tile))
                __result = 0.5f;
        }

        public static bool CalculatedMovementDifficultyAtOcean(int tile, bool perceivedStatic, ref float __result, int? ticksAbs = null, StringBuilder explanation = null)
        {
            if( (Find.WorldGrid[tile].biome == BiomeDefOf.Ocean || Find.WorldGrid[tile].biome == BiomeDefOf.Lake) && (!perceivedStatic || ticksAbs is null))
            {
                if(explanation != null && explanation.Length > 0)
                    explanation.AppendLine();
                __result = 0.5f;
                if(explanation != null)
                    explanation.AppendLine("OceanPassable".Translate());
                return false;
            }
            return true;
        }

        public static void IslandExitTile(int destinationTile, Dialog_FormCaravan __instance, bool ___reform, ref int ___startingTile, Map ___map)
        {
            if(currentFormingCaravan != null && (!___reform && ___startingTile < 0))
            {
                List<Pawn> pawns = TransferableUtility.GetPawnsFromTransferables(currentFormingCaravan.transferables);
                if(HelperMethods.HasBoat(pawns))
                {
                    List<int> neighboringCells = new List<int>();
                    Find.WorldGrid.GetTileNeighbors(___map.Tile, neighboringCells);
                    foreach(int neighbor in neighboringCells)
                    {
                        if(HelperMethods.WaterCovered(neighbor))
                        {
                            ___startingTile = neighbor;
                            return;
                        }
                    }
                }
            }
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
        public static void AddVehiclesSection(TransferableOneWayWidget widget, List<TransferableOneWay> transferables)
        {
            IEnumerable<TransferableOneWay> source = from x in transferables
                                                     where x.ThingDef.category == ThingCategory.Pawn
                                                     select x;
            widget.AddSection("ShipSection".Translate(), from x in source
                                                         where !(((Pawn)x.AnyThing).GetComp<CompVehicle>() is null) && !((Pawn)x.AnyThing).OnDeepWater()
                                                         select x);
        }

        public static bool StartFormingCaravanForVehicles(List<Pawn> pawns, List<Pawn> downedPawns, Faction faction, List<TransferableOneWay> transferables,
            IntVec3 meetingPoint, IntVec3 exitSpot, int startingTile, int destinationTile)
        {
            if (pawns.Any((Pawn x) => HelperMethods.IsVehicle(x) && (x.GetComp<CompVehicle>().movementStatus is VehicleMovementStatus.Online)))
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

                List<TransferableOneWay> list = transferables;
                list.RemoveAll((TransferableOneWay x) => x.CountToTransfer <= 0 || !x.HasAnyThing || x.AnyThing is Pawn);

                foreach (Pawn p in pawns)
                {
                    Lord lord = p.GetLord();
                    if (!(lord is null))
                    {
                        lord.Notify_PawnLost(p, PawnLostCondition.ForcedToJoinOtherLord, null);
                    }
                }
                List<Pawn> ships = pawns.Where(x => HelperMethods.IsVehicle(x)).ToList();
                List<Pawn> capablePawns = pawns.Where(x => !HelperMethods.IsVehicle(x) && x.IsColonist && !x.Downed && !x.Dead).ToList();
                List<Pawn> prisoners = pawns.Where(x => !HelperMethods.IsVehicle(x) && !x.IsColonist && !x.RaceProps.Animal).ToList();
                int seats = 0;
                foreach (Pawn ship in ships)
                {
                    seats += ship.GetComp<CompVehicle>().SeatsAvailable;
                }
                if ((pawns.Where(x => !HelperMethods.IsVehicle(x)).ToList().Count + downedPawns.Count) > seats)
                {
                    Log.Error("Can't start forming caravan with vehicles(s) selected and not enough seats to house all pawns. Seats: " + seats + " Pawns boarding: " +
                        (pawns.Where(x => !HelperMethods.IsVehicle(x)).ToList().Count + downedPawns.Count), false);
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

        public static IEnumerable<CodeInstruction> DoBottomButtonsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            Label breakLabel = ilg.DefineLabel();

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Ldloc_S && ((LocalBuilder)instruction.operand).LocalIndex == 19)
                {
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, operand: 19);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.CanStartCaravan)));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, operand: breakLabel);
                }
                else if (instruction.LoadsField(AccessTools.Field(typeof(Dialog_FormCaravan), "destinationTile")))
                {
                    instructionList[i - 1].labels.Add(breakLabel);
                }
                yield return instruction;
            }
        }

        public static bool CheckVehicleCrewRequirements(List<Pawn> pawns, ref bool __result)
        {
            if(HelperMethods.HasVehicle(pawns))
            {
                __result = HelperMethods.CanStartCaravan(pawns);
                return __result;
            }
            return true;
        }

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

        public static bool RemovePawnAddVehicle(Pawn pawn, Lord lord, bool removeFromDowned = true)
        {
            if (lord.ownedPawns.Any(x => HelperMethods.IsVehicle(x)))
            {
                bool flag = false;
                bool flag2 = false;
                string text = "";
                string textShip = "";
                List<Pawn> ownedShips = lord.ownedPawns.FindAll(x => !(x.GetComp<CompVehicle>() is null));
                foreach (Pawn ship in ownedShips)
                {
                    if (ship.GetComp<CompVehicle>().AllPawnsAboard.Contains(pawn))
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
                text += flag ? "MessagePawnLostWhileFormingCaravan".Translate(pawn).CapitalizeFirst().ToString() : flag2 ? textShip :
                    ("MessagePawnLostWhileFormingCaravan".Translate(pawn).CapitalizeFirst().ToString() + "MessagePawnLostWhileFormingCaravan_AllLost".Translate().ToString());
                bool flag3 = true;
                if (!flag2 && !flag)
                    CaravanFormingUtility.StopFormingCaravan(lord);
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

        public static void GetVehicleAndSendCaravanLord(Pawn p, ref Lord __result)
        {
            if (__result is null)
            {
                if (HelperMethods.IsFormingCaravanShipHelper(p))
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

        public static bool IsFormingCaravanVehicle(Pawn p, ref bool __result)
        {
            Lord lord = p.GetLord();
            if (!(lord is null) && (lord.LordJob is LordJob_FormAndSendCaravanShip))
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
            if (__instance.job.lord.LordJob is LordJob_FormAndSendCaravanShip)
            {
                __result = ((LordJob_FormAndSendCaravanShip)__instance.job.lord.LordJob).transferables;
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
                    yield return new CodeInstruction(opcode: OpCodes.Isinst, operand: typeof(LordJob_FormAndSendCaravanShip));
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
                    yield return new CodeInstruction(opcode: OpCodes.Castclass, operand: typeof(LordJob_FormAndSendCaravanShip));
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
            if(__instance.lord.LordJob is LordJob_FormAndSendCaravanShip)
            {
                List<Pawn> ships = __instance.lord.ownedPawns.Where(x => !(x.GetComp<CompVehicle>() is null)).ToList();
                foreach(Pawn ship in ships)
                {
                    ship.mindState.duty = new PawnDuty(DutyDefOf_Ships.PrepareCaravan_WaitShip);
                }
            }
        }

        public static bool TryFindExitSpotShips(List<Pawn> pawns, bool reachableForEveryColonist, out IntVec3 spot, Map ___map, int ___startingTile, ref bool __result)
        {
            if(pawns.Any(x => HelperMethods.IsBoat(x)))
            {
                //Rot4 rotFromTo = Find.WorldGrid.GetRotFromTo(__instance.CurrentTile, ___startingTile); WHEN WORLD GRID IS ESTABLISHED
                Rot4 rotFromTo;
                if(Find.World.CoastDirectionAt(___map.Tile).IsValid)
                {
                    rotFromTo = Find.World.CoastDirectionAt(___map.Tile);
                }
                else if(!Find.WorldGrid[___map.Tile]?.Rivers?.NullOrEmpty() ?? false)
                {
                    List<Tile.RiverLink> rivers = Find.WorldGrid[___map.Tile].Rivers;
                    Tile.RiverLink river = HelperMethods.BiggestRiverOnTile(Find.WorldGrid[___map.Tile].Rivers);

                    float angle = Find.WorldGrid.GetHeadingFromTo(___map.Tile, (from r1 in rivers
                                                                                orderby -r1.river.degradeThreshold
                                                                                select r1).First<Tile.RiverLink>().neighbor);
                    if (angle < 45)
                    {
                        rotFromTo = Rot4.South;
                    }
                    else if (angle < 135)
                    {
                        rotFromTo = Rot4.East;
                    }
                    else if (angle < 225)
                    {
                        rotFromTo = Rot4.North;
                    }
                    else if (angle < 315)
                    {
                        rotFromTo = Rot4.West;
                    }
                    else
                    {
                        rotFromTo = Rot4.South;
                    }
                }
                else
                {
                    Log.Warning("No Coastline or River detected on map: " + ___map.uniqueID + ". Selecting edge of map with most water cells.");
                    int n = CellRect.WholeMap(___map).GetEdgeCells(Rot4.North).Where(x => GenGridShips.Standable(x, ___map, MapExtensionUtility.GetExtensionToMap(___map))).Count();
                    int e = CellRect.WholeMap(___map).GetEdgeCells(Rot4.East).Where(x => GenGridShips.Standable(x, ___map, MapExtensionUtility.GetExtensionToMap(___map))).Count();
                    int s = CellRect.WholeMap(___map).GetEdgeCells(Rot4.South).Where(x => GenGridShips.Standable(x, ___map, MapExtensionUtility.GetExtensionToMap(___map))).Count();
                    int w = CellRect.WholeMap(___map).GetEdgeCells(Rot4.West).Where(x => GenGridShips.Standable(x, ___map, MapExtensionUtility.GetExtensionToMap(___map))).Count();
                    rotFromTo = SPExtra.Max4IntToRot(n, e, s, w);
                }
                __result = TryFindExitSpotOnWater(pawns, reachableForEveryColonist, rotFromTo, out spot, ___startingTile, ___map) || TryFindExitSpotOnWater(pawns, reachableForEveryColonist, rotFromTo.Rotated(RotationDirection.Clockwise),
                    out spot, ___startingTile, ___map) || TryFindExitSpotOnWater(pawns, reachableForEveryColonist, rotFromTo.Rotated(RotationDirection.Counterclockwise), out spot, ___startingTile, ___map) ||
                    TryFindExitSpotOnWater(pawns, reachableForEveryColonist, rotFromTo.Opposite, out spot, ___startingTile, ___map); 
                
                Pawn pawn = pawns.FindAll(x => HelperMethods.IsBoat(x)).MaxBy(x => x.def.size.z);
                SPMultiCell.ClampToMap(pawn, ref spot, ___map);
                return false;
            }
            spot = IntVec3.Invalid;
            return true;
        }
        public static bool TryFindExitSpotOnWater(List<Pawn> pawns, bool reachableForEveryColonist, Rot4 exitDirection, out IntVec3 spot, int startingTile, Map map)
        {
            if(startingTile < 0)
            {
                Log.Error("Can't find exit spot because startingTile is not set.", false);
                spot = IntVec3.Invalid;
                return false;
            }
            Pawn leadShip = pawns.Where(x => HelperMethods.IsBoat(x)).MaxBy(y => y.def.size.z);
            bool validator(IntVec3 x) => !x.Fogged(map) && GenGridShips.Standable(x, map, MapExtensionUtility.GetExtensionToMap(map));
            List<IntVec3> cells = CellRect.WholeMap(map).GetEdgeCells(exitDirection).ToList();
            Dictionary<IntVec3, float> cellDist = new Dictionary<IntVec3, float>();

            foreach(IntVec3 c in cells)
            {
                float dist = (float)(Math.Sqrt(Math.Pow((c.x - leadShip.Position.x), 2) + Math.Pow((c.z - leadShip.Position.z), 2)));
                cellDist.Add(c, dist);
            }
            cellDist = cellDist.OrderBy(x => x.Value).ToDictionary(z => z.Key, y => y.Value);
            List<Pawn> ships = pawns.Where(x => HelperMethods.IsBoat(x)).ToList();

            for(int i = 0; i < cells.Count; i++)
            {
                IntVec3 iV2 = cellDist.Keys.ElementAt(i);
                if(validator(iV2))
                {
                    if(ships.All(x => ShipReachabilityUtility.CanReachShip(x, iV2, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn)))
                    {
                        IntVec2 v2 = new IntVec2(iV2.x, iV2.z);
                        int halfSize = leadShip.def.size.z + 1;
                        switch(exitDirection.AsInt)
                        {
                            case 0:
                                for (int j = 0; j < halfSize; j++)
                                {
                                    if (!map.terrainGrid.TerrainAt(new IntVec3(iV2.x, iV2.y, iV2.z - j)).IsWater)
                                        goto IL_0;
                                }
                                break;
                            case 1:
                                for(int j = 0; j < halfSize; j++)
                                {
                                    if(!map.terrainGrid.TerrainAt(new IntVec3(iV2.x - j, iV2.y, iV2.z)).IsWater)
                                        goto IL_0;
                                }
                                break;
                            case 2:
                                for (int j = 0; j < halfSize; j++)
                                {
                                    if (!map.terrainGrid.TerrainAt(new IntVec3(iV2.x, iV2.y, iV2.z + j)).IsWater)
                                        goto IL_0;
                                }
                                break;
                            case 3:
                                for (int j = 0; j < halfSize; j++)
                                {
                                    if (!map.terrainGrid.TerrainAt(new IntVec3(iV2.x + j, iV2.y, iV2.z)).IsWater)
                                        goto IL_0;
                                }
                                break;
                        }
                        spot = iV2;
                        return spot.IsValid;
                    }
                    IL_0:;
                }
            }
            spot = IntVec3.Invalid;
            return false;
        }

        public static bool TryFindPackingSpotShips(IntVec3 exitSpot, out IntVec3 packingSpot, ref bool __result, Dialog_FormCaravan __instance, Map ___map)
        {
            if(__instance.transferables.Any(x => x.ThingDef.category is ThingCategory.Pawn && HelperMethods.IsBoat(x.AnyThing as Pawn)))
            {
                List<Thing> tmpPackingSpots = new List<Thing>();
                List<Thing> list = ___map.listerThings.ThingsOfDef(ThingDefOf.CaravanPackingSpot);
                TraverseParms traverseParms = TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false);
                List<Pawn> ships = new List<Pawn>();
                foreach (TransferableOneWay t in __instance.transferables)
                {
                    if(HelperMethods.IsBoat(t.AnyThing as Pawn))
                        ships.Add(t.AnyThing as Pawn);
                }
                foreach(Thing t in list)
                {
                    foreach(Pawn p in ships)
                    {
                        if (___map.reachability.CanReach(p.Position, t, PathEndMode.OnCell, traverseParms))
                        {
                            tmpPackingSpots.Add(t);
                        }
                    }
                }
                if(tmpPackingSpots.Any<Thing>())
                {
                    Thing thing = tmpPackingSpots.RandomElement<Thing>();
                    tmpPackingSpots.Clear();
                    packingSpot = thing.Position;
                    __result = true;
                    return false;
                }

                __result = CellFinder.TryFindRandomCellNear(ships.First().Position, ___map, 15, (IntVec3 c) => c.InBounds(___map) && c.Standable(___map) && !___map.terrainGrid.TerrainAt(c).IsWater, out packingSpot);
                if(!__result)
                {
                    __result = CellFinder.TryFindRandomCellNear(ships.First().Position, ___map, 20, (IntVec3 c) => c.InBounds(___map) && c.Standable(___map), out packingSpot);
                }

                if(!__result)
                {
                    Messages.Message("PackingSpotNotFoundBoats".Translate(), MessageTypeDefOf.CautionInput, false);
                    __result = RCellFinder.TryFindRandomSpotJustOutsideColony(ships.First().Position, ___map, out packingSpot);
                }
                return false;
            }
            packingSpot = IntVec3.Invalid;
            return true;
        }

        public static bool FillTabVehicleCaravan(ITab_Pawn_FormingCaravan __instance, ref List<Thing> ___thingsToSelect, Vector2 ___size, 
            ref float ___lastDrawnHeight, ref Vector2 ___scrollPosition, ref List<Thing> ___tmpSingleThing)
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
                MethodInfo doPeopleAndAnimals = AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "DoPeopleAndAnimals");
                doPeopleAndAnimals.Invoke(__instance, method1Args);
                num = (float)method1Args[1];
                num += 4f;
                HelperMethods.DoItemsListForShip(rect, ref num, ref ___tmpSingleThing, __instance);
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

        public static IEnumerable<CodeInstruction> ShipsArrivedTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
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

                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(EnterMapUtilityBoats), nameof(EnterMapUtilityBoats.Enter)));
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

                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(EnterMapUtilityBoats), nameof(EnterMapUtilityBoats.Enter)));
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

                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(EnterMapUtilityBoats), nameof(EnterMapUtilityBoats.Enter)));
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

                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(EnterMapUtilityBoats), nameof(EnterMapUtilityBoats.Enter)));
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

                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(EnterMapUtilityBoats), nameof(EnterMapUtilityBoats.Enter)));
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
            if(HelperMethods.HasBoat(caravan))
            {
                EnterMapUtilityBoats.Enter(caravan, map, enterMode, dropInventoryMode, draftColonists, extraCellValidator);
                return false;
            }
            return true;
        }

        public static bool EnterMapShipsCatchAll2(Caravan caravan, Map map, Func<Pawn, IntVec3> spawnCellGetter, CaravanDropInventoryMode dropInventoryMode = CaravanDropInventoryMode.DoNotDrop, bool draftColonists = false)
        {
            if(HelperMethods.HasBoat(caravan))
            {
                EnterMapUtilityBoats.EnterSpawn(caravan, map, spawnCellGetter, dropInventoryMode, draftColonists);
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

        public static bool GetInspectStringVehicle(Caravan __instance, ref string __result)
        {
            if(__instance.PawnsListForReading.Any(x => HelperMethods.IsVehicle(x)))
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
                foreach(Pawn ship in __instance.PawnsListForReading.Where(x => HelperMethods.IsVehicle(x)))
                {
                    numS++;
                    foreach(Pawn p in ship.GetComp<CompVehicle>().AllPawnsAboard)
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
                foreach(Pawn p in __instance.PawnsListForReading.Where(x => !HelperMethods.IsVehicle(x)))
                {
                    if (p.IsColonist)
                        num++;
                    if (p.RaceProps.Animal)
                        num2++;
                    if (p.IsPrisoner)
                        num3++;
                    if (p.Downed)
                        num4++;
                    if (p.InMentalState)
                        num5++;
                }

                if (numS > 1)
                {
                    Dictionary<Thing, int> vehicleCounts = new Dictionary<Thing, int>();
                    foreach(Pawn p in __instance.PawnsListForReading.Where(x => HelperMethods.IsVehicle(x)))
                    {
                        if(vehicleCounts.ContainsKey(p))
                        {
                            vehicleCounts[p]++;
                        }
                        else
                        {
                            vehicleCounts.Add(p, 1);
                        }
                    }

                    foreach(KeyValuePair<Thing, int> vehicles in vehicleCounts)
                    {
                        stringBuilder.Append($"{vehicles.Value} {vehicles.Key.LabelCap}");
                    }
                }
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
                    else if (HelperMethods.HasBoat(__instance))
                        stringBuilder.Append("CaravanSailing".Translate());
                    else
                        stringBuilder.Append("CaravanTraveling".Translate());
                }
                else
                {
                    Settlement settlementBase = CaravanVisitUtility.SettlementVisitedNow(__instance);
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

        public static bool CaravanLostAllVehicles(Pawn member, Caravan __instance)
        {
            if(HelperMethods.HasVehicle(__instance) && !__instance.PawnsListForReading.Any(x => !HelperMethods.IsVehicle(x)))
            {
                if(!__instance.Spawned)
                {
                    Log.Error("Caravan member died in an unspawned caravan. Unspawned caravans shouldn't be kept for more than a single frame.", false);
                }
                if(!__instance.PawnsListForReading.Any(x => HelperMethods.IsVehicle(x) && !x.Dead && x.GetComp<CompVehicle>().AllPawnsAboard.Any((Pawn y) => y != member && __instance.IsOwner(y))))
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
                if(__instance.PawnsListForReading.Any(x => x.GetComp<CompVehicle>().Props.vehiclePowerType == PowerType.Manual))
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

        public static IEnumerable<CodeInstruction> BoardVehiclesWhenReformingCaravanTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            foreach(CodeInstruction instruction in instructions)
            {
                if(instruction.opcode == OpCodes.Ret)
                {
                    Label label = ilg.DefineLabel();
                    Label label2 = ilg.DefineLabel();

                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_1);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.HasVehicle), new Type[] { typeof(Caravan) }));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

                    /*yield return new CodeInstruction(opcode: OpCodes.Ldloc_1);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), parameters: new Type[] { typeof(Caravan) }, nameof(HelperMethods.AbleToEmbark)));
                    yield return new CodeInstruction(opcode: OpCodes.Brtrue, label2);*/

                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_1);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.BoardAllCaravanPawns)));

                    instruction.labels.Add(label);
                }
                yield return instruction;
            }
        }

        public static void PostOpenSetInstance(Dialog_FormCaravan __instance)
        {
            currentFormingCaravan = __instance;
        }

        public static void PostCloseSetInstance(Dialog_FormCaravan __instance)
        {
            if(!__instance.choosingRoute)
                currentFormingCaravan = null;
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
            if(!RimShipMod.mod.settings.debugSpawnBoatBuildingGodMode && Prefs.DevMode && DebugSettings.godMode && newThing.def.HasModExtension<SpawnThingBuilt>())
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
        internal static Dialog_FormCaravan currentFormingCaravan;
        internal static bool routePlannerActive;
        internal static Dictionary<Map, List<WaterRegion>> terrainChangedCount = new Dictionary<Map, List<WaterRegion>>();

        /// <summary>
        /// Debugging
        /// </summary>
        internal static List<WorldPath> debugLines = new List<WorldPath>();
        internal static List<Pair<int, int>> tiles = new List<Pair<int,int>>(); // Pair -> TileID : Cycle
        internal static readonly bool debug = false;
        internal static readonly bool drawPaths = false;
    }
}