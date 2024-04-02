using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Vehicles
{
	public class UpgradeInProgress : IExposable
	{
		private string nodeKey;

		[Unsaved]
		public UpgradeNode node;

		private VehiclePawn vehicle;

		protected float workLeft;

		protected bool cachedStoredCostSatisfied = false;
		
		public UpgradeInProgress(VehiclePawn vehicle, UpgradeNode node)
		{
			nodeKey = node.key;

			this.node = node;
			this.vehicle = vehicle;
		}

		public float WorkLeft => workLeft;

		public bool TryComplete(float amount)
		{
			workLeft -= amount;
			if (workLeft <= 0)
			{
				return true;
			}
			return false;
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref nodeKey, nameof(nodeKey));
			Scribe_References.Look(ref vehicle, nameof(vehicle));
			Scribe_Values.Look(ref workLeft, nameof(workLeft));

			if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				node = vehicle.CompUpgradeTree.Props.def.GetNode(nodeKey);
			}
		}
	}
}
