using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using SmashTools;
using Vehicles.Defs;

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
			foreach(Pawn p in lord.ownedPawns)
			{
				if(p is VehiclePawn)
				{
					p.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.TravelOrWaitVehicle, exitSpot, -1f)
					{
						locomotion = LocomotionUrgency.Jog
					};
					p.drafter.Drafted = true;
				}
			}
		}

		public override void LordToilTick()
		{
			if(Find.TickManager.TicksGame % 100 == 0)
			{
				GatherAnimalsAndSlavesForShipsUtility.CheckArrived(lord, lord.ownedPawns.Where(p => p is VehiclePawn).ToList(), exitSpot, "ReadyToExitMap", (Pawn p) => true, lord.ownedPawns.NotNullAndAny(b => b.IsBoat()), null);
			}
		}
	}
}
