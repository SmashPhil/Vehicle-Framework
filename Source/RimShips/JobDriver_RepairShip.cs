using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimShips.Lords;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips.Jobs
{
    public class JobDriver_RepairShip : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return false;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            return null;
        }
    }
}
