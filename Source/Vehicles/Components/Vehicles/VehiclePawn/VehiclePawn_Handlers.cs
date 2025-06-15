using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Verse.AI.Group;

namespace Vehicles;

public partial class VehiclePawn
{
  //Bills related to boarding VehicleHandler
  public List<Bill_BoardVehicle> bills = [];
  public List<VehicleRoleHandler> handlers = [];

  /* ----- Caches for VehicleHandlers ----- */

  public List<VehicleRoleHandler> OccupiedHandlers { get; private set; } = [];

  public List<Pawn> AllPawnsAboard { get; private set; } = [];

  public Dictionary<HandlingType, List<Pawn>> PawnsByHandlingType { get; private set; } = new()
  {
    [HandlingType.None] = [],
    [HandlingType.Movement] = [],
    [HandlingType.Turret] = [],
  };

  /* -------------------------------------- */

  public int PawnCountToOperate
  {
    get
    {
      int pawnCount = 0;
      foreach (VehicleRoleHandler handler in handlers)
      {
        if (handler.role.HandlingTypes.HasFlag(HandlingType.Movement))
        {
          pawnCount += handler.role.SlotsToOperate;
        }
      }
      return pawnCount;
    }
  }

  public int PawnCountToOperateLeft
  {
    get { return PawnCountToOperate - PawnsByHandlingType[HandlingType.Movement].Count; }
  }

  public bool CanMoveWithOperators
  {
    get
    {
      switch (MovementPermissions)
      {
        case VehiclePermissions.Autonomous:
          return true;
        case VehiclePermissions.Immobile:
          return false;
        case VehiclePermissions.DriverNeeded:
        {
          foreach (VehicleRoleHandler handler in handlers)
          {
            if (handler.role.HandlingTypes.HasFlag(HandlingType.Movement) &&
              !handler.RoleFulfilled)
            {
              return false;
            }
          }
          return true;
        }
        default:
          throw new NotImplementedException(nameof(VehiclePermissions));
      }
    }
  }

  public List<Pawn> Passengers => PawnsByHandlingType[HandlingType.None];

