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

		public List<VehicleRole> roles;

		public override int ListerCount => 1;

		public override bool UnlockOnLoad => true;

		public override void Unlock(VehiclePawn vehicle)
		{
			try
			{
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
			}
			catch (Exception ex)
			{
				Log.Error($"{VehicleHarmony.LogLabel} Unable to add stat values to {vehicle.LabelShort}. Report on workshop page. \nException: {ex}");
				return;
			}

			vehicle.VehicleDef.buildDef.soundBuilt?.PlayOneShot(new TargetInfo(vehicle.Position, vehicle.Map, false));
		}

		public override void Refund(VehiclePawn vehicle)
		{
			try
			{
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
			}
			catch (Exception ex)
			{
				Log.Error($"{VehicleHarmony.LogLabel} Unable to add stat values to {vehicle.LabelShort}. Report on workshop page. \nException: {ex}");
				return;
			}

			vehicle.VehicleDef.buildDef.soundBuilt?.PlayOneShot(new TargetInfo(vehicle.Position, vehicle.Map, false));
		}

		public struct ArmorUpgrade
		{
			public string key;
			public List<StatModifier> statModifiers;

			public UpgradeType type;
		}

		public enum UpgradeType
		{
			Add,
			Set,
		}
	}
}
