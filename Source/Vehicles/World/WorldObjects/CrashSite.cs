using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Vehicles.World;

[PublicAPI]
public class CrashSite : MapParent
{
  // 10 seconds of observation before removal if all pawns are downed / dead
  public const int TicksTillRemovalAfterCrash = 10 * GenTicks.TicksPerRealSecond;

  private Settlement reinforcementsFrom;

  private int ticksSinceCrash;
  private int ticksTillReinforcements;
  private FloatRange scaleFactor = new(1.5f, 2.5f);

  private WorldPath pathToSite;

  public virtual Settlement Settlement => reinforcementsFrom;

  public int InitiateReinforcementsRequest([NotNull] Settlement reinforcementsFrom)
  {
    this.reinforcementsFrom = reinforcementsFrom;
    ticksSinceCrash = 0;
    pathToSite =
      reinforcementsFrom.Tile.Layer.Pather.FindPath(reinforcementsFrom.Tile, Tile, null);
    if (!pathToSite.Found)
    {
      ticksTillReinforcements = int.MaxValue;
      return -1;
    }
    return ticksTillReinforcements = Mathf.RoundToInt(pathToSite.TotalCost * 1.5f);
  }

  protected override void Tick()
  {
    base.Tick();
    ticksSinceCrash++;
    ticksTillReinforcements--;
    if (ticksTillReinforcements < 0 && reinforcementsFrom != null)
    {
      ReinforcementsArrived();
    }
  }

  protected virtual LordJob CreateLordJob(IncidentParms parms)
  {
    return new LordJob_AssaultColony(parms.faction, true, false);
  }

  protected virtual void ReinforcementsArrived()
  {
    if (!CellFinder.TryFindRandomEdgeCellWith(
      cell => cell.Standable(Map) && Map.reachability.CanReachColony(cell), Map,
      CellFinder.EdgeRoadChance_Hostile, out IntVec3 edgeCell))
    {
      return;
    }

    IncidentParms parms = new()
    {
      target = Map,
      points = StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap),
      faction = reinforcementsFrom.Faction
    };
    PawnGroupMakerParms defaultPawnGroupMakerParms =
      IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Combat, parms);
    defaultPawnGroupMakerParms.generateFightersOnly = true;
    defaultPawnGroupMakerParms.dontUseSingleUseRocketLaunchers = true;
    List<Pawn> enemies = PawnGroupMakerUtility.GeneratePawns(defaultPawnGroupMakerParms)
     .ToList();

    foreach (Pawn pawn in enemies)
    {
      IntVec3 loc = CellFinder.RandomSpawnCellForPawnNear(edgeCell, Map);
      GenSpawn.Spawn(pawn, loc, Map, Rot4.Random);
    }

    LordJob lordJob = CreateLordJob(parms);
    LordMaker.MakeNewLord(parms.faction, lordJob, Map, enemies);

    ChoiceLetter letter = LetterMaker.MakeLetter("VF_ReinforcementsArrivedLabel".Translate(),
      "VF_ReinforcementsArrived".Translate(reinforcementsFrom.Label), LetterDefOf.ThreatBig,
      reinforcementsFrom.Faction);
    Find.LetterStack.ReceiveLetter(letter);
    ticksTillReinforcements = Mathf.RoundToInt(pathToSite.TotalCost * scaleFactor.RandomInRange);
  }

  // TODO 1.7 - convert into Site with site part for followup raid
  public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
  {
    alsoRemoveWorldObject = false;
    if (ticksSinceCrash < TicksTillRemovalAfterCrash)
      return false;

    // Copying Site::ShouldRemoveNow
    if (Map.mapPawns.AnyPawnBlockingMapRemoval)
      return false;
    foreach (PocketMapParent pocketMapParent in Find.World.pocketMaps)
    {
      if (pocketMapParent.sourceMap == Map && pocketMapParent.Map.mapPawns.AnyPawnBlockingMapRemoval)
        return false;
    }
    if (ModsConfig.OdysseyActive && Map.listerThings.AnyThingWithDef(ThingDefOf.GravAnchor))
      return false;

    if (TransporterUtility.IncomingTransporterPreventingMapRemoval(Map))
      return false;

    alsoRemoveWorldObject = true;
    return true;
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_References.Look(ref reinforcementsFrom, nameof(reinforcementsFrom));
    Scribe_Values.Look(ref ticksTillReinforcements, nameof(ticksTillReinforcements));
    Scribe_Values.Look(ref ticksSinceCrash, nameof(ticksSinceCrash));

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
      pathToSite =
        reinforcementsFrom.Tile.Layer.Pather.FindPath(reinforcementsFrom.Tile, Tile, null);
    }
  }
}