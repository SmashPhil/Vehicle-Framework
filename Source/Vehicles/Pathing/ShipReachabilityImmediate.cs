using Verse;
using Verse.AI;


namespace Vehicles.AI
{
    public static class ShipReachabilityImmediate
    {
        public static bool CanReachImmediateShip(IntVec3 start, LocalTargetInfo target, Map map, PathEndMode peMode, Pawn pawn)
        {
            if (!target.IsValid) return false;
            target = (LocalTargetInfo)GenPathShip.ResolvePathMode(pawn, target.ToTargetInfo(map), ref peMode);
            if(target.HasThing)
            {
                Thing thing = target.Thing;
                if(!thing.Spawned)
                {
                    if(!(pawn is null))
                    {
                        if (pawn.carryTracker.innerContainer.Contains(thing))
                        {
                            return true;
                        }
                        if (pawn.inventory.innerContainer.Contains(thing))
                        {
                            return true;
                        }
                        if (pawn.apparel != null && pawn.apparel.Contains(thing))
                        {
                            return true;
                        }
                        if (pawn.equipment != null && pawn.equipment.Contains(thing))
                        {
                            return true;
                        }
                    }
                    return false;
                }
                if(thing.Map != map)
                {
                    return false;
                }
            }
            if(!target.HasThing || target.Thing.def.size.x == 1 && target.Thing.def.size.z == 1)
            {
                if (start == target.Cell) return true;
            }
            else if(start.IsInside(target.Thing))
            {
                return true;
            }
            return peMode == PathEndMode.Touch && TouchPathEndModeUtilityShips.IsAdjacentOrInsideAndAllowedToTouch(start, target, map);
        }

        public static bool CanReachImmediateShip(this Pawn pawn, LocalTargetInfo target, PathEndMode peMode)
        {
            return pawn.Spawned && ShipReachabilityImmediate.CanReachImmediateShip(pawn.Position, target, pawn.Map, peMode, pawn);
        }

        public static bool CanReachImmediateNonLocalShip(this Pawn pawn, TargetInfo target, PathEndMode peMode)
        {
            return pawn.Spawned && (target.Map is null || target.Map == pawn.Map) && pawn.CanReachImmediateShip((LocalTargetInfo)target, peMode);
        }

        public static bool CanReachImmediateShip(IntVec3 start, CellRect rect, Map map, PathEndMode peMode, Pawn pawn)
        {
            IntVec3 c = rect.ClosestCellTo(start);
            return ShipReachabilityImmediate.CanReachImmediateShip(start, c, map, peMode, pawn);
        }
    }
}
