using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;
using SmashTools;

namespace Vehicles
{
	public class ActionUpgrade : Upgrade 
	{
		private List<ResolvedMethod<VehiclePawn>> upgradeMethods;

		private List<ResolvedMethod<VehiclePawn>> refundMethods;

		private bool unlockOnLoad = false;

		public override int ListerCount => 1;

		public override bool UnlockOnLoad => unlockOnLoad;

		public override void Unlock(VehiclePawn vehicle)
		{
			try
			{
				if (!upgradeMethods.NullOrEmpty())
				{
					foreach (ResolvedMethod<VehiclePawn> method in upgradeMethods)
					{
						method.Invoke(null, vehicle);
					}
				}
			}
			catch(Exception ex)
			{
				Log.Error($"{VehicleHarmony.LogLabel} Unable to invoke unlock method for {GetType()}. \nException: {ex}");
				return;
			}

			vehicle.VehicleDef.buildDef.soundBuilt?.PlayOneShot(new TargetInfo(vehicle.Position, vehicle.Map, false));
		}

		public override void Refund(VehiclePawn vehicle)
		{
			try
			{
				if (!refundMethods.NullOrEmpty())
				{
					foreach (ResolvedMethod<VehiclePawn> method in refundMethods)
					{
						method.Invoke(null, vehicle);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error($"{VehicleHarmony.LogLabel} Unable to invoke refund method for {GetType()}. \nException: {ex}");
				return;
			}
		}
	}
}
