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
    public class PawnTable_Ships : PawnTable
    {
        public PawnTable_Ships(PawnTableDef def, Func<IEnumerable<Pawn>> pawnsGetter, int uiWidth, int uiHeight) : base(def, pawnsGetter, uiWidth,
            uiHeight)
        { }

        protected override IEnumerable<Pawn> LabelSortFunction(IEnumerable<Pawn> input)
        {
            return from p in input
                   orderby p.Name is null || p.Name.Numerical, (!(p.Name is NameSingle)) ? 0 : ((NameSingle)p.Name).Number, p.def.label
                   select p;
        }

        protected override IEnumerable<Pawn> PrimarySortFunction(IEnumerable<Pawn> input)
        {
            return from p in input
                   orderby p.RaceProps.baseBodySize
                   select p;
        }
    }

}