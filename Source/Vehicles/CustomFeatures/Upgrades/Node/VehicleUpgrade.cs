using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using RimWorld;

namespace Vehicles
{
	public class VehicleUpgrade : Upgrade
	{
		public List<ArmorUpgrade> armor;

		public List<HealthUpgrade> health;

		public List<VehicleRole> roles;

		public override bool UnlockOnLoad => true;

		public override void Unlock(VehiclePawn vehicle, bool unlockingAfterLoad)
		{
			if (!unlockingAfterLoad && !roles.NullOrEmpty()) //Roles are serialized as VehicleHandler, no need to re-upgrade
			{
				foreach (VehicleRole role in roles)
				{
					vehicle.AddRole(role);
				}
			}
			if (!armor.NullOrEmpty())
			{
				foreach (ArmorUpgrade armorUpgrade in armor)
				{
					if (!armorUpgrade.key.NullOrEmpty() && !armorUpgrade.statModifiers.NullOrEmpty())
					{
						VehicleComponent component = vehicle.statHandler.GetComponent(armorUpgrade.key);
						switch (armorUpgrade.type)
						{
							case UpgradeType.Add:
								component.AddArmorModifiers[node.key] = armorUpgrade.statModifiers;
								break;
							case UpgradeType.Set:
								component.SetArmorModifiers[node.key] = armorUpgrade.statModifiers;
								break;
						}
					}
				}
			}
			if (!health.NullOrEmpty())
			{
				foreach (HealthUpgrade healthUpgrade in health)
				{
					if (!healthUpgrade.key.NullOrEmpty())
					{
						VehicleComponent component = vehicle.statHandler.GetComponent(healthUpgrade.key);

						if (healthUpgrade.value.HasValue)
						{
							switch (healthUpgrade.type)
							{
								case UpgradeType.Add:
									component.AddHealthModifiers[node.key] = healthUpgrade.value.Value;
									break;
								case UpgradeType.Set:
									component.SetHealthModifier = healthUpgrade.value.Value;
									break;
							}
						}


						if (healthUpgrade.depth != null)
						{
							component.depthOverride = healthUpgrade.depth;
						}
					}
				}
			}
		}

		public override void Refund(VehiclePawn vehicle)
		{
			if (!roles.NullOrEmpty())
			{
				foreach (VehicleRole role in roles)
				{
					vehicle.RemoveRole(role);
				}
			}
			if (!armor.NullOrEmpty())
			{
				foreach (ArmorUpgrade armorUpgrade in armor)
				{
					if (!armorUpgrade.key.NullOrEmpty() && !armorUpgrade.statModifiers.NullOrEmpty())
					{
						VehicleComponent component = vehicle.statHandler.GetComponent(armorUpgrade.key);
						switch (armorUpgrade.type)
						{
							case UpgradeType.Add:
								component.AddArmorModifiers.Remove(node.key);
								break;
							case UpgradeType.Set:
								component.SetArmorModifiers.Remove(node.key);
								break;
						}
					}
				}
			}
			if (!health.NullOrEmpty())
			{
				foreach (HealthUpgrade healthUpgrade in health)
				{
					if (!healthUpgrade.key.NullOrEmpty())
					{
						VehicleComponent component = vehicle.statHandler.GetComponent(healthUpgrade.key);

						if (healthUpgrade.value.HasValue)
						{
							switch (healthUpgrade.type)
							{
								case UpgradeType.Add:
									component.AddHealthModifiers.Remove(node.key);
									break;
								case UpgradeType.Set:
									component.SetHealthModifier = -1;
									break;
							}
						}


						if (healthUpgrade.depth != null)
						{
							component.depthOverride = null;
						}
					}
				}
			}
		}

		public struct ArmorUpgrade
		{
			public string key;
			public List<StatModifier> statModifiers;

			public UpgradeType type;
		}

		public struct HealthUpgrade
		{
			public string key;
			public float? value;

			public VehicleComponent.VehiclePartDepth? depth;

			public UpgradeType type;
		}
	}
}
