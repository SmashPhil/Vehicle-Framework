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
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips.Jobs
{
    internal class Toils_Board
    {
        public static Toil BoardShip(Pawn pawnBoarding, TargetIndex index)
        {
            Toil toil = new Toil();

            toil.initAction = delegate ()
            {
                CompShips ship = toil.actor.jobs.curJob.GetTarget(index).Thing.TryGetComp<CompShips>();
                ship.Notify_Boarded(pawnBoarding);
            };
            return toil;
        }
    }
}
