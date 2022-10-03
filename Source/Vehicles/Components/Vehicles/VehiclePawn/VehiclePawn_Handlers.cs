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

namespace Vehicles
{
	public partial class VehiclePawn
	{
		//Bills related to boarding VehicleHandler
		public List<Bill_BoardVehicle> bills = new List<Bill_BoardVehicle>();
		public List<VehicleHandler> handlers = new List<VehicleHandler>();

		public bool MovementHandlerAvailable
		{
			get
			{
				foreach (VehicleHandler handler in this.handlers)
				{
					if (handler.role.handlingTypes.NotNullAndAny(h => h == HandlingTypeFlags.Movement) && handler.handlers.Count < handler.role.slotsToOperate)
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
					if (role.handlingTypes.NotNullAndAny(h => h == HandlingTypeFlags.Movement))
					{
						pawnCount += role.slotsToOperate;
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
					if (!handler.role.handlingTypes.NullOrEmpty() && handler.role.handlingTypes.Contains(HandlingTypeFlags.Movement))
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
				foreach (VehicleHandler handler in handlers)
				{
					if (!handler.role.handlingTypes.NullOrEmpty() && handler.role.handlingTypes.Contains(HandlingTypeFlags.Movement) && !handler.RoleFulfilled)
					{
						return false;
					}
				}
				return true;
			}
		}

		public List<Pawn> AllPawnsAboard
		{
			get
			{
				List<Pawn> pawnsOnShip = new List<Pawn>();
				if (handlers != null && handlers.Count > 0)
				{
					foreach (VehicleHandler handler in handlers)
					{
						if (handler.handlers != null && handler.handlers.Count > 0)
						{
							pawnsOnShip.AddRange(handler.handlers);
						}
					}
				}

				return pawnsOnShip;
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
						if (handler.role.handlingTypes.NotNullAndAny(h => h == HandlingTypeFlags.Movement))
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
					if (handler.role.handlingTypes.NotNullAndAny(h => h == HandlingTypeFlags.Cannon))
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
						if (handler.role.handlingTypes.NullOrEmpty())
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
					x += handler.role.slots - handler.handlers.Count;
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
					x += handler.role.slots;
				}
				return x;
			}
		}

		public void AddHandlers(List<VehicleHandler> handlerList)
		{
			if (handlerList.NullOrEmpty()) return;
			foreach (VehicleHandler handler in handlerList)
			{
				VehicleHandler existingHandler = handlers.FirstOrDefault(h => h == handler);
				if (existingHandler != null)
				{
					existingHandler.role.turretIds.AddRange(handler.role.turretIds);
				}
				else
				{
					var handlerPermanent = new VehicleHandler(this, handler.role);
					handlers.Add(handlerPermanent);
				}
			}
		}

		public void RemoveHandlers(List<VehicleHandler> handlerList)
		{
			if (handlerList.NullOrEmpty()) return;
			foreach (VehicleHandler handler in handlerList)
			{
				VehicleHandler vehicleHandler = handlers.FirstOrDefault(h => h == handler);
			}
		}

		public List<VehicleHandler> GetAllHandlersMatch(HandlingTypeFlags? handlingTypeFlag, string cannonKey = "")
		{
			if (handlingTypeFlag is null)
			{
				return handlers.Where(h => h.role.handlingTypes.NullOrEmpty()).ToList();
			}
			return handlers.FindAll(x => x.role.handlingTypes.NotNullAndAny(h => h == handlingTypeFlag) && (handlingTypeFlag != HandlingTypeFlags.Cannon || (!x.role.turretIds.NullOrEmpty() && x.role.turretIds.Contains(cannonKey))));
		}

		public List<VehicleHandler> GetPriorityHandlers(HandlingTypeFlags? handlingTypeFlag = null)
		{
			return handlers.Where(h => h.role.handlingTypes.NotNullAndAny() && (handlingTypeFlag is null || h.role.handlingTypes.Contains(handlingTypeFlag.Value))).ToList();
		}

		public VehicleHandler GetHandlersMatch(Pawn pawn)
		{
			return handlers.FirstOrDefault(x => x.handlers.Contains(pawn));
		}

		public VehicleHandler NextAvailableHandler(Predicate<HandlingTypeFlags> flag = null, bool priorityHandlers = false)
		{
			IEnumerable<VehicleHandler> prioritizedHandlers = priorityHandlers ? handlers.Where(h => h.role.handlingTypes.NotNullAndAny()) : handlers;
			IEnumerable<VehicleHandler> filteredHandlers = flag is null ? prioritizedHandlers : prioritizedHandlers.Where(h => h.role.handlingTypes.NotNullAndAny(ht => flag(ht)));
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

		public void Notify_Boarded(Pawn pawnToBoard, Map map = null)
		{
			if (bills != null && bills.Count > 0)
			{
				Bill_BoardVehicle bill = bills.FirstOrDefault(x => x.pawnToBoard == pawnToBoard);
				if (bill != null)
				{
					if (pawnToBoard.IsWorldPawn())
					{
						Log.Error("Tried boarding vehicle with world pawn. Use Notify_BoardedCaravan instead.");
						return;
					}
					if (pawnToBoard.Spawned)
					{
						pawnToBoard.DeSpawn(DestroyMode.Vanish);
					}
					EventRegistry[VehicleEventDefOf.PawnEntered].ExecuteEvents();
					if (bill.handler.handlers.TryAddOrTransfer(pawnToBoard, true))
					{
						if (pawnToBoard != null)
						{
							if (map != null)
							{
								map.GetCachedMapComponent<VehicleReservationManager>().ReleaseAllClaimedBy(pawnToBoard);
							}
							else
							{
								Map.GetCachedMapComponent<VehicleReservationManager>().ReleaseAllClaimedBy(pawnToBoard);
							}
							Find.WorldPawns.PassToWorld(pawnToBoard, PawnDiscardDecideMode.Decide);
						}
					}
					else if (pawnToBoard.holdingOwner != null)
					{
						pawnToBoard.holdingOwner.TryTransferToContainer(pawnToBoard, bill.handler.handlers);
					}
					bills.Remove(bill);
				}
			}
		}

		public void Notify_BoardedCaravan(Pawn pawnToBoard, ThingOwner handler)
		{
			if (!pawnToBoard.IsWorldPawn())
			{
				Log.Warning("Tried boarding Caravan with non-worldpawn");
			}

			if (pawnToBoard.holdingOwner != null)
			{
				pawnToBoard.holdingOwner.TryTransferToContainer(pawnToBoard, handler);
			}
			else
			{
				handler.TryAdd(pawnToBoard);
			}
		}

		public void RemovePawn(Pawn pawn)
		{
			for (int i = 0; i < handlers.Count; i++)
			{
				VehicleHandler handler = handlers[i];
				if (handler.handlers.Remove(pawn))
				{
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
				EventRegistry[VehicleEventDefOf.PawnExited].ExecuteEvents();
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
			if (!AllPawnsAboard.NotNullAndAny() && outOfFoodNotified)
			{
				outOfFoodNotified = false;
			}
		}

		public void DisembarkAll()
		{
			var pawnsToDisembark = new List<Pawn>(AllPawnsAboard);
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
				foreach (Pawn p in pawnsToDisembark)
				{
					DisembarkPawn(p);
				}
			}
		}

		private void TrySatisfyPawnNeeds()
		{
			if (Spawned || this.IsCaravanMember())
			{
				foreach (Pawn p in AllPawnsAboard)
				{
					TrySatisfyPawnNeeds(p);
				}
			}
		}

		private void TrySatisfyPawnNeeds(Pawn pawn)
		{
			if (pawn.Dead) return;
			List<Need> allNeeds = pawn.needs.AllNeeds;
			int tile = this.IsCaravanMember() ? this.GetCaravan().Tile : Map.Tile;

			for (int i = 0; i < allNeeds.Count; i++)
			{
				Need need = allNeeds[i];
				if (need is Need_Rest)
				{
					if (CaravanNightRestUtility.RestingNowAt(tile) || GetHandlersMatch(pawn).role.handlingTypes.NullOrEmpty())
					{
						TrySatisfyRest(pawn, need as Need_Rest);
					}
				}
				else if (need is Need_Food)
				{
					if (!CaravanNightRestUtility.RestingNowAt(tile))
						TrySatisfyFood(pawn, need as Need_Food);
				}
				else if (need is Need_Chemical)
				{
					if (!CaravanNightRestUtility.RestingNowAt(tile))
						TrySatisfyChemicalNeed(pawn, need as Need_Chemical);
				}
				else if (need is Need_Joy)
				{
					if (!CaravanNightRestUtility.RestingNowAt(tile))
						TrySatisfyJoyNeed(pawn, need as Need_Joy);
				}
				else if (need is Need_Comfort)
				{
					need.CurLevel = 0.5f;
				}
				else if (need is Need_Outdoors)
				{
					need.CurLevel = 0.25f;
				}
			}
		}

		private void TrySatisfyRest(Pawn pawn, Need_Rest rest)
		{
			Building_Bed bed = (Building_Bed)inventory.innerContainer.InnerListForReading.Find(x => x is Building_Bed); // Reserve?
			float restValue = bed is null ? 0.75f : bed.GetStatValue(StatDefOf.BedRestEffectiveness, true);
			restValue *= pawn.GetStatValue(StatDefOf.RestRateMultiplier, true);
			if (restValue > 0)
				rest.CurLevel += 0.0057142857f * restValue;
		}

		private void TrySatisfyFood(Pawn pawn, Need_Food food)
		{
			if (food.CurCategory < HungerCategory.Hungry)
				return;

			if (TryGetBestFood(pawn, out Thing thing, out Pawn owner))
			{
				food.CurLevel += thing.Ingested(pawn, food.NutritionWanted);
				if (thing.Destroyed)
				{
					owner.inventory.innerContainer.Remove(thing);
					if (this.IsCaravanMember())
					{
						this.GetCaravan().RecacheImmobilizedNow();
						this.GetCaravan().RecacheDaysWorthOfFood();
					}
				}
				if (!outOfFoodNotified && !TryGetBestFood(pawn, out _, out Pawn _))
				{
					Messages.Message("ShipOutOfFood".Translate(LabelShort), this, MessageTypeDefOf.NegativeEvent, false);
					outOfFoodNotified = true;
				}
			}
		}

		public bool TryGetBestFood(Pawn forPawn, out Thing food, out Pawn owner)
		{
			List<Thing> list = inventory.innerContainer.InnerListForReading;
			Thing thing = null;
			float num = 0f;
			foreach (Thing foodItem in list)
			{
				if (CanEatForNutrition(foodItem, forPawn))
				{
					float foodScore = CaravanPawnsNeedsUtility.GetFoodScore(foodItem, forPawn);
					if (thing is null || foodScore > num)
					{
						thing = foodItem;
						num = foodScore;
					}
				}
			}
			if (this.IsCaravanMember())
			{
				foreach (Thing foodItem2 in CaravanInventoryUtility.AllInventoryItems(this.GetCaravan()))
				{
					if (CanEatForNutrition(foodItem2, forPawn))
					{
						float foodScore = CaravanPawnsNeedsUtility.GetFoodScore(foodItem2, forPawn);
						if (thing is null || foodScore > num)
						{
							thing = foodItem2;
							num = foodScore;
						}
					}
				}
			}

			if (thing != null)
			{
				food = thing;
				owner = this.IsCaravanMember() ? CaravanInventoryUtility.GetOwnerOf(this.GetCaravan(), thing) : this;
				return true;
			}
			food = null;
			owner = null;
			return false;
		}

		private void TrySatisfyChemicalNeed(Pawn pawn, Need_Chemical chemical)
		{
			if (chemical.CurCategory >= DrugDesireCategory.Satisfied)
			{
				return;
			}

			if (TryGetDrugToSatisfyNeed(pawn, chemical, out Thing drug, out Pawn owner))
			{
				IngestDrug(pawn, drug, owner);
			}
		}

		public void IngestDrug(Pawn pawn, Thing drug, Pawn owner)
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
		public bool TryGetDrugToSatisfyNeed(Pawn forPawn, Need_Chemical chemical, out Thing drug, out Pawn owner)
		{
			Hediff_Addiction addictionHediff = chemical.AddictionHediff;
			if (addictionHediff is null)
			{
				drug = null;
				owner = null;
				return false;
			}
			List<Thing> list = inventory.innerContainer.InnerListForReading;

			Thing thing = null;
			foreach (Thing t in list)
			{
				if (t.IngestibleNow && t.def.IsDrug)
				{
					CompDrug compDrug = t.TryGetComp<CompDrug>();
					if (compDrug != null && compDrug.Props.chemical != null)
					{
						if (compDrug.Props.chemical.addictionHediff == addictionHediff.def)
						{
							if (forPawn.drugs is null || forPawn.drugs.CurrentPolicy[t.def].allowedForAddiction || forPawn.story is null || forPawn.story.traits.DegreeOfTrait(TraitDefOf.DrugDesire) > 0)
							{
								thing = t;
								break;
							}
						}
					}
				}
			}
			if (this.IsCaravanMember())
			{
				foreach (Thing t in CaravanInventoryUtility.AllInventoryItems(this.GetCaravan()))
				{
					if (t.IngestibleNow && t.def.IsDrug)
					{
						CompDrug compDrug = t.TryGetComp<CompDrug>();
						if (compDrug != null && compDrug.Props.chemical != null)
						{
							if (compDrug.Props.chemical.addictionHediff == addictionHediff.def)
							{
								if (forPawn.drugs is null || forPawn.drugs.CurrentPolicy[t.def].allowedForAddiction || forPawn.story is null || forPawn.story.traits.DegreeOfTrait(TraitDefOf.DrugDesire) > 0)
								{
									thing = t;
									break;
								}
							}
						}
					}
				}
			}
			if (thing != null)
			{
				drug = thing;
				owner = this.IsCaravanMember() ? CaravanInventoryUtility.GetOwnerOf(this.GetCaravan(), thing) : this;
				return true;
			}
			drug = null;
			owner = null;
			return false;
		}

		public static bool CanEatForNutrition(Thing item, Pawn forPawn)
		{
			return item.IngestibleNow && item.def.IsNutritionGivingIngestible && forPawn.WillEat(item, null) && item.def.ingestible.preferability > FoodPreferability.NeverForNutrition &&
				(!item.def.IsDrug || !forPawn.IsTeetotaler()) && (!forPawn.RaceProps.Humanlike || forPawn.needs.food.CurCategory >= HungerCategory.Starving || item.def.ingestible.preferability >
				FoodPreferability.DesperateOnlyForHumanlikes);
		}

		private void TrySatisfyJoyNeed(Pawn pawn, Need_Joy joy)
		{
			if (pawn.IsHashIntervalTick(1250))
			{
				float num = vPather.MovingNow ? 4E-05f : 4E-3f; //Incorporate 'shifts'
				if (num <= 0f)
					return;
				num *= 1250f;
				List<JoyKindDef> tmpJoyList = GetAvailableJoyKindsFor(pawn);
				if (!tmpJoyList.TryRandomElementByWeight((JoyKindDef x) => 1f - Mathf.Clamp01(pawn.needs.joy.tolerances[x]), out JoyKindDef joyKind))
					return;
				joy.GainJoy(num, joyKind);
				tmpJoyList.Clear();
			}
		}

		public List<JoyKindDef> GetAvailableJoyKindsFor(Pawn p)
		{
			List<JoyKindDef> outJoyKinds = new List<JoyKindDef>();
			if (!p.needs.joy.tolerances.BoredOf(JoyKindDefOf.Meditative))
				outJoyKinds.Add(JoyKindDefOf.Meditative);
			if (!p.needs.joy.tolerances.BoredOf(JoyKindDefOf.Social))
			{
				int num = 0;
				foreach (Pawn targetpawn in AllPawnsAboard)
				{
					if (!targetpawn.Downed && targetpawn.RaceProps.Humanlike && !targetpawn.InMentalState)
					{
						num++;
					}
				}
				if (num >= 2)
					outJoyKinds.Add(JoyKindDefOf.Social);
			}
			return outJoyKinds;
		}
	}
}
