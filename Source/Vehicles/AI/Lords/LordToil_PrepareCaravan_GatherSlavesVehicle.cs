using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using SmashTools;

namespace Vehicles.Lords
{
	public class LordToil_PrepareCaravan_GatherSlavesVehicle : LordToil
	{
		private IntVec3 meetingPoint;

		public LordToil_PrepareCaravan_GatherSlavesVehicle(IntVec3 meetingPoint)
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
			foreach (Pawn p in lord.ownedPawns)
			{
				if(p is VehiclePawn)
				{
					p.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.PrepareCaravan_WaitVehicle);
				}
				else if (!p.RaceProps.Animal && !p.IsColonist)
				{
					p.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.PrepareCaravan_SendSlavesToVehicle, meetingPoint, -1f);
				}
				else
				{
					p.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_Wait, meetingPoint, -1f);
				}
			}
		}

		public override void LordToilTick()
		{
			if (Find.TickManager.TicksGame % 100 == 0)
			{
				List<Pawn> pawns = lord.ownedPawns.Where(v => !(v is VehiclePawn)).ToList();

				if (!pawns.NotNullAndAny(x => !x.IsColonist && x.RaceProps.Humanlike && x.Spawned))
				{
					lord.ReceiveMemo("AllSlavesGathered");
				}
			}
		}
	}
}
