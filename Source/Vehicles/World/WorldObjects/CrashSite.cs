using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Vehicles
{
	public class CrashSite : MapParent
	{
		private const int TicksTillRemovalAfterCrash = 500;
		
		private Settlement reinforcementsFrom;

		private int ticksSinceCrash;
		private int ticksTillReinforcements;
		private FloatRange scaleFactor = new FloatRange(1.5f, 2.5f);

		private WorldPath pathToSite;

		public virtual Settlement Settlement
		{
			get
			{
				return reinforcementsFrom;
			}
		}

		public int InitiateReinforcementsRequest(Settlement reinforcementsFrom)
		{
			this.reinforcementsFrom = reinforcementsFrom;
			ticksSinceCrash = 0;
			pathToSite = Find.WorldPathFinder.FindPath(reinforcementsFrom.Tile, Tile, null);
			if (reinforcementsFrom is null || !pathToSite.Found)
			{
				ticksTillReinforcements = int.MaxValue;
				return -1;
			}
			return ticksTillReinforcements = Mathf.RoundToInt(pathToSite.TotalCost * 1.5f);
		}

		public override void Tick()
		{
			base.Tick();
			if (!MapHelper.AnyVehicleSkyfallersBlockingMap(Map))
			{
				ticksSinceCrash++;
			}

			ticksTillReinforcements--;
			if (ticksTillReinforcements < 0 && reinforcementsFrom != null)
			{
				ReinforcementsArrived();
			}
		}

		protected virtual LordJob CreateLordJob(List<Pawn> generatedPawns, IncidentParms parms)
		{
			return new LordJob_AssaultColony(parms.faction, true, false, false, false, true);
		}

		protected virtual void ReinforcementsArrived()
		{
			if (!CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => x.Standable(Map) && Map.reachability.CanReachColony(x), Map, CellFinder.EdgeRoadChance_Hostile, out IntVec3 edgeCell))
			{
				return;
			}

			IncidentParms parms = new IncidentParms()
			{
				target = Map,
				points = StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap),
				faction = reinforcementsFrom.Faction
			};
			PawnGroupMakerParms defaultPawnGroupMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Combat, parms, false);
			defaultPawnGroupMakerParms.generateFightersOnly = true;
			defaultPawnGroupMakerParms.dontUseSingleUseRocketLaunchers = true;
			List<Pawn> enemies = PawnGroupMakerUtility.GeneratePawns(defaultPawnGroupMakerParms, true).ToList();

			for (int i = 0; i < enemies.Count; i++)
			{
				IntVec3 loc = CellFinder.RandomSpawnCellForPawnNear(edgeCell, Map, 4);
				GenSpawn.Spawn(enemies[i], loc, Map, Rot4.Random, WipeMode.Vanish, false);
			}

			LordJob lordJob = CreateLordJob(enemies, parms);
			if (lordJob != null)
			{
				LordMaker.MakeNewLord(parms.faction, lordJob, Map, enemies);
			}

			var letter = LetterMaker.MakeLetter("ReinforcementsArrivedLabel".Translate(), "ReinforcementsArrived".Translate(reinforcementsFrom.Label), LetterDefOf.ThreatBig, reinforcementsFrom.Faction);
			Find.LetterStack.ReceiveLetter(letter);
			ticksTillReinforcements = Mathf.RoundToInt(pathToSite.TotalCost * scaleFactor.RandomInRange);
		}

		public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
		{
			if (!Map.mapPawns.AnyPawnBlockingMapRemoval && !MapHelper.AnyVehicleSkyfallersBlockingMap(Map) && ticksSinceCrash >= TicksTillRemovalAfterCrash)
			{
				alsoRemoveWorldObject = true;
				return true;
			}
			alsoRemoveWorldObject = false;
			return false;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref reinforcementsFrom, "reinforcementsFrom");
			Scribe_Values.Look(ref ticksTillReinforcements, "ticksTillReinforcements");
			Scribe_Values.Look(ref ticksSinceCrash, "ticksSinceCrash");
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				pathToSite = Find.WorldPathFinder.FindPath(reinforcementsFrom.Tile, Tile, null);
			}
		}
	}
}
