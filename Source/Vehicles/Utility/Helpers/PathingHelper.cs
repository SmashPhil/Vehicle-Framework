using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using Vehicles.AI;


namespace Vehicles
{
	public static class PathingHelper
	{
		/// <summary>
		/// Check if cell is currently claimed by a vehicle
		/// </summary>
		/// <param name="map"></param>
		/// <param name="cell"></param>
		public static bool VehicleInCell(Map map, IntVec3 cell)
		{
			return map.GetCachedMapComponent<VehiclePositionManager>().PositionClaimed(cell);
		}

		/// <see cref="VehicleInCell(Map, IntVec3)"/>
		public static bool VehicleInCell(Map map, int x, int z)
		{
			return VehicleInCell(map, new IntVec3(x, 0, z));
		}

		/// <summary>
		/// Calculate angle of Vehicle
		/// </summary>
		/// <param name="pawn"></param>
		public static float CalculateAngle(this VehiclePawn vehicle, out bool northSouthRotation)
		{
			northSouthRotation = false;
			if (vehicle is null) return 0f;
			if (vehicle.vPather.Moving)
			{
				IntVec3 c = vehicle.vPather.nextCell - vehicle.Position;
				if (c.x > 0 && c.z > 0)
				{
					vehicle.Angle = -45f;
				}
				else if (c.x > 0 && c.z < 0)
				{
					vehicle.Angle = 45f;
				}
				else if (c.x < 0 && c.z < 0)
				{
					vehicle.Angle = -45f;
				}
				else if (c.x < 0 && c.z > 0)
				{
					vehicle.Angle = 45f;
				}
				else
				{
					vehicle.Angle = 0f;
				}
			}
			if (vehicle.VehicleGraphic.EastDiagonalRotated && (vehicle.FullRotation == Rot8.NorthEast || vehicle.FullRotation == Rot8.SouthEast) ||
				(vehicle.VehicleGraphic.WestDiagonalRotated && (vehicle.FullRotation == Rot8.NorthWest || vehicle.FullRotation == Rot8.SouthWest)))
			{
				northSouthRotation = true;
			}
			return vehicle.Angle;
		}
	}
}
