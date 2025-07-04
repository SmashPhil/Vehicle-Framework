using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

[PublicAPI]
public static class CaravanHelper
{
  private static readonly HashSet<PlanetTile> AvailableExitTiles = [];
  private static readonly List<PlanetTile> NeighborTiles = [];

  public static VehicleAssignment assignedSeats = new();

  private static int pawnsBeingAdded;

  /// <summary>
  /// VehicleCaravan is able to be created and embark given list of pawns
  /// </summary>
  /// <param name="pawns"></param>
  /// <returns></returns>
  public static bool AbleToEmbark(List<Pawn> pawns)
  {
    return HasEnoughSpacePawns(pawns) && HasEnoughPawnsToEmbark(pawns);
  }

  /// <summary>
  /// VehicleCaravan can embark directly from Caravan
  /// </summary>
  /// <param name="caravan"></param>
  /// <returns></returns>
  public static bool AbleToEmbark(Caravan caravan)
  {
    List<Pawn> pawns = [];
    foreach (Pawn p in caravan.PawnsListForReading)
    {
      if (p is VehiclePawn vehicle)
      {
        pawns.AddRange(vehicle.AllPawnsAboard);
      }
      pawns.Add(p);
    }
    return AbleToEmbark(pawns);
  }

  /// <summary>
  /// <see cref="CaravanExitMapUtility.BestExitTileToGoTo"/> but implemented for vehicle pathfinding.
  /// </summary>
  public static PlanetTile BestExitTileToGoTo(List<VehicleDef> vehicleDefs,
    PlanetTile destinationTile,
    Map from)
  {
    PlanetTile exitTile = -1;
    using WorldPath worldPath = Find.World.GetComponent<WorldVehiclePathfinder>()
     .FindPath(from.Tile, destinationTile, vehicleDefs);
    if (worldPath.Found && worldPath.NodesLeftCount >= 2)
    {
      exitTile = worldPath.NodesReversed[^2];
    }
    if (!exitTile.Valid)
      return RandomBestExitTileFrom(vehicleDefs, from);

    float shortestDistance = 0;
    PlanetTile nearestExitTile = PlanetTile.Invalid;
    List<PlanetTile> validExitTiles = AvailableExitTilesAt(vehicleDefs, from);
    foreach (PlanetTile validTile in validExitTiles)
    {
      if (validTile == exitTile)
        return validTile;

      float distanceBetween =
        (Find.WorldGrid.GetTileCenter(validTile) - Find.WorldGrid.GetTileCenter(exitTile))
       .MagnitudeHorizontalSquared();
      if (!nearestExitTile.Valid || distanceBetween < shortestDistance)
      {
        nearestExitTile = validTile;
        shortestDistance = distanceBetween;
      }
    }
    return nearestExitTile;
  }

  public static PlanetTile RandomBestExitTileFrom(List<VehicleDef> vehicleDefs, Map map)
  {
    Tile tile = map.TileInfo;
    List<PlanetTile> options = AvailableExitTilesAt(vehicleDefs, map);

    if (options.NullOrEmpty())
      return PlanetTile.Invalid;

    if (tile is not SurfaceTile surfaceTile)
      return options.RandomElement();

    List<SurfaceTile.RoadLink> roads = surfaceTile.Roads;
    int bestRoadIndex = -1;
    for (int i = 0; i < roads.Count; i++)
    {
      SurfaceTile.RoadLink roadLink = roads[i];
      if (options.Contains(roadLink.neighbor) && (bestRoadIndex == -1 ||
        roadLink.road.priority > roads[bestRoadIndex].road.priority))
      {
        bestRoadIndex = i;
      }
    }

    if (bestRoadIndex == -1)
      return options.RandomElement();

    return roads
     .Where(roadLink =>
        options.Contains(roadLink.neighbor) && roadLink.road == roads[bestRoadIndex].road)
     .RandomElement().neighbor;
  }

