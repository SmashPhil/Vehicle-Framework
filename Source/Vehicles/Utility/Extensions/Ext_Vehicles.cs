using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using Vehicles.AI;
using Vehicles.Lords;

namespace Vehicles
{
	public static class Ext_Vehicles
	{
		/// <summary>
		/// Get AerialVehicle pawn is currently inside
		/// </summary>
		/// <param name="pawn"></param>
		/// <returns><c>null</c> if not currently inside an AerialVehicle</returns>
		public static AerialVehicleInFlight GetAerialVehicle(this Pawn pawn)
		{
			foreach (AerialVehicleInFlight aerial in VehicleWorldObjectsHolder.Instance.AerialVehicles)
			{
				if (aerial.vehicle == pawn || aerial.vehicle.AllPawnsAboard.Contains(pawn))
				{
					return aerial;
				}
			}
			return null;
		}

		/// <summary>
		/// Get all unique Vehicles in <paramref name="vehicles"/>
		/// </summary>
		/// <param name="vehicles"></param>
		public static HashSet<VehicleDef> UniqueVehicleDefsInList(this List<VehiclePawn> vehicles)
		{
			return vehicles.Select(v => v.VehicleDef).Distinct().ToHashSet();
		}

		/// <summary>
		/// Checking if thing is a boat
		/// </summary>
		/// <param name="thing"></param>
		public static bool IsBoat(this Thing thing)
		{
			return thing is VehiclePawn vehicle && vehicle.VehicleDef.vehicleType == VehicleType.Sea;
		}

		/// <summary>
		/// Any Vehicle exists in collection of pawns
		/// </summary>
		/// <param name="pawns"></param>
		public static bool HasVehicle(this IEnumerable<Pawn> pawns)
		{
			return pawns.NotNullAndAny(x => x is VehiclePawn);
		}

		/// <summary>
		/// Any Boat exists in collection of pawns
		/// </summary>
		/// <param name="pawns"></param>
		/// <returns></returns>
		public static bool HasBoat(this IEnumerable<Pawn> pawns)
		{
			return pawns?.NotNullAndAny(x => IsBoat(x)) ?? false;
		}

		/// <summary>
		/// Caravan contains one or more Vehicles
		/// </summary>
		/// <param name="pawn"></param>
		public static bool HasVehicleInCaravan(this Pawn pawn)
		{
			return pawn.IsFormingCaravan() && pawn.GetLord().LordJob is LordJob_FormAndSendVehicles && pawn.GetLord().ownedPawns.NotNullAndAny(p => p is VehiclePawn);
		}

		/// <summary>
		/// Get VehicleCaravan pawn is in
		/// </summary>
		/// <param name="pawn"></param>
		/// <returns><c>null</c> if pawn is not currently inside a VehicleCaravan</returns>
		public static VehicleCaravan GetVehicleCaravan(this Pawn pawn)
		{
			return pawn.ParentHolder as VehicleCaravan;
		}

		//REDO
		/// <summary>
		/// Vehicle speed should be reduced temporarily
		/// </summary>
		/// <param name="vehicle"></param>
		public static bool SlowSpeed(this VehiclePawn vehicle)
		{
			var lord = vehicle.GetLord();
			if (lord is null)
			{
				return false;
			}
			return false;// vehicleLordCategories.Contains(lord.LordJob.GetType());
		}

		/// <summary>
		/// Vehicle is able to travel on the coast of <paramref name="tile"/>
		/// </summary>
		/// <param name="vehicleDef"></param>
		/// <param name="tile"></param>
		public static bool CoastalTravel(this VehicleDef vehicleDef, int tile)
		{
			return (vehicleDef.vehicleType == VehicleType.Sea || 
				(vehicleDef.properties.customBiomeCosts.ContainsKey(BiomeDefOf.Ocean) && vehicleDef.properties.customBiomeCosts[BiomeDefOf.Ocean] <= WorldVehiclePathGrid.ImpassableMovementDifficulty) ) &&
				Find.World.CoastDirectionAt(tile).IsValid;
		}

