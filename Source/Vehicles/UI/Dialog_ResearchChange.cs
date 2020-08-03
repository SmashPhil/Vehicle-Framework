using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
    public class Dialog_ResearchChange : Window
    {
        public Dialog_ResearchChange()
        {
            this.forcePause = true;
            this.doCloseX = true;
            this.doCloseButton = false;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = true;
            this.projectsSearched = new List<ResearchProjectDef>();
            this.addedResearch = false;
            charCount = 0;
        }

        public override Vector2 InitialSize => new Vector2(200f, 350f);

        public override void DoWindowContents(Rect inRect)
        {
            Rect labelRect = new Rect(inRect.x, inRect.y, inRect.width, 24f);
            Rect searchBarRect = new Rect(inRect.x, labelRect.y + 24f, inRect.width, 24f);
            Widgets.Label(labelRect, "SearchResearch".Translate());
            charCount = researchString?.Count() ?? 0;
            researchString = Widgets.TextArea(searchBarRect, researchString);

            /*if (researchString.Count() != charCount || researchChanged)
            {
                projectsSearched = DefDatabase<ResearchProjectDef>.AllDefs.Where(x => !VehicleMod.mod.props.ResearchPrerequisites.Contains(x) && CultureInfo.CurrentCulture.CompareInfo.IndexOf(x.defName, researchString, CompareOptions.IgnoreCase) >= 0).ToList();
                researchChanged = false;
            }

            for (int i = 0; i < projectsSearched.Count; i++)
            {
                ResearchProjectDef proj = projectsSearched[i];
                Rect projectRect = new Rect(inRect.x, searchBarRect.y + 24 + (24 * i), inRect.width, 30f);
                if (Widgets.ButtonText(projectRect, proj.defName, false, false, true) && !VehicleMod.mod.props.ResearchPrerequisites.Contains(proj))
                {
                    addedResearch = true;
                    researchChanged = true;
                    VehicleMod.mod.props.AddCustomResearch(proj);
                }
            }*/
        }

        public override void PreClose()
        {
            /*if (addedResearch)
                Messages.Message("RestartGameResearch".Translate(), MessageTypeDefOf.CautionInput, false);*/ //Uncomment to send message to restart game if research has been changed
        }

        bool addedResearch;

        bool researchChanged;

        private string researchString;

        int charCount;

        private List<ResearchProjectDef> projectsSearched;
    }
}