  public static List<PlanetTile> AvailableExitTilesAt(List<VehicleDef> vehicleDefs, Map map)
  {
    Assert.IsTrue(AvailableExitTiles.Count == 0);
    Assert.IsTrue(NeighborTiles.Count == 0);
    try
    {
      PlanetTile currentTileID = map.Tile;
      RimWorld.Planet.World world = Find.World;
      WorldGrid grid = world.grid;
      grid.GetTileNeighbors(currentTileID, NeighborTiles);
      VehicleDef largestVehicle = vehicleDefs.MaxBy(vehicleDef => vehicleDef.Size.z);
      foreach (PlanetTile tile in NeighborTiles)
      {
        if (vehicleDefs.Exists(vehicleDef =>
          !Find.World.GetComponent<WorldVehiclePathGrid>().Passable(tile, vehicleDef)))
          continue;

        CaravanExitMapUtility.GetExitMapEdges(grid, currentTileID, tile, out Rot4 primary,
          out Rot4 secondary);

        if (primary == Rot4.Invalid || !CellFinderExtended.TryFindRandomEdgeCellWith(
          CellValidator, map, primary, largestVehicle, CellFinder.EdgeRoadChance_Ignore,
          out _))
        {
          if (secondary == Rot4.Invalid || !CellFinderExtended.TryFindRandomEdgeCellWith(
            CellValidator, map,
            secondary, largestVehicle, CellFinder.EdgeRoadChance_Ignore, out _))
          {
            continue;
          }
        }
        AvailableExitTiles.Add(tile);
        continue;

        bool CellValidator(IntVec3 cell)
        {
          foreach (VehicleDef vehicleDef in vehicleDefs)
          {
            if (!cell.Walkable(vehicleDef, map) || cell.Fogged(map))
              return false;
          }
          return true;
        }
      }
      List<PlanetTile> exitTiles = AvailableExitTiles.ToList();
      exitTiles.SortBy(tile => grid.GetHeadingFromTo(currentTileID, tile));
      return exitTiles;
    }
    finally
    {
      AvailableExitTiles.Clear();
      NeighborTiles.Clear();
    }
  }

  /// <summary>
  /// Vehicle has enough room to house all pawns 
  /// </summary>
  /// <param name="pawns"></param>
  /// <returns></returns>
  private static bool HasEnoughSpacePawns(List<Pawn> pawns)
  {
    int num = 0;
    foreach (Pawn p in pawns)
    {
      if (p is VehiclePawn vehicle)
      {
        num += vehicle.TotalSeats;
      }
    }
    return pawns.Count(pawn => pawn is not VehiclePawn) <= num;
  }

  /// <summary>
  /// Vehicle has enough pawns to embark the caravan
  /// </summary>
  /// <param name="pawns"></param>
  /// <returns></returns>
  private static bool HasEnoughPawnsToEmbark(List<Pawn> pawns)
  {
    int num = 0;
    foreach (Pawn p in pawns)
    {
      if (p is VehiclePawn vehicle)
      {
        num += vehicle.PawnCountToOperate;
      }
    }
    return pawns.Count(pawn => pawn is not VehiclePawn) >= num;
  }

  public static IEnumerable<Pawn> AllSendablePawnsInVehicles(Map map,
    bool allowEvenIfDowned = false, bool allowEvenIfInMentalState = false,
    bool allowEvenIfPrisonerNotSecure = false,
    bool allowCapturableDownedPawns = false, bool allowLodgers = false)
  {
    foreach (Pawn mapPawn in map.mapPawns.AllPawnsSpawned)
    {
      if (mapPawn is not VehiclePawn vehicle || vehicle.Faction != Faction.OfPlayer)
        continue;

      foreach (Pawn pawn in vehicle.AllPawnsAboard)
      {
        bool allowDowned = allowEvenIfDowned || !pawn.Downed;
        bool allowMentalState = allowEvenIfInMentalState || !pawn.InMentalState;
        bool allowFaction = pawn.Faction == Faction.OfPlayer || pawn.IsPrisonerOfColony ||
          (allowCapturableDownedPawns && pawn.Downed && !pawn.mindState.WillJoinColonyIfRescued &&
            CaravanUtility.ShouldAutoCapture(pawn, Faction.OfPlayer));
        bool allowQuestLodger = !pawn.IsQuestLodger() || allowLodgers;
        bool allowPrisoner = allowEvenIfPrisonerNotSecure || !pawn.IsPrisoner ||
          pawn.guest.PrisonerIsSecure;
        bool allowLordAssignment = pawn.GetLord() == null ||
          pawn.GetLord().LordJob is LordJob_VoluntarilyJoinable ||
          pawn.GetLord().LordJob.IsCaravanSendable;
        if (allowDowned && allowMentalState && allowFaction && pawn.RaceProps.allowedOnCaravan &&
          !pawn.IsQuestHelper() && allowQuestLodger && allowPrisoner && allowLordAssignment)
        {
          yield return pawn;
        }
      }
    }
  }

