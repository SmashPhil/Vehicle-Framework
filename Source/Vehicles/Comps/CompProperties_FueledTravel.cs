using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;


namespace Vehicles
{
    public class CompProperties_FueledTravel : CompProperties
    {
        public CompProperties_FueledTravel()
        {
            compClass = typeof(CompFueledTravel);
        }

        public ThingDef fuelType;

        public bool electricPowered;
        public float ticksPerCharge = 50f;

        public float fuelConsumptionRate;

        public int fuelCapacity;

        public FuelConsumptionCondition fuelConsumptionCondition;

        public List<OffsetMote> motesGenerated;

        public ThingDef MoteDisplayed;

        public int TicksToSpawnMote;
    }

    public struct OffsetMote
    {
        public float xOffset;
        public float zOffset;
        public int numTimesSpawned;

        public int NumTimesSpawned
        {
            get
            {
                return numTimesSpawned == 0 ? 1 : numTimesSpawned;
            }
        }

        public bool windAffected;

        public float moteThrownSpeed;

        public float? predeterminedAngleVector;
    }
}
