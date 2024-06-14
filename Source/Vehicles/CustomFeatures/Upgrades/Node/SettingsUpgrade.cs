using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;
using System.Runtime;

namespace Vehicles
{
	public class SettingsUpgrade : Upgrade
	{
		public UpgradeState state;

		public override bool UnlockOnLoad => true;

		public override void Unlock(VehiclePawn vehicle, bool unlockingPostLoad)
		{
			vehicle.CompUpgradeTree.AddSettings(state);

			if (!state.settings.NullOrEmpty())
			{
				foreach (UpgradeState.Setting setting in state.settings)
				{
					setting.Unlocked(vehicle, unlockingPostLoad);
				}
			}
		}

		public override void Refund(VehiclePawn vehicle)
		{
			vehicle.CompUpgradeTree.RemoveSettings(state);

			if (!state.settings.NullOrEmpty())
			{
				foreach (UpgradeState.Setting setting in state.settings)
				{
					setting.Refunded(vehicle);
				}
			}
		}
	}
}
