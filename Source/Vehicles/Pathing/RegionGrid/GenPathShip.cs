using RimWorld;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles.AI
{
	public static class GenPathShip
	{
		public static TargetInfo ResolvePathMode(Pawn pawn, TargetInfo dest, ref PathEndMode peMode)
		{
			if(dest.HasThing && dest.Thing.Spawned)
			{
				peMode = PathEndMode.Touch;
				return dest;
			}
			if(peMode == PathEndMode.InteractionCell)
			{
				if(!dest.HasThing)
				{
					Log.Error("Pathed to cell " + dest + " with PathEndMode.InteractionCell.");
				}
				peMode = PathEndMode.OnCell;
				return new TargetInfo(dest.Thing.InteractionCell, dest.Thing.Map, false);
			}
			if(peMode == PathEndMode.ClosestTouch)
			{
				peMode = ResolveClosestTouchPathMode(pawn, pawn.Map.GetCachedMapComponent<WaterMap>(), dest.Cell);
			}
			return dest;
		}

		public static PathEndMode ResolveClosestTouchPathMode(Pawn pawn, WaterMap mapE, IntVec3 target)
		{
			if(ShouldNotEnterCell(pawn, mapE, target))
			{
				return PathEndMode.Touch;
			}
			return PathEndMode.OnCell;
		}

		private static bool ShouldNotEnterCell(Pawn pawn, WaterMap mapE, IntVec3 dest)
		{
			if(mapE.ShipPathGrid.PerceivedPathCostAt(dest) > 30)
			{
				return true;
			}
			if(!GenGridShips.Walkable(dest, mapE))
			{
				return true;
			}
			if(pawn is VehiclePawn)
			{
				if(dest.IsForbidden(pawn))
				{
					return true;
				}
				//Add utility for doors later?
			}

			return false;
		}
	}
}
