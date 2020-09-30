using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;

namespace Vehicles
{
    public class IncidentWorker_ArmoredRaid : IncidentWorker_RaidEnemy
    {
        protected override bool FactionCanBeGroupSource(Faction f, Map map, bool desperate = false)
		{
			return base.FactionCanBeGroupSource(f, map, desperate) && f.HostileTo(Faction.OfPlayer) && (desperate || (float)GenDate.DaysPassed >= f.def.earliestRaidDays);
		}

		protected override bool TryExecuteWorker(IncidentParms parms)
		{
			if (!TryExecuteVehicleRaid(parms))
			{
				return false;
			}
			Find.TickManager.slower.SignalForceNormalSpeedShort();
			Find.StoryWatcher.statsRecord.numRaidsEnemy++;
			return true;
		}

		protected virtual bool TryExecuteVehicleRaid(IncidentParms parms)
        {
			ResolveRaidPoints(parms);
			if (!TryResolveRaidFaction(parms))
			{
				return false;
			}
			PawnGroupKindDef combat = PawnGroupKindDefOf.Combat;
			ResolveRaidStrategy(parms, combat);
			ResolveRaidArriveMode(parms);
			parms.raidStrategy.Worker.TryGenerateThreats(parms);
			if (!parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms))
			{
				return false;
			}
			float points = parms.points;
			parms.points = AdjustedRaidPoints(parms.points, parms.raidArrivalMode, parms.raidStrategy, parms.faction, combat);

			List<Pawn> vehicleList = new List<Pawn>();
			
			List<Pawn> list = PawnGroupMakerUtility.GeneratePawns(IncidentParmsUtility.GetDefaultPawnGroupMakerParms(combat, parms, false), true).ToList<Pawn>();
			if (list.Count == 0)
			{
				Log.Error("Got no pawns spawning raid from parms " + parms, false);
				return false;
			}
			vehicleList = ReplaceWithVehicles(parms, list);
			parms.raidArrivalMode.Worker.Arrive(vehicleList, parms);

			GenerateRaidLoot(parms, points, vehicleList);
			TaggedString baseLetterLabel = GetLetterLabel(parms);
			TaggedString baseLetterText = GetLetterText(parms, vehicleList);
			PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(vehicleList, ref baseLetterLabel, ref baseLetterText, GetRelatedPawnsInfoLetterText(parms), true, true);
			List<TargetInfo> list2 = new List<TargetInfo>();
			if (parms.pawnGroups != null)
			{
				List<List<Pawn>> list3 = IncidentParmsUtility.SplitIntoGroups(vehicleList, parms.pawnGroups);
				List<Pawn> list4 = list3.MaxBy((List<Pawn> x) => x.Count);
				if (list4.Any())
				{
					list2.Add(list4[0]);
				}
				for (int i = 0; i < list3.Count; i++)
				{
					if (list3[i] != list4 && list3[i].Any())
					{
						list2.Add(list3[i][0]);
					}
				}
			}
			else if (vehicleList.Any())
			{
				foreach (Pawn t in vehicleList)
				{
					list2.Add(t);
				}
			}
			SendStandardLetter(baseLetterLabel, baseLetterText, GetLetterDef(), parms, list2, Array.Empty<NamedArgument>());
			parms.raidStrategy.Worker.MakeLords(parms, vehicleList);
			LessonAutoActivator.TeachOpportunity(ConceptDefOf.EquippingWeapons, OpportunityType.Critical);
			if (!PlayerKnowledgeDatabase.IsComplete(ConceptDefOf.ShieldBelts))
			{
				for (int j = 0; j < vehicleList.Count; j++)
				{
					if (vehicleList[j].apparel?.WornApparel.Any((Apparel ap) => ap is ShieldBelt) ?? false)
					{
						LessonAutoActivator.TeachOpportunity(ConceptDefOf.ShieldBelts, OpportunityType.Critical);
						break;
					}
				}
			}
			return true;
        }

