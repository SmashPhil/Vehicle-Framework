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
    [StaticConstructorOnStartup]
    public class Command_SetRange : Command
    {
        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            if (this.cannons is null) this.cannons = new List<ShipCannons>();
            if (!this.cannons.ContainsAllOfList(activeCannons)) this.cannons.AddRange(this.activeCannons);

            int startLevel = (int)this.activeCannons.Min(x => x.Range);
            int minRange = (int)this.activeCannons.Max(x => x.minRange);
            int maxRange = (int)this.activeCannons.Min(x => x.maxRange);

            Func<int, string> textGetter;
            textGetter = ((int x) => "CannonRange".Translate(x));
            Dialog_Slider window = new Dialog_Slider(textGetter, (int)minRange, (int)maxRange, delegate (int value)
            {
                foreach (ShipCannons shipcannon in this.cannons)
                {
                    shipcannon.Range = (float)value;
                }
            }, startLevel);
            Find.WindowStack.Add(window);
        }

        public override bool InheritInteractionsFrom(Gizmo other)
        {
            if (this.cannons is null)
            {
                this.cannons = new List<ShipCannons>();
            }
            this.cannons.AddRange(((Command_SetRange)other).activeCannons);
            return false;
        }

        public List<ShipCannons> activeCannons;

        private List<ShipCannons> cannons;
    }
}
