using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Sound;
using Verse.AI;
using Verse.AI.Group;
using SmashTools;
using SmashTools.Performance;

namespace Vehicles
{
	public partial class VehiclePawn
	{
		//Bills related to boarding VehicleHandler
		public List<Bill_BoardVehicle> bills = new List<Bill_BoardVehicle>();
		public List<VehicleHandler> handlers = new List<VehicleHandler>();

		/* ----- Caches for VehicleHandlers ----- */

		public List<VehicleHandler> OccupiedHandlers { get; private set; } = new List<VehicleHandler>();

		public List<Pawn> AllPawnsAboard { get; private set; } = new List<Pawn>();

		/* -------------------------------------- */

		public bool MovementHandlerAvailable
		{
			get
			{
				foreach (VehicleHandler handler in handlers)
				{
					if (handler.role.HandlingTypes.HasFlag(HandlingTypeFlags.Movement) && handler.handlers.Count < handler.role.SlotsToOperate)
					{
						return false;
					}
				}
				return CompFueledTravel == null || CompFueledTravel.Fuel > 0f;
			}
		}

		public int PawnCountToOperate
		{
			get
			{
				int pawnCount = 0;
				foreach (VehicleRole role in VehicleDef.properties.roles)
				{
					if (role.HandlingTypes.HasFlag(HandlingTypeFlags.Movement))
					{
						pawnCount += role.SlotsToOperate;
					}
				}
				return pawnCount;
			}
		}

		public int PawnCountToOperateLeft
		{
			get
			{
				int pawnsMounted = 0;
				foreach (VehicleHandler handler in handlers)
				{
					if (handler.role.HandlingTypes.HasFlag(HandlingTypeFlags.Movement))
					{
						pawnsMounted += handler.handlers.Count;
					}
				}
				return PawnCountToOperate - pawnsMounted;
			}
		}

		public bool CanMoveWithOperators
		{
			get
			{
				if (MovementPermissions == VehiclePermissions.NoDriverNeeded)
				{
					return true;
				}
				foreach (VehicleHandler handler in handlers)
				{
					if (handler.role.HandlingTypes.HasFlag(HandlingTypeFlags.Movement) && !handler.RoleFulfilled)
					{
						return false;
					}
				}
				return true;
			}
		}

		public List<Pawn> AllCrewAboard
		{
			get
			{
				List<Pawn> crewOnShip = new List<Pawn>();
				if (!(handlers is null))
				{
					foreach (VehicleHandler handler in handlers)
					{
						if (handler.role.HandlingTypes.HasFlag(HandlingTypeFlags.Movement))
						{
							crewOnShip.AddRange(handler.handlers);
						}
					}
				}
				return crewOnShip;
			}
		}

		public List<Pawn> AllCannonCrew
		{
			get
			{
				List<Pawn> weaponCrewOnShip = new List<Pawn>();
				foreach (VehicleHandler handler in handlers)
				{
					if (handler.role.HandlingTypes.HasFlag(HandlingTypeFlags.Turret))
					{
						weaponCrewOnShip.AddRange(handler.handlers);
					}
				}
				return weaponCrewOnShip;
			}
		}

		public List<Pawn> Passengers
		{
			get
			{
				List<Pawn> passengers = new List<Pawn>();
				if (!handlers.NullOrEmpty())
				{
					foreach (VehicleHandler handler in handlers)
					{
						if (handler.role.HandlingTypes == HandlingTypeFlags.None)
						{
							passengers.AddRange(handler.handlers);
						}
					}
				}
				return passengers;
			}
		}

		public List<Pawn> AllCapablePawns
		{
			get
			{
				List<Pawn> pawnsOnShip = new List<Pawn>();
				if (!(handlers is null) && handlers.Count > 0)
				{
					foreach (VehicleHandler handler in handlers)
					{
						if (!(handler.handlers is null) && handler.handlers.Count > 0) pawnsOnShip.AddRange(handler.handlers);
					}
				}
				pawnsOnShip = pawnsOnShip.Where(x => x.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))?.ToList();
				return pawnsOnShip ?? new List<Pawn>() { };
			}
		}

