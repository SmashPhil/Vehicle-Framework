using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using RimShips.AI;
using RimShips.Lords;
using RimShips.Defs;
using HarmonyLib;
using SPExtended;

namespace RimShips
{
    public static class HelperMethods
    {
        public static bool IsShipDef(ThingDef td)
        {
            return td?.GetCompProperties<CompProperties_Ships>() != null;
        }

        public static bool BoatCantTraverse(int tile)
        {
            bool flag = !WaterCovered(tile) && (!Find.World.CoastDirectionAt(tile).IsValid || Find.World.Impassable(tile));
            bool riverFlag = false;
            if (ShipHarmony.currentFormingCaravan != null || ((Find.WorldSelector.SelectedObjects.Any() && Find.WorldSelector.SelectedObjects.All(x => x is Caravan && (x as Caravan).IsPlayerControlled && 
                HasShip(x as Caravan))) && !ShipHarmony.routePlannerActive))
            {
                List<Pawn> pawns = ShipHarmony.currentFormingCaravan is null ? GrabShipsFromCaravans(Find.WorldSelector.SelectedObjects.Cast<Caravan>().ToList()) : 
                    TransferableUtility.GetPawnsFromTransferables(ShipHarmony.currentFormingCaravan.transferables);
                riverFlag = !pawns.Any(x => IsShip(x)) ? false : RiverIsValid(tile, pawns);
            }
            return flag && !riverFlag;
        }
        public static bool IsWaterTile(int tile, List<Pawn> pawns = null)
        {
            return WaterCovered(tile) || Find.World.CoastDirectionAt(tile).IsValid || RiverIsValid(tile, pawns.Where(x => IsShip(x)).ToList());
        }

        public static bool WaterCovered(int tile)
        {
            return Find.WorldGrid[tile].biome == BiomeDefOf.Ocean || Find.WorldGrid[tile].biome == BiomeDefOf.Lake;
        }

        public static bool IsNotWaterTile(int tile, List<Pawn> pawns = null)
        {
            return !IsWaterTile(tile, pawns);
        }

