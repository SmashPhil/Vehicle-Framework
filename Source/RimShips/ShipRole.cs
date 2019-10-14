using RimWorld;
using Verse;
using Harmony;
using System.Collections.Generic;

namespace RimShips
{
    public enum HandlingTypeFlags {None,Movement,Manipulation,Weapons}

    public class ShipRole : IExposable
    {
        public HandlingTypeFlags handlingTypes;
        public string label = "Driver";
        public List<PawnGenOption> preferredHandlers = new List<PawnGenOption>();
        public int slots;
        public int slotsToOperate;
        public string slotTag;
        public ShipRole()
        {

        }
        public ShipRole(ShipHandler group)
        {
            label = group.role.label;
            handlingTypes = group.role.handlingTypes;
            slots = group.role.slots;
            slotsToOperate = group.role.slotsToOperate;
            slotTag = group.role.slotTag;
            preferredHandlers = group.role.preferredHandlers;
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref label, "label", "");
            Scribe_Values.Look(ref handlingTypes, "handlingTYpes", HandlingTypeFlags.None);
            Scribe_Values.Look(ref slots, "slots", 1);
            Scribe_Values.Look(ref slotsToOperate, "slotsToOperate", 1);
            Scribe_Collections.Look(ref preferredHandlers, "preferredHandlers", LookMode.Deep);
        }
    }
}
