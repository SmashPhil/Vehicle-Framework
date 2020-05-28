using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Vehicles.Defs;
using Verse;

namespace Vehicles.UI
{
    public class MainTabWindow_Ships : MainTabWindow_PawnTable
    {
        protected override PawnTableDef PawnTableDef
        {
            get
            {
                return PawnTableDefOf_Ships.Vehicles;
            }
        }

        protected override IEnumerable<Pawn> Pawns
        {
            get
            {
                return from p in Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer)
                       where p.RaceProps.ToolUser && !(p.TryGetComp<CompVehicle>() is null)
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