using CoreLib.Performance;
using Verse;

namespace Vehicles;

public class Paratrooper : IAirDroppable
{
	public Pawn pawn;

	Thing IAirDroppable.Thing => pawn;

	ThingDef IAirDroppable.SkyfallerDef => SkyfallerDefOf.AirdropParatrooper;

	public Paratrooper(Pawn pawn)
	{
		this.pawn = pawn;
	}

	void IExposable.ExposeData()
	{
		Scribe_Deep.Look(ref pawn, nameof(pawn));
	}

	void IAirDroppable.OnDropped(Map map, IntVec3 pos)
	{
#if DEBUG
		const float DropRadii = 8.9f;
		if (DebugSettings.ShowDevGizmos)
		{
			foreach (IntVec3 cell in GenRadial.RadialCellsAround(pos, DropRadii, true))
			{
				map.debugDrawer.FlashCell(cell, duration: 180);
			}
		}
#endif
	}

	void IAirDroppable.OnFailureToDrop(Map map, IntVec3 simPos)
	{
		const int TimeToEnter = 4 * 1000; // ms

		new Debouncer(delegate
		{
			IntVec3 loc = CellFinder.RandomClosewalkCellNear(simPos, map, 12);
			if (loc.IsValid && loc.InBounds(map))
			{
				GenSpawn.Spawn(pawn, loc, map, Rot4.Random);
			}
			else
			{
				pawn.DestroyOrPassToWorld();
			}
		}, TimeToEnter).Invoke();
	}
}