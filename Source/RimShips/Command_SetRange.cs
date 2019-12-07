using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

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
