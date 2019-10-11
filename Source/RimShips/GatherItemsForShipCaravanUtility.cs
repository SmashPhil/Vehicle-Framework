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
using RimShips.Build;
using RimShips.Defs;
using RimShips.Lords;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips
{
    public static class GatherItemsForShipCaravanUtility
    {
        public static Thing FindThingToHaul(Pawn p, Lord lord)
        {
            GatherItemsForShipCaravanUtility.neededItems.Clear();
            List<TransferableOneWay> transferables = ((LordJob_FormAndSendCaravanShip)lord.LordJob).transferables;
            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableOneWay transferableOneWay = transferables[i];
                if (GatherItemsForShipCaravanUtility.CountLeftToTransfer(p, transferableOneWay, lord) > 0)
                {
                    for (int j = 0; j < transferableOneWay.things.Count; j++)
                    {
                        GatherItemsForShipCaravanUtility.neededItems.Add(transferableOneWay.things[j]);
                    }
                }
            }
            if (!GatherItemsForShipCaravanUtility.neededItems.Any<Thing>())
            {
                return null;
            }
            Thing result = GenClosest.ClosestThingReachable(p.Position, p.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), 
                PathEndMode.Touch, TraverseParms.For(p, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, (Thing x) => 
                GatherItemsForShipCaravanUtility.neededItems.Contains(x) && p.CanReserve(x, 1, -1, null, false), null, 0, -1, false, RegionType.Set_Passable, false);
            GatherItemsForShipCaravanUtility.neededItems.Clear();
            return result;
        }

        public static int CountLeftToTransfer(Pawn pawn, TransferableOneWay transferable, Lord lord)
        {
            if (transferable.CountToTransfer <= 0 || !transferable.HasAnyThing)
            {
                return 0;
            }
            return Mathf.Max(transferable.CountToTransfer - GatherItemsForShipCaravanUtility.TransferableCountHauledByOthers(pawn, transferable, lord), 0);
        }

        private static int TransferableCountHauledByOthers(Pawn pawn, TransferableOneWay transferable, Lord lord)
        {
            if (!transferable.HasAnyThing)
            {
                Log.Warning("Can't determine transferable count hauled by others because transferable has 0 things.", false);
                return 0;
            }
            List<Pawn> allPawnsSpawned = lord.Map.mapPawns.AllPawnsSpawned;
            int num = 0;
            for (int i = 0; i < allPawnsSpawned.Count; i++)
            {
                Pawn pawn2 = allPawnsSpawned[i];
                if (pawn2 != pawn)
                {
                    if (pawn2.CurJob != null && pawn2.CurJob.def == JobDefOf.PrepareCaravan_GatherItems && pawn2.CurJob.lord == lord)
                    {
                        Thing toHaul = ((JobDriver_PrepareCaravan_GatherItems)pawn2.jobs.curDriver).ToHaul;
                        if (transferable.things.Contains(toHaul) || TransferableUtility.TransferAsOne(transferable.AnyThing, toHaul, TransferAsOneMode.PodsOrCaravanPacking))
                        {
                            num += toHaul.stackCount;
                        }
                    }
                }
            }
            return num;
        }

        private static HashSet<Thing> neededItems = new HashSet<Thing>();
    }
}
