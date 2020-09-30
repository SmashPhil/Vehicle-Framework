using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
    public struct VehicleGenerationRequest
    {
        public VehicleGenerationRequest(PawnKindDef kindDef, Faction faction, bool randomizeColors = false, bool randomizeMask = false)
        {
            KindDef = kindDef;
            Faction = faction;

            if (randomizeColors)
            {
                Rand.PushState();
                float r1 = Rand.Range(0.25f, .75f);
                float g1 = Rand.Range(0.25f, .75f);
                float b1 = Rand.Range(0.25f, .75f);
                ColorOne = new Color(r1, g1, b1, 1f);
                float r2 = Rand.Range(0.25f, .75f);
                float g2 = Rand.Range(0.25f, .75f);
                float b2 = Rand.Range(0.25f, .75f);
                ColorTwo = new Color(r2, g2, b2, 1f);
                Rand.PopState();
            }
            else
            {
                var lifeStage = kindDef.lifeStages.MaxBy(l => l.bodyGraphicData.drawSize).bodyGraphicData;
                ColorOne = lifeStage.color;
                ColorTwo = lifeStage.colorTwo;
            }

            RandomizeMask = randomizeMask;
        }

        public PawnKindDef KindDef { get; set; }
        public Faction Faction { get; set; }
        public Color ColorOne { get; set; }
        public Color ColorTwo { get; set; }
        public bool RandomizeMask { get; set; }
        
        //REDO - Need to add more randomization for story telling
    }
}
