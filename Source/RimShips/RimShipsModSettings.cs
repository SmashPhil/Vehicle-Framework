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
using RimShips.Build;
using RimShips.Defs;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips
{
    public class RimshipsModSettings : ModSettings
    {
        public float beachMultiplier;
        public bool forceFactionCoastOption;
        public int forceFactionCoastRadius;

        public int coastRadius => forceFactionCoastOption ? forceFactionCoastRadius : 0;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref beachMultiplier, "beachMultiplier");
            Scribe_Values.Look(ref forceFactionCoastRadius, "forceFactionCoastRadius", 1);
            Scribe_Values.Look(ref forceFactionCoastOption, "forceFactionCoastOption", true);
            base.ExposeData();
        }
    }

    public class RimShipMod : Mod
    {
        public RimshipsModSettings settings;
        public static RimShipMod mod;

        public RimShipMod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<RimshipsModSettings>();
            mod = this;
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();

            listingStandard.Begin(inRect);
            listingStandard.Label("BeachGenMultiplier".Translate(Mathf.Round(settings.beachMultiplier)));
            settings.beachMultiplier = listingStandard.Slider(settings.beachMultiplier, 0f, 200f);
            listingStandard.GapLine(16f);

            listingStandard.CheckboxLabeled("ForceSettlementCoastOption".Translate(), ref settings.forceFactionCoastOption, "ForceSettlementCoastTooltip".Translate());
            if(settings.forceFactionCoastOption)
            {
                listingStandard.Label("ForceSettlementCoast".Translate(Mathf.Round(settings.forceFactionCoastRadius)));
                settings.forceFactionCoastRadius = (int)listingStandard.Slider((float)settings.forceFactionCoastRadius, 0f, 10f);  
            }
            listingStandard.GapLine(16f);


            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "RimShips".Translate();
        }
    }
}
