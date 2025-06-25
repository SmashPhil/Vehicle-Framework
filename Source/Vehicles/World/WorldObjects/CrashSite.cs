using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Vehicles.World;

public class CrashSite : MapParent
{
  private const int TicksTillRemovalAfterCrash = 500;

  private Settlement reinforcementsFrom;

  private int ticksSinceCrash;
  private int ticksTillReinforcements;
  private FloatRange scaleFactor = new(1.5f, 2.5f);

  private WorldPath pathToSite;

  public virtual Settlement Settlement
  {
    get { return reinforcementsFrom; }
  }

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

  public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
  {
    if (!Map.mapPawns.AnyPawnBlockingMapRemoval &&
      !MapHelper.AnyVehicleSkyfallersBlockingMap(Map) &&
      ticksSinceCrash >= TicksTillRemovalAfterCrash)
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