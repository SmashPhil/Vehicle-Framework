using RimWorld;
using Verse;
using System.Collections.Generic;

namespace Vehicles
{
    public enum HandlingTypeFlags {None, Cannons, Turret, Movement }

    public class ShipRole : IExposable
    {
        public HandlingTypeFlags handlingType;
        public string label = "Driver";
        public List<PawnGenOption> preferredHandlers = new List<PawnGenOption>();
        public int slots;
        public int slotsToOperate;
        public ShipRole()
        {

        }
        public ShipRole(ShipHandler group)
        {
            label = group.role.label;
            handlingType = group.role.handlingType;
            slots = group.role.slots;
            slotsToOperate = group.role.slotsToOperate;
            preferredHandlers = group.role.preferredHandlers;
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref label, "label", "");
            Scribe_Values.Look(ref handlingType, "handlingType", HandlingTypeFlags.None);
            Scribe_Values.Look(ref slots, "slots", 1);
            Scribe_Values.Look(ref slotsToOperate, "slotsToOperate", 1);
            Scribe_Collections.Look(ref preferredHandlers, "preferredHandlers", LookMode.Deep);
        }
    }
}
