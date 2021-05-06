using Verse;
using RimWorld;
using RimWorld.Planet;
using Vehicles.Defs;

namespace Vehicles
{
	public static class DebugHelper
	{
		public static void DebugDrawSettlement(int from, int to)
		{
			PeaceTalks o = (PeaceTalks)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfVehicles.DebugSettlement);
			o.Tile = from;
			o.SetFaction(Faction.OfMechanoids);
			Find.WorldObjects.Add(o);
			if (VehicleHarmony.drawPaths)
				VehicleHarmony.debugLines.Add(Find.WorldPathFinder.FindPath(from, to, null, null));
		}
	}
}
