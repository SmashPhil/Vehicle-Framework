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
		private List<ResolvedMethod<VehiclePawn>> unlockMethods;

		private List<ResolvedMethod<VehiclePawn>> refundMethods;

		private bool unlockOnLoad = false;

		public override bool UnlockOnLoad => unlockOnLoad;

		public override void Unlock(VehiclePawn vehicle, bool unlockingAfterLoad)
		{
			if (!unlockMethods.NullOrEmpty())
			{
				foreach (ResolvedMethod<VehiclePawn> method in unlockMethods)
				{
					method.Invoke(null, vehicle);
				}
			}
		}

		public override void Refund(VehiclePawn vehicle)
		{
			if (!refundMethods.NullOrEmpty())
			{
				foreach (ResolvedMethod<VehiclePawn> method in refundMethods)
				{
					method.Invoke(null, vehicle);
				}
			}
		}
	}
}
