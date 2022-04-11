using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;
using SmashTools;
using Vehicles;

namespace Vehicles.Lords
{
	public class LordToil_PrepareCaravan_LeaveWithVehicles : LordToil
	{
		private IntVec3 exitSpot;

		public LordToil_PrepareCaravan_LeaveWithVehicles(IntVec3 exitSpot)
		{
			this.exitSpot = exitSpot;
		}

		public override bool AllowSatisfyLongNeeds => false;

		public override float? CustomWakeThreshold => new float?(0.5f);

		public override bool AllowRestingInBed => false;

		public override bool AllowSelfTend => false;

		public override void UpdateAllDuties()
		{
			RotatingList<VehiclePawn> vehicles = lord.ownedPawns.Where(p => p is VehiclePawn).Cast<VehiclePawn>().ToRotatingList();

			foreach (Pawn pawn in lord.ownedPawns)
			{
				if (pawn is VehiclePawn)
				{
					pawn.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.TravelOrWaitVehicle, exitSpot, -1f)
					{
						locomotion = LocomotionUrgency.Jog
					};
					pawn.drafter.Drafted = true;
				}
				else
				{
					VehiclePawn vehicle = vehicles.Next;
					pawn.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.FollowVehicle, vehicle, vehicle.VehicleDef.Size.z * 1.5f)
					{
						locomotion = LocomotionUrgency.Jog
					};
				}
			}
		}

		public override void LordToilTick()
		{
			if (Find.TickManager.TicksGame % 100 == 0)
			{
				ExitMapUtility.CheckArrived(lord, lord.ownedPawns, exitSpot, "ReadyToExitMap", (Pawn p) => true, lord.ownedPawns.NotNullAndAny(b => b.IsBoat()), null);
			}
		}
	}
}
