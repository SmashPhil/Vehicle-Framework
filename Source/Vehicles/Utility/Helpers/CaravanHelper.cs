using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using Vehicles.Defs;
using Vehicles.Lords;

namespace Vehicles
{
	public static class CaravanHelper
	{
		public static Dictionary<Pawn, Pair<VehiclePawn, VehicleHandler>> assignedSeats = new Dictionary<Pawn, Pair<VehiclePawn, VehicleHandler>>();

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
			List<Pawn> pawns = new List<Pawn>();
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
			return pawns.Where(x => !(x is VehiclePawn)).Count() <= num;
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
			return pawns.Where(x => !(x is VehiclePawn)).Count() >= num;
		}

		/// <summary>
		/// Toggle state of caravan between land and sea
		/// </summary>
		/// <param name="caravan"></param>
		/// <param name="dock"></param>
		public static void ToggleDocking(Caravan caravan, bool dock = false)
		{
			if (caravan.HasBoat())
			{
				if (!dock)
				{
					BoardAllCaravanPawns(caravan);
				}
				else
				{
					List<VehiclePawn> ships = caravan.PawnsListForReading.Where(p => p.IsBoat()).Cast<VehiclePawn>().ToList();
					ships.ForEach(b => b.DisembarkAll());
				}
			}
		}

		/// <summary>
		/// Spawn DockedBoat object to store boats on World map
		/// </summary>
		/// <param name="caravan"></param>
		public static void SpawnDockedBoatObject(Caravan caravan)
		{
			if (!caravan.HasBoat())
			{
				Log.Error("Attempted to dock boats with no boats in caravan. This could have serious errors in the future. - Smash Phil");
			}
			ToggleDocking(caravan, true);
			Find.WindowStack.Add(new Dialog_DockBoat(caravan));
		}

		/// <summary>
		/// Directly board all pawns in caravan into Boats in caravan
		/// </summary>
		/// <param name="caravan"></param>
		public static void BoardAllCaravanPawns(Caravan caravan)
		{
			if (!AbleToEmbark(caravan))
			{
				if (caravan.pather.Moving)
				{
					caravan.pather.StopDead();
				}
				Messages.Message("CantMoveDocked".Translate(), MessageTypeDefOf.RejectInput, false);
				return;
			}

			List<Pawn> sailors = caravan.PawnsListForReading.Where(p => !p.IsBoat()).ToList();
			List<VehiclePawn> ships = caravan.PawnsListForReading.Where(p => p.IsBoat()).Cast<VehiclePawn>().ToList();
			foreach(VehiclePawn ship in ships)
			{ 
				for (int j = 0; j < ship.PawnCountToOperate; j++)
				{
					if (sailors.Count <= 0)
					{
						return;
					}
					foreach (VehicleHandler handler in ship.handlers)
					{
						if (handler.AreSlotsAvailable)
						{
							ship.Notify_BoardedCaravan(sailors.Pop(), handler.handlers);
							break;
						}
					}
				}
			}
			if (sailors.Count > 0)
			{
				int x = 0;
				while (sailors.Count > 0)
				{
					VehiclePawn ship = ships[x];
					foreach (VehicleHandler handler in ship.handlers)
					{
						if (handler.AreSlotsAvailable)
						{
							ship.Notify_BoardedCaravan(sailors.Pop(), handler.handlers);
							break;
						}
					}
					x = (x + 2) > ships.Count ? 0 : ++x;
				}
			}
		}

		/// <summary>
		/// Board all pawns automatically into assigned seats
		/// </summary>
		/// <param name="pawns"></param>
		public static void BoardAllAssignedPawns(ref List<Pawn> pawns)
		{
			List<VehiclePawn> vehicles = pawns.Where(p => p is VehiclePawn).Cast<VehiclePawn>().ToList();
			List<Pawn> nonVehicles = pawns.Where(p => !(p is VehiclePawn)).ToList();
			foreach(Pawn pawn in nonVehicles)
			{
				if(assignedSeats.ContainsKey(pawn) && vehicles.Contains(assignedSeats[pawn].First))
				{
					assignedSeats[pawn].First.GiveLoadJob(pawn, assignedSeats[pawn].Second);
					assignedSeats[pawn].First.Notify_Boarded(pawn);
					pawns.Remove(pawn);
				}
			}
		}

