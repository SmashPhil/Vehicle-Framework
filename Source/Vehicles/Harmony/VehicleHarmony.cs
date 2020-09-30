using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
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
using OpCodes = System.Reflection.Emit.OpCodes;
using Vehicles.Components;

namespace Vehicles
{
    [StaticConstructorOnStartup]
    internal static class VehicleHarmony
    {
        static VehicleHarmony()
        {
            var harmony = new Harmony("rimworld.vehicles.smashphil");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            //Harmony.DEBUG = true;

            #region Functions

            /* Map Gen */
            harmony.Patch(original: AccessTools.Method(typeof(BeachMaker), nameof(BeachMaker.Init)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(BeachMakerTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(TileFinder), nameof(TileFinder.RandomSettlementTileFor)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(PushSettlementToCoastTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(PathGrid), nameof(PathGrid.RecalculatePerceivedPathCostUnderThing)), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(RecalculateShipPathCostUnderThing)));

            /* Health */
            harmony.Patch(original: AccessTools.Method(typeof(HealthUtility), nameof(HealthUtility.GetGeneralConditionLabel)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(ReplaceConditionLabel)));
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_HealthTracker), "ShouldBeDowned"),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehicleShouldBeDowned)));
            harmony.Patch(original: AccessTools.Method(typeof(PawnDownedWiggler), nameof(PawnDownedWiggler.WigglerTick)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehicleShouldWiggle)));
            harmony.Patch(original: AccessTools.Method(typeof(HediffUtility), nameof(HediffUtility.CanHealNaturally)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehiclesDontHeal)));
            harmony.Patch(original: AccessTools.Method(typeof(HediffUtility), nameof(HediffUtility.CanHealFromTending)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehiclesDontHealTended)));
            harmony.Patch(original: AccessTools.Method(typeof(Widgets), nameof(Widgets.InfoCardButton), new Type[] { typeof(float), typeof(float), typeof(Thing) }), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(InfoCardVehiclesTranspiler))); //REDO - change to remove info card button rather than patching the widget
            harmony.Patch(original: AccessTools.Method(typeof(Verb_CastAbility), nameof(Verb_CastAbility.CanHitTarget)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehiclesImmuneToPsycast)));

            /* Rendering */
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_RotationTracker), nameof(Pawn_RotationTracker.UpdateRotation)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(UpdateVehicleRotation)));
            harmony.Patch(original: AccessTools.Method(typeof(ColonistBar), "CheckRecacheEntries"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(CheckRecacheEntriesTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(ColonistBarColonistDrawer), "DrawIcons"), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(DrawIconsVehicles)));
            harmony.Patch(original: AccessTools.Method(typeof(SelectionDrawer), "DrawSelectionBracketFor"),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(DrawSelectionBracketsVehicles)));
            harmony.Patch(original: AccessTools.Method(typeof(PawnFootprintMaker), nameof(PawnFootprintMaker.FootprintMakerTick)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(BoatWakesTicker)));
            harmony.Patch(original: AccessTools.Method(typeof(PawnTweener), "TweenedPosRoot"),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehicleTweenedPosRoot)));

            //Change targeter to transpiler on UIRoot_Play
            harmony.Patch(original: AccessTools.Method(typeof(Targeter), nameof(Targeter.TargeterOnGUI)), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(DrawCannonTargeter)));
            harmony.Patch(original: AccessTools.Method(typeof(Targeter), nameof(Targeter.ProcessInputEvents)), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(ProcessCannonInputEvents)));
            harmony.Patch(original: AccessTools.Method(typeof(Targeter), nameof(Targeter.TargeterUpdate)), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(CannonTargeterUpdate)));
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_DrawTracker), nameof(Pawn_DrawTracker.Notify_DamageApplied)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehiclesDamageTakenWiggler)));
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_DrawTracker), nameof(Pawn_DrawTracker.Notify_DamageDeflected)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehiclesDamageDeflectedWiggler)));
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_DrawTracker), nameof(Pawn_DrawTracker.DrawTrackerTick)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehiclesDrawTrackerTick)));
            
            /* Gizmos */
            harmony.Patch(original: AccessTools.Method(typeof(Settlement), nameof(Settlement.GetCaravanGizmos)), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(NoAttackSettlementWhenDocked)));
            harmony.Patch(original: AccessTools.Method(typeof(Settlement), nameof(Settlement.GetGizmos)), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(AddVehicleCaravanGizmoPassthrough)));
            harmony.Patch(original: AccessTools.Method(typeof(FormCaravanComp), nameof(FormCaravanComp.GetGizmos)), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(AddVehicleGizmosPassthrough)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.GetGizmos)), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(GizmosForVehicleCaravans)));

            /* Pathing */
            harmony.Patch(original: AccessTools.Method(typeof(FloatMenuMakerMap), "GotoLocationOption"),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(GotoLocationShips)));
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.StartPath)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(StartVehiclePath)));
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_PathFollower), "NeedNewPath"),
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(IsVehicleInNextCell)));
            harmony.Patch(original: AccessTools.Method(typeof(PathFinder), nameof(PathFinder.FindPath), new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode) }),
                transpiler: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(PathAroundVehicles)));

            /* World Pathing */
            harmony.Patch(original: AccessTools.Method(typeof(WorldSelector), "AutoOrderToTileNow"),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(AutoOrderVehicleCaravanPathing)));
            harmony.Patch(original: AccessTools.Method(typeof(Caravan_PathFollower), "StartPath"),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(StartVehicleCaravanPath)));
            harmony.Patch(original: AccessTools.Method(typeof(TilesPerDayCalculator), nameof(TilesPerDayCalculator.ApproxTilesPerDay), new Type[] { typeof(Caravan), typeof(StringBuilder) }),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(ApproxTilesForShips)));
            harmony.Patch(original: AccessTools.Method(typeof(WorldRoutePlanner), nameof(WorldRoutePlanner.WorldRoutePlannerUpdate)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehicleRoutePlannerUpdateHook)));
            harmony.Patch(original: AccessTools.Method(typeof(WorldRoutePlanner), nameof(WorldRoutePlanner.WorldRoutePlannerOnGUI)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehicleRoutePlannerOnGUIHook)));
            harmony.Patch(original: AccessTools.Method(typeof(WorldRoutePlanner), nameof(WorldRoutePlanner.DoRoutePlannerButton)), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehicleRoutePlannerButton)));

            /* Upgrade Stats */
            harmony.Patch(original: AccessTools.Method(typeof(Pawn), "TicksPerMove"),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehicleMoveSpeedUpgradeModifier)));
            harmony.Patch(original: AccessTools.Method(typeof(MassUtility), nameof(MassUtility.Capacity)), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehicleCargoCapacity)));

            /* World Handling */
            harmony.Patch(original: AccessTools.Method(typeof(WorldPawns), nameof(WorldPawns.GetSituation)), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(SituationBoardedVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(WorldPawns), nameof(WorldPawns.RemoveAndDiscardPawnViaGC)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(DoNotRemoveDockedBoats)));

            /* Jobs */
            harmony.Patch(original: AccessTools.Method(typeof(JobUtility), nameof(JobUtility.TryStartErrorRecoverJob)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehicleErrorRecoverJob)));
            harmony.Patch(original: AccessTools.Method(typeof(JobGiver_Wander), "TryGiveJob"),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehiclesDontWander)));
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.CheckForJobOverride)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(NoOverrideDamageTakenTranspiler)));
            harmony.Patch(original: AccessTools.Property(typeof(JobDriver_PrepareCaravan_GatherItems), "Transferables").GetGetMethod(nonPublic: true),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(TransferablesVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(ThingRequest), nameof(ThingRequest.Accepts)), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(AcceptsVehicleRefuelable)));

            /* Lords */
            harmony.Patch(original: AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherAnimals), nameof(LordToil_PrepareCaravan_GatherAnimals.UpdateAllDuties)), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(UpdateDutyOfVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(GatheringsUtility), nameof(GatheringsUtility.ShouldGuestKeepAttendingGathering)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehiclesDontParty)));

            /* Caravan Formation */
            harmony.Patch(original: AccessTools.Method(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.IsFormingCaravan)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(IsFormingCaravanVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(TransferableUtility), nameof(TransferableUtility.CanStack)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(CanStackVehicleTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(GiveToPackAnimalUtility), nameof(GiveToPackAnimalUtility.UsablePackAnimalWithTheMostFreeSpace)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(UsableVehicleWithMostFreeSpace)));
            harmony.Patch(original: AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(AddHumanLikeOrdersLoadVehiclesTranspiler)));

            /* Caravan */
            harmony.Patch(original: AccessTools.Method(typeof(Dialog_FormCaravan), "TryReformCaravan"),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(ConfirmLeaveVehiclesOnReform)));
            harmony.Patch(original: AccessTools.Method(typeof(CollectionsMassCalculator), nameof(CollectionsMassCalculator.Capacity), new Type[] { typeof(List<ThingCount>), typeof(StringBuilder) }),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(CapacityWithVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(MassUtility), nameof(MassUtility.CanEverCarryAnything)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(CanCarryIfVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "FillTab"),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(FillTabVehicleCaravan)));
            harmony.Patch(original: AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "DoPeopleAndAnimals"), 
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(DoPeopleAnimalsAndVehicle)));
            //harmony.Patch(original: AccessTools.Method(typeof(CaravanArrivalAction_Enter), nameof(CaravanArrivalAction_Enter.Arrived)), prefix: null, postfix: null,
            //    transpiler: new HarmonyMethod(typeof(VehicleHarmony),
            //    nameof(VehiclesArrivedTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanArrivalAction_VisitEscapeShip), "DoArrivalAction"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(ShipsVisitEscapeShipTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(SettlementUtility), "AttackNow"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(AttackNowWithShipsTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanArrivalAction_VisitSite), "DoEnter"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(DoEnterWithShipsTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanEnterMapUtility), nameof(CaravanEnterMapUtility.Enter), new Type[]{typeof(Caravan), typeof(Map), typeof(CaravanEnterMode), typeof(CaravanDropInventoryMode), typeof(bool), typeof(Predicate<IntVec3>) }),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(EnterMapVehiclesCatchAll1)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanEnterMapUtility), nameof(CaravanEnterMapUtility.Enter), new Type[] { typeof(Caravan), typeof(Map), typeof(Func<Pawn, IntVec3>), typeof(CaravanDropInventoryMode), typeof(bool) }),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(EnterMapVehiclesCatchAll2)));
            harmony.Patch(original: AccessTools.Property(typeof(Caravan), nameof(Caravan.AllOwnersDowned)).GetGetMethod(),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(AllOwnersDownedVehicle)));
            harmony.Patch(original: AccessTools.Property(typeof(Caravan), nameof(Caravan.AllOwnersHaveMentalBreak)).GetGetMethod(),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(AllOwnersMentalBreakVehicle)));
            harmony.Patch(original: AccessTools.Property(typeof(Caravan), nameof(Caravan.NightResting)).GetGetMethod(),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(NoRestForVehicles)));
            harmony.Patch(original: AccessTools.Method(typeof(Caravan), nameof(Caravan.ContainsPawn)), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(ContainsPawnInVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(Caravan), nameof(Caravan.IsOwner)), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(IsOwnerOfVehicle)));
            harmony.Patch(original: AccessTools.Property(typeof(MapPawns), nameof(MapPawns.AnyPawnBlockingMapRemoval)).GetGetMethod(), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(AnyVehicleBlockingMapRemoval)));
            harmony.Patch(original: AccessTools.Method(typeof(SettlementDefeatUtility), nameof(SettlementDefeatUtility.CheckDefeated)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(CheckDefeatedWithVehiclesTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(Tale_DoublePawn), nameof(Tale_DoublePawn.Concerns)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(ConcernNullThing)));
            harmony.Patch(original: AccessTools.Method(typeof(WITab_Caravan_Needs), "FillTab"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehicleNeedsFillTabTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(WITab_Caravan_Needs), "UpdateSize"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehicleNeedsUpdateSizeTranspiler)));
            harmony.Patch(original: AccessTools.Property(typeof(WITab_Caravan_Gear), "Pawns").GetGetMethod(nonPublic: true), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehicleGearTabPawns)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanInventoryUtility), nameof(CaravanInventoryUtility.AllInventoryItems)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehicleAllInventoryItems)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanInventoryUtility), nameof(CaravanInventoryUtility.GiveThing)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehicleGiveThingInventoryTranspiler)));
            harmony.Patch(original: AccessTools.Property(typeof(WITab_Caravan_Health), "Pawns").GetGetMethod(nonPublic: true),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehicleHealthTabPawns)));
            harmony.Patch(original: AccessTools.Property(typeof(WITab_Caravan_Social), "Pawns").GetGetMethod(nonPublic: true),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(VehicleSocialTabPawns)));
            harmony.Patch(original: AccessTools.Method(typeof(BestCaravanPawnUtility), nameof(BestCaravanPawnUtility.FindPawnWithBestStat)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(FindPawnInVehicleWithBestStat)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanArrivalAction_OfferGifts), nameof(CaravanArrivalAction_OfferGifts.Arrived)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(UnloadVehicleOfferGifts)));
            harmony.Patch(original: AccessTools.Method(typeof(Settlement_TraderTracker), nameof(Settlement_TraderTracker.GiveSoldThingToPlayer)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(GiveSoldThingToVehicleTranspiler)));
            harmony.Patch(original: AccessTools.Method(typeof(Caravan_NeedsTracker), "TrySatisfyPawnNeeds"),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(TrySatisfyVehiclePawnsNeeds)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanUtility), nameof(CaravanUtility.IsCaravanMember)), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(IsParentCaravanMember)));
            harmony.Patch(original: AccessTools.Method(typeof(CaravanUtility), nameof(CaravanUtility.RandomOwner)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(RandomVehicleOwner)));

            /* Raids and AI */

            /* Draftable */
            harmony.Patch(original: AccessTools.Property(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted)).GetSetMethod(),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(DraftedVehiclesCanMove)));
            harmony.Patch(original: AccessTools.Method(typeof(FloatMenuMakerMap), "CanTakeOrder"), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(CanVehicleTakeOrder)));
            harmony.Patch(original: AccessTools.Method(typeof(FloatMenuUtility), nameof(FloatMenuUtility.GetMeleeAttackAction)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(NoMeleeForVehicles))); //Change..?
            harmony.Patch(original: AccessTools.Method(typeof(PawnComponentsUtility), nameof(PawnComponentsUtility.CreateInitialComponents)), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(CreateInitialVehicleComponents)));
            harmony.Patch(original: AccessTools.Method(typeof(PawnComponentsUtility), nameof(PawnComponentsUtility.AddAndRemoveDynamicComponents)), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(AddAndRemoveVehicleComponents)));

            /* Construction */
            harmony.Patch(original: AccessTools.Method(typeof(Frame), nameof(Frame.CompleteConstruction)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(CompleteConstructionVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(ListerBuildingsRepairable), nameof(ListerBuildingsRepairable.Notify_BuildingRepaired)),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(Notify_RepairedVehicle)));
            harmony.Patch(original: AccessTools.Method(typeof(GenSpawn), name: nameof(GenSpawn.Spawn), new Type[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool) }),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(SpawnVehicleGodMode)));

            /* Extra */
            harmony.Patch(original: AccessTools.Property(typeof(MapPawns), nameof(MapPawns.FreeColonistsSpawnedOrInPlayerEjectablePodsCount)).GetGetMethod(), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(FreeColonistsInVehiclesTransport)));
            harmony.Patch(original: AccessTools.Method(typeof(MapPawns), nameof(MapPawns.FreeHumanlikesSpawnedOfFaction)), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(FreeHumanlikesSpawnedInVehicles)));
            harmony.Patch(original: AccessTools.Method(typeof(MapPawns), nameof(MapPawns.FreeHumanlikesOfFaction)), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(FreeHumanlikesInVehicles)));
            harmony.Patch(original: AccessTools.Method(typeof(Selector), "HandleMapClicks"),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(MultiSelectFloatMenu)));
            harmony.Patch(original: AccessTools.Method(typeof(MentalState_Manhunter), nameof(MentalState_Manhunter.ForceHostileTo), new Type[] { typeof(Thing) }), prefix: null,
                postfix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(ManhunterDontAttackVehicles)));
            harmony.Patch(original: AccessTools.Method(typeof(Projectile_Explosive), "Impact"),
                prefix: new HarmonyMethod(typeof(VehicleHarmony),
                nameof(ShellsImpactWater)));

            /* Debug Patches */
            if(debug)
            {
                harmony.Patch(original: AccessTools.Method(typeof(WorldRoutePlanner), nameof(WorldRoutePlanner.WorldRoutePlannerUpdate)), prefix: null,
                    postfix: new HarmonyMethod(typeof(VehicleHarmony),
                    nameof(DebugSettlementPaths)));
                harmony.Patch(original: AccessTools.Method(typeof(WorldObjectsHolder), nameof(WorldObjectsHolder.Add)),
                    prefix: new HarmonyMethod(typeof(VehicleHarmony),
                    nameof(DebugWorldObjects)));
                harmony.Patch(original: AccessTools.Method(typeof(RegionGrid), nameof(RegionGrid.DebugDraw)), prefix: null,
                    postfix: new HarmonyMethod(typeof(VehicleHarmony),
                    nameof(DebugDrawWaterRegion)));
            }

            //harmony.Patch(original: AccessTools.Method(typeof(), ""), 
            //    prefix: new HarmonyMethod(typeof(VehicleHarmony),
            //    nameof(TestDebug)));

            HelperMethods.CannonTargeter = new CannonTargeter();

            HelperMethods.missingIcon = ContentFinder<Texture2D>.Get("Upgrades/missingIcon", true);
            HelperMethods.assignedSeats = new Dictionary<Pawn, Pair<VehiclePawn, VehicleHandler>>();

            HelperMethods.ValidateAllVehicleDefs();
            #endregion Functions
        }

        #region Debug

        /// <summary>
        /// Generic patch method for testing
        /// </summary>
        /// <returns></returns>
        public static bool TestDebug()
        {
            return true;
        }

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
            ___map.GetCachedMapComponent<WaterMap>()?.WaterRegionGrid?.DebugDraw();
        }

        /// <summary>
        /// Draw paths from original settlement position to new position when moving settlement to coastline
        /// </summary>
        public static void DebugSettlementPaths()
        {
            if (drawPaths && !debugLines.AnyNullified()) 
                return;
            if (drawPaths)
            {
                foreach (WorldPath wp in debugLines)
                {
                    wp.DrawPath(null);
                }
            }

            foreach (Pair<int, int> t in tiles)
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
            ___map.GetCachedMapComponent<WaterMap>()?.ShipPathGrid?.RecalculatePerceivedPathCostUnderThing(t);
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
                if (pawn is VehiclePawn vehicle)
                {
                    if (vehicle.GetCachedComp<CompVehicle>().movementStatus == VehicleMovementStatus.Offline && !pawn.Dead)
                    {
                        if (HelperMethods.IsBoat(pawn) && vehicle.GetCachedComp<CompVehicle>().beached)
                        {
                            __result = vehicle.GetCachedComp<CompVehicle>().Props.healthLabel_Beached;
                        }
                        else
                        {
                            __result = vehicle.GetCachedComp<CompVehicle>().Props.healthLabel_Immobile;
                        }

                        return false;
                    }
                    if (pawn.Dead)
                    {
                        __result = vehicle.GetCachedComp<CompVehicle>().Props.healthLabel_Dead;
                        return false;
                    }
                    if (pawn.health.summaryHealth.SummaryHealthPercent < 0.95)
                    {
                        __result = vehicle.GetCachedComp<CompVehicle>().Props.healthLabel_Injured;
                        return false;
                    }
                    __result = vehicle.GetCachedComp<CompVehicle>().Props.healthLabel_Healthy;
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
            if (___pawn != null && ___pawn is VehiclePawn vehicle)
            {
                __result = vehicle.GetCachedComp<CompVehicle>().Props.downable;
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
            if (___pawn != null && ___pawn is VehiclePawn vehicle && !vehicle.GetCachedComp<CompVehicle>().Props.movesWhenDowned)
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
            if(hd.pawn.IsVehicle())
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
            if(hd.pawn.IsVehicle())
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

        /// <summary>
        /// Block vehicles from receiving psycast effects
        /// </summary>
        /// <param name="targ"></param>
        /// <returns></returns>
        public static bool VehiclesImmuneToPsycast(LocalTargetInfo targ)
        {
            if (targ.Pawn is VehiclePawn vehicle)
            {
                Log.Message($"Psycast blocked for {vehicle}");
                return false;
            }
            return true;
        }

        #endregion HealthStats

        #region Rendering
        /// <summary>
        /// Use own Vehicle rotation to disallow moving rotation for various tasks such as Drafted
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        public static bool UpdateVehicleRotation(Pawn_RotationTracker __instance, Pawn ___pawn)
        {
            if (HelperMethods.IsVehicle(___pawn))
            {
                VehiclePawn pawn = ___pawn as VehiclePawn;
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
                    pawn.UpdateRotationAndAngle();
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

        //REDO (remove flag and implement more concrete transpiler)
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
        /// Render small vehicle icon on colonist bar picture rect if they are currently onboard a vehicle
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
            GUI.DrawTexture(rect2, VehicleTex.CachedTextureIcons[handler.vehiclePawn.def]);
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
            var vehicle = obj as VehiclePawn;
            var building = obj as VehicleBuilding;
            if(vehicle != null || building?.vehicleReference != null)
            {
                if (vehicle is null)
                {
                    vehicle = building.vehicleReference;
                }
                Vector3[] brackets = new Vector3[4];
                float angle = vehicle.Angle;

                Vector3 newDrawPos = vehicle.DrawPosTransformed(vehicle.GetCachedComp<CompVehicle>().Props.hitboxOffsetX, vehicle.GetCachedComp<CompVehicle>().Props.hitboxOffsetZ, angle);

                FieldInfo info = AccessTools.Field(typeof(SelectionDrawer), "selectTimes");
                object o = info.GetValue(null);
                SPMultiCell.CalculateSelectionBracketPositionsWorldForMultiCellPawns(brackets, vehicle, newDrawPos, vehicle.RotatedSize.ToVector2(), (Dictionary<object, float>)o, Vector2.one, angle, 1f);

                int num = (angle != 0) ? (int)angle : 0;
                for (int i = 0; i < 4; i++)
                {
                    Quaternion rotation = Quaternion.AngleAxis(num, Vector3.up);
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
            if(___pawn.IsBoat())
            {
                VehiclePawn boat = ___pawn as VehiclePawn;
                if ((boat.Drawer.DrawPos - ___lastFootprintPlacePos).MagnitudeHorizontalSquared() > 0.1)
                {
                    Vector3 drawPos = boat.Drawer.DrawPos;
                    if (drawPos.ToIntVec3().InBounds(boat.Map) && !boat.GetCachedComp<CompVehicle>().beached)
                    {
                        MoteMaker.MakeWaterSplash(drawPos, boat.Map, 7 * boat.GetCachedComp<CompVehicle>().Props.wakeMultiplier, boat.GetCachedComp<CompVehicle>().Props.wakeSpeed);
                        ___lastFootprintPlacePos = drawPos;
                    }
                }
                else if(VehicleMod.settings.passiveWaterWaves)
                {
                    if(Find.TickManager.TicksGame % 360 == 0)
                    {
                        float offset = Mathf.PingPong(Find.TickManager.TicksGame / 10, boat.ageTracker.CurKindLifeStage.bodyGraphicData.drawSize.y / 4);
                        MoteMaker.MakeWaterSplash(boat.Drawer.DrawPos - new Vector3(0,0, offset), boat.Map, boat.GetCachedComp<CompVehicle>().Props.wakeMultiplier, boat.GetCachedComp<CompVehicle>().Props.wakeSpeed);
                    }
                }
                return false;
            }
            return true;
        }

        public static bool VehicleTweenedPosRoot(Pawn ___pawn, ref Vector3 __result)
        {
            if(___pawn is VehiclePawn vehicle)
            {
                if (!vehicle.Spawned)
                {
                    __result = vehicle.Position.ToVector3Shifted();
                    return false;
                }
                float num = vehicle.VehicleMovedPercent();
                __result = vehicle.vPather.nextCell.ToVector3Shifted() * num + vehicle.Position.ToVector3Shifted() * (1f - num); //+ PawnCollisionOffset?
                return false;
            }
            return true;
        }

        /* ---------------- Hooks onto Targeter calls ---------------- */
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
        /* ----------------------------------------------------------- */

        /// <summary>
        /// Call associated Vehicle_DrawTracker event for damage taken
        /// </summary>
        /// <param name="dinfo"></param>
        /// <param name="___pawn"></param>
        /// <param name="__instance"></param>
        /// <returns></returns>
        public static bool VehiclesDamageTakenWiggler(DamageInfo dinfo, Pawn ___pawn, Pawn_DrawTracker __instance)
        {
            if(___pawn is VehiclePawn vehicle && !vehicle.GetCachedComp<CompVehicle>().Props.movesWhenDowned)
            {
                (___pawn as VehiclePawn).Drawer.Notify_DamageApplied(dinfo);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Call associated Vehicle_DrawTracker event for damage deflected
        /// </summary>
        /// <param name="dinfo"></param>
        /// <param name="___pawn"></param>
        /// <param name="__instance"></param>
        /// <returns></returns>
        public static bool VehiclesDamageDeflectedWiggler(DamageInfo dinfo, Pawn ___pawn, Pawn_DrawTracker __instance)
        {
            if(___pawn is VehiclePawn vehicle && !vehicle.GetCachedComp<CompVehicle>().Props.movesWhenDowned)
            {
                (___pawn as VehiclePawn).Drawer.Notify_DamageDeflected(dinfo);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Hook onto Pawn_DrawTracker Tick() method for Vehicle_DrawTracker. Diverts base Pawn class for ticking Pawn_DrawTracker if pawn is a vehicle.
        /// If both Vehicle_DrawTracker and Pawn_DrawTracker tick, there will be duplicates due to both rendering simultaneously.
        /// </summary>
        /// <param name="___pawn"></param>
        /// <returns></returns>
        public static bool VehiclesDrawTrackerTick(Pawn ___pawn)
        {
            if(___pawn.IsVehicle())
            {
                (___pawn as VehiclePawn).Drawer.VehicleDrawerTick();
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

                if (VehicleMod.settings.debugDisableWaterPathing && vehicle.GetCachedComp<CompVehicle>().beached)
                    vehicle.GetCachedComp<CompVehicle>().RemoveBeachedStatus();
                if (value && !__instance.Drafted)
                {
                    if(!VehicleMod.settings.debugDraftAnyShip && vehicle.TryGetComp<CompFueledTravel>() != null && vehicle.GetCachedComp<CompFueledTravel>().EmptyTank)
                    {
                        Messages.Message("CompShips_OutOfFuel".Translate(), MessageTypeDefOf.RejectInput);
                        return false;
                    }
                    if(vehicle.GetCachedComp<CompUpgradeTree>()?.CurrentlyUpgrading ?? false)
                    {
                        Messages.Message("CompUpgrade_UpgradeInProgress".Translate(), MessageTypeDefOf.RejectInput);
                        return false;
                    }
                    vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().ClearReservedFor(vehicle);
                }
                else if(!value && vehicle.vPather.curPath != null)
                {
                    vehicle.vPather.PatherFailed();
                }
                if(!VehicleMod.settings.fishingPersists) vehicle.GetCachedComp<CompVehicle>().currentlyFishing = false;
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
            if(pawn is VehiclePawn)
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
            if(__instance.def.projectile.explosionDelay == 0 && terrainImpact.IsWater && !__instance.Position.GetThingList(__instance.Map).AnyNullified(x => x is VehiclePawn vehicle))
            {
                __instance.Explode();
            }
            return true;
        }

        public static void CreateInitialVehicleComponents(Pawn pawn)
        {
            if (pawn is VehiclePawn vehicle && vehicle.vPather is null)
			{
				vehicle.vPather = new Vehicle_PathFollower(vehicle);
                vehicle.vehicleAI = new VehicleAI(vehicle);
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
        /// <summary>
        /// Disable the ability to attack a settlement when docked there. Breaks immersion and can cause an entry cell error. (Boats Only)
        /// </summary>
        /// <param name="caravan"></param>
        /// <param name="__result"></param>
        /// <param name="__instance"></param>
        public static void NoAttackSettlementWhenDocked(Caravan caravan, ref IEnumerable<Gizmo> __result, Settlement __instance)
        {
            if(HelperMethods.HasBoat(caravan) && !caravan.pather.Moving)
            {
                List<Gizmo> gizmos = __result.ToList();
                if (caravan.PawnsListForReading.AnyNullified(x => !HelperMethods.IsBoat(x)))
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
        /// <returns></returns>
        public static IEnumerable<Gizmo> AddVehicleGizmosPassthrough(IEnumerable<Gizmo> __result, FormCaravanComp __instance)
        {
            IEnumerator<Gizmo> enumerator = __result.GetEnumerator();
            if(__instance.parent is MapParent mapParent && __instance.ParentHasMap)
            {
                if(!__instance.Reform)
                {
                    yield return new Command_Action()
                    {
                        defaultLabel = "CommandFormVehicleCaravan".Translate(),
		                defaultDesc = "CommandFormVehicleCaravanDesc".Translate(),
		                icon = Settlement.FormCaravanCommand,
                        action = delegate ()
                        {
                            Find.Tutor.learningReadout.TryActivateConcept(ConceptDefOf.FormCaravan);
                            Find.WindowStack.Add(new Dialog_FormVehicleCaravan(mapParent.Map));
                        }
                    };
                }
                else if(mapParent.Map.mapPawns.AllPawnsSpawned.Where(p => p.IsVehicle()).Count() > 0)
                {
                    Command_Action command_Action = new Command_Action
                    {
                        defaultLabel = "CommandReformVehicleCaravan".Translate(),
                        defaultDesc = "CommandReformVehicleCaravanDesc".Translate(),
                        icon = FormCaravanComp.FormCaravanCommand,
                        hotKey = KeyBindingDefOf.Misc2,
                        action = delegate ()
                        {
                            Find.WindowStack.Add(new Dialog_FormVehicleCaravan(mapParent.Map, true));
                        }
                    };
                    if (GenHostility.AnyHostileActiveThreatToPlayer(mapParent.Map, true))
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

        #endregion Gizmos

        #region Pathing

        /// <summary>
        /// Intercepts FloatMenuMakerMap call to restrict by size and call through to custom water based pathing requirements
        /// </summary>
        /// <param name="clickCell"></param>
        /// <param name="pawn"></param>
        /// <param name="__result"></param>
        /// <returns></returns>
        public static bool GotoLocationShips(IntVec3 clickCell, Pawn pawn, ref FloatMenuOption __result)
        {
            if (pawn is VehiclePawn vehicle)
            {
                if (vehicle.Faction != Faction.OfPlayer)
                    return false;
                Log.Message("Goto");
                if (vehicle.LocationRestrictedBySize(clickCell))
                {
                    Messages.Message("VehicleCannotFit".Translate(), MessageTypeDefOf.RejectInput);
                    return false;
                }

                if (vehicle.GetCachedComp<CompFueledTravel>() != null && vehicle.GetCachedComp<CompFueledTravel>().EmptyTank)
                {
                    Messages.Message("VehicleOutOfFuel".Translate(), MessageTypeDefOf.RejectInput);
                    return false;
                }

                if (DebugSettings.godMode)
                {
                    Log.Message("-> " + clickCell + " | " + vehicle.Map.terrainGrid.TerrainAt(clickCell).LabelCap + " | " + vehicle.Map.GetCachedMapComponent<WaterMap>().ShipPathGrid.CalculatedCostAt(clickCell) +
                        " - " + vehicle.Map.GetCachedMapComponent<WaterMap>().ShipPathGrid.pathGrid[vehicle.Map.cellIndices.CellToIndex(clickCell)]);
                }

                if (HelperMethods.IsBoat(vehicle) && !VehicleMod.settings.debugDisableWaterPathing)
                {
                    //if (!VehicleMod.settings.debugDisableSmoothPathing && pawn.GetCachedComp<CompVehicle>().Props.diagonalRotation)
                    //{
                    //    if (!(pawn as VehiclePawn).InitiateSmoothPath(clickCell))
                    //    {
                    //        Log.Error($"Failed Smooth Pathing. Cell: {clickCell} Pawn: {pawn.LabelShort}");
                    //    }
                    //    return false;
                    //}
                    int num = GenRadial.NumCellsInRadius(2.9f);
                    int i = 0;
                    IntVec3 curLoc;
                    while (i < num)
                    {
                        curLoc = GenRadial.RadialPattern[i] + clickCell;
                        if (GenGridShips.Standable(curLoc, vehicle.Map))
                        {
                            if (curLoc == vehicle.Position || vehicle.GetCachedComp<CompVehicle>().beached)
                            {
                                __result = null;
                                return false;
                            }
                            if (!ShipReachabilityUtility.CanReachShip(vehicle, curLoc, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                            {
                                if (debug) Log.Message("CANT REACH");
                                __result = new FloatMenuOption("CannotSailToCell".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null);
                                return false;
                            }
                            Action action = delegate ()
                            {
                                Job job = new Job(JobDefOf.Goto, curLoc);
                                if (vehicle.Map.exitMapGrid.IsExitCell(Verse.UI.MouseCell()))
                                {
                                    job.exitMapOnArrival = true;
                                }
                                else if (!vehicle.Map.IsPlayerHome && !vehicle.Map.exitMapGrid.MapUsesExitGrid && CellRect.WholeMap(vehicle.Map).IsOnEdge(Verse.UI.MouseCell(), 3) &&
                                    vehicle.Map.Parent.GetComponent<FormCaravanComp>() != null && MessagesRepeatAvoider.MessageShowAllowed("MessagePlayerTriedToLeaveMapViaExitGrid-" +
                                    vehicle.Map.uniqueID, 60f))
                                {
                                    FormCaravanComp component = vehicle.Map.Parent.GetComponent<FormCaravanComp>();
                                    if (component.CanFormOrReformCaravanNow)
                                    {
                                        Messages.Message("MessagePlayerTriedToLeaveMapViaExitGrid_CanReform".Translate(), vehicle.Map.Parent, MessageTypeDefOf.RejectInput, false);
                                    }
                                    else
                                    {
                                        Messages.Message("MessagePlayerTriedToLeaveMapViaExitGrid_CantReform".Translate(), vehicle.Map.Parent, MessageTypeDefOf.RejectInput, false);
                                    }
                                }
                                if (vehicle.jobs.TryTakeOrderedJob(job, JobTag.Misc))
                                {
                                    MoteMaker.MakeStaticMote(curLoc, vehicle.Map, ThingDefOf.Mote_FeedbackGoto, 1f);
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
                }
                else
                {
                    int num = GenRadial.NumCellsInRadius(2.9f);
                    int i = 0;
                    IntVec3 curLoc;
                    
                    while (i < num)
                    {
                        curLoc = GenRadial.RadialPattern[i] + clickCell;
                        if (GenGrid.Standable(curLoc, pawn.Map))
                        {
                            if (curLoc == pawn.Position)
                            {
                                __result = null;
                                return false;
                            }
                            if (!ReachabilityUtility.CanReach(pawn, curLoc, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                            {
                                if (debug) Log.Message("CANT REACH");
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
                }
                __result = null;
                return false;
            }
            else
            {
                if (HelperMethods.VehicleInCell(pawn.Map, clickCell))
                {
                    __result = new FloatMenuOption("CannotGoNoPath".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Intercept Pawn_PathFollower call to StartPath with VehiclePawn's own Vehicle_PathFollower. Required for custom path values and water based pathing
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="peMode"></param>
        /// <param name="___pawn"></param>
        /// <returns></returns>
        public static bool StartVehiclePath(LocalTargetInfo dest, PathEndMode peMode, Pawn ___pawn)
        {
            if(___pawn is VehiclePawn vehicle)
            {
                Log.Message("START PATH");
                vehicle.vPather.StartPath(dest, peMode);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determine if next cell is walkable with final determination if vehicle is in cell or not
        /// </summary>
        /// <param name="__result"></param>
        /// <param name="___pawn"></param>
        /// <param name="nextCell"></param>
        public static void IsVehicleInNextCell(ref bool __result, Pawn ___pawn, Pawn_PathFollower __instance)
        {
            if (!__result)
            {
                //Peek 2 nodes ahead to avoid collision last second
                __result = (__instance.curPath.NodesLeftCount > 1 && HelperMethods.VehicleInCell(___pawn.Map, __instance.curPath.Peek(1))) || (__instance.curPath.NodesLeftCount > 2 && HelperMethods.VehicleInCell(___pawn.Map, __instance.curPath.Peek(2)));
            }
        }

        /// <summary>
        /// Set cells in which vehicles reside as impassable to other Pawns
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="ilg"></param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> PathAroundVehicles(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            for(int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.Calls(AccessTools.Method(typeof(CellIndices), nameof(CellIndices.CellToIndex), new Type[] { typeof(int), typeof(int) })))
                {
                    Label label = ilg.DefineLabel();
                    Label vehicleLabel = ilg.DefineLabel();

                    yield return instruction; //CALLVIRT CELLTOINDEX
                    instruction = instructionList[++i];
                    yield return instruction; //STLOC.S 38
                    instruction = instructionList[++i];

                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(typeof(PathFinder), "map"));
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, 36);
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, 37);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.VehicleInCell), new Type[] { typeof(Map), typeof(int), typeof(int) }));

                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);
                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(opcode: OpCodes.Br, vehicleLabel);

                    for(int j = i; j < instructionList.Count; j++)
                    {
                        CodeInstruction instruction2 = instructionList[j];
                        if (instruction2.opcode == OpCodes.Brfalse || instruction2.opcode == OpCodes.Brfalse_S)
                        {
                            instruction2.labels.Add(vehicleLabel);
                            break;
                        }
                    }

                    instruction.labels.Add(label);
                }
                yield return instruction;
            }
        }

        #endregion Pathing
        
        #region WorldPathing
        /// <summary>
        /// Intercept AutoOrderToTileNow method to StartPath on VehicleCaravan_PathFollower
        /// Necessary due to CaravanUtility.BestGotoDestNear returning incorrect positions based on custom tile values for vehicles
        /// </summary>
        /// <param name="c"></param>
        /// <param name="tile"></param>
        /// <returns></returns>
        public static bool AutoOrderVehicleCaravanPathing(Caravan c, int tile)
        {
            if(c is VehicleCaravan caravan && caravan.HasVehicle())
            {
                if (tile < 0 || (tile == caravan.Tile && !caravan.vPather.Moving))
			    {
				    return false;
			    }
                int num = caravan.BestGotoDestForVehicle(tile);
			    if (num >= 0)
			    {
				    caravan.vPather.StartPath(num, null, true, true);
				    caravan.gotoMote.OrderedToTile(num);
				    SoundDefOf.ColonistOrdered.PlayOneShotOnCamera(null);
			    }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Catch-All for Caravan_PathFollower.StartPath, redirect to VehicleCaravan_PathFollower
        /// </summary>
        /// <param name="destTile"></param>
        /// <param name="arrivalAction"></param>
        /// <param name="___caravan"></param>
        /// <param name="repathImmediately"></param>
        /// <param name="resetPauseStatus"></param>
        /// <returns></returns>
        public static bool StartVehicleCaravanPath(int destTile, CaravanArrivalAction arrivalAction, Caravan ___caravan, bool repathImmediately = false, bool resetPauseStatus = true)
        {
            if(___caravan is VehicleCaravan vehicleCaravan && vehicleCaravan.HasVehicle())
            {
                vehicleCaravan.vPather.StartPath(destTile, arrivalAction, repathImmediately, resetPauseStatus);
            }
            return true;
        }

        //REDO
        public static bool ApproxTilesForShips(Caravan caravan, StringBuilder explanation = null)
        {
            //Continue here
            return true;
        }

        /* --------------- VehicleRoutePlanner Hook --------------- */
        public static void VehicleRoutePlannerUpdateHook()
        {
            Find.World.GetCachedWorldComponent<VehicleRoutePlanner>().WorldRoutePlannerUpdate();
        }

        public static void VehicleRoutePlannerOnGUIHook()
        {
            Find.World.GetCachedWorldComponent<VehicleRoutePlanner>().WorldRoutePlannerOnGUI();
        }

        public static void VehicleRoutePlannerButton(ref float curBaseY)
        {
            Find.World.GetCachedWorldComponent<VehicleRoutePlanner>().DoRoutePlannerButton(ref curBaseY);
        }
        /* ------------------------------------------------------- */
        #endregion WorldPathing

        #region UpgradeStatModifiers

        //REDO - Needs implementation of Weight based speed declination
        /// <summary>
        /// Apply MoveSpeed upgrade stat to vehicles
        /// </summary>
        /// <param name="diagonal"></param>
        /// <param name="__instance"></param>
        /// <param name="__result"></param>
        public static bool VehicleMoveSpeedUpgradeModifier(bool diagonal, Pawn __instance, ref int __result)
        {
            if(__instance is VehiclePawn vehicle)
            {
                float num = vehicle.GetCachedComp<CompVehicle>().ActualMoveSpeed / 60;
                float num2 = 1 / num;
                if (vehicle.Spawned && !vehicle.Map.roofGrid.Roofed(vehicle.Position))
                    num2 /= vehicle.Map.weatherManager.CurMoveSpeedMultiplier;
                if (diagonal)
                    num2 *= 1.41421f;
                __result = Mathf.Clamp(Mathf.RoundToInt(num2), 1, 450);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Apply Cargo capacity upgrade stat to vehicles
        /// </summary>
        /// <param name="p"></param>
        /// <param name="__result"></param>
        public static void VehicleCargoCapacity(Pawn p, ref float __result)
        {
            if(p is VehiclePawn vehicle)
            {
                __result = HelperMethods.ExtractUpgradeValue(vehicle, StatUpgradeCategory.CargoCapacity);
            }
        }

        #endregion UpgradeStatModifiers

        #region WorldHandling
        /// <summary>
        /// Prevent RimWorld Garbage Collection from snatching up VehiclePawn inhabitants and VehicleCaravan's VehiclePawn inhabitants by changing
        /// the WorldPawnSituation of pawns onboard vehicles
        /// </summary>
        /// <param name="p"></param>
        /// <param name="__result"></param>
        public static void SituationBoardedVehicle(Pawn p, ref WorldPawnSituation __result)
        {
            if(__result == WorldPawnSituation.Free && p.Faction != null && p.Faction == Faction.OfPlayerSilentFail)
            {
                foreach(Map map in Find.Maps)
                {
                    foreach(VehiclePawn vehicle in map.mapPawns.AllPawnsSpawned.Where(v => v is VehiclePawn vehicle && v.Faction == Faction.OfPlayer))
                    {
                        if(vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.Contains(p))
                        {
                            __result = WorldPawnSituation.CaravanMember;
                            return;
                        }
                    }
                }
                foreach(Caravan c in Find.WorldObjects.Caravans)
                {
                    foreach(VehiclePawn vehicle in c.PawnsListForReading.Where(v => v is VehiclePawn vehicle))
                    {
                        if(vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.Contains(p))
                        {
                            __result = WorldPawnSituation.CaravanMember;
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Prevent RimWorld Garbage Collection from removing DockedBoats as well as pawns onboard DockedBoat WorldObjects
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
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
                    if(innerPawn is VehiclePawn vehicle && vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.Contains(p))
                    {
                        
                        return false;
                    }
                }
            }
            return true;
        }

        #endregion WorldHandling

        #region Jobs
        //REDO
        /// <summary>
        /// Intercept Error Recover handler of no job, and assign idling for vehicle
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        /// <param name="concreteDriver"></param>
        /// <returns></returns>
        public static bool VehicleErrorRecoverJob(Pawn pawn, string message, Exception exception = null, JobDriver concreteDriver = null)
        {
            if(pawn.IsVehicle())
            {
                Log.Message("ErrorRecover");
                if (pawn.jobs != null)
                {
                    if (pawn.jobs.curJob != null)
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
                            pawn.jobs.StartJob(new Job(JobDefOf_Vehicles.IdleVehicle, 150, false), JobCondition.None, null, false, true, null, null, false);
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
            if(pawn is VehiclePawn)
            {
                Log.Message("Idle");
                __result = new Job(JobDefOf_Vehicles.IdleVehicle);
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

        public static bool ConfirmLeaveVehiclesOnReform(Dialog_FormCaravan __instance, ref List<TransferableOneWay> ___transferables, Map ___map, int ___destinationTile, ref bool __result)
        {
            if(HelperMethods.HasVehicle(___map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)))
            if(HelperMethods.HasVehicle(___map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)))
            {
                List<Pawn> pawns = TransferableUtility.GetPawnsFromTransferables(___transferables);
                List<Pawn> correctedPawns = pawns.Where(p => !p.IsVehicle()).ToList();
                string vehicles = "";
                foreach(Pawn pawn in pawns.Where(p => p.IsVehicle()))
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

        public static bool CapacityWithVehicle(List<ThingCount> thingCounts, ref float __result, StringBuilder explanation = null)
        {
            if(thingCounts.AnyNullified(x => HelperMethods.IsVehicle(x.Thing as Pawn)))
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

        public static void AcceptsVehicleRefuelable(Thing t, ref bool __result, ThingRequest __instance)
        {
            if(t is VehiclePawn vehicle && __instance.group == ThingRequestGroup.Refuelable)
            {
                __result = vehicle.GetCachedComp<CompFueledTravel>() != null;
            }
            if (__instance.group == ThingRequestGroup.Refuelable)
                Log.Message($"Checking {t.LabelShort} Result: {__result}");
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
                    yield return new CodeInstruction(opcode: OpCodes.Ldsfld, AccessTools.Field(typeof(JobDefOf_Vehicles), nameof(JobDefOf_Vehicles.CarryItemToVehicle)));
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

        //REDO
        public static void UpdateDutyOfVehicle(LordToil_PrepareCaravan_GatherAnimals __instance)
        {
            if(__instance.lord.LordJob is LordJob_FormAndSendVehicles)
            {
                List<Pawn> ships = __instance.lord.ownedPawns.Where(x => x.IsVehicle()).ToList();
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
                    if(pawn is VehiclePawn vehicle)
                    {
                        if(vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.AnyNullified())
                        {
                            num += vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.FindAll(x => x.IsFreeColonist).Count;
                            num2 += vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.FindAll(x => x.IsFreeColonist && x.InMentalState).Count;
                            num3 += vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.FindAll(x => x.IsPrisoner).Count;
                            num4 += vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.FindAll(x => x.IsPrisoner && x.InMentalState).Count;
                            num5 += vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.FindAll(x => x.RaceProps.Animal).Count;
                            num6 += vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.FindAll(x => x.RaceProps.Animal && x.InMentalState).Count;
                            num7 += vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.FindAll(x => x.RaceProps.Animal && x.RaceProps.packAnimal).Count;
                        }
                        if(!vehicle.GetCachedComp<CompVehicle>().beached)
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
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.HasVehicle), new Type[] { typeof(Caravan) }));
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
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.HasVehicle), new Type[] { typeof(Caravan) }));
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
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.HasVehicle), new Type[] { typeof(Caravan) }));
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
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(HelperMethods), nameof(HelperMethods.HasVehicle), new Type[] { typeof(Caravan) }));
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
            if(caravan.HasVehicle())
            {
                EnterMapUtilityVehicles.EnterAndSpawn(caravan, map, enterMode, dropInventoryMode, draftColonists, extraCellValidator);
                return false;
            }
            return true;
        }

        public static bool EnterMapVehiclesCatchAll2(Caravan caravan, Map map, Func<Pawn, IntVec3> spawnCellGetter, CaravanDropInventoryMode dropInventoryMode = CaravanDropInventoryMode.DoNotDrop, bool draftColonists = false)
        {
            if(caravan.HasVehicle())
            {
                EnterMapUtilityVehicles.EnterAndSpawn(caravan, map, CaravanEnterMode.Edge, dropInventoryMode, draftColonists, null);
                return false;
            }
            return true;
        }

        public static bool AllOwnersDownedVehicle(Caravan __instance, ref bool __result)
        {
            if(__instance.PawnsListForReading.AnyNullified(x => HelperMethods.IsVehicle(x)))
            {
                foreach (Pawn pawn in __instance.pawns)
                {
                    if(pawn is VehiclePawn vehicle && vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.All(x => x.Downed))
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
            if(__instance.PawnsListForReading.AnyNullified(x => HelperMethods.IsVehicle(x)))
            {
                foreach(Pawn pawn in __instance.pawns)
                {
                    if(pawn is VehiclePawn vehicle && vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.All(x => x.InMentalState))
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
                foreach(Pawn pawn in __instance.AllPawnsSpawned)
                {
                    if(pawn is VehiclePawn vehicle && vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.AnyNullified())
                    {
                        foreach (Pawn sailor in vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard)
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
            if(HelperMethods.HasVehicle(__instance) && !__instance.PawnsListForReading.AnyNullified(x => !HelperMethods.IsVehicle(x)))
            {
                __result = false;
                if(__instance.PawnsListForReading.AnyNullified(x => x is VehiclePawn vehicle && vehicle.GetCachedComp<CompVehicle>().navigationCategory == NavigationCategory.Manual))
                {
                    __result = __instance.Spawned && (!__instance.pather.Moving || __instance.pather.nextTile != __instance.pather.Destination || !Caravan_PathFollower.IsValidFinalPushDestination(__instance.pather.Destination) ||
                        Mathf.CeilToInt(__instance.pather.nextTileCostLeft / 1f) > 10000) && CaravanNightRestUtility.RestingNowAt(__instance.Tile);
                }
                else if(__instance.PawnsListForReading.AnyNullified(x => x is VehiclePawn vehicle && vehicle.GetCachedComp<CompVehicle>().navigationCategory == NavigationCategory.Opportunistic))
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
            if(HelperMethods.HasVehicle(__result))
            {
                List<Pawn> pawns = new List<Pawn>();
                foreach(Pawn pawn in __result)
                {
                    if(pawn is VehiclePawn vehicle)
                    {
                        pawns.AddRange(vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard);
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
            if(HelperMethods.HasVehicle(caravan))
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
                        inventoryItems.AddRange(vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.SelectMany(p => p.inventory.innerContainer));
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

        public static bool VehicleHealthTabPawns(ref List<Pawn> __result)
        {
            if (Find.WorldSelector.SingleSelectedObject is Caravan caravan && HelperMethods.HasVehicle(caravan))
            {
                List<Pawn> pawns = new List<Pawn>();
                foreach (Pawn p in caravan.PawnsListForReading)
                {
                    if (p is VehiclePawn vehicle)
                    {
                        pawns.AddRange(vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard);
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
                        pawns.AddRange(vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard);
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
            if(HelperMethods.HasVehicle(caravan))
            {
                float num = -1f;
                foreach(Pawn pawn in caravan.PawnsListForReading)
                {
                    if(pawn is VehiclePawn vehicle)
                    {
                        foreach(Pawn innerPawn in vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.Where(p => !p.Dead && !p.Downed && !p.InMentalState && caravan.IsOwner(p)))
                        {
                            float statValue = innerPawn.GetStatValue(stat, true);
                            if(__result is null || statValue > num)
                            {
                                __result = innerPawn;
                                num = statValue;
                            }
                        }
                    }
                    else if(!stat.Worker.IsDisabledFor(pawn))
                    {
                        float statValue = pawn.GetStatValue(stat, true);
                        if(__result is null || statValue > num)
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
                __result = __instance.PawnsListForReading.Any(v => v is VehiclePawn vehicle && vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.Contains(p));
            }
        }

        public static void IsOwnerOfVehicle(Pawn p, Caravan __instance, ref bool __result)
        {
            if(!__result)
            {
                __result = __instance.PawnsListForReading.Any(v => v is VehiclePawn vehicle && vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.Contains(p) && CaravanUtility.IsOwner(p, __instance.Faction));
            }
        }

        //REDO?
        public static void UnloadVehicleOfferGifts(Caravan caravan)
        {
            if(HelperMethods.HasVehicle(caravan))
            {
                HelperMethods.ToggleDocking(caravan, true);
            }
        }

        //REDO?
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
        #endregion Caravan

        #region Raids

        #endregion 

        #region Construction
        public static bool CompleteConstructionVehicle(Pawn worker, Frame __instance)
        {
            if (__instance.def.entityDefToBuild is VehicleBuildDef def && def.thingToSpawn != null)
            {
                VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(def.thingToSpawn, worker.Faction);
                __instance.resourceContainer.ClearAndDestroyContents(DestroyMode.Vanish);
                Map map = __instance.Map;
                __instance.Destroy(DestroyMode.Vanish);

                if (def.soundBuilt != null)
                {
                    def.soundBuilt.PlayOneShot(new TargetInfo(__instance.Position, map, false));
                }
                vehicle.SetFaction(worker.Faction);
                GenSpawn.Spawn(vehicle, __instance.Position, map, __instance.Rotation, WipeMode.FullRefund, false);
                worker.records.Increment(RecordDefOf.ThingsConstructed);

                vehicle.GetCachedComp<CompVehicle>().Rename();
                //Quality?
                //Art?
                //Tale RecordTale LongConstructionProject?
                return false;
            }
            return true;
        }

        public static bool Notify_RepairedVehicle(Building b, ListerBuildingsRepairable __instance)
        {
            if (b is VehicleBuilding building && b.def is VehicleBuildDef vehicleDef && vehicleDef.thingToSpawn != null)
            {
                if (b.HitPoints < b.MaxHitPoints)
                    return true;

                Pawn vehicle;
                if(building.vehicleReference != null)
                {
                    vehicle = building.vehicleReference;
                    vehicle.health.Reset();
                }
                else
                {
                    vehicle = PawnGenerator.GeneratePawn(vehicleDef.thingToSpawn);
                }
                
                Map map = b.Map;
                IntVec3 position = b.Position;
                Rot4 rotation = b.Rotation;

                AccessTools.Method(typeof(ListerBuildingsRepairable), "UpdateBuilding").Invoke(__instance, new object[] { b });
                if (vehicleDef.soundBuilt != null)
                {
                    vehicleDef.soundBuilt.PlayOneShot(new TargetInfo(position, map, false));
                }
                if(vehicle.Faction != Faction.OfPlayer)
                {
                    vehicle.SetFaction(Faction.OfPlayer);
                }
                b.Destroy(DestroyMode.Vanish);
                vehicle.ForceSetStateToUnspawned();
                GenSpawn.Spawn(vehicle, position, map, rotation, WipeMode.FullRefund, false);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Catch All for vehicles spawned in. Handles GodMode placing of vehicle buildings and corrects immovable spawn locations
        /// </summary>
        /// <param name="newThing"></param>
        /// <param name="loc"></param>
        /// <param name="map"></param>
        /// <param name="rot"></param>
        /// <param name="__result"></param>
        /// <param name="wipeMode"></param>
        /// <param name="respawningAfterLoad"></param>
        /// <returns></returns>
        public static bool SpawnVehicleGodMode(Thing newThing, ref IntVec3 loc, Map map, Rot4 rot, Thing __result, WipeMode wipeMode, bool respawningAfterLoad)
        {
            if (newThing.def is VehicleBuildDef def)
            {
                Log.Message("2");
                if (!VehicleMod.settings.debugSpawnVehicleBuildingGodMode)
                {
                    VehiclePawn vehiclePawn = VehicleSpawner.GenerateVehicle(def.thingToSpawn, newThing.Faction);// (VehiclePawn)PawnGenerator.GeneratePawn(def.thingToSpawn);
                    
                    if (def.soundBuilt != null)
                    {
                        def.soundBuilt.PlayOneShot(new TargetInfo(loc, map, false));
                    }
                    VehiclePawn vehicleSpawned = (VehiclePawn)GenSpawn.Spawn(vehiclePawn, loc, map, rot, WipeMode.FullRefund, false);
                    vehicleSpawned.GetCachedComp<CompVehicle>().Rename();
                    __result = vehicleSpawned;
                    return false;
                }
            }
            else if (newThing is VehiclePawn vehicle)
            {
                bool standable = true;
                foreach (IntVec3 c in vehicle.PawnOccupiedCells(loc, rot))
                {
                    if (!c.InBounds(map) || (vehicle.IsBoat() ? !GenGridShips.Standable(c, map) : !GenGrid.Standable(c, map)))
                    {
                        standable = false;
                        break;
                    }
                }
                bool validator(IntVec3 c)
                {
                    foreach (IntVec3 c2 in vehicle.PawnOccupiedCells(c, rot))
                    {
                        if (vehicle.IsBoat() ? !GenGridShips.Standable(c, map) : !GenGrid.Standable(c, map))
                            return false;
                    }
                    return true;
                }
                if (standable)
                    return true;
                if (!CellFinder.TryFindRandomCellNear(loc, map, 20, validator, out IntVec3 newLoc, 100))
                {
                    Log.Error($"Unable to find location to spawn {newThing.LabelShort} after 100 attempts. Aborting spawn.");
                    return false;
                }
                loc = newLoc;
            }
            else if (newThing is Pawn pawn && !pawn.Dead)
            {
                if (pawn.InsideVehicle(map))
                {
                    bool validator(IntVec3 c)
                    {
                        return GenGrid.Standable(c, map) && !pawn.InsideVehicle(map);
                    }
                    if (CellFinder.TryFindRandomCellNear(loc, map, 10, validator, out IntVec3 newLoc, 100))
                    {
                        loc = newLoc;
                    }
                }
            }
            else
            {
                //REDO
                //var things = map.thingGrid.ThingsListAt(loc);
                //if (things.AnyNullified(t => t is VehiclePawn))
                //{
                //    bool validator(IntVec3 c)
                //    {
                //        return GenGrid.Standable(c, map) && !map.thingGrid.ThingsListAt(c).AnyNullified(t => t is VehiclePawn);
                //    }
                //    if(CellFinder.TryFindRandomCellNear(loc, map, 20, validator, out IntVec3 newLoc, 100))
                //    {
                //        loc = newLoc;
                //    }
                //}
            }
            return true;
        }

        #endregion Construction

        #region Extra

        public static void FreeColonistsInVehiclesTransport(ref int __result, List<Pawn> ___pawnsSpawned)
        {
            List<VehiclePawn> vehicles = ___pawnsSpawned.Where(x => x is VehiclePawn vehicle && x.Faction == Faction.OfPlayer).Cast<VehiclePawn>().ToList();
            
            foreach(VehiclePawn vehicle in vehicles)
            {
                if(vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.AnyNullified(x => !x.Dead))
                    __result += vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.Count;
            }
        }

        public static void FreeHumanlikesSpawnedInVehicles(Faction faction, ref List<Pawn> __result, MapPawns __instance)
        {
            List<Pawn> innerPawns = __instance.SpawnedPawnsInFaction(faction).Where(p => p.IsVehicle()).SelectMany(v => (v as VehiclePawn).GetCachedComp<CompVehicle>().AllPawnsAboard).ToList();
            __result.AddRange(innerPawns);
        }

        public static void FreeHumanlikesInVehicles(Faction faction, ref List<Pawn> __result, MapPawns __instance)
        {
            List<Pawn> innerPawns = __instance.AllPawns.Where(p => p.Faction == faction && p.IsVehicle()).SelectMany(v => (v as VehiclePawn).GetCachedComp<CompVehicle>().AllPawnsAboard).ToList();
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
                        return !HelperMethods.MultiSelectClicker(___selected);
                    }
                }
            }
            return true;
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

        /// <summary>
        /// Debugging
        /// </summary>
        internal static List<WorldPath> debugLines = new List<WorldPath>();
        internal static List<Pair<int, int>> tiles = new List<Pair<int,int>>(); // Pair -> TileID : Cycle
        internal static readonly bool debug = false;
        internal static readonly bool drawPaths = false;
        internal const string LogLabel = "[Vehicles]";
    }
}