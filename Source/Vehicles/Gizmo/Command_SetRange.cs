using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Vehicles
{
    [StaticConstructorOnStartup]
    public class Command_SetRange : Command
    {
        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);

            int startLevel = (int)cannonComp.Range;
            int minRange = (int)cannonComp.MinRange;
            int maxRange = (int)cannonComp.MaxRangeGrouped;

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