		/// <summary>
		/// Pawns <paramref name="pawns"/> are able to travel on the world map given their current Vehicle usage
		/// </summary>
		/// <param name="pawns"></param>
		public static bool CanStartCaravan(List<Pawn> pawns)
		{
			int seats = 0;
			int pawnCount = 0;
			int prereq = 0;
			bool flag = pawns.NotNullAndAny(p => p.IsBoat()); //Ships or No Ships

			foreach (Pawn p in pawns)
			{
				if (p is VehiclePawn vehicle)
				{
					seats += vehicle.SeatsAvailable;
					prereq += vehicle.PawnCountToOperate - vehicle.AllCrewAboard.Count;
				}
				else if (p.IsColonistPlayerControlled && !p.Downed && !p.Dead)
				{
					pawnCount++;
				}
			}

			bool flag2 = flag ? pawnCount > seats : false; //Not Enough Room, must board all pawns
			bool flag3 = pawnCount < prereq;
			if (flag2)
			{
				Messages.Message("CaravanMustHaveEnoughSpaceOnShip".Translate(), MessageTypeDefOf.RejectInput, false);
			}
			if (flag3)
			{
				Messages.Message("CaravanMustHaveEnoughPawnsToOperate".Translate(prereq), MessageTypeDefOf.RejectInput, false);
			}
			return !flag2 && !flag3;
		}
		
		/// <summary>
		/// Pawn is currently forming VehicleCaravan
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public static bool IsFormingCaravanShipHelper(Pawn pawn)
		{
			Lord lord = pawn.GetLord();
			return !(lord is null) && lord.LordJob is LordJob_FormAndSendVehicles;
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
		public static VehicleCaravan ExitMapAndCreateVehicleCaravan(IEnumerable<Pawn> pawns, Faction faction, int exitFromTile, int directionTile, int destinationTile, bool sendMessage = true)
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

			List<Pawn> tmpPawns = new List<Pawn>();
			tmpPawns.AddRange(pawns);

			Map map = null;
			for (int i = 0; i < tmpPawns.Count; i++)
			{
				AddCaravanExitTaleIfShould(tmpPawns[i]);
				map = tmpPawns[i].MapHeld;
				if (map != null)
				{
					break;
				}
			}
			VehicleCaravan caravan = MakeVehicleCaravan(tmpPawns, faction, exitFromTile, false);
			Rot4 exitDir = (map != null) ? Find.WorldGrid.GetRotFromTo(exitFromTile, directionTile) : Rot4.Invalid;
			for (int j = 0; j < tmpPawns.Count; j++)
			{
				tmpPawns[j].ExitMap(false, exitDir);
			}
			List<Pawn> pawnsListForReading = caravan.PawnsListForReading;
			for (int k = 0; k < pawnsListForReading.Count; k++)
			{
				if (!pawnsListForReading[k].IsWorldPawn())
				{
					Find.WorldPawns.PassToWorld(pawnsListForReading[k], PawnDiscardDecideMode.Decide);
				}
			}
			if (map != null)
			{
				map.Parent.Notify_CaravanFormed(caravan);
				map.retainedCaravanData.Notify_CaravanFormed(caravan);
			}
			if (!caravan.vPather.Moving && caravan.Tile != directionTile)
			{
				caravan.vPather.StartPath(directionTile, null, true, true);
				caravan.vPather.nextTileCostLeft /= 2f;
				caravan.tweener.ResetTweenedPosToRoot();
			}
			if (destinationTile != -1)
			{
				List<FloatMenuOption> list = FloatMenuMakerWorld.ChoicesAtFor(destinationTile, caravan);
				if (list.NotNullAndAny((FloatMenuOption x) => !x.Disabled))
				{
					list.First((FloatMenuOption x) => !x.Disabled).action();
				}
				else
				{
					caravan.vPather.StartPath(destinationTile, null, true, true);
				}
			}
			if (sendMessage)
			{
				TaggedString taggedString = "MessageFormedCaravan".Translate(caravan.Name).CapitalizeFirst();
				if (caravan.vPather.Moving && caravan.vPather.ArrivalAction != null)
				{
					taggedString += " " + "MessageFormedCaravan_Orders".Translate() + ": " + caravan.vPather.ArrivalAction.Label + ".";
				}
				Messages.Message(taggedString, caravan, MessageTypeDefOf.TaskCompletion, true);
			}
			return caravan;
		}

