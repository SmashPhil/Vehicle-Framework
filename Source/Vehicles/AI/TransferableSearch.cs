using RimWorld;
using SmashTools.Performance;
using Verse;
using Verse.AI.Group;

namespace Vehicles;

public sealed class TransferableSearch : ISharedJobSearch, IPoolable
{
	private JobDef jobDef;
	private Lord lord;
	private TransferableOneWay transferable;

	bool IPoolable.InPool { get; set; }

	ThingDef ISharedJobSearch.ThingDef => transferable.ThingDef;

	public void Init(JobDef jobDef, TransferableOneWay transferable)
	{
		this.jobDef = jobDef;
		this.transferable = transferable;
	}

	public void Init(JobDef jobDef, TransferableOneWay transferable, Lord lord)
	{
		Init(jobDef, transferable);
		this.lord = lord;
	}

	bool ISharedJobSearch.IsMatchingThing(Thing thing)
	{
		return transferable.things.Contains(thing);
	}

	bool ISharedJobSearch.ShouldConsiderPawn(Pawn otherPawn)
	{
		Lord otherLord = otherPawn.lord ?? otherPawn.CurJob?.lord;
		return otherLord == lord && otherPawn.CurJobDef == jobDef;
	}

	void IPoolable.Reset()
	{
		jobDef = null;
		transferable = null;
		lord = null;
	}
}