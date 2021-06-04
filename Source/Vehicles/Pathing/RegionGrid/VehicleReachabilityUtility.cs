using System.Collections.Generic;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles.AI
{
	public static class ShipReachabilityUtility
	{
		public static bool CanReachShip(this Pawn pawn, LocalTargetInfo dest, PathEndMode peMode, Danger maxDanger, bool canBash = false, TraverseMode mode = TraverseMode.ByPawn)
		{
			return pawn.Spawned && pawn.Map.GetCachedMapComponent<VehicleMapping>().VehicleReachability.CanReachShip(pawn.Position, dest, peMode, TraverseParms.For(pawn, maxDanger, mode, canBash));
		}

		public static bool CanReachShipNonLocal(this Pawn pawn, TargetInfo dest, PathEndMode peMode, Danger maxDanger, bool canBash = false, TraverseMode mode = TraverseMode.ByPawn)
		{
			return pawn.Spawned && pawn.Map.GetCachedMapComponent<VehicleMapping>().VehicleReachability.CanReachShipNonLocal(pawn.Position, dest, peMode, TraverseParms.For(pawn, maxDanger, mode, canBash));
		}

		public static bool CanReachShipMapEdge(this Pawn pawn)
		{
			return pawn.Spawned && pawn.Map.GetCachedMapComponent<VehicleMapping>().VehicleReachability.CanReachMapEdge(pawn.Position, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false));
		}

		public static void ClearCache()
		{
			List<Map> maps = Find.Maps;
			for (int i = 0; i < maps.Count; i++)
			{
				maps[i].reachability.ClearCache();
				maps[i].GetCachedMapComponent<VehicleMapping>().VehicleReachability.ClearCache();
			}
		}

		public static void ClearCacheFor(Pawn pawn)
		{
			List<Map> maps = Find.Maps;
			for (int i = 0; i < maps.Count; i++)
			{
				maps[i].reachability.ClearCacheFor(pawn);
				maps[i].GetCachedMapComponent<VehicleMapping>().VehicleReachability.ClearCacheFor(pawn);
			}
		}
	}
}
