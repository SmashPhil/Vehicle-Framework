using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Performance;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

public class IncidentWorker_ShuttleDowned : IncidentWorker
{
  public static void Execute(AerialVehicleInFlight aerialVehicle, string[] reasons,
    WorldObject culprit = null, IntVec3? cell = null)
  {
    IncidentWorker_ShuttleDowned incidentWorker =
      VehicleIncidentDefOf.ShuttleCrashed.Worker as IncidentWorker_ShuttleDowned;
    Assert.IsNotNull(incidentWorker);
    bool executed = incidentWorker.TryExecuteEvent(
      aerialVehicle, reasons, culprit: culprit, cell: cell);
    Assert.IsTrue(executed);
  }

  protected virtual string GetLetterText(AerialVehicleInFlight aerialVehicle, string[] reasons,
    WorldObject culprit)
  {
    StringBuilder letterText = new();
    if (culprit != null)
    {
      letterText.AppendLine("VF_IncidentCrashedSite_ShotDown".Translate(aerialVehicle, culprit));
    }
    else
    {
      letterText.AppendLine("VF_IncidentCrashedSite_Crashing".Translate(aerialVehicle));
      if (!reasons.NullOrEmpty())
      {
        letterText.AppendLine(
          "VF_IncidentReasonLister".Translate(string.Join(Environment.NewLine, reasons)));
      }
    }
    return letterText.ToString();
  }

  protected virtual string GetLetterLabel(AerialVehicleInFlight aerialVehicle,
    WorldObject culprit)
  {
    return culprit is null ?
      "VF_IncidentCrashedSiteLabel_Crashing".Translate(aerialVehicle.vehicle) :
      "VF_IncidentCrashedSiteLabel_ShotDown".Translate(aerialVehicle.vehicle, culprit);
  }

  protected virtual bool TryExecuteEvent(AerialVehicleInFlight aerialVehicle, string[] reasons,
    WorldObject culprit = null, IntVec3? cell = null)
  {
    try
    {
      int ticksTillArrival =
        GenerateMapAndReinforcements(aerialVehicle, culprit, out Map crashSite);
      IntVec3 crashingCell = cell ?? RandomCrashingCell(aerialVehicle, crashSite);
      if (crashingCell == IntVec3.Invalid)
        return false;

      AerialVehicleArrivalAction_CrashSpecificCell arrivalAction = new(aerialVehicle.vehicle,
        crashSite.Parent, crashSite.Tile, crashingCell, Rot4.East);
      arrivalAction.Arrived(aerialVehicle, crashSite.Tile);
      aerialVehicle.Destroy();
      string settlementLabel = culprit?.Label ?? string.Empty;
      if (ticksTillArrival > 0)
      {
        string hoursTillArrival = (ticksTillArrival / 2500f).RoundTo(1).ToString();
        SendCrashSiteLetter(culprit, GetLetterLabel(aerialVehicle, culprit),
          GetLetterText(aerialVehicle, reasons, culprit),
          def.letterDef, crashSite.Parent, aerialVehicle.Label, settlementLabel, hoursTillArrival);
      }
      else
      {
        SendCrashSiteLetter(culprit, GetLetterLabel(aerialVehicle, culprit),
          GetLetterText(aerialVehicle, reasons, culprit), def.letterDef, crashSite.Parent,
          aerialVehicle.Label, settlementLabel);
      }
      return true;
    }
    catch (Exception ex)
    {
      Log.Error($"Failed to execute incident {GetType()}. Exception=\"{ex}\"");
      return false;
    }
  }

  protected virtual IntVec3 RandomCrashingCell(AerialVehicleInFlight aerialVehicle, Map crashSite)
  {
    RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith(Validator, crashSite,
      out IntVec3 result);
    return result;

    bool Validator(IntVec3 cell)
    {
      if (cell.Fogged(crashSite))
        return false;
      if (!cell.InBounds(crashSite))
        return false;

      return aerialVehicle.vehicle.PawnOccupiedCells(cell, Rot4.East).All(hitboxCell =>
        hitboxCell.Walkable(aerialVehicle.vehicle.VehicleDef,
          crashSite.GetCachedMapComponent<VehiclePathingSystem>()) &&
        !Ext_Vehicles.IsRoofed(hitboxCell, crashSite));
    }
  }

  protected virtual int GenerateMapAndReinforcements(AerialVehicleInFlight aerialVehicle,
    WorldObject culprit, out Map crashSiteMap)
  {
    int ticksTillArrival = -1;
    if (Find.WorldObjects.MapParentAt(aerialVehicle.Tile) is { Map: not null } mapParent)
    {
      crashSiteMap = mapParent.Map;
    }
    else
    {
      int num = CaravanIncidentUtility.CalculateIncidentMapSize(
        aerialVehicle.vehicle.AllPawnsAboard, aerialVehicle.vehicle.AllPawnsAboard);
      crashSiteMap = GetOrGenerateMapUtility.GetOrGenerateMap(aerialVehicle.Tile,
        new IntVec3(num, 1, num), WorldObjectDefOfVehicles.CrashedShipSite);
      CrashSite crashSite = crashSiteMap.Parent as CrashSite;
      Assert.IsNotNull(crashSite);

      if (culprit is Settlement settlement)
        ticksTillArrival = crashSite.InitiateReinforcementsRequest(settlement);
      MapHelper.UnfogMapFromEdge(crashSiteMap, aerialVehicle.vehicle.VehicleDef);
    }
    return ticksTillArrival;
  }

  protected virtual void SendCrashSiteLetter(WorldObject shotDownBy, TaggedString baseLetterLabel,
    TaggedString baseLetterText, LetterDef letterDef,
    LookTargets lookTargets, params NamedArgument[] textArgs)
  {
    if (baseLetterLabel.NullOrEmpty() || baseLetterText.NullOrEmpty())
    {
      Log.Error("Sending standard incident letter with no label or text.");
    }
    ChoiceLetter choiceLetter = LetterMaker.MakeLetter(baseLetterLabel.Formatted(textArgs),
      baseLetterText.Formatted(textArgs), letterDef, lookTargets, shotDownBy?.Faction);
    List<HediffDef> list3 = [];
    if (!def.letterHyperlinkHediffDefs.NullOrEmpty())
      list3.AddRange(def.letterHyperlinkHediffDefs);
    choiceLetter.hyperlinkHediffDefs = list3;
    Find.LetterStack.ReceiveLetter(choiceLetter);
  }

  [NoProfiling]
  protected override bool TryExecuteWorker(IncidentParms parms)
  {
    throw new NotImplementedException("Shuttle downed event cannot be called through the Worker");
  }
}