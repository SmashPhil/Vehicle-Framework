using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles.World;

internal static class CaravanGrouper
{
	public static List<Group> ExtractIncompatibleCaravanGroups(List<VehiclePawn> vehicles, List<Pawn> pawns)
	{
		List<Group> groups = [];
		foreach (VehiclePawn vehicle in vehicles)
		{
			if (!TryAddToGroup(groups, vehicle))
			{
				Group group = new()
				{
					Type = vehicle.VehicleDef.type
				};
				group.vehicles.Add(vehicle);
				groups.Add(group);
				if (group.Type == VehicleType.Land)
				{
					group.pawns.AddRange(pawns);
					pawns.Clear();
				}
			}
		}
		return groups;

		static bool TryAddToGroup(List<Group> groups, VehiclePawn vehicle)
		{
			// Aerial vehicles are restricted to 1 per object. This should be removed if this changes.
			if (vehicle.VehicleDef.type == VehicleType.Air)
				return false;

			foreach (Group group in groups)
			{
				if (group.vehicles.FirstOrDefault()?.VehicleDef.type == vehicle.VehicleDef.type ||
					vehicle.VehicleDef.type is VehicleType.Universal)
				{
					group.vehicles.Add(vehicle);
					return true;
				}
			}
			return false;
		}
	}

	public class Group
	{
		public readonly List<Pawn> pawns = [];
		public readonly List<VehiclePawn> vehicles = [];

		public IEnumerable<Pawn> AllPawns => pawns.Concat(vehicles);

		public VehicleType Type { get; init; }
	}
}