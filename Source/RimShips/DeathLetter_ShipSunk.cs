using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using RimShips.AI;
using RimShips.Lords;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;


namespace RimShips
{
    public class DeathLetter_ShipSunk : ChoiceLetter
    {

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                yield return base.Option_Close;
                yield return base.Option_JumpToLocation;
                yield break;
            }
        }

        public override void OpenLetter()
        {
            Pawn targetPawn = this.lookTargets.TryGetPrimaryTarget().Thing as Pawn;
            
            string text = this.text;

            string textPawnList = "";
            foreach(Pawn p in targetPawn?.GetComp<CompShips>()?.AllPawnsAboard)
            {
                textPawnList += p.LabelShort + ". ";
            }
            text = string.Format("{0}\n{1}", "LastEventsForShip".Translate(targetPawn.LabelShort), textPawnList);

            DiaNode diaNode = new DiaNode(text);
            diaNode.options.AddRange(this.Choices);
            WindowStack windowStack = Find.WindowStack;
            DiaNode nodeRoot = diaNode;
            Faction relatedFaction = this.relatedFaction;
            bool radioMode = this.radioMode;
            windowStack.Add(new Dialog_NodeTreeWithFactionInfo(nodeRoot, relatedFaction, false, radioMode, this.title));
        }
    }
}
