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

namespace RimShips.UI
{
    public class ITab_Ship_Passengers : ITab
    {
        public ITab_Ship_Passengers()
        {
            this.size = new Vector2(630f, 430f);
            this.labelKey = "TabShipPassengers";
            this.tutorTag = "Passengers";
        }

        private Pawn ShipForView
        {
            get
            {
                return !(base.SelPawn is null) ? base.SelPawn : null;
            }
        }

        private List<Pawn> Passengers
        {
            get
            {
                return base.SelPawn.TryGetComp<CompShips>() is null ? null : base.SelPawn.GetComp<CompShips>().AllPawnsAboard;
            }
        }

        private List<PawnCapacityDef> CapacitiesToDisplay
        {
            get
            {
                capacitiesToDisplay.Clear();
                List<PawnCapacityDef> allDefsListForReading = DefDatabase<PawnCapacityDef>.AllDefsListForReading;
                foreach(PawnCapacityDef pcd in allDefsListForReading)
                {
                    if(pcd.showOnCaravanHealthTab)
                    {
                        capacitiesToDisplay.Add(pcd);
                    }
                }
                capacitiesToDisplay.SortBy(x => x.listOrder);
                return capacitiesToDisplay;
            }
        }
        private float SpecificHealthTabWidth
        {
            get
            {
                /*this.EnsureSpecificHealthTabForPawnValid();
                if (this.specificShipTab.DestroyedOrNull())
                {
                    return 0f;
                }*/
                return 630f;
            }
        }
        protected override void FillTab()
        {
            Pawn thisShip = this.ShipForView;
            if(thisShip is null)
            {
                Log.Error("Ship tab found no selected pawn to display.", false);
                return;
            }

            Rect outRect = new Rect(0f, 20f, this.size.x, this.size.y - 20f);
            
        }





        private static List<PawnCapacityDef> capacitiesToDisplay = new List<PawnCapacityDef>();
    }
}