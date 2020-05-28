using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using Vehicles.AI;
using Vehicles.Lords;
using Vehicles.Defs;
using HarmonyLib;
using SPExtended;

namespace Vehicles
{
    [StaticConstructorOnStartup]
    public static class HelperMethods
    {
        #region Pathing
        public static bool BoatCantTraverse(int tile, Caravan caravan = null)
        {
            bool flag = !WaterCovered(tile) && (!Find.World.CoastDirectionAt(tile).IsValid || Find.World.Impassable(tile));
            bool riverFlag = false;
            if (ShipHarmony.currentFormingCaravan != null || caravan != null || ((Find.WorldSelector.SelectedObjects.Any() && Find.WorldSelector.SelectedObjects.All(x => x is Caravan && (x as Caravan).IsPlayerControlled && 
                HasVehicle(x as Caravan))) && !ShipHarmony.routePlannerActive))
            {
                List<Pawn> pawns;
                if(caravan != null)
                {
                    pawns = caravan.PawnsListForReading;
                }
                else if(ShipHarmony.currentFormingCaravan != null)
                {
                    pawns = TransferableUtility.GetPawnsFromTransferables(ShipHarmony.currentFormingCaravan.transferables);
                }
                else
                {
                    pawns = GrabBoatsFromCaravans(Find.WorldSelector.SelectedObjects.Cast<Caravan>().ToList());
                }
                riverFlag = !pawns.Any(x => IsBoat(x)) ? false : RiverIsValid(tile, pawns);
            }
            return flag && !riverFlag;
        }

        public static bool ImpassableModified(World world, int tileID, int startTile, int destTile, Caravan caravan)
        {
            if (caravan is null && ShipHarmony.currentFormingCaravan is null)
            {
                if (ShipHarmony.routePlannerActive && world.CoastDirectionAt(startTile).IsValid && world.CoastDirectionAt(destTile).IsValid)
                    return ImpassableForBoatPlanner(tileID, destTile) && world.Impassable(tileID); //Route planner doesn't know if you have boat or not, so check both
                return ShipHarmony.routePlannerActive && (WaterCovered(startTile) || Find.World.CoastDirectionAt(startTile).IsValid) && (WaterCovered(destTile) || world.CoastDirectionAt(destTile).IsValid) ?
                    ImpassableForBoatPlanner(tileID, destTile) : world.Impassable(tileID);
            }
            bool riverValid = caravan is null && !(ShipHarmony.currentFormingCaravan is null) ? RiverIsValid(tileID, TransferableUtility.GetPawnsFromTransferables(ShipHarmony.currentFormingCaravan.transferables)) : 
                RiverIsValid(tileID, caravan.PawnsListForReading.Where(x => IsBoat(x)).ToList());
            bool flag = Find.WorldGrid[tileID].biome == BiomeDefOf.Ocean || Find.WorldGrid[tileID].biome == BiomeDefOf.Lake;
            return HasVehicle(caravan) ? (!WaterCovered(tileID) && !(Find.World.CoastDirectionAt(tileID).IsValid && tileID == destTile) &&
                !(RimShipMod.mod.settings.riverTravel && riverValid)) : (flag || world.Impassable(tileID));
        }

        public static bool ImpassableForBoatPlanner(int tileID, int destTile = 0)
        {
            bool flag = Find.WorldGrid[tileID].biome == BiomeDefOf.Ocean || Find.WorldGrid[tileID].biome == BiomeDefOf.Lake || (tileID == destTile && Find.World.CoastDirectionAt(tileID).IsValid);
            return !flag;
        }

        public static void PatherFailedHelper(ref Pawn_PathFollower instance, Pawn pawn)
        {
            instance.StopDead();
            pawn?.jobs?.curDriver?.Notify_PatherFailed();
        }

        public static void PatherArrivedHelper(Pawn_PathFollower instance, Pawn pawn)
        {
            instance.StopDead();
            if (!(pawn.jobs.curJob is null))
            {
                pawn.jobs.curDriver.Notify_PatherArrived();
            }
        }

        public static void FaceShipAdjacentCell(IntVec3 c, Pawn pawn)
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