  /// <summary>
  /// Spawn DockedBoat object to store boats on World map
  /// </summary>
  /// <param name="caravan"></param>
  public static void StashVehicles(VehicleCaravan caravan)
  {
    Find.WindowStack.Add(new Dialog_StashVehicle(caravan));
  }

  /// <summary>
  /// Board all pawns automatically into assigned seats
  /// </summary>
  public static void BoardAllAssignedPawns()
  {
    foreach (AssignedSeat seat in assignedSeats.AllAssignments.Values)
    {
      seat.Vehicle.TryAddPawn(seat.pawn, seat.handler);
    }
    assignedSeats.Clear();
  }

  /// <summary>
  /// Pawns <paramref name="pawns"/> are able to travel on the world map given their current Vehicle usage
  /// </summary>
  /// <param name="pawns"></param>
  public static bool CanStartCaravan([NotNull] List<Pawn> pawns)
  {
    int seats = 0;
    int pawnCount = 0;
    int prereq = 0;

    foreach (Pawn p in pawns)
    {
      if (p is VehiclePawn vehicle)
      {
        seats += vehicle.SeatsAvailable;
        prereq += vehicle.PawnCountToOperate -
          vehicle.PawnsByHandlingType[HandlingType.Movement].Count;
      }
      else if (p.IsColonistPlayerControlled && !p.Downed && !p.Dead)
      {
        pawnCount++;
      }
    }

    //Not Enough Room, must board all pawns
    bool notEnoughSeats = pawns.Exists(pawn => pawn.IsBoat()) && pawnCount > seats;
    bool prereqNotMet = pawnCount < prereq;
    if (notEnoughSeats)
    {
      Messages.Message("VF_CaravanMustHaveEnoughSpaceOnShip".Translate(),
        MessageTypeDefOf.RejectInput, false);
    }
    if (prereqNotMet)
    {
      Messages.Message("VF_CaravanMustHaveEnoughPawnsToOperate".Translate(prereq),
        MessageTypeDefOf.RejectInput, false);
    }
    return !notEnoughSeats && !prereqNotMet;
  }

  /// <summary>
  /// Pawn is currently forming VehicleCaravan
  /// </summary>
  public static bool IsFormingCaravanShipHelper(Pawn pawn)
  {
    return pawn.GetLord() is { LordJob: LordJob_FormAndSendVehicles };
  }

