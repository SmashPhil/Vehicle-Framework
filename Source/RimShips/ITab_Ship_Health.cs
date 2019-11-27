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
    public class ITab_Ship_Health : ITab
    {
        public ITab_Ship_Health()
        {
            this.size = new Vector2(460f, 450f);
            this.labelKey = "TabBoatHealth";
        }

        public override bool IsVisible => !base.SelPawn.GetComp<CompShips>()?.beached ?? false;

        protected override void FillTab()
        {
            /*GUI.color = Color.white;
            Rect rect = new Rect(0f, 20f, this.size.x*0.375f, this.size.y - 20f).Rounded();
            Rect rect2 = new Rect(rect.xMax, 20f, this.size.x - rect.width, this.size.y - 20f);
            Widgets.DrawMenuSection(rect);
            List<TabRecord> list = new List<TabRecord>();
            list.Add(new TabRecord("HealthOverview".Translate(), delegate ()
            {
                capacityTab = true;
            }, !capacityTab));
            TabDrawer.DrawTabs(rect, list, 200f);
            rect = rect.ContractedBy(9f)*/

            HealthCardUtility.DrawPawnHealthCard(new Rect(0f, TopPadding, this.size.x, this.size.y - IconSize), base.SelPawn, false, false, base.SelThing);
        }

        private static bool capacityTab = true;

        private const float TopPadding = 20f;

        private const float IconSize = 20f;
    }
}
