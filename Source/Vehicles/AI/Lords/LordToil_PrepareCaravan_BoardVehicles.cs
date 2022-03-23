using System.Collections.Generic;
using System.Linq;
using Vehicles.Defs;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles.Lords
{
	public class LordToil_PrepareCaravan_BoardVehicles : LordToil
	{
		private IntVec3 meetingPoint;

		public LordToil_PrepareCaravan_BoardVehicles(IntVec3 meetingPoint)
		{
			this.meetingPoint = meetingPoint;
		}

		public override float? CustomWakeThreshold
		{
			get
			{
				return new float?(0.5f);
			}
		}

		public override bool AllowRestingInBed
		{
			get
			{
				return false;
			}
		}

		public override void UpdateAllDuties()
		{
			foreach(Pawn p in lord.ownedPawns)
			{
				if(p is VehiclePawn)
				{
					p.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.PrepareCaravan_WaitShip);
				}
				else
				{
					p.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.PrepareCaravan_BoardShip)
					{
						locomotion = LocomotionUrgency.Jog
					};
				}
			}
		}

		public override void LordToilTick()
		{
			if(Find.TickManager.TicksGame % 200 == 0)
			{
				bool flag = true;
				List<Pawn> pawns = new List<Pawn>(lord.ownedPawns.Where(p => !(p is VehiclePawn)));
				foreach (Pawn pawn in pawns)
				{
					var vehicle = (lord.LordJob as LordJob_FormAndSendVehicles).GetVehicleAssigned(pawn);
					if (vehicle.handler != null)
					{
						if(vehicle.vehicle.AllPawnsAboard.Contains(pawn))
						{
							lord.ownedPawns.Remove(pawn);
						}
						else
						{
							flag = false;
						}
					}
				}
				if(flag)
				{
					lord.ReceiveMemo("AllPawnsOnboard");
				}
			}
		}
	}
}
