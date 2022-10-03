using System.Collections.Generic;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleRole : IExposable
	{
		public string key;
		public string label = "[MissingLabel]";
		public List<HandlingTypeFlags> handlingTypes = new List<HandlingTypeFlags>();
		public int slots;
		public int slotsToOperate;
		public bool exposed = false;
		public List<string> turretIds;
		public ComponentHitbox hitbox = new ComponentHitbox();
		public float chanceToHit = 0.3f;
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
			Scribe_Values.Look(ref key, nameof(key), forceSave: true);
			Scribe_Values.Look(ref label, nameof(label), string.Empty);
			Scribe_Collections.Look(ref handlingTypes, nameof(handlingTypes));
			Scribe_Values.Look(ref slots, nameof(slots), 1);
			Scribe_Values.Look(ref slotsToOperate, nameof(slotsToOperate), 1);
			Scribe_Values.Look(ref exposed, nameof(exposed), false);
			Scribe_Collections.Look(ref turretIds, nameof(turretIds));
		}
	}
}