  public List<Pawn> AllCapablePawns
  {
    get
    {
      List<Pawn> pawnsOnShip = new List<Pawn>();
      if (!(handlers is null) && handlers.Count > 0)
      {
        foreach (VehicleRoleHandler handler in handlers)
        {
          if (!(handler.thingOwner is null) && handler.thingOwner.Count > 0)
            pawnsOnShip.AddRange(handler.thingOwner);
        }
      }

      pawnsOnShip = pawnsOnShip
       .Where(x => x.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))?.ToList();
      return pawnsOnShip ?? new List<Pawn>() { };
    }
  }

  public int SeatsAvailable
  {
    get
    {
      int x = 0;
      foreach (VehicleRoleHandler handler in handlers)
      {
        x += handler.role.Slots - handler.thingOwner.Count;
      }

      return x;
    }
  }

  public int TotalSeats
  {
    get
    {
      int x = 0;
      foreach (VehicleRoleHandler handler in handlers)
      {
        x += handler.role.Slots;
      }

      return x;
    }
  }

  public void RecachePawnCount()
  {
    PawnsByHandlingType.ClearValueLists();
    OccupiedHandlers.Clear();
    AllPawnsAboard.Clear();

    foreach (VehicleRoleHandler handler in handlers)
    {
      if (handler.thingOwner.Any)
      {
        OccupiedHandlers.Add(handler);
        foreach (Pawn pawn in handler.thingOwner)
        {
          AllPawnsAboard.Add(pawn);

          if (handler.role.HandlingTypes == HandlingType.None)
          {
            PawnsByHandlingType[HandlingType.None].Add(pawn);
          }
          else
          {
            TryAddToCache(pawn, handler.role.HandlingTypes, HandlingType.Movement,
              PawnsByHandlingType);
            TryAddToCache(pawn, handler.role.HandlingTypes, HandlingType.Turret,
              PawnsByHandlingType);
          }
        }
      }
    }
    return;

    static void TryAddToCache(Pawn pawn, HandlingType value, HandlingType mask,
      Dictionary<HandlingType, List<Pawn>> cache)
    {
      if (value.HasFlag(mask))
        cache[mask].Add(pawn);
    }
  }

  public void AddRole(VehicleRole role)
  {
    role.ResolveReferences(VehicleDef);
    handlers.Add(new VehicleRoleHandler(this, role));
    handlers.Sort();
    ResetRenderStatus();
  }

  public void RemoveRole(VehicleRole role)
  {
    // Temporary measure to avoid the destruction of all pawns within the role being removed
    DisembarkAll();
    for (int i = handlers.Count - 1; i >= 0; i--)
    {
      VehicleRoleHandler handler = handlers[i];
      if (handler.role.key == role.key)
      {
        DrawTracker.RemoveRenderer(handler);
        handlers.RemoveAt(i);
      }
    }
  }

  public void RemoveRole(string roleKey)
  {
    // Temporary measure to avoid the destruction of all pawns within the role being removed
    DisembarkAll();
    for (int i = handlers.Count - 1; i >= 0; i--)
    {
      VehicleRoleHandler handler = handlers[i];
      if (handler.role.key == roleKey)
      {
        DrawTracker.RemoveRenderer(handler);
        handlers.RemoveAt(i);
      }
    }
  }

  public VehicleRoleHandler GetHandler(string roleKey)
  {
    foreach (VehicleRoleHandler handler in handlers)
    {
      if (handler.role.key == roleKey)
      {
        return handler;
      }
    }

    return null;
  }

  public List<VehicleRoleHandler> GetAllHandlersMatch(HandlingType? handlingTypeFlag,
    string turretKey = "")
  {
    if (handlingTypeFlag is null)
    {
      return handlers.Where(handler => handler.role.HandlingTypes == HandlingType.None)
       .ToList();
    }

    return handlers.FindAll(x =>
      x.role.HandlingTypes.HasFlag(handlingTypeFlag) &&
      (handlingTypeFlag != HandlingType.Turret || (!x.role.TurretIds.NullOrEmpty() &&
        x.role.TurretIds.Contains(turretKey))));
  }

  public List<VehicleRoleHandler> GetPriorityHandlers(HandlingType? handlingTypeFlag = null)
  {
    return handlers.Where(h =>
      h.role.HandlingTypes > HandlingType.None && (handlingTypeFlag is null ||
        h.role.HandlingTypes.HasFlag(handlingTypeFlag.Value))).ToList();
  }

  public VehicleRoleHandler GetHandlersMatch(Pawn pawn)
  {
    return handlers.FirstOrDefault(x => x.thingOwner.Contains(pawn));
  }

  public VehicleRoleHandler NextAvailableHandler(HandlingType? handlingTypeFlag = null,
    bool priorityHandlers = false)
  {
    foreach (VehicleRoleHandler handler in handlers)
    {
      if (priorityHandlers && handler.role.HandlingTypes == HandlingType.None)
        continue;
      if (handlingTypeFlag != null &&
        !handler.role.HandlingTypes.HasFlag(handlingTypeFlag)) continue;

      if (handler.AreSlotsAvailableAndReservable)
        return handler;
    }

    return null;
  }

  public void GiveLoadJob(Pawn pawn, VehicleRoleHandler handler)
  {
    if (bills != null && bills.Count > 0)
    {
      Bill_BoardVehicle bill = bills.FirstOrDefault(x => x.pawnToBoard == pawn);
      if (!(bill is null))
      {
        bill.handler = handler;
        return;
      }
    }

    bills.Add(new Bill_BoardVehicle(pawn, handler));
  }

  /// <summary>
  /// Pawn with bill has boarded vehicle.
  /// </summary>
  /// <remarks>For boarding vehicles outside of the job system, use <see cref="TryAddPawn"/></remarks>
  /// <param name="pawnToBoard"></param>
  /// <param name="map"></param>
  /// <returns>Pawn successfully boarded the vehicle</returns>
  public bool Notify_Boarded(Pawn pawnToBoard)
  {
    if (bills != null && bills.Count > 0)
    {
      Bill_BoardVehicle bill = bills.FirstOrDefault(x => x.pawnToBoard == pawnToBoard);
      if (bill != null)
      {
        if (pawnToBoard.IsWorldPawn())
        {
          Log.Error("Tried boarding vehicle with world pawn. Use Notify_BoardedCaravan instead.");
          return false;
        }

        if (!TryAddPawn(pawnToBoard, bill.handler))
        {
          return false;
        }

        bills.Remove(bill);
        return true;
      }
    }

    return false;
  }

  public bool TryAddPawn(Pawn pawn)
  {
    if (handlers.NullOrEmpty())
      return false;

    foreach (VehicleRoleHandler handler in handlers)
    {
      if (TryAddPawn(pawn, handler))
      {
        return true;
      }
    }

    return false;
  }

  public bool TryAddPawn(Pawn pawn, VehicleRoleHandler handler)
  {
    // Pawn can be boarded pre-spawned for events such as raids, in this case the map will be null
    // and no reservation checks are needed.
    VehicleReservationManager reservationManager = null;
    if (Spawned)
    {
      reservationManager = Map.GetCachedMapComponent<VehicleReservationManager>();
      if (!reservationManager.ReservedBy<VehicleRoleHandler, VehicleHandlerReservation>(this, pawn,
          handler) && !handler.AreSlotsAvailable)
      {
        //If pawn attempts to board vehicle role which is already full, stop immediately
        return false;
      }
    }

    Assert.IsTrue(handlers.Contains(handler));
    bool result = true;
    if (!handler.AreSlotsAvailable)
    {
      return false;
    }

    if (pawn.Spawned)
    {
      pawn.DeSpawn(DestroyMode.WillReplace);
    }

    if (!handler.thingOwner.TryAddOrTransfer(pawn, canMergeWithExistingStacks: false) &&
      pawn.holdingOwner != null)
    {
      //If can't add to handler and currently has other owner, transfer
      result = pawn.holdingOwner.TryTransferToContainer(pawn, handler.thingOwner);
    }

    reservationManager?.ReleaseAllClaimedBy(pawn);
    if (result)
    {
      EventRegistry?[VehicleEventDefOf.PawnEntered].ExecuteEvents();
    }

    return result;
  }

  public void Notify_BoardedCaravan(Pawn pawnToBoard, ThingOwner handler)
  {
    if (!pawnToBoard.IsWorldPawn())
    {
      Log.Warning("Tried boarding Caravan with non-worldpawn");
      return;
    }

    if (pawnToBoard.holdingOwner != null)
    {
      pawnToBoard.holdingOwner.TryTransferToContainer(pawnToBoard, handler);
    }
    else
    {
      handler.TryAdd(pawnToBoard);
    }

    EventRegistry[VehicleEventDefOf.PawnEntered].ExecuteEvents();
  }

  public void RemovePawn(Pawn pawn)
  {
    foreach (VehicleRoleHandler handler in handlers)
    {
      if (TryRemovePawn(pawn, handler))
        break;
    }
  }

  public bool TryRemovePawn(Pawn pawn, VehicleRoleHandler handler)
  {
    if (handler.thingOwner.Remove(pawn))
    {
      EventRegistry[VehicleEventDefOf.PawnRemoved].ExecuteEvents();
      if (Spawned)
        Map.GetCachedMapComponent<VehicleReservationManager>().ReleaseAllClaimedBy(pawn);
      return true;
    }
    return false;
  }

  public void DisembarkPawn(Pawn pawn)
  {
    Assert.IsTrue(pawn.ParentHolder is VehicleRoleHandler);
    // In Caravan
    if (this.GetVehicleCaravan() is { } caravan)
    {
      RemovePawn(pawn);
      caravan.AddPawn(pawn, true);
      Assert.IsFalse(pawn.IsWorldPawn());
      Find.WorldPawns.PassToWorld(pawn);
      return;
    }

    Assert.IsTrue(Spawned,
      $"Trying to disembark pawn from unspawned vehicle that is not in a caravan. {pawn} would be lost forever.");
    // On Map
    if (!pawn.Spawned)
    {
      CellRect occupiedRect = this.OccupiedRect().ExpandedBy(1);
      IntVec3 loc = Position;
      if (occupiedRect.EdgeCells
       .Where(cell => cell.InBounds(Map) && cell.Standable(Map) &&
          !cell.GetThingList(Map).NotNullAndAny(thing => thing is Pawn))
       .TryRandomElement(out IntVec3 newLoc))
      {
        loc = newLoc;
      }

      GenSpawn.Spawn(pawn, loc, MapHeld);
      if (!loc.Standable(Map))
      {
        pawn.pather.TryRecoverFromUnwalkablePosition(false);
      }

      if (lord is not null)
      {
        pawn.GetLord()?.Notify_PawnLost(pawn, PawnLostCondition.ForcedToJoinOtherLord);
        lord.AddPawn(pawn);
      }
    }

    RemovePawn(pawn);
    EventRegistry[VehicleEventDefOf.PawnExited].ExecuteEvents();
    if (!AllPawnsAboard.NotNullAndAny() && outOfFoodNotified)
    {
      outOfFoodNotified = false;
    }
  }

  public void DisembarkAll()
  {
    if (this.GetVehicleCaravan() is { } caravan)
    {
      foreach (VehicleRoleHandler handler in handlers)
      {
        handler.thingOwner.TryTransferAllToContainer(caravan.pawns, false);
      }
    }
    else if (Spawned)
    {
      using (new EventDisabler<VehicleEventDef>(this))
      {
        for (int i = AllPawnsAboard.Count - 1; i >= 0; i--)
        {
          DisembarkPawn(AllPawnsAboard[i]);
        }
      }
      EventRegistry[VehicleEventDefOf.PawnExited].ExecuteEvents();
      Assert.IsTrue(AllPawnsAboard.Count == 0);
    }
    else
    {
      // Invalid operation but better to send the pawns to world and let the game decide how to
      // handle them
      Log.Warning("Disembarking from vehicle when it is not spawned or in a caravan.");
      foreach (VehicleRoleHandler handler in handlers)
      {
        foreach (Pawn pawn in handler.thingOwner)
        {
          Find.WorldPawns.PassToWorld(pawn);
        }
      }
    }
  }

  internal void TickHandlers()
  {
    // Only need to tick VehicleHandlers with pawns inside them
    foreach (VehicleRoleHandler handler in OccupiedHandlers)
    {
      handler.DoTick();
    }
  }

  public void TrySatisfyPawnNeeds()
  {
    if ((Spawned || this.IsCaravanMember()) && AllPawnsAboard.Count > 0)
    {
      //Not utilizing AllPawnsAboard since VehicleHandler is needed for checks further down the call stack
      for (int i = AllPawnsAboard.Count - 1; i >= 0; i--)
      {
        Pawn pawn = AllPawnsAboard[i];
        TrySatisfyPawnNeeds(pawn);
      }
    }
  }

  public static void TrySatisfyPawnNeeds(Pawn pawn)
  {
    if (pawn.Dead) return;

    List<Need> allNeeds = pawn.needs.AllNeeds;
    VehicleRoleHandler handler = pawn.ParentHolder as VehicleRoleHandler;
    int tile;
    VehicleCaravan vehicleCaravan = pawn.GetVehicleCaravan();
    if (vehicleCaravan != null)
    {
      tile = vehicleCaravan.Tile;
    }
    else if (handler != null)
    {
      tile = handler.vehicle.Map.Tile;
    }
    else if (pawn.Spawned)
    {
      tile = pawn.Map.Tile;
    }
    else
    {
      Log.Error(
        $"Trying to satisfy pawn needs but pawn is not part of VehicleCaravan, vehicle crew, or spawned.");
      return;
    }

    for (int i = 0; i < allNeeds.Count; i++)
    {
      Need need = allNeeds[i];
      switch (need)
      {
        case Need_Rest _:
          if (CaravanNightRestUtility.RestingNowAt(tile) ||
            (vehicleCaravan != null && !vehicleCaravan.vehiclePather.MovingNow))
          {
            TrySatisfyRest(handler, pawn, need as Need_Rest);
          }

        break;
        case Need_Food _:
          if (!CaravanNightRestUtility.RestingNowAt(tile))
          {
            TrySatisfyFood(handler, pawn, need as Need_Food);
          }

        break;
        case Need_Chemical _:
          if (!CaravanNightRestUtility.RestingNowAt(tile))
          {
            TrySatisfyChemicalNeed(handler, pawn, need as Need_Chemical);
          }

        break;
        case Need_Joy _:
          if (!CaravanNightRestUtility.RestingNowAt(tile))
          {
            TrySatisfyJoyNeed(handler, pawn, need as Need_Joy);
          }

        break;
        case Need_Comfort _:
          if (handler != null)
          {
            need.CurLevel = handler.role.Comfort; //TODO - add comfort factor for roles
          }

        break;
        case Need_Outdoors _:
          if (handler == null || handler.role.Exposed)
          {
            need.NeedInterval();
          }

        break;
      }
    }

    if (ModsConfig.BiotechActive && pawn.genes != null)
    {
      Gene_Hemogen firstGeneOfType = pawn.genes.GetFirstGeneOfType<Gene_Hemogen>();
      if (firstGeneOfType != null)
      {
        TrySatisfyHemogenNeed(handler, pawn, firstGeneOfType);
      }
    }

    Pawn_PsychicEntropyTracker psychicEntropy = pawn.psychicEntropy;
    if (psychicEntropy?.Psylink != null)
    {
      TryGainPsyfocus(handler, pawn, psychicEntropy);
    }
  }

  private static void TrySatisfyRest(VehicleRoleHandler handler, Pawn pawn, Need_Rest rest)
  {
    bool cantRestWhileMoving = false;
    VehiclePawn vehicle = handler?.vehicle;
    if (handler != null)
    {
      cantRestWhileMoving = handler.RequiredForMovement &&
        vehicle.VehicleDef.navigationCategory <= NavigationCategory.Opportunistic;
    }

    //Handler not required for movement OR Not Moving (Local) OR Not Moving (World)
    if (!cantRestWhileMoving ||
      (vehicle != null && vehicle.Spawned && !vehicle.vehiclePather.Moving) ||
      (pawn.GetVehicleCaravan() is VehicleCaravan vehicleCaravan &&
        !vehicleCaravan.vehiclePather.MovingNow))
    {
      float restValue =
        StatDefOf.BedRestEffectiveness.valueIfMissing; //TODO - add rest modifier for vehicles
      rest.TickResting(restValue);
    }
  }

  //REDO - Incorporate ChildCare from Biotech (ie. like Caravan_NeedsTracker.TrySatisfyFoodNeed)
  private static void TrySatisfyFood(VehicleRoleHandler handler, Pawn pawn, Need_Food food)
  {
    if (food.CurCategory < HungerCategory.Hungry) return;

    if (TryGetBestFood(pawn, out Thing thing, out Pawn owner))
    {
      food.CurLevel += thing.Ingested(pawn, food.NutritionWanted);
      if (thing.Destroyed)
      {
        owner.inventory.innerContainer.Remove(thing);
        pawn.GetVehicleCaravan()?.RecacheInventory();
      }

      if (handler != null && !handler.vehicle.outOfFoodNotified &&
        !TryGetBestFood(pawn, out _, out Pawn _))
      {
        Messages.Message("VF_OutOfFood".Translate(handler.vehicle.LabelShort), handler.vehicle,
          MessageTypeDefOf.NegativeEvent, false);
        handler.vehicle.outOfFoodNotified = true;
      }
    }
  }

  private static bool TryGetBestFood(Pawn forPawn, out Thing food, out Pawn owner)
  {
    float num = 0f;
    food = null;
    owner = null;

    if (forPawn.GetVehicleCaravan() is VehicleCaravan vehicleCaravan)
    {
      CheckInventory(CaravanInventoryUtility.AllInventoryItems(vehicleCaravan), forPawn, ref food,
        ref num);
      if (food != null)
      {
        owner = CaravanInventoryUtility.GetOwnerOf(vehicleCaravan, food);
      }
    }
    else if (forPawn.ParentHolder is VehicleRoleHandler handler)
    {
      owner = forPawn;
      CheckInventory(forPawn.inventory.innerContainer, forPawn, ref food, ref num);
      if (food is null)
      {
        VehiclePawn vehicle = handler.vehicle;
        owner = vehicle;
        CheckInventory(vehicle.inventory.innerContainer, forPawn, ref food, ref num);
      }
    }

    return food != null;

    static void CheckInventory(IEnumerable<Thing> items, Pawn forPawn, ref Thing food,
      ref float score)
    {
      foreach (Thing potentialFood in items)
      {
        if (CanEatForNutrition(potentialFood, forPawn))
        {
          float foodScore = CaravanPawnsNeedsUtility.GetFoodScore(potentialFood, forPawn);
          if (food is null || foodScore > score)
          {
            food = potentialFood;
            score = foodScore;
          }
        }
      }
    }
  }

  private static void TrySatisfyChemicalNeed(VehicleRoleHandler handler, Pawn pawn,
    Need_Chemical chemical)
  {
    if (chemical.CurCategory >= DrugDesireCategory.Satisfied)
    {
      return;
    }

    if (TryGetDrugToSatisfyNeed(handler, pawn, chemical, out Thing drug, out Pawn owner))
    {
      IngestDrug(pawn, drug, owner);
    }
  }

  private static void IngestDrug(Pawn pawn, Thing drug, Pawn owner)
  {
    float num = drug.Ingested(pawn, 0f);
    Need_Food food = pawn.needs.food;
    if (food != null)
    {
      food.CurLevel += num;
    }

    if (drug.Destroyed)
    {
      owner.inventory.innerContainer.Remove(drug);
    }
  }

  private static bool TryGetDrugToSatisfyNeed(VehicleRoleHandler handler, Pawn forPawn,
    Need_Chemical chemical, out Thing drug, out Pawn owner)
  {
    Hediff_Addiction addictionHediff = chemical.AddictionHediff;
    drug = null;
    owner = null;

    if (addictionHediff is null)
    {
      return false;
    }

    if (forPawn.GetVehicleCaravan() is VehicleCaravan vehicleCaravan)
    {
      CheckInventory(CaravanInventoryUtility.AllInventoryItems(vehicleCaravan), forPawn,
        addictionHediff, ref drug);
      if (drug != null)
      {
        owner = CaravanInventoryUtility.GetOwnerOf(vehicleCaravan, drug);
      }
    }
    else if (handler != null)
    {
      owner = forPawn;
      CheckInventory(forPawn.inventory.innerContainer, forPawn, addictionHediff, ref drug);
      if (drug is null)
      {
        VehiclePawn vehicle = handler.vehicle;
        owner = vehicle;
        CheckInventory(vehicle.inventory.innerContainer, forPawn, addictionHediff, ref drug);
      }
    }

    return drug != null;

    static void CheckInventory(IEnumerable<Thing> items, Pawn forPawn,
      Hediff_Addiction addictionHediff, ref Thing drug)
    {
      foreach (Thing thing in items)
      {
        if (thing.IngestibleNow && thing.def.IsDrug)
        {
          CompDrug compDrug = thing.TryGetComp<CompDrug>();
          if (compDrug != null && compDrug.Props.chemical != null)
          {
            if (compDrug.Props.chemical.addictionHediff == addictionHediff.def)
            {
              if (forPawn.drugs is null ||
                forPawn.drugs.CurrentPolicy[thing.def].allowedForAddiction ||
                forPawn.story is null ||
                forPawn.story.traits.DegreeOfTrait(TraitDefOf.DrugDesire) > 0)
              {
                drug = thing;
                break;
              }
            }
          }
        }
      }
    }
  }

  private static bool CanEatForNutrition(Thing item, Pawn forPawn)
  {
    return item.IngestibleNow && item.def.IsNutritionGivingIngestible &&
      forPawn.WillEat(item, null) &&
      item.def.ingestible.preferability > FoodPreferability.NeverForNutrition &&
      (!item.def.IsDrug || !forPawn.IsTeetotaler()) && (!forPawn.RaceProps.Humanlike ||
        forPawn.needs.food.CurCategory >= HungerCategory.Starving ||
        item.def.ingestible.preferability >
        FoodPreferability.DesperateOnlyForHumanlikes);
  }

  private static void TrySatisfyJoyNeed(VehicleRoleHandler handler, Pawn pawn, Need_Joy joy)
  {
    if (pawn.IsHashIntervalTick(1250))
    {
      float amount = 0; //Incorporate 'shifts'
      bool moving = false;
      if (pawn.GetVehicleCaravan() is VehicleCaravan vehicleCaravan)
      {
        moving = vehicleCaravan.vehiclePather.Moving;
      }
      else if (handler != null)
      {
        moving = handler.vehicle.vehiclePather.Moving;
      }

      amount = moving ? 4E-05f : 4E-3f;
      if (amount > 0f)
      {
        amount *= 1250f;
        List<JoyKindDef> availableJoyKinds = GetAvailableJoyKindsFor(handler, pawn);
        if (!availableJoyKinds.TryRandomElementByWeight(
          (JoyKindDef joyKindDef) => 1f - Mathf.Clamp01(pawn.needs.joy.tolerances[joyKindDef]),
          out JoyKindDef joyKind))
        {
          return;
        }

        joy.GainJoy(amount, joyKind);
      }
    }
  }

  private static List<JoyKindDef> GetAvailableJoyKindsFor(VehicleRoleHandler handler, Pawn forPawn)
  {
    List<JoyKindDef> outJoyKinds = new List<JoyKindDef>();
    if (!forPawn.needs.joy.tolerances.BoredOf(JoyKindDefOf.Meditative))
    {
      outJoyKinds.Add(JoyKindDefOf.Meditative);
    }

    if (!forPawn.needs.joy.tolerances.BoredOf(JoyKindDefOf.Social))
    {
      int pawnCount = 0;
      if (forPawn.GetVehicleCaravan() is VehicleCaravan vehicleCaravan)
      {
        foreach (Pawn otherPawn in vehicleCaravan.PawnsListForReading)
        {
          if (ValidSocialPawn(otherPawn))
          {
            pawnCount++;
          }
        }
      }
      else if (handler != null)
      {
        foreach (Pawn otherPawn in handler.vehicle.AllPawnsAboard)
        {
          if (ValidSocialPawn(otherPawn))
          {
            pawnCount++;
          }
        }
      }

      if (pawnCount >= 2) //2+ since it includes pawn needing the socializing
      {
        outJoyKinds.Add(JoyKindDefOf.Social);
      }
    }

    return outJoyKinds;

    static bool ValidSocialPawn(Pawn targetPawn)
    {
      return !targetPawn.Downed && targetPawn.RaceProps.Humanlike && !targetPawn.InMentalState;
    }
  }

  private static void TrySatisfyHemogenNeed(VehicleRoleHandler handler, Pawn forPawn,
    Gene_Hemogen hemogenGene)
  {
    if (hemogenGene.ShouldConsumeHemogenNow())
    {
      Thing hemogenPack = null;
      Pawn owner = null;
      if (forPawn.GetVehicleCaravan() is VehicleCaravan vehicleCaravan)
      {
        CheckInventory(CaravanInventoryUtility.AllInventoryItems(vehicleCaravan), forPawn,
          ref hemogenPack);
        if (hemogenPack != null)
        {
          owner = CaravanInventoryUtility.GetOwnerOf(vehicleCaravan, hemogenPack);
        }
      }
      else if (handler != null)
      {
        owner = forPawn;
        CheckInventory(forPawn.inventory.innerContainer, forPawn, ref hemogenPack);
        if (hemogenPack is null)
        {
          VehiclePawn vehicle = handler.vehicle;
          owner = vehicle;
          CheckInventory(vehicle.inventory.innerContainer, forPawn, ref hemogenPack);
        }
      }

      if (hemogenPack != null)
      {
        float amount =
          hemogenPack.Ingested(forPawn, hemogenPack.GetStatValue(StatDefOf.Nutrition));
        Pawn_NeedsTracker needs = forPawn.needs;
        if (needs?.food != null)
        {
          forPawn.needs.food.CurLevel += amount;
        }

        if (hemogenPack.Destroyed && owner != null)
        {
          owner.inventory.innerContainer.Remove(hemogenPack);
          forPawn.GetVehicleCaravan()?.RecacheInventory();
        }
      }

      static void CheckInventory(IEnumerable<Thing> items, Pawn forPawn, ref Thing hemogenPack)
      {
        foreach (Thing thing in items)
        {
          if (thing.def == ThingDefOf.HemogenPack)
          {
            hemogenPack = thing;
            return;
          }
        }
      }
    }
  }

  private static void TryGainPsyfocus(VehicleRoleHandler handler, Pawn pawn,
    Pawn_PsychicEntropyTracker tracker)
  {
    if (pawn.GetVehicleCaravan() is VehicleCaravan vehicleCaravan &&
      !vehicleCaravan.vehiclePather.MovingNow && !vehicleCaravan.NightResting)
    {
      tracker.GainPsyfocus(null);
    }
    else if (pawn.GetAerialVehicle() is AerialVehicleInFlight aerialVehicle &&
      !aerialVehicle.Flying)
    {
      tracker.GainPsyfocus(null);
    }
    else if (handler != null && !handler.vehicle.Drafted)
    {
      tracker.GainPsyfocus(null);
    }
  }
}