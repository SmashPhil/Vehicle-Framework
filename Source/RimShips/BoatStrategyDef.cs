using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace RimShips
{
    public class BoatStrategyDef : Def
    {
        public BoatStrategyWorker Worker
        {
            get
            {
                if(this.workerInt is null)
                {
                    this.workerInt = (BoatStrategyWorker)Activator.CreateInstance(this.workerClass);
                    this.workerInt.def = this;
                }
                return this.workerInt;
            }
        }

        public Type workerClass;

        public SimpleCurve selectionWeightPerPointsCurve;

        public float minPawns = 5f;

        [MustTranslate]
        public string arrivalTextEnemy;

        [MustTranslate]
        public string letterLabelEnemy;

        public SimpleCurve pointsFactorCurve;

        public bool pawnsCanBringFood;

        public List<PawnsArrivalModeDef> arriveModes;

        private BoatStrategyWorker workerInt;
    }
}
