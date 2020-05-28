using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Vehicles
{
    public class CompProperties_VehicleTracks : CompProperties
    {
        public CompProperties_VehicleTracks()
        {
            compClass = typeof(CompVehicleTracks);
        }

        public List<VehicleTrack> tracks = new List<VehicleTrack>();
    }

    public struct VehicleTrack
    {
        public Pair<IntVec2, IntVec2> trackPoint;

        public List<ThingCategory> destroyableCategories; //REDO
    }
}
