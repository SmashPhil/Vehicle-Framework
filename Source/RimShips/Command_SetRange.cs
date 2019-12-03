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
using SPExtendedLibrary;

namespace RimShips
{
    [StaticConstructorOnStartup]
    public class Command_SetRange : Command
    {
        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);

            int startLevel = (int)this.cannonComp.Range;
            int minRange = (int)this.cannonComp.MinRange;
            int maxRange = (int)this.cannonComp.MaxRange;

            Func<int, string> textGetter;
            textGetter = ((int x) => "CannonRange".Translate(x));
            Dialog_Slider window = new Dialog_Slider(textGetter, (int)minRange, (int)maxRange, delegate (int value)
            {
                cannonComp.Range = (float)value;
            }, startLevel);
            Find.WindowStack.Add(window);
        }

        public List<CannonHandler> activeCannons;
        public CompCannons cannonComp;
    }
}