		/// <summary>
		/// Create VehicleCaravan on World Map
		/// </summary>
		/// <param name="pawns"></param>
		/// <param name="faction"></param>
		/// <param name="startingTile"></param>
		/// <param name="addToWorldPawnsIfNotAlready"></param>
		/// <returns></returns>
		public static VehicleCaravan MakeVehicleCaravan(IEnumerable<Pawn> pawns, Faction faction, int startingTile, bool addToWorldPawnsIfNotAlready)
		{
			if (startingTile < 0 && addToWorldPawnsIfNotAlready)
			{
				Log.Warning("Tried to create a caravan but chose not to spawn a caravan but pass pawns to world. This can cause bugs because pawns can be discarded.");
			}
			List<Pawn> tmpPawns = new List<Pawn>();
			tmpPawns.AddRange(pawns);

			VehicleCaravan caravan = (VehicleCaravan)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfVehicles.VehicleCaravan);
			if (startingTile >= 0)
			{
				caravan.Tile = startingTile;
			}
			caravan.SetFaction(faction);
			if (startingTile >= 0)
			{
				Find.WorldObjects.Add(caravan);
			}
			for (int i = 0; i < tmpPawns.Count; i++)
			{
				Pawn pawn = tmpPawns[i];
				if (pawn.Dead)
				{
					Log.Warning("Tried to form a caravan with a dead pawn " + pawn);
				}
				else
				{
					caravan.AddPawn(pawn, addToWorldPawnsIfNotAlready);
					if (addToWorldPawnsIfNotAlready && !pawn.IsWorldPawn())
					{
						Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
					}
				}
			}
			caravan.Name = CaravanNameGenerator.GenerateCaravanName(caravan);
			caravan.SetUniqueId(Find.UniqueIDsManager.GetNextCaravanID());
			return caravan;
		}

		/// <summary>
		/// Create Tale for VehicleCaravan
		/// </summary>
		/// <param name="pawn"></param>
		public static void AddCaravanExitTaleIfShould(Pawn pawn)
		{
			if (pawn.Spawned && pawn.IsFreeColonist)
			{
				if (pawn.Map.IsPlayerHome)
				{
					TaleRecorder.RecordTale(TaleDefOf.CaravanFormed, new object[]
					{
						pawn
					});
					return;
				}
				if (GenHostility.AnyHostileActiveThreatToPlayer(pawn.Map, false))
				{
					TaleRecorder.RecordTale(TaleDefOf.CaravanFled, new object[]
					{
						pawn
					});
				}
			}
		}

		/// <summary>
		/// Retrieve pawn from map pawns and include pawns inside vehicles
		/// </summary>
		/// <param name="pawns"></param>
		public static List<Pawn> GrabPawnsFromMapPawnsInVehicle(List<Pawn> pawns)
		{
			List<VehiclePawn> vehicles = pawns.Where(x => x.Faction == Faction.OfPlayer && x is VehiclePawn).Cast<VehiclePawn>().ToList();
			if (vehicles.NullOrEmpty())
			{
				return pawns.Where(x => x.Faction == Faction.OfPlayer && x.RaceProps.Humanlike).ToList();
			}
			return vehicles.RandomElement().AllCapablePawns;
		}

		/// <summary>
		/// Get all pawns onboard vehicles from list of pawns
		/// </summary>
		/// <param name="pawns"></param>
		/// <returns></returns>
		public static List<Pawn> GrabPawnsIfVehicles(List<Pawn> pawns)
		{
			if (pawns is null)
			{
				return null;
			}
			if (!pawns.HasVehicle())
			{
				return pawns;
			}
			List<Pawn> ships = new List<Pawn>();
			foreach (Pawn pawn in pawns)
			{
				if (pawn is VehiclePawn vehicle)
				{
					ships.AddRange(vehicle.AllPawnsAboard);
				}
				else
				{
					ships.Add(pawn);
				}
			}
			return ships;
		}

		/// <summary>
		/// Get all pawns inside vehicles in <paramref name="caravan"/>
		/// </summary>
		/// <param name="caravan"></param>
		public static List<Pawn> ExtractPawnsFromCaravan(Caravan caravan)
		{
			List<Pawn> innerPawns = new List<Pawn>();
			foreach (Pawn pawn in caravan.PawnsListForReading)
			{
				if (pawn is VehiclePawn vehicle)
				{
					innerPawns.AddRange(vehicle.AllPawnsAboard);
				}
			}
			return innerPawns;
		}

		/// <summary>
		/// Total capacity left for VehicleCaravan currently forming
		/// </summary>
		/// <param name="lordJob"></param>
		public static float CapacityLeft(LordJob_FormAndSendVehicles lordJob)
		{
			float num = CollectionsMassCalculator.MassUsageTransferables(lordJob.transferables, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, false, false);
			List<ThingCount> tmpCaravanPawns = new List<ThingCount>();
			for (int i = 0; i < lordJob.lord.ownedPawns.Count; i++)
			{
				Pawn pawn = lordJob.lord.ownedPawns[i];
				tmpCaravanPawns.Add(new ThingCount(pawn, pawn.stackCount));
			}
			num += CollectionsMassCalculator.MassUsage(tmpCaravanPawns, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, false, false);
			float num2 = CollectionsMassCalculator.Capacity(tmpCaravanPawns, null);
			tmpCaravanPawns.Clear();
			return num2 - num;
		}

		/// <summary>
		/// Pawns and vehicles available for carrying cargo
		/// </summary>
		/// <param name="pawn"></param>
		public static IEnumerable<Pawn> UsableCandidatesForCargo(Pawn pawn)
		{
			IEnumerable<Pawn> candidates = (!pawn.IsFormingCaravan()) ? pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction) : pawn.GetLord().ownedPawns;
			candidates = from x in candidates
						 where x is VehiclePawn
						 select x;
			return candidates;
		}

		/// <summary>
		/// Get vehicle with the most free space that is able to hold cargo 
		/// </summary>
		/// <param name="pawn"></param>
		public static Pawn UsableVehicleWithTheMostFreeSpace(Pawn pawn)
		{
			
			Pawn carrierPawn = null;
			float num = 0f;
			foreach(Pawn p in UsableCandidatesForCargo(pawn))
			{
				if(p is VehiclePawn && p != pawn && pawn.CanReach(p, PathEndMode.Touch, Danger.Deadly, false))
				{
					float num2 = MassUtility.FreeSpace(p);
					if(carrierPawn is null || num2 > num)
					{
						carrierPawn = p;
						num = num2;
					}
				}
			}
			return carrierPawn;
		}

		/// <summary>
		/// Draw items in VehicleCaravan that is currently being formed
		/// </summary>
		/// <param name="inRect"></param>
		/// <param name="curY"></param>
		/// <param name="tmpSingleThing"></param>
		/// <param name="instance"></param>
		public static void DoItemsListForVehicle(Rect inRect, ref float curY, ref List<Thing> tmpSingleThing, ITab_Pawn_FormingCaravan instance)
		{
			LordJob_FormAndSendVehicles lordJob_FormAndSendCaravanVehicle = (LordJob_FormAndSendVehicles)(Find.Selector.SingleSelectedThing as Pawn).GetLord().LordJob;
			Rect position = new Rect(0f, curY, (inRect.width - 10f) / 2f, inRect.height);
			float a = 0f;
			GUI.BeginGroup(position);
			Widgets.ListSeparator(ref a, position.width, "ItemsToLoad".Translate());
			bool flag = false;
			foreach (TransferableOneWay transferableOneWay in lordJob_FormAndSendCaravanVehicle.transferables)
			{
				if (transferableOneWay.CountToTransfer > 0 && transferableOneWay.HasAnyThing)
				{
					flag = true;
					MethodInfo doThingRow = AccessTools.Method(type: typeof(ITab_Pawn_FormingCaravan), name: "DoThingRow");
					object[] args = new object[] { transferableOneWay.ThingDef, transferableOneWay.CountToTransfer, transferableOneWay.things, position.width, a };
					doThingRow.Invoke(instance, args);
					a = (float)args[4];
				}
			}
			if (!flag)
			{
				Widgets.NoneLabel(ref a, position.width, null);
			}
			GUI.EndGroup();
			Rect position2 = new Rect((inRect.width + 10f) / 2f, curY, (inRect.width - 10f) / 2f, inRect.height);
			float b = 0f;
			GUI.BeginGroup(position2);
			Widgets.ListSeparator(ref b, position2.width, "LoadedItems".Translate());
			bool flag2 = false;
			foreach (Pawn pawn in lordJob_FormAndSendCaravanVehicle.lord.ownedPawns)
			{
				if (!pawn.inventory.UnloadEverything)
				{
					foreach (Thing thing in pawn.inventory.innerContainer)
					{
						flag2 = true;
						tmpSingleThing.Clear();
						tmpSingleThing.Add(thing);
						MethodInfo doThingRow = AccessTools.Method(type: typeof(ITab_Pawn_FormingCaravan), name: "DoThingRow");
						object[] args = new object[] { thing.def, thing.stackCount, tmpSingleThing, position2.width, b };
						doThingRow.Invoke(instance, args);
						b = (float)args[4];
					}
				}
			}
			if (!flag2)
			{
				Widgets.NoneLabel(ref b, position.width, null);
			}
			GUI.EndGroup();
			curY += Mathf.Max(a, b);
		}
	}
}
