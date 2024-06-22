using System.Collections.Generic;
using Verse;
using SmashTools;
using static Vehicles.VehicleUpgrade;

namespace Vehicles
{
	/// <summary>
	/// Container of data related to a 'seat' in a vehicle
	/// </summary>
	public class VehicleRole : ITweakFields
	{
		public string key;
		public string label = "[MissingLabel]";

		//Operating
		private HandlingTypeFlags handlingTypes = HandlingTypeFlags.None;
		private int slots;
		private int slotsToOperate;
		private float comfort = 0.5f;
		private List<string> turretIds;

		//Damaging
		private ComponentHitbox hitbox = new ComponentHitbox();
		private bool exposed = false;
		private float chanceToHit = 0.3f;

		//Rendering
		[TweakField]
		private PawnOverlayRenderer pawnRenderer;

		private readonly List<RoleUpgrade> upgrades = new List<RoleUpgrade>();

		public VehicleRole()
		{
		}

		public VehicleRole(VehicleHandler group) : this(group.role)
		{
		}

		public VehicleRole(VehicleRole reference)
		{
			if (string.IsNullOrEmpty(reference.key))
			{
				Log.Error($"Missing Key on VehicleRole {reference.label}");
			}
			CopyFrom(reference);
		}

		//Operating
		public HandlingTypeFlags HandlingTypes
		{
			get
			{
				if (!upgrades.NullOrEmpty())
				{
					for (int i = upgrades.Count - 1; i >= 0; i--)
					{
						RoleUpgrade roleUpgrade = upgrades[i];
						if (roleUpgrade.handlingTypes != null)
						{
							return roleUpgrade.handlingTypes.Value;
						}
					}
				}
				return handlingTypes;
			}
		}

		public int Slots
		{
			get
			{
				if (!upgrades.NullOrEmpty())
				{
					for (int i = upgrades.Count - 1; i >= 0; i--)
					{
						RoleUpgrade roleUpgrade = upgrades[i];
						if (roleUpgrade.slots != null)
						{
							return roleUpgrade.slots.Value;
						}
					}
				}
				return slots;
			}
		}

		public int SlotsToOperate
		{
			get
			{
				if (!upgrades.NullOrEmpty())
				{
					foreach (RoleUpgrade roleUpgrade in upgrades)
					{
						if (roleUpgrade.slotsToOperate != null)
						{
							return roleUpgrade.slotsToOperate.Value;
						}
					}
				}
				return slotsToOperate;
			}
		}

		public float Comfort
		{
			get
			{
				if (!upgrades.NullOrEmpty())
				{
					foreach (RoleUpgrade roleUpgrade in upgrades)
					{
						if (roleUpgrade.comfort != null)
						{
							return roleUpgrade.comfort.Value;
						}
					}
				}
				return comfort;
			}
		}

		public List<string> TurretIds
		{
			get
			{
				if (!upgrades.NullOrEmpty())
				{
					for (int i = upgrades.Count - 1; i >= 0; i--)
					{
						RoleUpgrade roleUpgrade = upgrades[i];
						if (roleUpgrade.turretIds != null) //empty turret ids can be used to override role management of turret
						{
							return roleUpgrade.turretIds;
						}
					}
				}
				return turretIds;
			}
		}

		//Damaging
		public ComponentHitbox Hitbox
		{
			get
			{
				if (!upgrades.NullOrEmpty())
				{
					for (int i = upgrades.Count - 1; i >= 0; i--)
					{
						RoleUpgrade roleUpgrade = upgrades[i];
						if (roleUpgrade.hitbox != null) //empty turret ids can be used to override role management of turret
						{
							return roleUpgrade.hitbox;
						}
					}
				}
				return hitbox;
			}
		}

		public bool Exposed
		{
			get
			{
				if (!upgrades.NullOrEmpty())
				{
					for (int i = upgrades.Count - 1; i >= 0; i--)
					{
						RoleUpgrade roleUpgrade = upgrades[i];
						if (roleUpgrade.exposed != null)
						{
							return roleUpgrade.exposed.Value;
						}
					}
				}
				return exposed;
			}
		}

		public float ChanceToHit
		{
			get
			{
				if (!upgrades.NullOrEmpty())
				{
					for (int i = upgrades.Count - 1; i >= 0; i--)
					{
						RoleUpgrade roleUpgrade = upgrades[i];
						if (roleUpgrade.chanceToHit != null)
						{
							return roleUpgrade.chanceToHit.Value;
						}
					}
				}
				return chanceToHit;
			}
		}

		//Rendering
		public PawnOverlayRenderer PawnRenderer
		{
			get
			{
				if (!upgrades.NullOrEmpty())
				{
					for (int i = upgrades.Count - 1; i >= 0; i--)
					{
						RoleUpgrade roleUpgrade = upgrades[i];
						if (roleUpgrade.pawnRenderer != null)
						{
							return roleUpgrade.pawnRenderer;
						}
					}
				}
				return pawnRenderer;
			}
		}

		public bool RequiredForCaravan => slotsToOperate > 0 && handlingTypes.HasFlag(HandlingTypeFlags.Movement);

		string ITweakFields.Category => nameof(PawnOverlayRenderer);

		string ITweakFields.Label => label;

		public bool Resolved { get; private set; } = false;

		public void AddUpgrade(RoleUpgrade roleUpgrade)
		{
			upgrades.Add(roleUpgrade);
		}

		public void RemoveUpgrade(RoleUpgrade roleUpgrade)
		{
			upgrades.Remove(roleUpgrade);
		}

		public void CopyFrom(VehicleRole reference)
		{
			key = reference.key;
			label = reference.label;

			handlingTypes = reference.handlingTypes;
			slots = reference.slots;
			slotsToOperate = reference.slotsToOperate;

			turretIds = null;
			if (!reference.turretIds.NullOrEmpty())
			{
				turretIds = new List<string>(reference.turretIds);
			}

			hitbox = reference.hitbox;
			exposed = reference.exposed;
			chanceToHit = reference.chanceToHit;

			pawnRenderer = reference.pawnRenderer;
		}

		public void CopyFrom(RoleUpgrade upgrade)
		{
			if (upgrade.handlingTypes != null)
			{
				handlingTypes = upgrade.handlingTypes.Value;
			}
			if (upgrade.slots != null)
			{
				slots = upgrade.slots.Value;
			}
			if (upgrade.slotsToOperate != null)
			{
				slotsToOperate = upgrade.slotsToOperate.Value;
			}
			if (upgrade.turretIds != null)
			{
				turretIds = new List<string>(upgrade.turretIds);
			}
			if (upgrade.hitbox != null)
			{
				hitbox = upgrade.hitbox;
			}
			if (upgrade.exposed != null)
			{
				exposed = upgrade.exposed.Value;
			}
			if (upgrade.chanceToHit != null)
			{
				chanceToHit = upgrade.chanceToHit.Value;
			}

			if (pawnRenderer != null)
			{
				pawnRenderer = upgrade.pawnRenderer;
			}
		}

		public void ResolveReferences(VehicleDef vehicleDef)
		{
			if (Resolved)
			{
				return;
			}
			
			hitbox.Initialize(vehicleDef);

			Resolved = true;
		}

		void ITweakFields.OnFieldChanged()
		{
		}
	}
}
