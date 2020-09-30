using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
    public class VehicleAI : IExposable
    {
        public VehicleAI(VehiclePawn vehicle) 
        {
            this.vehicle = vehicle;
        }

        public void AITick()
        {
            foreach (VehicleComp comp in vehicle.GetAllAIComps())
            {
                comp.AITick();
            }
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref vehicle, "vehicle");
        }

        private VehiclePawn vehicle;
    }
}