		protected virtual List<Pawn> ReplaceWithVehicles(IncidentParms parms, List<Pawn> pawns)
        {
			List<PawnKindDef> vehicles = VehicleSpawner.GetAppropriateVehicles(parms.faction, parms.points, parms.generateFightersOnly).ToList();
            Log.Message($"Total: {parms.points} Group: {parms.raidStrategy} Vehicles: {vehicles.Count}");
            int countToReplace = pawns.Count / 2;

            List<VehiclePawn> vehicleTmp = new List<VehiclePawn>();
            foreach (PawnKindDef vehicleKind in vehicles)
            {
                CompProperties_Vehicle comp = vehicleKind.race.GetCompProperties<CompProperties_Vehicle>();
                if (comp.roles.Where(r => !r.handlingTypes.NullOrEmpty()).Count() <= countToReplace)
                {
                    countToReplace -= comp.roles.Where(r => !r.handlingTypes.NullOrEmpty()).Count();
                    VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(new VehicleGenerationRequest(vehicleKind, parms.faction, true, true));
                    vehicleTmp.Add(vehicle);
                }
                if (countToReplace <= 0)
                    break;
            }
			pawns.AddRange(vehicleTmp);
			return pawns;
        }

		protected override bool TryResolveRaidFaction(IncidentParms parms)
		{
			Map map = (Map)parms.target;
			if (parms.faction != null)
			{
				return true;
			}
			float num = parms.points;
			if (num <= 0f)
			{
				num = 999999f;
			}
			return PawnGroupMakerUtility.TryGetRandomFactionForCombatPawnGroup(num, out parms.faction, (Faction f) => this.FactionCanBeGroupSource(f, map, false), true, true, true, true) || PawnGroupMakerUtility.TryGetRandomFactionForCombatPawnGroup(num, out parms.faction, (Faction f) => this.FactionCanBeGroupSource(f, map, true), true, true, true, true);
		}

		public override void ResolveRaidStrategy(IncidentParms parms, PawnGroupKindDef groupKind)
		{
			parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
			return;
			if (parms.raidStrategy != null)
			{
				return;
			}
			Map map = (Map)parms.target;
			Predicate<PawnsArrivalModeDef> validator;
			RaidStrategyDef raidStrategy;
			DefDatabase<RaidStrategyDef>.AllDefs.Where(delegate(RaidStrategyDef d)
			{
				if (!d.Worker.CanUseWith(parms, groupKind))
				{
					return false;
				}
				if (parms.raidArrivalMode != null)
				{
					return true;
				}
				if (d.arriveModes != null)
				{
					List<PawnsArrivalModeDef> arriveModes = d.arriveModes;
					Predicate<PawnsArrivalModeDef> validator2;
					if ((validator2 = validator) == null)
					{
						validator2 = (validator = ((PawnsArrivalModeDef x) => x.Worker.CanUseWith(parms)));
					}
					return arriveModes.Any(validator2);
				}
				return false;
			}).TryRandomElementByWeight((RaidStrategyDef d) => d.Worker.SelectionWeight(map, parms.points), out raidStrategy);
			parms.raidStrategy = raidStrategy;
			if (parms.raidStrategy == null)
			{
				Log.Error(string.Concat(new object[]
				{
					"No raid stategy found, defaulting to ImmediateAttack. Faction=",
					parms.faction.def.defName,
					", points=",
					parms.points,
					", groupKind=",
					groupKind,
					", parms=",
					parms
				}), false);
				parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
			}
		}

		//protected override void GenerateRaidLoot(IncidentParms parms, float raidLootPoints, List<Pawn> pawns)
		//{
		//	if (parms.faction.def.raidLootMaker == null || !pawns.Any<Pawn>())
		//	{
		//		return;
		//	}
		//	raidLootPoints *= Find.Storyteller.difficultyValues.EffectiveRaidLootPointsFactor;
		//	float num = parms.faction.def.raidLootValueFromPointsCurve.Evaluate(raidLootPoints);
		//	if (parms.raidStrategy != null)
		//	{
		//		num *= parms.raidStrategy.raidLootValueFactor;
		//	}
		//	ThingSetMakerParams parms2 = default(ThingSetMakerParams);
		//	parms2.totalMarketValueRange = new FloatRange?(new FloatRange(num, num));
		//	parms2.makingFaction = parms.faction;
		//	List<Thing> loot = parms.faction.def.raidLootMaker.root.Generate(parms2);
		//	new RaidLootDistributor(parms, pawns, loot).DistributeLoot();
		//}
    }
}