        public static int CostToMoveIntoCellShips(Pawn pawn, IntVec3 c)
        {
            int num = (c.x == pawn.Position.x || c.z == pawn.Position.z) ? pawn.TicksPerMoveCardinal : pawn.TicksPerMoveDiagonal;
            num += MapExtensionUtility.GetExtensionToMap(pawn.Map)?.getShipPathGrid?.CalculatedCostAt(c) ?? 200;
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

            if(!RimShipMod.mod.settings.debugDisableSmoothPathing)
            {
                return pawn.GetComp<CompVehicle>().BearingAngle;
            }

            if (pawn.pather.Moving)
            {
                IntVec3 c = pawn.pather.nextCell - pawn.Position;
                if (c.x > 0 && c.z > 0)
                {
                    pawn.GetComp<CompVehicle>().Angle = -45f;
                }
                else if (c.x > 0 && c.z < 0)
                {
                    pawn.GetComp<CompVehicle>().Angle = 45f;
                }
                else if (c.x < 0 && c.z < 0)
                {
                    pawn.GetComp<CompVehicle>().Angle = -45f;
                }
                else if (c.x < 0 && c.z > 0)
                {
                    pawn.GetComp<CompVehicle>().Angle = 45f;
                }
                else
                {
                    pawn.GetComp<CompVehicle>().Angle = 0f;
                }
            }
            return pawn.GetComp<CompVehicle>().Angle;
        }

        public static bool NeedNewPath(LocalTargetInfo destination, PawnPath curPath, Pawn pawn, PathEndMode peMode, IntVec3 lastPathedTargetPosition)
        {
            if (!destination.IsValid || curPath is null || !curPath.Found || curPath.NodesLeftCount == 0)
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
            while (num3 < 20 && num3 < curPath.NodesLeftCount)
            {
                intVec = curPath.Peek(num3);
                if (!GenGridShips.Walkable(intVec, MapExtensionUtility.GetExtensionToMap(pawn.Map)))
                    return true;
                if (num3 != 0 && intVec.AdjacentToDiagonal(other) && (ShipPathFinder.BlocksDiagonalMovement(pawn.Map.cellIndices.CellToIndex(intVec.x, other.z), pawn.Map,
                    MapExtensionUtility.GetExtensionToMap(pawn.Map)) || ShipPathFinder.BlocksDiagonalMovement(pawn.Map.cellIndices.CellToIndex(other.x, intVec.z), pawn.Map,
                    MapExtensionUtility.GetExtensionToMap(pawn.Map))))
                    return true;
                other = intVec;
                num3++;
            }
            return false;
        }

        public static bool TrySetNewPath(ref Pawn_PathFollower instance, ref IntVec3 lastPathedTargetPosition, LocalTargetInfo destination, Pawn pawn, Map map, ref PathEndMode peMode)
        {
            PawnPath pawnPath = GenerateNewPath(ref lastPathedTargetPosition, destination, ref pawn, map, peMode);
            if (!pawnPath.Found)
            {
                PatherFailedHelper(ref instance, pawn);
                return false;
            }
            if (!(instance.curPath is null))
            {
                instance.curPath.ReleaseToPool();
            }
            instance.curPath = pawnPath;
            int num = 0;
            int foundPathWhichCollidesWithPawns = Traverse.Create(instance).Field("foundPathWhichCollidesWithPawns").GetValue<int>();
            int foundPathWithDanger = Traverse.Create(instance).Field("foundPathWithDanger").GetValue<int>();
            while (num < 20 && num < instance.curPath.NodesLeftCount)
            {
                IntVec3 c = instance.curPath.Peek(num);

                if (pawn.GetComp<CompVehicle>().beached) break;
                if (PawnUtility.ShouldCollideWithPawns(pawn) && PawnUtility.AnyPawnBlockingPathAt(c, pawn, false, false, false))
                {
                    foundPathWhichCollidesWithPawns = Find.TickManager.TicksGame;
                }
                if (PawnUtility.KnownDangerAt(c, pawn.Map, pawn))
                {
                    foundPathWithDanger = Find.TickManager.TicksGame;
                }
                if (foundPathWhichCollidesWithPawns == Find.TickManager.TicksGame && foundPathWithDanger == Find.TickManager.TicksGame)
                {
                    break;
                }
                num++;
            }
            return true;
        }

