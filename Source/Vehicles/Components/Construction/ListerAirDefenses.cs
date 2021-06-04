using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class ListerAirDefenses : MapComponent
	{
		private readonly Dictionary<Faction, HashSet<Building_Artillery>> airDefensesToScan = new Dictionary<Faction, HashSet<Building_Artillery>>();

		public ListerAirDefenses(Map map) : base(map)
		{
		}

		public List<Building_Artillery> AllAirDefenses() => airDefensesToScan.SelectMany(dict => dict.Value).ToList();

		public HashSet<Building_Artillery> AirDefensesForFaction(Faction faction)
		{
			if (airDefensesToScan.TryGetValue(faction, out var airDefenses))
			{
				return airDefenses;
			}
			return new HashSet<Building_Artillery>();
		}

		public void Notify_AirDefenseSpawned(Building_Artillery airDefense)
		{
			if (airDefensesToScan.TryGetValue(airDefense.Faction, out var airDefenses))
			{
				airDefenses.Add(airDefense);
			}
			else
			{
				airDefensesToScan.Add(airDefense.Faction, new HashSet<Building_Artillery>() { airDefense });
			}
		}

		public void Notify_AirDefenseDespawned(Building_Artillery airDefense)
		{
			if (airDefensesToScan.TryGetValue(airDefense.Faction, out var airDefenses))
			{
				airDefenses.Remove(airDefense);
			}
		}
	}
}
