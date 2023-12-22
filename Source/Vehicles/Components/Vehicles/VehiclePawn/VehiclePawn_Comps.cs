using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using SmashTools;

namespace Vehicles
{
	public partial class VehiclePawn
	{
		private bool fetchedCompVehicleTurrets;
		private bool fetchedCompFuel;
		private bool fetchedCompUpgradeTree;
		private bool fetchedCompVehicleLauncher;

		private CompVehicleTurrets compVehicleTurrets;
		private CompFueledTravel compFuel;
		private CompUpgradeTree compUpgradeTree;
		private CompVehicleLauncher compVehicleLauncher;

		private SelfOrderingList<ThingComp> cachedComps = new SelfOrderingList<ThingComp>();
		private List<ThingComp> compTickers = new List<ThingComp>();

		public CompVehicleTurrets CompVehicleTurrets
		{
			get
			{
				if (!fetchedCompVehicleTurrets)
				{
					compVehicleTurrets = GetCachedComp<CompVehicleTurrets>();
					fetchedCompVehicleTurrets = true;
				}
				return compVehicleTurrets;
			}
		}

		public CompFueledTravel CompFueledTravel
		{
			get
			{
				if (!fetchedCompFuel)
				{
					compFuel = GetCachedComp<CompFueledTravel>();
					fetchedCompFuel = true;
				}
				return compFuel;
			}
		}

		public CompUpgradeTree CompUpgradeTree
		{
			get
			{
				if (!fetchedCompUpgradeTree)
				{
					compUpgradeTree = GetCachedComp<CompUpgradeTree>();
					fetchedCompUpgradeTree = true;
				}
				return compUpgradeTree;
			}
		}

		public CompVehicleLauncher CompVehicleLauncher
		{
			get
			{
				if (!fetchedCompVehicleLauncher)
				{
					compVehicleLauncher = GetCachedComp<CompVehicleLauncher>();
					fetchedCompVehicleLauncher = true;
				}
				return compVehicleLauncher;
			}
		}

		public void AddComp(ThingComp comp)
		{
			cachedComps.Add(comp);
		}

		public void RemoveComp(ThingComp comp)
		{
			cachedComps.Remove(comp);
		}

		public T GetCachedComp<T>() where T : ThingComp
		{
			for (int i = 0; i < cachedComps.Count; i++)
			{
				if (cachedComps[i] is T t)
				{
					cachedComps.CountIndex(i);
					return t;
				}
			}
			return default;
		}

		protected virtual void RecacheComponents()
		{
			cachedComps = new SelfOrderingList<ThingComp>();
			foreach (ThingComp thingComp in AllComps)
			{
				cachedComps.Add(thingComp);
				if (!(thingComp is VehicleComp vehicleComp) || !vehicleComp.TickByRequest)
				{
					compTickers.Add(thingComp); //Tick normally, if VehicleComp and not TickByRequest it cannot request to stop
				}
			}
		}

	}
}
