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

			WorkLeft = node.work;
		}

		public float WorkLeft
		{
			get
			{
				return workLeft;
			}
			set
			{
				if (workLeft != value)
				{
					workLeft = value;
				}
			}
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
