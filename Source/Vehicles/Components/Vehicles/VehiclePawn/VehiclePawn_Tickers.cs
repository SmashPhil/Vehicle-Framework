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
		private List<ThingComp> compTickers = new List<ThingComp>();

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

		protected override void ReceiveCompSignal(string signal)
		{
			if (signal == CompSignals.RanOutOfFuel)
			{
				vPather.StopDead();
				if (Spawned)
				{
					drafter.Drafted = false;
				}
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
			cachedComps = new SelfOrderingList<ThingComp>(AllComps);
			compTickers.Clear();
			foreach (ThingComp comp in AllComps)
			{
				if (comp.GetType().GetMethod("CompTick").MethodImplemented())
				{
					compTickers.Add(comp);
				}
			}
		}

		public override void Tick()
		{
			BaseTickOptimized();
			TickAllComps();
			if (Spawned)
			{
				vPather.PatherTick();
				sustainers.Tick();
			}
			if (Faction != Faction.OfPlayer)
			{
				vehicleAI?.AITick();
			}
			if (this.IsHashIntervalTick(150) && AllPawnsAboard.Any())
			{
				TrySatisfyPawnNeeds();
			}
		}

		public override void TickLong()
		{
			base.TickLong();
			statHandler.MarkAllDirty();
		}

		protected virtual void TickAllComps()
		{
			foreach (ThingComp comp in compTickers)
			{
				comp.CompTick();
			}
		}

		protected virtual void BaseTickOptimized()
		{
			if (Find.TickManager.TicksGame % 250 == 0)
			{
				TickRare();
			}
			bool suspended = Suspended;
			if (!suspended)
			{
				if (Spawned)
				{
					//stances.StanceTrackerTick(); //TODO - Add as tick requester for stunning
					jobs.JobTrackerTick();
				}
				//equipment?.EquipmentTrackerTick();

				//caller?.CallTrackerTick();
				//skills?.SkillsTick();
				//abilities?.AbilitiesTick();
				inventory?.InventoryTrackerTick();
				drafter?.DraftControllerTick();
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
