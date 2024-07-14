using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using SmashTools;
using UnityEngine;

namespace Vehicles
{
	public partial class VehiclePawn
	{
		[Unsaved]
		private bool fetchedCompVehicleTurrets;
		[Unsaved]
		private bool fetchedCompFuel;
		[Unsaved]
		private bool fetchedCompUpgradeTree;
		[Unsaved]
		private bool fetchedCompVehicleLauncher;

		[Unsaved]
		private CompVehicleTurrets compVehicleTurrets;
		[Unsaved]
		private CompFueledTravel compFuel;
		[Unsaved]
		private CompUpgradeTree compUpgradeTree;
		[Unsaved]
		private CompVehicleLauncher compVehicleLauncher;

		[Unsaved]
		private SelfOrderingList<ThingComp> cachedComps = new SelfOrderingList<ThingComp>();
		[Unsaved]
		private List<ThingComp> deactivatedComps = new List<ThingComp>();
		[Unsaved]
		private List<ThingComp> compTickers = new List<ThingComp>();

		private List<ActivatableThingComp> activatableComps = new List<ActivatableThingComp>();
		private List<Type> deactivatedCompTypes = new List<Type>();

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
			AllComps.Add(comp);
			RecacheComponents();
		}

		public bool RemoveComp(ThingComp comp)
		{
			bool result = AllComps.Remove(comp);
			if (result)
			{
				RecacheComponents();
			}
			return result;
		}

		public void ActivateComp(ThingComp comp)
		{
			ActivatableThingComp activatableComp = activatableComps.FirstOrDefault(activatableComp => activatableComp.Type == comp.GetType());
			if (activatableComp == null)
			{
				activatableComp = new ActivatableThingComp();
				activatableComp.Init(this, comp);
				activatableComps.Add(activatableComp);
			}
			activatableComp.Owners++;
		}

		public void DeactivateComp(ThingComp comp)
		{
			foreach (ActivatableThingComp activatableComp in activatableComps)
			{
				if (activatableComp.Type == comp.GetType())
				{
					activatableComp.Owners--;
					return;
				}
			}
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

		public ThingComp GetComp(Type type)
		{
			for (int i = 0; i < AllComps.Count; i++) //AllComps should always be initialized to new instance list, and never be null
			{
				if (AllComps[i].GetType().SameOrSubclass(type))
				{
					return AllComps[i];
				}
			}
			return default;
		}

		public ThingComp GetDeactivatedComp(Type type)
		{
			for (int i = 0; i < deactivatedComps.Count; i++) //AllComps should always be initialized to new instance list, and never be null
			{
				if (deactivatedComps[i].GetType().SameOrSubclass(type))
				{
					return deactivatedComps[i];
				}
			}
			return default;
		}

		protected virtual void RecacheComponents()
		{
			fetchedCompVehicleTurrets = false;
			fetchedCompFuel = false;
			fetchedCompUpgradeTree = false;
			fetchedCompVehicleLauncher = false;

			cachedComps.Clear();
			if (!AllComps.NullOrEmpty())
			{
				cachedComps.AddRange(AllComps);
			}

			RecacheCompTickers();
		}

		private void RecacheCompTickers()
		{
			compTickers.Clear();
			foreach (ThingComp thingComp in AllComps)
			{
				if (!(thingComp is VehicleComp vehicleComp) || !vehicleComp.TickByRequest)
				{
					compTickers.Add(thingComp);
				}
			}
		}

		private void SyncActivatableComps()
		{
			foreach (ActivatableThingComp activatableComp in activatableComps)
			{
				ThingComp matchingComp = AllComps.FirstOrDefault(thingComp => thingComp.GetType() == activatableComp.Type);
				if (matchingComp == null)
				{
					Log.Error($"Unable to sync {activatableComp.Type}. No matching comp in comp list.");
					RemoveComp(matchingComp);
					continue;
				}
				activatableComp.Init(this, matchingComp);
				activatableComp.RevalidateCompStatus();
			}
		}

		private class ActivatableThingComp : IExposable
		{
			[Unsaved]
			private VehiclePawn vehicle;
			[Unsaved]
			private ThingComp comp;

			private int owners;
			private Type type;
			
			public ActivatableThingComp()
			{
			}

			public bool Deactivated => owners == 0;

			public Type Type => type;

			public int Owners
			{
				get
				{
					return owners;
				}
				set
				{
					if (owners != value)
					{
						owners = Mathf.Clamp(value, 0, int.MaxValue);
						RevalidateCompStatus();
					}
				}
			}

			public void RevalidateCompStatus()
			{
				if (Deactivated)
				{
					if (vehicle.RemoveComp(comp))
					{
						vehicle.deactivatedComps.Add(comp);
						vehicle.deactivatedCompTypes.Add(comp.GetType());
						vehicle.activatableComps.Remove(this);
					}
				}
				else if (!vehicle.AllComps.Contains(comp))
				{
					vehicle.AddComp(comp);
				}
			}

			public void Init(VehiclePawn vehicle, ThingComp comp)
			{
				this.vehicle = vehicle;
				this.comp = comp;
				type = comp.GetType();
			}

			public void ExposeData()
			{
				Scribe_Values.Look(ref owners, nameof(owners));
				Scribe_Values.Look(ref type, nameof(type));
			}
		}
	}
}