        public static PawnPath GenerateNewPath(ref IntVec3 lastPathedTargetPosition, LocalTargetInfo destination, ref Pawn pawn, Map map, PathEndMode peMode)
        {
            lastPathedTargetPosition = destination.Cell;
            return MapExtensionUtility.GetExtensionToMap(map)?.getShipPathFinder?.FindShipPath(pawn.Position, destination, pawn, peMode) ?? PawnPath.NotFound;
        }

        public static void SetupMoveIntoNextCell(ref Pawn_PathFollower instance, Pawn pawn, LocalTargetInfo destination)
        {
            if (instance.curPath.NodesLeftCount <= 1)
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
            if (!GenGridShips.Walkable(instance.nextCell, MapExtensionUtility.GetExtensionToMap(pawn.Map)))
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

        public static bool OnDeepWater(this Pawn pawn)
        {
            //Splitting Caravan?
            if (pawn?.Map is null && pawn.IsWorldPawn())
                return false;
            return (pawn.Map.terrainGrid.TerrainAt(pawn.Position) == TerrainDefOf.WaterDeep || pawn.Map.terrainGrid.TerrainAt(pawn.Position) == TerrainDefOf.WaterMovingChestDeep ||
                pawn.Map.terrainGrid.TerrainAt(pawn.Position) == TerrainDefOf.WaterOceanDeep) && GenGrid.Impassable(pawn.Position, pawn.Map);
        }

        #endregion

        #region SmoothPathing

        public static bool InitiateSmoothPath(this VehiclePawn boat, IntVec3 cell)
        {
            Log.Message("Initiate");
            if (!cell.IsValid)
                return false;
            try
            {
                boat.GetComp<CompVehicle>().EnqueueCellImmediate(cell);
                boat.GetComp<CompVehicle>().CheckTurnSign();
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

        #endregion

        #region FeatureChecking

        public static bool IsShipDef(ThingDef td)
        {
            return td?.GetCompProperties<CompProperties_Vehicle>() != null;
        }

        public static bool IsBoat(Pawn p)
        {
            return IsVehicle(p) && p.GetComp<CompVehicle>().Props.vehicleType == VehicleType.Sea;
        }

        public static bool IsVehicle(Pawn p)
        {
            return p?.TryGetComp<CompVehicle>() != null;
        }

        public static bool IsVehicle(Thing t)
        {
            return IsVehicle(t as Pawn);
        }

        public static bool IsVehicle(ThingDef td)
        {
            return td.GetCompProperties<CompProperties_Vehicle>() != null;
        }

        public static bool HasVehicle(List<Pawn> pawns)
        {
            return pawns?.Any(x => IsVehicle(x)) ?? false;
        }

        public static bool HasVehicle(IEnumerable<Pawn> pawns)
        {
            return pawns?.Any(x => IsVehicle(x)) ?? false;
        }

        public static bool HasVehicle(Caravan c)
        {
            return (c is null) ? (ShipHarmony.currentFormingCaravan is null) ? false : HasVehicle(TransferableUtility.GetPawnsFromTransferables(ShipHarmony.currentFormingCaravan.transferables)) : HasVehicle(c?.PawnsListForReading);
        }

        public static bool HasVehicleInCaravan(Pawn p)
        {
            return p.IsFormingCaravan() && p.GetLord().LordJob is LordJob_FormAndSendCaravanShip && p.GetLord().ownedPawns.Any(x => IsVehicle(x));
        }

        public static bool HasBoat(List<Pawn> pawns)
        {
            return pawns?.Any(x => IsBoat(x)) ?? false;
        }

        public static bool HasBoat(IEnumerable<Pawn> pawns)
        {
            return pawns?.Any(x => IsBoat(x)) ?? false;
        }

        public static bool HasBoat(Caravan c)
        {
            return (c is null) ? (ShipHarmony.currentFormingCaravan is null) ? false : HasBoat(TransferableUtility.GetPawnsFromTransferables(ShipHarmony.currentFormingCaravan.transferables)) : HasBoat(c?.PawnsListForReading);
        }

        public static bool HasUpgradeMenu(Pawn p)
        {
            return p?.TryGetComp<CompUpgradeTree>() != null;
        }

        //REDO
        public static bool HasEnoughSpacePawns(List<Pawn> pawns)
        {
            int num = 0;
            foreach (Pawn p in pawns.Where(x => IsVehicle(x)))
            {
                num += p.GetComp<CompVehicle>().TotalSeats;
            }
            return pawns.Where(x => !IsVehicle(x)).Count() <= num;
        }

        //REDO
        public static bool HasEnoughPawnsToEmbark(List<Pawn> pawns)
        {
            int num = 0;
            foreach (Pawn p in pawns.Where(x => IsVehicle(x)))
            {
                num += p.GetComp<CompVehicle>().PawnCountToOperate;
            }
            return pawns.Where(x => !IsVehicle(x)).Count() >= num;
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
                if (IsVehicle(p))
                {
                    pawns.AddRange(p.GetComp<CompVehicle>().AllPawnsAboard);
                }
                pawns.Add(p);
            }
            return AbleToEmbark(pawns);
        }

        public static bool HasCannons(this Pawn p)
        {
            return !(p?.TryGetComp<CompCannons>() is null) ? true : false;
        }

        public static bool HasCannons(List<Pawn> pawns)
        {
            return pawns.All(x => x.HasCannons());
        }

        public static bool FueledVehicle(this Pawn p)
        {
            return IsVehicle(p) && !(p?.TryGetComp<CompFueledTravel>() is null) ? true : false;
        }

        //Needs case for captured ships?

        public static bool WillAutoJoinIfCaptured(Pawn ship)
        {
            return ship.GetComp<CompVehicle>().movementStatus != VehicleMovementStatus.Offline && !ship.GetComp<CompVehicle>().beached;
        }

        #endregion

        #region WorldMap
        public static bool IsWaterTile(int tile, List<Pawn> pawns = null)
        {
            return WaterCovered(tile) || Find.World.CoastDirectionAt(tile).IsValid || RiverIsValid(tile, pawns.Where(x => IsBoat(x)).ToList());
        }

        public static bool WaterCovered(int tile)
        {
            return Find.WorldGrid[tile].biome == BiomeDefOf.Ocean || Find.WorldGrid[tile].biome == BiomeDefOf.Lake;
        }

        public static bool IsNotWaterTile(int tile, List<Pawn> pawns = null)
        {
            return !IsWaterTile(tile, pawns);
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
                    List<Pawn> ships = caravan.PawnsListForReading.Where(x => IsBoat(x)).ToList();
                    for (int i = 0; i < ships.Count; i++)
                    {
                        Pawn ship = ships[i];
                        ship?.GetComp<CompVehicle>()?.DisembarkAll();
                    }
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
            List<Pawn> ships = caravan.PawnsListForReading.Where(x => IsBoat(x)).ToList();
            for (int i = 0; i < ships.Count; i++)
            {
                Pawn ship = ships[i];
                for (int j = 0; j < ship.GetComp<CompVehicle>().PawnCountToOperate; j++)
                {
                    if (sailors.Count <= 0)
                    {
                        return;
                    }
                    foreach (ShipHandler handler in ship.GetComp<CompVehicle>().handlers)
                    {
                        if (handler.AreSlotsAvailable)
                        {
                            ship.GetComp<CompVehicle>().Notify_BoardedCaravan(sailors.Pop(), handler.handlers);
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
                    Pawn ship = ships[x];
                    foreach (ShipHandler handler in ship.GetComp<CompVehicle>().handlers)
                    {
                        if (handler.AreSlotsAvailable)
                        {
                            ship.GetComp<CompVehicle>().Notify_BoardedCaravan(sailors.Pop(), handler.handlers);
                            break;
                        }
                    }
                    x = (x + 2) > ships.Count ? 0 : ++x;
                }
            }
        }

        public static bool RiverIsValid(int tileID, List<Pawn> ships)
        {
            if (!RimShipMod.mod.settings.riverTravel || ships is null || !ships.Any(x => IsBoat(x)))
                return false;
            bool flag = RimShipMod.mod.settings.boatSizeMatters ? (!Find.WorldGrid[tileID].Rivers.NullOrEmpty()) ? ShipsFitOnRiver(BiggestRiverOnTile(Find.WorldGrid[tileID]?.Rivers).river, ships) : false : (Find.WorldGrid[tileID].Rivers?.Any() ?? false);
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

        //Work In Progress
        public static bool TryFindClosestWaterTile(int sourceTile, int destinationTile, out int exitTile)
        {
            exitTile = -1;
            List<int> neighbors = new List<int>();
            Find.WorldGrid.GetTileNeighbors(sourceTile, neighbors);
            Rot4 dir = Find.WorldGrid.GetRotFromTo(sourceTile, destinationTile);
            foreach(int tile in neighbors)
            {
                if(WaterCovered(tile))
                {
                    exitTile = tile;

                }
            }
            return exitTile > 0;
        }

        #endregion

        #region CaravanFormation
        public static bool CanStartCaravan(List<Pawn> caravan)
        {
            int seats = 0;
            int pawns = 0;
            int prereq = 0;
            bool flag = caravan.Any(x => x.GetComp<CompVehicle>() != null); //Ships or No Ships
            if (flag)
            {
                foreach (Pawn p in caravan)
                {
                    if (IsVehicle(p))
                    {
                        seats += p.GetComp<CompVehicle>().SeatsAvailable;
                        prereq += p.GetComp<CompVehicle>().PawnCountToOperate - p.GetComp<CompVehicle>().AllCrewAboard.Count;
                    }
                    else if (p.IsColonistPlayerControlled && !p.Downed && !p.Dead)
                    {
                        pawns++;
                    }
                }
            }
            bool flag2 = flag ? pawns > seats : false; //Not Enough Room
            bool flag3 = flag ? pawns < prereq : false; //Not Enough Pawns to Sail
            if (flag2)
                Messages.Message("CaravanMustHaveEnoughSpaceOnShip".Translate(), MessageTypeDefOf.RejectInput, false);
            if (!caravan.Any(x => CaravanUtility.IsOwner(x, Faction.OfPlayer) && !x.Downed))
                Messages.Message("CaravanMustHaveAtLeastOneColonist".Translate(), MessageTypeDefOf.RejectInput, false);
            if (flag3)
                Messages.Message("CaravanMustHaveEnoughPawnsToOperate".Translate(prereq), MessageTypeDefOf.RejectInput, false);
            return !flag2 && !flag3;
        }
        #endregion

        #region GetData

        public static List<Pawn> GrabPawnsFromMapPawnsInVehicle(List<Pawn> allPawns)
        {
            List<Pawn> playerShips = allPawns.Where(x => x.Faction == Faction.OfPlayer && IsVehicle(x)).ToList();
            if (!playerShips.Any())
                return allPawns.Where(x => x.Faction == Faction.OfPlayer && x.RaceProps.Humanlike).ToList();
            return playerShips.RandomElement<Pawn>().GetComp<CompVehicle>()?.AllCapablePawns;
        }

        public static List<Pawn> GrabPawnsFromVehicles(List<Pawn> ships)
        {
            if (!ships.Any(x => IsVehicle(x)))
                return null;
            List<Pawn> pawns = new List<Pawn>();
            foreach (Pawn p in ships)
            {
                if (IsVehicle(p))
                    pawns.AddRange(p.GetComp<CompVehicle>().AllPawnsAboard);
                else
                    pawns.Add(p);
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
                if (IsVehicle(p))
                    ships.AddRange(p.GetComp<CompVehicle>().AllPawnsAboard);
                else
                    ships.Add(p);
            }
            return ships;
        }

        public static List<Pawn> GrabPawnsFromVehicleCaravanSilentFail(Caravan caravan)
        {
            if (caravan is null || !HasVehicle(caravan))
                return null;
            List<Pawn> ships = new List<Pawn>();
            foreach (Pawn p in caravan.PawnsListForReading)
            {
                if (IsVehicle(p))
                    ships.AddRange(p.GetComp<CompVehicle>().AllPawnsAboard);
                else
                    ships.Add(p);
            }
            return ships;
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

        public static bool IsFormingCaravanShipHelper(Pawn p)
        {
            Lord lord = p.GetLord();
            return !(lord is null) && lord.LordJob is LordJob_FormAndSendCaravanShip;
        }

        public static List<Pawn> ExtractPawnsFromCaravan(Caravan caravan)
        {
            List<Pawn> sailors = new List<Pawn>();

            foreach (Pawn ship in caravan.PawnsListForReading)
            {
                if (IsVehicle(ship))
                {
                    sailors.AddRange(ship.GetComp<CompVehicle>().AllPawnsAboard);
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

        public static float ExtractUpgradeValue(Pawn vehicle, StatUpgrade stat)
        {
            if(IsVehicle(vehicle) && HasUpgradeMenu(vehicle))
            {
                switch(stat)
                {
                    case StatUpgrade.Armor:
                        return vehicle.GetComp<CompVehicle>().ArmorPoints;
                    case StatUpgrade.Speed:
                        return vehicle.GetComp<CompVehicle>().MoveSpeedModifier;
                    case StatUpgrade.CargoCapacity:
                        return vehicle.GetComp<CompVehicle>().CargoCapacity;
                    case StatUpgrade.FuelConsumptionRate:
                        return vehicle.GetComp<CompFueledTravel>().FuelEfficiency;
                    case StatUpgrade.FuelCapacity:
                        return vehicle.GetComp<CompFueledTravel>().FuelCapacity;
                }
            }
            return 0f;
        }

        public static List<Pawn> GetVehiclesForColonistBar(List<Map> maps, int i)
        {
            Map map = maps[i];
            List<Pawn> vehicles = new List<Pawn>();
            foreach (Pawn vehicle in map.mapPawns.AllPawnsSpawned)
            {
                if (IsVehicle(vehicle))
                {
                    //ships.Add(ship); /*  Uncomment to add Ships to colonist bar  */
                    foreach (Pawn p in vehicle.GetComp<CompVehicle>().AllPawnsAboard)
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
                         where IsVehicle(x)
                         select x;
            return candidates;
        }

        public static Pawn UsableVehicleWithTheMostFreeSpace(Pawn pawn)
        {
            
            Pawn carrierPawn = null;
            float num = 0f;
            foreach(Pawn p in UsableCandidatesForCargo(pawn))
            {
                if(IsVehicle(p) && p != pawn && pawn.CanReach(p, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
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

        public static int GetTickMultiplier(char c)
		{
			switch(char.ToLower(c))
			{
				case 'h':
					return 2500;
				case 'd':
					return 60000;
				case 'q':
					return 900000;
				case 'y':
					return 3600000;
				case 't':
					return 1;
			}
            throw new NotSupportedException($"Unable to Parse {c} in RimWorld Duration String.");
		}

        public static string TicksToRealTime(int ticks)
        {
            if (ticks <= 0)
                return "00:00:00:00";

            int seconds = ticks / 60;
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return time.ToString(@"dd\:hh\:mm\:ss");
        }

        public static string TicksToGameTime(int ticks)
        {
            if (ticks <= 0)
                return "00:00:00:00";
            int days = Math.DivRem(ticks, 60000, out int hourRemainder);
			int hours = Math.DivRem(hourRemainder, 2500, out int minuteRemainder);
			int minutes = Math.DivRem(minuteRemainder, 42, out int secondRemainder);
			int seconds = Math.DivRem(secondRemainder, 7, out int runoff);

            return $"{days.ToString("D2")}:{hours.ToString("D2")}:{minutes.ToString("D2")}:{seconds.ToString("D2")}";
        }

        #endregion

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
            if (selPawns.Any(x => x.Faction != Faction.OfPlayer || IsVehicle(x)))
                return false;
            IntVec3 mousePos = Verse.UI.MouseMapPosition().ToIntVec3();
            if (selectedObjects.Count > 1 && selectedObjects.All(x => x is Pawn))
            {
                foreach (Thing thing in selPawns[0].Map.thingGrid.ThingsAt(mousePos))
                {
                    if (IsVehicle(thing))
                    {
                        (thing as Pawn).GetComp<CompVehicle>().MultiplePawnFloatMenuOptions(selPawns);
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion

        #region Rendering
        public static Pair<float,float> ShipDrawOffset(CompVehicle shipComp, float xOffset, float yOffset)
        {
            switch(shipComp.Pawn.Rotation.AsInt)
            {
                //East
                case 1:
                    if(shipComp.Angle == 45)
                    {
                        SPTuple2<float,float> rotatedOffset = SPTrig.RotatePointClockwise(yOffset, -xOffset, 45f);
                        return new Pair<float, float>(rotatedOffset.First, rotatedOffset.Second);
                    }
                    else if(shipComp.Angle == -45)
                    {
                        SPTuple2<float,float> rotatedOffset = SPTrig.RotatePointCounterClockwise(yOffset, -xOffset, 45f);
                        return new Pair<float, float>(rotatedOffset.First, rotatedOffset.Second);
                    }
                    return new Pair<float, float>(yOffset, -xOffset);
                //South
                case 2:
                    return new Pair<float, float>(-xOffset, -yOffset);
                //West
                case 3:
                    if(shipComp.Angle != 0)
                    {
                        if(shipComp.Angle == 45)
                        {
                            SPTuple2<float,float> rotatedOffset = SPTrig.RotatePointClockwise(-yOffset, xOffset, 45f);
                            return new Pair<float, float>(rotatedOffset.First, rotatedOffset.Second);
                        }
                        else if(shipComp.Angle == -45)
                        {
                            SPTuple2<float,float> rotatedOffset = SPTrig.RotatePointCounterClockwise(-yOffset, xOffset, 45f);
                            return new Pair<float, float>(rotatedOffset.First, rotatedOffset.Second);
                        }
                    }
                    return new Pair<float, float>(-yOffset, xOffset);
                //North
                default:
                    return new Pair<float, float>(xOffset, yOffset);
            }
        }

        public static float CustomFloatBeach(Map map)
        {
            float mapSizeMultiplier = (float)(map.Size.x >= map.Size.z ? map.Size.x : map.Size.z) / 250f;
            float beach = 60f; //Rand.Range(40f, 80f);
            return (float)(beach + (beach * (RimShipMod.mod.settings.beachMultiplier) / 100f)) * mapSizeMultiplier;
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
                    ShipHarmony.tiles.Add(new Pair<int, int>(tileID, 0));
                return tileID;
            }

            while (searchedRadius < RimShipMod.mod.settings.CoastRadius)
            {
                for (int j = 0; j < stackFull.Count; j++)
                {
                    searchTile = stack.Pop();
                    SPExtra.GetList<int>(Find.WorldGrid.tileIDToNeighbors_offsets, Find.WorldGrid.tileIDToNeighbors_values, searchTile, neighbors);
                    int count = neighbors.Count;
                    for (int i = 0; i < count; i++)
                    {
                        if (allSearchedTiles.Any(x => x == neighbors[i]))
                            continue;
                        newTilesSearch.Add(neighbors[i]);
                        allSearchedTiles.Add(neighbors[i]);
                        if (Find.World.CoastDirectionAt(neighbors[i]).IsValid)
                        {
                            if (Find.WorldGrid[neighbors[i]].biome.canBuildBase && Find.WorldGrid[neighbors[i]].biome.implemented && Find.WorldGrid[neighbors[i]].hilliness != Hilliness.Impassable)
                            {
                                if(ShipHarmony.debug && !(faction is null)) DebugDrawSettlement(tileID, neighbors[i]);
                                if(!(faction is null))
                                    ShipHarmony.tiles.Add(new Pair<int, int>(neighbors[i], searchedRadius));
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
            PeaceTalks o = (PeaceTalks)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfShips.DebugSettlement);
            o.Tile = from;
            o.SetFaction(Faction.OfMechanoids);
            Find.WorldObjects.Add(o);
            if (ShipHarmony.drawPaths)
                ShipHarmony.debugLines.Add(Find.WorldPathFinder.FindPath(from, to, null, null));
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

        public static void DrawAttachedThing(Texture2D baseTexture, Graphic baseGraphic, Vector2 baseRenderLocation,Vector2 baseDrawSize,
            Texture2D texture, Graphic graphic, Vector2 renderLocation, Vector2 renderOffset, Material baseMat, Material mat, float rotation, Pawn parent, int drawLayer)
        {
            if (texture != null && renderLocation != null)
            {
                try
                {
                    Vector3 topVectorRotation = new Vector3(renderOffset.x, 1f, renderOffset.y).RotatedBy(rotation);
                    Pair<float, float> drawOffset = ShipDrawOffset(parent.GetComp<CompVehicle>(), renderLocation.x, renderLocation.y);
                    Vector3 topVectorLocation = new Vector3(parent.DrawPos.x + drawOffset.First, parent.DrawPos.y + drawLayer, parent.DrawPos.z + drawOffset.Second);
                    Mesh cannonMesh = graphic.MeshAt(Rot4.North);

                    if(RimShipMod.mod.settings.debugDrawCannonGrid)
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
                        Pair<float, float> baseDrawOffset = HelperMethods.ShipDrawOffset(parent.GetComp<CompVehicle>(), baseRenderLocation.x, baseRenderLocation.y);
                        Vector3 baseVectorLocation = new Vector3(parent.DrawPos.x + baseDrawOffset.First, parent.DrawPos.y, parent.DrawPos.z + baseDrawOffset.Second);
                        baseMatrix.SetTRS(baseVectorLocation + Altitudes.AltIncVect, parent.Rotation.AsQuat, new Vector3(baseDrawSize.x, 1f, baseDrawSize.y));
                        Graphics.DrawMesh(MeshPool.plane10, baseMatrix, baseMat, 0);
                    }

                    if(RimShipMod.mod.settings.debugDrawCannonGrid)
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
        #endregion

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

        public static void FillableBarLabeled(Rect rect, float fillPercent, string label, StatUpgrade upgrade, Texture2D innerTex, Texture2D outlineTex, float? actualValue = null, float addedValue = 0f, float bgFillPercent = 0f)
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
                case StatUpgrade.Armor:
                    fillTex = ArmorStatBarTexture;
                    addedFillTex = ArmorAddedStatBarTexture;
                    break;
                case StatUpgrade.Speed:
                    fillTex = SpeedStatBarTexture;
                    addedFillTex = SpeedAddedStatBarTexture;
                    break;
                case StatUpgrade.CargoCapacity:
                    fillTex = CargoStatBarTexture;
                    addedFillTex = CargoAddedStatBarTexture;
                    break;
                case StatUpgrade.FuelConsumptionRate:
                    fillTex = FuelStatBarTexture;
                    addedFillTex = FuelAddedStatBarTexture;
                    break;
                case StatUpgrade.FuelCapacity:
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

        public static void DoItemsListForShip(Rect inRect, ref float curY, ref List<Thing> tmpSingleThing, ITab_Pawn_FormingCaravan instance)
        {
            LordJob_FormAndSendCaravanShip lordJob_FormAndSendCaravanShip = (LordJob_FormAndSendCaravanShip)(Find.Selector.SingleSelectedThing as Pawn).GetLord().LordJob;
            Rect position = new Rect(0f, curY, (inRect.width - 10f) / 2f, inRect.height);
            float a = 0f;
            GUI.BeginGroup(position);
            Widgets.ListSeparator(ref a, position.width, "ItemsToLoad".Translate());
            bool flag = false;
            foreach (TransferableOneWay transferableOneWay in lordJob_FormAndSendCaravanShip.transferables)
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
            foreach (Pawn pawn in lordJob_FormAndSendCaravanShip.lord.ownedPawns)
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
        #endregion

        public static CannonTargeter CannonTargeter = new CannonTargeter();
        public static Texture2D missingIcon;

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
    }
}
