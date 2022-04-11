using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles.Lords
{
	public class LordToil_PrepareCaravan_GatherCargo : LordToil
	{
		private IntVec3 meetingPoint;

		public LordToil_PrepareCaravan_GatherCargo(IntVec3 meetingPoint)
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
			foreach(Pawn pawn in lord.ownedPawns)
			{
				if (pawn.IsColonist)
				{
					pawn.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.PrepareVehicleCaravan_GatherItems);
				}
				else if(pawn.RaceProps.Animal)
				{
					pawn.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.PrepareCaravan_BoardVehicle);
				}
				else if (pawn is VehiclePawn)
				{
					pawn.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.PrepareCaravan_WaitVehicle);
				}
				else
				{
					pawn.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_Wait, meetingPoint);
				}
			}
		}

		public override void LordToilTick()
		{
			base.LordToilTick();
			if(Find.TickManager.TicksGame % 120 == 0)
			{
				bool flag = true;
				List<Pawn> capablePawns = lord.ownedPawns.Where(x => !x.Downed && !x.Dead).ToList();
				foreach(Pawn pawn in capablePawns)
				{
					if(pawn.IsColonist && pawn.mindState.lastJobTag != JobTag.WaitingForOthersToFinishGatheringItems)
					{
						flag = false;
						break;
					}
				}
				if(flag)
				{
					foreach(Pawn pawn in Map.mapPawns.AllPawnsSpawned)
					{
						if(pawn.CurJob != null && pawn.jobs.curDriver is JobDriver_PrepareVehicleCaravan_GatheringItems && pawn.CurJob.lord == this.lord)
						{
							flag = false;
							break;
						}
					}
				}
				if(flag || (lord.LordJob as LordJob_FormAndSendVehicles).ForceCaravanLeave)
				{
					lord.ReceiveMemo("AllItemsGathered");
				}
			}
		}
	}
}
