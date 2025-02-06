using System;
using System.Collections.Generic;
using RimWorld;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Vehicles
{
	public static class CombatPositionFinder
	{
		public static bool TryFindCastPosition(in CastPositionRequest req, out IntVec3 dest)
		{
			dest = req.vehicle.Position;
			//bool found = false;
			//found = vehicle.Position.InHorDistOf(target.Position, maxDist) &&
			//	GenSight.LineOfSightToThing(vehicle.Position, target, vehicle.Map, skipFirstCell: true);
			//return found;

			VehiclePawn vehicle = req.vehicle;
			Map map = vehicle.Map;
			IntVec3 vehiclePos = vehicle.Position;
			IntVec3 targetPos = req.target.Cell;

			float maxRangeSquared = req.maxRange * req.maxRange;
			float maxRangeFromLocusSquared = req.maxRangeFromLocus * req.maxRangeFromLocus;
			float rangeFromTarget = (vehiclePos - targetPos).LengthHorizontal;
			float rangeFromTargetSquared = (vehiclePos - targetPos).LengthHorizontalSquared;
			float optimalRangeSquared = req.range * req.range;
			float rangeFromTargetToCellSquared = float.NaN;
			float rangeFromCasterToCellSquared = float.NaN;

			ByteGrid avoidGrid = vehicle.GetAvoidGrid(false);
			CellRect searchRect = CellRect.WholeMap(map);

			int maxRegions = req.maxRegions;

			if (req.maxRegions > 0)
			{
				VehicleRegion region = VehicleRegionAndRoomQuery.RegionAt(vehiclePos, map, vehicle.VehicleDef, RegionType.Set_Passable);
				if (region == null)
				{
					Log.Error("TryFindCastPosition requiring region traversal but root region is null.");
					dest = IntVec3.Invalid;
					return false;
				}
				int inRadiusMark = Rand.Int;
				VehicleRegionTraverser.MarkRegionsBFS(region, null, req.maxRegions, inRadiusMark, RegionType.Set_Passable);
				if (req.maxRangeFromLocus > 0.01f)
				{
					VehicleRegion locusReg = VehicleRegionAndRoomQuery.RegionAt(req.locus, map, vehicle.VehicleDef, RegionType.Set_Passable);
					if (locusReg == null)
					{
						Log.Error($"locus {req.locus} has no region");
						dest = IntVec3.Invalid;
						return false;
					}
					if (locusReg.mark != inRadiusMark)
					{
						inRadiusMark = Rand.Int;
						VehicleRegionTraverser.BreadthFirstTraverse(region, null, delegate (VehicleRegion reg)
						{
							reg.mark = inRadiusMark;
							maxRegions++;
							return reg == locusReg;
						}, 999999, RegionType.Set_Passable);
					}
				}
			}

			// Clip inside target range
			int maxRangeTargetInt = Mathf.CeilToInt(req.maxRange);
			CellRect maxRangeTargetRect = new CellRect(targetPos.x - maxRangeTargetInt, targetPos.z - maxRangeTargetInt, 
				maxRangeTargetInt * 2 + 1, maxRangeTargetInt * 2 + 1);
			searchRect.ClipInsideRect(maxRangeTargetRect);

			if (req.maxRangeFromLocus > 0.01f)
			{
				// Clip inside locus range
				int maxRangeLocusInt = Mathf.CeilToInt(req.maxRangeFromLocus);
				CellRect maxRangeLocusRect = new CellRect(targetPos.x - maxRangeLocusInt, targetPos.z - maxRangeLocusInt, 
					maxRangeLocusInt * 2 + 1, maxRangeLocusInt * 2 + 1);
				searchRect.ClipInsideRect(maxRangeLocusRect);
			}

			IntVec3 bestSpot = IntVec3.Invalid;
			float bestSpotPref = 0.001f;
			
			if (req.preferredCastPosition != null && req.preferredCastPosition.Value.IsValid)
			{
				EvaluateCell(req, req.preferredCastPosition.Value);
				if (bestSpot.IsValid && bestSpotPref > 0.001f)
				{
					dest = req.preferredCastPosition.Value;
					return true;
				}
			}

			EvaluateCell(req, vehiclePos);

			if (bestSpotPref >= 1.0f)
			{
				dest = vehiclePos;
				return true;
			}

			float slope = -1f / CellLine.Between(targetPos, vehiclePos).Slope;
			CellLine cellLine = new CellLine(targetPos, slope);
			bool cellAbove = cellLine.CellIsAbove(vehiclePos);
			foreach (IntVec3 cell in searchRect)
			{
				if (cellLine.CellIsAbove(cell) == cellAbove && searchRect.Contains(cell))
				{
					EvaluateCell(req, cell);
				}
			}
			if (bestSpot.IsValid && bestSpotPref > 0.33f)
			{
				dest = bestSpot;
				return true;
			}

			foreach (IntVec3 cell in searchRect)
			{
				if (cellLine.CellIsAbove(cell) != cellAbove && searchRect.Contains(cell))
				{
					EvaluateCell(req, cell);
				}
			}
			if (bestSpot.IsValid)
			{
				dest = bestSpot;
				return true;
			}

			dest = vehiclePos;
			return false;

			// Non-static, needs to modify variables from the caller
			void EvaluateCell(in CastPositionRequest req, IntVec3 cell)
			{
				if (req.validator != null && !req.validator(cell))
				{
					return;
				}

				// Locus range limit
				if (maxRangeFromLocusSquared > 0.01f && (cell - req.locus).LengthHorizontalSquared > maxRangeFromLocusSquared)
				{
					if (DebugViewSettings.drawCastPositionSearch)
					{
						map.debugDrawer.FlashCell(cell, 0.1f, "home", 50);
					}
					return;
				}
				// Caster range limit
				if (maxRangeSquared > 0.01f)
				{
					rangeFromCasterToCellSquared = (cell - vehicle.Position).LengthHorizontalSquared;
					if (rangeFromCasterToCellSquared > maxRangeSquared)
					{
						if (DebugViewSettings.drawCastPositionSearch)
						{
							map.debugDrawer.FlashCell(cell, 0.2f, "cstr", 50);
						}
						return;
					}
				}
				if (!cell.Standable(map))
				{
					return;
				}

				int inRadiusMark = Rand.Int;
				if (req.maxRegions > 0 && VehicleRegionAndRoomQuery.RegionAt(cell, map, vehicle.VehicleDef).mark != inRadiusMark)
				{
					if (DebugViewSettings.drawCastPositionSearch)
					{
						map.debugDrawer.FlashCell(cell, 0.64f, "rad mark", 50);
					}
					return;
				}

				if (!vehicle.CanReachVehicle(cell, PathEndMode.OnCell, Danger.Some))
				{
					if (DebugViewSettings.drawCastPositionSearch)
					{
						map.debugDrawer.FlashCell(cell, 0.4f, "can't reach", 50);
					}
					return;
				}
				float pref = CastPositionPreference(req, cell);
				if (avoidGrid != null)
				{
					byte b = avoidGrid[cell];
					pref *= Mathf.Max(0.1f, (37.5f - b) / 37.5f);
				}
				if (DebugViewSettings.drawCastPositionSearch)
				{
					map.debugDrawer.FlashCell(cell, pref / 4f, pref.ToString("F3"), 50);
				}
				if (pref < bestSpotPref)
				{
					return;
				}
				//if (!verb.CanHitTargetFrom(cell, req.target))
				//{
				//	if (DebugViewSettings.drawCastPositionSearch)
				//	{
				//		map.debugDrawer.FlashCell(cell, 0.6f, "can't hit", 50);
				//	}
				//	return;
				//}
				if (!map.pawnDestinationReservationManager.CanReserve(cell, vehicle, false))
				{
					if (DebugViewSettings.drawCastPositionSearch)
					{
						map.debugDrawer.FlashCell(cell, pref * 0.9f, "resvd", 50);
					}
					return;
				}
				if (PawnUtility.KnownDangerAt(cell, map, vehicle))
				{
					if (DebugViewSettings.drawCastPositionSearch)
					{
						map.debugDrawer.FlashCell(cell, 0.9f, "danger", 50);
					}
					return;
				}
				bestSpot = cell;
				bestSpotPref = pref;

				float CastPositionPreference(in CastPositionRequest req, IntVec3 cell)
				{
					Assert.IsTrue(UnityData.IsInMainThread);
					Assert.IsTrue(!float.IsNaN(rangeFromTargetToCellSquared));
					Assert.IsTrue(!float.IsNaN(rangeFromCasterToCellSquared));

					bool passable = true;
					List<Thing> list = map.thingGrid.ThingsListAtFast(cell);
					for (int i = 0; i < list.Count; i++)
					{
						Thing thing = list[i];
						if (thing is Fire fire && fire.parent == null)
						{
							return -1f;
						}
						if (thing.def.passability == Traversability.PassThroughOnly)
						{
							passable = false;
						}
					}
					float scoreFinal = 0.3f;
					if (vehicle.kindDef.aiAvoidCover)
					{
						scoreFinal += 8f - CoverUtility.TotalSurroundingCoverScore(cell, map);
					}
					float scoreDist = (vehiclePos - cell).LengthHorizontal;
					if (rangeFromTarget > 100f)
					{
						scoreDist -= rangeFromTarget - 100f;
						if (scoreDist < 0f)
						{
							scoreDist = 0f;
						}
					}
					scoreFinal *= Mathf.Pow(0.967f, scoreDist);
					rangeFromTargetToCellSquared = (cell - targetPos).LengthHorizontalSquared;
					float scoreOptimal = Mathf.Abs(rangeFromTargetToCellSquared - optimalRangeSquared) / optimalRangeSquared;
					scoreOptimal = 1f - scoreOptimal;
					scoreOptimal = 0.7f + 0.3f * scoreOptimal;
					if (rangeFromTargetToCellSquared < 25f)
					{
						scoreOptimal *= 0.5f;
					}
					scoreFinal *= scoreOptimal;
					if (rangeFromCasterToCellSquared > rangeFromTargetSquared)
					{
						scoreFinal *= 0.4f;
					}
					if (!passable)
					{
						scoreFinal *= 0.2f;
					}
					return scoreFinal;
				}
			}
		}
	}

	public readonly struct CastPositionRequest
	{
		public readonly VehiclePawn vehicle;
		public readonly LocalTargetInfo target;

		public readonly float maxRange;
		public readonly float positionDistance;
		public readonly float maxRangeFromLocus;
		public readonly int maxRegions;
		public readonly float range;

		public readonly IntVec3 locus;

		public readonly IntVec3? preferredCastPosition;

		public readonly Func<IntVec3, bool> validator;

		public CastPositionRequest(VehiclePawn vehicle, LocalTargetInfo target, Func<IntVec3, bool> validator = null)
		{
			this.vehicle = vehicle;
			this.target = target;
			this.validator = validator;
			range = vehicle.CompVehicleTurrets.MaxRange * vehicle.VehicleDef.npcProperties.targetPositionRadiusPercent;
		}

		public CastPositionRequest(VehiclePawn vehicle, LocalTargetInfo target, IntVec3? preferredCastPosition, Func<IntVec3, bool> validator = null)
			: this(vehicle, target, validator: validator)
		{
			this.preferredCastPosition = preferredCastPosition;
		}

		public CastPositionRequest(VehiclePawn vehicle, LocalTargetInfo target, IntVec3? preferredCastPosition, float maxRange, float positionDistance,
			float maxRangeFromLocus, Func<IntVec3, bool> validator = null) : this(vehicle, target, validator: validator)
		{
			this.maxRange = maxRange;
			this.positionDistance = positionDistance;
			this.maxRangeFromLocus = maxRangeFromLocus;
		}

		public CastPositionRequest(VehiclePawn vehicle, LocalTargetInfo target, IntVec3? preferredCastPosition,
			float maxRangeFromCaster, float maxRangeFromTarget,
			float maxRangeFromLocus, int maxRegions, Func<IntVec3, bool> validator = null)
			: this(vehicle, target, preferredCastPosition, maxRangeFromCaster, maxRangeFromTarget, maxRangeFromLocus, validator: validator)
		{
			this.maxRegions = maxRegions;
		}

		public bool HasThing => target.HasThing;

		public static CastPositionRequest For(VehiclePawn vehicle)
		{
			VehicleNPCProperties npcProps = vehicle.VehicleDef.npcProperties;
			return new CastPositionRequest(vehicle, vehicle.mindState.enemyTarget, vehicle.Position,
				npcProps.targetAcquireRadius,
				vehicle.CompVehicleTurrets.MaxRange * npcProps.targetPositionRadiusPercent,
				npcProps.targetAcquireRadius);
		}
	}
}
