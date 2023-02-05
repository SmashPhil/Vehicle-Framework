using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Sound;
using Verse.AI;
using Verse.AI.Group;
using SmashTools;

namespace Vehicles
{
	public partial class VehiclePawn
	{
		[Unsaved]
		public VehicleSustainers sustainers;

		private CompVehicleTurrets compVehicleTurrets;
		private CompFueledTravel compFuel;
		private CompUpgradeTree compUpgradeTree;
		private CompVehicleLauncher compVehicleLauncher;

		private SelfOrderingList<ThingComp> cachedComps = new SelfOrderingList<ThingComp>();
		private HashSet<ThingComp> compTickers = new HashSet<ThingComp>();

		public override bool Suspended => false; //Vehicles are not suspendable

		public CompVehicleTurrets CompVehicleTurrets
		{
			get
			{
				if (compVehicleTurrets is null)
				{
					compVehicleTurrets = GetSortedComp<CompVehicleTurrets>();
				}
				return compVehicleTurrets;
			}
		}

		public CompFueledTravel CompFueledTravel
		{
			get
			{
				if (compFuel is null)
				{
					compFuel = GetSortedComp<CompFueledTravel>();
				}
				return compFuel;
			}
		}

		public CompUpgradeTree CompUpgradeTree
		{
			get
			{
				if (compUpgradeTree is null)
				{
					compUpgradeTree = GetSortedComp<CompUpgradeTree>();
				}
				return compUpgradeTree;
			}
		}

		public CompVehicleLauncher CompVehicleLauncher
		{
			get
			{
				if (compVehicleLauncher is null)
				{
					compVehicleLauncher = GetSortedComp<CompVehicleLauncher>();
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

		public T GetSortedComp<T>() where T : ThingComp
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

		public override void Tick()
		{
			BaseTickOptimized();
			TickAllComps();
			if (Faction != Faction.OfPlayer)
			{
				vehicleAI?.AITick();
			}
			if (this.IsHashIntervalTick(150) && AllPawnsAboard.Count > 0)
			{
				TrySatisfyPawnNeeds();
			}
		}

		public bool RequestTickStart<T>(T comp) where T : ThingComp
		{
			return compTickers.Add(comp);
		}

		public bool RequestTickStop<T>(T comp) where T : ThingComp
		{
			return compTickers.Remove(comp);
		}

		protected virtual void TickAllComps()
		{
			foreach (ThingComp comp in compTickers)
			{
				comp.CompTick();
			}
		}

		public override void TickRare()
		{
			base.TickRare();
			statHandler.MarkAllDirty();
		}

		protected virtual void BaseTickOptimized()
		{
			if (Find.TickManager.TicksGame % 250 == 0)
			{
				TickRare();
			}
			if (!Suspended)
			{
				if (Spawned)
				{
					vPather.PatherTick();
					sustainers.Tick();
					//stances.StanceTrackerTick(); //TODO - Add as tick requester for stunning
					if (Drafted)
					{
						jobs.JobTrackerTick();
					}
				}
				//equipment?.EquipmentTrackerTick();

				//caller?.CallTrackerTick();
				//skills?.SkillsTick();
				//abilities?.AbilitiesTick();
				inventory?.InventoryTrackerTick();
				//relations?.RelationsTrackerTick();

				if (ModsConfig.RoyaltyActive)
				{
					//royalty?.RoyaltyTrackerTick();
				}
				ageTracker.AgeTick();
				records.RecordsTick();
			}
		}
	}
}