		public int SeatsAvailable
		{
			get
			{
				int x = 0;
				foreach (VehicleHandler handler in handlers)
				{
					x += handler.role.Slots - handler.handlers.Count;
				}
				return x;
			}
		}

		public int TotalSeats
		{
			get
			{
				int x = 0;
				foreach (VehicleHandler handler in handlers)
				{
					x += handler.role.Slots;
				}
				return x;
			}
		}

		public void RecachePawnCount()
		{
			OccupiedHandlers.Clear();
			AllPawnsAboard.Clear();
			foreach (VehicleHandler handler in handlers)
			{
				if (handler.handlers.Any)
				{
					OccupiedHandlers.Add(handler);
					foreach (Pawn pawn in handler.handlers)
					{
						AllPawnsAboard.Add(pawn);
					}
				}
			}
		}

		[Obsolete("Use AddRole instead", true)] //TODO 1.6 - Remove
		public void AddHandlers(List<VehicleHandler> handlerList)
		{
			if (handlerList.NullOrEmpty()) return;
			foreach (VehicleHandler handler in handlerList)
			{
				VehicleHandler existingHandler = handlers.FirstOrDefault(h => h == handler);
				if (existingHandler != null)
				{
					existingHandler.role.TurretIds.AddRange(handler.role.TurretIds);
				}
				else
				{
					var handlerPermanent = new VehicleHandler(this, handler.role);
					handlers.Add(handlerPermanent);
				}
			}
		}

		[Obsolete("Use RemoveHandler instead", true)] //TODO 1.6 - Remove
		public void RemoveHandlers(List<VehicleHandler> handlerList)
		{
			if (handlerList.NullOrEmpty()) return;
			foreach (VehicleHandler handler in handlerList)
			{
				VehicleHandler vehicleHandler = handlers.FirstOrDefault(h => h == handler);
			}
		}

		public void AddRole(VehicleRole role)
		{
			role.ResolveReferences(VehicleDef);
			handlers.Add(new VehicleHandler(this, role));
		}

		public void RemoveRole(VehicleRole role)
		{
			DisembarkAll(); //Temporary measure to avoid the destruction of all pawns within the role being removed
			for (int i = handlers.Count - 1; i >= 0; i--)
			{
				VehicleHandler handler = handlers[i];
				if (handler.role.key == role.key)
				{
					handlers.RemoveAt(i);
				}
			}
		}

		public void RemoveRole(string roleKey)
		{
			DisembarkAll(); //Temporary measure to avoid the destruction of all pawns within the role being removed
			for (int i = handlers.Count - 1; i >= 0; i--)
			{
				VehicleHandler handler = handlers[i];
				if (handler.role.key == roleKey)
				{
					handlers.RemoveAt(i);
				}
			}
		}

		public VehicleHandler GetHandler(string roleKey)
		{
			foreach (VehicleHandler handler in handlers)
			{
				if (handler.role.key == roleKey)
				{
					return handler;
				}
			}
			return null;
		}

		public List<VehicleHandler> GetAllHandlersMatch(HandlingTypeFlags? handlingTypeFlag, string turretKey = "")
		{
			if (handlingTypeFlag is null)
			{
				return handlers.Where(handler => handler.role.HandlingTypes == HandlingTypeFlags.None).ToList();
			}
			return handlers.FindAll(x => x.role.HandlingTypes.HasFlag(handlingTypeFlag) && (handlingTypeFlag != HandlingTypeFlags.Turret || (!x.role.TurretIds.NullOrEmpty() && x.role.TurretIds.Contains(turretKey))));
		}

		public List<VehicleHandler> GetPriorityHandlers(HandlingTypeFlags? handlingTypeFlag = null)
		{
			return handlers.Where(h => h.role.HandlingTypes > HandlingTypeFlags.None && (handlingTypeFlag is null || h.role.HandlingTypes.HasFlag(handlingTypeFlag.Value))).ToList();
		}

		public VehicleHandler GetHandlersMatch(Pawn pawn)
		{
			return handlers.FirstOrDefault(x => x.handlers.Contains(pawn));
		}

