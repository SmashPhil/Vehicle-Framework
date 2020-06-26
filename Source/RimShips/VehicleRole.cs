using RimWorld;
using Verse;
using System.Collections.Generic;

namespace Vehicles
{
    public enum HandlingTypeFlags {Cannon, Turret, Movement}

    public class VehicleRole : IExposable
    {
        public string label = "Missing Label";
        public List<HandlingTypeFlags> handlingTypes;
        public int slots;
        public int slotsToOperate;
        public List<string> cannonIds;

        public bool RequiredForCaravan => slotsToOperate > 0 && handlingTypes.Any(h => h == HandlingTypeFlags.Movement);

        public VehicleRole()
        {

        }
        public VehicleRole(VehicleHandler group)
        {
            label = group.role.label;
            handlingTypes = new List<HandlingTypeFlags>();
            if(group.role.handlingTypes != null)
                handlingTypes.AddRange(group.role.handlingTypes);
            slots = group.role.slots;
            slotsToOperate = group.role.slotsToOperate;
            cannonIds = new List<string>();
            cannonIds.AddRange(group.role.cannonIds);
        }

        public VehicleRole(VehicleRole reference)
        {
            label = reference.label;
            handlingTypes = new List<HandlingTypeFlags>();
            if(reference.handlingTypes != null)
                handlingTypes.AddRange(reference.handlingTypes);
            slots = reference.slots;
            slotsToOperate = reference.slotsToOperate;
            cannonIds = reference.cannonIds;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref label, "label", "");
            Scribe_Collections.Look(ref handlingTypes, "handlingTypes");
            Scribe_Values.Look(ref slots, "slots", 1);
            Scribe_Values.Look(ref slotsToOperate, "slotsToOperate", 1);
            Scribe_Collections.Look(ref cannonIds, "cannonIds");
        }
    }
}
