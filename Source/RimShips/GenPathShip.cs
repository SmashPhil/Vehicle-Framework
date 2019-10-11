using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using RimShips.AI;
using RimShips.Jobs;
using RimShips.UI;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips.AI
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
            if(!(pawn is null) && !(pawn.GetComp<CompShips>() is null))
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