		//REDO - cleanup
		public VehicleHandler NextAvailableHandler(HandlingTypeFlags? handlingTypeFlag = null, bool priorityHandlers = false)
		{
			IEnumerable<VehicleHandler> prioritizedHandlers = priorityHandlers ? handlers.Where(h => h.role.HandlingTypes > HandlingTypeFlags.None) : handlers;
			IEnumerable<VehicleHandler> filteredHandlers = handlingTypeFlag is null ? prioritizedHandlers : prioritizedHandlers.Where(h => h.role.HandlingTypes.HasFlag(handlingTypeFlag));
			foreach (VehicleHandler handler in filteredHandlers)
			{
				if (handler.AreSlotsAvailable)
				{
					return handler;
				}
			}
			return null;
		}

		public void GiveLoadJob(Pawn pawn, VehicleHandler handler)
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

		public bool Notify_Boarded(Pawn pawnToBoard, Map map = null)
		{
			if (bills != null && bills.Count > 0)
			{
				Bill_BoardVehicle bill = bills.FirstOrDefault(x => x.pawnToBoard == pawnToBoard);
				if (bill != null)
				{
					map ??= Map;
					if (pawnToBoard.IsWorldPawn())
					{
						Log.Error("Tried boarding vehicle with world pawn. Use Notify_BoardedCaravan instead.");
						return false;
					}
					VehicleReservationManager reservationManager = map.GetCachedMapComponent<VehicleReservationManager>();
					if (!reservationManager.ReservedBy<VehicleHandler, VehicleHandlerReservation>(this, pawnToBoard, bill.handler) && !bill.handler.AreSlotsAvailable)
					{
						bool canReserve = Map.GetCachedMapComponent<VehicleReservationManager>().CanReserve<VehicleHandler, VehicleHandlerReservation>(this, null, bill.handler);
						return false; //If pawn attempts to board vehicle role which is already full, stop immediately
					}
					if (pawnToBoard.Spawned)
					{
						pawnToBoard.DeSpawn(DestroyMode.WillReplace);
					}
					if (!bill.handler.handlers.TryAddOrTransfer(pawnToBoard, canMergeWithExistingStacks: false) && pawnToBoard.holdingOwner != null)
					{
						//If can't add to handler and currently has other owner, transfer
						pawnToBoard.holdingOwner.TryTransferToContainer(pawnToBoard, bill.handler.handlers);
					}
					reservationManager.ReleaseAllClaimedBy(pawnToBoard);
					bills.Remove(bill);
					EventRegistry[VehicleEventDefOf.PawnEntered].ExecuteEvents();
					return true;
				}
			}
			return false;
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

		public void RemoveAllPawns()
		{
			foreach (Pawn pawn in AllPawnsAboard.ToList())
			{
				RemovePawn(pawn);
			}
		}

		public void RemovePawn(Pawn pawn)
		{
			for (int i = 0; i < handlers.Count; i++)
			{
				VehicleHandler handler = handlers[i];
				if (handler.handlers.Remove(pawn))
				{
					EventRegistry[VehicleEventDefOf.PawnRemoved].ExecuteEvents();
					if (Spawned)
					{
						Map.GetCachedMapComponent<VehicleReservationManager>().ReleaseAllClaimedBy(pawn);
					}
					return;
				}
			}
		}

		public void DisembarkPawn(Pawn pawn)
		{
			if (!pawn.Spawned)
			{
				CellRect occupiedRect = this.OccupiedRect().ExpandedBy(1);
				IntVec3 loc = Position;
				if (occupiedRect.EdgeCells.Where(c => GenGrid.InBounds(c, Map) && GenGrid.Standable(c, Map) && !c.GetThingList(Map).NotNullAndAny(t => t is Pawn)).TryRandomElement(out IntVec3 newLoc))
				{
					loc = newLoc;
				}
				GenSpawn.Spawn(pawn, loc, MapHeld);
				if (!GenGrid.Standable(loc, Map))
				{
					pawn.pather.TryRecoverFromUnwalkablePosition(false);
				}
				if (this.GetLord() is Lord lord)
				{
					if (pawn.GetLord() is Lord otherLord)
					{
						otherLord.Notify_PawnLost(pawn, PawnLostCondition.ForcedToJoinOtherLord);
					}
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
			List<Pawn> pawnsToDisembark = new List<Pawn>(AllPawnsAboard);
			if (!(pawnsToDisembark is null) && pawnsToDisembark.Count > 0)
			{
				if (this.GetCaravan() != null && !Spawned)
				{
					List<VehicleHandler> handlerList = handlers;
					for (int i = 0; i < handlerList.Count; i++)
					{
						VehicleHandler handler = handlerList[i];
						handler.handlers.TryTransferAllToContainer(this.GetCaravan().pawns, false);
					}
					return;
				}
				foreach (Pawn pawn in pawnsToDisembark)
				{
					DisembarkPawn(pawn);
				}
			}
		}

		internal void TickHandlers()
		{
			//Only need to tick VehicleHandlers with pawns inside them
			for (int i = 0; i < OccupiedHandlers.Count; i++)
			{
				OccupiedHandlers[i].Tick();
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
			VehicleHandler handler = pawn.ParentHolder as VehicleHandler;
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
				Log.Error($"Trying to satisfy pawn needs but pawn is not part of VehicleCaravan, vehicle crew, or spawned.");
				return;
			}
			for (int i = 0; i < allNeeds.Count; i++)
			{
				Need need = allNeeds[i];
				switch (need)
				{
					case Need_Rest _:
						if (CaravanNightRestUtility.RestingNowAt(tile) || (vehicleCaravan != null && !vehicleCaravan.vehiclePather.MovingNow))
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
						need.CurLevel = handler.role.Comfort; //TODO - add comfort factor for roles
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

		private static void TrySatisfyRest(VehicleHandler handler, Pawn pawn, Need_Rest rest)
		{
			bool cantRestWhileMoving = false;
			VehiclePawn vehicle = handler?.vehicle;
			if (handler != null)
			{
				cantRestWhileMoving = handler.RequiredForMovement && vehicle.VehicleDef.navigationCategory <= NavigationCategory.Opportunistic;
			}
			//Handler not required for movement OR Not Moving (Local) OR Not Moving (World)
			if (!cantRestWhileMoving || (vehicle != null && vehicle.Spawned && !vehicle.vehiclePather.Moving) || (pawn.GetVehicleCaravan() is VehicleCaravan vehicleCaravan && !vehicleCaravan.vehiclePather.MovingNow))
			{
				float restValue = StatDefOf.BedRestEffectiveness.valueIfMissing; //TODO - add rest modifier for vehicles
				rest.TickResting(restValue);
			}
		}

		//REDO - Incorporate ChildCare from Biotech (ie. like Caravan_NeedsTracker.TrySatisfyFoodNeed)
		private static void TrySatisfyFood(VehicleHandler handler, Pawn pawn, Need_Food food)
		{
			if (food.CurCategory < HungerCategory.Hungry) return;

			if (TryGetBestFood(pawn, out Thing thing, out Pawn owner))
			{
				food.CurLevel += thing.Ingested(pawn, food.NutritionWanted);
				if (thing.Destroyed)
				{
					owner.inventory.innerContainer.Remove(thing);
					if (pawn.GetVehicleCaravan() is VehicleCaravan vehicleCaravan)
					{
						vehicleCaravan.RecacheImmobilizedNow();
						vehicleCaravan.RecacheDaysWorthOfFood();
					}
				}
				if (handler != null && !handler.vehicle.outOfFoodNotified && !TryGetBestFood(pawn, out _, out Pawn _))
				{
					Messages.Message("VF_OutOfFood".Translate(handler.vehicle.LabelShort), handler.vehicle, MessageTypeDefOf.NegativeEvent, false);
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
				CheckInventory(CaravanInventoryUtility.AllInventoryItems(vehicleCaravan), forPawn, ref food, ref num);
				if (food != null)
				{
					owner = CaravanInventoryUtility.GetOwnerOf(vehicleCaravan, food);
				}
			}
			else if (forPawn.ParentHolder is VehicleHandler handler)
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

			static void CheckInventory(IEnumerable<Thing> items, Pawn forPawn, ref Thing food, ref float score)
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

		private static void TrySatisfyChemicalNeed(VehicleHandler handler, Pawn pawn, Need_Chemical chemical)
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

		private static bool TryGetDrugToSatisfyNeed(VehicleHandler handler, Pawn forPawn, Need_Chemical chemical, out Thing drug, out Pawn owner)
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
				CheckInventory(CaravanInventoryUtility.AllInventoryItems(vehicleCaravan), forPawn, addictionHediff, ref drug);
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

			static void CheckInventory(IEnumerable<Thing> items, Pawn forPawn, Hediff_Addiction addictionHediff, ref Thing drug)
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
								if (forPawn.drugs is null || forPawn.drugs.CurrentPolicy[thing.def].allowedForAddiction || forPawn.story is null || forPawn.story.traits.DegreeOfTrait(TraitDefOf.DrugDesire) > 0)
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
			return item.IngestibleNow && item.def.IsNutritionGivingIngestible && forPawn.WillEat(item, null) && item.def.ingestible.preferability > FoodPreferability.NeverForNutrition &&
				(!item.def.IsDrug || !forPawn.IsTeetotaler()) && (!forPawn.RaceProps.Humanlike || forPawn.needs.food.CurCategory >= HungerCategory.Starving || item.def.ingestible.preferability >
				FoodPreferability.DesperateOnlyForHumanlikes);
		}

		private static void TrySatisfyJoyNeed(VehicleHandler handler, Pawn pawn, Need_Joy joy)
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
					if (!availableJoyKinds.TryRandomElementByWeight((JoyKindDef joyKindDef) => 1f - Mathf.Clamp01(pawn.needs.joy.tolerances[joyKindDef]), out JoyKindDef joyKind))
					{
						return;
					}
					joy.GainJoy(amount, joyKind);
				}
			}
		}

		private static List<JoyKindDef> GetAvailableJoyKindsFor(VehicleHandler handler, Pawn forPawn)
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

		private static void TrySatisfyHemogenNeed(VehicleHandler handler, Pawn forPawn, Gene_Hemogen hemogenGene)
		{
			if (hemogenGene.ShouldConsumeHemogenNow())
			{
				Thing hemogenPack = null;
				Pawn owner = null;
				if (forPawn.GetVehicleCaravan() is VehicleCaravan vehicleCaravan)
				{
					CheckInventory(CaravanInventoryUtility.AllInventoryItems(vehicleCaravan), forPawn, ref hemogenPack);
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
					float amount = hemogenPack.Ingested(forPawn, hemogenPack.GetStatValue(StatDefOf.Nutrition));
					Pawn_NeedsTracker needs = forPawn.needs;
					if (needs?.food != null)
					{
						forPawn.needs.food.CurLevel += amount;
					}
					if (hemogenPack.Destroyed && owner != null)
					{
						owner.inventory.innerContainer.Remove(hemogenPack);

						if (forPawn.GetVehicleCaravan() is Caravan caravan)
						{
							caravan.RecacheImmobilizedNow();
							caravan.RecacheDaysWorthOfFood();
						}
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

		private static void TryGainPsyfocus(VehicleHandler handler, Pawn pawn, Pawn_PsychicEntropyTracker tracker)
		{
			if (pawn.GetVehicleCaravan() is VehicleCaravan vehicleCaravan && !vehicleCaravan.vehiclePather.MovingNow && !vehicleCaravan.NightResting)
			{
				tracker.GainPsyfocus(null);
			}
			else if (pawn.GetAerialVehicle() is AerialVehicleInFlight aerialVehicle && !aerialVehicle.Flying)
			{
				tracker.GainPsyfocus(null);
			}
			else if (handler != null && !handler.vehicle.Drafted)
			{
				tracker.GainPsyfocus(null);
			}
		}
	}
}
