using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using SPExtended;

namespace RimShips
{
    public enum FuelConsumptionCondition {Moving, Drafted, Always };
    public class CompProperties_FueledTravel : CompProperties
    {
        public CompProperties_FueledTravel()
        {
            this.compClass = typeof(CompFueledTravel);
        }

        public ThingDef fuelType;

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
