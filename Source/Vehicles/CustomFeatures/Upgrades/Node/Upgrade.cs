using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public abstract class Upgrade
	{
		protected UpgradeNode node;

		public abstract bool UnlockOnLoad { get; }

		public virtual int ListerCount { get; }

		/// <summary>
		/// Called when node has upgraded fully, after upgrade build ticks hits 0 or triggered by god mode
		/// </summary>
		public abstract void Unlock(VehiclePawn vehicle);

		/// <summary>
		/// Undo Upgrade action. Should be polar opposite of Upgrade functionality to revert changes
		/// </summary>
		public abstract void Refund(VehiclePawn vehicle);

		public void Init(UpgradeNode node)
		{
			this.node = node;
		}
	}
}
