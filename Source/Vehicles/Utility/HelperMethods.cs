using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using Vehicles.AI;
using Vehicles.Lords;
using Vehicles.Defs;
using HarmonyLib;
using Vehicles.UI;

namespace Vehicles
{
    [StaticConstructorOnStartup]
    public static class HelperMethods
    {
        #region Pathing

        public static bool VehicleInCell(Map map, IntVec3 cell)
        {
            if (map.thingGrid.ThingsListAt(cell).AnyNullified(t => t is VehiclePawn))
            {
                return true;
            }
            return false;
        }

        public static bool VehicleInCell(Map map, int x, int z)
        {
            return VehicleInCell(map, new IntVec3(x, 0, z));
        }

        public static int CostToMoveIntoCellShips(Pawn pawn, IntVec3 c)
        {
            int num = (c.x == pawn.Position.x || c.z == pawn.Position.z) ? pawn.TicksPerMoveCardinal : pawn.TicksPerMoveDiagonal;
            num += pawn.Map.GetCachedMapComponent<WaterMap>().ShipPathGrid.CalculatedCostAt(c);
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

        public static float ShipAngle(Pawn pawn)
        {
            if (pawn is null) return 0f;
            VehiclePawn vehicle = pawn as VehiclePawn;

            if (vehicle.vPather.Moving)
            {
                IntVec3 c = vehicle.vPather.nextCell - vehicle.Position;
                if (c.x > 0 && c.z > 0)
                {
                    vehicle.Angle = -45f;
                }
                else if (c.x > 0 && c.z < 0)
                {
                    vehicle.Angle = 45f;
                }
                else if (c.x < 0 && c.z < 0)
                {
                    vehicle.Angle = -45f;
                }
                else if (c.x < 0 && c.z > 0)
                {
                    vehicle.Angle = 45f;
                }
                else
                {
                    vehicle.Angle = 0f;
                }
            }
            return vehicle.Angle;
        }

        public static bool OnDeepWater(this Pawn pawn)
        {
            //Splitting Caravan?
            if (pawn?.Map is null && pawn.IsWorldPawn())
                return false;
            return (pawn.Map.terrainGrid.TerrainAt(pawn.Position) == TerrainDefOf.WaterDeep || pawn.Map.terrainGrid.TerrainAt(pawn.Position) == TerrainDefOf.WaterMovingChestDeep ||
                pawn.Map.terrainGrid.TerrainAt(pawn.Position) == TerrainDefOf.WaterOceanDeep) && GenGrid.Impassable(pawn.Position, pawn.Map);
        }        

        #endregion Pathing

        #region SmoothPathing

        public static bool InitiateSmoothPath(this VehiclePawn boat, IntVec3 cell)
        {
            if (!cell.IsValid)
                return false;
            try
            {
                boat.GetCachedComp<CompVehicle>().EnqueueCellImmediate(cell);
                boat.GetCachedComp<CompVehicle>().CheckTurnSign();
            }
            catch(Exception ex)
            {
                Log.Error($"Unable to initiate Smooth Pathing. Cell: {cell} Boat: {boat.LabelShort} Exception: {ex.Message}");
                return false;
            }
            return true;
        }

        public static int NearestTurnSign(float bearingAngle, float angle)
        {
            if (bearingAngle == angle)
                return 0;
            float angleAbs = SPTrig.BearingToAbsoluteAngle(bearingAngle);

            float angleCheck = angle - angleAbs;
            float provisional = 0f;
            if (angleCheck < 180 && angleCheck > -180)
                provisional = angleCheck;
            else if (angleCheck > 180)
                provisional = angleCheck - 360;
            else if (angleCheck < -180)
                provisional = angleCheck + 360;
            if (provisional > 0)
                return 1; // right turn;
            if (provisional < 0)
                return -1; // left turn;
            int turn = Rand.RangeInclusive(1, 2);
            return turn == 1 ? 1 : -1;
        }

        #endregion SmoothPathing

        #region FeatureChecking
        //Thing -> ThingWithComps -> Pawn -> VehiclePawn
        public static bool IsBoat(this Pawn p) //REDO
        {
            return p is VehiclePawn vehicle && vehicle.GetCachedComp<CompVehicle>().Props.vehicleType == VehicleType.Sea;
        }

        [Obsolete("Switch to literal case")]
        public static bool IsVehicle(this Thing t)
        {
            return t is VehiclePawn;
        }

        public static bool HasVehicle(List<Pawn> pawns)
        {
            return pawns.AnyNullified(x => IsVehicle(x));
        }

        public static bool HasVehicle(IEnumerable<Pawn> pawns)
        {
            return pawns.AnyNullified(x => IsVehicle(x));
        }

        public static bool HasVehicle(this Caravan c)
        {
            return c is VehicleCaravan && (c is null) ? !(VehicleHarmony.currentFormingCaravan is null) && HasVehicle(TransferableUtility.GetPawnsFromTransferables(VehicleHarmony.currentFormingCaravan.transferables)) : HasVehicle(c?.PawnsListForReading);
        }

        public static bool HasVehicleInCaravan(Pawn p)
        {
            return p.IsFormingCaravan() && p.GetLord().LordJob is LordJob_FormAndSendVehicles && p.GetLord().ownedPawns.AnyNullified(x => IsVehicle(x));
        }

        public static bool HasBoat(this List<VehiclePawn> pawns)
        {
            return pawns?.AnyNullified(x => IsBoat(x)) ?? false;
        }

        public static bool HasBoat(IEnumerable<Pawn> pawns)
        {
            return pawns?.AnyNullified(x => IsBoat(x)) ?? false;
        }

        public static bool HasBoat(this Caravan c)
        {
            return (c is null) ? (VehicleHarmony.currentFormingCaravan is null) ? false : HasBoat(TransferableUtility.GetPawnsFromTransferables(VehicleHarmony.currentFormingCaravan.transferables)) : HasBoat(c?.PawnsListForReading);
        }

        public static bool HasUpgradeMenu(Pawn p)
        {
            return p?.TryGetComp<CompUpgradeTree>() != null;
        }

        //REDO
        public static bool HasEnoughSpacePawns(List<Pawn> pawns)
        {
            int num = 0;
            foreach (Pawn p in pawns)
            {
                if (p is VehiclePawn vehicle)
                {
                    num += vehicle.GetCachedComp<CompVehicle>().TotalSeats;
                }
            }
            return pawns.Where(x => !(x is VehiclePawn)).Count() <= num;
        }

        //REDO
        public static bool HasEnoughPawnsToEmbark(List<Pawn> pawns)
        {
            int num = 0;
            foreach (Pawn p in pawns)
            {
                if (p is VehiclePawn vehicle)
                {
                    num += vehicle.GetCachedComp<CompVehicle>().PawnCountToOperate;
                }
            }
            return pawns.Where(x => !(x is VehiclePawn)).Count() >= num;
        }

        public static bool AbleToEmbark(List<Pawn> pawns)
        {
            return HasEnoughSpacePawns(pawns) && HasEnoughPawnsToEmbark(pawns);
        }

        public static bool AbleToEmbark(Caravan caravan)
        {
            List<Pawn> pawns = new List<Pawn>();
            foreach (Pawn p in caravan.PawnsListForReading)
            {
                if (p is VehiclePawn vehicle)
                {
                    pawns.AddRange(vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard);
                }
                pawns.Add(p);
            }
            return AbleToEmbark(pawns);
        }

        public static bool HasCannons(this Pawn p)
        {
            return p is VehiclePawn vehicle && vehicle.GetCachedComp<CompCannons>() != null;
        }

        public static bool HasCannons(List<Pawn> pawns)
        {
            return pawns.All(x => x.HasCannons());
        }

        public static bool FueledVehicle(this Pawn p)
        {
            return p is VehiclePawn vehicle && vehicle.GetCachedComp<CompFueledTravel>() != null;
        }

        public static void ValidateAllVehicleDefs()
        {
            foreach(ThingDef vehicleDef in DefDatabase<ThingDef>.AllDefs.Where(v => v.GetCompProperties<CompProperties_Vehicle>() != null))
            {
                var props = vehicleDef.GetCompProperties<CompProperties_Vehicle>();
				if (props.customBiomeCosts is null)
					props.customBiomeCosts = new Dictionary<BiomeDef, float>();
                if (props.customHillinessCosts is null)
                    props.customHillinessCosts = new Dictionary<Hilliness, float>();
                if (props.customRoadCosts is null)
                    props.customRoadCosts = new Dictionary<RoadDef, float>();
                if (props.customTerrainCosts is null)
                    props.customTerrainCosts = new Dictionary<TerrainDef, int>();
                if (props.customThingCosts is null)
                    props.customThingCosts = new Dictionary<ThingDef, int>();

				if(props.vehicleType == VehicleType.Sea)
                {
					if(!props.customBiomeCosts.ContainsKey(BiomeDefOf.Ocean))
						props.customBiomeCosts.Add(BiomeDefOf.Ocean, 1);
					if(!props.customBiomeCosts.ContainsKey(BiomeDefOf.Lake))
						props.customBiomeCosts.Add(BiomeDefOf.Lake, 1);
                }
            }
        }
        #endregion FeatureChecking

        #region WorldMap

        public static float VehicleWorldSpeedMultiplier(this ThingDef vehicleDef)
        {
			return vehicleDef.GetCompProperties<CompProperties_Vehicle>().worldSpeedMultiplier;
        }

        public static bool IsWaterTile(int tile, List<Pawn> pawns = null)
        {
            return WaterCovered(tile) || Find.World.CoastDirectionAt(tile).IsValid || RiverIsValid(tile, pawns.Where(x => IsBoat(x)).ToList());
        }

        public static bool WaterCovered(int tile)
        {
            return Find.WorldGrid[tile].biome == BiomeDefOf.Ocean || Find.WorldGrid[tile].biome == BiomeDefOf.Lake;
        }

        public static bool CoastalTravel(this ThingDef vehicleDef, int tile)
        {
            return (vehicleDef.GetCompProperties<CompProperties_Vehicle>().vehicleType == VehicleType.Sea || 
                (vehicleDef.GetCompProperties<CompProperties_Vehicle>().customBiomeCosts.ContainsKey(BiomeDefOf.Ocean) && vehicleDef.GetCompProperties<CompProperties_Vehicle>().customBiomeCosts[BiomeDefOf.Ocean] <= WorldVehiclePathGrid.ImpassableMovementDifficulty) ) &&
                Find.World.CoastDirectionAt(tile).IsValid;
        }

        public static void ToggleDocking(Caravan caravan, bool dock = false)
        {
            if (HasBoat(caravan))
            {
                if (!dock)
                {
                    BoardAllCaravanPawns(caravan);
                }
                else
                {
                    List<VehiclePawn> ships = caravan.PawnsListForReading.Where(x => IsBoat(x)).Cast<VehiclePawn>().ToList();
                    ships.ForEach(b => b.GetCachedComp<CompVehicle>().DisembarkAll());
                }
            }
        }

        public static void SpawnDockedBoatObject(Caravan caravan)
        {
            if (!HasBoat(caravan))
                Log.Error("Attempted to dock boats with no boats in caravan. This could have serious errors in the future. - Smash Phil");

            ToggleDocking(caravan, true);
            Find.WindowStack.Add(new Dialog_DockBoat(caravan));
        }

        public static void BoardAllCaravanPawns(Caravan caravan)
        {
            if (!AbleToEmbark(caravan))
            {
                if (caravan.pather.Moving)
                    caravan.pather.StopDead();
                Messages.Message("CantMoveDocked".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<Pawn> sailors = caravan.PawnsListForReading.Where(x => !IsBoat(x)).ToList();
            List<VehiclePawn> ships = caravan.PawnsListForReading.Where(x => IsBoat(x)).Cast<VehiclePawn>().ToList();
            foreach(VehiclePawn ship in ships)
            { 
                for (int j = 0; j < ship.GetCachedComp<CompVehicle>().PawnCountToOperate; j++)
                {
                    if (sailors.Count <= 0)
                    {
                        return;
                    }
                    foreach (VehicleHandler handler in ship.GetCachedComp<CompVehicle>().handlers)
                    {
                        if (handler.AreSlotsAvailable)
                        {
                            ship.GetCachedComp<CompVehicle>().Notify_BoardedCaravan(sailors.Pop(), handler.handlers);
                            break;
                        }
                    }
                }
            }
            if (sailors.Count > 0)
            {
                int x = 0;
                while (sailors.Count > 0)
                {
                    VehiclePawn ship = ships[x];
                    foreach (VehicleHandler handler in ship.GetCachedComp<CompVehicle>().handlers)
                    {
                        if (handler.AreSlotsAvailable)
                        {
                            ship.GetCachedComp<CompVehicle>().Notify_BoardedCaravan(sailors.Pop(), handler.handlers);
                            break;
                        }
                    }
                    x = (x + 2) > ships.Count ? 0 : ++x;
                }
            }
        }

        public static void BoardAllAssignedPawns(ref List<Pawn> pawns)
        {
            List<VehiclePawn> vehicles = pawns.Where(p => p.IsVehicle()).Cast<VehiclePawn>().ToList();
            List<Pawn> nonVehicles = pawns.Where(p => !p.IsVehicle()).ToList();
            foreach(Pawn pawn in nonVehicles)
            {
                if(assignedSeats.ContainsKey(pawn) && vehicles.Contains(assignedSeats[pawn].First))
                {
                    assignedSeats[pawn].First.GetCachedComp<CompVehicle>().GiveLoadJob(pawn, assignedSeats[pawn].Second);
                    assignedSeats[pawn].First.GetCachedComp<CompVehicle>().Notify_Boarded(pawn);
                    pawns.Remove(pawn);
                }
            }
        }

        public static bool RiverIsValid(int tileID, List<Pawn> ships)
        {
            if (!VehicleMod.settings.riverTravel || ships is null || !ships.AnyNullified(x => IsBoat(x)))
                return false;
            bool flag = VehicleMod.settings.boatSizeMatters ? (!Find.WorldGrid[tileID].Rivers.NullOrEmpty()) ? ShipsFitOnRiver(BiggestRiverOnTile(Find.WorldGrid[tileID]?.Rivers).river, ships) : false : (Find.WorldGrid[tileID].Rivers?.AnyNullified() ?? false);
            return flag;
        }

        public static Tile.RiverLink BiggestRiverOnTile(List<Tile.RiverLink> list)
        {
            return list.MaxBy(x => x.river.widthOnMap);
        }

        public static bool ShipsFitOnRiver(RiverDef river, List<Pawn> pawns)
        {
            foreach (Pawn p in pawns.Where(x => IsBoat(x)))
            {
                if ((p.def.GetCompProperties<CompProperties_Vehicle>()?.riverTraversability?.widthOnMap ?? int.MaxValue) > river.widthOnMap)
                    return false;
            }
            return true;
        }

        public static int BestGotoDestForVehicle(this Caravan c, int tile)
        {
            Predicate<int> predicate = (int t) => c.UniqueVehicleDefsInCaravan().All(v => Find.World.GetCachedWorldComponent<WorldVehiclePathGrid>().Passable(t, v)) && Find.World.GetCachedWorldComponent<WorldVehicleReachability>().CanReach(c, t);
			if (predicate(tile))
			{
				return tile;
			}
			GenWorldClosest.TryFindClosestTile(tile, predicate, out int result, 50, true);
			return result;
        }

        #endregion WorldMap

        #region CaravanFormation
        public static bool CanStartCaravan(List<Pawn> caravan)
        {
            int seats = 0;
            int pawns = 0;
            int prereq = 0;
            bool flag = caravan.AnyNullified(x => IsBoat(x)); //Ships or No Ships

            foreach (Pawn p in caravan)
            {
                if (p is VehiclePawn vehicle)
                {
                    seats += vehicle.GetCachedComp<CompVehicle>().SeatsAvailable;
                    prereq += vehicle.GetCachedComp<CompVehicle>().PawnCountToOperate - vehicle.GetCachedComp<CompVehicle>().AllCrewAboard.Count;
                }
                else if (p.IsColonistPlayerControlled && !p.Downed && !p.Dead)
                {
                    pawns++;
                }
            }

            bool flag2 = flag ? pawns > seats : false; //Not Enough Room, must board all pawns
            bool flag3 = pawns < prereq;
            if (flag2)
                Messages.Message("CaravanMustHaveEnoughSpaceOnShip".Translate(), MessageTypeDefOf.RejectInput, false);
            if (flag3)
                Messages.Message("CaravanMustHaveEnoughPawnsToOperate".Translate(prereq), MessageTypeDefOf.RejectInput, false);
            return !flag2 && !flag3;
        }

        public static void CreateVehicleCaravanTransferableWidgets(List<TransferableOneWay> transferables, out TransferableOneWayWidget pawnsTransfer, out TransferableVehicleWidget vehiclesTransfer, out TransferableOneWayWidget itemsTransfer, string thingCountTip, IgnorePawnsInventoryMode ignorePawnInventoryMass, Func<float> availableMassGetter, bool ignoreSpawnedCorpsesGearAndInventoryMass, int tile, bool playerPawnsReadOnly = false)
        {
	        pawnsTransfer = new TransferableOneWayWidget(null, null, null, thingCountTip, true, ignorePawnInventoryMass, false, availableMassGetter, 0f, ignoreSpawnedCorpsesGearAndInventoryMass, tile, true, true, true, false, true, false, playerPawnsReadOnly);
            vehiclesTransfer = new TransferableVehicleWidget(null, null, null, thingCountTip, true, ignorePawnInventoryMass, false, availableMassGetter, 0f, ignoreSpawnedCorpsesGearAndInventoryMass, tile, true, false, false, playerPawnsReadOnly);
	        AddVehicleAndPawnSections(pawnsTransfer, vehiclesTransfer, transferables);
	        itemsTransfer = new TransferableOneWayWidget(from x in transferables
	        where x.ThingDef.category != ThingCategory.Pawn
	        select x, null, null, thingCountTip, true, ignorePawnInventoryMass, false, availableMassGetter, 0f, ignoreSpawnedCorpsesGearAndInventoryMass, tile, true, false, false, true, false, true, false);
        }

        public static void AddVehicleAndPawnSections(TransferableOneWayWidget pawnWidget, TransferableVehicleWidget vehicleWidget, List<TransferableOneWay> transferables)
        {
	        IEnumerable<TransferableOneWay> source = from x in transferables
	        where x.ThingDef.category == ThingCategory.Pawn
	        select x;
            vehicleWidget.AddSection("VehiclesTab".Translate(), from x in source
            where x.AnyThing is VehiclePawn vehicle && !vehicle.OnDeepWater()
            select x);
	        pawnWidget.AddSection("ColonistsSection".Translate(), from x in source
	        where ((Pawn)x.AnyThing).IsFreeColonist
	        select x);
	        pawnWidget.AddSection("PrisonersSection".Translate(), from x in source
	        where ((Pawn)x.AnyThing).IsPrisoner
	        select x);
	        pawnWidget.AddSection("CaptureSection".Translate(), from x in source
	        where ((Pawn)x.AnyThing).Downed && CaravanUtility.ShouldAutoCapture((Pawn)x.AnyThing, Faction.OfPlayer)
	        select x);
	        pawnWidget.AddSection("AnimalsSection".Translate(), from x in source
	        where ((Pawn)x.AnyThing).RaceProps.Animal
	        select x);
            vehicleWidget.AvailablePawns = source.Where(x => !IsVehicle(x.AnyThing as Pawn) && (x.AnyThing as Pawn).IsColonistPlayerControlled).ToList();
        }

        public static void DoCountAdjustInterface(Rect rect, Transferable trad, List<TransferableOneWay> pawns, int index, int min, int max, bool flash = false, List<TransferableCountToTransferStoppingPoint> extraStoppingPoints = null, bool readOnly = false)
        {
	        var stoppingPoints = new List<TransferableCountToTransferStoppingPoint>();
	        if (extraStoppingPoints != null)
	        {
		        stoppingPoints.AddRange(extraStoppingPoints);
	        }
	        for (int i = stoppingPoints.Count - 1; i >= 0; i--)
	        {
		        if (stoppingPoints[i].threshold != 0 && (stoppingPoints[i].threshold <= min || stoppingPoints[i].threshold >= max))
		        {
			        stoppingPoints.RemoveAt(i);
		        }
	        }
	        bool flag = false;
	        for (int j = 0; j < stoppingPoints.Count; j++)
	        {
		        if (stoppingPoints[j].threshold == 0)
		        {
			        flag = true;
			        break;
		        }
	        }
	        if (!flag)
	        {
		        stoppingPoints.Add(new TransferableCountToTransferStoppingPoint(0, "0", "0"));
	        }
	        DoCountAdjustInterfaceInternal(rect, trad, pawns, stoppingPoints, index, min, max, flash, readOnly);
        }

        private static void DoCountAdjustInterfaceInternal(Rect rect, Transferable trad, List<TransferableOneWay> pawns, List<TransferableCountToTransferStoppingPoint> stoppingPoints, int index, int min, int max, bool flash, bool readOnly)
        {
            
	        rect = rect.Rounded();
	        Rect rect2 = new Rect(rect.center.x - 45f, rect.center.y - 12.5f, 90f, 25f).Rounded();
	        if (flash)
	        {
		        GUI.DrawTexture(rect2, TransferableUIUtility.FlashTex);
	        }
	        TransferableOneWay transferableOneWay = trad as TransferableOneWay;

		    bool flag3 = trad.CountToTransfer != 0;
		    bool flag4 = flag3;

		    Rect buttonRect = new Rect(rect2.x, rect2.y, 120f, rect.height);
			if(Widgets.ButtonText(buttonRect, "AssignSeats".Translate()))
            {
				Find.WindowStack.Add(new Dialog_AssignSeats(pawns, transferableOneWay));
            }
            Rect checkboxRect = new Rect(buttonRect.x + buttonRect.width + 5f, buttonRect.y, 24f, 24f);
            if(Widgets.ButtonImage(checkboxRect, flag4 ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex))
            {
                if (!flag4)
                {
                    Find.WindowStack.Add(new Dialog_AssignSeats(pawns, transferableOneWay));
                }
                else
                {
                    foreach(Pawn pawn in (trad.AnyThing as VehiclePawn).GetCachedComp<CompVehicle>().AllPawnsAboard)
                    {
                        if (assignedSeats.ContainsKey(pawn))
                            assignedSeats.Remove(pawn);
                    }
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    flag4 = !flag4;
                }
            }

		    if (flag4 != flag3)
		    {
			    if (flag4)
			    {
				    trad.AdjustTo(trad.GetMaximumToTransfer());
			    }
			    else
			    {
				    trad.AdjustTo(trad.GetMinimumToTransfer());
			    }
		    }
	        if (trad.CountToTransfer != 0)
	        {
		        Rect position = new Rect(rect2.x + rect2.width / 2f - (float)(TradeArrow.width / 2), rect2.y + rect2.height / 2f - (float)(TradeArrow.height / 2), (float)TradeArrow.width, (float)TradeArrow.height);
		        TransferablePositiveCountDirection positiveCountDirection2 = trad.PositiveCountDirection;
		        if ((positiveCountDirection2 == TransferablePositiveCountDirection.Source && trad.CountToTransfer > 0) || (positiveCountDirection2 == TransferablePositiveCountDirection.Destination && trad.CountToTransfer < 0))
		        {
			        position.x += position.width;
			        position.width *= -1f;
		        }
		        //GUI.DrawTexture(position, TradeArrow); //REDO?
	        }
        }

        public static void DrawVehicleTransferableInfo(Transferable trad, Rect idRect, Color labelColor)
        {
	        if (!trad.HasAnyThing && trad.IsThing)
	        {
		        return;
	        }
	        if (Mouse.IsOver(idRect))
	        {
		        Widgets.DrawHighlight(idRect);
	        }
	        Rect rect = new Rect(0f, 0f, 27f, 27f);
	        if (trad.IsThing)
	        {
                Widgets.ThingIcon(rect, trad.AnyThing, 1f);
	        }
	        else
	        {
		        trad.DrawIcon(rect);
	        }
	        if (trad.IsThing)
	        {
		        //Widgets.InfoCardButton(40f, 0f, trad.AnyThing);
	        }
	        Text.Anchor = TextAnchor.MiddleLeft;
	        Rect rect2 = new Rect(40f, 0f, idRect.width - 80f, idRect.height);
	        Text.WordWrap = false;
	        GUI.color = labelColor;
	        Widgets.Label(rect2, trad.LabelCap);
	        GUI.color = Color.white;
	        Text.WordWrap = true;
	        if (Mouse.IsOver(idRect))
	        {
		        Transferable localTrad = trad;
		        TooltipHandler.TipRegion(idRect, new TipSignal(delegate()
		        {
			        if (!localTrad.HasAnyThing && localTrad.IsThing)
			        {
				        return "";
			        }
			        string text = localTrad.LabelCap;
			        string tipDescription = localTrad.TipDescription;
			        if (!tipDescription.NullOrEmpty())
			        {
				        text = text + ": " + tipDescription;
			        }
			        return text;
		        }, localTrad.GetHashCode()));
	        }
        }

        public static VehicleCaravan ExitMapAndCreateVehicleCaravan(IEnumerable<Pawn> pawns, Faction faction, int exitFromTile, int directionTile, int destinationTile, bool sendMessage = true)
		{
			if (!GenWorldClosest.TryFindClosestPassableTile(exitFromTile, out exitFromTile))
			{
				Log.Error("Could not find any passable tile for a new caravan.", false);
				return null;
			}
			if (Find.World.Impassable(directionTile))
			{
				directionTile = exitFromTile;
			}

			List<Pawn> tmpPawns = new List<Pawn>();
            tmpPawns.AddRange(pawns);

			Map map = null;
			for (int i = 0; i < tmpPawns.Count; i++)
			{
				AddCaravanExitTaleIfShould(tmpPawns[i]);
				map = tmpPawns[i].MapHeld;
				if (map != null)
				{
					break;
				}
			}
			VehicleCaravan caravan = MakeVehicleCaravan(tmpPawns, faction, exitFromTile, false);
			Rot4 exitDir = (map != null) ? Find.WorldGrid.GetRotFromTo(exitFromTile, directionTile) : Rot4.Invalid;
			for (int j = 0; j < tmpPawns.Count; j++)
			{
				tmpPawns[j].ExitMap(false, exitDir);
			}
			List<Pawn> pawnsListForReading = caravan.PawnsListForReading;
			for (int k = 0; k < pawnsListForReading.Count; k++)
			{
				if (!pawnsListForReading[k].IsWorldPawn())
				{
					Find.WorldPawns.PassToWorld(pawnsListForReading[k], PawnDiscardDecideMode.Decide);
				}
			}
			if (map != null)
			{
				map.Parent.Notify_CaravanFormed(caravan);
				map.retainedCaravanData.Notify_CaravanFormed(caravan);
			}
			if (!caravan.vPather.Moving && caravan.Tile != directionTile)
			{
				caravan.vPather.StartPath(directionTile, null, true, true);
				caravan.vPather.nextTileCostLeft /= 2f;
				caravan.tweener.ResetTweenedPosToRoot();
			}
			if (destinationTile != -1)
			{
				List<FloatMenuOption> list = FloatMenuMakerWorld.ChoicesAtFor(destinationTile, caravan);
				if (list.AnyNullified((FloatMenuOption x) => !x.Disabled))
				{
					list.First((FloatMenuOption x) => !x.Disabled).action();
				}
				else
				{
					caravan.vPather.StartPath(destinationTile, null, true, true);
				}
			}
			if (sendMessage)
			{
				TaggedString taggedString = "MessageFormedCaravan".Translate(caravan.Name).CapitalizeFirst();
				if (caravan.vPather.Moving && caravan.vPather.ArrivalAction != null)
				{
					taggedString += " " + "MessageFormedCaravan_Orders".Translate() + ": " + caravan.vPather.ArrivalAction.Label + ".";
				}
				Messages.Message(taggedString, caravan, MessageTypeDefOf.TaskCompletion, true);
			}
			return caravan;
		}

        public static void AddCaravanExitTaleIfShould(Pawn pawn)
        {
	        if (pawn.Spawned && pawn.IsFreeColonist)
	        {
		        if (pawn.Map.IsPlayerHome)
		        {
			        TaleRecorder.RecordTale(TaleDefOf.CaravanFormed, new object[]
			        {
				        pawn
			        });
			        return;
		        }
		        if (GenHostility.AnyHostileActiveThreatToPlayer(pawn.Map, false))
		        {
			        TaleRecorder.RecordTale(TaleDefOf.CaravanFled, new object[]
			        {
				        pawn
			        });
		        }
	        }
        }

        public static VehicleCaravan MakeVehicleCaravan(IEnumerable<Pawn> pawns, Faction faction, int startingTile, bool addToWorldPawnsIfNotAlready)
		{
			if (startingTile < 0 && addToWorldPawnsIfNotAlready)
			{
				Log.Warning("Tried to create a caravan but chose not to spawn a caravan but pass pawns to world. This can cause bugs because pawns can be discarded.", false);
			}
            List<Pawn> tmpPawns = new List<Pawn>();
            tmpPawns.AddRange(pawns);

			VehicleCaravan caravan = (VehicleCaravan)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfVehicles.VehicleCaravan);
			if (startingTile >= 0)
			{
				caravan.Tile = startingTile;
			}
			caravan.SetFaction(faction);
			if (startingTile >= 0)
			{
				Find.WorldObjects.Add(caravan);
			}
			for (int i = 0; i < tmpPawns.Count; i++)
			{
				Pawn pawn = tmpPawns[i];
				if (pawn.Dead)
				{
					Log.Warning("Tried to form a caravan with a dead pawn " + pawn, false);
				}
				else
				{
					caravan.AddPawn(pawn, addToWorldPawnsIfNotAlready);
					if (addToWorldPawnsIfNotAlready && !pawn.IsWorldPawn())
					{
						Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
					}
				}
			}
			caravan.Name = CaravanNameGenerator.GenerateCaravanName(caravan);
			caravan.SetUniqueId(Find.UniqueIDsManager.GetNextCaravanID());
			return caravan;
		}

        #endregion CaravanFormation

        #region Data

        public static bool InsideVehicle(this Pawn pawn, Map map)
        {
            var vehicles = map.mapPawns.AllPawns.Where(p => p is VehiclePawn);
            foreach(VehiclePawn vehicle in vehicles)
            {
                if (vehicle.PawnOccupiedCells(vehicle.Position, vehicle.Rotation).Contains(pawn.Position))
                {
                    return true;
                }
            }
            return false;
        }

        public static VehicleCaravan GetVehicleCaravan(this Pawn pawn)
        {
            return pawn.ParentHolder as VehicleCaravan;
        }

        public static List<Pawn> GrabPawnsFromMapPawnsInVehicle(List<Pawn> allPawns)
        {
            List<VehiclePawn> playerShips = allPawns.Where(x => x.Faction == Faction.OfPlayer && x is VehiclePawn).Cast<VehiclePawn>().ToList();
            if (!playerShips.AnyNullified())
                return allPawns.Where(x => x.Faction == Faction.OfPlayer && x.RaceProps.Humanlike).ToList();
            return playerShips.RandomElement().GetCachedComp<CompVehicle>()?.AllCapablePawns;
        }

        public static List<Pawn> GrabPawnsFromVehicles(List<Pawn> ships)
        {
            if (!ships.AnyNullified(x => x is VehiclePawn))
                return null;
            List<Pawn> pawns = new List<Pawn>();
            foreach (Pawn pawn in ships)
            {
                if (pawn is VehiclePawn vehicle)
                    pawns.AddRange(vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard);
                else
                    pawns.Add(pawn);
            }
            return pawns;
        }

        public static List<Pawn> GrabPawnsIfVehicles(List<Pawn> pawns)
        {
            if (pawns is null)
                return null;
            if (!HasVehicle(pawns))
            {
                return pawns;
            }
            List<Pawn> ships = new List<Pawn>();
            foreach (Pawn p in pawns)
            {
                if (p is VehiclePawn vehicle)
                    ships.AddRange(vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard);
                else
                    ships.Add(p);
            }
            return ships;
        }

        public static List<Pawn> GrabPawnsFromVehicleCaravanSilentFail(this Caravan caravan)
        {
            if (caravan is null || !HasVehicle(caravan))
                return null;
            List<Pawn> vehicles = new List<Pawn>();
            foreach (Pawn p in caravan.PawnsListForReading)
            {
                if (p is VehiclePawn vehicle)
                    vehicles.AddRange(vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard);
                else
                    vehicles.Add(p);
            }
            return vehicles;
        }

        public static List<Pawn> GrabBoatsFromCaravans(List<Caravan> caravans)
        {
            if (!caravans.All(x => HasBoat(x)))
                return null;
            List<Pawn> ships = new List<Pawn>();
            foreach (Caravan c in caravans)
            {
                ships.AddRange(c.PawnsListForReading.Where(x => IsBoat(x)));
            }
            return ships;
        }

        public static HashSet<ThingDef> UniqueVehicleDefsInCaravan(this Caravan c)
        {
            var vehicleSet = new HashSet<ThingDef>();
            foreach(Pawn p in c.PawnsListForReading.Where(v => v is VehiclePawn))
            {
                vehicleSet.Add(p.def);
            }
            return vehicleSet;
        }

        public static HashSet<ThingDef> UniqueVehicleDefsInList(this List<VehiclePawn> vehicles)
        {
            return vehicles.Select(v => v.def).Distinct().ToHashSet();
        }

        public static bool IsFormingCaravanShipHelper(Pawn p)
        {
            Lord lord = p.GetLord();
            return !(lord is null) && lord.LordJob is LordJob_FormAndSendVehicles;
        }

        public static List<Pawn> ExtractPawnsFromCaravan(Caravan caravan)
        {
            List<Pawn> innerPawns = new List<Pawn>();
            foreach (Pawn pawn in caravan.PawnsListForReading)
            {
                if (pawn is VehiclePawn vehicle)
                {
                    innerPawns.AddRange(vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard);
                }
            }
            return innerPawns;
        }

        public static float CapacityLeft(LordJob_FormAndSendVehicles lordJob)
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

        public static float ExtractUpgradeValue(VehiclePawn vehicle, StatUpgradeCategory stat)
        {
            if(HasUpgradeMenu(vehicle))
            {
                switch(stat)
                {
                    case StatUpgradeCategory.Armor:
                        return vehicle.GetCachedComp<CompVehicle>().ArmorPoints;
                    case StatUpgradeCategory.Speed:
                        return vehicle.GetCachedComp<CompVehicle>().MoveSpeedModifier;
                    case StatUpgradeCategory.CargoCapacity:
                        return vehicle.GetCachedComp<CompVehicle>().CargoCapacity;
                    case StatUpgradeCategory.FuelConsumptionRate:
                        return vehicle.GetCachedComp<CompFueledTravel>()?.FuelEfficiency ?? 0f;
                    case StatUpgradeCategory.FuelCapacity:
                        return vehicle.GetCachedComp<CompFueledTravel>()?.FuelCapacity ?? 0f;
                }
            }
            return 0f;
        }

        public static List<Pawn> GetVehiclesForColonistBar(Map map)
        {
            List<Pawn> vehicles = new List<Pawn>();
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn is VehiclePawn vehicle)
                {
                    //ships.Add(ship); /*  Uncomment to add Ships to colonist bar  */
                    foreach (Pawn p in vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard)
                    {
                        if (p.IsColonist)
                            vehicles.Add(p);
                    }
                }
            }
            return vehicles;
        }