		/// <summary>
		/// <paramref name="vehicle"/>
		/// </summary>
		/// <param name="vehicle"></param>
		/// <returns></returns>
		public static bool OnDeepWater(this VehiclePawn vehicle)
		{
			//Splitting Caravan?
			if (vehicle?.Map is null && vehicle.IsWorldPawn())
			{
				return false;
			}
			return (vehicle.Map.terrainGrid.TerrainAt(vehicle.Position) == TerrainDefOf.WaterDeep || vehicle.Map.terrainGrid.TerrainAt(vehicle.Position) == TerrainDefOf.WaterMovingChestDeep ||
				vehicle.Map.terrainGrid.TerrainAt(vehicle.Position) == TerrainDefOf.WaterOceanDeep) && GenGrid.Impassable(vehicle.Position, vehicle.Map);
		}

		/// <see cref="DrivableFast(VehiclePawn, IntVec3)"/>
		public static bool Drivable(this VehiclePawn vehicle, IntVec3 cell)
		{
			return cell.InBounds(vehicle.Map) && DrivableFast(vehicle, cell);
		}
		
		/// <see cref="DrivableFast(VehiclePawn, IntVec3)"/>
		public static bool DrivableFast(this VehiclePawn vehicle, int index)
		{
			IntVec3 cell = vehicle.Map.cellIndices.IndexToCell(index);
			return DrivableFast(vehicle, cell);
		}

		/// <see cref="DrivableFast(VehiclePawn, IntVec3)"/>
		public static bool DrivableFast(this VehiclePawn vehicle, int x, int z)
		{
			IntVec3 cell = new IntVec3(x, 0, z);
			return DrivableFast(vehicle, cell);
		}

		/// <summary>
		/// <paramref name="vehicle"/> is able to move into <paramref name="cell"/>
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="cell"></param>
		public static bool DrivableFast(this VehiclePawn vehicle, IntVec3 cell)
		{
			VehiclePawn claimedBy = vehicle.Map.GetCachedMapComponent<VehiclePositionManager>().ClaimedBy(cell);
			bool passable = (claimedBy is null || claimedBy == vehicle) && (vehicle.VehicleDef.vehicleType == VehicleType.Sea ?
				vehicle.Map.GetCachedMapComponent<VehicleMapping>().VehiclePathGrid.WalkableFast(cell) : (vehicle.Map.pathGrid.pathGrid[vehicle.Map.cellIndices.CellToIndex(cell)] < 10000));
			return passable;
		}

		public static bool LocationRestrictedBySize(this VehiclePawn pawn, IntVec3 dest)
		{
			return CellRect.CenteredOn(dest, pawn.def.Size.x, pawn.def.Size.z).NotNullAndAny(c2 => pawn.IsBoat() ? (!c2.InBoundsShip(pawn.Map) || GenGridVehicles.Impassable(c2, pawn.Map)) : 
																												   (!c2.InBounds(pawn.Map) || MultithreadHelper.ImpassableReverseThreaded(c2, pawn.Map, pawn))) && 
																								   CellRect.CenteredOn(dest, pawn.def.Size.z, pawn.def.Size.x).NotNullAndAny(c2 => pawn.IsBoat() 
																												 ? (!c2.InBoundsShip(pawn.Map) || GenGridVehicles.Impassable(c2, pawn.Map)) : 
																												   (!c2.InBounds(pawn.Map) || MultithreadHelper.ImpassableReverseThreaded(c2, pawn.Map, pawn)));
		}

		/// <summary>
		/// Ensures the cellrect inhabited by the vehicle contains no Things that will block pathing and movement.
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="c"></param>
		public static bool CellRectStandable(this VehiclePawn vehicle, Map map, IntVec3? c = null, Rot4? rot = null)
		{
			IntVec3 loc = c ?? vehicle.Position;
			IntVec2 dimensions = vehicle.VehicleDef.Size;
			if (rot?.IsHorizontal ?? false)
			{
				int x = dimensions.x;
				dimensions.x = dimensions.z;
				dimensions.z = x;
			}
			foreach(IntVec3 cell in CellRect.CenteredOn(loc, dimensions.x, dimensions.z))
			{
				if(vehicle.IsBoat() && !GenGridVehicles.Standable(cell, map))
				{
					return false;
				}
				else if(!GenGrid.Standable(cell, map))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Seats assigned to vehicle in caravan formation
		/// </summary>
		/// <param name="vehicle"></param>
		public static int CountAssignedToVehicle(this VehiclePawn vehicle)
		{
			return CaravanHelper.assignedSeats.Where(a => a.Value.First == vehicle).Select(s => s.Key).Count();
		}
	}
}
