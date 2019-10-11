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
using RimShips.Defs;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips.UI
{
    public class MainTabWindow_Ships : MainTabWindow_PawnTable
    {
        protected override PawnTableDef PawnTableDef
        {
            get
            {
                return PawnTableDefOf_Ships.RimShips;
            }
        }

        protected override IEnumerable<Pawn> Pawns
        {
            get
            {
                return from p in Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer)
                       where p.RaceProps.ToolUser && !(p.TryGetComp<CompShips>() is null)
                       select p;
            }
        }

        public override void PostOpen()
        {
            base.PostOpen();
            Find.World.renderer.wantedMode = WorldRenderMode.None;
        }
    }
}