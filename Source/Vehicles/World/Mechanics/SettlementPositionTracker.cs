using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class SettlementPositionTracker : GameComponent 
	{
		private static List<Settlement> enemySettlements = new List<Settlement>();
		private static HashSet<SettlementAirDefense> activeSettlementDefenses = new HashSet<SettlementAirDefense>();

		public static Dictionary<Settlement, SettlementAirDefense> airDefenseCache = new Dictionary<Settlement, SettlementAirDefense>();
		private static Dictionary<int, SettlementAirDefense> airDefenseSaveable = new Dictionary<int, SettlementAirDefense>();

		public SettlementPositionTracker(Game game)
		{
		}

		public void FactionStatusChanged(Faction faction)
		{
			if (faction?.def.techLevel >= TechLevel.Industrial)
			{
				if (faction.HostileTo(Faction.OfPlayer))
				{
					enemySettlements.AddRange(Find.WorldObjects.Settlements.Where(s => s.Faction == faction));
				}
				else
				{
					enemySettlements.RemoveAll(s => s.Faction == faction);
				}
			}
		}

		public override void GameComponentTick()
		{
			base.GameComponentTick();
			for (int i = activeSettlementDefenses?.Count - 1 ?? -1; i >= 0; i--)
			{
				var defense = activeSettlementDefenses.ElementAt(i);
				defense.Attack();
			}
		}

		public override void FinalizeInit()
		{
			base.FinalizeInit();
			if (activeSettlementDefenses is null)
			{
				activeSettlementDefenses = new HashSet<SettlementAirDefense>();
			}
			if (enemySettlements is null)
			{
				enemySettlements = new List<Settlement>();
			}
			if (airDefenseCache is null)
			{
				airDefenseCache = new Dictionary<Settlement, SettlementAirDefense>();
			}
			RecacheSettlements();
		}

		public static void ActivateSettlementDefenses(SettlementAirDefense defense)
		{
			activeSettlementDefenses.Add(defense);
		}

		public static void DeactivateSettlementDefenses(SettlementAirDefense defense)
		{
			activeSettlementDefenses.Remove(defense);
		}

		public void RecacheSettlements()
		{
			airDefenseCache.Clear();
			enemySettlements.Clear();
			foreach (Settlement settlement in Find.WorldObjects.SettlementBases)
			{
				if (settlement.Faction?.def.techLevel >= TechLevel.Industrial)
				{
					if (!airDefenseCache.ContainsKey(settlement))
					{
						airDefenseCache.Add(settlement, new SettlementAirDefense(settlement));
					}
					if (settlement.Faction.HostileTo(Faction.OfPlayer))
					{
						enemySettlements.Add(settlement);
					}
				}
			}
		}

		public static void HighlightEnemySettlements()
		{
			if (WorldRendererUtility.WorldRenderedNow)
			{
				foreach (Settlement settlement in enemySettlements)
				{
					GenDraw.DrawWorldRadiusRing(settlement.Tile, airDefenseCache[settlement].radarDistance);
				}
			}
		}

		public static IEnumerable<Settlement> CheckNearbySettlements(AerialVehicleInFlight aerialVehicle, float speedPctPerTick)
		{
			float halfTicksPerTileTraveled = Ext_Math.RoundTo(speedPctPerTick * 100, 0.01f);
			int start = aerialVehicle.Tile;
			for (int i = 0; i < aerialVehicle.flightPath.Path.Count; i++)
			{
				int destination = aerialVehicle.flightPath[i];

				Vector3 position = Find.WorldGrid.GetTileCenter(start);
				for (float transition = 0; transition < 1; transition += halfTicksPerTileTraveled)
				{
					Vector3 partition = Vector3.Slerp(position, Find.WorldGrid.GetTileCenter(destination), transition);
					foreach (Settlement settlement in enemySettlements)
					{
						float distance = Ext_Math.SphericalDistance(partition, settlement.DrawPos);
						if (distance < airDefenseCache[settlement].radarDistance)
						{
							yield return settlement;
						}
					}
				}
				start = destination;
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref activeSettlementDefenses, "activeSettlementDefenses", LookMode.Deep);
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				airDefenseSaveable = new Dictionary<int, SettlementAirDefense>();
				if (airDefenseCache is null)
				{
					airDefenseCache = new Dictionary<Settlement, SettlementAirDefense>();
				}
				foreach (var settlementGroup in airDefenseCache)
				{
					airDefenseSaveable.Add(settlementGroup.Key.Tile, settlementGroup.Value);
				}
			}
			Scribe_Collections.Look(ref airDefenseSaveable, "airDefenseSaveable", LookMode.Value, LookMode.Deep);
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (airDefenseSaveable is null)
				{
					airDefenseSaveable = new Dictionary<int, SettlementAirDefense>();
				}
				foreach (var settlementGroup in airDefenseSaveable)
				{
					airDefenseCache.Add(Find.WorldObjects.SettlementAt(settlementGroup.Key), settlementGroup.Value);
				}
			}
		}
	}
}