        public static bool CanSetSail(List<Pawn> caravan)
        {
            int seats = 0;
            int pawns = 0;
            int prereq = 0;
            bool flag = caravan.Any(x => !(x.GetComp<CompShips>() is null)); //Ships or No Ships
            if (flag)
            {
                foreach (Pawn p in caravan)
                {
                    if (IsShip(p))
                    {
                        seats += p.GetComp<CompShips>().SeatsAvailable;
                        prereq += p.GetComp<CompShips>().PawnCountToOperate - p.GetComp<CompShips>().AllCrewAboard.Count;
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

        public static void ToggleDocking(Caravan caravan, bool dock = false)
        {
            if (HasShip(caravan))
            {
                if (!dock)
                {
                    BoardAllCaravanPawns(caravan);
                }
                else
                {
                    List<Pawn> ships = caravan.PawnsListForReading.Where(x => IsShip(x)).ToList();
                    for (int i = 0; i < ships.Count; i++)
                    {
                        Pawn ship = ships[i];
                        ship?.GetComp<CompShips>()?.DisembarkAll();
                    }
                }
            }
        }

        public static void SpawnDockedBoatObject(Caravan caravan)
        {
            if (!HasShip(caravan))
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

            List<Pawn> sailors = caravan.PawnsListForReading.Where(x => !IsShip(x)).ToList();
            List<Pawn> ships = caravan.PawnsListForReading.Where(x => IsShip(x)).ToList();
            for (int i = 0; i < ships.Count; i++)
            {
                Pawn ship = ships[i];
                for (int j = 0; j < ship.GetComp<CompShips>().PawnCountToOperate; j++)
                {
                    if (sailors.Count <= 0)
                    {
                        return;
                    }
                    foreach (ShipHandler handler in ship.GetComp<CompShips>().handlers)
                    {
                        if (handler.AreSlotsAvailable)
                        {
                            ship.GetComp<CompShips>().Notify_BoardedCaravan(sailors.Pop(), handler.handlers);
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
                    foreach (ShipHandler handler in ship.GetComp<CompShips>().handlers)
                    {
                        if (handler.AreSlotsAvailable)
                        {
                            ship.GetComp<CompShips>().Notify_BoardedCaravan(sailors.Pop(), handler.handlers);
                            break;
                        }
                    }
                    x = (x + 2) > ships.Count ? 0 : ++x;
                }
            }
        }

        public static void MultiSelectClicker(List<object> selectedObjects)
        {
            if (!selectedObjects.All(x => x is Pawn))
                return;
            List<Pawn> selPawns = new List<Pawn>();
            foreach(object o in selectedObjects)
            {
                if(o is Pawn)
                    selPawns.Add(o as Pawn);
            }
            if (selPawns.Any(x => x.Drafted || x.Faction != Faction.OfPlayer || IsShip(x)))
                return;
            IntVec3 mousePos = Verse.UI.MouseMapPosition().ToIntVec3();
            if (selectedObjects.Count > 1 && selectedObjects.All(x => x is Pawn))
            {
                foreach (Thing thing in selPawns[0].Map.thingGrid.ThingsAt(mousePos))
                {
                    if (IsShip(thing))
                    {
                        (thing as Pawn).GetComp<CompShips>().MultiplePawnFloatMenuOptions(selPawns);
                        return;
                    }
                }
            }
        }

        public static bool IsShip(Pawn p)
        {
            return !(p?.TryGetComp<CompShips>() is null) ? true : false;
        }

        public static bool IsShip(Thing t)
        {
            return IsShip(t as Pawn);
        }

        public static bool IsShip(ThingDef td)
        {
            return td.GetCompProperties<CompProperties_Ships>() != null;
        }

        public static bool HasShip(List<Pawn> pawns)
        {
            return pawns?.Any(x => IsShip(x)) ?? false;
        }

        public static bool HasShip(IEnumerable<Pawn> pawns)
        {
            return pawns?.Any(x => IsShip(x)) ?? false;
        }

        public static bool HasShip(Caravan c)
        {
            return (c is null) ? (ShipHarmony.currentFormingCaravan is null) ? false : HasShip(TransferableUtility.GetPawnsFromTransferables(ShipHarmony.currentFormingCaravan.transferables)) : HasShip(c?.PawnsListForReading);
        }

        public static bool HasShipInCaravan(Pawn p)
        {
            return p.IsFormingCaravan() && p.GetLord().LordJob is LordJob_FormAndSendCaravanShip && p.GetLord().ownedPawns.Any(x => IsShip(x));
        }

        public static bool HasEnoughSpacePawns(List<Pawn> pawns)
        {
            int num = 0;
            foreach (Pawn p in pawns.Where(x => IsShip(x)))
            {
                num += p.GetComp<CompShips>().TotalSeats;
            }
            return pawns.Where(x => !IsShip(x)).Count() <= num;
        }

        public static bool HasEnoughPawnsToEmbark(List<Pawn> pawns)
        {
            int num = 0;
            foreach (Pawn p in pawns.Where(x => IsShip(x)))
            {
                num += p.GetComp<CompShips>().PawnCountToOperate;
            }
            return pawns.Where(x => !IsShip(x)).Count() >= num;
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
                if (IsShip(p))
                {
                    pawns.AddRange(p.GetComp<CompShips>().AllPawnsAboard);
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

        public static List<Pawn> GrabPawnsFromMapPawnsInShip(List<Pawn> allPawns)
        {
            List<Pawn> playerShips = allPawns.Where(x => x.Faction == Faction.OfPlayer && IsShip(x)).ToList();
            if (!playerShips.Any())
                return allPawns.Where(x => x.Faction == Faction.OfPlayer && x.RaceProps.Humanlike).ToList();
            return playerShips.RandomElement<Pawn>().GetComp<CompShips>()?.AllCapablePawns;
        }

        public static List<Pawn> GrabPawnsFromShips(List<Pawn> ships)
        {
            if (!ships.Any(x => IsShip(x)))
                return null;
            List<Pawn> pawns = new List<Pawn>();
            foreach (Pawn p in ships)
            {
                if (IsShip(p))
                    pawns.AddRange(p.GetComp<CompShips>().AllPawnsAboard);
                else
                    pawns.Add(p);
            }
            return pawns;
        }

        public static List<Pawn> GrabPawnsIfShips(List<Pawn> pawns)
        {
            if (pawns is null)
                return null;
            if (!HasShip(pawns))
            {
                return pawns;
            }
            List<Pawn> ships = new List<Pawn>();
            foreach (Pawn p in pawns)
            {
                if (IsShip(p))
                    ships.AddRange(p.GetComp<CompShips>().AllPawnsAboard);
                else
                    ships.Add(p);
            }
            return ships;
        }

        public static List<Pawn> GrabPawnsFromShipCaravanSilentFail(Caravan caravan)
        {
            if (caravan is null || !HasShip(caravan))
                return null;
            List<Pawn> ships = new List<Pawn>();
            foreach (Pawn p in caravan.PawnsListForReading)
            {
                if (IsShip(p))
                    ships.AddRange(p.GetComp<CompShips>().AllPawnsAboard);
                else
                    ships.Add(p);
            }
            return ships;
        }

        public static List<Pawn> GrabShipsFromCaravans(List<Caravan> caravans)
        {
            if (!caravans.All(x => HasShip(x)))
                return null;
            List<Pawn> ships = new List<Pawn>();
            foreach (Caravan c in caravans)
            {
                ships.AddRange(c.PawnsListForReading.Where(x => IsShip(x)));
            }
            return ships;
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
                RiverIsValid(tileID, caravan.PawnsListForReading.Where(x => IsShip(x)).ToList());
            bool flag = Find.WorldGrid[tileID].biome == BiomeDefOf.Ocean || Find.WorldGrid[tileID].biome == BiomeDefOf.Lake;
            return HasShip(caravan) ? (!WaterCovered(tileID) && !(Find.World.CoastDirectionAt(tileID).IsValid && tileID == destTile) &&
                !(RimShipMod.mod.settings.riverTravel && riverValid)) : (flag || world.Impassable(tileID));
        }

        public static bool ImpassableForBoatPlanner(int tileID, int destTile = 0)
        {
            bool flag = Find.WorldGrid[tileID].biome == BiomeDefOf.Ocean || Find.WorldGrid[tileID].biome == BiomeDefOf.Lake || (tileID == destTile && Find.World.CoastDirectionAt(tileID).IsValid);
            return !flag;
        }

        public static bool RiverIsValid(int tileID, List<Pawn> ships)
        {
            if (!RimShipMod.mod.settings.riverTravel || ships is null || !ships.Any(x => IsShip(x)))
                return false;
            bool flag = RimShipMod.mod.settings.boatSizeMatters ? (Find.WorldGrid[tileID].Rivers?.Any() ?? false) ? ShipsFitOnRiver(BiggestRiverOnTile(Find.WorldGrid[tileID]?.Rivers).river, ships) : false : (Find.WorldGrid[tileID].Rivers?.Any() ?? false);
            return flag;
        }

        public static Tile.RiverLink BiggestRiverOnTile(List<Tile.RiverLink> list)
        {
            return list.MaxBy(x => x.river.GetRiverSize());
        }

        public static bool ShipsFitOnRiver(RiverDef river, List<Pawn> pawns)
        {
            foreach (Pawn p in pawns.Where(x => IsShip(x)))
            {
                if ((p.def.GetCompProperties<CompProperties_Ships>()?.riverTraversability?.GetRiverSize() ?? 5) > river.GetRiverSize())
                    return false;
            }
            return true;
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

        //Needs case for captured ships?

        public static bool WillAutoJoinIfCaptured(Pawn ship)
        {
            return ship.GetComp<CompShips>().movementStatus != ShipMovementStatus.Offline && !ship.GetComp<CompShips>().beached;
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
                if (IsShip(ship))
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

                if (pawn.GetComp<CompShips>().beached) break;
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

        public static List<Pawn> GetShipsForColonistBar(List<Map> maps, int i)
        {
            Map map = maps[i];
            List<Pawn> ships = new List<Pawn>();
            foreach (Pawn ship in map.mapPawns.AllPawnsSpawned)
            {
                if (IsShip(ship))
                {
                    //ships.Add(ship); /*  Uncomment to add Ships to colonist bar  */
                    foreach (Pawn p in ship.GetComp<CompShips>().AllPawnsAboard)
                    {
                        if (p.IsColonist)
                            ships.Add(p);
                    }
                }
            }
            return ships;
        }

        public static bool OnDeepWater(this Pawn pawn)
        {
            //Splitting Caravan?
            if (pawn?.Map is null && pawn.IsWorldPawn())
                return false;
            return (pawn.Map.terrainGrid.TerrainAt(pawn.Position) == TerrainDefOf.WaterDeep || pawn.Map.terrainGrid.TerrainAt(pawn.Position) == TerrainDefOf.WaterMovingChestDeep ||
                pawn.Map.terrainGrid.TerrainAt(pawn.Position) == TerrainDefOf.WaterOceanDeep) && GenGrid.Impassable(pawn.Position, pawn.Map);
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

        public static IEnumerable<Pawn> UsableCandidatesForCargo(Pawn pawn)
        {
            IEnumerable<Pawn> candidates = (!pawn.IsFormingCaravan()) ? pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction) : pawn.GetLord().ownedPawns;
            candidates = from x in candidates
                         where IsShip(x)
                         select x;
            return candidates;
        }

        public static Pawn UsableBoatWithTheMostFreeSpace(Pawn pawn)
        {
            
            Pawn carrierPawn = null;
            float num = 0f;
            foreach(Pawn p in UsableCandidatesForCargo(pawn))
            {
                if(IsShip(p) && p != pawn && pawn.CanReach(p, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
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
    }
}
