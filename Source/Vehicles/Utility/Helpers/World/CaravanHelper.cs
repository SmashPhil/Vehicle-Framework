using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using HarmonyLib;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public static class CaravanHelper
	{
		private static List<int> availableExitTiles = new List<int>();
		private static List<int> neighborTiles = new List<int>();
		public static Dictionary<Pawn, AssignedSeat> assignedSeats = new Dictionary<Pawn, AssignedSeat>();

		private static int pawnsBeingAdded = 0;

		/// <summary>
		/// Remove all pawns from <see cref="assignedSeats"/> for this vehicle
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="onRemoval">Function call for every pawn removed from assigned seating</param>
		public static void ClearAssignedSeats(VehiclePawn vehicle, Action<Pawn> onRemoval)
		{
			List<Pawn> assignedPawns = assignedSeats.Where(kvp => kvp.Value.vehicle == vehicle).Select(kvp => kvp.Key).ToList();

			if (!assignedPawns.NullOrEmpty())
			{
				foreach (Pawn pawn in assignedPawns)
				{
					assignedSeats.Remove(pawn);
					onRemoval(pawn);
				}
				foreach (Pawn pawn in vehicle.AllPawnsAboard)
				{
					onRemoval(pawn);
				}
			}
		}

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
		/// <see cref="CaravanExitMapUtility.BestExitTileToGoTo(int, Map)"/> but implemented for vehicle pathfinding.
		/// </summary>
		/// <param name="destinationTile"></param>
		/// <param name="from"></param>
		/// <returns></returns>
		public static int BestExitTileToGoTo(List<VehicleDef> vehicleDefs, int destinationTile, Map from)
		{
			int exitTile = -1;
			using WorldPath worldPath = Find.World.GetComponent<WorldVehiclePathfinder>().FindPath(from.Tile, destinationTile, vehicleDefs);
			if (worldPath.Found && worldPath.NodesLeftCount >= 2)
			{
				exitTile = worldPath.NodesReversed[worldPath.NodesReversed.Count - 2];
			}
			if (exitTile == -1)
			{
				return RandomBestExitTileFrom(vehicleDefs, from);
			}
			float shortestDistance = 0;
			int nearestExitTile = -1;
			List<int> validExitTiles = AvailableExitTilesAt(vehicleDefs, from);
			for (int i = 0; i < validExitTiles.Count; i++)
			{
				if (validExitTiles[i] == exitTile)
				{
					return validExitTiles[i];
				}
				float distanceBetween = (Find.WorldGrid.GetTileCenter(validExitTiles[i]) - Find.WorldGrid.GetTileCenter(exitTile)).MagnitudeHorizontalSquared();
				if (nearestExitTile == -1 || distanceBetween < shortestDistance)
				{
					nearestExitTile = validExitTiles[i];
					shortestDistance = distanceBetween;
				}
			}
			return nearestExitTile;
		}

		public static int RandomBestExitTileFrom(List<VehicleDef> vehicleDefs, Map map)
		{
			Tile tile = map.TileInfo;
			List<int> options = AvailableExitTilesAt(vehicleDefs, map);
			if (options.NullOrEmpty())
			{
				return -1;
			}
			List<Tile.RoadLink> roads = tile.Roads;
			if (roads == null)
			{
				return options.RandomElement();
			}
			int bestRoadIndex = -1;
			for (int i = 0; i < roads.Count; i++)
			{
				if (options.Contains(roads[i].neighbor) && (bestRoadIndex == -1 || roads[i].road.priority > roads[bestRoadIndex].road.priority))
				{
					bestRoadIndex = i;
				}
			}
			if (bestRoadIndex == -1)
			{
				return options.RandomElement();
			}
			return roads.Where((Tile.RoadLink rl) => options.Contains(rl.neighbor) && rl.road == roads[bestRoadIndex].road).RandomElement().neighbor;
		}

		public static List<int> AvailableExitTilesAt(List<VehicleDef> vehicleDefs, Map map)
		{
			availableExitTiles.Clear();
			{
				int currentTileID = map.Tile;
				World world = Find.World;
				WorldGrid grid = world.grid;
				grid.GetTileNeighbors(currentTileID, neighborTiles);
				VehicleDef largestVehicle = vehicleDefs.MaxBy(vehicleDef => vehicleDef.Size.z);
				for (int i = 0; i < neighborTiles.Count; i++)
				{
					int tile = neighborTiles[i];
					if (vehicleDefs.All(vehicleDef => Find.World.GetComponent<WorldVehiclePathGrid>().Passable(tile, vehicleDef)))
					{
						CaravanExitMapUtility.GetExitMapEdges(grid, currentTileID, tile, out var primary, out var secondary);
						if ((primary != Rot4.Invalid && CellFinderExtended.TryFindRandomEdgeCellWith((IntVec3 cell) => vehicleDefs.All(vehicleDef => GenGridVehicles.Walkable(cell, vehicleDef, map) && !cell.Fogged(map)), map, primary, largestVehicle, CellFinder.EdgeRoadChance_Ignore, out IntVec3 result) || 
							(secondary != Rot4.Invalid && CellFinderExtended.TryFindRandomEdgeCellWith((IntVec3 cell) => vehicleDefs.All(vehicleDef => GenGridVehicles.Walkable(cell, vehicleDef, map) && !cell.Fogged(map)), map, secondary, largestVehicle, CellFinder.EdgeRoadChance_Ignore, out result))) && !availableExitTiles.Contains(tile))
						{
							availableExitTiles.Add(tile);
						}
					}
				}
				availableExitTiles.SortBy((int x) => grid.GetHeadingFromTo(currentTileID, x));
			}
			return availableExitTiles;
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

		public static IEnumerable<Pawn> AllSendablePawnsInVehicles(Map map, bool allowEvenIfDowned = false, bool allowEvenIfInMentalState = false, bool allowEvenIfPrisonerNotSecure = false, 
			bool allowCapturableDownedPawns = false, bool allowLodgers = false)
		{
			foreach (VehiclePawn vehicle in map.mapPawns.AllPawnsSpawned.Where(pawn => pawn is VehiclePawn && pawn.Faction == Faction.OfPlayer))
			{
				foreach (Pawn pawn in vehicle.AllPawnsAboard)
				{
					bool allowDowned = allowEvenIfDowned || !pawn.Downed;
					bool allowMentalState = allowEvenIfInMentalState || !pawn.InMentalState;
					bool allowFaction = pawn.Faction == Faction.OfPlayer || pawn.IsPrisonerOfColony || (allowCapturableDownedPawns && pawn.Downed && !pawn.mindState.WillJoinColonyIfRescued && CaravanUtility.ShouldAutoCapture(pawn, Faction.OfPlayer));
					bool allowQuestLodger = !pawn.IsQuestLodger() || allowLodgers;
					bool allowPrisoner = allowEvenIfPrisonerNotSecure || !pawn.IsPrisoner || pawn.guest.PrisonerIsSecure;
					bool allowLordAssignment = pawn.GetLord() == null || pawn.GetLord().LordJob is LordJob_VoluntarilyJoinable || pawn.GetLord().LordJob.IsCaravanSendable;
					if (allowDowned && allowMentalState && allowFaction && pawn.RaceProps.allowedOnCaravan && !pawn.IsQuestHelper() && allowQuestLodger && allowPrisoner && allowLordAssignment)
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

		public static Caravan CaravanForMerging(Caravan caravan, List<Caravan> caravans)
		{
			if (caravans.NotNullAndAny(caravan => caravan is VehicleCaravan vehicleCaravan))
			{
				//Prioritize vehicle caravans for merging into
				caravan = caravans.Where(caravan => caravan is VehicleCaravan vehicleCaravan).MaxBy(caravan => caravan.PawnsListForReading.Count);
			}
			return caravan;
		}

		/// <summary>
		/// Board all pawns automatically into assigned seats
		/// </summary>
		/// <param name="pawns"></param>
		public static void BoardAllAssignedPawns(List<Pawn> pawns)
		{
			List<VehiclePawn> vehicles = pawns.Where(p => p is VehiclePawn).Cast<VehiclePawn>().ToList();
			List<Pawn> nonVehicles = pawns.Where(p => !(p is VehiclePawn)).ToList();
			foreach (Pawn pawn in nonVehicles)
			{
				if (assignedSeats.ContainsKey(pawn) && vehicles.Contains(assignedSeats[pawn].vehicle))
				{
					if (pawn.Spawned)
					{
						assignedSeats[pawn].vehicle.TryAddPawn(pawn, assignedSeats[pawn].handler);
					}
					else if (!pawn.IsInVehicle())
					{
						assignedSeats[pawn].vehicle.Notify_BoardedCaravan(pawn, assignedSeats[pawn].handler.handlers);
					}
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
			bool hasBoats = pawns.NotNullAndAny(p => p.IsBoat()); //Ships or No Ships

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

			bool notEnoughSeats = hasBoats ? pawnCount > seats : false; //Not Enough Room, must board all pawns
			bool prereqNotMet = pawnCount < prereq;
			if (notEnoughSeats)
			{
				Messages.Message("VF_CaravanMustHaveEnoughSpaceOnShip".Translate(), MessageTypeDefOf.RejectInput, false);
			}
			if (prereqNotMet)
			{
				Messages.Message("VF_CaravanMustHaveEnoughPawnsToOperate".Translate(prereq), MessageTypeDefOf.RejectInput, false);
			}
			return !notEnoughSeats && !prereqNotMet;
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

			List<Pawn> pawnList = pawns.ToList();

			Map map = null;
			foreach (Pawn pawn in pawnList)
			{
				AddVehicleCaravanExitTaleIfShould(pawn);
				map = pawn.MapHeld;
				if (map != null)
				{
					break;
				}
			}
			VehicleCaravan caravan = MakeVehicleCaravan(pawnList, faction, exitFromTile, false);
			Rot4 exitDir = (map != null) ? Find.WorldGrid.GetRotFromTo(exitFromTile, directionTile) : Rot4.Invalid;
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
				caravan.vehiclePather.StartPath(directionTile, null, true, true);
				caravan.vehiclePather.nextTileCostLeft /= 2f;
				caravan.vehicleTweener.ResetTweenedPosToRoot();
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
					caravan.vehiclePather.StartPath(destinationTile, null, true, true);
				}
			}
			if (sendMessage)
			{
				TaggedString taggedString = "MessageFormedCaravan".Translate(caravan.Name).CapitalizeFirst();
				if (caravan.vehiclePather.Moving && caravan.vehiclePather.ArrivalAction != null)
				{
					taggedString += " " + "MessageFormedCaravan_Orders".Translate() + ": " + caravan.vehiclePather.ArrivalAction.Label + ".";
				}
				Messages.Message(taggedString, caravan, MessageTypeDefOf.TaskCompletion, true);
			}
			return caravan;
		}

		public static bool OpportunistcallyCreatedAerialVehicle(VehiclePawn vehicle, int tile)
		{
			bool canExit = Find.World.GetComponent<WorldVehiclePathGrid>().Passable(tile, vehicle.VehicleDef);
			if (!canExit && vehicle.GetCachedComp<CompVehicleLauncher>() is CompVehicleLauncher)
			{
				AerialVehicleInFlight.Create(vehicle, tile);
				if (vehicle.Spawned)
				{
					vehicle.jobs.StopAll();
					vehicle.DeSpawn(DestroyMode.Vanish);
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// Find random starting tile for VehicleCaravan
		/// </summary>
		/// <param name="tileID"></param>
		/// <param name="exitDir"></param>
		public static int FindRandomStartingTileBasedOnExitDir(VehiclePawn vehicle, int tileID, Rot4 exitDir)
		{
			List<int> tileCandidates = new List<int>();
			List<int> neighbors = new List<int>();
			WorldVehiclePathGrid vehiclePathGrid = WorldVehiclePathGrid.Instance;
			Find.WorldGrid.GetTileNeighbors(tileID, neighbors);
			for (int i = 0; i < neighbors.Count; i++)
			{
				int num = neighbors[i];
				if (vehiclePathGrid.Passable(num, vehicle.VehicleDef) && (!exitDir.IsValid || !(Find.WorldGrid.GetRotFromTo(tileID, num) != exitDir)))
				{
					tileCandidates.Add(num);
				}
			}
			if (tileCandidates.TryRandomElement(out int result))
			{
				return result;
			}
			if (neighbors.Where((int x) =>
				{
					if (!vehiclePathGrid.Passable(x, vehicle.VehicleDef))
					{
						return false;
					}
					Rot4 rotFromTo = Find.WorldGrid.GetRotFromTo(tileID, x);
					return ((exitDir == Rot4.North || exitDir == Rot4.South) && (rotFromTo == Rot4.East || rotFromTo == Rot4.West)) || ((exitDir == Rot4.East || exitDir == Rot4.West) && (rotFromTo == Rot4.North || rotFromTo == Rot4.South));
				}).TryRandomElement(out result))
			{
				return result;
			}
			if (neighbors.Where(tile => vehiclePathGrid.Passable(tile, vehicle.VehicleDef)).TryRandomElement(out result))
			{
				return result;
			}
			return tileID;
		}

		/// <summary>
		/// Create VehicleCaravan on World Map
		/// </summary>
		/// <param name="pawns"></param>
		/// <param name="faction"></param>
		/// <param name="startingTile"></param>
		/// <param name="addToWorldPawnsIfNotAlready"></param>
		public static VehicleCaravan MakeVehicleCaravan(IEnumerable<Pawn> pawns, Faction faction, int startingTile, bool addToWorldPawnsIfNotAlready)
		{
			if (startingTile < 0 && addToWorldPawnsIfNotAlready)
			{
				Log.Warning("Tried to create a caravan but chose not to spawn a caravan but pass pawns to world. This can cause bugs because pawns can be discarded.");
			}
			List<Pawn> pawnsList = pawns.ToList();

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
			for (int i = 0; i < pawnsList.Count; i++)
			{
				Pawn pawn = pawnsList[i];
				if (pawn.Dead)
				{
					Log.Warning("Tried to form a caravan with a dead pawn " + pawn);
				}
				else
				{
					caravan.AddPawn(pawn, addToWorldPawnsIfNotAlready);
					if (addToWorldPawnsIfNotAlready && !pawn.IsWorldPawn())
					{
						Find.WorldPawns.PassToWorld(pawn);
					}
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
			List<VehiclePawn> vehicles = pawns.Where(x => x.Faction == Faction.OfPlayer && x is VehiclePawn).Cast<VehiclePawn>().ToList();
			if (vehicles.NullOrEmpty())
			{
				return pawns.Where(x => x.Faction == Faction.OfPlayer && x.RaceProps.Humanlike).ToList();
			}
			return vehicles.RandomElement().AllCapablePawns;
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
			float num2 = CaravanInfoHelper.Capacity(tmpCaravanPawns, null);
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
				if (p is VehiclePawn && p != pawn && pawn.CanReach(p, PathEndMode.Touch, Danger.Deadly, false))
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
				Log.Warning($"Improper use of CanFitInVehicle which should only operate during TradeSessions.");
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
		public static void DoItemsListForVehicle(Rect inRect, ref float curY, ref List<Thing> tmpSingleThing, ITab_Pawn_FormingCaravan instance)
		{
			LordJob_FormAndSendVehicles lordJob_FormAndSendCaravanVehicle = (LordJob_FormAndSendVehicles)(Find.Selector.SingleSelectedThing as Pawn).GetLord().LordJob;
			Rect position = new Rect(0f, curY, (inRect.width - 10f) / 2f, inRect.height);
			float a = 0f;
			Widgets.BeginGroup(position);
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
			Widgets.EndGroup();
			Rect position2 = new Rect((inRect.width + 10f) / 2f, curY, (inRect.width - 10f) / 2f, inRect.height);
			float b = 0f;
			Widgets.BeginGroup(position2);
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
				if (GenHostility.AnyHostileActiveThreatToPlayer(author.Map, false))
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
			
			List<int> neighbors = new List<int>();
			int tile = pawn.Map.Tile;
			Find.WorldGrid.GetTileNeighbors(tile, neighbors);
			neighbors.Add(tile);
			List<Caravan> caravans = Find.WorldObjects.Caravans;
			for (int i = 0; i < caravans.Count; i++)
			{
				Caravan caravan = caravans[i];
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
			List<AerialVehicleInFlight> aerialVehicles = VehicleWorldObjectsHolder.Instance.AerialVehicles.Where(aerialVehicle => aerialVehicle.Tile == pawn.Map.Tile).ToList();
			
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
}
