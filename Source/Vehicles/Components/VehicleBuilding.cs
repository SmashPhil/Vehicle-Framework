using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Vehicles.Components
{
    public class VehicleBuilding : Building
    {
        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (vehicleReference != null)
            {
                vehicleReference.DrawAt(drawLoc, flip);
                if (vehicleReference.GetCachedComp<CompCannons>() != null)
                {
                    vehicleReference.GetCachedComp<CompCannons>().PostDraw();
                }
            }
            else
            {
                Log.ErrorOnce($"VehicleReference for building {LabelShort} is null. This should not happen unless spawning VehicleBuildings in DevMode.", GetHashCode());
                base.DrawAt(drawLoc, flip);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref vehicleReference, "vehicleReference");
        }

        public VehiclePawn vehicleReference;
    }
}