  /// <summary>
  /// Despawn all <paramref name="pawns"/> and create VehicleCaravan WorldObject on World map
  /// </summary>
  /// <param name="pawns"></param>
  /// <param name="faction"></param>
  /// <param name="exitFromTile"></param>
  /// <param name="directionTile"></param>
  /// <param name="destinationTile"></param>
  /// <param name="sendMessage"></param>
  public static VehicleCaravan ExitMapAndCreateVehicleCaravan(IEnumerable<Pawn> pawns,
    Faction faction, PlanetTile exitFromTile, PlanetTile directionTile, PlanetTile destinationTile,
    bool sendMessage = true)
  {
    if (!GenWorldClosest.TryFindClosestPassableTile(exitFromTile, out exitFromTile))
    {
      Log.Error("Could not find any passable tile for a new caravan.");
      return null;
    }
    if (Find.World.Impassable(directionTile))
    {
      directionTile = exitFromTile;
    }

    List<Pawn> pawnList = [];

    Map map = null;
    foreach (Pawn pawn in pawns)
    {
      if (!pawn.InVehicle())
        pawnList.Add(pawn);
      AddVehicleCaravanExitTaleIfShould(pawn);
      map ??= pawn.MapHeld;
    }
    VehicleCaravan caravan = MakeVehicleCaravan(pawnList, faction, exitFromTile, false);
    Rot4 exitDir = map != null ?
      Find.WorldGrid.GetRotFromTo(exitFromTile, directionTile) :
      Rot4.Invalid;
    foreach (Pawn pawn in pawnList)
    {
      pawn.ExitMap(false, exitDir);
    }
    foreach (Pawn pawn in caravan.PawnsListForReading)
    {
      if (!pawn.IsWorldPawn())
      {
        Find.WorldPawns.PassToWorld(pawn);
      }
    }
    if (map != null)
    {
      map.Parent.Notify_CaravanFormed(caravan);
      map.retainedCaravanData.Notify_CaravanFormed(caravan);
    }
    if (!caravan.vehiclePather.Moving && caravan.Tile != directionTile)
    {
      caravan.vehiclePather.StartPath(directionTile, arrivalAction: null, repathImmediately: true);
      caravan.vehiclePather.nextTileCostLeft /= 2f;
      caravan.vehicleTweener.ResetTweenedPosToRoot();
    }
    if (destinationTile.Valid)
    {
      List<FloatMenuOption> list = FloatMenuMakerWorld.ChoicesAtFor(destinationTile, caravan);
      if (list.NotNullAndAny(floatOpt => !floatOpt.Disabled))
      {
        list.First(floatOpt => !floatOpt.Disabled).action();
      }
      else
      {
        caravan.vehiclePather.StartPath(destinationTile, arrivalAction: null,
          repathImmediately: true);
      }
    }
    if (sendMessage)
    {
      TaggedString taggedString =
        "MessageFormedCaravan".Translate(caravan.Name).CapitalizeFirst();
      if (caravan.vehiclePather.Moving && caravan.vehiclePather.ArrivalAction != null)
      {
        taggedString += " " + "MessageFormedCaravan_Orders".Translate() + ": " +
          caravan.vehiclePather.ArrivalAction.Label + ".";
      }
      Messages.Message(taggedString, caravan, MessageTypeDefOf.TaskCompletion);
    }
    return caravan;
  }

  public static bool OpportunistcallyCreatedAerialVehicle(VehiclePawn vehicle, int tile)
  {
    bool canExit = Find.World.GetComponent<WorldVehiclePathGrid>()
     .Passable(tile, vehicle.VehicleDef);
    if (!canExit && vehicle.GetCachedComp<CompVehicleLauncher>() is not null)
    {
      AerialVehicleInFlight.Create(vehicle, tile);
      if (vehicle.Spawned)
      {
        vehicle.jobs.StopAll();
        vehicle.DeSpawn();
      }
      return true;
    }
    return false;
  }

  /// <summary>
  /// Find random starting tile for VehicleCaravan
  /// </summary>
  public static PlanetTile FindRandomStartingTileBasedOnExitDir(VehiclePawn vehicle, int tileID,
    Rot4 exitDir)
  {
    List<PlanetTile> tileCandidates = [];
    List<PlanetTile> neighbors = [];
    WorldVehiclePathGrid vehiclePathGrid = WorldVehiclePathGrid.Instance;
    Find.WorldGrid.GetTileNeighbors(tileID, neighbors);
    foreach (PlanetTile planetTile in neighbors)
    {
      if (vehiclePathGrid.Passable(planetTile, vehicle.VehicleDef) && (!exitDir.IsValid ||
        !(Find.WorldGrid.GetRotFromTo(tileID, planetTile) != exitDir)))
      {
        tileCandidates.Add(planetTile);
      }
    }
    if (tileCandidates.TryRandomElement(out PlanetTile result))
    {
      return result;
    }
    if (neighbors.Where(pt =>
      {
        if (!vehiclePathGrid.Passable(pt, vehicle.VehicleDef))
        {
          return false;
        }
        Rot4 rotFromTo = Find.WorldGrid.GetRotFromTo(tileID, pt);
        return ((exitDir == Rot4.North || exitDir == Rot4.South) &&
            (rotFromTo == Rot4.East || rotFromTo == Rot4.West)) ||
          ((exitDir == Rot4.East || exitDir == Rot4.West) &&
            (rotFromTo == Rot4.North || rotFromTo == Rot4.South));
      }).TryRandomElement(out result))
    {
      return result;
    }
    if (neighbors.Where(tile => vehiclePathGrid.Passable(tile, vehicle.VehicleDef))
     .TryRandomElement(out result))
    {
      return result;
    }
    return tileID;
  }

