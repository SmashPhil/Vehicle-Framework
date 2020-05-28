using RimWorld;
using Verse;
using Verse.AI;

namespace Vehicles.AI
{
    public static class GenPathShip
    {
        public static TargetInfo ResolvePathMode(Pawn pawn, TargetInfo dest, ref PathEndMode peMode, MapExtension mapE)
        {
            if(dest.HasThing && dest.Thing.Spawned)
            {
                peMode = PathEndMode.Touch;
                return dest;
            }
            if(peMode == PathEndMode.InteractionCell)
            {
                if(!dest.HasThing)
                {
                    Log.Error("Pathed to cell " + dest + " with PathEndMode.InteractionCell.", false);
                }
                peMode = PathEndMode.OnCell;
                return new TargetInfo(dest.Thing.InteractionCell, dest.Thing.Map, false);
            }
            if(peMode == PathEndMode.ClosestTouch)
            {
                peMode = GenPathShip.ResolveClosestTouchPathMode(pawn, mapE, dest.Cell);
            }
            return dest;
        }

        public static PathEndMode ResolveClosestTouchPathMode(Pawn pawn, MapExtension mapE, IntVec3 target)
        {
            if(GenPathShip.ShouldNotEnterCell(pawn, mapE, target))
            {
                return PathEndMode.Touch;
            }
            return PathEndMode.OnCell;
        }

        private static bool ShouldNotEnterCell(Pawn pawn, MapExtension mapE, IntVec3 dest)
        {
            if(mapE.getShipPathGrid.PerceivedPathCostAt(dest) > 30)
            {
                return true;
            }
            if(!GenGridShips.Walkable(dest, mapE))
            {
                return true;
            }
            if(!(pawn is null) && !(pawn.GetComp<CompVehicle>() is null))
            {
                if(dest.IsForbidden(pawn))
                {
                    return true;
                }
                //Add utility for doors later?
            }

            return false;
        }
    }
}
