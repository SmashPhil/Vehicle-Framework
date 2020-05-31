using RimWorld;
using Verse;
using System.Collections.Generic;

namespace Vehicles
{
    public enum HandlingTypeFlags {None, Cannon, Turret, Movement}

    public class ShipRole : IExposable
    {
        public HandlingTypeFlags handlingType;
        public string label = "Driver";
        public int slots;
        public int slotsToOperate;
        public List<string> cannonIDs;

        public ShipRole()
        {

        }
        public ShipRole(ShipHandler group)
        {
            label = group.role.label;
            handlingType = group.role.handlingType;
            slots = group.role.slots;
            slotsToOperate = group.role.slotsToOperate;
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref label, "label", "");
            Scribe_Values.Look(ref handlingType, "handlingType", HandlingTypeFlags.None);
            Scribe_Values.Look(ref slots, "slots", 1);
            Scribe_Values.Look(ref slotsToOperate, "slotsToOperate", 1);
            Scribe_Collections.Look(ref cannonIDs, "cannonIDs");
        }
    }
}
