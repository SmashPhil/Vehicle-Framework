using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public static class CombatPositionFinder
	{
		public static bool TryFindCastPosition(VehiclePawn vehicle, Thing target, float maxDist, bool wantsCover, out IntVec3 dest)
		{
			dest = vehicle.Position;
			bool found = false;

			found = vehicle.Position.InHorDistOf(target.Position, maxDist) &&
				GenSight.LineOfSightToThing(vehicle.Position, target, vehicle.Map, skipFirstCell: true);
			return found;
			//IntVec3 vehiclePos = vehicle.Position;
			//IntVec3 targetPos = target.Position;
			//ByteGrid avoidGrid = vehicle.GetAvoidGrid(false);
			//CellRect cellRect = CellRect.WholeMap(vehicle.Map);
			//if (maxDist > 0.01f)
			//{
			//	int num = Mathf.CeilToInt(maxDist);
			//	CellRect otherRect = new CellRect(vehiclePos.x - num, vehiclePos.z - num, num * 2 + 1, num * 2 + 1);
			//	cellRect.ClipInsideRect(otherRect);
			//}
			//int num2 = Mathf.CeilToInt(maxDist);
			//CellRect otherRect2 = new CellRect(vehiclePos.x - num2, vehiclePos.z - num2, num2 * 2 + 1, num2 * 2 + 1);
			//cellRect.ClipInsideRect(otherRect2);
			//if (CastPositionFinder.req.maxRangeFromLocus > 0.01f)
			//{
			//	int num3 = Mathf.CeilToInt(CastPositionFinder.req.maxRangeFromLocus);
			//	CellRect otherRect3 = new CellRect(vehiclePos.x - num3, vehiclePos.z - num3, num3 * 2 + 1, num3 * 2 + 1);
			//	cellRect.ClipInsideRect(otherRect3);
			//}
			//CastPositionFinder.bestSpot = IntVec3.Invalid;
			//CastPositionFinder.bestSpotPref = 0.001f;
			//CastPositionFinder.maxRangeFromCasterSquared = CastPositionFinder.req.maxRangeFromCaster * CastPositionFinder.req.maxRangeFromCaster;
			//CastPositionFinder.maxRangeFromTargetSquared = maxDist * maxDist;
			//CastPositionFinder.maxRangeFromLocusSquared = CastPositionFinder.req.maxRangeFromLocus * CastPositionFinder.req.maxRangeFromLocus;
			//CastPositionFinder.rangeFromTarget = (CastPositionFinder.req.caster.Position - CastPositionFinder.req.target.Position).LengthHorizontal;
			//CastPositionFinder.rangeFromTargetSquared = (float)(CastPositionFinder.req.caster.Position - CastPositionFinder.req.target.Position).LengthHorizontalSquared;
			//CastPositionFinder.optimalRangeSquared = CastPositionFinder.verb.verbProps.range * 0.8f * (CastPositionFinder.verb.verbProps.range * 0.8f);
			//if (CastPositionFinder.req.preferredCastPosition != null && CastPositionFinder.req.preferredCastPosition.Value.IsValid)
			//{
			//	CastPositionFinder.EvaluateCell(CastPositionFinder.req.preferredCastPosition.Value);
			//	if (CastPositionFinder.bestSpot.IsValid && CastPositionFinder.bestSpotPref > 0.001f)
			//	{
			//		dest = CastPositionFinder.req.preferredCastPosition.Value;
			//		return true;
			//	}
			//}
			//CastPositionFinder.EvaluateCell(CastPositionFinder.req.caster.Position);
			//if ((double)CastPositionFinder.bestSpotPref >= 1.0)
			//{
			//	dest = CastPositionFinder.req.caster.Position;
			//	return true;
			//}
			//float slope = -1f / CellLine.Between(CastPositionFinder.req.target.Position, CastPositionFinder.req.caster.Position).Slope;
			//CellLine cellLine = new CellLine(CastPositionFinder.req.target.Position, slope);
			//bool flag = cellLine.CellIsAbove(CastPositionFinder.req.caster.Position);
			//foreach (IntVec3 c in cellRect)
			//{
			//	if (cellLine.CellIsAbove(c) == flag && cellRect.Contains(c))
			//	{
			//		CastPositionFinder.EvaluateCell(c);
			//	}
			//}
			//if (CastPositionFinder.bestSpot.IsValid && CastPositionFinder.bestSpotPref > 0.33f)
			//{
			//	dest = CastPositionFinder.bestSpot;
			//	return true;
			//}
			//foreach (IntVec3 c2 in cellRect)
			//{
			//	if (cellLine.CellIsAbove(c2) != flag && cellRect.Contains(c2))
			//	{
			//		CastPositionFinder.EvaluateCell(c2);
			//	}
			//}
			//if (CastPositionFinder.bestSpot.IsValid)
			//{
			//	dest = CastPositionFinder.bestSpot;
			//	return true;
			//}
			//dest = CastPositionFinder.casterLoc;
			//return false;
		}
	}
}
