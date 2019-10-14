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
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips.Jobs
{
    public class Bill_BoardShip : IExposable
    {
        public ShipHandler handler;
        public Pawn pawnToBoard;

        public Bill_BoardShip()
        {

        }

        public Bill_BoardShip(Pawn newBoard, ShipHandler newHandler)
        {
            pawnToBoard = newBoard;
            handler = newHandler;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref pawnToBoard, "pawnToBoard");
            Scribe_References.Look(ref handler, "handler");
        }
    }
}