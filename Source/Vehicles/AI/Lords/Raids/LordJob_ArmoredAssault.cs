using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;

namespace Vehicles;

public class LordJob_ArmoredAssault : LordJob_VehicleNPC
{
  private static readonly IntRange AssaultTimeBeforeGiveUp = new IntRange(26000, 38000);

  private static readonly IntRange SapTimeBeforeGiveUp = new IntRange(33000, 38000);

  private Faction assaulterFaction;

  private RaiderPermissions permission = RaiderPermissions.All;
  private RaiderBehavior behavior = RaiderBehavior.None;

  public LordJob_ArmoredAssault()
  {
  }

  public LordJob_ArmoredAssault(SpawnedPawnParams parms)
  {
    assaulterFaction = parms.spawnerThing.Faction;
    permission = RaiderPermissions.All;
  }

  public LordJob_ArmoredAssault(Faction assaulterFaction,
    RaiderPermissions permission)
  {
    this.assaulterFaction = assaulterFaction;
    this.permission = permission;
  }

  public override float MaxVehicleSpeed => 4;

  public override bool GuiltyOnDowned => true;

  public override StateGraph CreateGraph()
  {
    StateGraph stateGraph = new();

    // Root behavior before any assault, such as Sapper to target position
    LordToil rootToil = null;
    // Main assault loop
    LordToil assaultColonyToil = new LordToil_AssaultColonyArmored();

    stateGraph.AddToil(assaultColonyToil);
    LordToil_ExitMap exitMapToil = new(LocomotionUrgency.Jog, interruptCurrentJob: true);
    exitMapToil.useAvoidGrid = true;
    stateGraph.AddToil(exitMapToil);

    if (assaulterFaction.def.humanlikeFaction)
    {
      // Exit map promptly
      AddTimeoutOrFleeToil(stateGraph, rootToil, assaultColonyToil, exitMapToil);
      // Kidnap someone and leave
      AddKidnapToil(stateGraph, rootToil, assaultColonyToil);
      // Steal stuff and leave
      AddCanStealToil(stateGraph, rootToil, assaultColonyToil);
    }

    // Exit map leisurely (Non-hostile)
    Transition leaveMapTransition = new(assaultColonyToil, exitMapToil);
    if (rootToil != null)
    {
      leaveMapTransition.AddSource(rootToil);
    }

    leaveMapTransition.AddTrigger(new Trigger_BecameNonHostileToPlayer());
    leaveMapTransition.AddPreAction(new TransitionAction_Message(
      "MessageRaidersLeaving".Translate(assaulterFaction.def.pawnsPlural.CapitalizeFirst(),
        assaulterFaction.Name)));
    stateGraph.AddTransition(leaveMapTransition);

    return stateGraph;
  }

  private void AddTimeoutOrFleeToil(StateGraph stateGraph, LordToil rootToil,
    LordToil assaultColonyToil,
    LordToil exitMapToil)
  {
    if (!permission.canTimeoutOrFlee) return;

    Transition giveUpAndLeaveTransition = new(assaultColonyToil, exitMapToil);
    if (rootToil != null)
    {
      giveUpAndLeaveTransition.AddSource(rootToil);
    }

    //giveUpAndLeaveTransition.AddTrigger(new Trigger_TicksPassed(sappers ? SapTimeBeforeGiveUp.RandomInRange : AssaultTimeBeforeGiveUp.RandomInRange));
    giveUpAndLeaveTransition.AddPreAction(new TransitionAction_Message(
      "MessageRaidersGivenUpLeaving".Translate(
        assaulterFaction.def.pawnsPlural.CapitalizeFirst(), assaulterFaction.Name), null,
      1f));
    stateGraph.AddTransition(giveUpAndLeaveTransition);
    Transition satisfiedLeaveTransition = new(assaultColonyToil, exitMapToil);
    if (rootToil != null)
    {
      satisfiedLeaveTransition.AddSource(rootToil);
    }

    float desiredColonyDamagePct = new FloatRange(0.25f, 0.35f).RandomInRange;
    satisfiedLeaveTransition.AddTrigger(
      new Trigger_FractionColonyDamageTaken(desiredColonyDamagePct, 900f));
    satisfiedLeaveTransition.AddPreAction(new TransitionAction_Message(
      "MessageRaidersSatisfiedLeaving".Translate(
        assaulterFaction.def.pawnsPlural.CapitalizeFirst(), assaulterFaction.Name)));
    stateGraph.AddTransition(satisfiedLeaveTransition);
  }

  private void AddKidnapToil(StateGraph stateGraph, LordToil rootToil, LordToil assaultColonyToil)
  {
    if (!permission.canKidnap) return;

    LordToil kidnapToil =
      stateGraph.AttachSubgraph(new LordJob_Kidnap().CreateGraph()).StartingToil;
    Transition kidnapAndLeaveTransition = new(assaultColonyToil, kidnapToil);
    if (rootToil != null)
    {
      kidnapAndLeaveTransition.AddSource(rootToil);
    }

    kidnapAndLeaveTransition.AddPreAction(new TransitionAction_Message(
      "MessageRaidersKidnapping".Translate(assaulterFaction.def.pawnsPlural.CapitalizeFirst(),
        assaulterFaction.Name), null, 1f));
    kidnapAndLeaveTransition.AddTrigger(new Trigger_KidnapVictimPresent());
    stateGraph.AddTransition(kidnapAndLeaveTransition);
  }

  private void AddCanStealToil(StateGraph stateGraph, LordToil rootToil, LordToil assaultColonyToil)
  {
    if (!permission.canSteal) return;

    LordToil stealThingToil =
      stateGraph.AttachSubgraph(new LordJob_Steal().CreateGraph()).StartingToil;
    Transition stealThingTransition = new(assaultColonyToil, stealThingToil);
    if (rootToil != null)
    {
      stealThingTransition.AddSource(rootToil);
    }

    stealThingTransition.AddPreAction(new TransitionAction_Message(
      "MessageRaidersStealing".Translate(assaulterFaction.def.pawnsPlural.CapitalizeFirst(),
        assaulterFaction.Name)));
    stealThingTransition.AddTrigger(new Trigger_HighValueThingsAround());
    stateGraph.AddTransition(stealThingTransition);
  }

  public override void ExposeData()
  {
    Scribe_References.Look(ref assaulterFaction, nameof(assaulterFaction));
    Scribe_Deep.Look(ref permission, nameof(permission));
    Scribe_Values.Look(ref behavior, nameof(behavior), RaiderBehavior.None);
  }

  [Flags]
  public enum RaiderBehavior
  {
    None = 0,
    Sapper = 1 << 0,
  }

  public struct RaiderPermissions : IExposable
  {
    public bool canKidnap;
    public bool canTimeoutOrFlee;
    public bool canSteal;

    public bool shouldSabotageVehicles;

    public static RaiderPermissions All => new()
    {
      canKidnap = true,
      canTimeoutOrFlee = true,
      canSteal = true,

      shouldSabotageVehicles = true,
    };

    void IExposable.ExposeData()
    {
      Scribe_Values.Look(ref canKidnap, nameof(canKidnap));
      Scribe_Values.Look(ref canTimeoutOrFlee, nameof(canTimeoutOrFlee));
      Scribe_Values.Look(ref canSteal, nameof(canSteal));

      Scribe_Values.Look(ref shouldSabotageVehicles, nameof(shouldSabotageVehicles));
    }
  }
}