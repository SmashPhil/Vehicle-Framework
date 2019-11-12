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

namespace RimShips
{
    public class ITab_Ship_Health : ITab
    {
        public ITab_Ship_Health()
        {
            this.labelKey = "TabHealth";
        }

        private List<Pawn> Passengers
        {
            get
            {
                return base.SelPawn.TryGetComp<CompShips>() is null ? null : base.SelPawn.GetComp<CompShips>().AllPawnsAboard;
            }
        }

        protected override void FillTab()
        {

        }
    }
}
