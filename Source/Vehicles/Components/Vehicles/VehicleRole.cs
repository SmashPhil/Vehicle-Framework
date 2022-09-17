using System.Collections.Generic;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleRole : IExposable
	{
		public string key;
		public string label = "Missing Label";
		public List<HandlingTypeFlags> handlingTypes = new List<HandlingTypeFlags>();
		public int slots;
		public int slotsToOperate;
		public List<string> turretIds;
		public ComponentHitbox hitbox = new ComponentHitbox();
		public PawnOverlayRenderer pawnRenderer;

		public VehicleRole()
		{

		}

		public VehicleRole(VehicleHandler group)
		{
			if (string.IsNullOrEmpty(group.role.key))
			{
				Log.Error($"Missing Key on VehicleRole {group.role.label}");
			}
			key = group.role.key;
			label = group.role.label;
			handlingTypes = new List<HandlingTypeFlags>();
			if (group.role.handlingTypes != null)
			{
				handlingTypes.AddRange(group.role.handlingTypes);
			}
			slots = group.role.slots;
			slotsToOperate = group.role.slotsToOperate;
			turretIds = new List<string>();
			turretIds.AddRange(group.role.turretIds);
			pawnRenderer = group.role.pawnRenderer;
		}

		public VehicleRole(VehicleRole reference)
		{
			if (string.IsNullOrEmpty(reference.key))
			{
				Log.Error($"Missing Key on VehicleRole {reference.label}");
			}
			key = reference.key;
			label = reference.label;
			handlingTypes = new List<HandlingTypeFlags>();
			if (reference.handlingTypes != null)
			{
				handlingTypes.AddRange(reference.handlingTypes);
			}
			slots = reference.slots;
			slotsToOperate = reference.slotsToOperate;
			turretIds = reference.turretIds;
			hitbox = reference.hitbox;
			pawnRenderer = reference.pawnRenderer;
		}

		public bool RequiredForCaravan => slotsToOperate > 0 && handlingTypes.NotNullAndAny(h => h == HandlingTypeFlags.Movement);

		public void ExposeData()
		{
			Scribe_Values.Look(ref key, "key");
			Scribe_Values.Look(ref label, "label", "");
			Scribe_Collections.Look(ref handlingTypes, "handlingTypes");
			Scribe_Values.Look(ref slots, "slots", 1);
			Scribe_Values.Look(ref slotsToOperate, "slotsToOperate", 1);
			Scribe_Collections.Look(ref turretIds, "turretIds");
		}
	}
}