        public static IEnumerable<Pawn> UsableCandidatesForCargo(Pawn pawn)
        {
            IEnumerable<Pawn> candidates = (!pawn.IsFormingCaravan()) ? pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction) : pawn.GetLord().ownedPawns;
            candidates = from x in candidates
                         where x is VehiclePawn
                         select x;
            return candidates;
        }

        public static Pawn UsableVehicleWithTheMostFreeSpace(Pawn pawn)
        {
            
            Pawn carrierPawn = null;
            float num = 0f;
            foreach(Pawn p in UsableCandidatesForCargo(pawn))
            {
                if(p is VehiclePawn && p != pawn && pawn.CanReach(p, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
                {
                    float num2 = MassUtility.FreeSpace(p);
                    if(carrierPawn is null || num2 > num)
                    {
                        carrierPawn = p;
                        num = num2;
                    }
                }
            }
            return carrierPawn;
        }

        public static IEnumerable<T> GetEnumerableOfType<T>(params object[] constructorArgs) where T : class
        {
            List<T> objects = new List<T>();
            foreach (Type type in 
                Assembly.GetAssembly(typeof(T)).GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T))))
            {
                objects.Add((T)Activator.CreateInstance(type, constructorArgs));
            }
            objects.Sort();
            return objects;
        }

        public static int CountAssignedToVehicle(this VehiclePawn vehicle)
        {
            return assignedSeats.Where(a => a.Value.First == vehicle).Select(s => s.Key).Count();
        }

        #endregion Data

        #region Selector
        public static bool MultiSelectClicker(List<object> selectedObjects)
        {
            if (!selectedObjects.All(x => x is Pawn))
                return false;
            List<Pawn> selPawns = new List<Pawn>();
            foreach(object o in selectedObjects)
            {
                if(o is Pawn)
                    selPawns.Add(o as Pawn);
            }
            if (selPawns.AnyNullified(x => x.Faction != Faction.OfPlayer || IsVehicle(x)))
                return false;
            IntVec3 mousePos = Verse.UI.MouseMapPosition().ToIntVec3();
            if (selectedObjects.Count > 1 && selectedObjects.All(x => x is Pawn))
            {
                foreach (Thing thing in selPawns[0].Map.thingGrid.ThingsAt(mousePos))
                {
                    if (IsVehicle(thing))
                    {
                        (thing as VehiclePawn).GetCachedComp<CompVehicle>().MultiplePawnFloatMenuOptions(selPawns);
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion Selector

        #region Rendering
        public static SPTuple2<float,float> ShipDrawOffset(VehiclePawn vehicle, float xOffset, float yOffset, out SPTuple2<float, float> rotationOffset, float turretRotation = 0, CannonHandler attachedTo = null)
        {
            rotationOffset = SPTuple2<float,float>.zero; //COME BACK TO
            if(attachedTo != null)
            {
                return SPTrig.RotatePointClockwise(attachedTo.cannonRenderLocation.x + xOffset, attachedTo.cannonRenderLocation.y + yOffset, turretRotation);
            }
            
            switch(vehicle.Rotation.AsInt)
            {
                //East
                case 1:
                    if(vehicle.Angle == 45)
                    {
                        return SPTrig.RotatePointClockwise(yOffset, -xOffset, 45f);
                    }
                    else if(vehicle.Angle == -45)
                    {
                        return SPTrig.RotatePointCounterClockwise(yOffset, -xOffset, 45f);
                    }
                    return new SPTuple2<float, float>(yOffset, -xOffset);
                //South
                case 2:
                    return new SPTuple2<float, float>(-xOffset, -yOffset);
                //West
                case 3:
                    if(vehicle.Angle != 0)
                    {
                        if(vehicle.Angle == 45)
                        {
                            return SPTrig.RotatePointClockwise(-yOffset, xOffset, 45f);
                        }
                        else if(vehicle.Angle == -45)
                        {
                            return SPTrig.RotatePointCounterClockwise(-yOffset, xOffset, 45f);
                        }
                    }
                    return new SPTuple2<float, float>(-yOffset, xOffset);
                //North
                default:
                    return new SPTuple2<float, float>(xOffset, yOffset);
            }
        }

        public static float CustomFloatBeach(Map map)
        {
            float mapSizeMultiplier = (float)(map.Size.x >= map.Size.z ? map.Size.x : map.Size.z) / 250f;
            float beach = 60f; //Rand.Range(40f, 80f);
            return (float)(beach + (beach * (VehicleMod.settings.beachMultiplier) / 100f)) * mapSizeMultiplier;
        }

        public static int PushSettlementToCoast(int tileID, Faction faction)
        {
            List<int> neighbors = new List<int>();
            Stack<int> stack = new Stack<int>();
            stack.Push(tileID);
            Stack<int> stackFull = stack;
            List<int> newTilesSearch = new List<int>();
            List<int> allSearchedTiles = new List<int>() { tileID };
            int searchTile;
            int searchedRadius = 0;

            if (Find.World.CoastDirectionAt(tileID).IsValid)
            {
                if (Find.WorldGrid[tileID].biome.canBuildBase && !(faction is null))
                    VehicleHarmony.tiles.Add(new Pair<int, int>(tileID, 0));
                return tileID;
            }

            while (searchedRadius < VehicleMod.settings.CoastRadius)
            {
                for (int j = 0; j < stackFull.Count; j++)
                {
                    searchTile = stack.Pop();
                    SPExtra.GetList<int>(Find.WorldGrid.tileIDToNeighbors_offsets, Find.WorldGrid.tileIDToNeighbors_values, searchTile, neighbors);
                    int count = neighbors.Count;
                    for (int i = 0; i < count; i++)
                    {
                        if (allSearchedTiles.AnyNullified(x => x == neighbors[i]))
                            continue;
                        newTilesSearch.Add(neighbors[i]);
                        allSearchedTiles.Add(neighbors[i]);
                        if (Find.World.CoastDirectionAt(neighbors[i]).IsValid)
                        {
                            if (Find.WorldGrid[neighbors[i]].biome.canBuildBase && Find.WorldGrid[neighbors[i]].biome.implemented && Find.WorldGrid[neighbors[i]].hilliness != Hilliness.Impassable)
                            {
                                if(VehicleHarmony.debug && !(faction is null)) DebugDrawSettlement(tileID, neighbors[i]);
                                if(!(faction is null))
                                    VehicleHarmony.tiles.Add(new Pair<int, int>(neighbors[i], searchedRadius));
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

        public static void DebugDrawSettlement(int from, int to)
        {
            PeaceTalks o = (PeaceTalks)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfVehicles.DebugSettlement);
            o.Tile = from;
            o.SetFaction(Faction.OfMechanoids);
            Find.WorldObjects.Add(o);
            if (VehicleHarmony.drawPaths)
                VehicleHarmony.debugLines.Add(Find.WorldPathFinder.FindPath(from, to, null, null));
        }

        public static Mote ThrowMoteEnhanced(Vector3 loc, Map map, MoteThrown mote, bool overrideSaturation = false)
        {
            if(!loc.ShouldSpawnMotesAt(map) || (overrideSaturation && map.moteCounter.Saturated))
            {
                return null;
            }

            GenSpawn.Spawn(mote, loc.ToIntVec3(), map, WipeMode.Vanish);
            return mote;
        }

        public static void DrawDefaultCannonMesh(CannonHandler cannon, Vector3 pos, int layer)
        {
            SPTuple2<float, float> drawOffset = ShipDrawOffset(cannon.pawn, cannon.cannonRenderLocation.x, cannon.cannonRenderLocation.y, out SPTuple2<float, float> rotOffset1, 0f, cannon.attachedTo);
            Vector3 posFinal = new Vector3(pos.x + drawOffset.First + rotOffset1.First, pos.y + cannon.drawLayer, pos.z + drawOffset.Second + rotOffset1.Second);
            Graphics.DrawMesh(cannon.CannonGraphic.MeshAt(Rot4.West), posFinal, Quaternion.identity, cannon.CannonMaterial, layer);
        }

        public static void DrawAttachedThing(Texture2D baseTexture, Graphic baseGraphic, Vector2 baseRenderLocation,Vector2 baseDrawSize,
            Texture2D texture, Graphic graphic, Vector2 renderLocation, Vector2 renderOffset, Material baseMat, Material mat, float rotation, VehiclePawn parent, int drawLayer, CannonHandler attachedTo = null)
        {
            if (texture != null && renderLocation != null)
            {
                try
                {
                    Vector3 topVectorRotation = new Vector3(renderOffset.x, 1f, renderOffset.y).RotatedBy(rotation);
                    float locationRotation = 0f;
                    if(attachedTo != null)
                    {
                        locationRotation = attachedTo.TurretRotation;
                    }
                    SPTuple2<float, float> drawOffset = ShipDrawOffset(parent, renderLocation.x, renderLocation.y, out SPTuple2<float, float> rotOffset1, locationRotation, attachedTo);
                    
                    Vector3 topVectorLocation = new Vector3(parent.DrawPos.x + drawOffset.First + rotOffset1.First, parent.DrawPos.y + drawLayer, parent.DrawPos.z + drawOffset.Second + rotOffset1.Second);
                    Mesh cannonMesh = graphic.MeshAt(Rot4.North);
                    
                    if(VehicleMod.settings.debugDrawCannonGrid)
                    {
                        Material debugCenterMat = MaterialPool.MatFrom("Debug/cannonCenter");
                        Matrix4x4 debugCenter = default;
                        debugCenter.SetTRS(topVectorLocation + Altitudes.AltIncVect, Quaternion.identity, new Vector3(0.15f, 1f, 0.15f));
                        Graphics.DrawMesh(MeshPool.plane10, debugCenter, debugCenterMat, 0);
                    }

                    Graphics.DrawMesh(cannonMesh, topVectorLocation, rotation.ToQuat(), mat, 0);

                    if(baseMat != null && baseRenderLocation != null)
                    {
                        Matrix4x4 baseMatrix = default;
                        SPTuple2<float, float> baseDrawOffset = ShipDrawOffset(parent, baseRenderLocation.x, baseRenderLocation.y, out SPTuple2<float, float> rotOffset2);
                        Vector3 baseVectorLocation = new Vector3(parent.DrawPos.x + baseDrawOffset.First, parent.DrawPos.y, parent.DrawPos.z + baseDrawOffset.Second);
                        baseMatrix.SetTRS(baseVectorLocation + Altitudes.AltIncVect, parent.Rotation.AsQuat, new Vector3(baseDrawSize.x, 1f, baseDrawSize.y));
                        Graphics.DrawMesh(MeshPool.plane10, baseMatrix, baseMat, 0);
                    }

                    if(VehicleMod.settings.debugDrawCannonGrid)
                    {
                        Material debugMat = MaterialPool.MatFrom("Debug/cannonAlignment");
                        Matrix4x4 debugGrid = default;
                        debugGrid.SetTRS(topVectorLocation + Altitudes.AltIncVect, 0f.ToQuat(), new Vector3(5f, 1f, 5f));
                        Graphics.DrawMesh(MeshPool.plane10, debugGrid, debugMat, 0);
                    }
                }
                catch(Exception ex)
                {
                    Log.Error(string.Format("Error occurred during rendering of attached thing on {0}. Exception: {1}", parent.Label, ex.Message));
                }
            }
        }

        /// <summary>
        /// Draw cannon textures on GUI given collection of cannons and vehicle GUI is being drawn for
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="displayRect"></param>
        /// <param name="cannons"></param>
        /// <param name="vehicleMaskName"></param>
        /// <param name="resolveGraphics"></param>
        /// <param name="manualColorOne"></param>
        /// <param name="manualColorTwo"></param>
        /// <remarks>Might possibly want to throw into separate threads</remarks>
        public static void DrawCannonTextures(this VehiclePawn vehicle, Rect displayRect, IEnumerable<CannonHandler> cannons, string vehicleMaskName, bool resolveGraphics = false, Color? manualColorOne = null, Color? manualColorTwo = null)
        {
            foreach (CannonHandler cannon in cannons)
            {
                PawnKindLifeStage biggestStage = vehicle.kindDef.lifeStages.MaxBy(x => x.bodyGraphicData?.drawSize ?? Vector2.zero);

                if(resolveGraphics)
                    cannon.ResolveCannonGraphics(vehicle);

                if (cannon.CannonBaseGraphic != null)
                {
                    float baseWidth = (displayRect.width / biggestStage.bodyGraphicData.drawSize.x) * cannon.baseCannonDrawSize.x;
                    float baseHeight = (displayRect.height / biggestStage.bodyGraphicData.drawSize.y) * cannon.baseCannonDrawSize.y;

                    float xBase = displayRect.x + (displayRect.width / 2) - (baseWidth / 2) + ((vehicle.GetCachedComp<CompVehicle>().Props.displayUISize.x / biggestStage.bodyGraphicData.drawSize.x) * cannon.baseCannonRenderLocation.x);
                    float yBase = displayRect.y + (displayRect.height / 2) - (baseHeight / 2) - ((vehicle.GetCachedComp<CompVehicle>().Props.displayUISize.y / biggestStage.bodyGraphicData.drawSize.y) * cannon.baseCannonRenderLocation.y);

                    Rect baseCannonDrawnRect = new Rect(xBase, yBase, baseWidth, baseHeight);
                    GenUI.DrawTextureWithMaterial(baseCannonDrawnRect, cannon.CannonBaseTexture, cannon.CannonBaseGraphic.MatSingle);
                }

                float cannonWidth = (displayRect.width / biggestStage.bodyGraphicData.drawSize.x) * cannon.CannonGraphicData.drawSize.x;
                float cannonHeight = (displayRect.height / biggestStage.bodyGraphicData.drawSize.y) * cannon.CannonGraphicData.drawSize.y;

                /// ( center point of vehicle) + (UI size / drawSize) * cannonPos
                /// y axis inverted as UI goes top to bottom, but DrawPos goes bottom to top
                float xCannon = (displayRect.x + (displayRect.width / 2) - (cannonWidth / 2)) + ((vehicle.GetCachedComp<CompVehicle>().Props.displayUISize.x / biggestStage.bodyGraphicData.drawSize.x) * cannon.cannonRenderLocation.x);
                float yCannon = (displayRect.y + (displayRect.height / 2) - (cannonHeight / 2)) - ((vehicle.GetCachedComp<CompVehicle>().Props.displayUISize.y / biggestStage.bodyGraphicData.drawSize.y) * cannon.cannonRenderLocation.y);

                Rect cannonDrawnRect = new Rect(xCannon, yCannon, cannonWidth, cannonHeight);
                Material cannonMat = new Material(cannon.CannonGraphic.MatAt(Rot4.North, vehicle));
                
                if (cannon.CannonGraphic.Shader.SupportsMaskTex() && (manualColorOne != null || manualColorTwo != null) && cannon.CannonGraphic.GetType().IsAssignableFrom(typeof(Graphic_Cannon)))
                {
                    //REDO
                    MaterialRequest matReq = default;
                    matReq.mainTex = cannon.CannonTexture;
                    matReq.shader = cannon.CannonGraphic.Shader;
                    matReq.color = manualColorOne != null ? manualColorOne.Value : vehicle.DrawColor;
                    matReq.colorTwo = manualColorTwo != null ? manualColorTwo.Value : vehicle.DrawColorTwo;
                    matReq.maskTex = (cannon.CannonGraphic as Graphic_Cannon).maskTexPatterns[vehicleMaskName].Second[(cannon.CannonGraphic as Graphic_Cannon).CurrentIndex()];
                    cannonMat = MaterialPool.MatFrom(matReq);
                }

                GenUI.DrawTextureWithMaterial(cannonDrawnRect, cannon.CannonTexture, cannonMat);

                if (VehicleMod.settings.debugDrawCannonGrid)
                {
                    Widgets.DrawLineHorizontal(cannonDrawnRect.x, cannonDrawnRect.y, cannonDrawnRect.width);
                    Widgets.DrawLineHorizontal(cannonDrawnRect.x, cannonDrawnRect.y + cannonDrawnRect.height, cannonDrawnRect.width);
                    Widgets.DrawLineVertical(cannonDrawnRect.x, cannonDrawnRect.y, cannonDrawnRect.height);
                    Widgets.DrawLineVertical(cannonDrawnRect.x + cannonDrawnRect.width, cannonDrawnRect.y, cannonDrawnRect.height);
                }
            }
        }

        /// <summary>
        /// Draw Vehicle texture with option to manually apply colors to Material
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="vehicleTex"></param>
        /// <param name="vehicle"></param>
        /// <param name="vehicleMaskName"></param>
        /// <param name="resolveGraphics"></param>
        /// <param name="manualColorOne"></param>
        /// <param name="manualColorTwo"></param>
        public static void DrawVehicleTex(Rect rect, Texture2D vehicleTex, VehiclePawn vehicle, string vehicleMaskName, bool resolveGraphics = false, Color? manualColorOne = null, Color? manualColorTwo = null)
        {
            float UISizeX = vehicle.GetCachedComp<CompVehicle>().Props.displayUISize.x * rect.width;
            float UISizeY = vehicle.GetCachedComp<CompVehicle>().Props.displayUISize.y * rect.height;

            Rect displayRect = new Rect(rect.x, rect.y, UISizeX, UISizeY);
            Material mat = new Material(vehicle.VehicleGraphic.MatAt(Rot4.North, vehicle));
            
            if (vehicle.VehicleGraphic.Shader.SupportsMaskTex() && (manualColorOne != null || manualColorTwo != null))
            {
                MaterialRequest matReq = default;
                matReq.mainTex = vehicleTex;
                matReq.shader = vehicle.VehicleGraphic.Shader;
                matReq.color = manualColorOne != null ? manualColorOne.Value : vehicle.DrawColor;
                matReq.colorTwo = manualColorTwo != null ? manualColorTwo.Value : vehicle.DrawColorTwo;
                matReq.maskTex = vehicle.VehicleGraphic.maskTexPatterns[vehicleMaskName].Second[0];
                mat = MaterialPool.MatFrom(matReq);
            }

            GenUI.DrawTextureWithMaterial(displayRect, vehicleTex, mat);

            if (vehicle.GetCachedComp<CompCannons>() != null)
            {
                vehicle.DrawCannonTextures(displayRect, vehicle.GetCachedComp<CompCannons>().Cannons.OrderBy(x => x.drawLayer), vehicleMaskName, resolveGraphics, manualColorOne, manualColorTwo);
            }
        }

        public static void ThrowStaticText(Vector3 loc, Map map, string text, Color color, float timeBeforeStartFadeout = -1f)
        {
	        IntVec3 intVec = loc.ToIntVec3();
	        if (!intVec.InBounds(map))
	        {
		        return;
	        }
	        MoteText moteText = (MoteText)ThingMaker.MakeThing(ThingDefOf.Mote_Text, null);
	        moteText.exactPosition = loc;
	        moteText.SetVelocity(0f, 0f);
	        moteText.text = text;
	        moteText.textColor = color;
	        if (timeBeforeStartFadeout >= 0f)
	        {
		        moteText.overrideTimeBeforeStartFadeout = timeBeforeStartFadeout;
	        }
	        GenSpawn.Spawn(moteText, intVec, map, WipeMode.Vanish);
        }

        public static bool LocationRestrictedBySize(this VehiclePawn pawn, IntVec3 dest)
        {
            return CellRect.CenteredOn(dest, pawn.def.Size.x, pawn.def.Size.z).AnyNullified(c2 => IsBoat(pawn) ? (!c2.InBoundsShip(pawn.Map) || GenGridShips.Impassable(c2, pawn.Map)) : (!c2.InBounds(pawn.Map) || c2.ImpassableReverseThreaded(pawn.Map, pawn)))
                && CellRect.CenteredOn(dest, pawn.def.Size.z, pawn.def.Size.x).AnyNullified(c2 => IsBoat(pawn) ? (!c2.InBoundsShip(pawn.Map) || GenGridShips.Impassable(c2, pawn.Map)) : (!c2.InBounds(pawn.Map) || c2.ImpassableReverseThreaded(pawn.Map, pawn)));
        }

        public static void DrawLinesBetweenTargets(VehiclePawn pawn, Job curJob, JobQueue jobQueue)
		{
			Vector3 a = pawn.Position.ToVector3Shifted();
			if (pawn.vPather.curPath != null)
			{
				a = pawn.vPather.Destination.CenterVector3;
			}
			else if (curJob != null && curJob.targetA.IsValid && (!curJob.targetA.HasThing || (curJob.targetA.Thing.Spawned && curJob.targetA.Thing.Map == pawn.Map)))
			{
				GenDraw.DrawLineBetween(a, curJob.targetA.CenterVector3, AltitudeLayer.Item.AltitudeFor());
				a = curJob.targetA.CenterVector3;
			}
			for (int i = 0; i < jobQueue.Count; i++)
			{
				if (jobQueue[i].job.targetA.IsValid)
				{
					if (!jobQueue[i].job.targetA.HasThing || (jobQueue[i].job.targetA.Thing.Spawned && jobQueue[i].job.targetA.Thing.Map == pawn.Map))
					{
						Vector3 centerVector = jobQueue[i].job.targetA.CenterVector3;
						GenDraw.DrawLineBetween(a, centerVector, AltitudeLayer.Item.AltitudeFor());
						a = centerVector;
					}
				}
				else
				{
					List<LocalTargetInfo> targetQueueA = jobQueue[i].job.targetQueueA;
					if (targetQueueA != null)
					{
						for (int j = 0; j < targetQueueA.Count; j++)
						{
							if (!targetQueueA[j].HasThing || (targetQueueA[j].Thing.Spawned && targetQueueA[j].Thing.Map == pawn.Map))
							{
								Vector3 centerVector2 = targetQueueA[j].CenterVector3;
								GenDraw.DrawLineBetween(a, centerVector2, AltitudeLayer.Item.AltitudeFor());
								a = centerVector2;
							}
						}
					}
				}
			}
		}

        public static void DrawAngleLines(Vector3 cannonPos, Vector2 restrictedAngle, float minRange, float maxRange, float theta, float additionalAngle = 0f)
        {
            Vector3 minTargetPos1 = cannonPos.PointFromAngle(minRange, restrictedAngle.x + additionalAngle);
            Vector3 minTargetPos2 = cannonPos.PointFromAngle(minRange, restrictedAngle.y + additionalAngle);

            Vector3 maxTargetPos1 = cannonPos.PointFromAngle(maxRange, restrictedAngle.x + additionalAngle);
            Vector3 maxTargetPos2 = cannonPos.PointFromAngle(maxRange, restrictedAngle.y + additionalAngle);

            GenDraw.DrawLineBetween(minTargetPos1, maxTargetPos1);
            GenDraw.DrawLineBetween(minTargetPos2, maxTargetPos2);
            if (minRange > 0)
            {
                GenDraw.DrawLineBetween(cannonPos, minTargetPos1, SimpleColor.Red);
                GenDraw.DrawLineBetween(cannonPos, minTargetPos2, SimpleColor.Red);
            }

            float angleStart = restrictedAngle.x;

            Vector3 lastPointMin = minTargetPos1;
            Vector3 lastPointMax = maxTargetPos1;
            for (int angle = 0; angle < theta + 1; angle++)
            {
                Vector3 targetPointMax = cannonPos.PointFromAngle(maxRange, angleStart + angle + additionalAngle);
                GenDraw.DrawLineBetween(lastPointMax, targetPointMax);
                lastPointMax = targetPointMax;

                if (minRange > 0)
                {
                    Vector3 targetPointMin = cannonPos.PointFromAngle(minRange, angleStart + angle + additionalAngle);
                    GenDraw.DrawLineBetween(lastPointMin, targetPointMin, SimpleColor.Red);
                    lastPointMin = targetPointMin;
                }
            }
        }

        #endregion Rendering

        #region UI
        public static void LabelStyled(Rect rect, string label, GUIStyle style)
        {
	        Rect position = rect;
	        float num = Prefs.UIScale / 2f;
	        if (Prefs.UIScale > 1f && Math.Abs(num - Mathf.Floor(num)) > 1E-45f)
	        {
		        position.xMin = Widgets.AdjustCoordToUIScalingFloor(rect.xMin);
		        position.yMin = Widgets.AdjustCoordToUIScalingFloor(rect.yMin);
		        position.xMax = Widgets.AdjustCoordToUIScalingCeil(rect.xMax + 1E-05f);
		        position.yMax = Widgets.AdjustCoordToUIScalingCeil(rect.yMax + 1E-05f);
	        }
	        GUI.Label(position, label, style);
        }

        public static void LabelOutlineStyled(Rect rect, string label, GUIStyle style, Color outerColor)
        {
            Rect position = rect;
	        float num = Prefs.UIScale / 2f;
	        if (Prefs.UIScale > 1f && Math.Abs(num - Mathf.Floor(num)) > 1E-45f)
	        {
		        position.xMin = Widgets.AdjustCoordToUIScalingFloor(rect.xMin);
		        position.yMin = Widgets.AdjustCoordToUIScalingFloor(rect.yMin);
		        position.xMax = Widgets.AdjustCoordToUIScalingCeil(rect.xMax + 1E-05f);
		        position.yMax = Widgets.AdjustCoordToUIScalingCeil(rect.yMax + 1E-05f);
	        }

            var innerColor = style.normal.textColor;
            style.normal.textColor = outerColor;
            position.x--;
            GUI.Label(position, label, style);
            position.x += 2;
            GUI.Label(position, label, style);
            position.x--;
            position.y--;
            GUI.Label(position, label, style);
            position.y += 2;
            GUI.Label(position, label, style);
            position.y--;
            style.normal.textColor = innerColor;
	        GUI.Label(position, label, style);
        }

        public static void FillableBarLabeled(Rect rect, float fillPercent, string label, StatUpgradeCategory upgrade, Texture2D innerTex, Texture2D outlineTex, float? actualValue = null, float addedValue = 0f, float bgFillPercent = 0f)
        {
	        if (fillPercent < 0f)
	        {
		        fillPercent = 0f;
	        }
	        if (fillPercent > 1f)
	        {
		        fillPercent = 1f;
	        }

            Texture2D fillTex;
            Texture2D addedFillTex;
            switch(upgrade)
            {
                case StatUpgradeCategory.Armor:
                    fillTex = ArmorStatBarTexture;
                    addedFillTex = ArmorAddedStatBarTexture;
                    break;
                case StatUpgradeCategory.Speed:
                    fillTex = SpeedStatBarTexture;
                    addedFillTex = SpeedAddedStatBarTexture;
                    break;
                case StatUpgradeCategory.CargoCapacity:
                    fillTex = CargoStatBarTexture;
                    addedFillTex = CargoAddedStatBarTexture;
                    break;
                case StatUpgradeCategory.FuelConsumptionRate:
                    fillTex = FuelStatBarTexture;
                    addedFillTex = FuelAddedStatBarTexture;
                    break;
                case StatUpgradeCategory.FuelCapacity:
                    fillTex = FuelStatBarTexture;
                    addedFillTex = FuelAddedStatBarTexture;
                    break;
                default:
                    fillTex = BaseContent.BadTex;
                    addedFillTex = BaseContent.WhiteTex;
                    break;
            }

            FillableBarHollowed(rect, fillPercent, bgFillPercent, fillTex, addedFillTex, innerTex, outlineTex);
            
            Rect rectLabel = rect;
            rectLabel.x += 5f;

            GUIStyle style = new GUIStyle(Text.CurFontStyle);
            //style.fontStyle = FontStyle.Bold;

            LabelOutlineStyled(rectLabel, label, style, Color.black);
            if(actualValue != null)
            {
                Rect valueRect = rect;
                valueRect.width /= 2;
                valueRect.x = rectLabel.x + rectLabel.width / 2 - 6f;
                style.alignment = TextAnchor.MiddleRight;

                string value = string.Format("{1} {0}", actualValue.ToString(), addedValue != 0 ? "(" + (addedValue > 0 ? "+" : "") +  addedValue.ToString() + ")" : "");
                LabelOutlineStyled(valueRect, value, style, Color.black);
                //GUI.DrawTexture(valueRect, fillTex); //For Alignment
            }
        }

        public static void FillableBarHollowed(Rect rect, float fillPercent, float bgFillPercent, Texture2D fillTex, Texture2D addedFillTex, Texture2D innerTex, Texture2D bgTex)
        {
            GUI.DrawTexture(rect, bgTex);
            rect = rect.ContractedBy(2f);

            Rect rect2 = rect;
            rect2.width -= 2f;
            GUI.DrawTexture(rect2, innerTex);


            Rect fullBarRect = rect;
            fullBarRect.width *= fillPercent;
            GUI.DrawTexture(fullBarRect, fillTex);

            if(bgFillPercent != 0)
            {
                if(bgFillPercent < 0)
                {
                    Rect rectBG = rect;

                    if(fillPercent + bgFillPercent < 0)
                    {
                        rectBG.width *= fillPercent;
                        rectBG.x = fullBarRect.x;
                    }
                    else
                    {
                        rectBG.width *= bgFillPercent;
                        rectBG.x = fullBarRect.x + fullBarRect.width;
                    }
                    GUI.DrawTexture(rectBG, innerTex);
                }
                else
                {
                    Rect rectBG = rect;
                    rectBG.x = fullBarRect.x + fullBarRect.width;
                    rectBG.width *= bgFillPercent;
                    GUI.DrawTexture(rectBG, addedFillTex);
                }
            }
        }

        public static void DoItemsListForVehicle(Rect inRect, ref float curY, ref List<Thing> tmpSingleThing, ITab_Pawn_FormingCaravan instance)
        {
            LordJob_FormAndSendVehicles lordJob_FormAndSendCaravanVehicle = (LordJob_FormAndSendVehicles)(Find.Selector.SingleSelectedThing as Pawn).GetLord().LordJob;
            Rect position = new Rect(0f, curY, (inRect.width - 10f) / 2f, inRect.height);
            float a = 0f;
            GUI.BeginGroup(position);
            Widgets.ListSeparator(ref a, position.width, "ItemsToLoad".Translate());
            bool flag = false;
            foreach (TransferableOneWay transferableOneWay in lordJob_FormAndSendCaravanVehicle.transferables)
            {
                if (transferableOneWay.CountToTransfer > 0 && transferableOneWay.HasAnyThing)
                {
                    flag = true;
                    MethodInfo doThingRow = AccessTools.Method(type: typeof(ITab_Pawn_FormingCaravan), name: "DoThingRow");
                    object[] args = new object[] { transferableOneWay.ThingDef, transferableOneWay.CountToTransfer, transferableOneWay.things, position.width, a };
                    doThingRow.Invoke(instance, args);
                    a = (float)args[4];
                }
            }
            if (!flag)
            {
                Widgets.NoneLabel(ref a, position.width, null);
            }
            GUI.EndGroup();
            Rect position2 = new Rect((inRect.width + 10f) / 2f, curY, (inRect.width - 10f) / 2f, inRect.height);
            float b = 0f;
            GUI.BeginGroup(position2);
            Widgets.ListSeparator(ref b, position2.width, "LoadedItems".Translate());
            bool flag2 = false;
            foreach (Pawn pawn in lordJob_FormAndSendCaravanVehicle.lord.ownedPawns)
            {
                if (!pawn.inventory.UnloadEverything)
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
            if (!flag2)
            {
                Widgets.NoneLabel(ref b, position.width, null);
            }
            GUI.EndGroup();
            curY += Mathf.Max(a, b);
        }

        public static void LabelUnderlined(Rect rect, string label, Color labelColor, Color lineColor)
        {
	        Color color = GUI.color;
	        rect.y += 3f;
	        GUI.color = labelColor;
	        Text.Anchor = TextAnchor.UpperLeft;
	        Widgets.Label(rect, label);
	        rect.y += 20f;
            GUI.color = lineColor;
	        Widgets.DrawLineHorizontal(rect.x - 1, rect.y, rect.width - 1);
	        rect.y += 2f;
	        GUI.color = color;
        }

        public static void LabelUnderlined(Rect rect, string label, string label2, Color labelColor, Color label2Color, Color lineColor)
        {
	        Color color = GUI.color;
	        rect.y += 3f;
	        GUI.color = labelColor;
	        Text.Anchor = TextAnchor.UpperLeft;
	        Widgets.Label(rect, label);
            GUI.color = label2Color;
            Rect rect2 = new Rect(rect);
            rect2.x += Text.CalcSize(label).x + 5f;
	        Widgets.Label(rect2, label2);
            rect.y += 20f;
            GUI.color = lineColor;
	        Widgets.DrawLineHorizontal(rect.x - 1, rect.y, rect.width - 1);
	        rect.y += 2f;
	        GUI.color = color;
        }

        /// <summary>
        /// Draws ColorPicker (and HuePicker)
        /// </summary>
        /// <param name="fullRect"></param>
        /// <returns></returns>
        public static float DrawColorPicker(Rect fullRect)
		{
			Rect rect = fullRect.ContractedBy(10f);
			rect.width = 15f;
            if (Input.GetMouseButtonDown(0) && Mouse.IsOver(rect) && !Dialog_ColorPicker.draggingHue)
            {
                Dialog_ColorPicker.draggingHue = true;
            }
            if (Dialog_ColorPicker.draggingHue && Event.current.isMouse)
            {
                float num = Dialog_ColorPicker.hue;
                Dialog_ColorPicker.hue = Mathf.InverseLerp(rect.height, 0f, Event.current.mousePosition.y - rect.y);
                if (Dialog_ColorPicker.hue != num)
                {
                    Dialog_ColorPicker.SetColor(Dialog_ColorPicker.hue, Dialog_ColorPicker.saturation, Dialog_ColorPicker.value);
                }
            }
            if (Input.GetMouseButtonUp(0))
			{
				Dialog_ColorPicker.draggingHue = false;
			}
			Widgets.DrawBoxSolid(rect.ExpandedBy(1f), Color.grey);
			Widgets.DrawTexturePart(rect, new Rect(0f, 0f, 1f, 1f), Dialog_ColorPicker.HueChart);
			Rect rect2 = new Rect(0f, 0f, 16f, 16f)
			{
				center = new Vector2(rect.center.x, rect.height * (1f - Dialog_ColorPicker.hue) + rect.y).Rounded()
			};

			Widgets.DrawTextureRotated(rect2, ColorHue, 0f);
			rect = fullRect.ContractedBy(10f);
			rect.x = rect.xMax - rect.height;
			rect.width = rect.height;
            if (Input.GetMouseButtonDown(0) && Mouse.IsOver(rect) && !Dialog_ColorPicker.draggingCP)
            {
                Dialog_ColorPicker.draggingCP = true;
            }
            if (Dialog_ColorPicker.draggingCP)
            {
                Dialog_ColorPicker.saturation = Mathf.InverseLerp(0f, rect.width, Event.current.mousePosition.x - rect.x);
                Dialog_ColorPicker.value = Mathf.InverseLerp(rect.width, 0f, Event.current.mousePosition.y - rect.y);
                Dialog_ColorPicker.SetColor(Dialog_ColorPicker.hue, Dialog_ColorPicker.saturation, Dialog_ColorPicker.value);
            }
            if (Input.GetMouseButtonUp(0))
			{
				Dialog_ColorPicker.draggingCP = false;
			}
			Widgets.DrawBoxSolid(rect.ExpandedBy(1f), Color.grey);
			Widgets.DrawBoxSolid(rect, Color.white);
			GUI.color = Color.HSVToRGB(Dialog_ColorPicker.hue, 1f, 1f);
			Widgets.DrawTextureFitted(rect, Dialog_ColorPicker.ColorChart, 1f);
            float centerPoint = rect.x + rect.width / 2;
			GUI.color = Color.white;
			GUI.BeginClip(rect);
			rect2.center = new Vector2(rect.width * Dialog_ColorPicker.saturation, rect.width * (1f - Dialog_ColorPicker.value));
			if (Dialog_ColorPicker.value >= 0.4f && (Dialog_ColorPicker.hue <= 0.5f || Dialog_ColorPicker.saturation <= 0.5f))
			{
				GUI.color = Dialog_ColorPicker.Blackist;
			}
			Widgets.DrawTextureFitted(rect2, ColorPicker, 1f);
			GUI.color = Color.white;
			GUI.EndClip();
            return centerPoint;
		}

        #endregion UI

        #region TargetingAndDamage

        public static LocalTargetInfo GetCannonTarget(this CannonHandler cannon, float restrictedAngle = 0f, TargetingParameters param = null)
        {
            if (cannon.pawn.GetCachedComp<CompCannons>() != null && cannon.pawn.GetCachedComp<CompCannons>().WeaponStatusOnline && cannon.pawn.Faction != null) //add fire at will option
            {
                TargetScanFlags targetScanFlags = TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedLOSToNonPawns | TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
                Thing thing = (Thing)BestAttackTarget(cannon, targetScanFlags, null, 0f, 9999f, default(IntVec3), float.MaxValue, false, false);
                if (thing != null)
                {
                    return new LocalTargetInfo(thing);
                }
            }
            return LocalTargetInfo.Invalid;
        }

        public static IAttackTarget BestAttackTarget(CannonHandler cannon, TargetScanFlags flags, Predicate<Thing> validator = null, float minDist = 0f, float maxDist = 9999f, IntVec3 locus = default(IntVec3), float maxTravelRadiusFromLocus = 3.4028235E+38f, bool canBash = false, bool canTakeTargetsCloserThanEffectiveMinRange = true)
		{
			VehiclePawn searcherPawn = cannon.pawn;

			float minDistSquared = minDist * minDist;
            float num = maxTravelRadiusFromLocus + cannon.MaxRange;
			float maxLocusDistSquared = num * num;
			Func<IntVec3, bool> losValidator = null;
			if ((flags & TargetScanFlags.LOSBlockableByGas) != TargetScanFlags.None)
			{
				losValidator = delegate(IntVec3 vec3)
				{
					Gas gas = vec3.GetGas(searcherPawn.Map);
					return gas == null || !gas.def.gas.blockTurretTracking;
				};
			}
			Predicate<IAttackTarget> innerValidator = delegate(IAttackTarget t)
			{
				Thing thing = t.Thing;
				if (t == searcherPawn)
				{
					return false;
				}
				if (minDistSquared > 0f && (float)(searcherPawn.Position - thing.Position).LengthHorizontalSquared < minDistSquared)
				{
					return false;
				}
				if (!canTakeTargetsCloserThanEffectiveMinRange)
				{
                    float num2 = cannon.MinRange;
					if (num2 > 0f && (float)(cannon.pawn.Position - thing.Position).LengthHorizontalSquared < num2 * num2)
					{
						return false;
					}
				}
				if (maxTravelRadiusFromLocus < 9999f && (thing.Position - locus).LengthHorizontalSquared > maxLocusDistSquared)
				{
					return false;
				}
				if (!searcherPawn.HostileTo(thing))
				{
					return false;
				}
				if (validator != null && !validator(thing))
				{
					return false;
				}
				if ((flags & TargetScanFlags.NeedLOSToAll) != TargetScanFlags.None)
				{
					if (losValidator != null && (!losValidator(searcherPawn.Position) || !losValidator(thing.Position)))
					{
						return false;
					}
					if (!searcherPawn.CanSee(thing, losValidator))
					{
						if (t is Pawn)
						{
							if ((flags & TargetScanFlags.NeedLOSToPawns) != TargetScanFlags.None)
							{
								return false;
							}
						}
						else if ((flags & TargetScanFlags.NeedLOSToNonPawns) != TargetScanFlags.None)
						{
							return false;
						}
					}
				}
				if (((flags & TargetScanFlags.NeedThreat) != TargetScanFlags.None || (flags & TargetScanFlags.NeedAutoTargetable) != TargetScanFlags.None) && t.ThreatDisabled(searcherPawn))
				{
					return false;
				}
				if ((flags & TargetScanFlags.NeedAutoTargetable) != TargetScanFlags.None && !AttackTargetFinder.IsAutoTargetable(t))
				{
					return false;
				}
				if ((flags & TargetScanFlags.NeedActiveThreat) != TargetScanFlags.None && !GenHostility.IsActiveThreatTo(t, searcherPawn.Faction))
				{
					return false;
				}
				Pawn pawn = t as Pawn;
				if ((flags & TargetScanFlags.NeedNonBurning) != TargetScanFlags.None && thing.IsBurning())
				{
					return false;
				}

				if (thing.def.size.x == 1 && thing.def.size.z == 1)
				{
					if (thing.Position.Fogged(thing.Map))
					{
						return false;
					}
				}
				else
				{
					bool flag2 = false;
					using (CellRect.Enumerator enumerator = thing.OccupiedRect().GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							if (!enumerator.Current.Fogged(thing.Map))
							{
								flag2 = true;
								break;
							}
						}
					}
					if (!flag2)
					{
						return false;
					}
				}
				return true;
			};

            List<IAttackTarget> tmpTargets = new List<IAttackTarget>();
			tmpTargets.AddRange(searcherPawn.Map.attackTargetsCache.GetPotentialTargetsFor(searcherPawn));

			bool flag = false;
			for (int i = 0; i < tmpTargets.Count; i++)
			{
				IAttackTarget attackTarget = tmpTargets[i];
				if (attackTarget.Thing.Position.InHorDistOf(searcherPawn.Position, maxDist) && innerValidator(attackTarget) && cannon.pawn.GetCachedComp<CompCannons>().TryFindShootLineFromTo(searcherPawn.Position, new LocalTargetInfo(attackTarget.Thing), out ShootLine resultingLine))
				{
					flag = true;
					break;
				}
			}
			IAttackTarget result;
			if (flag)
			{
				tmpTargets.RemoveAll((IAttackTarget x) => !x.Thing.Position.InHorDistOf(searcherPawn.Position, maxDist) || !innerValidator(x));
				result = GetRandomShootingTargetByScore(tmpTargets, searcherPawn);
			}
			else
			{
				Predicate<Thing> validator2;
				if ((flags & TargetScanFlags.NeedReachableIfCantHitFromMyPos) != TargetScanFlags.None && (flags & TargetScanFlags.NeedReachable) == TargetScanFlags.None)
				{
					validator2 = ((Thing t) => innerValidator((IAttackTarget)t) && cannon.pawn.GetCachedComp<CompCannons>().TryFindShootLineFromTo(searcherPawn.Position, new LocalTargetInfo(t), out ShootLine resultingLine));
				}
				else
				{
					validator2 = ((Thing t) => innerValidator((IAttackTarget)t));
				}
				result = (IAttackTarget)GenClosest.ClosestThing_Global(searcherPawn.Position, tmpTargets, maxDist, validator2, null);
			}
			tmpTargets.Clear();
			return result;
		}

        public static IAttackTarget GetRandomShootingTargetByScore(List<IAttackTarget> targets, VehiclePawn searcher)
        {
	        Pair<IAttackTarget, float> pair;
	        if (GetAvailableShootingTargetsByScore(targets, searcher).TryRandomElementByWeight((Pair<IAttackTarget, float> x) => x.Second, out pair))
	        {
		        return pair.First;
	        }
	        return null;
        }

        public static List<Pair<IAttackTarget, float>> GetAvailableShootingTargetsByScore(List<IAttackTarget> rawTargets, VehiclePawn searcher)
        {
	        List<Pair<IAttackTarget, float>> availableShootingTargets = new List<Pair<IAttackTarget, float>>();
            List<float> tmpTargetScores = new List<float>();
            List<bool> tmpCanShootAtTarget = new List<bool>();
	        if (rawTargets.Count == 0)
	        {
                return availableShootingTargets;
	        }
	        tmpTargetScores.Clear();
	        tmpCanShootAtTarget.Clear();
	        float num = 0f;
	        IAttackTarget attackTarget = null;
	        for (int i = 0; i < rawTargets.Count; i++)
	        {
		        tmpTargetScores.Add(float.MinValue);
		        tmpCanShootAtTarget.Add(false);
		        if (rawTargets[i] != searcher)
		        {
			        bool flag = searcher.GetCachedComp<CompCannons>().TryFindShootLineFromTo(searcher.Position, new LocalTargetInfo(rawTargets[i].Thing), out ShootLine shootLine);
			        tmpCanShootAtTarget[i] = flag;
			        if (flag)
			        {
				        float shootingTargetScore = GetShootingTargetScore(rawTargets[i], searcher);
				        tmpTargetScores[i] = shootingTargetScore;
				        if (attackTarget == null || shootingTargetScore > num)
				        {
					        attackTarget = rawTargets[i];
					        num = shootingTargetScore;
				        }
			        }
		        }
	        }
	        if (num < 1f)
	        {
		        if (attackTarget != null)
		        {
			        availableShootingTargets.Add(new Pair<IAttackTarget, float>(attackTarget, 1f));
		        }
	        }
	        else
	        {
		        float num2 = num - 30f;
		        for (int j = 0; j < rawTargets.Count; j++)
		        {
			        if (rawTargets[j] != searcher && tmpCanShootAtTarget[j])
			        {
				        float num3 = tmpTargetScores[j];
				        if (num3 >= num2)
				        {
					        float second = Mathf.InverseLerp(num - 30f, num, num3);
					        availableShootingTargets.Add(new Pair<IAttackTarget, float>(rawTargets[j], second));
				        }
			        }
		        }
	        }
	        return availableShootingTargets;
        }

        private static float GetShootingTargetScore(IAttackTarget target, IAttackTargetSearcher searcher)
        {
	        float num = 60f;
	        num -= Mathf.Min((target.Thing.Position - searcher.Thing.Position).LengthHorizontal, 40f);
	        if (target.TargetCurrentlyAimingAt == searcher.Thing)
	        {
		        num += 10f;
	        }
	        if (searcher.LastAttackedTarget == target.Thing && Find.TickManager.TicksGame - searcher.LastAttackTargetTick <= 300)
	        {
		        num += 40f;
	        }
	        num -= CoverUtility.CalculateOverallBlockChance(target.Thing.Position, searcher.Thing.Position, searcher.Thing.Map) * 10f;
	        Pawn pawn = target as Pawn;
	        if (pawn != null && pawn.RaceProps.Animal && pawn.Faction != null && !pawn.IsFighting())
	        {
		        num -= 50f;
	        }
	        //num += _  - add additional cost based on how close to friendly fire
	        return num * target.TargetPriorityFactor;
        }

        public static void Explode(this Projectile proj)
        {
            TerrainDef terrainImpact = proj.Map.terrainGrid.TerrainAt(proj.Position);
            Map map = proj.Map;
            proj.Destroy(DestroyMode.Vanish);
            if (proj.def.projectile.explosionEffect != null)
            {
                Effecter effecter = proj.def.projectile.explosionEffect.Spawn();
                effecter.Trigger(new TargetInfo(proj.Position, map, false), new TargetInfo(proj.Position, map, false));
                effecter.Cleanup();
            }
            IntVec3 position = proj.Position;
            Map map2 = map;

            int waterDepth = map.terrainGrid.TerrainAt(proj.Position).IsWater ? map.terrainGrid.TerrainAt(proj.Position) == TerrainDefOf.WaterOceanShallow ||
                map.terrainGrid.TerrainAt(proj.Position) == TerrainDefOf.WaterShallow || map.terrainGrid.TerrainAt(proj.Position) == TerrainDefOf.WaterMovingShallow ? 1 : 2 : 0;
            if (waterDepth == 0) Log.Error("Impact Water Depth is 0, but terrain is water.");
            float explosionRadius = (proj.def.projectile.explosionRadius / (2f * waterDepth));
            if (explosionRadius < 1) explosionRadius = 1f;
            DamageDef damageDef = proj.def.projectile.damageDef;
            Thing launcher = null;
            int damageAmount = proj.DamageAmount;
            float armorPenetration = proj.ArmorPenetration;
            SoundDef soundExplode;
            soundExplode = SoundDefOf_Ships.Explode_BombWater; //Changed for current issues
            SoundStarter.PlayOneShot(soundExplode, new TargetInfo(proj.Position, map, false));
            ThingDef equipmentDef = null;
            ThingDef def = proj.def;
            Thing thing = null;
            ThingDef postExplosionSpawnThingDef = proj.def.projectile.postExplosionSpawnThingDef;
            float postExplosionSpawnChance = 0.0f;
            float chanceToStartFire = proj.def.projectile.explosionChanceToStartFire * 0.0f;
            int postExplosionSpawnThingCount = proj.def.projectile.postExplosionSpawnThingCount;
            ThingDef preExplosionSpawnThingDef = proj.def.projectile.preExplosionSpawnThingDef;
            GenExplosion.DoExplosion(position, map2, explosionRadius, damageDef, launcher, damageAmount, armorPenetration, soundExplode,
                equipmentDef, def, thing, postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount,
                proj.def.projectile.applyDamageToExplosionCellsNeighbors, preExplosionSpawnThingDef, proj.def.projectile.preExplosionSpawnChance,
                proj.def.projectile.preExplosionSpawnThingCount, chanceToStartFire, proj.def.projectile.explosionDamageFalloff);
        }

        #endregion TargetingAndDamage

        #region MultithreadingUtils

        public static bool ImpassableReverseThreaded(this IntVec3 c, Map map, Pawn vehicle)
		{
            if (c == vehicle.Position)
                return false;
            else if (!c.InBounds(map))
                return true;
            try
            {
                //Create new list for thread safety
                List<Thing> list = new List<Thing>(map.thingGrid.ThingsListAtFast(c));
			    for (int i = 0; i < list.Count; i++)
			    {
				    if (list[i].def.passability == Traversability.Impassable)
				    {
					    return true;
				    }
			    }
            }
            catch(Exception ex)
            {
                Log.ErrorOnce($"Exception Thrown in ThreadId [{Thread.CurrentThread.ManagedThreadId}] Exception: {ex.StackTrace}", vehicle.thingIDNumber ^ Thread.CurrentThread.ManagedThreadId);
            }
			return false;
		}

        ///Exceptions thrown during Task will be handled at the TaskFactory level
        public static Pawn AnyVehicleBlockingPathAt(IntVec3 c, VehiclePawn vehicle, bool actAsIfHadCollideWithPawnsJob = false, bool collideOnlyWithStandingPawns = false, bool forPathFinder = false)
		{
            List<Thing> thingList = new List<Thing>(c.GetThingList(vehicle.Map)); //Create new list so ref type list is not overriden mid-task
			if (thingList.Count == 0)
			{
                return null;
			}
			bool flag = false;
			if (actAsIfHadCollideWithPawnsJob)
			{
				flag = true;
			}
			else
			{
				Job curJob = vehicle.CurJob;
				if (curJob != null && (curJob.collideWithPawns || curJob.def.collideWithPawns || vehicle.jobs.curDriver.collideWithPawns))
				{
					flag = true;
				}
				else if (vehicle.Drafted)
				{
					bool moving = vehicle.vPather.Moving;
				}
			}
			for (int i = 0; i < thingList.Count; i++)
			{
				Pawn pawn = thingList[i] as Pawn;
				if (pawn != null && pawn != vehicle && !pawn.Downed && (!collideOnlyWithStandingPawns || (!pawn.pather.MovingNow && (!pawn.pather.Moving || !pawn.pather.MovedRecently(60)))))
				{
					if (pawn.HostileTo(vehicle))
					{
                        return pawn;
					}
					if (flag && (forPathFinder || !vehicle.Drafted || !pawn.RaceProps.Animal))
					{
						Job curJob2 = pawn.CurJob;
						if (curJob2 != null && (curJob2.collideWithPawns || curJob2.def.collideWithPawns || pawn.jobs.curDriver.collideWithPawns))
						{
                            return pawn;
						}
					}
				}
			}

            return null;
		}

        #endregion MultithreadingUtils

        #region Textures

        public static List<string> GetAllFolderNamesInFolder(string folderPath)
        {
            if (!UnityData.IsInMainThread)
			{
				Log.Error("Tried to get all resources in a folder \"" + folderPath + "\" from a different thread. All resources must be loaded in the main thread.", false);
                return new List<string>();
			}

            var fullPath = string.Concat(ConditionalPatchApplier.VehicleMMD.RootDir, '/', GenFilePaths.ContentPath<Texture2D>(), folderPath);

            if (!Directory.Exists(fullPath))
            {
                return new List<string>();
            }

            var folders = Directory.GetDirectories(fullPath);
            for (int i = 0; i < folders.Length; i++)
            {
                folders[i] = folders[i].Replace('\\', '/');
            }
            return folders.ToList();
        }

        #endregion Textures

        public static CannonTargeter CannonTargeter = new CannonTargeter();

        public static Texture2D missingIcon;
        public static Dictionary<Pawn, Pair<VehiclePawn, VehicleHandler>> assignedSeats = new Dictionary<Pawn, Pair<VehiclePawn, VehicleHandler>>();

        public static Texture2D FillableBarBackgroundTex = SolidColorMaterials.NewSolidColorTexture(Color.black);
        public static Texture2D FillableBarInnerTex = SolidColorMaterials.NewSolidColorTexture(new ColorInt(19, 22, 27).ToColor);

        public static Texture2D ArmorStatBarTexture = SolidColorMaterials.NewSolidColorTexture(new ColorInt(35, 50, 185).ToColor);
        public static Texture2D ArmorAddedStatBarTexture = SolidColorMaterials.NewSolidColorTexture(new ColorInt(35, 50, 185, 120).ToColor);

        public static Texture2D SpeedStatBarTexture = SolidColorMaterials.NewSolidColorTexture(new ColorInt(0, 115, 40).ToColor);
        public static Texture2D SpeedAddedStatBarTexture = SolidColorMaterials.NewSolidColorTexture(new ColorInt(0, 115, 40, 120).ToColor);
        
        public static Texture2D CargoStatBarTexture = SolidColorMaterials.NewSolidColorTexture(new ColorInt(185, 110, 15).ToColor);
        public static Texture2D CargoAddedStatBarTexture = SolidColorMaterials.NewSolidColorTexture(new ColorInt(185, 110, 15, 120).ToColor);

        public static Texture2D FuelStatBarTexture = SolidColorMaterials.NewSolidColorTexture(new ColorInt(60, 30, 30).ToColor);
        public static Texture2D FuelAddedStatBarTexture = SolidColorMaterials.NewSolidColorTexture(new ColorInt(60, 30, 30, 120).ToColor);

        public static readonly Texture2D FillableBarTexture = SolidColorMaterials.NewSolidColorTexture(0.5f, 0.5f, 0.5f, 0.5f);
        public static readonly Texture2D ClearBarTexture = BaseContent.ClearTex;

        public static readonly Texture2D TradeArrow = ContentFinder<Texture2D>.Get("UI/Widgets/TradeArrow");

        public static readonly Texture2D ColorPicker = ContentFinder<Texture2D>.Get("UI/ColorCog");
        public static readonly Texture2D ColorHue = ContentFinder<Texture2D>.Get("UI/ColorHue");

        public static readonly Material RangeCircle_ExtraWide = MaterialPool.MatFrom("UI/RangeField_ExtraWide", ShaderDatabase.MoteGlow);
        public static readonly Material RangeCircle_Wide = MaterialPool.MatFrom("UI/RangeField_Wide", ShaderDatabase.MoteGlow);
        public static readonly Material RangeCircle_Mid = MaterialPool.MatFrom("UI/RangeField_Mid", ShaderDatabase.MoteGlow);
        public static readonly Material RangeCircle_Close = MaterialPool.MatFrom("UI/RangeField_Close", ShaderDatabase.MoteGlow);

        public static readonly Color IconColor = new Color(0.84f, 0.84f, 0.84f); 

        public static readonly SPTuple<int, int, int> RangeDistances = SPTuple.Create(5, 15, 25); 

        public static Material RangeMat(int radius)
        {
            if(radius <= RangeDistances.First)
            {
                return RangeCircle_Close;
            }
            else if(radius <= RangeDistances.Second)
            {
                return RangeCircle_Mid;
            }
            else if(radius <= 25)
            {
                return RangeCircle_Wide;
            }
            else
            {
                return RangeCircle_ExtraWide;
            }
        }
    }
}
