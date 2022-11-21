using System.Collections.Generic;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleRole
	{
		public string key;
		public string label = "[MissingLabel]";

		//Operating
		public List<HandlingTypeFlags> handlingTypes = new List<HandlingTypeFlags>();
		public int slots;
		public int slotsToOperate;
		public List<string> turretIds;
		//Damaging
		public ComponentHitbox hitbox = new ComponentHitbox();
		public bool exposed = false;
		public float chanceToHit = 0.3f;
		//Rendering
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
	}
}
