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
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips
{
    public enum ShipSeats { Captain, Cannon, Crew, Passenger }
    public enum ShipWeaponStatus { Offline, Online }
    public enum ShipMovementStatus { Offline, Online }

    public class CompShips : ThingComp
    {
        public List<Jobs.Bill_BoardShip> bills = new List<Jobs.Bill_BoardShip>();

        public bool draftStatusChanged = false;
        public bool beached = false;
        public float rotation = 0f;

        public List<ShipHandler> handlers = new List<ShipHandler>();
        public Rot4 lastDirection = Rot4.South;
        public ShipMovementStatus movementStatus = ShipMovementStatus.Online;
        public List<ThingCountClass> repairCostList = new List<ThingCountClass>();

        public bool CanMoveOnLand = false;
        public bool ResolvedITTab;
        public bool ResolvedPawns;

        public bool warnNoFuel;
        public ShipWeaponStatus weaponStatus = ShipWeaponStatus.Offline;

        public List<Pawn> Passengers
        {
            get
            {
                List<Pawn> pawns = new List<Pawn>();
                foreach(ShipHandler h in handlers)
                {
                    pawns.Add(h.shipPawn);
                }
                return pawns;
            }
        }
        public bool CanMove => Props.moveable > ShipPermissions.DriverNeeded || MovementHandlerAvailable;

        public Pawn Pawn => parent as Pawn;
        public ShipProperties Props => (ShipProperties)props;

        public bool MovementHandlerAvailable
        {
            get
            {
                bool result = false;
                if (handlers != null && handlers.Count > 0)
                {
                    foreach (ShipHandler h in handlers)
                    {
                        if (h.handlers != null && h.handlers.Count > 0)
                        {
                            if (h.role != null)
                            {
                                if ((h.role.handlingTypes & HandlingTypeFlags.Movement) != HandlingTypeFlags.None)
                                {
                                    result = h.handlers.Any((Pawn x) => !x.Dead && !x.Downed);
                                }
                            }
                        }
                    }
                }
                return result;
            }
        }

        public List<Pawn> AllPawnsAboard
        {
            get
            {
                List<Pawn> pawnsOnShip = new List<Pawn>();
                if (!(handlers is null) && handlers.Count > 0)
                {
                    foreach (ShipHandler handler in handlers)
                    {
                        if (!(handler.handlers is null) && handler.handlers.Count > 0) pawnsOnShip.AddRange(handler.handlers);
                    }
                }
                return pawnsOnShip;
            }
        }

        public int SeatsAvailable
        {
            get
            {
                int x = 0;
                foreach(ShipHandler handler in handlers)
                {
                    x += handler.role.slots - handler.handlers.Count;
                }
                return x;
            }
        }

        public void Rename()
        {
            if(this.Props.nameable)
            {
                Find.WindowStack.Add(new Dialog_GiveShipName(this.Pawn));
            }
        }
        public float Rotation
        {
            get
            {
                return this.rotation;
            }
            set
            {
                if (value == this.rotation)
                {
                    return;
                }
                this.rotation = value;
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!this.Pawn.Dead && !this.Pawn.Drafted)
            {
                Command_Action unloadAll = new Command_Action();
                unloadAll.defaultLabel = "Unload Everyone";
                unloadAll.icon = TexCommandShips.UnloadAll;
                unloadAll.action = delegate ()
                {
                    DisembarkAll();
                };
                unloadAll.hotKey = KeyBindingDefOf.Misc2;
                yield return unloadAll;

                foreach(ShipHandler handler in handlers)
                {
                    for(int i = 0; i < handler.handlers.Count; i++)
                    {
                        Pawn currentPawn = handler.handlers.InnerListForReading[i];
                        Command_Action unload = new Command_Action();
                        unload.defaultLabel = "Unload " + currentPawn.LabelShort;
                        unload.icon = TexCommandShips.UnloadPassenger;
                        unload.action = delegate ()
                        {
                            DisembarkPawn(currentPawn);
                        };
                        yield return unload;
                    }
                }
            }
            yield break;
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn pawn)
        {
            if (!pawn.RaceProps.ToolUser)
            {
                yield break;
            }
            if (!pawn.CanReserveAndReach(this.parent, PathEndMode.InteractionCell, Danger.Deadly, 1, -1, null, false))
            {
                yield break;
            }
            if (this.movementStatus is ShipMovementStatus.Offline)
            {
                yield break;
            }
            foreach (ShipHandler handler in handlers)
            {
                if (handler.AreSlotsAvailable)
                {
                    FloatMenuOption opt = new FloatMenuOption("BoardShip".Translate(this.parent.LabelShort, handler.role.label, (handler.role.slots - handler.handlers.Count).ToString()), delegate ()
                    {
                        Job job = new Job(RimShips_JobDefOf.Board, this.parent);
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        GiveLoadJob(pawn, handler);
                    }, MenuOptionPriority.Default, null, null, 0f, null, null);
                    yield return opt;
                }
            }
            yield break;
        }

        public void GiveLoadJob(Pawn pawn, ShipHandler handler)
        {
            if (this.bills is null) Log.Error("List of bills is NULL, please notify Mod Author of RimShips.");

            if (!(bills is null) && bills.Count > 0)
            {
                Jobs.Bill_BoardShip bill = bills.FirstOrDefault(x => x.pawnToBoard == pawn);
                if (!(bill is null))
                {
                    bill.handler = handler;
                    return;
                }
            }
            bills.Add(new Jobs.Bill_BoardShip(pawn, Pawn, handler));
        }

        public ShipRole uniqueShip(ShipRole _role)
        {
            return _role;
        }
        public void Notify_Boarded(Pawn pawnToBoard)
        {
            if( !(bills is null) & (bills.Count > 0))
            {
                Jobs.Bill_BoardShip bill = bills.FirstOrDefault(x => x.pawnToBoard == pawnToBoard);
                if(!(bill is null))
                {
                    if (pawnToBoard.IsWorldPawn())
                    {
                        Log.Warning("Tried boarding ship with world pawn");
                    }

                    Faction faction = pawnToBoard.Faction;
                    pawnToBoard.DeSpawn();
                    
                    if ( !(pawnToBoard.holdingOwner is null))
                    {
                        pawnToBoard.holdingOwner.TryTransferToContainer(pawnToBoard, bill.handler.handlers);
                    }
                    else
                    {
                        bill.handler.handlers.TryAdd(pawnToBoard);
                    }
                    if (!pawnToBoard.IsWorldPawn())
                    {
                        Find.WorldPawns.PassToWorld(pawnToBoard, PawnDiscardDecideMode.Decide);
                    }
                    pawnToBoard.SetFaction(faction);
                    bills.Remove(bill);
                }
            }
        }

        public void DisembarkPawn(Pawn pawn)
        {
            if(!pawn.Spawned)
            {
                GenSpawn.Spawn(pawn, Pawn.PositionHeld.RandomAdjacentCellCardinal(), Pawn.MapHeld);
                if(!(this.Pawn.GetLord() is null))
                {
                    this.Pawn.GetLord().AddPawn(pawn);
                }
            }
            RemovePawn(pawn);
        }

        public void DisembarkAll()
        {
            var pawnsToDisembark = new List<Pawn>(AllPawnsAboard);
            if( !(pawnsToDisembark is null) && pawnsToDisembark.Count > 0)
            {
                foreach(Pawn p in pawnsToDisembark)
                {
                    DisembarkPawn(p);
                }
            }
        }
        public void RemovePawn(Pawn pawn)
        {
            if(handlers is List<ShipHandler> handl && !handl.NullOrEmpty())
            {
                List<ShipHandler> tempHandler = handl.FindAll(x => x.handlers.InnerListForReading.Contains(pawn));
                if(!tempHandler.NullOrEmpty())
                {
                    foreach(ShipHandler h in tempHandler)
                    {
                        if (h.handlers.InnerListForReading.Remove(pawn)) return;
                    }
                }
            }
        }
        public void InitializeShipHandlers()
        {
            if (!(handlers is null) && handlers.Count > 0) return;

            if( !(Props.roles is null) && Props.roles.Count > 0)
            {
                foreach(ShipRole role in Props.roles)
                {
                    handlers.Add(new ShipHandler(Pawn, role, new List<Pawn>()));
                }
            }
        }

        public void BeachShip()
        {
            this.movementStatus = ShipMovementStatus.Offline;
            this.beached = true;
        }

        public override void CompTick()
        {
            base.CompTick();
            InitializeShipHandlers();
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            //init your variables here
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref weaponStatus, "weaponStatus", ShipWeaponStatus.Online);
            Scribe_Values.Look(ref movementStatus, "movingStatus", ShipMovementStatus.Online);
            Scribe_Values.Look(ref lastDirection, "lastDirection", Rot4.South);

            
            Scribe_Collections.Look(ref handlers, "handlers", LookMode.Deep);
            Scribe_Collections.Look(ref bills, "bills", LookMode.Deep);
        }
    }
}