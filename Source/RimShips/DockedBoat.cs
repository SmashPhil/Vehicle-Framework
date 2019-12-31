using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace RimShips
{
    [HasDebugOutput]
    public class DockedBoat : WorldObject
    {
        public override Material Material
        {
            get
            {
                if(this.cachedMaterial is null)
                {
                    Color color;
                    if(base.Faction != null)
                        color = base.Faction.Color;
                    else
                        color = Color.white;
                    this.cachedMaterial = MaterialPool.MatFrom(this.def.texture, ShaderDatabase.WorldOverlayTransparentLit, color, WorldMaterials.WorldObjectRenderQueue);
                }
                return this.cachedMaterial;
            }
        }

        private int TotalAvailableSeats
        {
            get
            {
                int num = 0;
                foreach(Pawn p in dockedBoats)
                {
                    num += p.GetComp<CompShips>()?.SeatsAvailable ?? 0;
                }
                return num;
            }
        }

        public void Notify_CaravanArrived(Caravan caravan)
        {
            if(caravan.PawnsListForReading.Where(x => !ShipHarmony.IsShip(x)).Count() > this.TotalAvailableSeats)
            {
                Messages.Message("CaravanMustHaveEnoughSpaceOnShip".Translate(), this, MessageTypeDefOf.RejectInput, false);
                return;
            }
            for(int i = dockedBoats.Count-1; i >= 0; i--)
            {
                Pawn p = dockedBoats[i];
                caravan.pawns.TryAddOrTransfer(p);
            }
            Find.WorldObjects.Remove(this);
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            foreach(FloatMenuOption o in base.GetFloatMenuOptions(caravan))
            {
                yield return o;
            }
            foreach(FloatMenuOption f in CaravanArrivalAction_DockedBoats.GetFloatMenuOptions(caravan, this))
            {
                yield return f;
            }
        }


        public ThingOwner<Pawn> dockedBoats = new ThingOwner<Pawn>();

        private Material cachedMaterial;
    }
}
