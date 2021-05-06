using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;

namespace Vehicles
{
	public class LordJob_ArmoredAssault : LordJob_VehicleNPC
	{
		private static readonly IntRange AssaultTimeBeforeGiveUp = new IntRange(26000, 38000);

		private static readonly IntRange SapTimeBeforeGiveUp = new IntRange(33000, 38000);

		private Faction assaulterFaction;

		private bool canKidnap = true;

		private bool canTimeoutOrFlee = true;

		private bool canSteal = true;

		public LordJob_ArmoredAssault()
		{
		}

		public LordJob_ArmoredAssault(SpawnedPawnParams parms)
		{
			assaulterFaction = parms.spawnerThing.Faction;
			canKidnap = false;
			canTimeoutOrFlee = false;
			canSteal = false;
		}

		public LordJob_ArmoredAssault(Faction assaulterFaction, bool canKidnap = true, bool canTimeoutOrFlee = true, bool canSteal = true)
		{
			this.assaulterFaction = assaulterFaction;
			this.canKidnap = canKidnap;
			this.canTimeoutOrFlee = canTimeoutOrFlee;
			this.canSteal = canSteal;
		}

		public override float MaxVehicleSpeed => 4;

		public override bool GuiltyOnDowned
		{
			get
			{
				return true;
			}
		}

		public override StateGraph CreateGraph()
		{
			StateGraph stateGraph = new StateGraph();
			LordToil lordToil = null;

			LordToil lordToil2 = new LordToil_AssaultColonyArmored();

			stateGraph.AddToil(lordToil2);
			LordToil_ExitMap lordToil_ExitMap = new LordToil_ExitMap(LocomotionUrgency.Jog, false, true);
			lordToil_ExitMap.useAvoidGrid = true;
			stateGraph.AddToil(lordToil_ExitMap);
			if (assaulterFaction.def.humanlikeFaction)
			{
				if (canTimeoutOrFlee)
				{
					Transition transition3 = new Transition(lordToil2, lordToil_ExitMap, false, true);
					if (lordToil != null)
					{
						transition3.AddSource(lordToil);
					}
					//transition3.AddTrigger(new Trigger_TicksPassed(sappers ? SapTimeBeforeGiveUp.RandomInRange : AssaultTimeBeforeGiveUp.RandomInRange));
					transition3.AddPreAction(new TransitionAction_Message("MessageRaidersGivenUpLeaving".Translate(assaulterFaction.def.pawnsPlural.CapitalizeFirst(), assaulterFaction.Name), null, 1f));
					stateGraph.AddTransition(transition3, false);
					Transition transition4 = new Transition(lordToil2, lordToil_ExitMap, false, true);
					if (lordToil != null)
					{
						transition4.AddSource(lordToil);
					}
					FloatRange floatRange = new FloatRange(0.25f, 0.35f);
					float randomInRange = floatRange.RandomInRange;
					transition4.AddTrigger(new Trigger_FractionColonyDamageTaken(randomInRange, 900f));
					transition4.AddPreAction(new TransitionAction_Message("MessageRaidersSatisfiedLeaving".Translate(assaulterFaction.def.pawnsPlural.CapitalizeFirst(), assaulterFaction.Name), null, 1f));
					stateGraph.AddTransition(transition4, false);
				}
				if (canKidnap)
				{
					LordToil startingToil = stateGraph.AttachSubgraph(new LordJob_Kidnap().CreateGraph()).StartingToil;
					Transition transition5 = new Transition(lordToil2, startingToil, false, true);
					if (lordToil != null)
					{
						transition5.AddSource(lordToil);
					}
					transition5.AddPreAction(new TransitionAction_Message("MessageRaidersKidnapping".Translate(assaulterFaction.def.pawnsPlural.CapitalizeFirst(), assaulterFaction.Name), null, 1f));
					transition5.AddTrigger(new Trigger_KidnapVictimPresent());
					stateGraph.AddTransition(transition5, false);
				}
				if (canSteal)
				{
					LordToil startingToil2 = stateGraph.AttachSubgraph(new LordJob_Steal().CreateGraph()).StartingToil;
					Transition transition6 = new Transition(lordToil2, startingToil2, false, true);
					if (lordToil != null)
					{
						transition6.AddSource(lordToil);
					}
					transition6.AddPreAction(new TransitionAction_Message("MessageRaidersStealing".Translate(assaulterFaction.def.pawnsPlural.CapitalizeFirst(), assaulterFaction.Name), null, 1f));
					transition6.AddTrigger(new Trigger_HighValueThingsAround());
					stateGraph.AddTransition(transition6, false);
				}
			}
			Transition transition7 = new Transition(lordToil2, lordToil_ExitMap, false, true);
			if (lordToil != null)
			{
				transition7.AddSource(lordToil);
			}
			transition7.AddTrigger(new Trigger_BecameNonHostileToPlayer());
			transition7.AddPreAction(new TransitionAction_Message("MessageRaidersLeaving".Translate(assaulterFaction.def.pawnsPlural.CapitalizeFirst(), assaulterFaction.Name), null, 1f));
			stateGraph.AddTransition(transition7, false);
			return stateGraph;
		}

		public override void ExposeData()
		{
			Scribe_References.Look(ref assaulterFaction, "assaulterFaction", false);
			Scribe_Values.Look(ref canKidnap, "canKidnap", true, false);
			Scribe_Values.Look(ref canTimeoutOrFlee, "canTimeoutOrFlee", true, false);
			Scribe_Values.Look(ref canSteal, "canSteal", true, false);
		}
	}
}
