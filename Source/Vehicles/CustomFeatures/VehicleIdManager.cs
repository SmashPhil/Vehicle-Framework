using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace Vehicles
{
    public class VehicleIdManager : GameComponent
    {
        public VehicleIdManager(Game game)
        {
        }

        public int GetNextUpgradeId()
        {
            return GetNextId(ref nextUpgradeId);
        }

        public int GetNextCannonId()
        {
            return GetNextId(ref nextCannonId);
        }

        public int GetNextHandlerId()
        {
            return GetNextId(ref nextVehicleHandlerId);
        }

        private int GetNextId(ref int id)
        {
            id++;
            return id;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextUpgradeId, "nextUpgradeId", 0);
            Scribe_Values.Look(ref nextCannonId, "nextCannonId", 0);
            Scribe_Values.Look(ref nextVehicleHandlerId, "nextVehicleHandlerId", 0);
        }

        public int nextUpgradeId;

        public int nextCannonId;

        public int nextVehicleHandlerId;
    }
}
