using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public static class SelectionHelper
	{
		public static bool MultiSelectClicker(List<object> selectedObjects)
		{
			if (!selectedObjects.All(x => x is Pawn))
			{
				return false;
			}
			List<Pawn> selPawns = new List<Pawn>();
			foreach(object o in selectedObjects)
			{
				if (o is Pawn)
				{
					selPawns.Add(o as Pawn);
				}
			}
			if (selPawns.NotNullAndAny(x => x.Faction != Faction.OfPlayer || x is VehiclePawn))
			{
				return false;
			}
			IntVec3 mousePos = Verse.UI.MouseMapPosition().ToIntVec3();
			if (selectedObjects.Count > 1 && selectedObjects.All(x => x is Pawn))
			{
				foreach (Thing thing in selPawns[0].Map.thingGrid.ThingsAt(mousePos))
				{
					if (thing is VehiclePawn)
					{
						(thing as VehiclePawn).MultiplePawnFloatMenuOptions(selPawns);
						return true;
					}
				}
			}
			return false;
		}
	}
}
