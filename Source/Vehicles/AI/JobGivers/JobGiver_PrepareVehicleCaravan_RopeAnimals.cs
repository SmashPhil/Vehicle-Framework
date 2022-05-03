using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles
{
	[Obsolete("Incomplete")]
	public class JobGiver_PrepareVehicleCaravan_RopeAnimals : JobGiver_PrepareCaravan_CollectPawns
	{
		protected override JobDef RopeJobDef => JobDefOf_Vehicles.RopeAnimalToVehicle;

		protected override Job TryGiveJob(Pawn pawn)
		{
			LocalTargetInfo vehicleTarget = pawn.mindState.duty.focus;
			VehiclePawn vehicle = vehicleTarget.Pawn as VehiclePawn;
			Pawn ropee;
			if (pawn.roping.IsRopingOthers)
			{
				ropee = pawn.roping.Ropees[0];
			}
			else
			{
				ropee = FindAnimalNeedingRoping(pawn);
			}
			if (ropee == null || vehicle == null)
			{
				return null;
			}
			IntVec3 cell = vehicle.SurroundingCells.FirstOrDefault(cell => cell.IsValid && cell.WalkableBy(ropee.Map, ropee) && cell.WalkableBy(pawn.Map, pawn));
			Job job = JobMaker.MakeJob(RopeJobDef, ropee, vehicleTarget, cell);
			job.lord = pawn.GetLord();
			DecorateJob(job);
			return job;
		}

		protected virtual Pawn FindAnimalNeedingRoping(Pawn pawn)
		{
			foreach (Pawn pawn2 in pawn.GetLord().ownedPawns)
			{
				if (AnimalNeedsGathering(pawn, pawn2))
				{
					return pawn2;
				}
			}
			return null;
		}
	}
}
