using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace Vehicles
{
    public class CustomVehicleDamageMultiplier : DefModExtension
    {
        public List<PawnKindDef> vehicleSpecifics;

        public float damageMultiplier;

        public bool ignoreArmor = false;
    }
}
