using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using Vehicles.Lords;

namespace Vehicles
{
	public static class VehicleCaravanFormingUtility
	{
		public static void StartFormingCaravan(List<Pawn> pawns, List<Pawn> downedPawns, Faction faction, List<TransferableOneWay> transferables,
			IntVec3 meetingPoint, IntVec3 exitSpot, int startingTile, int destinationTile)
		{
			if (startingTile < 0)
			{
				Log.Error("Can't start forming caravan because startingTile is invalid.");
				return;
			}
			if (!pawns.NotNullAndAny())
			{
				Log.Error("Can't start forming caravan with 0 pawns.");
				return;
			}
			if(!pawns.NotNullAndAny(x => x is VehiclePawn))
			{
				Log.Error("Can't start forming vehicle caravan without any vehicles");
				return;
			}

			if (pawns.NotNullAndAny(x => x is VehiclePawn vehicle && vehicle.IsBoat() && (vehicle.movementStatus is VehicleMovementStatus.Online)))
			{

				List<TransferableOneWay> list = transferables;
				list.RemoveAll((TransferableOneWay x) => x.CountToTransfer <= 0 || !x.HasAnyThing || x.AnyThing is Pawn);

				foreach (Pawn p in pawns)
				{
					Lord pLord = p.GetLord();
					if (pLord != null)
					{
						pLord.Notify_PawnLost(p, PawnLostCondition.ForcedToJoinOtherLord, null);
					}
				}
				List<VehiclePawn> vehicles = pawns.Where(p => p.IsBoat()).Cast<VehiclePawn>().ToList();
				List<Pawn> capablePawns = pawns.Where(x => !(x is VehiclePawn) && x.IsColonist && !x.Downed && !x.Dead).ToList();
				List<Pawn> prisoners = pawns.Where(x => !(x is VehiclePawn) && !x.IsColonist && !x.RaceProps.Animal).ToList();
				int seats = 0;
				foreach (VehiclePawn vehicle in vehicles)
				{
					seats += vehicle.SeatsAvailable;
				}
				if ((pawns.Where(p => !p.IsBoat()).ToList().Count + downedPawns.Count) > seats)
				{
					Log.Error("Can't start forming caravan with vehicles(s) selected and not enough seats to house all pawns. Seats: " + seats + " Pawns boarding: " +
						(pawns.Where(x => !(x is VehiclePawn)).ToList().Count + downedPawns.Count));
					return;
				}

				LordJob_FormAndSendVehicles lordJob = new LordJob_FormAndSendVehicles(list, vehicles, capablePawns, downedPawns, prisoners, meetingPoint, exitSpot, startingTile,
					destinationTile, true);
				LordMaker.MakeNewLord(Faction.OfPlayer, lordJob, pawns[0].MapHeld, pawns);
				vehicles.ForEach(v => v.DisembarkAll());

				foreach (Pawn p in pawns)
				{
					if (p.Spawned)
					{
						p.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
					}
				}
			}
			else if(pawns.NotNullAndAny(x => x is VehiclePawn vehicle && vehicle.movementStatus is VehicleMovementStatus.Online))
			{
				List<TransferableOneWay> list = transferables;
				list.RemoveAll((TransferableOneWay x) => x.CountToTransfer <= 0 || !x.HasAnyThing || x.AnyThing is Pawn);

				foreach (Pawn p in pawns)
				{
					Lord lord = p.GetLord();
					if (lord != null)
					{
						lord.Notify_PawnLost(p, PawnLostCondition.ForcedToJoinOtherLord, null);
					}
				}

				List<VehiclePawn> vehicles = pawns.Where(x => x is VehiclePawn).Cast<VehiclePawn>().ToList();
				List<Pawn> capablePawns = pawns.Where(x => !(x is VehiclePawn) && x.IsColonist && !x.Downed && !x.Dead).ToList();
				List<Pawn> prisoners = pawns.Where(x => !(x is VehiclePawn) && !x.IsColonist && !x.RaceProps.Animal).ToList();

				LordJob_FormAndSendVehicles lordJob = new LordJob_FormAndSendVehicles(list, vehicles, capablePawns, downedPawns, prisoners, meetingPoint, exitSpot, startingTile, destinationTile);
				LordMaker.MakeNewLord(Faction.OfPlayer, lordJob, pawns[0].MapHeld, pawns);
				vehicles.ForEach(v => v.DisembarkAll());

				foreach (Pawn p in pawns)
				{
					if (p.Spawned)
					{
						p.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
					}
				}
			}
		}

		public static void RemovePawnFromCaravan(Pawn pawn, Lord lord, bool removeFromDowned = true)
		{
			bool flag = false;
			bool flag2 = false;
			string text = "";
			string textShip = "";
			List<VehiclePawn> vehicles = lord.ownedPawns.FindAll(x => x is VehiclePawn).Cast<VehiclePawn>().ToList();
			foreach (VehiclePawn vehicle in vehicles)
			{
				if (vehicle.AllPawnsAboard.Contains(pawn))
				{
					textShip = "MessagePawnBoardedFormingCaravan".Translate(pawn, vehicle.LabelShort).CapitalizeFirst();
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				foreach (Pawn p in lord.ownedPawns)
				{
					if (p != pawn && CaravanUtility.IsOwner(p, Faction.OfPlayer))
					{
						flag = true;
						break;
					}
				}
			}
			text += flag ? "MessagePawnLostWhileFormingCaravan".Translate(pawn).CapitalizeFirst().ToString() : flag2 ? textShip :
				("MessagePawnLostWhileFormingCaravan".Translate(pawn).CapitalizeFirst().ToString() + "MessagePawnLostWhileFormingCaravan_AllLost".Translate().ToString());
			bool flag3 = true;
			if (!flag2 && !flag)
				CaravanFormingUtility.StopFormingCaravan(lord);
			if (flag)
			{
				pawn.inventory.UnloadEverything = true;
				if (lord.ownedPawns.Contains(pawn))
				{
					lord.Notify_PawnLost(pawn, PawnLostCondition.ForcedByPlayerAction, null);
					flag3 = false;
				}
				LordJob_FormAndSendVehicles lordJob_FormAndSendCaravanVehicle = lord.LordJob as LordJob_FormAndSendVehicles;
				if (lordJob_FormAndSendCaravanVehicle != null && lordJob_FormAndSendCaravanVehicle.downedPawns.Contains(pawn))
				{
					if (!removeFromDowned)
					{
						flag3 = false;
					}
					else
					{
						lordJob_FormAndSendCaravanVehicle.downedPawns.Remove(pawn);
					}
				}
			}
			if (flag3)
			{
				MessageTypeDef msg = flag2 ? MessageTypeDefOf.SilentInput : MessageTypeDefOf.NegativeEvent;
				Messages.Message(text, pawn, msg, true);
			}
		}

		public static Lord GetVehicleAndSendCaravanLord(Pawn p)
		{
			if (CaravanHelper.IsFormingCaravanShipHelper(p))
			{
				return p.GetLord();
			}
			if (p.Spawned)
			{
				List<Lord> lords = p.Map.lordManager.lords;
				foreach (Lord lord in lords)
				{
					LordJob_FormAndSendVehicles lordJob_FormAndSendCaravanShip = lord.LordJob as LordJob_FormAndSendVehicles;
					if (!(lordJob_FormAndSendCaravanShip is null) && lordJob_FormAndSendCaravanShip.downedPawns.Contains(p))
					{
						return lord;
					}
				}
			}
			return null;
		}
	}
}