  /// <summary>
  /// Create VehicleCaravan on World Map
  /// </summary>
  [MustUseReturnValue]
  public static VehicleCaravan MakeVehicleCaravan(IEnumerable<Pawn> pawns, Faction faction,
    PlanetTile startingTile, bool addToWorldPawnsIfNotAlready)
  {
    if (!startingTile.Valid && addToWorldPawnsIfNotAlready)
    {
      Log.Warning(
        "Tried to create a caravan but chose not to spawn a caravan but pass pawns to world. This can cause bugs because pawns can be discarded.");
    }

    VehicleCaravan caravan =
      (VehicleCaravan)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfVehicles.VehicleCaravan);
    if (startingTile.Valid)
      caravan.Tile = startingTile;
    caravan.SetFaction(faction);
    if (startingTile.Valid)
    {
      Find.WorldObjects.Add(caravan);
    }
    foreach (Pawn pawn in VehicleFilter.FilterOutPassengers(pawns))
    {
      if (pawn.Dead)
      {
        Trace.Fail($"Tried to form caravan with dead pawn {pawn}. Removing...");
        continue;
      }
      if (pawn.GetVehicle() is { } vehicle)
        vehicle.RemovePawn(pawn);
      caravan.AddPawn(pawn, addToWorldPawnsIfNotAlready);
      if (addToWorldPawnsIfNotAlready && !pawn.IsWorldPawn())
      {
        Find.WorldPawns.PassToWorld(pawn);
      }
    }
    caravan.Name = CaravanNameGenerator.GenerateCaravanName(caravan);
    caravan.SetUniqueId(Find.UniqueIDsManager.GetNextCaravanID());
    caravan.PostInit();
    return caravan;
  }

  /// <summary>
  /// Retrieve pawn from map pawns and include pawns inside vehicles
  /// </summary>
  /// <param name="pawns"></param>
  public static List<Pawn> GrabPawnsFromMapPawnsInVehicle(List<Pawn> pawns)
  {
    List<VehiclePawn> vehicles = pawns
     .Where(x => x.Faction == Faction.OfPlayer && x is VehiclePawn).Cast<VehiclePawn>().ToList();
    if (vehicles.NullOrEmpty())
      return pawns.Where(x => x.Faction == Faction.OfPlayer && x.RaceProps.Humanlike).ToList();
    return vehicles.RandomElement().AllCapablePawns;
  }

  /// <summary>
  /// Total capacity left for VehicleCaravan currently forming
  /// </summary>
  /// <param name="lordJob"></param>
  public static float CapacityLeft(LordJob_FormAndSendVehicles lordJob)
  {
    float num = CollectionsMassCalculator.MassUsageTransferables(lordJob.transferables,
      IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload);
    List<ThingCount> tmpCaravanPawns = [];
    foreach (Pawn pawn in lordJob.lord.ownedPawns)
    {
      tmpCaravanPawns.Add(new ThingCount(pawn, pawn.stackCount));
    }
    num += CollectionsMassCalculator.MassUsage(tmpCaravanPawns,
      IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload);
    float num2 = CaravanInfoHelper.Capacity(tmpCaravanPawns);
    tmpCaravanPawns.Clear();
    return num2 - num;
  }

  /// <summary>
  /// Pawns and vehicles available for carrying cargo
  /// </summary>
  /// <param name="pawn"></param>
  private static IEnumerable<Pawn> UsableCandidatesForCargo(Pawn pawn)
  {
    IEnumerable<Pawn> candidates = !pawn.IsFormingCaravan() ?
      pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction) :
      pawn.GetLord().ownedPawns;
    return candidates.Where(candidate => candidate is VehiclePawn);
  }

  /// <summary>
  /// Get vehicle with the most free space that is able to hold cargo 
  /// </summary>
  /// <param name="pawn"></param>
  public static Pawn UsableVehicleWithTheMostFreeSpace(Pawn pawn)
  {
    Pawn carrierPawn = null;
    float num = 0f;
    foreach (Pawn p in UsableCandidatesForCargo(pawn))
    {
      if (p is VehiclePawn && p != pawn &&
        pawn.CanReach(p, PathEndMode.Touch, Danger.Deadly))
      {
        float num2 = MassUtility.FreeSpace(p);
        if (carrierPawn is null || num2 > num)
        {
          carrierPawn = p;
          num = num2;
        }
      }
    }
    return carrierPawn;
  }

  public static void CountPawnsBeingTraded(List<Tradeable> ___cachedTradeables)
  {
    int count = 0;
    foreach (Tradeable tradeable in ___cachedTradeables)
    {
      if (tradeable.AnyThing is Pawn pawn && pawn.RaceProps.Humanlike)
      {
        if (tradeable.ActionToDo == TradeAction.PlayerBuys)
        {
          count += tradeable.CountToTransfer;
        }
        else if (tradeable.ActionToDo == TradeAction.PlayerSells)
        {
          count -= tradeable.CountToTransfer;
        }
      }
    }
    pawnsBeingAdded = count;
  }

  public static bool CanFitInVehicle(AerialVehicleInFlight aerialVehicle)
  {
    if (!TradeSession.Active)
    {
      Log.Warning(
        "Improper use of CanFitInVehicle which should only operate during TradeSessions.");
      return true;
    }
    return aerialVehicle.vehicle.SeatsAvailable - pawnsBeingAdded > 0;
  }

  /// <summary>
  /// Draw items in VehicleCaravan that is currently being formed
  /// </summary>
  /// <param name="inRect"></param>
  /// <param name="curY"></param>
  /// <param name="tmpSingleThing"></param>
  /// <param name="instance"></param>
  public static void DoItemsListForVehicle(Rect inRect, ref float curY,
    ref List<Thing> tmpSingleThing, ITab_Pawn_FormingCaravan instance)
  {
    LordJob_FormAndSendVehicles lordJobFormAndSendCaravanVehicle =
      (LordJob_FormAndSendVehicles)(Find.Selector.SingleSelectedThing as Pawn).GetLord().LordJob;
    Rect position = new(0f, curY, (inRect.width - 10f) / 2f, inRect.height);
    float a = 0f;
    Widgets.BeginGroup(position);
    Widgets.ListSeparator(ref a, position.width, "ItemsToLoad".Translate());
    bool transferingA = false;
    foreach (TransferableOneWay transferableOneWay in lordJobFormAndSendCaravanVehicle
     .transferables)
    {
      if (transferableOneWay.CountToTransfer > 0 && transferableOneWay.HasAnyThing)
      {
        transferingA = true;
        MethodInfo doThingRow =
          AccessTools.Method(type: typeof(ITab_Pawn_FormingCaravan), name: "DoThingRow");
        object[] args =
        [
          transferableOneWay.ThingDef, transferableOneWay.CountToTransfer,
          transferableOneWay.things, position.width, a
        ];
        doThingRow.Invoke(instance, args);
        a = (float)args[4];
      }
    }
    if (!transferingA)
    {
      Widgets.NoneLabel(ref a, position.width);
    }
    Widgets.EndGroup();
    Rect position2 = new((inRect.width + 10f) / 2f, curY, (inRect.width - 10f) / 2f,
      inRect.height);
    float b = 0f;
    Widgets.BeginGroup(position2);
    Widgets.ListSeparator(ref b, position2.width, "LoadedItems".Translate());
    bool transferingB = false;
    foreach (Pawn pawn in lordJobFormAndSendCaravanVehicle.lord.ownedPawns)
    {
      if (!pawn.inventory.UnloadEverything)
      {
        foreach (Thing thing in pawn.inventory.innerContainer)
        {
          transferingB = true;
          tmpSingleThing.Clear();
          tmpSingleThing.Add(thing);
          MethodInfo doThingRow = AccessTools.Method(type: typeof(ITab_Pawn_FormingCaravan),
            name: "DoThingRow");
          object[] args = [thing.def, thing.stackCount, tmpSingleThing, position2.width, b];
          doThingRow.Invoke(instance, args);
          b = (float)args[4];
        }
      }
    }
    if (!transferingB)
    {
      Widgets.NoneLabel(ref b, position.width);
    }
    Widgets.EndGroup();
    curY += Mathf.Max(a, b);
  }

  /// <summary>
  /// Create Tale for VehicleCaravan
  /// </summary>
  /// <param name="pawn"></param>
  public static void AddVehicleCaravanExitTaleIfShould(Pawn pawn)
  {
    Pawn author = pawn;
    if (pawn is VehiclePawn vehicle)
    {
      author = vehicle.AllPawnsAboard.FirstOrFallback(pawn);
    }
    if (author.Spawned && author.IsFreeColonist)
    {
      if (author.Map.IsPlayerHome)
      {
        TaleRecorder.RecordTale(TaleDefOf.CaravanFormed, author);
        return;
      }
      if (GenHostility.AnyHostileActiveThreatToPlayer(author.Map))
      {
        TaleRecorder.RecordTale(TaleDefOf.CaravanFled, author);
      }
    }
  }

  public static Caravan FindCaravanToJoinForAllowingVehicles(Pawn pawn)
  {
    if (pawn.Faction != Faction.OfPlayer && pawn.HostFaction != Faction.OfPlayer)
    {
      return null;
    }
    if (!pawn.Spawned)
    {
      return null;
    }
    if (pawn is VehiclePawn vehicle)
    {
      if (!vehicle.CanReachVehicleMapEdge())
      {
        return null;
      }
    }
    else
    {
      if (!pawn.CanReachMapEdge())
      {
        return null;
      }
    }

    List<PlanetTile> neighbors = [];
    PlanetTile tile = pawn.Map.Tile;
    Find.WorldGrid.GetTileNeighbors(tile, neighbors);
    neighbors.Add(tile);
    foreach (Caravan caravan in Find.WorldObjects.Caravans)
    {
      if (neighbors.Contains(caravan.Tile) && caravan.autoJoinable)
      {
        if (pawn is VehiclePawn vehicle2 && caravan is VehicleCaravan vehicleCaravan)
        {
          if (!vehicleCaravan.ViableForCaravan(vehicle2))
          {
            return null;
          }
        }
        if (pawn.HostFaction == null)
        {
          if (caravan.Faction == pawn.Faction)
          {
            return caravan;
          }
        }
        else if (caravan.Faction == pawn.HostFaction)
        {
          return caravan;
        }
      }
    }
    return null;
  }

  public static AerialVehicleInFlight FindAerialVehicleToJoinForAllowingVehicles(Pawn pawn)
  {
    if (pawn.Faction != Faction.OfPlayer && pawn.HostFaction != Faction.OfPlayer)
    {
      return null;
    }
    if (!pawn.Spawned)
    {
      return null;
    }
    if (pawn is VehiclePawn)
    {
      return null; //Aerial vehicles can't fly together
    }
    else if (!pawn.CanReachMapEdge())
    {
      return null;
    }
    List<AerialVehicleInFlight> aerialVehicles = VehicleWorldObjectsHolder.Instance.AerialVehicles
     .Where(aerialVehicle => aerialVehicle.Tile == pawn.Map.Tile).ToList();

    foreach (AerialVehicleInFlight aerialVehicle in aerialVehicles)
    {
      if (pawn.HostFaction == null)
      {
        if (aerialVehicle.Faction == pawn.Faction)
        {
          return aerialVehicle;
        }
      }
      if (aerialVehicle.Faction == pawn.HostFaction)
      {
        return aerialVehicle;
      }
    }
    return null;
  }
}