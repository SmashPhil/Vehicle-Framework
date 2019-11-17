using RimWorld;
using RimWorld.Planet;
using Verse;
using Harmony;
using RimShips.Defs;
using System.Collections.Generic;

namespace RimShips
{
    public class ShipHandler : IExposable, ILoadReferenceable, IThingHolder
    {
        public ThingOwner<Pawn> handlers;

        public List<BodyPartRecord> occupiedParts;

        public ShipRole role;

        public List<Pawn> currentlyReserving = new List<Pawn>();

        private List<Pawn> tempSavedPawns = new List<Pawn>();

        public int uniqueID = -1;
        public Pawn shipPawn;
         
        public ShipHandler()
        {
            if(handlers is null)
            {
                handlers = new ThingOwner<Pawn>(this, false, LookMode.Reference);
            }
        }

        public ShipHandler(Pawn newShip)
        {
            uniqueID = Find.UniqueIDsManager.GetNextThingID();
            shipPawn = newShip;
            if(handlers is null)
            {
                handlers = new ThingOwner<Pawn>(this, false, LookMode.Reference);
            }
        }

        public ShipHandler(Pawn newShip, ShipRole newRole)
        {
            uniqueID = Find.UniqueIDsManager.GetNextThingID();
            shipPawn = newShip;
            role = newRole;
            if (handlers is null)
            {
                handlers = new ThingOwner<Pawn>(this, false, LookMode.Reference);
            }
        }

        public ShipHandler(Pawn newShip, ShipRole newRole, List<Pawn> newHandlers)
        {
            uniqueID = Find.UniqueIDsManager.GetNextThingID();
            shipPawn = newShip;
            role = newRole;
            if (handlers is null)
            {
                handlers = new ThingOwner<Pawn>(this, false, LookMode.Reference);
            }
            if(currentlyReserving is null)
            {
                currentlyReserving = new List<Pawn>();
            }
            if((newHandlers?.Count ?? 0) > 0)
            {
                foreach(Pawn p in newHandlers)
                {
                    if(p.Spawned) { p.DeSpawn(); }
                    if(p.holdingOwner != null) { p.holdingOwner = null; }
                    if (!p.IsWorldPawn()) { Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.Decide); }
                }
                handlers.TryAddRangeOrTransfer(newHandlers);
            }
        }

        public void ReservationHandler()
        {
            if (currentlyReserving is null) currentlyReserving = new List<Pawn>();

            for(int i = 0; i < currentlyReserving.Count; i++)
            {
                Pawn p = currentlyReserving[i];
                if (!p.Spawned || (p.CurJob.def != JobDefOf_Ships.Board && (p.CurJob.targetA.Thing as Pawn) != this.shipPawn))
                {
                    currentlyReserving.Remove(p);
                }
            }
        }

        public bool AreSlotsAvailable
        {
            get
            { 
                return !(role is null) && ((this?.handlers?.Count ?? 0) + (currentlyReserving?.Count ?? 0)) >= role.slots ? false : true;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref uniqueID, "uniqueID", -1);
            Scribe_References.Look(ref shipPawn, "shipPawn");
            Scribe_Deep.Look(ref role, "role");

            if(Scribe.mode == LoadSaveMode.Saving)
            {
                tempSavedPawns.Clear();
                tempSavedPawns.AddRange(handlers.InnerListForReading);
                handlers.RemoveAll(x => x is Pawn);
            }

            Scribe_Collections.Look(ref tempSavedPawns, "tempSavedPawns", LookMode.Reference);
            Scribe_Collections.Look(ref currentlyReserving, "currentlyReserving", LookMode.Deep);
            Scribe_Deep.Look(ref handlers, "handlers", this);

            if(Scribe.mode == LoadSaveMode.PostLoadInit || Scribe.mode == LoadSaveMode.Saving)
            {
                for(int j = 0; j < tempSavedPawns.Count; j++)
                {
                    handlers.TryAdd(tempSavedPawns[j], true);
                }
                tempSavedPawns.Clear();
            }
        }

        public string GetUniqueLoadID()
        {
            return "ShipHandlerGroup_" + uniqueID;
        }

        public IThingHolder ParentHolder => shipPawn;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return handlers;
        }
    }
}
